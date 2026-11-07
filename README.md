# ТОиР-Цех — веб-платформа управления техническим обслуживанием оборудования

Курс: Б1.В.ДЭ.01.02.02 · КГЭУ · семестр 1 · 2026/2027

| Поле | Значение |
|------|----------|
| Студент | Гаранин Антон |
| Группа | ТРИС-1-23 |
| Тема | Разработка веб-платформы управления техническим обслуживанием оборудования для промышленного предприятия |
| Организация | Механосборочный цех № 2, служба главного механика, машиностроительный завод «Уралмаш» (ПАО «Уралмашзавод», г. Екатеринбург) |
| Главный объект | Заявка на ремонт / ТО оборудования (`/api/work-orders`) |
| Справочник | Оборудование (`equipmentId`) |
| Стек | Vue 3 · ASP.NET Core Web API · PostgreSQL |
| Статус темы | ждём утверждения |

## Запуск клиента (Vue)

Нужен Node.js LTS (22.18+ или 24): проверить командами `node -v` и `npm -v`.

```bash
cd frontend
copy .env.example .env      # Windows; в Linux/macOS: cp .env.example .env
npm install
npm run dev
```

Открыть адрес Vite, обычно `http://localhost:5173` (если порт занят, Vite напишет другой адрес в терминале). Остановить — `Ctrl+C`.

В клиенте три страницы со своими адресами; меню в шапке видно на всех:

| Адрес | Страница |
|-------|----------|
| `http://localhost:5173/login` | вход (логин, пароль) — открывается первым, пока не вошли |
| `http://localhost:5173/` | список заявок на ремонт (загрузка, пусто, данные или ошибка) — после входа |
| `http://localhost:5173/new` | создание заявки (`title`, `equipmentId`, `description`) — только мастеру и механику |

С ЛР11 клиент работает с **настоящим API** на `http://localhost:5000` (адрес — `VITE_API_BASE_URL` в `.env`), поэтому сервер должен быть запущен. Вход — `POST /api/auth/login`, полученный токен клиент кладёт в `sessionStorage` и шлёт в каждом запросе заголовком `Authorization: Bearer …`. В шапке — ФИО и роль, кнопка «Выйти». Учебный mock ЛР6 остался (`USE_MOCK = true` в `frontend/src/api/http.js`), но входа в нём нет. Подробнее — [`frontend/README.md`](frontend/README.md).

## Запуск сервера (ASP.NET Core Web API)

Нужен .NET SDK 10 (LTS): проверить командой `dotnet --version`. Если команды нет — установить SDK с https://dotnet.microsoft.com/download (не только Runtime).

Сервер хранит заявки в PostgreSQL (ЛР10), поэтому перед первым запуском нужны база, строка подключения и миграция — подробно в разделе «База данных» ниже. Коротко:

```bash
cd backend
dotnet restore
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=toir_workshop;Username=postgres;Password=ВАШ_ПАРОЛЬ"
dotnet user-secrets set "Jwt:Key" "ЛЮБАЯ_СЛУЧАЙНАЯ_СТРОКА_НЕ_КОРОЧЕ_32_СИМВОЛОВ"
dotnet ef database update
dotnet run
```

В терминале должно появиться `Now listening on: http://localhost:5000` и `Hosting environment: Development`. Остановить — `Ctrl+C`.

| Адрес | Что там |
|-------|---------|
| `http://localhost:5000` | API |
| `http://localhost:5000/swagger` | Swagger — список адресов API с кнопкой «Try it out» (только в Development) |
| `http://localhost:5000/health` | `ok` — **только** «процесс жив и отвечает», не проверяет создание заявок |
| `http://localhost:5000/info` | имя приложения, среда, версия |

### Вход и права (ЛР11)

**Способ входа — учебный JWT в заголовке `Authorization: Bearer <токен>`** (cookie не используем). Токен выдаёт `POST /api/auth/login`, он действует 8 часов (смена); `GET /api/auth/me` — кто вошёл.

| Логин | Роль | Что может |
|-------|------|-----------|
| `ivanov`, `smirnov` | мастер участка | создавать, отменять **свою** заявку в `New` |
| `kuznetsov` | механик цеха | создавать, назначать слесаря, отменять заявку в `New` |
| `petrov`, `sidorov` | слесарь-ремонтник | переводить **свою** заявку в `InProgress` и `Closed` |
| `orlov` | начальник цеха | только смотреть |

Учебный пароль у всех — **`Toir-2026!`** (только для стенда, нигде больше не используется). В базе лежит хэш `PasswordHasher` из ASP.NET Identity, а не этот текст. Полная таблица «роль × действие» — [`docs/role-matrix.md`](docs/role-matrix.md).

Без токена любой адрес `/api/work-orders` отвечает **401**, роли без права или на чужую заявку — **403**. Права проверяет сервер, а не клиент.

