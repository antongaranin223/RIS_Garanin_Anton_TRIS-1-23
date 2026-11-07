using Backend.Data;
using Backend.Infrastructure;
using Backend.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;

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

// Описание API для Swagger: /openapi/v1.json
builder.Services.AddOpenApi();

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
