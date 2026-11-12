# frontend — клиент «ТОиР-Цех»

Vue 3 + Vite + vue-router, язык JavaScript. Порт разработки — `5173`, адрес будущего API — `5000` (схема в [`../docs/architecture.md`](../docs/architecture.md)).

```bash
copy .env.example .env   # один раз (Windows); Linux/macOS: cp .env.example .env
npm install              # поставить зависимости (папка node_modules в Git не хранится)
npm run dev              # запустить Vite → http://localhost:5173
npm run lint             # проверить код (ESLint + Oxlint)
npm run build            # собрать в dist/ (в Git не хранится)
```

`.env` в Git не хранится, в репозитории только `.env.example`. После правки `.env` перезапустите `npm run dev`.

## Адреса

| Адрес | Страница |
|-------|----------|
| `/` | `pages/WorkOrderList.vue` — список заявок: загрузка / пусто / данные / ошибка; фильтры, кнопки действий по ролям |
| `/new` | `pages/WorkOrderNew.vue` — создание заявки, оборудование из `GET /api/equipment` |
| `/login` | `pages/LoginPage.vue` — вход |

Маршрут `/new` — это экран в браузере. Создание заявки — HTTP-запрос `POST /api/work-orders`.

## API и mock

Страницы вызывают только функции из `src/api/` (`getWorkOrders`, `createWorkOrder`, `assignWorkOrder`, `changeWorkOrderStatus`, `getEquipment`, `getFitters`, `login`) и не знают ни адреса, ни mock, ни токена.

| `VITE_USE_MOCK` в `frontend/.env` | Куда уходят запросы |
|-----------------------------------|---------------------|
| `false` (рабочий режим, ЛР12) | `${VITE_API_BASE_URL}/api/…` = `http://localhost:5000/api/…` — ASP.NET Core Web API, вход и права проверяет сервер |
| `true` | `http://localhost:5173/api/work-orders` — учебный mock в dev-сервере Vite (`vite-mock-api.js` → `src/api/mockWorkOrders.js`); входа и справочников в mock нет, поэтому дальше экрана входа не пройти (см. `docs/known-defects.md`) |

После правки `.env` перезапустите `npm run dev`: Vite читает его только при старте.

**Вход (ЛР11).** `/login` отправляет `POST /api/auth/login`, ответ `{ token, expiresAt, user }` сохраняется в `sessionStorage` (`src/api/session.js`): переживает F5, пропадает при закрытии вкладки. `src/api/http.js` добавляет `Authorization: Bearer <токен>` в каждый запрос. Без входа роутер открывает `/login`; если сервер ответил `401` на запрос с токеном (истёк) — сессия очищается и снова открывается `/login`. Кнопка «Создать» видна только мастеру и механику — это удобство, запрещает сервер (`403`).

**Безопасный вывод.** Текст заявок выводится только через `{{ }}` — Vue экранирует `<`, `>`, и описание `<script>alert(1)</script>` видно как текст. `v-html` в проекте не используется.

Mock отвечает с задержкой 0,5 с настоящими HTTP-кодами (видно в F12 → Network). Данные mock живут в памяти процесса `npm run dev`: F5 их не сбрасывает, перезапуск Vite — сбрасывает.

Учебные флаги в адресе страницы (работают только с mock, `VITE_USE_MOCK=true`):

| Адрес | Что происходит |
|-------|----------------|
| `/?empty` | список пустой — `200` и `[]` |
| `/?force500`, `/new?force500` | mock отвечает `500` |
| `/new?skipClientCheck` | поля не проверяются в браузере — видно `400` от сервера |

Отказ «нет связи»: открыть клиент при незапущенном API на порту 5000.

## Файлы

```text
vite-mock-api.js           ← плагин Vite: отдаёт mock на /api/work-orders (только npm run dev)
.env.example               ← VITE_API_BASE_URL=http://localhost:5000, VITE_USE_MOCK=false
src/
├── main.js                ← createApp, app.use(router), mount
├── App.vue                ← AppHeader + router-view
├── router.js              ← три маршрута; без входа — на /login, /new только мастеру и механику
├── workOrdersStore.js     ← единственный общий массив заявок (заполняется ответом API)
├── workOrderValidation.js ← правила полей: title 5–80, equipmentId > 0, description 10–500
├── api/
│   ├── http.js            ← USE_MOCK (из .env), адрес API, fetch с Authorization, ApiError, errorMessage
│   ├── session.js         ← токен и текущий пользователь (sessionStorage), подписи ролей
│   ├── auth.js            ← login(), logout()
│   ├── workOrders.js      ← getWorkOrders, createWorkOrder, assignWorkOrder, changeWorkOrderStatus
│   ├── reference.js       ← getEquipment, getFitters (справочники для формы и «Назначить»)
│   └── mockWorkOrders.js  ← учебный «сервер»: задержка, 200 / 201 / 400 / 500
├── assets/main.css        ← общие стили
├── components/
│   ├── AppHeader.vue      ← название, меню, ФИО и роль вошедшего, «Выйти»
│   └── WorkOrderTable.vue ← таблица: props items/user/fitters, события assign и status; кнопки по ролям
└── pages/
    ├── WorkOrderList.vue  ← loading / empty / data / error, назначение и смена статуса, фильтры
    ├── WorkOrderNew.vue   ← проверка, createWorkOrder, кнопка disabled на время POST
    └── LoginPage.vue      ← вход: POST /api/auth/login, «Неверный логин или пароль»
```

Имена полей совпадают с контрактом API ЛР2 ([`../docs/api-contract.md`](../docs/api-contract.md)): `title`, `equipmentId`, `description`; статусы `New`, `InProgress`, `Closed`, `Cancelled`.
