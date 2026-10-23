# frontend — клиент «ТОиР-Цех»

Vue 3 + Vite, язык JavaScript. Порт разработки — `5173` (схема в [`../docs/architecture.md`](../docs/architecture.md)).

```bash
npm install     # поставить зависимости (папка node_modules в Git не хранится)
npm run dev     # запустить Vite → http://localhost:5173
npm run lint    # проверить код (ESLint + Oxlint)
npm run build   # собрать в dist/ (в Git не хранится)
```

| Файл | Что внутри |
|------|------------|
| `src/App.vue` | три экрана: список заявок, создание заявки, вход; переключение кнопками и `v-if` |
| `src/main.js` | точка входа: подключает общие стили и монтирует `App` |
| `src/assets/main.css` | общие стили (цвета, кнопки, фокус) |
| `vite.config.js` | настройки Vite, порт `5173` |

Имена полей совпадают с контрактом API ЛР2 ([`../docs/api-contract.md`](../docs/api-contract.md)): `title`, `equipmentId`, `description`; статусы `New`, `InProgress`, `Closed`, `Cancelled`.
