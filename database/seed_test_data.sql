-- KURATOR Test Data Seed Script
-- Based on Business Analysis Requirements
-- Uses ON CONFLICT to handle existing data

BEGIN;

-- ============================================
-- 1. REFERENCE VALUES (Справочники)
-- ============================================

-- Influence Status (Статус влияния: A/B/C/D)
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt")
VALUES
('InfluenceStatus', 'A', 'Статус A', 'Активное сотрудничество, высокая степень влияния', 1, true, NOW(), NOW()),
('InfluenceStatus', 'B', 'Статус B', 'Умеренное сотрудничество, средняя степень влияния', 2, true, NOW(), NOW()),
('InfluenceStatus', 'C', 'Статус C', 'Потенциальное сотрудничество, ограниченное влияние', 3, true, NOW(), NOW()),
('InfluenceStatus', 'D', 'Статус D', 'Минимальный контакт, низкая степень влияния', 4, true, NOW(), NOW())
ON CONFLICT ("Category", "Code") DO NOTHING;

-- Influence Type (Тип влияния)
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt")
VALUES
('InfluenceType', 'NAV', 'Навигационное', 'Помогает ориентироваться в структуре', 1, true, NOW(), NOW()),
('InfluenceType', 'INT', 'Интерпретационное', 'Помогает понимать процессы и решения', 2, true, NOW(), NOW()),
('InfluenceType', 'FUN', 'Функциональное', 'Помогает решать конкретные задачи', 3, true, NOW(), NOW()),
('InfluenceType', 'REP', 'Репутационное', 'Влияет на репутацию и имидж', 4, true, NOW(), NOW()),
('InfluenceType', 'ANA', 'Аналитическое', 'Предоставляет аналитику и экспертизу', 5, true, NOW(), NOW())
ON CONFLICT ("Category", "Code") DO NOTHING;

-- Communication Channel (Канал коммуникации)
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt")
VALUES
('CommunicationChannel', 'PHONE', 'Телефон', 'Телефонная связь', 1, true, NOW(), NOW()),
('CommunicationChannel', 'EMAIL', 'Email', 'Электронная почта', 2, true, NOW(), NOW()),
('CommunicationChannel', 'MEETING', 'Личная встреча', 'Личная встреча', 3, true, NOW(), NOW()),
('CommunicationChannel', 'MESSENGER', 'Мессенджер', 'Telegram, WhatsApp и др.', 4, true, NOW(), NOW()),
('CommunicationChannel', 'VIDEO', 'Видеоконференция', 'Zoom, Teams и др.', 5, true, NOW(), NOW())
ON CONFLICT ("Category", "Code") DO NOTHING;

-- Contact Source (Источник контакта)
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt")
VALUES
('ContactSource', 'CONF', 'Конференция', 'Знакомство на конференции', 1, true, NOW(), NOW()),
('ContactSource', 'REF', 'Рекомендация', 'По рекомендации', 2, true, NOW(), NOW()),
('ContactSource', 'COLD', 'Холодный контакт', 'Самостоятельный поиск', 3, true, NOW(), NOW()),
('ContactSource', 'PARTNER', 'Партнёр', 'Через партнёрскую сеть', 4, true, NOW(), NOW()),
('ContactSource', 'EVENT', 'Мероприятие', 'Деловое мероприятие', 5, true, NOW(), NOW())
ON CONFLICT ("Category", "Code") DO NOTHING;

-- Interaction Type (Тип касания)
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt")
VALUES
('InteractionType', 'CALL', 'Звонок', 'Телефонный звонок', 1, true, NOW(), NOW()),
('InteractionType', 'MEET', 'Встреча', 'Личная встреча', 2, true, NOW(), NOW()),
('InteractionType', 'EMAIL', 'Письмо', 'Email переписка', 3, true, NOW(), NOW()),
('InteractionType', 'MSG', 'Сообщение', 'Сообщение в мессенджере', 4, true, NOW(), NOW()),
('InteractionType', 'EVENT', 'Мероприятие', 'Совместное мероприятие', 5, true, NOW(), NOW()),
('InteractionType', 'GIFT', 'Подарок', 'Передача подарка', 6, true, NOW(), NOW())
ON CONFLICT ("Category", "Code") DO NOTHING;

-- Interaction Result (Результат касания)
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt")
VALUES
('InteractionResult', 'POS', 'Позитивный', 'Положительный результат', 1, true, NOW(), NOW()),
('InteractionResult', 'NEU', 'Нейтральный', 'Нейтральный результат', 2, true, NOW(), NOW()),
('InteractionResult', 'NEG', 'Негативный', 'Отрицательный результат', 3, true, NOW(), NOW()),
('InteractionResult', 'PEND', 'В ожидании', 'Ожидается обратная связь', 4, true, NOW(), NOW())
ON CONFLICT ("Category", "Code") DO NOTHING;

-- Organization (Орган/структура)
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt")
VALUES
('Organization', 'GOV', 'Государственный орган', 'Государственные структуры', 1, true, NOW(), NOW()),
('Organization', 'MUN', 'Муниципалитет', 'Муниципальные органы', 2, true, NOW(), NOW()),
('Organization', 'CORP', 'Корпорация', 'Крупные корпорации', 3, true, NOW(), NOW()),
('Organization', 'SMB', 'Средний бизнес', 'Средний и малый бизнес', 4, true, NOW(), NOW()),
('Organization', 'NGO', 'НКО', 'Некоммерческие организации', 5, true, NOW(), NOW()),
('Organization', 'MEDIA', 'СМИ', 'Средства массовой информации', 6, true, NOW(), NOW()),
('Organization', 'EDU', 'Образование', 'Образовательные учреждения', 7, true, NOW(), NOW())
ON CONFLICT ("Category", "Code") DO NOTHING;

