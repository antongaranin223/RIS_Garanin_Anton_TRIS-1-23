# Data — доступ к данным

- `AppDbContext.cs` — контекст EF Core: таблицы `areas`, `equipment`, `users`, `work_orders` (ЛР10).
  Имена таблиц, столбцов, индексов и ограничений заданы явно и совпадают с `db/schema.sql` (ЛР9).
- `ReferenceData.cs` — начальные данные справочников (участки, оборудование `equipmentId` 1–12, пользователи с ролями).
  Передаются в `HasData`, их вставляет миграция `InitialCreate`; хэши паролей пользователей — миграция `AddPasswordHashes` (ЛР11). Во время работы сервер читает справочники из БД.

Заявки хранятся в PostgreSQL: `Services/EfWorkOrderService.cs`. Схему создают миграции из `../Migrations`
(`dotnet ef database update`). Строка подключения — ключ `ConnectionStrings:Default` в dotnet user-secrets, не в Git.
