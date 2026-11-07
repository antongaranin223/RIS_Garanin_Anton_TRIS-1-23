namespace Backend.Models;

// Единица оборудования — справочник (docs/data-model.md, §2.2; таблица equipment).
public class Equipment
{
    public int Id { get; init; }

    public string InventoryNumber { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    // FK на участок (equipment.area_id → areas.id).
    public int AreaId { get; init; }

    // Навигация: EF подставляет участок, если запросить его через Include.
    public Area? Area { get; init; }
}
