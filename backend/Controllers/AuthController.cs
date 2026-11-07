using Backend.Auth;
using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

// Вход по логину и паролю (ФТ-1) и «кто я» для шапки клиента.
// Способ входа — учебный JWT в заголовке Authorization: Bearer <токен> (cookie не используем).
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(
    AppDbContext db,
    IPasswordHasher<AppUser> passwordHasher,
    JwtTokenService tokens,
    ILogger<AuthController> logger) : ControllerBase
{
    private const string WrongCredentials = "Неверный логин или пароль";

    // Хэш для сравнения, когда логина нет в базе: проверка пароля идёт всегда, и по времени ответа
    // нельзя понять, существует ли такой логин. Посчитан один раз при запуске, пароль случайный.
    private static readonly string DummyHash =
        new PasswordHasher<AppUser>().HashPassword(new AppUser(), Guid.NewGuid().ToString());

    // POST /api/auth/login → 200 + токен; 400 пустые поля; 401 неверный логин или пароль.
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        var login = request.Login!.Trim();
        var user = db.Users.FirstOrDefault(u => u.Login == login);

        // Пароль сравнивает библиотека: она берёт соль из хэша, считает PBKDF2 и сравнивает за постоянное время.
        var result = passwordHasher.VerifyHashedPassword(user ?? new AppUser(), user?.PasswordHash ?? DummyHash, request.Password!);
        if (user is null || result == PasswordVerificationResult.Failed)
        {
            // В лог — только логин, пароль никогда не пишем.
            logger.LogInformation("Login failed; login={Login}", login);
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: WrongCredentials);
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // Библиотека стала строже (больше итераций) — сохраняем новый хэш того же пароля.
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password!);
            db.SaveChanges();
        }

        var (token, expiresAt) = tokens.CreateToken(user);
        logger.LogInformation("Login ok; userId={UserId}; role={Role}", user.Id, user.Role);
        return Ok(new LoginResponse(token, expiresAt, ToResponse(user)));
    }

    // GET /api/auth/me → 200 текущий пользователь; 401 без токена или с испорченным/истёкшим токеном.
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me()
    {
        var user = db.Users.AsNoTracking().FirstOrDefault(u => u.Id == User.GetUserId());
        // Токен ещё действует, а пользователя уже нет в базе — личность не подтверждена.
        return user is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Пользователь не найден, войдите снова")
            : Ok(ToResponse(user));
    }

    private static CurrentUserResponse ToResponse(AppUser u) => new(u.Id, u.Login, u.FullName, u.Role);
}
