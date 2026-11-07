using Backend.Auth;
using Backend.Dtos;
using Backend.Infrastructure;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

// Главный ресурс: заявки на ремонт /api/work-orders (docs/api-contract.md).
// Контроллер тонкий: принял HTTP, вызвал сервис, превратил результат в код ответа.
// Правила процесса (кто и когда может сменить статус) — в сервисе.
// DELETE нет: отмена заявки — статус Cancelled.
//
// Права (ЛР11, docs/role-matrix.md):
//   [Authorize] на классе — без действующего JWT любой метод отвечает 401;
//   [Authorize(Roles = …)] на методе — вошедшему с другой ролью 403;
//   «своя / чужая заявка» проверяется в сервисе по id из токена — тоже 403.
[ApiController]
[Authorize]
[Route("api/work-orders")]
[Produces("application/json")]
public class WorkOrdersController : ControllerBase
{
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
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<IEnumerable<WorkOrderResponse>> GetAll() =>
        Ok(_service.GetAll().Select(WorkOrderResponse.From));

    // GET /api/work-orders/{id} → 200 или 404.
    [HttpGet("{id:int}")]
    [ProducesResponseType<WorkOrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<WorkOrderResponse> GetById(int id) => ToResponse(_service.GetById(id));

    // POST /api/work-orders → 201 + Location; 400 при плохих полях; 401 без входа; 403 слесарю и начальнику цеха.
    [HttpPost]
    [Authorize(Roles = $"{Backend.Models.UserRoles.Foreman},{Backend.Models.UserRoles.Mechanic}")]
    [ProducesResponseType<WorkOrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public ActionResult<WorkOrderResponse> Create(
        CreateWorkOrderRequest request,
        [FromHeader(Name = FailureHeader)] string? simulateFailure)
    {
        try
        {
            // Автор — из токена (User.GetUserId()), клиент не может подставить чужой id.
            var result = _service.Create(request, User.GetUserId(), _env.IsDevelopment() && simulateFailure == "1");
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
    // Назначает только механик цеха (ФТ-9): мастеру, слесарю и начальнику цеха — 403.
    [HttpPatch("{id:int}/assignee")]
    [Authorize(Roles = Backend.Models.UserRoles.Mechanic)]
    [ProducesResponseType<WorkOrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<WorkOrderResponse> Assign(int id, AssignWorkOrderRequest request) =>
        ToResponse(_service.Assign(id, request.AssigneeUserId!.Value, User.GetUserId()));

    // PATCH /api/work-orders/{id}/status — только смена статуса; исполнитель не меняется.
    // Роль здесь атрибутом не ограничить: кто может, зависит от перехода и от того, чья это заявка, —
    // это решает сервис (слесарь — только свою, ФТ-8; отмена — механик или автор-мастер, ФТ-7).
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<WorkOrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<WorkOrderResponse> ChangeStatus(int id, ChangeWorkOrderStatusRequest request) =>
        ToResponse(_service.ChangeStatus(id, request.Status!, User.GetUserId()));

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

            case ServiceError.Forbidden:
                _logger.LogInformation("WorkOrder action forbidden (403): userId={UserId}; {Reason}",
                    User.GetUserId(), result.Message);
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: result.Message);

            case ServiceError.NotFound:
                return Problem(statusCode: StatusCodes.Status404NotFound, title: result.Message);

            default:
                _logger.LogInformation("WorkOrder action rejected (409): {Reason}", result.Message);
                return Problem(statusCode: StatusCodes.Status409Conflict, title: result.Message);
        }
    }
}
