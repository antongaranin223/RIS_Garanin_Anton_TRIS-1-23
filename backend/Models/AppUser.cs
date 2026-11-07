namespace Backend.Models;

// Пользователь (docs/data-model.md, §2.4; таблица users).
public class AppUser
{
    public int Id { get; init; }

    public string Login { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    // Один из кодов UserRoles.
    public string Role { get; init; } = string.Empty;

    // Только хеш пароля (НФТ-5). Вход появится в ЛР11, до этого столбец пустой (NULL).
    public string? PasswordHash { get; init; }
}

// Коды ролей (docs/lab-01-requirements.md, «Роли»).
public static class UserRoles
{
    public const string Foreman = "Foreman";     // мастер участка
    public const string Mechanic = "Mechanic";   // механик цеха
    public const string Fitter = "Fitter";       // слесарь-ремонтник
    public const string ShopChief = "ShopChief"; // начальник цеха

    public static readonly string[] All = [Foreman, Mechanic, Fitter, ShopChief];
}
