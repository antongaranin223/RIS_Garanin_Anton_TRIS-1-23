# ТОиР-Цех — веб-платформа управления техническим обслуживанием оборудования

Курс: Б1.В.ДЭ.01.02.02 · КГЭУ · семестр 1 · 2026/2027

| Поле | Значение |
|------|----------|
| Студент | Гаранин Антон |
| Группа | ТРИС-1-23 |
| Тема | Разработка веб-платформы управления техническим обслуживанием оборудования для промышленного предприятия |
| Организация | Механосборочный цех № 2, служба главного механика, машиностроительный завод «Уралмаш» (ПАО «Уралмашзавод», г. Екатеринбург) |
| Главный объект | Заявка на ремонт / ТО оборудования (`/api/work-orders`) |
| Справочник | Оборудование (`/api/equipment`, поле `equipmentId`) |
| Статус темы | ждём утверждения |

Мастер участка создаёт заявку на сломанный станок, механик цеха назначает слесаря, слесарь переводит заявку в ремонт и закрывает её. Начальник цеха видит все заявки.

## Стенд

```text
 Браузер ──► Vue 3 (Vite)          http://localhost:5173
               │  fetch + Authorization: Bearer <JWT>
               ▼
             ASP.NET Core Web API   http://localhost:5000   (Swagger: /swagger)
               │  EF Core + Npgsql
               ▼
             PostgreSQL             localhost:5432, база toir_workshop
```

| Что | Версия | Проверить |
|-----|--------|-----------|
| .NET SDK | 10 (LTS) | `dotnet --version` → `10.0.…` |
| Инструмент миграций `dotnet-ef` | 10.0.x (не 11) | `dotnet ef --version` |
| Node.js | 22.18+ или 24 (LTS) | `node -v` |
| PostgreSQL | 16 или новее (с ним pgAdmin) | `psql --version` |

Проверено на .NET SDK 10.0.114, `dotnet-ef` 10.0.15, Node 22.23, PostgreSQL 16.15 (предзащита 12.11.2026).

Порты фиксированы: клиент **5173**, API **5000**, PostgreSQL **5432**. Если порт 5173 занят, Vite выберет другой, но API пустит запросы только с `http://localhost:5173` (CORS). Освободите порт, а не меняйте его.

## Развёртывание с нуля

Команды — для PowerShell на Windows. В Linux/macOS вместо `copy` пишите `cp`.

### 0. Установить инструменты

1. .NET SDK 10 — https://dotnet.microsoft.com/download (именно SDK, не Runtime).
2. Node.js LTS — https://nodejs.org.
3. PostgreSQL 16+ — https://www.postgresql.org/download/windows/. Пароль пользователя `postgres` задаётся при установке: запомните его, в репозиторий он не попадает.
4. Инструмент миграций, один раз на компьютер. Версия закреплена под EF Core 10 проекта: без `--version` ставится самый новый `dotnet-ef` (с ноября 2026 — 11.x):

   ```powershell
   dotnet tool install --global dotnet-ef --version "10.0.*"
   ```

   **Закройте и снова откройте терминал**, затем проверьте `dotnet ef --version` → `10.0.…`. Если уже стоит другая версия, удалите её командой `dotnet tool uninstall --global dotnet-ef` и повторите установку.

5. Если PowerShell пишет «выполнение сценариев отключено» на `npm`, разрешите скрипты для своего пользователя (один раз, права администратора не нужны):

   ```powershell
   Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
   ```

   Или вместо `npm` пишите `npm.cmd`.

### 1. Получить код

```powershell
git clone https://github.com/antongaranin223/RIS_Garanin_Anton_TRIS-1-23.git
cd RIS_Garanin_Anton_TRIS-1-23
```

### 2. Создать пустую базу

```powershell
& "C:\Program Files\PostgreSQL\16\bin\createdb.exe" -U postgres toir_workshop
```

