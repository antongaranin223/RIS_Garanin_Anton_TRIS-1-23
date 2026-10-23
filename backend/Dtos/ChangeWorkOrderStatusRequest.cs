using System.ComponentModel.DataAnnotations;
using Backend.Models;

namespace Backend.Dtos;

// Тело PATCH /api/work-orders/{id}/status — только новый статус, исполнитель не меняется.
public class ChangeWorkOrderStatusRequest
{
    [Required(ErrorMessage = "Укажите статус")]
    [AllowedValues(
        WorkOrderStatuses.New, WorkOrderStatuses.InProgress,
        WorkOrderStatuses.Closed, WorkOrderStatuses.Cancelled,
        ErrorMessage = "Допустимые значения: New, InProgress, Closed, Cancelled")]
    public string? Status { get; set; }
}
