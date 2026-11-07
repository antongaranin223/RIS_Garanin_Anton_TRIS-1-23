# Отчёт по лабораторной работе № 10

| Поле | Значение |
|------|----------|
| Дисциплина | Б1.В.ДЭ.01.02.02 · РИС и Web |
| Студент | Гаранин Антон |
| Группа | ТРИС-1-23 |
| Дата | 07.11.2026 |
| № ЛР | 10 |
| Тема работы | EF Core и PostgreSQL |
| Тема проекта | «ТОиР-Цех» — веб-платформа управления техническим обслуживанием оборудования |

---

## 1. Цель

Тот же REST заявок на ремонт, что в ЛР8, но источник данных — PostgreSQL через EF Core, а не список в памяти процесса. Проверка: создали заявку → остановили API → запустили снова → заявка на месте. Строка подключения с паролем в Git не попадает.

## 2. Что сделали

1. **Пакеты** (`backend/backend.csproj`):
   - `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 — провайдер PostgreSQL, вместе с ним приходят EF Core и драйвер Npgsql;
   - `Microsoft.EntityFrameworkCore.Design` 10.0.14 — нужен командам `dotnet ef`, в сборку приложения не попадает (`PrivateAssets=all`);
   - глобальный инструмент `dotnet-ef` 10.0.14.
2. **Сущности** (`backend/Models`): к `WorkOrder`, `Equipment`, `AppUser` добавлены навигации по FK; новая сущность `Area` (участок), у `AppUser` — `PasswordHash` (пока `NULL`, вход — ЛР11).
3. **`backend/Data/AppDbContext.cs`** — четыре `DbSet`: `Areas`, `Equipment`, `Users`, `WorkOrders`. Имена таблиц, столбцов, PK, FK, индексов и CHECK заданы явно и совпадают с `db/schema.sql` (таблица в §4).
4. **Начальные данные** — справочники из `Data/ReferenceData.cs` (3 участка, 12 единиц оборудования, 6 пользователей) переданы в `HasData` и вставляются миграцией. Заявки не сидируются: их создают через API.
5. **Миграция `InitialCreate`** — `backend/Migrations/`, добавлена в Git. Применена командой `dotnet ef database update`.
6. **`Services/EfWorkOrderService.cs`** вместо `InMemoryWorkOrderService` (класс со `List<WorkOrder>` удалён). Правила процесса и переходы статусов — те же, что в ЛР8. Справочники для проверки `equipmentId` и `assigneeUserId` теперь читаются из БД.
7. **`Program.cs`** — `AddDbContext<AppDbContext>(UseNpgsql(...))` по ключу `ConnectionStrings:Default`; сервис зарегистрирован как `Scoped` (один `DbContext` на запрос). Если строки подключения нет, API при старте пишет понятную ошибку с командой `user-secrets`.
8. **Контроллер не изменился.** JSON и коды ответов ЛР8 те же. `WorkOrderResponse` берёт ФИО исполнителя из навигации `Assignee` (JOIN с `users`), а не из статического списка.
9. **Человеческий номер** `ЗР-ГГГГ-NNNN` — следующий после максимального за год. Если два POST одновременно получили один номер, второй INSERT отклоняет UNIQUE-индекс `work_orders_number_key`; сервис ловит `23505` и отвечает **409**, а не 500.
10. **Логи SQL** — в `appsettings.Development.json` включён уровень `Information` для `Microsoft.EntityFrameworkCore.Database.Command`: в консоли `dotnet run` видно, какой SQL построил EF. Значения параметров не выводятся (EF скрывает их по умолчанию).
11. **Организация** в документах заменена на реальную: механосборочный цех № 2 машиностроительного завода «Уралмаш» (ПАО «Уралмашзавод», г. Екатеринбург). Сотрудники, оборудование и заявки в проекте условные.

## 3. Где лежит пароль

- В `appsettings.json` — только пустой ключ `"ConnectionStrings": { "Default": "" }`.
- Настоящая строка — в **dotnet user-secrets** (хранится в профиле пользователя, вне репозитория; в `backend.csproj` — только `UserSecretsId`, это не секрет):

```bash
cd backend
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=toir_workshop;Username=postgres;Password=ВАШ_ПАРОЛЬ"
```

- Альтернатива (сервер, CI) — переменная окружения `ConnectionStrings__Default`.

Проверка перед сдачей:

```text
$ git grep -i password
backend/Models/AppUser.cs:16:    public string? PasswordHash { get; init; }
backend/Program.cs:39:  … dotnet user-secrets set … Password=…   ← подсказка в сообщении об ошибке
db/schema.sql:51:    password_hash  varchar(200) NULL
docs/…, frontend/src/pages/LoginPage.vue …     ← описания полей и форма входа
```

Настоящего пароля в выводе нет — только имена полей и подсказки.

## 4. Миграция и соответствие имён

```text
$ dotnet ef migrations add InitialCreate
Build succeeded.
Done. To undo this action, use 'ef migrations remove'

