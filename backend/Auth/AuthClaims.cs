using System.Security.Claims;

namespace Backend.Auth;

// Имена утверждений (claims) в токене. Короткие имена из стандарта JWT, без длинных URI Microsoft:
// в Program.cs отключено переименование (MapInboundClaims = false), поэтому в коде и в токене одни и те же имена.
public static class AuthClaims
{
    public const string UserId = "sub";   // id пользователя из таблицы users
    public const string Name = "name";    // ФИО для шапки
    public const string Role = "role";    // код роли: Foreman, Mechanic, Fitter, ShopChief

    // id текущего пользователя. [Authorize] на контроллере гарантирует, что токен проверен и sub в нём есть.
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(UserId)
                  ?? throw new InvalidOperationException("В токене нет утверждения sub"));
}
