namespace Backend.Models;

// Участок цеха — справочник для оборудования (docs/data-model.md, §2.3; таблица areas из ЛР9).
public class Area
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}