$ dotnet ef database update
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20261107071750_InitialCreate'.
Done.

$ dotnet ef migrations has-pending-model-changes
No changes have been made to the model since the last migration.
```

Таблицы в PostgreSQL после `database update`:

```text
toir_workshop=# \dt
 public | __EFMigrationsHistory | table | postgres
 public | areas                 | table | postgres
 public | equipment             | table | postgres
 public | users                 | table | postgres
 public | work_orders           | table | postgres

 areas | equipment | users | work_orders
-------+-----------+-------+-------------
     3 |        12 |     6 |           0
```

`__EFMigrationsHistory` — служебная таблица EF: в ней записано, какие миграции уже применены (`20261107071750_InitialCreate`, `10.0.14`).

| C# (`WorkOrder`) | PostgreSQL (`work_orders`) | Как настроено |
|---|---|---|
| `Id` | `id` | PK `work_orders_pkey`, identity |
| `Number` | `number` varchar(16) | UNIQUE `work_orders_number_key` |
| `Title` / `Description` | `title` varchar(80) / `description` varchar(500) | `HasMaxLength` + CHECK длины после `btrim` |
| `Status` | `status` varchar(16), DEFAULT `'New'` | CHECK `work_orders_status_check` (4 кода) |
| `EquipmentId` | `equipment_id` | FK `work_orders_equipment_id_fkey`, `ON DELETE RESTRICT` |
| `CreatedByUserId` | `created_by_user_id` | FK → `users`, `RESTRICT` |
| `AssigneeUserId` (`int?`) | `assignee_user_id` NULL | FK → `users`, `RESTRICT`; CHECK `work_orders_work_needs_assignee_chk` |
| `CreatedAt` / `UpdatedAt` (`DateTimeOffset`) | `created_at` / `updated_at` `timestamptz`, DEFAULT `now()` | |
| `Equipment.InventoryNumber`, `AreaId` | `equipment.inventory_number`, `area_id` | UNIQUE, FK → `areas` |
| `AppUser.FullName`, `PasswordHash` | `users.full_name`, `password_hash` | CHECK `users_role_check` |

Сверка миграции с `db/schema.sql` (SQL миграции получен `dotnet ef migrations script`):

| Что | `schema.sql` (ЛР9) | Миграция EF | Почему |
|---|---|---|---|
| Таблицы, столбцы, типы, NULL | — | совпадают | имена заданы явно в `AppDbContext` |
| CHECK, FK, UNIQUE, имена ограничений | — | совпадают | `HasCheckConstraint`, `HasConstraintName`, `HasDatabaseName` |
| Индексы ЛР9 (статус, станок, исполнитель, `created_at DESC`, участок) | — | совпадают | `HasIndex(...)`, у даты `IsDescending()` |
| Identity | `GENERATED ALWAYS` | `GENERATED BY DEFAULT` | так Npgsql делает по умолчанию; нужно, чтобы `HasData` вставил справочники с заданными `id`. После вставки миграция сама сдвигает последовательности (`setval`), поэтому новые строки не столкнутся с seed |
| Индекс `work_orders_created_by_user_id_idx` | нет | есть | EF создаёт индекс на каждый FK; пригодится для «заявок автора» |

`schema.sql` и `seed.sql` оставлены как справка ЛР9. Учебная база для API создаётся только миграциями: если в `toir_workshop` уже лежат таблицы из `schema.sql`, базу нужно пересоздать (`dropdb` / `createdb`) и выполнить `dotnet ef database update`. Иначе миграция упадёт на `relation "areas" already exists`.

## 5. Маршруты работают как в ЛР8

Проверено `curl` на `http://localhost:5000` (пустая база после миграции):

