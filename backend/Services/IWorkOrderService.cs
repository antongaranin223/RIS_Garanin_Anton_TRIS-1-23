using Backend.Dtos;
using Backend.Models;

namespace Backend.Services;

// Правила работы с заявками на ремонт. Контроллер знает только этот интерфейс.
public interface IWorkOrderService
{
    IReadOnlyList<WorkOrder> GetAll();

    ServiceResult<WorkOrder> GetById(int id);

    // currentUserId — кто выполняет действие: id из проверенного JWT (утверждение sub), не из тела и не из адреса.
    // simulateFailure — учебный сбой (только Development).
    ServiceResult<WorkOrder> Create(CreateWorkOrderRequest request, int currentUserId, bool simulateFailure = false);

    ServiceResult<WorkOrder> Assign(int id, int assigneeUserId, int currentUserId);

    ServiceResult<WorkOrder> ChangeStatus(int id, string newStatus, int currentUserId);
}
