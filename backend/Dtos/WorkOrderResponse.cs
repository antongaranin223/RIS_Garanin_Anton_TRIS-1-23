using Backend.Data;
using Backend.Models;

namespace Backend.Dtos;

// Что сервер отдаёт клиенту о заявке (docs/api-contract.md, §3).
// Сущность WorkOrder хранится отдельно: DTO — это «формочка по сети», а не строка таблицы.
public record WorkOrderResponse(
    int Id,
    string Number,
    string Title,
    string Description,
    string Status,
    int EquipmentId,
    int CreatedByUserId,
    int? AssigneeUserId,
    UserShortResponse? Assignee,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static WorkOrderResponse From(WorkOrder w)
    {
        var assignee = w.AssigneeUserId is int id ? ReferenceData.FindUser(id) : null;
        return new WorkOrderResponse(
            w.Id, w.Number, w.Title, w.Description, w.Status, w.EquipmentId,
            w.CreatedByUserId, w.AssigneeUserId,
            assignee is null ? null : new UserShortResponse(assignee.Id, assignee.FullName),
            w.CreatedAt, w.UpdatedAt);
    }
}

// ФИО исполнителя для списка — собирается из справочника пользователей, в заявке хранится только id (3НФ).
public record UserShortResponse(int Id, string FullName);
