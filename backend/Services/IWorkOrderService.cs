using Backend.Dtos;
using Backend.Models;

namespace Backend.Services;

// Правила работы с заявками на ремонт. Контроллер знает только этот интерфейс.
public interface IWorkOrderService
{
    IReadOnlyList<WorkOrder> GetAll();

    ServiceResult<WorkOrder> GetById(int id);

    // currentUserId — кто создаёт (заголовок X-User-Id). simulateFailure — учебный сбой (только Development).
    ServiceResult<WorkOrder> Create(CreateWorkOrderRequest request, int currentUserId, bool simulateFailure = false);

    ServiceResult<WorkOrder> Assign(int id, int assigneeUserId);

    ServiceResult<WorkOrder> ChangeStatus(int id, string newStatus, int currentUserId);
}
