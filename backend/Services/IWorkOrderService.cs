using Backend.Models;

namespace Backend.Services;

// Правила работы с заявками на ремонт. Контроллер знает только этот интерфейс.
// В ЛР7 — только чтение списка; создание (POST) с проверкой полей появится в ЛР8.
public interface IWorkOrderService
{
    IReadOnlyList<WorkOrder> GetAll();
}
