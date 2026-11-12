namespace Backend.Dtos;

// Единица оборудования для выпадающего списка формы (GET /api/equipment).
public record EquipmentResponse(int Id, string InventoryNumber, string Name, int AreaId, string AreaName);

// Пользователь для списка «Назначить» (GET /api/users?role=Fitter). Логина и хэша пароля здесь нет.
public record UserListItemResponse(int Id, string FullName, string Role);
