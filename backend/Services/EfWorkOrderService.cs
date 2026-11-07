using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Backend.Services;

// Заявки хранятся в PostgreSQL (таблица work_orders) через EF Core.
// Остановили API, запустили снова — заявки на месте: источник данных — база, а не List<> в памяти процесса.
// Права (ЛР11): кто выполняет действие, сервис узнаёт из currentUserId — это id из проверенного JWT.
// Отказ по роли или «чужая заявка» → Forbidden (403); нарушение жизненного цикла → Conflict (409).
// Именно здесь проверяется чужой id: «исполнитель этой строки == текущий пользователь».
public class EfWorkOrderService(AppDbContext db, ILogger<EfWorkOrderService> logger) : IWorkOrderService
{
    // Имя UNIQUE-индекса на work_orders.number (AppDbContext, schema.sql).
    private const string NumberUniqueConstraint = "work_orders_number_key";

    public IReadOnlyList<WorkOrder> GetAll() =>
        // SELECT … FROM work_orders AS w LEFT JOIN users AS u ON u.id = w.assignee_user_id
        // ORDER BY w.created_at DESC, w.id DESC
        db.WorkOrders
            .AsNoTracking()               // только чтение: EF не следит за изменениями
            .Include(w => w.Assignee)     // ФИО исполнителя для ответа — JOIN, а не второй запрос
            .OrderByDescending(w => w.CreatedAt)
            .ThenByDescending(w => w.Id)
            .ToList();

    public ServiceResult<WorkOrder> GetById(int id)
    {
        // SELECT … FROM work_orders AS w LEFT JOIN users AS u … WHERE w.id = @id LIMIT 1
        var workOrder = db.WorkOrders
            .AsNoTracking()
            .Include(w => w.Assignee)
            .FirstOrDefault(w => w.Id == id);

        return workOrder is null
            ? ServiceResult<WorkOrder>.NotFound($"Заявка {id} не найдена")
            : ServiceResult<WorkOrder>.Ok(workOrder);
    }

    public ServiceResult<WorkOrder> Create(CreateWorkOrderRequest request, int currentUserId, bool simulateFailure = false)
    {
        // Формат полей уже проверен атрибутами DTO ([ApiController] → 400). Здесь — правила со справочниками из БД.
        var equipmentId = request.EquipmentId!.Value;
        if (!db.Equipment.Any(e => e.Id == equipmentId))
        {
            return ServiceResult<WorkOrder>.Invalid("equipmentId", "Оборудование с таким id не найдено");
        }

        var author = db.Users.AsNoTracking().FirstOrDefault(u => u.Id == currentUserId);
        if (author is null || (author.Role != UserRoles.Foreman && author.Role != UserRoles.Mechanic))
        {
            // Вторая линия защиты: [Authorize(Roles = …)] на контроллере уже отсекает другие роли.
            return ServiceResult<WorkOrder>.Forbidden("Создавать заявки может только мастер участка или механик цеха");
        }

        if (simulateFailure)
        {
            // Учебный сбой ЛР8: «баг» после успешной валидации, чтобы проверить лог и безопасный 500.
            throw new InvalidOperationException("Учебный сбой при создании заявки");
        }

        var now = DateTimeOffset.UtcNow;
        var workOrder = new WorkOrder
        {
            // id не задаём: его выдаёт PostgreSQL (identity), EF прочитает его после INSERT.
            Number = NextNumber(now.Year),
            Title = request.Title!.Trim(),
            Description = request.Description!.Trim(),
            Status = WorkOrderStatuses.New,   // всегда New, клиент статус не задаёт
            EquipmentId = equipmentId,
            CreatedByUserId = currentUserId,
            AssigneeUserId = null,            // исполнителя назначат отдельным PATCH
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.WorkOrders.Add(workOrder);
        try
        {
            db.SaveChanges(); // INSERT INTO work_orders (…) VALUES (…) RETURNING id
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, NumberUniqueConstraint))
        {
            // Два POST одновременно получили один и тот же следующий номер: второй INSERT отклонён
            // UNIQUE-индексом. Это ожидаемая гонка, а не авария: 409, клиент может повторить запрос.
            db.Entry(workOrder).State = EntityState.Detached;
            logger.LogWarning("CreateWorkOrder number conflict; number={Number}", workOrder.Number);
            return ServiceResult<WorkOrder>.Conflict(
                $"Номер {workOrder.Number} уже занят другой заявкой. Повторите запрос");
        }

        return ServiceResult<WorkOrder>.Ok(workOrder);
    }

