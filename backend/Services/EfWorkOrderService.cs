using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Backend.Services;

// Заявки хранятся в PostgreSQL (таблица work_orders) через EF Core.
// Остановили API, запустили снова — заявки на месте: источник данных — база, а не List<> в памяти процесса.
// Правила процесса те же, что в ЛР8; контроллер не изменился (DI, IWorkOrderService).
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
            return ServiceResult<WorkOrder>.Conflict("Создавать заявки может только мастер участка или механик цеха");
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

    public ServiceResult<WorkOrder> Assign(int id, int assigneeUserId)
    {
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
        var conflict = CheckTransition(workOrder, newStatus, currentUserId, user);
        if (conflict is not null)
        {
            return ServiceResult<WorkOrder>.Conflict(conflict);
        }

        workOrder.Status = newStatus;
        workOrder.UpdatedAt = DateTimeOffset.UtcNow;
        db.SaveChanges(); // UPDATE work_orders SET status = …, updated_at = … WHERE id = …
        return ServiceResult<WorkOrder>.Ok(workOrder);
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

    // Таблица переходов статусов (docs/data-model.md, §6) — в одном месте, а не в каждом методе контроллера.
    // Возвращает текст отказа (→ 409) или null, если переход разрешён.
    private static string? CheckTransition(WorkOrder w, string to, int currentUserId, AppUser? user)
    {
        var from = w.Status;

        switch (from, to)
        {
            case (WorkOrderStatuses.New, WorkOrderStatuses.InProgress):
                if (w.AssigneeUserId is null)
                    return "Нельзя перевести в ремонт: исполнитель не назначен";
                if (w.AssigneeUserId != currentUserId)
                    return "Перевести в ремонт может только назначенный слесарь";
                return null;

            case (WorkOrderStatuses.InProgress, WorkOrderStatuses.Closed):
                if (w.AssigneeUserId != currentUserId)
                    return "Завершить ремонт может только назначенный слесарь";
                return null;

            case (WorkOrderStatuses.New, WorkOrderStatuses.Cancelled):
                if (user?.Role == UserRoles.Mechanic)
                    return null;
                if (user?.Role == UserRoles.Foreman && w.CreatedByUserId == currentUserId)
                    return null;
                return "Отменить заявку может механик цеха или мастер — автор заявки";

            default:
                return $"Недопустимый переход статуса: {from} → {to}";
        }
    }
}
