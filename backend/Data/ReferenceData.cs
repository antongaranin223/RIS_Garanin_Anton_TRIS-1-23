using Backend.Models;

namespace Backend.Data;

// Учебные справочники в памяти: оборудование и пользователи.
// Нужны, чтобы сервер проверял equipmentId и assigneeUserId (ФТ-3, ФТ-4).
// В ЛР10 эти данные переедут в таблицы PostgreSQL как начальные данные (seed).
public static class ReferenceData
{
    public static readonly IReadOnlyList<Equipment> Equipment = new List<Equipment>
    {
        new() { Id = 1, InventoryNumber = "ИНВ-04501", Name = "Токарный станок 16К20", AreaId = 1 },
        new() { Id = 2, InventoryNumber = "ИНВ-04502", Name = "Кран-балка 5 т, пролёт 1", AreaId = 3 },
        new() { Id = 3, InventoryNumber = "ИНВ-04512", Name = "Токарный станок с ЧПУ 16А20Ф3", AreaId = 1 },
        new() { Id = 4, InventoryNumber = "ИНВ-04513", Name = "Токарный станок с ЧПУ 16А20Ф3", AreaId = 1 },
        new() { Id = 5, InventoryNumber = "ИНВ-04520", Name = "Фрезерный станок 6Р82", AreaId = 2 },
        new() { Id = 6, InventoryNumber = "ИНВ-04521", Name = "Вертикально-фрезерный станок с ЧПУ", AreaId = 2 },
        new() { Id = 7, InventoryNumber = "ИНВ-04530", Name = "Гидравлический пресс П6324", AreaId = 3 },
        new() { Id = 8, InventoryNumber = "ИНВ-04531", Name = "Кривошипный пресс КД2128", AreaId = 3 },
        new() { Id = 9, InventoryNumber = "ИНВ-04540", Name = "Кран-балка 3,2 т, пролёт 2", AreaId = 3 },
        new() { Id = 10, InventoryNumber = "ИНВ-04550", Name = "Сверлильный станок 2Н135", AreaId = 2 },
        new() { Id = 11, InventoryNumber = "ИНВ-04560", Name = "Шлифовальный станок 3Е711", AreaId = 2 },
        new() { Id = 12, InventoryNumber = "ИНВ-04570", Name = "Компрессор винтовой ВК-20", AreaId = 3 },
    };

    public static readonly IReadOnlyList<AppUser> Users = new List<AppUser>
    {
        new() { Id = 1, Login = "ivanov", FullName = "Иванов А. П.", Role = UserRoles.Foreman },
        new() { Id = 2, Login = "kuznetsov", FullName = "Кузнецов Д. В.", Role = UserRoles.Mechanic },
        new() { Id = 3, Login = "orlov", FullName = "Орлов Е. Н.", Role = UserRoles.ShopChief },
        new() { Id = 4, Login = "smirnov", FullName = "Смирнов О. Л.", Role = UserRoles.Foreman },
        new() { Id = 5, Login = "petrov", FullName = "Петров С. И.", Role = UserRoles.Fitter },
        new() { Id = 6, Login = "sidorov", FullName = "Сидоров В. А.", Role = UserRoles.Fitter },
    };

    public static bool EquipmentExists(int id) => Equipment.Any(e => e.Id == id);

    public static AppUser? FindUser(int id) => Users.FirstOrDefault(u => u.Id == id);
}
