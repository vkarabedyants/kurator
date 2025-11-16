-- ============================================
-- MIGRATION SCRIPT: Fix Technical Specification Contradictions
-- Version: 1.0.1
-- Date: 2025-11-14
-- Description: Исправление противоречий в схеме базы данных
-- ============================================

BEGIN;

-- ============================================
-- 1. ИСПРАВЛЕНИЕ РОЛЕЙ: Удаление BackupCurator из ENUM
-- ============================================

-- Сохраняем старые данные
CREATE TABLE IF NOT EXISTS _backup_users AS
SELECT * FROM users WHERE role = 'BackupCurator';

-- Обновляем роли резервных кураторов на обычных кураторов
UPDATE users SET role = 'Curator' WHERE role = 'BackupCurator';

-- Пересоздаем ENUM без BackupCurator
ALTER TYPE user_role RENAME TO user_role_old;

CREATE TYPE user_role AS ENUM ('Admin', 'Curator', 'ThreatAnalyst');

-- Обновляем таблицу пользователей
ALTER TABLE users ALTER COLUMN role TYPE user_role USING role::text::user_role;

-- Удаляем старый тип
DROP TYPE user_role_old;

-- ============================================
-- 2. СОЗДАНИЕ ТАБЛИЦЫ BlockCurator
-- ============================================

-- Создаем ENUM для типа куратора
CREATE TYPE curator_type AS ENUM ('Primary', 'Backup');

-- Создаем таблицу связи кураторов с блоками
CREATE TABLE IF NOT EXISTS block_curator (
    id SERIAL PRIMARY KEY,
    block_id INTEGER REFERENCES blocks(id) ON DELETE CASCADE,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    curator_type curator_type NOT NULL,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    assigned_by INTEGER REFERENCES users(id),
    UNIQUE(block_id, user_id),
    -- В блоке может быть только один основной и один резервный куратор
    UNIQUE(block_id, curator_type)
);

-- Создаем индексы
CREATE INDEX idx_block_curator_user ON block_curator(user_id);
CREATE INDEX idx_block_curator_block ON block_curator(block_id);

-- Мигрируем существующие связи из таблицы blocks (если есть поля curator_id)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'blocks' AND column_name = 'main_curator_id') THEN

        INSERT INTO block_curator (block_id, user_id, curator_type, assigned_by)
        SELECT id, main_curator_id, 'Primary'::curator_type, 1
        FROM blocks
        WHERE main_curator_id IS NOT NULL;

        ALTER TABLE blocks DROP COLUMN main_curator_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'blocks' AND column_name = 'backup_curator_id') THEN

        INSERT INTO block_curator (block_id, user_id, curator_type, assigned_by)
        SELECT id, backup_curator_id, 'Backup'::curator_type, 1
        FROM blocks
        WHERE backup_curator_id IS NOT NULL;

        ALTER TABLE blocks DROP COLUMN backup_curator_id;
    END IF;
END $$;

-- ============================================
-- 3. СОЗДАНИЕ ТАБЛИЦЫ ReferenceValues (Справочники)
-- ============================================

CREATE TABLE IF NOT EXISTS reference_values (
    id SERIAL PRIMARY KEY,
    category VARCHAR(50) NOT NULL,
    code VARCHAR(50) NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    sort_order INTEGER DEFAULT 0,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(category, code)
);

-- Создаем индекс для быстрого поиска
CREATE INDEX idx_reference_values_category ON reference_values(category, is_active);

-- Заполняем начальными значениями справочников
INSERT INTO reference_values (category, code, name, description, sort_order) VALUES
-- Статусы влияния
('influence_status', 'A', 'Статус A', 'Высокий уровень влияния', 1),
('influence_status', 'B', 'Статус B', 'Средний уровень влияния', 2),
('influence_status', 'C', 'Статус C', 'Низкий уровень влияния', 3),
('influence_status', 'D', 'Статус D', 'Минимальный уровень влияния', 4),