-- Risk Sphere (Сфера риска для Watchlist)
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt")
VALUES
('RiskSphere', 'LEGAL', 'Юридическая', 'Юридические риски', 1, true, NOW(), NOW()),
('RiskSphere', 'REPUT', 'Репутационная', 'Репутационные риски', 2, true, NOW(), NOW()),
('RiskSphere', 'FIN', 'Финансовая', 'Финансовые риски', 3, true, NOW(), NOW()),
('RiskSphere', 'OPER', 'Операционная', 'Операционные риски', 4, true, NOW(), NOW()),
('RiskSphere', 'COMP', 'Конкурентная', 'Конкурентные риски', 5, true, NOW(), NOW()),
('RiskSphere', 'REG', 'Регуляторная', 'Регуляторные риски', 6, true, NOW(), NOW())
ON CONFLICT ("Category", "Code") DO NOTHING;

-- ============================================
-- 2. USERS (Пользователи)
-- ============================================

-- Password hash for 'Admin123!' generated with BCrypt
INSERT INTO users ("Login", "PasswordHash", "Role", "IsFirstLogin", "IsActive", "MfaEnabled", "CreatedAt")
VALUES
('curator1', '$2a$11$Z2iwgX4CkQbhtu4LXTudGeD9caysC6MZHlLggDb25jL2acEdngipC', 'Curator', false, true, false, NOW()),
('curator2', '$2a$11$Z2iwgX4CkQbhtu4LXTudGeD9caysC6MZHlLggDb25jL2acEdngipC', 'Curator', false, true, false, NOW()),
('curator3', '$2a$11$Z2iwgX4CkQbhtu4LXTudGeD9caysC6MZHlLggDb25jL2acEdngipC', 'Curator', false, true, false, NOW()),
('analyst1', '$2a$11$Z2iwgX4CkQbhtu4LXTudGeD9caysC6MZHlLggDb25jL2acEdngipC', 'ThreatAnalyst', false, true, false, NOW()),
('analyst2', '$2a$11$Z2iwgX4CkQbhtu4LXTudGeD9caysC6MZHlLggDb25jL2acEdngipC', 'ThreatAnalyst', false, true, false, NOW())
ON CONFLICT ("Login") DO NOTHING;

-- ============================================
-- 3. BLOCKS (Блоки)
-- ============================================

-- Create additional blocks (keep existing TEST block)
INSERT INTO blocks ("Name", "Code", "Description", "Status", "CreatedAt", "UpdatedAt")
VALUES
('Операционный блок', 'OP', 'Блок для операционной деятельности', 'Active', NOW(), NOW()),
('Стратегический блок', 'STR', 'Блок для стратегических инициатив', 'Active', NOW(), NOW()),
('Региональный блок', 'REG', 'Блок для региональных контактов', 'Active', NOW(), NOW()),
('Архивный блок', 'ARCH', 'Архивированный блок для примера', 'Archived', NOW(), NOW())
ON CONFLICT ("Code") DO NOTHING;

-- ============================================
-- 4. BLOCK-CURATOR ASSIGNMENTS
-- ============================================

-- Get user IDs and block IDs for assignments
DO $$
DECLARE
    admin_id INT := (SELECT "Id" FROM users WHERE "Login" = 'admin');
    curator1_id INT := (SELECT "Id" FROM users WHERE "Login" = 'curator1');
    curator2_id INT := (SELECT "Id" FROM users WHERE "Login" = 'curator2');
    curator3_id INT := (SELECT "Id" FROM users WHERE "Login" = 'curator3');
    test_block_id INT := (SELECT "Id" FROM blocks WHERE "Code" = 'TEST');
    op_block_id INT := (SELECT "Id" FROM blocks WHERE "Code" = 'OP');
    str_block_id INT := (SELECT "Id" FROM blocks WHERE "Code" = 'STR');
    reg_block_id INT := (SELECT "Id" FROM blocks WHERE "Code" = 'REG');
BEGIN
    -- TEST block: curator1 primary (skip if TEST block already has primary curator)
    IF NOT EXISTS (SELECT 1 FROM block_curator WHERE "BlockId" = test_block_id AND "CuratorType" = 'Primary') THEN
        INSERT INTO block_curator ("BlockId", "UserId", "CuratorType", "AssignedAt", "AssignedBy")
        VALUES (test_block_id, curator1_id, 'Primary', NOW(), admin_id);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM block_curator WHERE "BlockId" = test_block_id AND "CuratorType" = 'Backup') THEN
        INSERT INTO block_curator ("BlockId", "UserId", "CuratorType", "AssignedAt", "AssignedBy")
        VALUES (test_block_id, curator2_id, 'Backup', NOW(), admin_id);
    END IF;

    -- OP block: curator2 primary, curator3 backup
    IF NOT EXISTS (SELECT 1 FROM block_curator WHERE "BlockId" = op_block_id AND "CuratorType" = 'Primary') THEN
        INSERT INTO block_curator ("BlockId", "UserId", "CuratorType", "AssignedAt", "AssignedBy")
        VALUES (op_block_id, curator2_id, 'Primary', NOW(), admin_id);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM block_curator WHERE "BlockId" = op_block_id AND "CuratorType" = 'Backup') THEN
        INSERT INTO block_curator ("BlockId", "UserId", "CuratorType", "AssignedAt", "AssignedBy")
        VALUES (op_block_id, curator3_id, 'Backup', NOW(), admin_id);
    END IF;

    -- STR block: curator1 primary
    IF NOT EXISTS (SELECT 1 FROM block_curator WHERE "BlockId" = str_block_id AND "CuratorType" = 'Primary') THEN
        INSERT INTO block_curator ("BlockId", "UserId", "CuratorType", "AssignedAt", "AssignedBy")
        VALUES (str_block_id, curator1_id, 'Primary', NOW(), admin_id);
    END IF;

    -- REG block: curator3 primary
    IF NOT EXISTS (SELECT 1 FROM block_curator WHERE "BlockId" = reg_block_id AND "CuratorType" = 'Primary') THEN
        INSERT INTO block_curator ("BlockId", "UserId", "CuratorType", "AssignedAt", "AssignedBy")
        VALUES (reg_block_id, curator3_id, 'Primary', NOW(), admin_id);
    END IF;
END $$;