**Ключ подписи JWT — секрет**, его нет в Git: `dotnet user-secrets set "Jwt:Key" "…"` (или переменная окружения `Jwt__Key`). Без ключа API не запустится и напишет, какую команду выполнить. Сменили ключ — все выданные токены перестают действовать, нужно войти заново.

**Swagger:** выполнить `POST /api/auth/login` → скопировать `token` → кнопка **Authorize** вверху → вставить токен (без слова `Bearer`) → дальше запросы идут от этого пользователя.

**CORS:** API разрешает запросы только с адреса клиента `http://localhost:5173` (`Cors:Origins` в `appsettings.json`), не со всех сайтов.

### REST заявок на ремонт (ЛР8)

Путь и поля — из контракта ЛР2 ([`docs/api-contract.md`](docs/api-contract.md)): ресурс `/api/work-orders`, поля создания `title`, `equipmentId`, `description`, статусы `New`, `InProgress`, `Closed`, `Cancelled`.

| Метод и путь | Успех | Отказы |
|--------------|-------|--------|
| `GET /api/work-orders` | `200`, массив (пустой — `[]`) | — |
| `GET /api/work-orders/{id}` | `200` | `404` |
| `POST /api/work-orders` | `201`, `id`, `number` `ЗР-ГГГГ-NNNN`, `status: "New"` | `400` поля, `401` без входа, `403` не мастер/механик, `409` номер занят параллельным запросом |
| `PATCH /api/work-orders/{id}/assignee` | `200`, статус остаётся `New` | `400` не слесарь, `401`, `403` не механик, `404`, `409` заявка не в `New` |
| `PATCH /api/work-orders/{id}/status` | `200` | `400` неизвестный код, `401`, `403` чужая заявка или не та роль, `404`, `409` запрещённый переход |

`DELETE` нет: отмена — статус `Cancelled`. Ошибки приходят в формате `ProblemDetails`, у `400` — `errors` по полям. `GET` тоже требуют входа (`401` без токена).

**Кто выполняет действие** — только пользователь из токена (утверждение `sub`). Заголовка `X-User-Id` из ЛР8 больше нет. Оборудование — `equipmentId` от 1 до 12.

**Коллекция запросов:** [`docs/api-collection/work-orders.postman_collection.json`](docs/api-collection/work-orders.postman_collection.json). В Postman: *Import* → выбрать файл → *Run collection* по порядку. Запросы `00` входят под учебными пользователями и сохраняют токены в переменные, запрос `02` запоминает `id` созданной заявки. Внутри проверки 200, 201, 400, 401, 403, 404, 409 и учебного 500. Быстрые запросы для VS Code (REST Client) — `backend/backend.http`: сначала выполнить четыре запроса входа.

**Учебный сбой** (только Development): заголовок `X-Simulate-Failure: 1` на `POST /api/work-orders` → клиенту `500` без stack trace, в консоли `dotnet run` строка `CreateWorkOrder failed; equipmentId=…; error=InvalidOperationException`.

Ограничения:

- заявки хранятся в PostgreSQL через EF Core и **переживают перезапуск сервера** (ЛР10);
- **`/health` проверяет только живость процесса**: он не принимает полей заявки, и его `200` не доказывает, что заявки создаются;
- регистрации, смены и восстановления пароля нет: учётки создаёт миграция (seed);
- выход — клиент просто забывает токен; сам токен действует до конца срока (8 часов), отозвать его на сервере нельзя;
- в клиенте пока только вход, список и создание; назначение и смена статуса на экране — ЛР12 (через API уже работают).

## База данных (PostgreSQL, ЛР9–ЛР10)

SQL-скрипты лежат в [`db/`](db):

| Файл | Что внутри |
|------|------------|
| [`db/schema.sql`](db/schema.sql) | таблицы `areas`, `equipment`, `users`, `work_orders`; PK, FK, CHECK (четыре статуса), индексы. Можно запускать повторно: в начале удаляет и заново создаёт только таблицы проекта |
| [`db/seed.sql`](db/seed.sql) | учебные данные: 3 участка, 12 единиц оборудования, 6 пользователей, 8 заявок во всех четырёх статусах |
| [`db/queries.sql`](db/queries.sql) | фильтр + сортировка, JOIN с названиями оборудования и участка, GROUP BY по статусам, проверки CHECK и FK |

