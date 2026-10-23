using Backend.Data;
using Backend.Dtos;
using Backend.Models;

namespace Backend.Services;

// Заявки хранятся в памяти процесса: после Ctrl+C и нового dotnet run список снова пустой.
// Хранение в PostgreSQL — ЛР10; контроллер при этом не изменится (DI, IWorkOrderService).
public class InMemoryWorkOrderService : IWorkOrderService
{
    private readonly List<WorkOrder> _workOrders = new();
    private readonly Lock _lock = new(); // singleton: запросы приходят параллельно
    private int _nextId = 1;

    public IReadOnlyList<WorkOrder> GetAll()
    {
        lock (_lock)
        {
            // Новые заявки — первыми. Пустой список — это просто пустой массив, не ошибка.
            return _workOrders.OrderByDescending(w => w.Id).ToList();
        }
    }

    public ServiceResult<WorkOrder> GetById(int id)
    {
        lock (_lock)
        {
            var workOrder = Find(id);
            return workOrder is null
                ? ServiceResult<WorkOrder>.NotFound($"Заявка {id} не найдена")
                : ServiceResult<WorkOrder>.Ok(workOrder);
        }
    }

    public ServiceResult<WorkOrder> Create(CreateWorkOrderRequest request, int currentUserId, bool simulateFailure = false)
    {
        // Формат полей уже проверен атрибутами DTO ([ApiController] → 400). Здесь — правила со справочниками.
        var equipmentId = request.EquipmentId!.Value;
        if (!ReferenceData.EquipmentExists(equipmentId))
        {
            return ServiceResult<WorkOrder>.Invalid("equipmentId", "Оборудование с таким id не найдено");
        }

        var author = ReferenceData.FindUser(currentUserId);
        if (author is null || (author.Role != UserRoles.Foreman && author.Role != UserRoles.Mechanic))
        {
            return ServiceResult<WorkOrder>.Conflict("Создавать заявки может только мастер участка или механик цеха");
        }

        if (simulateFailure)
        {
            // Учебный сбой ЛР8: «баг» после успешной валидации, чтобы проверить лог и безопасный 500.
            throw new InvalidOperationException("Учебный сбой при создании заявки");
        }

        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            var id = _nextId++;
            var workOrder = new WorkOrder
            {
                Id = id,
                Number = $"ЗР-{now.Year}-{id:D4}",
                Title = request.Title!.Trim(),
                Description = request.Description!.Trim(),
                Status = WorkOrderStatuses.New,   // всегда New, клиент статус не задаёт
                EquipmentId = equipmentId,
                CreatedByUserId = currentUserId,
                AssigneeUserId = null,            // исполнителя назначат отдельным PATCH
                CreatedAt = now,
                UpdatedAt = now,
            };
            _workOrders.Add(workOrder);
            return ServiceResult<WorkOrder>.Ok(workOrder);
        }
    }

    public ServiceResult<WorkOrder> Assign(int id, int assigneeUserId)
    {
        var assignee = ReferenceData.FindUser(assigneeUserId);
        if (assignee is null || assignee.Role != UserRoles.Fitter)
        {
            return ServiceResult<WorkOrder>.Invalid("assigneeUserId", "Пользователь не найден или не является слесарем-ремонтником");
        }

        lock (_lock)
        {
            var workOrder = Find(id);
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
            workOrder.UpdatedAt = DateTimeOffset.UtcNow;
            return ServiceResult<WorkOrder>.Ok(workOrder);
        }
    }

    public ServiceResult<WorkOrder> ChangeStatus(int id, string newStatus, int currentUserId)
    {
        lock (_lock)
        {
            var workOrder = Find(id);
            if (workOrder is null)
            {
                return ServiceResult<WorkOrder>.NotFound($"Заявка {id} не найдена");
            }

            var conflict = CheckTransition(workOrder, newStatus, currentUserId);
            if (conflict is not null)
            {
                return ServiceResult<WorkOrder>.Conflict(conflict);
            }

            workOrder.Status = newStatus;
            workOrder.UpdatedAt = DateTimeOffset.UtcNow;
            return ServiceResult<WorkOrder>.Ok(workOrder);
        }
    }

    // Таблица переходов статусов (docs/data-model.md, §6) — в одном месте, а не в каждом методе контроллера.
    // Возвращает текст отказа (→ 409) или null, если переход разрешён.
    private static string? CheckTransition(WorkOrder w, string to, int currentUserId)
    {
        var from = w.Status;
        var user = ReferenceData.FindUser(currentUserId);

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

    private WorkOrder? Find(int id) => _workOrders.FirstOrDefault(w => w.Id == id);
}
