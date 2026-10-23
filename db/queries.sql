-- =============================================================================
-- ТОиР-Цех · учебные запросы (ЛР9)
-- Запуск после schema.sql и seed.sql:  psql -U postgres -d toir_workshop -f db/queries.sql
-- =============================================================================

SET client_encoding = 'UTF8';
SET TIME ZONE 'Europe/Moscow';  -- время в выводе — по Казани (МСК)

-- === 1. Фильтр и сортировка: новые заявки, свежие сверху (экран «Список», фильтр по статусу)
SELECT number, title, status, created_at
FROM work_orders
WHERE status = 'New'
ORDER BY created_at DESC;

-- === 2. JOIN: заявка + название станка и участка + ФИО исполнителя (вместо одних id)
-- equipment и areas — обязательные связи (JOIN), исполнителя может не быть (LEFT JOIN).
SELECT w.number,
       w.title,
       e.inventory_number,
       e.name                         AS equipment_name,
       a.name                         AS area_name,
       coalesce(f.full_name, '—')     AS assignee,
       w.status
FROM work_orders AS w
JOIN equipment   AS e ON e.id = w.equipment_id
JOIN areas       AS a ON a.id = e.area_id
LEFT JOIN users  AS f ON f.id = w.assignee_user_id
ORDER BY w.created_at DESC;

-- === 3. GROUP BY: сколько заявок в каждом статусе (сводка начальника цеха, ФТ-11)
SELECT status, COUNT(*) AS count
FROM work_orders
GROUP BY status
ORDER BY status;

-- === 4. JOIN + GROUP BY: какие участки сейчас ждут ремонта (незакрытые заявки по участкам)
SELECT a.name                                                 AS area_name,
       COUNT(*)                                               AS open_work_orders,
       COUNT(*) FILTER (WHERE w.status = 'InProgress')        AS in_progress
FROM work_orders AS w
JOIN equipment   AS e ON e.id = w.equipment_id
JOIN areas       AS a ON a.id = e.area_id
WHERE w.status IN ('New', 'InProgress')
GROUP BY a.name
ORDER BY open_work_orders DESC, a.name;

-- === 5. «Мои заявки» слесаря Петрова (ФТ-12): фильтр по исполнителю, сортировка по дате
SELECT w.number, w.title, w.status, w.updated_at
FROM work_orders AS w
JOIN users AS f ON f.id = w.assignee_user_id
WHERE f.login = 'petrov'
ORDER BY w.updated_at DESC;

-- -----------------------------------------------------------------------------
-- Проверки ограничений. Каждая — в своей транзакции с ROLLBACK:
-- PostgreSQL должен отказать, а рабочие данные не меняются.
-- -----------------------------------------------------------------------------

-- === 6. CHECK: статус Done — ожидаем ERROR ... violates check constraint "work_orders_status_check"
BEGIN;
INSERT INTO work_orders (number, title, description, status, equipment_id, created_by_user_id)
VALUES ('ЗР-2026-9001', 'Проверка статуса Done', 'Такой статус база принять не должна.', 'Done',
        (SELECT id FROM equipment WHERE inventory_number = 'ИНВ-04501'),
        (SELECT id FROM users WHERE login = 'ivanov'));
ROLLBACK;

-- === 7. FOREIGN KEY: несуществующее оборудование 999 — ожидаем ERROR ... violates foreign key constraint
BEGIN;
INSERT INTO work_orders (number, title, description, equipment_id, created_by_user_id)
VALUES ('ЗР-2026-9002', 'Проверка внешнего ключа', 'Оборудования с id 999 в справочнике нет.', 999,
        (SELECT id FROM users WHERE login = 'ivanov'));
ROLLBACK;

-- === 8. CHECK: InProgress без исполнителя — ожидаем ERROR ... "work_orders_work_needs_assignee_chk"
BEGIN;
UPDATE work_orders SET status = 'InProgress' WHERE number = 'ЗР-2026-0006';
ROLLBACK;

-- === 9. Контроль: после проверок данные не изменились
SELECT COUNT(*) AS total_work_orders FROM work_orders;
