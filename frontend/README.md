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

Страницы вызывают только `getWorkOrders()` и `createWorkOrder(body)` из `src/api/workOrders.js` и не знают ни адреса, ни mock.

| `USE_MOCK` в `src/api/workOrders.js` | Куда уходят запросы |
|--------------------------------------|---------------------|
| `true` (сейчас) | `http://localhost:5173/api/work-orders` — учебный mock в dev-сервере Vite (`vite-mock-api.js` → `src/api/mockWorkOrders.js`) |
| `false` | `${VITE_API_BASE_URL}/api/work-orders` = `http://localhost:5000/api/work-orders` — ASP.NET Core Web API |

Mock отвечает с задержкой 0,5 с настоящими HTTP-кодами (видно в F12 → Network). Данные mock живут в памяти процесса `npm run dev`: F5 их не сбрасывает, перезапуск Vite — сбрасывает.

Учебные флаги в адресе страницы (работают только с mock):

| Адрес | Что происходит |
|-------|----------------|
| `/?empty` | список пустой — `200` и `[]` |
| `/?force500`, `/new?force500` | mock отвечает `500` |
| `/new?skipClientCheck` | поля не проверяются в браузере — видно `400` от сервера |

Отказ «нет связи»: поставить `USE_MOCK = false` при незапущенном API на порту 5000.

## Файлы

```text
vite-mock-api.js           ← плагин Vite: отдаёт mock на /api/work-orders (только npm run dev)
.env.example               ← VITE_API_BASE_URL=http://localhost:5000
src/
├── main.js                ← createApp, app.use(router), mount
├── App.vue                ← AppHeader + router-view
├── router.js              ← три маршрута
├── workOrdersStore.js     ← единственный общий массив заявок (заполняется ответом API)
├── workOrderValidation.js ← правила полей: title 5–80, equipmentId > 0, description 10–500
├── api/
│   ├── workOrders.js      ← USE_MOCK, fetch, getWorkOrders, createWorkOrder, ApiError
│   └── mockWorkOrders.js  ← учебный «сервер»: задержка, 200 / 201 / 400 / 500
├── assets/main.css        ← общие стили
├── components/
│   ├── AppHeader.vue      ← название и меню из router-link
│   └── WorkOrderTable.vue ← таблица, получает items через props
└── pages/
    ├── WorkOrderList.vue  ← loading / empty / data / error
    ├── WorkOrderNew.vue   ← проверка, createWorkOrder, кнопка disabled на время POST
    └── LoginPage.vue      ← форма: login, password
```

Имена полей совпадают с контрактом API ЛР2 ([`../docs/api-contract.md`](../docs/api-contract.md)): `title`, `equipmentId`, `description`; статусы `New`, `InProgress`, `Closed`, `Cancelled`.
