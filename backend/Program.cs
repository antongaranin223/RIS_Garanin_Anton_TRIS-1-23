using Backend.Infrastructure;
using Backend.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

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

// DI: контроллер получает IWorkOrderService через конструктор и не создаёт реализацию через new.
// Singleton — один экземпляр на всё приложение: все запросы видят один список в памяти.
// В ЛР10 здесь подставим реализацию с PostgreSQL, контроллер не изменится.
builder.Services.AddSingleton<IWorkOrderService, InMemoryWorkOrderService>();

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