Номер версии в пути — ваш (`16`, `17`…). Если папка `bin` есть в PATH, достаточно `createdb -U postgres toir_workshop`. Пароль `postgres` команда спросит сама. Можно и через pgAdmin: *Databases* → *Create* → *Database…* → имя `toir_workshop`.

Таблицы руками **не** создаём: их создадут миграции на шаге 4. Если база с таким именем уже есть (например, из ЛР9 с таблицами от `schema.sql`), удалите её: `dropdb -U postgres toir_workshop` — и создайте снова.

### 3. Задать секреты сервера (вне репозитория)

```powershell
cd backend
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=toir_workshop;Username=postgres;Password=ВАШ_ПАРОЛЬ_POSTGRES"
dotnet user-secrets set "Jwt:Key" "ЛЮБАЯ_СЛУЧАЙНАЯ_СТРОКА_НЕ_КОРОЧЕ_32_СИМВОЛОВ"
```

- `ВАШ_ПАРОЛЬ_POSTGRES` — пароль, заданный при установке PostgreSQL.
- `Jwt:Key` — придумайте сами: им сервер подписывает токены входа.
- user-secrets хранятся в профиле пользователя Windows (`%APPDATA%\Microsoft\UserSecrets\`), а не в папке проекта, поэтому в Git не попадают.
- Вместо user-secrets можно задать переменные окружения `ConnectionStrings__Default` и `Jwt__Key`.

### 4. Применить миграции

Там же, в папке `backend`:

```powershell
dotnet restore
dotnet ef database update
```

`dotnet restore` скачивает пакеты NuGet (EF Core, Npgsql, JwtBearer). Без него на свежей копии `dotnet ef` падает с `NETSDK1004: Assets file … project.assets.json not found`.

Ожидается в конце:

```text
Applying migration '20261107071750_InitialCreate'.
Applying migration '20261107073927_AddPasswordHashes'.
Done.
```

Красная строка `fail: … __EFMigrationsHistory` при **первом** запуске — норма: EF проверяет, есть ли таблица истории, не находит и создаёт её. В базе появятся таблицы `areas`, `equipment`, `users`, `work_orders`, а также 3 участка, 12 единиц оборудования и 6 учебных пользователей. Заявок нет — их создают через приложение.

### 5. Запустить API (терминал 1)

```powershell
dotnet run
```

Ждать строку `Now listening on: http://localhost:5000`. Проверка: http://localhost:5000/swagger открывается, http://localhost:5000/health отвечает `ok`. Окно не закрывать. Остановить — `Ctrl+C`.

Если API сразу падает с сообщением «Не задана строка подключения» или «Не задан ключ подписи JWT», вернитесь к шагу 3: команды выполняются в папке `backend`.

### 6. Запустить клиент (терминал 2)

```powershell
cd frontend
copy .env.example .env
npm install
npm run dev
```

Открыть http://localhost:5173 — появится экран входа.

В `frontend/.env` (из `.env.example`) — `VITE_API_BASE_URL=http://localhost:5000` и `VITE_USE_MOCK=false`, секретов там нет. После правки `.env` перезапустите `npm run dev`.

### Учебные логины

Пароль у всех — **`Toir-2026!`**. Он учебный, нигде вне стенда не используется. В базе хранится хэш, а не этот текст.

| Логин | Кто | Что может на экране |
|-------|-----|---------------------|
| `ivanov` | Иванов А. П., мастер участка | создать заявку, отменить свою новую |
| `smirnov` | Смирнов О. Л., мастер участка | то же (второй мастер — для проверки «чужой» заявки) |
| `kuznetsov` | Кузнецов Д. В., механик цеха | создать, **назначить** слесаря, отменить новую |
| `petrov` | Петров С. И., слесарь-ремонтник | «В ремонт» и «Выполнена» у **своих** заявок, фильтр «Мои заявки» |
| `sidorov` | Сидоров В. А., слесарь-ремонтник | то же (второй слесарь — для проверки 403) |
| `orlov` | Орлов Е. Н., начальник цеха | только смотреть, фильтр по статусу |

Полная матрица «роль × действие» с кодами ответов — [`docs/role-matrix.md`](docs/role-matrix.md).

## Проверочный сценарий (3 минуты)

1. Войти как `ivanov` → «Создать» → заголовок, оборудование из списка, описание → «Создать». В списке появится строка `ЗР-2026-0001`, `id 1`, статус «Новая». **Запишите id.**
2. «Выйти» → войти как `kuznetsov` → в строке заявки выбрать «Петров С. И.» → «Назначить». Исполнитель — Петров, статус по-прежнему «Новая».
3. «Выйти» → войти как `sidorov`: у заявки Петрова кнопок нет.
4. «Выйти» → войти как `petrov` → «В ремонт». Статус «В ремонте».
5. F12 → Network: `POST /api/work-orders` → 201, в Response тот же `id`; `PATCH …/1/assignee` → 200; `PATCH …/1/status` → 200.
6. pgAdmin или `psql -U postgres -d toir_workshop`:

   ```sql
   SELECT id, number, title, status, assignee_user_id FROM work_orders WHERE id = 1;
   ```

   Ожидается: `InProgress`, `assignee_user_id = 5`.
7. В терминале API — `Ctrl+C`, снова `dotnet run`. Обновить страницу: заявка на месте, «В ремонте».

Обход экрана: в Swagger выполнить `POST /api/auth/login` за `sidorov`, нажать **Authorize**, вставить токен и выполнить `PATCH /api/work-orders/1/status` с `{"status":"Closed"}` → **403** «Нет прав на изменение этой заявки». Права проверяет сервер, а не спрятанная кнопка.

Таблица «шаг — что ждём — где смотреть» по трём критериям ЛР1 — [`docs/acceptance-check.md`](docs/acceptance-check.md).

## API

Контракт — [`docs/api-contract.md`](docs/api-contract.md). Все адреса, кроме входа, требуют заголовок `Authorization: Bearer <токен>`. Ошибки приходят в формате `ProblemDetails`, у `400` — `errors` по полям.

| Метод и путь | Кто | Успех | Отказы |
|--------------|-----|-------|--------|
| `POST /api/auth/login` | все | `200`, `token`, `user` | `400`, `401` неверный логин или пароль |
| `GET /api/auth/me` | вошедшие | `200` | `401` |
| `GET /api/equipment` | вошедшие | `200`, оборудование с участком | `401` |
| `GET /api/users?role=Fitter` | механик | `200`, слесари | `401`, `403` |
| `GET /api/work-orders` | вошедшие | `200`, массив (пустой — `[]`) | `401` |
| `GET /api/work-orders/{id}` | вошедшие | `200` | `401`, `404` |
| `POST /api/work-orders` | мастер, механик | `201`, `number` `ЗР-ГГГГ-NNNN`, `status: "New"` | `400`, `401`, `403`, `409` номер занят параллельным запросом |
| `PATCH /api/work-orders/{id}/assignee` | механик | `200`, статус остаётся `New` | `400` не слесарь, `401`, `403`, `404`, `409` не `New` |
| `PATCH /api/work-orders/{id}/status` | по матрице ролей | `200` | `400` неизвестный код, `401`, `403` чужая заявка или роль, `404`, `409` запрещённый переход |

`DELETE` нет: отмена — статус `Cancelled`. `401` — сервер не знает, кто вы; `403` — знает, но вам нельзя; `409` — можно, но не в этом состоянии заявки.

- **Swagger**: http://localhost:5000/swagger → `POST /api/auth/login` → скопировать `token` → **Authorize** → вставить токен без слова `Bearer`.
- **Postman**: *Import* → [`docs/api-collection/work-orders.postman_collection.json`](docs/api-collection/work-orders.postman_collection.json) → *Run collection* по порядку. Запросы `00` сами входят и сохраняют токены. Внутри 27 запросов с проверками 200/201/400/401/403/404/409/500.
- **VS Code (REST Client)**: [`backend/backend.http`](backend/backend.http) — сначала четыре запроса входа.
- **Учебный сбой** (только Development): заголовок `X-Simulate-Failure: 1` на `POST /api/work-orders` → `500` без stack trace, в консоли API — одна строка `CreateWorkOrder failed; …`.

## Как устроено

**Сервер** (`backend/`), ASP.NET Core Web API:

- `Controllers/` — `WorkOrdersController` (заявки), `AuthController` (вход), `ReferenceController` (оборудование, слесари).
- `Services/EfWorkOrderService.cs` — правила процесса и права: переходы статусов, «своя / чужая заявка».
- `Data/AppDbContext.cs` — EF Core, таблицы и ограничения как в `db/schema.sql`.
- `Migrations/` — схема и начальные данные.
- `Auth/` — выпуск и проверка JWT.
- Пароли проверяет `PasswordHasher` из ASP.NET Identity.
- CORS разрешает только `http://localhost:5173` (`Cors:Origins` в `appsettings.json`).
- В Development в консоли видно SQL, который строит EF.

**Клиент** (`frontend/`), Vue 3 + Vite + vue-router. Подробнее — [`frontend/README.md`](frontend/README.md).

- `src/api/http.js` — адрес API и заголовок `Authorization`.
- `src/api/session.js` — токен в `sessionStorage`.
- Страницы: `/login`, `/` (список с действиями по ролям), `/new` (создание).
- Пользовательский текст выводится только через `{{ }}`, без `v-html`.
- Учебный mock ЛР6 включается `VITE_USE_MOCK=true`, но входа в нём нет — см. известные дефекты.

**База** (`db/`) — SQL-скрипты ЛР9: [`schema.sql`](db/schema.sql), [`seed.sql`](db/seed.sql), [`queries.sql`](db/queries.sql). Это **справка** по схеме и примеры запросов. Базу для приложения создают миграции (шаг 4), а не эти скрипты. `seed.sql` на базе после миграций выполнять не нужно: справочники там уже есть.

**Новая миграция** при изменении модели: `dotnet ef migrations add ИмяИзменения` → `dotnet ef database update`. Применённые миграции вручную не редактируем.

## Ограничения и известные дефекты

Список с приоритетами — [`docs/known-defects.md`](docs/known-defects.md). Главное:

- Регистрации и смены пароля нет. Учётки создаёт миграция.
- «Выйти» только забывает токен в браузере. Сам токен действует до конца срока (8 часов).
- Фильтры списка работают в браузере, постраничной загрузки нет.
- Изменения, сделанные другими пользователями, видны после «Обновить».

## Документы

- [Заявка на тему](docs/project-proposal.md) · [Требования ЛР1](docs/lab-01-requirements.md)
- ЛР2: [Архитектура](docs/architecture.md) · [Модель данных](docs/data-model.md) · [Контракт API](docs/api-contract.md) · [Матрица требований](docs/requirements-matrix.md)
- [Матрица ролей](docs/role-matrix.md) (ЛР11)
- [Проверка трёх критериев](docs/acceptance-check.md) · [Известные дефекты](docs/known-defects.md) (ЛР12)
- [Предзащита: дефекты, повтор критериев, план демо](docs/predefense-check.md) · [Пояснительная записка](docs/kursovaya-zapiska.md) (ЛР13)
- [Коллекция запросов API](docs/api-collection/work-orders.postman_collection.json)
- Отчёты: [ЛР2](docs/reports/lab-02.md) · [ЛР3](docs/reports/lab-03.md) · [ЛР4](docs/reports/lab-04.md) · [ЛР5](docs/reports/lab-05.md) · [ЛР6](docs/reports/lab-06.md) · [ЛР7](docs/reports/lab-07.md) · [ЛР8](docs/reports/lab-08.md) · [ЛР9](docs/reports/lab-09.md) · [ЛР10](docs/reports/lab-10.md) · [ЛР11](docs/reports/lab-11.md) · [ЛР12](docs/reports/lab-12.md) · [ЛР13](docs/reports/lab-13.md)

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
| 12 | Сквозная сборка | сдано на проверку |
| 13 | Предзащита | сдано на проверку |
