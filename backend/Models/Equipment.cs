namespace Backend.Models;

// Единица оборудования — справочник (docs/data-model.md, §2.2).
public class Equipment
{
    public int Id { get; init; }

    public string InventoryNumber { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    // FK на участок; таблица участков появится вместе с PostgreSQL (ЛР10).
    public int AreaId { get; init; }
}
