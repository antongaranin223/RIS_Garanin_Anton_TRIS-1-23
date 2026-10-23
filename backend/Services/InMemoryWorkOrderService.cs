using Backend.Models;

namespace Backend.Services;

// Заготовка: заявки хранятся в памяти процесса.
// После Ctrl+C и нового dotnet run список снова пустой. Хранение в PostgreSQL — ЛР10.
public class InMemoryWorkOrderService : IWorkOrderService
{
    private readonly List<WorkOrder> _workOrders = new();

    // Новые заявки — первыми, как в списке клиента.
    public IReadOnlyList<WorkOrder> GetAll() =>
        _workOrders.OrderByDescending(w => w.Id).ToList();
}