| Запрос | Ожидание | Факт |
|---|---|---|
| `GET /api/work-orders` (база пустая) | 200, `[]` | 200 |
| `POST` мастер (`X-User-Id: 1`), станок 3 | 201, `ЗР-2026-0001`, `New` | 201 |
| `POST` механик (`X-User-Id: 2`), станок 7 | 201, `ЗР-2026-0002` | 201 |
| `GET /api/work-orders/1` | 200 | 200 |
| `GET /api/work-orders/999` | 404 | 404 `«Заявка 999 не найдена»` |
| `POST` с `equipmentId: 999` | 400 по полю `equipmentId` | 400 |
| `POST` от слесаря (`X-User-Id: 5`) | 409 | 409 |
| `PATCH /1/assignee` → слесарь 5 | 200, статус `New` | 200 |
| `PATCH /1/assignee` → механик 2 | 400 | 400 |
| `PATCH /999/assignee` | 404 | 404 |
| `PATCH /1/status` `InProgress` (слесарь 5) | 200 | 200 |
| `PATCH /2/status` `InProgress` без исполнителя | 409 | 409 |
| `PATCH /1/status` `Done` | 400 | 400 |
| `PATCH /999/status` | 404 | 404 |
| `PATCH /1/assignee` у заявки в `InProgress` | 409 | 409 |
| `POST` с `X-Simulate-Failure: 1` | 500 без stack trace | 500 |
| 8 одновременных `POST` | 201 или 409, без 500 | 5 × 201, 3 × 409 |

Последняя строка — проверка гонки номеров. В логе видно ожидаемый отказ, а не аварию:

```text
warn: Backend.Services.EfWorkOrderService[0]
      CreateWorkOrder number conflict; number=ЗР-2026-0004
info: Backend.Controllers.WorkOrdersController[0]
      WorkOrder action rejected (409): Номер ЗР-2026-0004 уже занят другой заявкой. Повторите запрос
```

## 6. Рестарт-тест

1. PostgreSQL запущен, база `toir_workshop` после `database update`.
2. API запущен (`dotnet run`, процесс от 07:18:23).
3. Создана заявка, затем назначен исполнитель и статус переведён в `InProgress`:
   `id = 1`, `number = ЗР-2026-0001`, `title = «Не включается шпиндель»`.
4. API остановлен штатно:

```text
info: Microsoft.Hosting.Lifetime[0]
      Application is shutting down...
$ curl http://localhost:5000/health
--- API не отвечает
```

5. `dotnet run` снова (новый процесс от 07:19:53):

```text
      Now listening on: http://localhost:5000
```

6. `GET /api/work-orders/1`:

```json
{"id":1,"number":"ЗР-2026-0001","title":"Не включается шпиндель",
 "description":"После смены инструмента шпиндель не стартует, на пульте ошибка E-12. Станок остановлен.",
 "status":"InProgress","equipmentId":3,"createdByUserId":1,"assigneeUserId":5,
 "assignee":{"id":5,"fullName":"Петров С. И."},
 "createdAt":"2026-11-07T07:18:45.914636+00:00","updatedAt":"2026-11-07T07:18:46.277468+00:00"}
HTTP 200
```

7. То же в базе:

```text
toir_workshop=# SELECT id, number, title, status, assignee_user_id FROM work_orders WHERE id = 1;
 id |    number    |         title          |   status   | assignee_user_id
----+--------------+------------------------+------------+------------------
  1 | ЗР-2026-0001 | Не включается шпиндель | InProgress |                5
```