-- ============================================
-- 5. CONTACTS (Контакты)
-- ============================================

DO $$
DECLARE
    curator1_id INT := (SELECT "Id" FROM users WHERE "Login" = 'curator1');
    curator2_id INT := (SELECT "Id" FROM users WHERE "Login" = 'curator2');
    curator3_id INT := (SELECT "Id" FROM users WHERE "Login" = 'curator3');
    admin_id INT := (SELECT "Id" FROM users WHERE "Login" = 'admin');
    test_block_id INT := (SELECT "Id" FROM blocks WHERE "Code" = 'TEST');
    op_block_id INT := (SELECT "Id" FROM blocks WHERE "Code" = 'OP');
    str_block_id INT := (SELECT "Id" FROM blocks WHERE "Code" = 'STR');
    reg_block_id INT := (SELECT "Id" FROM blocks WHERE "Code" = 'REG');
    status_a INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InfluenceStatus' AND "Code" = 'A');
    status_b INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InfluenceStatus' AND "Code" = 'B');
    status_c INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InfluenceStatus' AND "Code" = 'C');
    status_d INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InfluenceStatus' AND "Code" = 'D');
    type_nav INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InfluenceType' AND "Code" = 'NAV');
    type_int INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InfluenceType' AND "Code" = 'INT');
    type_fun INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InfluenceType' AND "Code" = 'FUN');
    type_rep INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InfluenceType' AND "Code" = 'REP');
    org_gov INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'Organization' AND "Code" = 'GOV');
    org_corp INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'Organization' AND "Code" = 'CORP');
    org_smb INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'Organization' AND "Code" = 'SMB');
    org_media INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'Organization' AND "Code" = 'MEDIA');
    ch_phone INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'CommunicationChannel' AND "Code" = 'PHONE');
    ch_email INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'CommunicationChannel' AND "Code" = 'EMAIL');
    ch_meet INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'CommunicationChannel' AND "Code" = 'MEETING');
    src_conf INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'ContactSource' AND "Code" = 'CONF');
    src_ref INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'ContactSource' AND "Code" = 'REF');
    src_partner INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'ContactSource' AND "Code" = 'PARTNER');
    src_cold INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'ContactSource' AND "Code" = 'COLD');
    actual_curator1 INT;
    actual_curator2 INT;
    actual_curator3 INT;