    public ServiceResult<WorkOrder> Assign(int id, int assigneeUserId, int currentUserId)
    {
        // ФТ-9: назначает только механик цеха. Вторая линия защиты после [Authorize(Roles = "Mechanic")].
        var current = db.Users.AsNoTracking().FirstOrDefault(u => u.Id == currentUserId);
        if (current?.Role != UserRoles.Mechanic)
        {
            return ServiceResult<WorkOrder>.Forbidden("Назначать исполнителя может только механик цеха");
        }

        var assignee = db.Users.FirstOrDefault(u => u.Id == assigneeUserId);
        if (assignee is null || assignee.Role != UserRoles.Fitter)
        {
            return ServiceResult<WorkOrder>.Invalid("assigneeUserId", "Пользователь не найден или не является слесарем-ремонтником");
        }

        // Без AsNoTracking: EF следит за загруженной строкой и в SaveChanges отправит UPDATE только изменённых столбцов.
        var workOrder = db.WorkOrders.FirstOrDefault(w => w.Id == id);
        if (workOrder is null)
        {
            return ServiceResult<WorkOrder>.NotFound($"Заявка {id} не найдена");
        }

        if (workOrder.Status != WorkOrderStatuses.New)
        {
            return ServiceResult<WorkOrder>.Conflict("Назначить исполнителя можно только для заявки в статусе «Новая»");
        }

        // Назначение не меняет статус: заявка остаётся New.
        workOrder.AssigneeUserId = assigneeUserId;
        workOrder.Assignee = assignee;
        workOrder.UpdatedAt = DateTimeOffset.UtcNow;
        db.SaveChanges(); // UPDATE work_orders SET assignee_user_id = …, updated_at = … WHERE id = …
        return ServiceResult<WorkOrder>.Ok(workOrder);
    }

    public ServiceResult<WorkOrder> ChangeStatus(int id, string newStatus, int currentUserId)
    {
        var workOrder = db.WorkOrders
            .Include(w => w.Assignee)
            .FirstOrDefault(w => w.Id == id);
        if (workOrder is null)
        {
            return ServiceResult<WorkOrder>.NotFound($"Заявка {id} не найдена");
        }

        var user = db.Users.AsNoTracking().FirstOrDefault(u => u.Id == currentUserId);
        if (user is null)
        {
            return ServiceResult<WorkOrder>.Forbidden(NoRights);
        }

        var refusal = CheckTransition(workOrder, newStatus, user);
        if (refusal is not null)
        {
            return refusal;
        }

        workOrder.Status = newStatus;
        workOrder.UpdatedAt = DateTimeOffset.UtcNow;
        db.SaveChanges(); // UPDATE work_orders SET status = …, updated_at = … WHERE id = …
        return ServiceResult<WorkOrder>.Ok(workOrder);
    }

    // Текст отказа из ФТ-8 — его же клиент показывает человеку.
    private const string NoRights = "Нет прав на изменение этой заявки";

    // Таблица переходов статусов (docs/data-model.md, §6) и права на каждый переход (docs/role-matrix.md).
    // Возвращает отказ (403 или 409) или null, если переход разрешён.
    // Порядок проверок: сначала «бывает ли такой переход вообще» (409), потом «можно ли тебе» (403).
    private static ServiceResult<WorkOrder>? CheckTransition(WorkOrder w, string to, AppUser user)
    {
        switch (w.Status, to)
        {
            case (WorkOrderStatuses.New, WorkOrderStatuses.InProgress):
            case (WorkOrderStatuses.InProgress, WorkOrderStatuses.Closed):
                // В ремонт и «выполнена» двигает только слесарь-ремонтник…
                if (user.Role != UserRoles.Fitter)
                    return ServiceResult<WorkOrder>.Forbidden($"{NoRights}: статус ремонта меняет только назначенный слесарь");
                // …и только когда исполнитель вообще назначен (правило процесса, не права) …
                if (w.AssigneeUserId is null)
                    return ServiceResult<WorkOrder>.Conflict("Нельзя перевести в ремонт: исполнитель не назначен");
                // …и только свою заявку: чужой id в адресе не помогает (ФТ-8).
                if (w.AssigneeUserId != user.Id)
                    return ServiceResult<WorkOrder>.Forbidden($"{NoRights}: она назначена другому слесарю");
                return null;

            case (WorkOrderStatuses.New, WorkOrderStatuses.Cancelled):
                // ФТ-7: отменяет механик цеха или мастер — автор этой заявки.
                if (user.Role == UserRoles.Mechanic)
                    return null;
                if (user.Role == UserRoles.Foreman && w.CreatedByUserId == user.Id)
                    return null;
                return ServiceResult<WorkOrder>.Forbidden($"{NoRights}: отменить может механик цеха или мастер — автор заявки");

            default:
                // ФТ-10: «Новая» → «Выполнена», любые изменения из «Выполнена» и «Отменена».
                return ServiceResult<WorkOrder>.Conflict($"Недопустимый переход статуса: {w.Status} → {to}");
        }
    }

    // Следующий номер ЗР-ГГГГ-NNNN за текущий год.
    // SELECT number FROM work_orders WHERE number LIKE 'ЗР-2026-%' ORDER BY number DESC LIMIT 1
    // Номера дополнены нулями до 4 цифр, поэтому строковая сортировка совпадает с числовой.
    private string NextNumber(int year)
    {
        var prefix = $"ЗР-{year}-";
        var last = db.WorkOrders
            .Where(w => w.Number.StartsWith(prefix))
            .OrderByDescending(w => w.Number)
            .Select(w => w.Number)
            .FirstOrDefault();

        var next = last is not null && int.TryParse(last[prefix.Length..], out var n) ? n + 1 : 1;
        return $"{prefix}{next:D4}";
    }

    private static bool IsUniqueViolation(DbUpdateException ex, string constraintName) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
        && pg.ConstraintName == constraintName;
}
