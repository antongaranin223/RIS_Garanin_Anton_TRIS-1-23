namespace Backend.Auth;

// Настройки учебного JWT (секция "Jwt" конфигурации).
// Issuer, Audience и срок жизни — в appsettings.json. Ключ подписи — секрет:
// dotnet user-secrets set "Jwt:Key" "…" (или переменная окружения Jwt__Key), в Git его нет.
public class JwtOptions
{
    public const string SectionName = "Jwt";

    // Кто выпустил токен (наш API).
    public string Issuer { get; set; } = string.Empty;

    // Для кого токен (клиент ТОиР-Цех).
    public string Audience { get; set; } = string.Empty;

    // Секрет подписи HMAC-SHA256: не короче 32 байт (256 бит).
    public string Key { get; set; } = string.Empty;

    // Срок жизни токена в минутах; по умолчанию — смена 8 часов. Истёк → 401, нужно войти снова.
    public int LifetimeMinutes { get; set; } = 480;
}
