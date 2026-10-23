using Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Сервисы (DI-контейнер) ---

builder.Services.AddControllers();

// Описание API для Swagger: /openapi/v1.json
builder.Services.AddOpenApi();

// DI: контроллер получает IWorkOrderService через конструктор и не создаёт реализацию через new.
// Singleton — один экземпляр на всё приложение, поэтому все запросы видят один список в памяти.
// В ЛР10 здесь подставим реализацию с PostgreSQL, контроллер не изменится.
builder.Services.AddSingleton<IWorkOrderService, InMemoryWorkOrderService>();

var app = builder.Build();

// --- Конвейер обработки запросов ---

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
