using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

// Тело POST /api/auth/login (docs/api-contract.md, §3.6).
public class LoginRequest
{
    [Required(ErrorMessage = "Укажите логин")]
    [StringLength(40, ErrorMessage = "Логин — не длиннее 40 символов")]
    public string? Login { get; set; }

    [Required(ErrorMessage = "Укажите пароль")]
    [StringLength(100, ErrorMessage = "Пароль — не длиннее 100 символов")]
    public string? Password { get; set; }
}

// Ответ на успешный вход: токен для заголовка Authorization и пользователь для шапки.
public record LoginResponse(string Token, DateTimeOffset ExpiresAt, CurrentUserResponse User);

// Текущий пользователь (вход и GET /api/auth/me). Хэш пароля сюда не попадает.
public record CurrentUserResponse(int Id, string Login, string FullName, string Role);