Тот же `id`, номер, тема, статус и исполнитель до и после рестарта. В ЛР8 после такого рестарта список был пустым. `GET /api/work-orders/999` после рестарта — по-прежнему 404.

## 7. LINQ и SQL

Строка C# из `EfWorkOrderService.GetById`:

```csharp
db.WorkOrders
    .AsNoTracking()
    .Include(w => w.Assignee)
    .FirstOrDefault(w => w.Id == id);
```

SQL, который EF отправил в PostgreSQL (из лога `dotnet run`):

```sql
SELECT w.id, w.assignee_user_id, w.created_at, w.created_by_user_id, w.description,
       w.equipment_id, w.number, w.status, w.title, w.updated_at,
       u.id, u.full_name, u.login, u.password_hash, u.role
FROM work_orders AS w
LEFT JOIN users AS u ON w.assignee_user_id = u.id
WHERE w.id = @id
LIMIT 1
```

Тот же смысл, что у запроса ЛР9 с `LEFT JOIN users` (§4.2 отчёта ЛР9): `.Where/FirstOrDefault(w => w.Id == id)` → `WHERE w.id = @id LIMIT 1`, `.Include(w => w.Assignee)` → `LEFT JOIN users`. JOIN именно `LEFT`, потому что `AssigneeUserId` допускает `NULL` — заявка без исполнителя не теряется. `@id` — параметр: значение не склеивается со строкой SQL, поэтому SQL-инъекции нет.

Ещё пары из лога:

| C# | SQL |
|---|---|
| `.OrderByDescending(w => w.CreatedAt).ThenByDescending(w => w.Id)` | `ORDER BY w.created_at DESC, w.id DESC` — использует индекс `work_orders_created_at_idx` |
| `.Where(w => w.Number.StartsWith(prefix)).OrderByDescending(w => w.Number).Select(w => w.Number).FirstOrDefault()` | `SELECT w.number FROM work_orders AS w WHERE w.number LIKE @prefix_startswith ORDER BY w.number DESC LIMIT 1` |
| `db.WorkOrders.Add(workOrder); db.SaveChanges();` | `INSERT INTO work_orders (…) VALUES (@p0, …) RETURNING id;` |
| `workOrder.Status = newStatus; … db.SaveChanges();` | `UPDATE work_orders SET status = @p0, updated_at = @p1 WHERE id = @p2;` — только изменённые столбцы |

## 8. Ответы для защиты

1. **Чем миграция отличается от `schema.sql`?** `schema.sql` — ручной скрипт: удаляет таблицы и создаёт их заново, вместе с данными. Миграция генерируется из модели C# и хранит шаг изменения схемы («создать таблицы», потом «добавить столбец»). EF записывает применённые шаги в `__EFMigrationsHistory`, поэтому `database update` выполняет только новые и не трогает данные. Миграции лежат в Git: у соседа та же схема одной командой. Сменили модель — новая миграция, применённую не редактируем.
2. **Где лежит пароль?** В `dotnet user-secrets` на моём компьютере, вне папки репозитория. В Git — пустой ключ в `appsettings.json` и `UserSecretsId` в `.csproj`. `git grep -i password` настоящего пароля не находит.
3. **Какой SQL соответствует `.Where`?** `WHERE` с параметром: `FirstOrDefault(w => w.Id == id)` → `WHERE w.id = @id LIMIT 1` (§7). EF переводит выражение в SQL и выполняет его в PostgreSQL, а не выбирает все строки в память.

## 9. Что осталось на следующую работу

1. ЛР11: вход и роли вместо заголовка `X-User-Id`, заполнить `password_hash` (новой миграцией сделать `NOT NULL`).
2. Защита от одновременной правки одной заявки (два PATCH сразу) — токен конкурентности (`xmin` в PostgreSQL), отдельной миграцией.
3. ЛР12: подключить клиент Vue к серверу (`USE_MOCK = false`).

---

Подпись студента: __________ дата: __________
