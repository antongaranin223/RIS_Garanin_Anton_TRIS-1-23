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
| `/` | `pages/WorkOrderList.vue` — список заявок: загрузка / пусто / данные / ошибка |
| `/new` | `pages/WorkOrderNew.vue` — создание заявки через `createWorkOrder` |
| `/login` | `pages/LoginPage.vue` — вход |

Маршрут `/new` — это экран в браузере. Создание заявки — HTTP-запрос `POST /api/work-orders`.

## API и mock

Страницы вызывают только `getWorkOrders()`, `createWorkOrder(body)` (`src/api/workOrders.js`) и `login()` (`src/api/auth.js`) и не знают ни адреса, ни mock, ни токена.

| `USE_MOCK` в `src/api/http.js` | Куда уходят запросы |
|--------------------------------|---------------------|
| `false` (сейчас, с ЛР11) | `${VITE_API_BASE_URL}/api/…` = `http://localhost:5000/api/…` — ASP.NET Core Web API, вход и права проверяет сервер |
| `true` | `http://localhost:5173/api/work-orders` — учебный mock в dev-сервере Vite (`vite-mock-api.js` → `src/api/mockWorkOrders.js`); входа в mock нет |

**Вход (ЛР11).** `/login` отправляет `POST /api/auth/login`, ответ `{ token, expiresAt, user }` сохраняется в `sessionStorage` (`src/api/session.js`): переживает F5, пропадает при закрытии вкладки. `src/api/http.js` добавляет `Authorization: Bearer <токен>` в каждый запрос. Без входа роутер открывает `/login`; если сервер ответил `401` на запрос с токеном (истёк) — сессия очищается и снова открывается `/login`. Кнопка «Создать» видна только мастеру и механику — это удобство, запрещает сервер (`403`).

**Безопасный вывод.** Текст заявок выводится только через `{{ }}` — Vue экранирует `<`, `>`, и описание `<script>alert(1)</script>` видно как текст. `v-html` в проекте не используется.

Mock отвечает с задержкой 0,5 с настоящими HTTP-кодами (видно в F12 → Network). Данные mock живут в памяти процесса `npm run dev`: F5 их не сбрасывает, перезапуск Vite — сбрасывает.

Учебные флаги в адресе страницы (работают только с mock, `USE_MOCK = true`):

| Адрес | Что происходит |
|-------|----------------|
| `/?empty` | список пустой — `200` и `[]` |
| `/?force500`, `/new?force500` | mock отвечает `500` |
| `/new?skipClientCheck` | поля не проверяются в браузере — видно `400` от сервера |

Отказ «нет связи»: открыть клиент при незапущенном API на порту 5000.

## Файлы

```text
vite-mock-api.js           ← плагин Vite: отдаёт mock на /api/work-orders (только npm run dev)
.env.example               ← VITE_API_BASE_URL=http://localhost:5000
src/
├── main.js                ← createApp, app.use(router), mount
├── App.vue                ← AppHeader + router-view
├── router.js              ← три маршрута; без входа — на /login, /new только мастеру и механику
├── workOrdersStore.js     ← единственный общий массив заявок (заполняется ответом API)
├── workOrderValidation.js ← правила полей: title 5–80, equipmentId > 0, description 10–500
├── api/
│   ├── http.js            ← USE_MOCK, адрес API, fetch с Authorization, ApiError, errorMessage
│   ├── session.js         ← токен и текущий пользователь (sessionStorage), подписи ролей
│   ├── auth.js            ← login(), logout()
│   ├── workOrders.js      ← getWorkOrders, createWorkOrder
│   └── mockWorkOrders.js  ← учебный «сервер»: задержка, 200 / 201 / 400 / 500
├── assets/main.css        ← общие стили
├── components/
│   ├── AppHeader.vue      ← название, меню, ФИО и роль вошедшего, «Выйти»
│   └── WorkOrderTable.vue ← таблица, получает items через props
└── pages/
    ├── WorkOrderList.vue  ← loading / empty / data / error
    ├── WorkOrderNew.vue   ← проверка, createWorkOrder, кнопка disabled на время POST
    └── LoginPage.vue      ← вход: POST /api/auth/login, «Неверный логин или пароль»
```

Имена полей совпадают с контрактом API ЛР2 ([`../docs/api-contract.md`](../docs/api-contract.md)): `title`, `equipmentId`, `description`; статусы `New`, `InProgress`, `Closed`, `Cancelled`.
