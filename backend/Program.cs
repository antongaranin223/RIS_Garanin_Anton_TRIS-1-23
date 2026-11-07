using System.Text;
using Backend.Auth;
using Backend.Data;
using Backend.Infrastructure;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// --- Сервисы (DI-контейнер) ---

builder.Services
    .AddControllers(options =>
    {
        // Ключи ошибок 400 — как имена полей в JSON (title, equipmentId…), а не как свойства C# (Title).
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Ответ 400 при плохом теле: ValidationProblemDetails с русским заголовком и понятными сообщениями.
        options.InvalidModelStateResponseFactory = ValidationResponses.InvalidModelState;
    });

// Все ошибки — одним видом JSON: ProblemDetails.
builder.Services.AddProblemDetails();

// Описание API для Swagger: /openapi/v1.json.
// Схема Bearer — кнопка Authorize в Swagger UI: вставить токен из POST /api/auth/login.
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Токен из POST /api/auth/login (только сам токен, без слова Bearer)",
    };
    document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] }];
    return Task.CompletedTask;
}));

// PostgreSQL через EF Core. Строка подключения — ключ ConnectionStrings:Default.
// Настоящее значение с паролем лежит вне репозитория: dotnet user-secrets (Development)
// или переменная окружения ConnectionStrings__Default. В appsettings.json — только пустой ключ.
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Не задана строка подключения ConnectionStrings:Default. " +
        "Выполните в папке backend: dotnet user-secrets set \"ConnectionStrings:Default\" \"Host=localhost;Port=5432;Database=toir_workshop;Username=postgres;Password=…\"");
}

// DbContext — Scoped: один на HTTP-запрос, после ответа закрывается и возвращает соединение в пул.
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// DI: контроллер получает IWorkOrderService через конструктор и не знает, где лежат данные.
// В ЛР8 был InMemoryWorkOrderService (Singleton со списком в памяти), теперь — EF Core и PostgreSQL.
// Scoped, потому что сервис использует DbContext; контроллер не изменился.
builder.Services.AddScoped<IWorkOrderService, EfWorkOrderService>();

// --- Вход и права (ЛР11): учебный JWT в заголовке Authorization: Bearer <токен> ---
// Выбран JWT, а не cookie: так обещано в контракте ЛР2, и токен одинаково удобно слать из Vue и Swagger.
// Ключ подписи — секрет, лежит вне Git: dotnet user-secrets set "Jwt:Key" "<32+ случайных символа>".
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
{
    throw new InvalidOperationException(
        "Не задан ключ подписи JWT (Jwt:Key, не короче 32 символов). " +
        "Выполните в папке backend: dotnet user-secrets set \"Jwt:Key\" \"<длинная случайная строка>\"");
}
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Утверждения из токена — под теми же короткими именами (sub, name, role), без переименования в URI.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,          // истёкший токен → 401
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidateIssuerSigningKey = true,  // подделанный или испорченный токен → 401
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = AuthClaims.Name,
            RoleClaimType = AuthClaims.Role,  // по нему работает [Authorize(Roles = …)]
        };
        options.Events = new JwtBearerEvents
        {
            // 401: личность не установлена (нет токена, испорчен, истёк). Тело — ProblemDetails, как у других ошибок.
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await WriteProblem(context.Response, StatusCodes.Status401Unauthorized,
                    context.AuthenticateFailure is null
                        ? "Требуется вход: передайте токен в заголовке Authorization: Bearer …"
                        : "Токен недействителен или истёк, войдите снова");
            },
            // 403: личность известна, но роли действие не разрешено ([Authorize(Roles = …)]).
            OnForbidden = context => WriteProblem(context.Response, StatusCodes.Status403Forbidden,
                "Нет прав на это действие для вашей роли"),
        };
    });
builder.Services.AddAuthorization();

// Хэши паролей — проверенной библиотекой ASP.NET Identity (PBKDF2 с солью), а не самописным кодом.
builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddSingleton<JwtTokenService>();

// CORS: клиент Vue (http://localhost:5173) — другой origin, чем API (:5000). Разрешаем только его адрес,
// не «*». Cookie не используем, поэтому AllowCredentials не нужен: токен идёт в заголовке Authorization.
const string ClientCors = "VueClient";
var clientOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(ClientCors, policy => policy
    .WithOrigins(clientOrigins)
    .WithHeaders("Authorization", "Content-Type")
    .WithMethods("GET", "POST", "PATCH")));

var app = builder.Build();

// --- Конвейер обработки запросов ---

// Страховка для любых неожиданных исключений вне контроллера заявок:
// одна строка в логе, клиенту — безопасный 500 ProblemDetails без stack trace (даже в Development).
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<IExceptionHandlerPathFeature>();
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("UnhandledException");
    logger.LogError("Request failed; path={Path}; error={Error}",
        feature?.Path, feature?.Error.GetType().Name);

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "Не удалось обработать запрос",
    }, options: null, contentType: "application/problem+json");
}));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Swagger UI: http://localhost:5000/swagger
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ТОиР-Цех API v1");
        options.DocumentTitle = "ТОиР-Цех API — Swagger";
    });
}

// HTTPS-редирект не включаем: проверка курса идёт по http://localhost:5000 (схема ЛР2).

app.UseCors(ClientCors);

// Сначала «кто ты» (разобрать и проверить JWT), потом «можно ли тебе» ([Authorize]).
app.UseAuthentication();
app.UseAuthorization();

// Главный ресурс — контроллеры (Controllers/WorkOrdersController.cs).
app.MapControllers();

// Служебные адреса — коротко через Minimal API.

// Только «процесс жив и отвечает». Не принимает полей заявки и не проверяет создание заявок.
app.MapGet("/health", () => Results.Text("ok"))
    .WithTags("Служебные")
    .WithSummary("Живость процесса (не проверяет создание заявок)");

// Сведения о приложении: имя, среда, версия.
app.MapGet("/info", (IConfiguration config, IWebHostEnvironment env) => Results.Ok(new
{
    name = config["App:Name"] ?? env.ApplicationName,
    environment = env.EnvironmentName,
    version = config["App:Version"] ?? "0.0.0",
    lab = config["App:Lab"],
}))
    .WithTags("Служебные")
    .WithSummary("Имя приложения, среда и версия");

app.Run();

// Ответ 401/403 в формате ProblemDetails (application/problem+json).
static Task WriteProblem(HttpResponse response, int status, string title)
{
    response.StatusCode = status;
    return response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title },
        options: null, contentType: "application/problem+json");
}
