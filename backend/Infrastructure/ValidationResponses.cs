using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Backend.Infrastructure;

// Единый вид ответа 400 (ValidationProblemDetails):
// title «Ошибка проверки полей», errors — по именам полей JSON (title, equipmentId, description).
public static class ValidationResponses
{
    public const string Title = "Ошибка проверки полей";

    // Фабрика ответа 400 для [ApiController], когда тело не прошло проверку DTO или не разобралось как JSON.
    public static IActionResult InvalidModelState(ActionContext context)
    {
        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(
            context.HttpContext, Clean(context.ModelState), StatusCodes.Status400BadRequest, Title);
        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    }

    // Сообщения System.Text.Json («The JSON value could not be converted to System.Nullable`1…»)
    // технические и раскрывают внутренние типы. Заменяем их понятным текстом и не отдаём клиенту.
    private static ModelStateDictionary Clean(ModelStateDictionary source)
    {
        var result = new ModelStateDictionary();
        // Ошибки разбора тела: ключи "$…" (JSON) и "" (тело пустое).
        var hasBodyErrors = source.Any(e => e.Value is { Errors.Count: > 0 } && (e.Key.Length == 0 || e.Key.StartsWith('$')));

        foreach (var (key, entry) in source)
        {
            if (entry.Errors.Count == 0)
            {
                continue;
            }

            if (key.Length == 0)
            {
                result.AddModelError("body", "Тело запроса пустое — пришлите JSON");
                continue;
            }

            if (key.StartsWith('$'))
            {
                var isTypeMismatch = entry.Errors.Any(e => e.ErrorMessage.StartsWith("The JSON value could not be converted"));
                if (isTypeMismatch && key.Length > 2)
                {
                    result.AddModelError(key[2..], "Неверный формат значения");
                }
                else
                {
                    result.AddModelError("body", "Тело запроса — некорректный JSON");
                }
                continue;
            }

            // «The request field is required» появляется вместе с ошибкой разбора тела — она уже описана выше.
            if (hasBodyErrors && key == "request")
            {
                continue;
            }

            foreach (var error in entry.Errors)
            {
                result.AddModelError(key, error.ErrorMessage);
            }
        }

        return result;
    }
}