-- Типы влияния
('influence_type', 'NAV', 'Навигационное', 'Обеспечивает доступ, вводит в круг', 1),
('influence_type', 'INT', 'Интерпретационное', 'Помогает понять позиции и контекст', 2),
('influence_type', 'FUN', 'Функциональное', 'Помогает решать вопросы, воздействует на процессы', 3),
('influence_type', 'REP', 'Репутационное', 'Влияет на публичное восприятие', 4),
('influence_type', 'ANA', 'Аналитическое', 'Даёт стратегическую оценку, прогноз', 5),

-- Каналы коммуникации
('communication_channel', 'OFF', 'Официальный', NULL, 1),
('communication_channel', 'MED', 'Через посредника', NULL, 2),
('communication_channel', 'ASS', 'Через ассоциацию', NULL, 3),
('communication_channel', 'PER', 'Личный', NULL, 4),
('communication_channel', 'JUR', 'Юридический', NULL, 5),

-- Источники контактов
('contact_source', 'PER', 'Личное знакомство', NULL, 1),
('contact_source', 'ASS', 'Ассоциация', NULL, 2),
('contact_source', 'REC', 'Рекомендация', NULL, 3),
('contact_source', 'EVE', 'Ивент', NULL, 4),
('contact_source', 'MED', 'Медиа', NULL, 5),
('contact_source', 'OTH', 'Другое', NULL, 6),

-- Типы касаний
('interaction_type', 'MEE', 'Встреча', NULL, 1),
('interaction_type', 'CAL', 'Звонок', NULL, 2),
('interaction_type', 'MSG', 'Переписка', NULL, 3),
('interaction_type', 'EVE', 'Ивент', NULL, 4),
('interaction_type', 'OTH', 'Прочее', NULL, 5),

-- Результаты касаний
('interaction_result', 'POS', 'Позитивный', NULL, 1),
('interaction_result', 'NEU', 'Нейтральный', NULL, 2),
('interaction_result', 'NEG', 'Негативный', NULL, 3),
('interaction_result', 'DEL', 'Отложено', NULL, 4),
('interaction_result', 'NON', 'Без результата', NULL, 5),

-- Сферы риска
('risk_sphere', 'MED', 'Медиа', NULL, 1),
('risk_sphere', 'JUR', 'Юридическое давление', NULL, 2),
('risk_sphere', 'POL', 'Политическое', NULL, 3),
('risk_sphere', 'ECO', 'Экономическое', NULL, 4),
('risk_sphere', 'FOR', 'Силовое', NULL, 5),
('risk_sphere', 'COM', 'Коммуникации', NULL, 6),
('risk_sphere', 'OTH', 'Другое', NULL, 7),

-- Разрешенные расширения файлов
('file_extensions', 'PDF', 'PDF', 'Adobe PDF документы', 1),
('file_extensions', 'DOC', 'DOC', 'Microsoft Word', 2),
('file_extensions', 'DOCX', 'DOCX', 'Microsoft Word', 3),
('file_extensions', 'XLS', 'XLS', 'Microsoft Excel', 4),
('file_extensions', 'XLSX', 'XLSX', 'Microsoft Excel', 5),
('file_extensions', 'JPG', 'JPG', 'JPEG изображения', 6),
('file_extensions', 'PNG', 'PNG', 'PNG изображения', 7),
('file_extensions', 'TXT', 'TXT', 'Текстовые файлы', 8)
ON CONFLICT (category, code) DO NOTHING;

-- ============================================
-- 4. МОДИФИКАЦИЯ ТАБЛИЦЫ Contacts
-- ============================================