Нужен PostgreSQL 16 или новее (установщик для Windows: https://www.postgresql.org/download/windows/, вместе с ним ставится pgAdmin). Учебная база — `toir_workshop`, не служебная `postgres`.

**Через `psql`** (из корня репозитория; на Windows `psql` лежит в `C:\Program Files\PostgreSQL\<версия>\bin` — добавьте папку в PATH или пишите полный путь):

```bash
createdb -U postgres toir_workshop
psql -U postgres -d toir_workshop -f db/schema.sql
psql -U postgres -d toir_workshop -f db/seed.sql
psql -U postgres -d toir_workshop -f db/queries.sql
```

**Через pgAdmin:** создать базу `toir_workshop` → *Query Tool* → открыть и выполнить `schema.sql`, затем `seed.sql`; запросы из `queries.sql` выполнять по одному (выделить запрос → F5).

Пересоздать всё с нуля: снова выполнить `schema.sql` и `seed.sql` (или `dropdb -U postgres toir_workshop` и команды выше).

Пароль — тот, что задали при установке PostgreSQL; `psql` спросит его сам. **Пароль в файлы и в Git не кладём.**

### Сервер и EF Core (ЛР10)

API работает с базой через EF Core (`backend/Data/AppDbContext.cs`). Схему для API создают **миграции** из `backend/Migrations`, а не `schema.sql` — тот остаётся справкой ЛР9. Справочники (участки, оборудование, пользователи с хэшами паролей) вставляют сами миграции; заявки создаются через API.

1. Инструмент миграций (один раз на компьютер):

   ```bash
   dotnet tool install --global dotnet-ef   # уже стоит: dotnet tool update --global dotnet-ef
   dotnet ef --version
   ```

2. Пустая база. Если в `toir_workshop` уже есть таблицы из `schema.sql`, пересоздайте её, иначе миграция упадёт на `relation "areas" already exists`:

   ```bash
   dropdb -U postgres --if-exists toir_workshop
   createdb -U postgres toir_workshop
   ```

3. Строка подключения — в **user-secrets**, вне репозитория (в `appsettings.json` только пустой ключ):

   ```bash
   cd backend
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=toir_workshop;Username=postgres;Password=ВАШ_ПАРОЛЬ"
   ```

   Там же — ключ подписи JWT (ЛР11), тоже не в Git:

   ```bash
   dotnet user-secrets set "Jwt:Key" "ЛЮБАЯ_СЛУЧАЙНАЯ_СТРОКА_НЕ_КОРОЧЕ_32_СИМВОЛОВ"
   ```

   Вместо user-secrets можно задать переменные окружения `ConnectionStrings__Default` и `Jwt__Key`.

4. Применить миграции и запустить:

   ```bash
   dotnet ef database update
   dotnet run
   ```

Миграции: `InitialCreate` (ЛР10, таблицы и справочники), `AddPasswordHashes` (ЛР11, хэши паролей, `password_hash NOT NULL`). Если база уже создана в ЛР10, `dotnet ef database update` применит только вторую — заявки останутся.

Сменили модель — новая миграция: `dotnet ef migrations add ИмяИзменения`, затем `dotnet ef database update`. Применённые миграции вручную не редактируем. В консоли `dotnet run` (Development) видно SQL, который строит EF.

## Документы

- [Заявка на тему](docs/project-proposal.md)
- [Требования ЛР1](docs/lab-01-requirements.md)
- [Архитектура (схема из трёх узлов)](docs/architecture.md) — ЛР2
- [Модель данных (ER, 3НФ, статусы)](docs/data-model.md) — ЛР2
- [Контракт API](docs/api-contract.md) — ЛР2
- [Матрица требований](docs/requirements-matrix.md) — ЛР2
- [Отчёт ЛР2](docs/reports/lab-02.md)
- [Отчёт ЛР3](docs/reports/lab-03.md)
- [Отчёт ЛР4](docs/reports/lab-04.md)
- [Отчёт ЛР5](docs/reports/lab-05.md)
- [Отчёт ЛР6](docs/reports/lab-06.md)
- [Отчёт ЛР7](docs/reports/lab-07.md)
- [Коллекция запросов API (ЛР8)](docs/api-collection/work-orders.postman_collection.json)
- [Отчёт ЛР8](docs/reports/lab-08.md)
- [Отчёт ЛР9](docs/reports/lab-09.md)
- [Отчёт ЛР10](docs/reports/lab-10.md)
- [Матрица ролей (ЛР11)](docs/role-matrix.md)
- [Отчёт ЛР11](docs/reports/lab-11.md)

## Лабораторные работы

| № | Название | Статус |
|---|----------|--------|
| 1 | Анализ и требования | выполнено |
| 2 | Архитектура и модель данных | сдано на проверку |
| 3 | Vue: каркас и три экрана | сдано на проверку |
| 4 | Vue: компоненты и маршруты | сдано на проверку |
| 5 | Vue: форма и список в памяти | сдано на проверку |
| 6 | Vue: клиент API и mock | сдано на проверку |
| 7 | ASP.NET Core: каркас Web API | сдано на проверку |
| 8 | REST: создание, чтение, назначение и статус | сдано на проверку |
| 9 | PostgreSQL и SQL | сдано на проверку |
| 10 | EF Core и PostgreSQL | сдано на проверку |
| 11 | Вход и права | сдано на проверку |
