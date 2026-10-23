# frontend — клиент «ТОиР-Цех»

Vue 3 + Vite + vue-router, язык JavaScript. Порт разработки — `5173` (схема в [`../docs/architecture.md`](../docs/architecture.md)).

```bash
npm install     # поставить зависимости (папка node_modules в Git не хранится)
npm run dev     # запустить Vite → http://localhost:5173
npm run lint    # проверить код (ESLint + Oxlint)
npm run build   # собрать в dist/ (в Git не хранится)
```

## Адреса

| Адрес | Страница |
|-------|----------|
| `/` | `pages/WorkOrderList.vue` — список заявок |
| `/new` | `pages/WorkOrderNew.vue` — создание заявки |
| `/login` | `pages/LoginPage.vue` — вход |

Форма на `/new` добавляет заявку со статусом `New` в общий массив и возвращает на `/`. Список живёт в памяти браузера: после F5 новые заявки пропадают — сервера и БД пока нет.

Маршрут `/new` — это экран в браузере, а не запрос к серверу. Создание заявки на сервере — будущий `POST http://localhost:5000/api/work-orders`.

## Файлы

```text
src/
├── main.js                ← createApp, app.use(router), mount
├── App.vue                ← AppHeader + router-view
├── router.js              ← три маршрута
├── workOrdersStore.js     ← единственный общий массив заявок, nextId, addWorkOrder
├── workOrderValidation.js ← правила полей: title 5–80, equipmentId > 0, description 10–500
├── assets/main.css        ← общие стили
├── components/
│   ├── AppHeader.vue      ← название и меню из router-link
│   └── WorkOrderTable.vue ← таблица, получает items через props
└── pages/
    ├── WorkOrderList.vue  ← берёт массив из store и передаёт в таблицу
    ├── WorkOrderNew.vue   ← форма: v-model, проверка, @submit.prevent → addWorkOrder
    └── LoginPage.vue      ← форма: login, password
```

Имена полей совпадают с контрактом API ЛР2 ([`../docs/api-contract.md`](../docs/api-contract.md)): `title`, `equipmentId`, `description`; статусы `New`, `InProgress`, `Closed`, `Cancelled`.