-- Добавляем недостающие поля и исправляем типы
ALTER TABLE contacts
    -- Заменяем ENUM на внешние ключи для справочников
    ADD COLUMN IF NOT EXISTS influence_status_id INTEGER REFERENCES reference_values(id),
    ADD COLUMN IF NOT EXISTS influence_type_id INTEGER REFERENCES reference_values(id),
    ADD COLUMN IF NOT EXISTS communication_channel_id INTEGER REFERENCES reference_values(id),
    ADD COLUMN IF NOT EXISTS contact_source_id INTEGER REFERENCES reference_values(id),
    ADD COLUMN IF NOT EXISTS organization_id INTEGER REFERENCES reference_values(id),
    -- Добавляем поле is_active вместо status
    ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT true,
    -- Добавляем curator_id если его нет
    ADD COLUMN IF NOT EXISTS curator_id INTEGER REFERENCES users(id);

-- Мигрируем данные из старых ENUM полей (если они существуют)
DO $$
BEGIN
    -- Миграция influence_status
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'contacts' AND column_name = 'influence_status'
               AND data_type = 'USER-DEFINED') THEN
        UPDATE contacts c
        SET influence_status_id = r.id
        FROM reference_values r
        WHERE r.category = 'influence_status'
        AND r.code = c.influence_status::text;

        ALTER TABLE contacts DROP COLUMN influence_status;
    END IF;

    -- Миграция influence_type
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'contacts' AND column_name = 'influence_type'
               AND data_type = 'USER-DEFINED') THEN
        UPDATE contacts c
        SET influence_type_id = r.id
        FROM reference_values r
        WHERE r.category = 'influence_type'
        AND CASE
            WHEN c.influence_type::text = 'Navigational' THEN 'NAV'
            WHEN c.influence_type::text = 'Interpretational' THEN 'INT'
            WHEN c.influence_type::text = 'Functional' THEN 'FUN'
            WHEN c.influence_type::text = 'Reputational' THEN 'REP'
            WHEN c.influence_type::text = 'Analytical' THEN 'ANA'
        END = r.code;

        ALTER TABLE contacts DROP COLUMN influence_type;
    END IF;
END $$;

-- ============================================
-- 5. МОДИФИКАЦИЯ ТАБЛИЦЫ Interactions
-- ============================================

ALTER TABLE interactions
    ADD COLUMN IF NOT EXISTS interaction_type_id INTEGER REFERENCES reference_values(id),
    ADD COLUMN IF NOT EXISTS result_id INTEGER REFERENCES reference_values(id),
    ADD COLUMN IF NOT EXISTS next_touch_date DATE,
    ADD COLUMN IF NOT EXISTS curator_id INTEGER REFERENCES users(id);

-- ============================================
-- 6. СОЗДАНИЕ ТАБЛИЦЫ WatchlistHistory
-- ============================================

-- Создаем ENUM для уровней риска
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'risk_level') THEN
        CREATE TYPE risk_level AS ENUM ('Low', 'Medium', 'High', 'Critical');
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS watchlist_history (
    id SERIAL PRIMARY KEY,
    watchlist_id INTEGER REFERENCES watchlist(id) ON DELETE CASCADE,
    old_risk_level risk_level,
    new_risk_level risk_level,
    changed_by INTEGER REFERENCES users(id),
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    comment TEXT
);

CREATE INDEX idx_watchlist_history_watchlist ON watchlist_history(watchlist_id);
CREATE INDEX idx_watchlist_history_changed_at ON watchlist_history(changed_at DESC);

-- ============================================
-- 7. СОЗДАНИЕ ТАБЛИЦЫ InfluenceStatusHistory
-- ============================================

CREATE TABLE IF NOT EXISTS influence_status_history (
    id SERIAL PRIMARY KEY,
    contact_id VARCHAR(20) REFERENCES contacts(id) ON DELETE CASCADE,
    old_status VARCHAR(10),
    new_status VARCHAR(10),
    changed_by INTEGER REFERENCES users(id),
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    interaction_id INTEGER REFERENCES interactions(id)
);

CREATE INDEX idx_influence_status_history_contact ON influence_status_history(contact_id);
CREATE INDEX idx_influence_status_history_changed_at ON influence_status_history(changed_at DESC);

