using Backend.Dtos;
using Backend.Infrastructure;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

// Главный ресурс: заявки на ремонт /api/work-orders (docs/api-contract.md).
// Контроллер тонкий: принял HTTP, вызвал сервис, превратил результат в код ответа.
// Правила процесса (кто и когда может сменить статус) — в сервисе.
// DELETE нет: отмена заявки — статус Cancelled.
[ApiController]
[Route("api/work-orders")]
[Produces("application/json")]
public class WorkOrdersController : ControllerBase
{
    // Текущий пользователь до появления входа (ЛР11): заголовок X-User-Id.
    private const string UserHeader = "X-User-Id";
    // Учебный сбой для проверки лога: заголовок X-Simulate-Failure: 1, работает только в Development.
    private const string FailureHeader = "X-Simulate-Failure";

    private readonly IWorkOrderService _service;
    private readonly ILogger<WorkOrdersController> _logger;
    private readonly IWebHostEnvironment _env;

    // Сервис и логгер приходят через DI, а не создаются через new.
    public WorkOrdersController(IWorkOrderService service, ILogger<WorkOrdersController> logger, IWebHostEnvironment env)
    {
        _service = service;
        _logger = logger;
        _env = env;
    }

    // GET /api/work-orders → 200 и массив; пустой список — 200 и [], не 404.
    [HttpGet]
    [ProducesResponseType<IEnumerable<WorkOrderResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<WorkOrderResponse>> GetAll() =>
        Ok(_service.GetAll().Select(WorkOrderResponse.From));

    // GET /api/work-orders/{id} → 200 или 404.
    [HttpGet("{id:int}")]
    [ProducesResponseType<WorkOrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<WorkOrderResponse> GetById(int id) => ToResponse(_service.GetById(id));

    // POST /api/work-orders → 201 + Location; 400 при плохих полях.
    [HttpPost]
    [ProducesResponseType<WorkOrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public ActionResult<WorkOrderResponse> Create(
        CreateWorkOrderRequest request,
        [FromHeader(Name = UserHeader)] int? userId,
        [FromHeader(Name = FailureHeader)] string? simulateFailure)
    {
        if (userId is null)
        {
            return MissingUserHeader();
        }

        try
        {
            var result = _service.Create(request, userId.Value, _env.IsDevelopment() && simulateFailure == "1");
            if (!result.IsSuccess)
            {
                return ToResponse(result);
            }

            var created = WorkOrderResponse.From(result.Value!);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            // Неожиданный сбой: разработчику — одна строка в логе (операция, ключ, тип ошибки),
            // без тела запроса целиком. Клиенту — безопасный ProblemDetails без stack trace и путей.
            _logger.LogError("CreateWorkOrder failed; equipmentId={EquipmentId}; error={Error}",
                request.EquipmentId, ex.GetType().Name);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Не удалось обработать запрос",
                detail: "Произошла внутренняя ошибка сервера. Попробуйте позже.");
        }
    }

    // PATCH /api/work-orders/{id}/assignee — только назначение исполнителя; статус не меняется.
    [HttpPatch("{id:int}/assignee")]
    [ProducesResponseType<WorkOrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<WorkOrderResponse> Assign(int id, AssignWorkOrderRequest request) =>
        ToResponse(_service.Assign(id, request.AssigneeUserId!.Value));

    // PATCH /api/work-orders/{id}/status — только смена статуса; исполнитель не меняется.
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<WorkOrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<WorkOrderResponse> ChangeStatus(
        int id,
        ChangeWorkOrderStatusRequest request,
        [FromHeader(Name = UserHeader)] int? userId)
    {
        if (userId is null)
        {
            return MissingUserHeader();
        }

        return ToResponse(_service.ChangeStatus(id, request.Status!, userId.Value));
    }

    // Ожидаемые исходы сервиса → коды HTTP. В лог не пишем как ошибку: это не авария.
    private ActionResult<WorkOrderResponse> ToResponse(ServiceResult<Backend.Models.WorkOrder> result)
    {
        switch (result.Error)
        {
            case ServiceError.None:
                return Ok(WorkOrderResponse.From(result.Value!));

            case ServiceError.Validation:
                ModelState.AddModelError(result.Field!, result.Message!);
                return ValidationProblem(title: ValidationResponses.Title, modelStateDictionary: ModelState);

            case ServiceError.NotFound:
                return Problem(statusCode: StatusCodes.Status404NotFound, title: result.Message);

            default:
                _logger.LogInformation("WorkOrder action rejected (409): {Reason}", result.Message);
                return Problem(statusCode: StatusCodes.Status409Conflict, title: result.Message);
        }
    }

    private ActionResult MissingUserHeader()
    {
        ModelState.AddModelError(UserHeader, "Укажите заголовок X-User-Id — кто выполняет действие (до появления входа в ЛР11)");
        return ValidationProblem(title: ValidationResponses.Title, modelStateDictionary: ModelState);
    }
}
