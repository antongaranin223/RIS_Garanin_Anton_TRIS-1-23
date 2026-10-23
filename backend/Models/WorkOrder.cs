namespace Backend.Models;

// Заявка на ремонт / ТО — главный объект (docs/data-model.md, §2.1).
// Имена свойств в JSON становятся camelCase: id, number, title, equipmentId…
public class WorkOrder
{
    // Машинный ключ (PK); ставит сервер.
    public int Id { get; set; }

    // Человеческий номер ЗР-ГГГГ-NNNN; ставит сервер.
    public string Number { get; set; } = string.Empty;

    // Краткая суть неисправности, 5–80 символов.
    public string Title { get; set; } = string.Empty;

    // Симптомы и обстоятельства, 10–500 символов.
    public string Description { get; set; } = string.Empty;

    // Один из кодов WorkOrderStatuses; при создании — New.
    public string Status { get; set; } = WorkOrderStatuses.New;

    // FK на справочник оборудования.
    public int EquipmentId { get; set; }

    // FK на автора (мастер или механик).
    public int CreatedByUserId { get; set; }

    // FK на слесаря-исполнителя; у новой заявки пусто.
    public int? AssigneeUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

// Коды статусов — только эти четыре, в API и в БД (docs/api-contract.md).
public static class WorkOrderStatuses
{
    public const string New = "New";
    public const string InProgress = "InProgress";
    public const string Closed = "Closed";
    public const string Cancelled = "Cancelled";
}
