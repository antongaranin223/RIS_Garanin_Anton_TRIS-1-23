using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

// Тело PATCH /api/work-orders/{id}/assignee — только исполнитель, статус не меняется.
public class AssignWorkOrderRequest
{
    [Required(ErrorMessage = "Укажите исполнителя")]
    [Range(1, int.MaxValue, ErrorMessage = "Номер исполнителя — целое число больше 0")]
    public int? AssigneeUserId { get; set; }
}
