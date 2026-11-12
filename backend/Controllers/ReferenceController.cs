using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

// Справочники для экрана (docs/api-contract.md, §2.2). Только чтение: методов изменения справочников в API нет.
[ApiController]
[Authorize]
[Route("api")]
[Produces("application/json")]
public class ReferenceController(AppDbContext db) : ControllerBase
{
    // GET /api/equipment → оборудование с названием участка, для выбора в форме заявки. Все роли.
    // SELECT e.…, a.name FROM equipment AS e JOIN areas AS a ON a.id = e.area_id ORDER BY e.inventory_number
    [HttpGet("equipment")]
    [ProducesResponseType<IEnumerable<EquipmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public IEnumerable<EquipmentResponse> GetEquipment() =>
        db.Equipment
            .AsNoTracking()
            .OrderBy(e => e.InventoryNumber)
            .Select(e => new EquipmentResponse(e.Id, e.InventoryNumber, e.Name, e.AreaId, e.Area!.Name))
            .ToList();

    // GET /api/users?role=Fitter → слесари для «Назначить». Нужен только механику цеха (ФТ-4), остальным — 403.
    [HttpGet("users")]
    [Authorize(Roles = UserRoles.Mechanic)]
    [ProducesResponseType<IEnumerable<UserListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public ActionResult<IEnumerable<UserListItemResponse>> GetUsers([FromQuery] string? role)
    {
        if (role is not null && !UserRoles.All.Contains(role))
        {
            ModelState.AddModelError("role", $"Неизвестная роль. Допустимо: {string.Join(", ", UserRoles.All)}");
            return ValidationProblem(title: Backend.Infrastructure.ValidationResponses.Title, modelStateDictionary: ModelState);
        }

        return Ok(db.Users
            .AsNoTracking()
            .Where(u => role == null || u.Role == role)
            .OrderBy(u => u.FullName)
            .Select(u => new UserListItemResponse(u.Id, u.FullName, u.Role))
            .ToList());
    }
}
