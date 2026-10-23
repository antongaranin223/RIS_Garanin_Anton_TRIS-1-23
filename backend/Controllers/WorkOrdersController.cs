using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

// Главный ресурс курса: заявки на ремонт /api/work-orders (docs/api-contract.md).
// Контроллер тонкий: принял HTTP, вызвал сервис, вернул код.
// В ЛР7 — только тестовый GET списка. POST, проверка полей и PATCH — в следующих работах.
[ApiController]
[Route("api/work-orders")]
public class WorkOrdersController : ControllerBase
{
    private readonly IWorkOrderService _service;

    // Сервис приходит через DI (регистрация в Program.cs), а не создаётся через new.
    public WorkOrdersController(IWorkOrderService service)
    {
        _service = service;
    }

    // GET /api/work-orders → 200 и массив; пустой список — 200 и [], не 404.
    [HttpGet]
    public ActionResult<IReadOnlyList<WorkOrder>> GetAll() => Ok(_service.GetAll());
}