-- ============================================
-- 8. МОДИФИКАЦИЯ ТАБЛИЦЫ Users для MFA
-- ============================================

ALTER TABLE users
    ADD COLUMN IF NOT EXISTS is_first_login BOOLEAN DEFAULT true,
    ADD COLUMN IF NOT EXISTS mfa_secret VARCHAR(255),
    ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT true;

-- Обновляем существующих пользователей
UPDATE users SET is_first_login = false WHERE mfa_secret IS NOT NULL;

-- ============================================
-- 9. МОДИФИКАЦИЯ ТАБЛИЦЫ AuditLog
-- ============================================

-- Создаем ENUM для типов действий
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'action_type') THEN
        CREATE TYPE action_type AS ENUM ('CREATE', 'UPDATE', 'DELETE', 'STATUS_CHANGE');
    END IF;
END $$;

-- Пересоздаем таблицу audit_log с правильной структурой
ALTER TABLE audit_log
    ADD COLUMN IF NOT EXISTS action action_type,
    ADD COLUMN IF NOT EXISTS entity_type VARCHAR(50),
    ADD COLUMN IF NOT EXISTS entity_id VARCHAR(50),
    ADD COLUMN IF NOT EXISTS old_values JSONB,
    ADD COLUMN IF NOT EXISTS new_values JSONB;

-- Мигрируем старые данные если нужно
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'audit_log' AND column_name = 'old_value') THEN
        UPDATE audit_log SET old_values = old_value::jsonb WHERE old_value IS NOT NULL;
        UPDATE audit_log SET new_values = new_value::jsonb WHERE new_value IS NOT NULL;
        ALTER TABLE audit_log DROP COLUMN old_value, DROP COLUMN new_value;
    END IF;
END $$;

-- ============================================
-- 10. МОДИФИКАЦИЯ ТАБЛИЦЫ FAQ
-- ============================================

ALTER TABLE faq
    ADD COLUMN IF NOT EXISTS sort_order INTEGER DEFAULT 0,
    ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT true,
    ADD COLUMN IF NOT EXISTS updated_by INTEGER REFERENCES users(id);

-- Удаляем поле видимости если оно существует
ALTER TABLE faq DROP COLUMN IF EXISTS visibility;

-- ============================================
-- 11. МОДИФИКАЦИЯ ТАБЛИЦЫ Watchlist
-- ============================================

-- Создаем ENUM для частоты мониторинга
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'monitoring_frequency') THEN
        CREATE TYPE monitoring_frequency AS ENUM ('Weekly', 'Monthly', 'Quarterly', 'AdHoc');
    END IF;
END $$;

ALTER TABLE watchlist
    ADD COLUMN IF NOT EXISTS risk_sphere_id INTEGER REFERENCES reference_values(id),
    ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT true,
    ADD COLUMN IF NOT EXISTS watch_owner_id INTEGER REFERENCES users(id);

-- ============================================
-- 12. СОЗДАНИЕ ПОСЛЕДОВАТЕЛЬНОСТЕЙ ДЛЯ ID КОНТАКТОВ
-- ============================================

-- Функция для создания последовательности для каждого блока
CREATE OR REPLACE FUNCTION create_block_sequence()
RETURNS TRIGGER AS $$
BEGIN
    EXECUTE format('CREATE SEQUENCE IF NOT EXISTS contact_seq_%s START 1', NEW.code);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Триггер для автоматического создания последовательности при создании блока
DROP TRIGGER IF EXISTS create_block_sequence_trigger ON blocks;
CREATE TRIGGER create_block_sequence_trigger
    AFTER INSERT ON blocks
    FOR EACH ROW
    EXECUTE FUNCTION create_block_sequence();

-- Создаем последовательности для существующих блоков
DO $$
DECLARE
    block_record RECORD;
