# ТОиР-Цех — веб-платформа управления техническим обслуживанием оборудования

Курс: Б1.В.ДЭ.01.02.02 · КГЭУ · семестр 1 · 2026/2027

| Поле | Значение |
|------|----------|
| Студент | Гаранин Антон |
| Группа | ТРИС-1-23 |
| Тема | Разработка веб-платформы управления техническим обслуживанием оборудования для промышленного предприятия |
| Организация | Механосборочный цех № 2, служба главного механика, машиностроительный завод «ТехМаш» (учебная) |
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
| `http://localhost:5173/` | список заявок на ремонт (загрузка, пусто, данные или ошибка) |
| `http://localhost:5173/new` | создание заявки (`title`, `equipmentId`, `description`) |
| `http://localhost:5173/login` | вход (логин, пароль) |

Данные идут через клиент API (`frontend/src/api/workOrders.js`). Пока сервера на C# нет, включён учебный mock (`USE_MOCK = true`): он отвечает на `/api/work-orders` прямо из dev-сервера Vite с задержкой 0,5 с и настоящими кодами 200 / 201 / 400 / 500 — их видно в F12 → Network. Учебные флаги в адресе: `/?empty` — пустой список, `/?force500` и `/new?force500` — ответ 500, `/new?skipClientCheck` — отправить поля без проверки в браузере, чтобы увидеть 400 от сервера. Подробнее — [`frontend/README.md`](frontend/README.md).

## Запуск сервера (ASP.NET Core Web API)

Нужен .NET SDK 10 (LTS): проверить командой `dotnet --version`. Если команды нет — установить SDK с https://dotnet.microsoft.com/download (не только Runtime).

```bash
cd backend
dotnet restore
dotnet run
```

В терминале должно появиться `Now listening on: http://localhost:5000` и `Hosting environment: Development`. Остановить — `Ctrl+C`.

| Адрес | Что там |
|-------|---------|
| `http://localhost:5000` | API |
| `http://localhost:5000/swagger` | Swagger — список адресов API с кнопкой «Try it out» (только в Development) |
| `http://localhost:5000/health` | `ok` — **только** «процесс жив и отвечает», не проверяет создание заявок |
| `http://localhost:5000/info` | имя приложения, среда, версия |

### REST заявок на ремонт (ЛР8)

Путь и поля — из контракта ЛР2 ([`docs/api-contract.md`](docs/api-contract.md)): ресурс `/api/work-orders`, поля создания `title`, `equipmentId`, `description`, статусы `New`, `InProgress`, `Closed`, `Cancelled`.

| Метод и путь | Успех | Отказы |
|--------------|-------|--------|
| `GET /api/work-orders` | `200`, массив (пустой — `[]`) | — |
| `GET /api/work-orders/{id}` | `200` | `404` |
| `POST /api/work-orders` | `201`, `id`, `number` `ЗР-ГГГГ-NNNN`, `status: "New"` | `400` поля, `409` создаёт не мастер/механик |
| `PATCH /api/work-orders/{id}/assignee` | `200`, статус остаётся `New` | `400` не слесарь, `404`, `409` заявка не в `New` |
| `PATCH /api/work-orders/{id}/status` | `200` | `400` неизвестный код, `404`, `409` запрещённый переход |

`DELETE` нет: отмена — статус `Cancelled`. Ошибки приходят в формате `ProblemDetails`, у `400` — `errors` по полям.

**Кто выполняет действие** (до входа в ЛР11) — заголовок `X-User-Id`, обязателен для `POST` и `PATCH …/status`. Учебные пользователи (`backend/Data/ReferenceData.cs`): `1` и `4` — мастера участка, `2` — механик цеха, `3` — начальник цеха, `5` и `6` — слесари-ремонтники. Оборудование — `equipmentId` от 1 до 12.

**Коллекция запросов:** [`docs/api-collection/work-orders.postman_collection.json`](docs/api-collection/work-orders.postman_collection.json). В Postman: *Import* → выбрать файл → переменная `baseUrl` = `http://localhost:5000` → *Run collection* (по порядку: запрос 02 запоминает `id` созданной заявки). В коллекции проверки 201, 400, 404, 409 и учебного 500. Быстрые запросы для VS Code / Rider — `backend/backend.http`.

**Учебный сбой** (только Development): заголовок `X-Simulate-Failure: 1` на `POST /api/work-orders` → клиенту `500` без stack trace, в консоли `dotnet run` строка `CreateWorkOrder failed; equipmentId=…; error=InvalidOperationException`.

Ограничения:

- **данные заявок пока в памяти, после перезапуска сервера пропадут**; хранение в PostgreSQL — ЛР10;
- **`/health` проверяет только живость процесса**: он не принимает полей заявки, и его `200` не доказывает, что заявки создаются;
- входа и ролей через токен ещё нет (ЛР11): пользователь берётся из `X-User-Id`, чужие действия возвращают `409`;
- клиент Vue к серверу пока не подключён (он работает с mock, см. выше).

## База данных (PostgreSQL, ЛР9)

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

Пароль — тот, что задали при установке PostgreSQL; `psql` спросит его сам. **Пароль в файлы и в Git не кладём.** Сервер ASP.NET Core к базе пока не подключён: заявки API ещё в памяти, подключение через EF Core — ЛР10.

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
