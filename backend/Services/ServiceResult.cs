namespace Backend.Services;

// Ожидаемые исходы операции сервиса. Это не аварии: контроллер превращает их в 400 / 404 / 409.
public enum ServiceError
{
    None,
    Validation, // поле не прошло проверку → 400
    NotFound,   // нет записи с таким id → 404
    Conflict,   // действие противоречит состоянию процесса → 409
}

public record ServiceResult<T>(T? Value, ServiceError Error, string? Message, string? Field)
{
    public bool IsSuccess => Error == ServiceError.None;

    public static ServiceResult<T> Ok(T value) => new(value, ServiceError.None, null, null);

    public static ServiceResult<T> Invalid(string field, string message) =>
        new(default, ServiceError.Validation, message, field);

    public static ServiceResult<T> NotFound(string message) =>
        new(default, ServiceError.NotFound, message, null);

    public static ServiceResult<T> Conflict(string message) =>
        new(default, ServiceError.Conflict, message, null);
}