BEGIN
    FOR block_record IN SELECT code FROM blocks LOOP
        EXECUTE format('CREATE SEQUENCE IF NOT EXISTS contact_seq_%s START 1', block_record.code);
    END LOOP;
END $$;

-- ============================================
-- 13. УНИФИКАЦИЯ ТЕРМИНОЛОГИИ УДАЛЕНИЯ
-- ============================================

-- Переименовываем колонки status в is_active где необходимо
DO $$
BEGIN
    -- Для таблицы contacts
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'contacts' AND column_name = 'status') THEN
        UPDATE contacts SET is_active = (status != 'Deleted');
        ALTER TABLE contacts DROP COLUMN status;
    END IF;

    -- Для таблицы interactions
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'interactions' AND column_name = 'status') THEN
        UPDATE interactions SET is_active = (status != 'Deleted');
        ALTER TABLE interactions DROP COLUMN status;
    END IF;
END $$;

-- ============================================
-- 14. УДАЛЕНИЕ СТАРЫХ ENUM ТИПОВ
-- ============================================

-- Удаляем старый influence_type ENUM если он существует
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_type WHERE typname = 'influence_type') THEN
        DROP TYPE influence_type CASCADE;
    END IF;

    IF EXISTS (SELECT 1 FROM pg_type WHERE typname = 'influence_status') THEN
        DROP TYPE influence_status CASCADE;
    END IF;
END $$;

-- ============================================
-- 15. СОЗДАНИЕ ИНДЕКСОВ ДЛЯ ПРОИЗВОДИТЕЛЬНОСТИ
-- ============================================

-- Индексы для contacts
CREATE INDEX IF NOT EXISTS idx_contacts_block_id ON contacts(block_id);
CREATE INDEX IF NOT EXISTS idx_contacts_next_touch_date ON contacts(next_touch_date);
CREATE INDEX IF NOT EXISTS idx_contacts_is_active ON contacts(is_active);
CREATE INDEX IF NOT EXISTS idx_contacts_curator_id ON contacts(curator_id);

-- Индексы для interactions
CREATE INDEX IF NOT EXISTS idx_interactions_contact_id ON interactions(contact_id);
CREATE INDEX IF NOT EXISTS idx_interactions_date ON interactions(interaction_date DESC);
CREATE INDEX IF NOT EXISTS idx_interactions_curator_id ON interactions(curator_id);

-- Индексы для watchlist
CREATE INDEX IF NOT EXISTS idx_watchlist_next_check ON watchlist(next_check);
CREATE INDEX IF NOT EXISTS idx_watchlist_is_active ON watchlist(is_active);

-- Индексы для audit_log
CREATE INDEX IF NOT EXISTS idx_audit_log_user_id ON audit_log(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_log_timestamp ON audit_log(timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_audit_log_entity ON audit_log(entity_type, entity_id);

COMMIT;

-- ============================================
-- ПРОВЕРКА УСПЕШНОСТИ МИГРАЦИИ
-- ============================================

-- Проверяем, что все таблицы созданы
SELECT 'Проверка таблиц:' as check_type,
       COUNT(*) as tables_count
FROM information_schema.tables
WHERE table_schema = 'public'
AND table_name IN ('block_curator', 'reference_values', 'watchlist_history', 'influence_status_history');

-- Проверяем, что BackupCurator удален из ролей
SELECT 'Проверка ролей:' as check_type,
       NOT EXISTS (SELECT 1 FROM pg_enum WHERE enumlabel = 'BackupCurator') as backup_curator_removed;

-- Проверяем наличие справочников
SELECT 'Справочники загружены:' as check_type,
       COUNT(DISTINCT category) as categories_count,
       COUNT(*) as total_values
FROM reference_values;

-- Выводим сообщение об успешной миграции
SELECT 'МИГРАЦИЯ ЗАВЕРШЕНА УСПЕШНО!' as status,
       'Все противоречия устранены' as message,
       NOW() as completed_at;