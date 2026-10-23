# Data — доступ к данным

`ReferenceData.cs` — учебные справочники в памяти (ЛР8): оборудование (`equipmentId` 1–12) и пользователи
с ролями. По ним сервер проверяет `equipmentId` при создании и `assigneeUserId` при назначении.

Заявки сейчас хранятся в памяти: `Services/InMemoryWorkOrderService.cs`.

В ЛР10 здесь появится контекст Entity Framework Core для PostgreSQL
(таблицы `work_orders`, `equipment`, `areas`, `users` — см. `docs/data-model.md`),
а справочники станут начальными данными (seed).
