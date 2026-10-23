-- =============================================================================
-- ТОиР-Цех · схема PostgreSQL (ЛР9)
-- Модель — docs/data-model.md (ЛР2); имена полей совпадают с JSON API (ЛР8):
--   JSON equipmentId  ↔  SQL equipment_id,  assigneeUserId ↔ assignee_user_id и т. д.
--
-- Запуск:  psql -U postgres -d toir_workshop -f db/schema.sql
-- Скрипт можно выполнять повторно: в начале он удаляет и заново создаёт
-- только таблицы этого проекта (учебное пересоздание схемы, не команда приложения).
-- =============================================================================

SET client_encoding = 'UTF8';  -- файл в UTF-8; на Windows psql иначе читает его как WIN1251

-- Пересоздание: таблицы удаляются в порядке, обратном зависимостям (сначала та, что ссылается).
DROP TABLE IF EXISTS work_orders;
DROP TABLE IF EXISTS equipment;
DROP TABLE IF EXISTS areas;
DROP TABLE IF EXISTS users;

-- -----------------------------------------------------------------------------
-- Участки цеха — справочник для оборудования.
-- Название участка хранится один раз здесь (3НФ), оборудование ссылается на него area_id.
-- -----------------------------------------------------------------------------
CREATE TABLE areas (
    id    integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name  varchar(60) NOT NULL UNIQUE
);

-- -----------------------------------------------------------------------------
-- Оборудование — справочник главного объекта (на курсе: Site / site_id).
-- -----------------------------------------------------------------------------
CREATE TABLE equipment (
    id                integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    inventory_number  varchar(20)  NOT NULL UNIQUE,
    name              varchar(100) NOT NULL,
    area_id           integer      NOT NULL REFERENCES areas (id) ON DELETE RESTRICT
);

-- зачем: JOIN оборудования с участком и выборка станков одного участка
CREATE INDEX equipment_area_id_idx ON equipment (area_id);

-- -----------------------------------------------------------------------------
-- Пользователи: автор заявки (мастер, механик) и исполнитель (слесарь).
-- -----------------------------------------------------------------------------
CREATE TABLE users (
    id             integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    login          varchar(40)  NOT NULL UNIQUE,
    full_name      varchar(100) NOT NULL,
    role           varchar(16)  NOT NULL
                   CHECK (role IN ('Foreman', 'Mechanic', 'Fitter', 'ShopChief')),
    -- Только хеш пароля (НФТ-5). Вход появится в ЛР11, до этого колонка пустая.
    password_hash  varchar(200) NULL
);

-- -----------------------------------------------------------------------------
-- Заявки на ремонт / ТО — главный объект (на курсе: tickets).
-- -----------------------------------------------------------------------------
CREATE TABLE work_orders (
    id                  integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    -- Человеческий номер ЗР-ГГГГ-NNNN; не замена id.
    number              varchar(16)  NOT NULL UNIQUE,
    title               varchar(80)  NOT NULL
                        CHECK (char_length(btrim(title)) BETWEEN 5 AND 80),
    description         varchar(500) NOT NULL
                        CHECK (char_length(btrim(description)) BETWEEN 10 AND 500),
    -- Только четыре кода статуса, как в API и на клиенте.
    status              varchar(16)  NOT NULL DEFAULT 'New'
                        CHECK (status IN ('New', 'InProgress', 'Closed', 'Cancelled')),
    -- Ссылка на справочник оборудования; название станка здесь НЕ хранится (3НФ).
    equipment_id        integer      NOT NULL REFERENCES equipment (id) ON DELETE RESTRICT,
    created_by_user_id  integer      NOT NULL REFERENCES users (id) ON DELETE RESTRICT,
    -- Исполнитель: у новой заявки NULL (ещё не назначен).
    assignee_user_id    integer      NULL     REFERENCES users (id) ON DELETE RESTRICT,
    created_at          timestamptz  NOT NULL DEFAULT now(),
    updated_at          timestamptz  NOT NULL DEFAULT now(),
    -- Правило процесса из ЛР8: «в ремонт» и «выполнена» только с назначенным исполнителем.
    CONSTRAINT work_orders_work_needs_assignee_chk
        CHECK (status IN ('New', 'Cancelled') OR assignee_user_id IS NOT NULL)
);

-- зачем: фильтр списка по статусу (WHERE status = 'New'), сводка начальника цеха (ФТ-11)
CREATE INDEX work_orders_status_idx ON work_orders (status);

-- зачем: фильтр по станку (ФТ-11) и JOIN заявок с оборудованием
CREATE INDEX work_orders_equipment_id_idx ON work_orders (equipment_id);

-- зачем: фильтр «Мои заявки» слесаря (ФТ-12: WHERE assignee_user_id = …)
CREATE INDEX work_orders_assignee_user_id_idx ON work_orders (assignee_user_id);

-- зачем: сортировка списка «новые сверху» (ORDER BY created_at DESC)
CREATE INDEX work_orders_created_at_idx ON work_orders (created_at DESC);
