namespace Backend.Models;

// Пользователь (docs/data-model.md, §2.4). Пароля пока нет — вход появится в ЛР11.
public class AppUser
{
    public int Id { get; init; }

    public string Login { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    // Один из кодов UserRoles.
    public string Role { get; init; } = string.Empty;
}

// Коды ролей (docs/lab-01-requirements.md, «Роли»).
public static class UserRoles
{
    public const string Foreman = "Foreman";     // мастер участка
    public const string Mechanic = "Mechanic";   // механик цеха
    public const string Fitter = "Fitter";       // слесарь-ремонтник
    public const string ShopChief = "ShopChief"; // начальник цеха
}
