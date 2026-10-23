using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

// Тело POST /api/work-orders. Только поля, которые заполняет человек.
// id, number, status, автора и исполнителя клиент НЕ присылает — их ставит сервер.
// Лишние поля в JSON (например "status": "Closed") просто игнорируются.
public class CreateWorkOrderRequest : IValidatableObject
{
    [Required(ErrorMessage = "Заголовок обязателен")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Номер оборудования обязателен")]
    [Range(1, int.MaxValue, ErrorMessage = "Номер оборудования — целое число больше 0")]
    public int? EquipmentId { get; set; }

    [Required(ErrorMessage = "Описание обязательно")]
    public string? Description { get; set; }

    // Ключи ошибок — имена полей JSON (title, description), как в контракте ЛР2.
    // Длины считаем после Trim: строка из пробелов не проходит (правила ФТ-3, как на клиенте).
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Title is not null)
        {
            var length = Title.Trim().Length;
            if (length < 5 || length > 80)
            {
                yield return new ValidationResult(
                    "Заголовок должен содержать от 5 до 80 символов", ["title"]);
            }
        }

        if (Description is not null)
        {
            var length = Description.Trim().Length;
            if (length < 10 || length > 500)
            {
                yield return new ValidationResult(
                    "Описание должно содержать от 10 до 500 символов", ["description"]);
            }
        }
    }
}