BEGIN
    -- Use admin as fallback if curators don't exist
    actual_curator1 := COALESCE(curator1_id, admin_id);
    actual_curator2 := COALESCE(curator2_id, admin_id);
    actual_curator3 := COALESCE(curator3_id, admin_id);

    -- TEST block contacts
    IF NOT EXISTS (SELECT 1 FROM contacts WHERE "ContactId" = 'TEST-001') THEN
        INSERT INTO contacts ("ContactId", "BlockId", "FullNameEncrypted", "OrganizationId", "Position", "InfluenceStatusId", "InfluenceTypeId", "UsefulnessDescription", "CommunicationChannelId", "ContactSourceId", "LastInteractionDate", "NextTouchDate", "NotesEncrypted", "ResponsibleCuratorId", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES ('TEST-001', test_block_id, 'Иванов Иван Иванович', org_gov, 'Директор департамента', status_a, type_nav, 'Ключевой контакт для навигации в госструктурах', ch_phone, src_conf, NOW() - INTERVAL '5 days', NOW() + INTERVAL '10 days', 'Важный контакт, требует регулярного внимания', actual_curator1, true, NOW(), NOW(), actual_curator1);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM contacts WHERE "ContactId" = 'TEST-002') THEN
        INSERT INTO contacts ("ContactId", "BlockId", "FullNameEncrypted", "OrganizationId", "Position", "InfluenceStatusId", "InfluenceTypeId", "UsefulnessDescription", "CommunicationChannelId", "ContactSourceId", "LastInteractionDate", "NextTouchDate", "NotesEncrypted", "ResponsibleCuratorId", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES ('TEST-002', test_block_id, 'Петрова Анна Сергеевна', org_corp, 'Заместитель генерального директора', status_b, type_fun, 'Функциональная поддержка проектов', ch_email, src_ref, NOW() - INTERVAL '10 days', NOW() + INTERVAL '5 days', 'Активное сотрудничество по проектам', actual_curator1, true, NOW(), NOW(), actual_curator1);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM contacts WHERE "ContactId" = 'TEST-003') THEN
        INSERT INTO contacts ("ContactId", "BlockId", "FullNameEncrypted", "OrganizationId", "Position", "InfluenceStatusId", "InfluenceTypeId", "UsefulnessDescription", "CommunicationChannelId", "ContactSourceId", "LastInteractionDate", "NextTouchDate", "NotesEncrypted", "ResponsibleCuratorId", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES ('TEST-003', test_block_id, 'Сидоров Михаил Петрович', org_smb, 'Владелец компании', status_c, type_rep, 'Репутационная поддержка', ch_meet, src_partner, NOW() - INTERVAL '30 days', NOW() - INTERVAL '5 days', 'Требует возобновления контакта', actual_curator2, true, NOW(), NOW(), actual_curator2);
    END IF;

    -- OP block contacts
    IF NOT EXISTS (SELECT 1 FROM contacts WHERE "ContactId" = 'OP-001') THEN
        INSERT INTO contacts ("ContactId", "BlockId", "FullNameEncrypted", "OrganizationId", "Position", "InfluenceStatusId", "InfluenceTypeId", "UsefulnessDescription", "CommunicationChannelId", "ContactSourceId", "LastInteractionDate", "NextTouchDate", "NotesEncrypted", "ResponsibleCuratorId", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES ('OP-001', op_block_id, 'Козлов Дмитрий Александрович', org_gov, 'Начальник управления', status_a, type_int, 'Интерпретация регуляторных требований', ch_phone, src_conf, NOW() - INTERVAL '3 days', NOW() + INTERVAL '14 days', 'Надёжный источник информации', actual_curator2, true, NOW(), NOW(), actual_curator2);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM contacts WHERE "ContactId" = 'OP-002') THEN
        INSERT INTO contacts ("ContactId", "BlockId", "FullNameEncrypted", "OrganizationId", "Position", "InfluenceStatusId", "InfluenceTypeId", "UsefulnessDescription", "CommunicationChannelId", "ContactSourceId", "LastInteractionDate", "NextTouchDate", "NotesEncrypted", "ResponsibleCuratorId", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES ('OP-002', op_block_id, 'Новикова Елена Владимировна', org_corp, 'Финансовый директор', status_b, type_fun, 'Финансовое партнёрство', ch_email, src_ref, NOW() - INTERVAL '7 days', NOW() + INTERVAL '7 days', 'Потенциал для расширения сотрудничества', actual_curator2, true, NOW(), NOW(), actual_curator2);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM contacts WHERE "ContactId" = 'OP-003') THEN
        INSERT INTO contacts ("ContactId", "BlockId", "FullNameEncrypted", "OrganizationId", "Position", "InfluenceStatusId", "InfluenceTypeId", "UsefulnessDescription", "CommunicationChannelId", "ContactSourceId", "LastInteractionDate", "NextTouchDate", "NotesEncrypted", "ResponsibleCuratorId", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES ('OP-003', op_block_id, 'Морозов Андрей Николаевич', org_media, 'Главный редактор', status_a, type_rep, 'Медийная поддержка', ch_meet, src_partner, NOW() - INTERVAL '14 days', NOW() + INTERVAL '3 days', 'Активный медийный партнёр', actual_curator3, true, NOW(), NOW(), actual_curator3);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM contacts WHERE "ContactId" = 'OP-004') THEN
        INSERT INTO contacts ("ContactId", "BlockId", "FullNameEncrypted", "OrganizationId", "Position", "InfluenceStatusId", "InfluenceTypeId", "UsefulnessDescription", "CommunicationChannelId", "ContactSourceId", "LastInteractionDate", "NextTouchDate", "NotesEncrypted", "ResponsibleCuratorId", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES ('OP-004', op_block_id, 'Волкова Ольга Игоревна', org_smb, 'Директор по развитию', status_c, type_nav, 'Навигация в бизнес-среде', ch_phone, src_conf, NOW() - INTERVAL '45 days', NOW() - INTERVAL '15 days', 'Нужно восстановить контакт', actual_curator3, true, NOW(), NOW(), actual_curator3);
    END IF;

    -- STR block contacts
    IF NOT EXISTS (SELECT 1 FROM contacts WHERE "ContactId" = 'STR-001') THEN
        INSERT INTO contacts ("ContactId", "BlockId", "FullNameEncrypted", "OrganizationId", "Position", "InfluenceStatusId", "InfluenceTypeId", "UsefulnessDescription", "CommunicationChannelId", "ContactSourceId", "LastInteractionDate", "NextTouchDate", "NotesEncrypted", "ResponsibleCuratorId", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES ('STR-001', str_block_id, 'Белов Сергей Викторович', org_gov, 'Советник министра', status_a, type_int, 'Стратегическое влияние на уровне министерства', ch_meet, src_ref, NOW() - INTERVAL '2 days', NOW() + INTERVAL '21 days', 'VIP-контакт, приоритетное внимание', actual_curator1, true, NOW(), NOW(), actual_curator1);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM contacts WHERE "ContactId" = 'STR-002') THEN
        INSERT INTO contacts ("ContactId", "BlockId", "FullNameEncrypted", "OrganizationId", "Position", "InfluenceStatusId", "InfluenceTypeId", "UsefulnessDescription", "CommunicationChannelId", "ContactSourceId", "LastInteractionDate", "NextTouchDate", "NotesEncrypted", "ResponsibleCuratorId", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES ('STR-002', str_block_id, 'Кузнецова Мария Дмитриевна', org_corp, 'Член совета директоров', status_b, type_fun, 'Стратегические решения на уровне совета', ch_email, src_partner, NOW() - INTERVAL '20 days', NOW() + INTERVAL '10 days', 'Важный корпоративный контакт', actual_curator1, true, NOW(), NOW(), actual_curator1);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM contacts WHERE "ContactId" = 'STR-003') THEN
        INSERT INTO contacts ("ContactId", "BlockId", "FullNameEncrypted", "OrganizationId", "Position", "InfluenceStatusId", "InfluenceTypeId", "UsefulnessDescription", "CommunicationChannelId", "ContactSourceId", "LastInteractionDate", "NextTouchDate", "NotesEncrypted", "ResponsibleCuratorId", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES ('STR-003', str_block_id, 'Соколов Алексей Петрович', org_gov, 'Депутат', status_d, type_rep, 'Лоббирование интересов', ch_phone, src_cold, NOW() - INTERVAL '60 days', NULL, 'Контакт требует реактивации', actual_curator1, false, NOW(), NOW(), actual_curator1);
    END IF;

    -- REG block contacts
    IF NOT EXISTS (SELECT 1 FROM contacts WHERE "ContactId" = 'REG-001') THEN
        INSERT INTO contacts ("ContactId", "BlockId", "FullNameEncrypted", "OrganizationId", "Position", "InfluenceStatusId", "InfluenceTypeId", "UsefulnessDescription", "CommunicationChannelId", "ContactSourceId", "LastInteractionDate", "NextTouchDate", "NotesEncrypted", "ResponsibleCuratorId", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES ('REG-001', reg_block_id, 'Федоров Владимир Сергеевич', org_gov, 'Губернатор области', status_a, type_nav, 'Навигация в региональных структурах', ch_meet, src_conf, NOW() - INTERVAL '7 days', NOW() + INTERVAL '30 days', 'Ключевой региональный контакт', actual_curator3, true, NOW(), NOW(), actual_curator3);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM contacts WHERE "ContactId" = 'REG-002') THEN
        INSERT INTO contacts ("ContactId", "BlockId", "FullNameEncrypted", "OrganizationId", "Position", "InfluenceStatusId", "InfluenceTypeId", "UsefulnessDescription", "CommunicationChannelId", "ContactSourceId", "LastInteractionDate", "NextTouchDate", "NotesEncrypted", "ResponsibleCuratorId", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES ('REG-002', reg_block_id, 'Попова Татьяна Александровна', org_corp, 'Генеральный директор', status_b, type_fun, 'Региональное бизнес-партнёрство', ch_email, src_partner, NOW() - INTERVAL '15 days', NOW() + INTERVAL '5 days', 'Активное региональное сотрудничество', actual_curator3, true, NOW(), NOW(), actual_curator3);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM contacts WHERE "ContactId" = 'REG-003') THEN
        INSERT INTO contacts ("ContactId", "BlockId", "FullNameEncrypted", "OrganizationId", "Position", "InfluenceStatusId", "InfluenceTypeId", "UsefulnessDescription", "CommunicationChannelId", "ContactSourceId", "LastInteractionDate", "NextTouchDate", "NotesEncrypted", "ResponsibleCuratorId", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES ('REG-003', reg_block_id, 'Орлов Николай Михайлович', org_media, 'Директор телеканала', status_c, type_rep, 'Региональная медийная поддержка', ch_phone, src_ref, NOW() - INTERVAL '25 days', NOW() - INTERVAL '5 days', 'Нужно возобновить контакт', actual_curator3, true, NOW(), NOW(), actual_curator3);
    END IF;
END $$;

-- ============================================
-- 6. INTERACTIONS (Касания)
-- ============================================

DO $$
DECLARE
    curator1_id INT := (SELECT "Id" FROM users WHERE "Login" = 'curator1');
    curator2_id INT := (SELECT "Id" FROM users WHERE "Login" = 'curator2');
    curator3_id INT := (SELECT "Id" FROM users WHERE "Login" = 'curator3');
    admin_id INT := (SELECT "Id" FROM users WHERE "Login" = 'admin');
    type_call INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InteractionType' AND "Code" = 'CALL');
    type_meet INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InteractionType' AND "Code" = 'MEET');
    type_email INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InteractionType' AND "Code" = 'EMAIL');
    type_msg INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InteractionType' AND "Code" = 'MSG');
    type_event INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InteractionType' AND "Code" = 'EVENT');
    res_pos INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InteractionResult' AND "Code" = 'POS');
    res_neu INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InteractionResult' AND "Code" = 'NEU');
    res_neg INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InteractionResult' AND "Code" = 'NEG');
    res_pend INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'InteractionResult' AND "Code" = 'PEND');
    contact_id INT;
    actual_curator1 INT;
    actual_curator2 INT;
    actual_curator3 INT;
BEGIN
    actual_curator1 := COALESCE(curator1_id, admin_id);
    actual_curator2 := COALESCE(curator2_id, admin_id);
    actual_curator3 := COALESCE(curator3_id, admin_id);

    -- Interactions for TEST-001
    contact_id := (SELECT "Id" FROM contacts WHERE "ContactId" = 'TEST-001');
    IF contact_id IS NOT NULL THEN
        INSERT INTO interactions ("ContactId", "InteractionDate", "InteractionTypeId", "CuratorId", "ResultId", "CommentEncrypted", "StatusChangeJson", "NextTouchDate", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES
        (contact_id, NOW() - INTERVAL '30 days', type_call, actual_curator1, res_pos, 'Первичный контакт, установлено взаимопонимание', '{"from": "C", "to": "B"}', NOW() - INTERVAL '15 days', true, NOW() - INTERVAL '30 days', NOW() - INTERVAL '30 days', actual_curator1),
        (contact_id, NOW() - INTERVAL '15 days', type_meet, actual_curator1, res_pos, 'Личная встреча, обсуждение возможностей сотрудничества', '{"from": "B", "to": "A"}', NOW() - INTERVAL '5 days', true, NOW() - INTERVAL '15 days', NOW() - INTERVAL '15 days', actual_curator1),
        (contact_id, NOW() - INTERVAL '5 days', type_email, actual_curator1, res_pos, 'Подтверждение договорённостей по email', NULL, NOW() + INTERVAL '10 days', true, NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days', actual_curator1);
    END IF;

    -- Interactions for TEST-002
    contact_id := (SELECT "Id" FROM contacts WHERE "ContactId" = 'TEST-002');
    IF contact_id IS NOT NULL THEN
        INSERT INTO interactions ("ContactId", "InteractionDate", "InteractionTypeId", "CuratorId", "ResultId", "CommentEncrypted", "StatusChangeJson", "NextTouchDate", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES
        (contact_id, NOW() - INTERVAL '20 days', type_call, actual_curator1, res_neu, 'Информационный звонок', NULL, NOW() - INTERVAL '10 days', true, NOW() - INTERVAL '20 days', NOW() - INTERVAL '20 days', actual_curator1),
        (contact_id, NOW() - INTERVAL '10 days', type_email, actual_curator1, res_pos, 'Отправлено коммерческое предложение', NULL, NOW() + INTERVAL '5 days', true, NOW() - INTERVAL '10 days', NOW() - INTERVAL '10 days', actual_curator1);
    END IF;

    -- Interactions for OP-001
    contact_id := (SELECT "Id" FROM contacts WHERE "ContactId" = 'OP-001');
    IF contact_id IS NOT NULL THEN
        INSERT INTO interactions ("ContactId", "InteractionDate", "InteractionTypeId", "CuratorId", "ResultId", "CommentEncrypted", "StatusChangeJson", "NextTouchDate", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES
        (contact_id, NOW() - INTERVAL '14 days', type_meet, actual_curator2, res_pos, 'Рабочая встреча по текущим вопросам', NULL, NOW() - INTERVAL '3 days', true, NOW() - INTERVAL '14 days', NOW() - INTERVAL '14 days', actual_curator2),
        (contact_id, NOW() - INTERVAL '3 days', type_call, actual_curator2, res_pos, 'Согласование позиции по регуляторным вопросам', NULL, NOW() + INTERVAL '14 days', true, NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days', actual_curator2);
    END IF;

    -- Interactions for OP-003
    contact_id := (SELECT "Id" FROM contacts WHERE "ContactId" = 'OP-003');
    IF contact_id IS NOT NULL THEN
        INSERT INTO interactions ("ContactId", "InteractionDate", "InteractionTypeId", "CuratorId", "ResultId", "CommentEncrypted", "StatusChangeJson", "NextTouchDate", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES
        (contact_id, NOW() - INTERVAL '30 days', type_event, actual_curator3, res_pos, 'Совместное участие в конференции', '{"from": "B", "to": "A"}', NOW() - INTERVAL '14 days', true, NOW() - INTERVAL '30 days', NOW() - INTERVAL '30 days', actual_curator3),
        (contact_id, NOW() - INTERVAL '14 days', type_meet, actual_curator3, res_pos, 'Обсуждение медийной стратегии', NULL, NOW() + INTERVAL '3 days', true, NOW() - INTERVAL '14 days', NOW() - INTERVAL '14 days', actual_curator3);
    END IF;

    -- Interactions for STR-001
    contact_id := (SELECT "Id" FROM contacts WHERE "ContactId" = 'STR-001');
    IF contact_id IS NOT NULL THEN
        INSERT INTO interactions ("ContactId", "InteractionDate", "InteractionTypeId", "CuratorId", "ResultId", "CommentEncrypted", "StatusChangeJson", "NextTouchDate", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES
        (contact_id, NOW() - INTERVAL '7 days', type_meet, actual_curator1, res_pos, 'Стратегическая встреча на высшем уровне', NULL, NOW() - INTERVAL '2 days', true, NOW() - INTERVAL '7 days', NOW() - INTERVAL '7 days', actual_curator1),
        (contact_id, NOW() - INTERVAL '2 days', type_call, actual_curator1, res_pos, 'Подтверждение поддержки инициативы', NULL, NOW() + INTERVAL '21 days', true, NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days', actual_curator1);
    END IF;

    -- Interactions for REG-001
    contact_id := (SELECT "Id" FROM contacts WHERE "ContactId" = 'REG-001');
    IF contact_id IS NOT NULL THEN
        INSERT INTO interactions ("ContactId", "InteractionDate", "InteractionTypeId", "CuratorId", "ResultId", "CommentEncrypted", "StatusChangeJson", "NextTouchDate", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES
        (contact_id, NOW() - INTERVAL '21 days', type_meet, actual_curator3, res_pos, 'Официальная встреча с губернатором', '{"from": "B", "to": "A"}', NOW() - INTERVAL '7 days', true, NOW() - INTERVAL '21 days', NOW() - INTERVAL '21 days', actual_curator3),
        (contact_id, NOW() - INTERVAL '7 days', type_call, actual_curator3, res_neu, 'Промежуточный звонок для поддержания контакта', NULL, NOW() + INTERVAL '30 days', true, NOW() - INTERVAL '7 days', NOW() - INTERVAL '7 days', actual_curator3);
    END IF;

    -- Some negative/pending interactions for variety
    contact_id := (SELECT "Id" FROM contacts WHERE "ContactId" = 'TEST-003');
    IF contact_id IS NOT NULL THEN
        INSERT INTO interactions ("ContactId", "InteractionDate", "InteractionTypeId", "CuratorId", "ResultId", "CommentEncrypted", "StatusChangeJson", "NextTouchDate", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES
        (contact_id, NOW() - INTERVAL '30 days', type_call, actual_curator2, res_neg, 'Контакт был недоступен, требуется повторная попытка', NULL, NOW() - INTERVAL '5 days', true, NOW() - INTERVAL '30 days', NOW() - INTERVAL '30 days', actual_curator2);
    END IF;

    contact_id := (SELECT "Id" FROM contacts WHERE "ContactId" = 'OP-004');
    IF contact_id IS NOT NULL THEN
        INSERT INTO interactions ("ContactId", "InteractionDate", "InteractionTypeId", "CuratorId", "ResultId", "CommentEncrypted", "StatusChangeJson", "NextTouchDate", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
        VALUES
        (contact_id, NOW() - INTERVAL '45 days', type_email, actual_curator3, res_pend, 'Отправлен запрос на встречу, ожидается ответ', NULL, NOW() - INTERVAL '15 days', true, NOW() - INTERVAL '45 days', NOW() - INTERVAL '45 days', actual_curator3);
    END IF;
END $$;

-- ============================================
-- 7. INFLUENCE STATUS HISTORY (История изменения статуса)
-- ============================================

DO $$
DECLARE
    curator1_id INT := (SELECT "Id" FROM users WHERE "Login" = 'curator1');
    curator2_id INT := (SELECT "Id" FROM users WHERE "Login" = 'curator2');
    curator3_id INT := (SELECT "Id" FROM users WHERE "Login" = 'curator3');
    admin_id INT := (SELECT "Id" FROM users WHERE "Login" = 'admin');
    contact_id INT;
    actual_curator1 INT;
    actual_curator3 INT;
BEGIN
    actual_curator1 := COALESCE(curator1_id, admin_id);
    actual_curator3 := COALESCE(curator3_id, admin_id);

    -- History for TEST-001 (C -> B -> A)
    contact_id := (SELECT "Id" FROM contacts WHERE "ContactId" = 'TEST-001');
    IF contact_id IS NOT NULL THEN
        INSERT INTO influence_status_history ("ContactId", "PreviousStatus", "NewStatus", "ChangedAt", "ChangedByUserId")
        VALUES
        (contact_id, 'C', 'B', NOW() - INTERVAL '30 days', actual_curator1),
        (contact_id, 'B', 'A', NOW() - INTERVAL '15 days', actual_curator1);
    END IF;

    -- History for OP-003 (B -> A)
    contact_id := (SELECT "Id" FROM contacts WHERE "ContactId" = 'OP-003');
    IF contact_id IS NOT NULL THEN
        INSERT INTO influence_status_history ("ContactId", "PreviousStatus", "NewStatus", "ChangedAt", "ChangedByUserId")
        VALUES
        (contact_id, 'B', 'A', NOW() - INTERVAL '30 days', actual_curator3);
    END IF;

    -- History for STR-001 (B -> A)
    contact_id := (SELECT "Id" FROM contacts WHERE "ContactId" = 'STR-001');
    IF contact_id IS NOT NULL THEN
        INSERT INTO influence_status_history ("ContactId", "PreviousStatus", "NewStatus", "ChangedAt", "ChangedByUserId")
        VALUES
        (contact_id, 'B', 'A', NOW() - INTERVAL '7 days', actual_curator1);
    END IF;

    -- History for REG-001 (B -> A)
    contact_id := (SELECT "Id" FROM contacts WHERE "ContactId" = 'REG-001');
    IF contact_id IS NOT NULL THEN
        INSERT INTO influence_status_history ("ContactId", "PreviousStatus", "NewStatus", "ChangedAt", "ChangedByUserId")
        VALUES
        (contact_id, 'B', 'A', NOW() - INTERVAL '21 days', actual_curator3);
    END IF;
END $$;

-- ============================================
-- 8. WATCHLIST (Список наблюдения)
-- ============================================

DO $$
DECLARE
    admin_id INT := (SELECT "Id" FROM users WHERE "Login" = 'admin');
    analyst1_id INT := (SELECT "Id" FROM users WHERE "Login" = 'analyst1');
    analyst2_id INT := (SELECT "Id" FROM users WHERE "Login" = 'analyst2');
    risk_legal INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'RiskSphere' AND "Code" = 'LEGAL');
    risk_reput INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'RiskSphere' AND "Code" = 'REPUT');
    risk_fin INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'RiskSphere' AND "Code" = 'FIN');
    risk_comp INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'RiskSphere' AND "Code" = 'COMP');
    risk_reg INT := (SELECT "Id" FROM reference_values WHERE "Category" = 'RiskSphere' AND "Code" = 'REG');
    actual_analyst1 INT;
    actual_analyst2 INT;
BEGIN
    actual_analyst1 := COALESCE(analyst1_id, admin_id);
    actual_analyst2 := COALESCE(analyst2_id, admin_id);

    INSERT INTO watchlist ("FullName", "RoleStatus", "RiskSphereId", "ThreatSource", "ConflictDate", "RiskLevel", "MonitoringFrequency", "LastCheckDate", "NextCheckDate", "DynamicsDescription", "WatchOwnerId", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
    VALUES
    ('Черняков Игорь Валентинович', 'Бывший партнёр', risk_legal, 'Судебные претензии по контракту', NOW() - INTERVAL '90 days', 'High', 'Weekly', NOW() - INTERVAL '5 days', NOW() + INTERVAL '2 days', 'Активное судебное разбирательство, требуется постоянный мониторинг', actual_analyst1, true, NOW(), NOW(), actual_analyst1),
    ('ООО "КонкурентПлюс"', 'Конкурент', risk_comp, 'Агрессивная рыночная стратегия', NOW() - INTERVAL '180 days', 'Critical', 'Weekly', NOW() - INTERVAL '3 days', NOW() + INTERVAL '4 days', 'Активно переманивает клиентов, усилить мониторинг', actual_analyst1, true, NOW(), NOW(), actual_analyst1),
    ('Журналист Смирнов П.А.', 'Представитель СМИ', risk_reput, 'Негативные публикации', NOW() - INTERVAL '60 days', 'Medium', 'Monthly', NOW() - INTERVAL '20 days', NOW() + INTERVAL '10 days', 'Публикует критические материалы, следить за активностью', actual_analyst2, true, NOW(), NOW(), actual_analyst2),
    ('Регулятор "Надзорорган"', 'Государственный орган', risk_reg, 'Внеплановая проверка', NOW() - INTERVAL '30 days', 'High', 'Weekly', NOW() - INTERVAL '7 days', NOW(), 'Проверка в процессе, критическая ситуация', actual_analyst1, true, NOW(), NOW(), actual_analyst1),
    ('Бывший сотрудник Власов К.', 'Бывший сотрудник', risk_legal, 'Трудовой спор', NOW() - INTERVAL '45 days', 'Low', 'Monthly', NOW() - INTERVAL '25 days', NOW() + INTERVAL '5 days', 'Мирное урегулирование вероятно', actual_analyst2, true, NOW(), NOW(), actual_analyst2),
    ('Инвестфонд "Альфа"', 'Потенциальный инвестор', risk_fin, 'Недружественное поглощение', NOW() - INTERVAL '120 days', 'Medium', 'Monthly', NOW() - INTERVAL '15 days', NOW() + INTERVAL '15 days', 'Мониторинг активности на рынке', actual_analyst1, true, NOW(), NOW(), actual_analyst1),
    ('Блогер @негативщик', 'Инфлюенсер', risk_reput, 'Негативные отзывы в соцсетях', NOW() - INTERVAL '14 days', 'Low', 'Monthly', NOW() - INTERVAL '10 days', NOW() + INTERVAL '20 days', 'Низкая аудитория, мониторинг профилактический', actual_analyst2, true, NOW(), NOW(), actual_analyst2);
END $$;

-- ============================================
-- 9. FAQ (Часто задаваемые вопросы)
-- ============================================

DO $$
DECLARE
    admin_id INT := (SELECT "Id" FROM users WHERE "Login" = 'admin');
BEGIN
    INSERT INTO faqs ("Title", "Content", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt", "UpdatedBy")
    VALUES
    ('Как создать новый контакт?', 'Для создания нового контакта:\n1. Перейдите в раздел "Контакты"\n2. Нажмите кнопку "Добавить контакт"\n3. Заполните обязательные поля: ФИО, блок, статус влияния\n4. Укажите дополнительную информацию\n5. Нажмите "Сохранить"', 1, true, NOW(), NOW(), admin_id),
    ('Что означают статусы влияния A/B/C/D?', 'Статусы влияния отражают степень сотрудничества:\n- A: Активное сотрудничество, высокое влияние\n- B: Умеренное сотрудничество, среднее влияние\n- C: Потенциальное сотрудничество, ограниченное влияние\n- D: Минимальный контакт, низкое влияние', 2, true, NOW(), NOW(), admin_id),
    ('Как добавить касание к контакту?', 'Существует два способа добавить касание:\n1. Из карточки контакта - кнопка "Добавить касание"\n2. Из раздела "Касания" - кнопка "Новое касание" с выбором контакта\n\nОбязательно укажите тип касания, результат и комментарий.', 3, true, NOW(), NOW(), admin_id),
    ('Как работает напоминание о следующем контакте?', 'При создании касания можно указать дату следующего контакта. Эта дата отображается:\n- В карточке контакта\n- В дашборде в разделе "Требуют внимания"\n- Просроченные контакты выделяются красным цветом', 4, true, NOW(), NOW(), admin_id),
    ('Кто имеет доступ к Watchlist?', 'Доступ к списку наблюдения (Watchlist) имеют:\n- Администраторы - полный доступ\n- Аналитики угроз - создание и редактирование записей\n\nКураторы НЕ имеют доступа к Watchlist.', 5, true, NOW(), NOW(), admin_id),
    ('Как архивировать блок?', 'Архивация блока доступна только администраторам:\n1. Перейдите в раздел "Блоки"\n2. Найдите нужный блок\n3. Нажмите "Редактировать"\n4. Измените статус на "Архивный"\n5. Сохраните изменения\n\nАрхивированные блоки не отображаются кураторам.', 6, true, NOW(), NOW(), admin_id);
END $$;

-- ============================================
-- 10. AUDIT LOGS (Журнал аудита)
-- ============================================

DO $$
DECLARE
    admin_id INT := (SELECT "Id" FROM users WHERE "Login" = 'admin');
    curator1_id INT := (SELECT "Id" FROM users WHERE "Login" = 'curator1');
    curator2_id INT := (SELECT "Id" FROM users WHERE "Login" = 'curator2');
    analyst1_id INT := (SELECT "Id" FROM users WHERE "Login" = 'analyst1');
BEGIN
    -- Only insert audit logs for admin (always exists)
    INSERT INTO audit_logs ("UserId", "Action", "EntityType", "EntityId", "OldValuesJson", "NewValuesJson", "Timestamp")
    VALUES
    (admin_id, 'Login', 'User', admin_id::varchar, NULL, '{"login": "admin"}', NOW() - INTERVAL '2 hours'),
    (admin_id, 'Create', 'Block', '2', NULL, '{"name": "Операционный блок", "code": "OP"}', NOW() - INTERVAL '1 hour 40 minutes'),
    (admin_id, 'Update', 'FAQ', '1', '{"sortOrder": 2}', '{"sortOrder": 1}', NOW() - INTERVAL '15 minutes');

    -- Conditional inserts for other users
    IF curator1_id IS NOT NULL THEN
        INSERT INTO audit_logs ("UserId", "Action", "EntityType", "EntityId", "OldValuesJson", "NewValuesJson", "Timestamp")
        VALUES
        (admin_id, 'Create', 'User', curator1_id::varchar, NULL, '{"login": "curator1", "role": "Curator"}', NOW() - INTERVAL '1 hour 50 minutes'),
        (curator1_id, 'Login', 'User', curator1_id::varchar, NULL, '{"login": "curator1"}', NOW() - INTERVAL '1 hour 30 minutes'),
        (curator1_id, 'Create', 'Contact', '1', NULL, '{"contactId": "TEST-001"}', NOW() - INTERVAL '1 hour 20 minutes'),
        (curator1_id, 'Create', 'Interaction', '1', NULL, '{"type": "Call", "result": "Positive"}', NOW() - INTERVAL '1 hour 10 minutes'),
        (curator1_id, 'Update', 'Contact', '1', '{"influenceStatus": "C"}', '{"influenceStatus": "B"}', NOW() - INTERVAL '1 hour');
    END IF;

    IF curator2_id IS NOT NULL THEN
        INSERT INTO audit_logs ("UserId", "Action", "EntityType", "EntityId", "OldValuesJson", "NewValuesJson", "Timestamp")
        VALUES
        (admin_id, 'Create', 'User', curator2_id::varchar, NULL, '{"login": "curator2", "role": "Curator"}', NOW() - INTERVAL '1 hour 45 minutes'),
        (curator2_id, 'Login', 'User', curator2_id::varchar, NULL, '{"login": "curator2"}', NOW() - INTERVAL '50 minutes'),
        (curator2_id, 'Create', 'Contact', '4', NULL, '{"contactId": "OP-001"}', NOW() - INTERVAL '45 minutes');
    END IF;

    IF analyst1_id IS NOT NULL THEN
        INSERT INTO audit_logs ("UserId", "Action", "EntityType", "EntityId", "OldValuesJson", "NewValuesJson", "Timestamp")
        VALUES
        (analyst1_id, 'Login', 'User', analyst1_id::varchar, NULL, '{"login": "analyst1"}', NOW() - INTERVAL '30 minutes'),
        (analyst1_id, 'Create', 'Watchlist', '1', NULL, '{"fullName": "Черняков И.В.", "riskLevel": "High"}', NOW() - INTERVAL '25 minutes');
    END IF;
END $$;

COMMIT;

-- Summary
SELECT 'Test data inserted successfully!' as status;
SELECT 'Users: ' || COUNT(*) FROM users;
SELECT 'Blocks: ' || COUNT(*) FROM blocks;
SELECT 'Reference Values: ' || COUNT(*) FROM reference_values;
SELECT 'Contacts: ' || COUNT(*) FROM contacts;
SELECT 'Interactions: ' || COUNT(*) FROM interactions;
SELECT 'Watchlist: ' || COUNT(*) FROM watchlist;
SELECT 'FAQs: ' || COUNT(*) FROM faqs;
SELECT 'Audit Logs: ' || COUNT(*) FROM audit_logs;
