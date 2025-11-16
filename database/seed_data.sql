-- ===================================
-- Начальные данные для справочников
-- ===================================

-- Статусы влияния
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('influence_status', 'A', 'Статус A', 'Высокий уровень влияния', 1, true, NOW(), NOW()),
('influence_status', 'B', 'Статус B', 'Средний уровень влияния', 2, true, NOW(), NOW()),
('influence_status', 'C', 'Статус C', 'Низкий уровень влияния', 3, true, NOW(), NOW()),
('influence_status', 'D', 'Статус D', 'Минимальный уровень влияния', 4, true, NOW(), NOW());

-- Типы влияния
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('influence_type', 'NAV', 'Навигационное', 'Обеспечивает доступ, вводит в круг', 1, true, NOW(), NOW()),
('influence_type', 'INT', 'Интерпретационное', 'Помогает понять позиции и контекст', 2, true, NOW(), NOW()),
('influence_type', 'FUN', 'Функциональное', 'Помогает решать вопросы, воздействует на процессы', 3, true, NOW(), NOW()),
('influence_type', 'REP', 'Репутационное', 'Влияет на публичное восприятие', 4, true, NOW(), NOW()),
('influence_type', 'ANA', 'Аналитическое', 'Даёт стратегическую оценку, прогноз', 5, true, NOW(), NOW());

-- Каналы коммуникации
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('communication_channel', 'OFF', 'Официальный', NULL, 1, true, NOW(), NOW()),
('communication_channel', 'MED', 'Через посредника', NULL, 2, true, NOW(), NOW()),
('communication_channel', 'ASS', 'Через ассоциацию', NULL, 3, true, NOW(), NOW()),
('communication_channel', 'PER', 'Личный', NULL, 4, true, NOW(), NOW()),
('communication_channel', 'JUR', 'Юридический', NULL, 5, true, NOW(), NOW());

-- Источники контактов
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('contact_source', 'PER', 'Личное знакомство', NULL, 1, true, NOW(), NOW()),
('contact_source', 'ASS', 'Ассоциация', NULL, 2, true, NOW(), NOW()),
('contact_source', 'REC', 'Рекомендация', NULL, 3, true, NOW(), NOW()),
('contact_source', 'EVE', 'Ивент', NULL, 4, true, NOW(), NOW()),
('contact_source', 'MED', 'Медиа', NULL, 5, true, NOW(), NOW()),
('contact_source', 'OTH', 'Другое', NULL, 6, true, NOW(), NOW());

-- Типы касаний
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('interaction_type', 'MEE', 'Встреча', NULL, 1, true, NOW(), NOW()),
('interaction_type', 'CAL', 'Звонок', NULL, 2, true, NOW(), NOW()),
('interaction_type', 'MSG', 'Переписка', NULL, 3, true, NOW(), NOW()),
('interaction_type', 'EVE', 'Ивент', NULL, 4, true, NOW(), NOW()),
('interaction_type', 'OTH', 'Прочее', NULL, 5, true, NOW(), NOW());

-- Результаты касаний
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('interaction_result', 'POS', 'Позитивный', NULL, 1, true, NOW(), NOW()),
('interaction_result', 'NEU', 'Нейтральный', NULL, 2, true, NOW(), NOW()),
('interaction_result', 'NEG', 'Негативный', NULL, 3, true, NOW(), NOW()),
('interaction_result', 'DEL', 'Отложено', NULL, 4, true, NOW(), NOW()),
('interaction_result', 'NON', 'Без результата', NULL, 5, true, NOW(), NOW());

-- Сферы риска
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('risk_sphere', 'MED', 'Медиа', NULL, 1, true, NOW(), NOW()),
('risk_sphere', 'JUR', 'Юридическое давление', NULL, 2, true, NOW(), NOW()),
('risk_sphere', 'POL', 'Политическое', NULL, 3, true, NOW(), NOW()),
('risk_sphere', 'ECO', 'Экономическое', NULL, 4, true, NOW(), NOW()),
('risk_sphere', 'FOR', 'Силовое', NULL, 5, true, NOW(), NOW()),
('risk_sphere', 'COM', 'Коммуникации', NULL, 6, true, NOW(), NOW()),
('risk_sphere', 'OTH', 'Другое', NULL, 7, true, NOW(), NOW());

-- Организации (примеры)
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('organization', 'VR', 'Верховная Рада', NULL, 1, true, NOW(), NOW()),
('organization', 'KMU', 'Кабинет Министров', NULL, 2, true, NOW(), NOW()),
('organization', 'NBU', 'НБУ', NULL, 3, true, NOW(), NOW()),
('organization', 'SBU', 'СБУ', NULL, 4, true, NOW(), NOW()),
('organization', 'MED', 'Медиа', NULL, 5, true, NOW(), NOW()),
('organization', 'OTH', 'Другое', NULL, 99, true, NOW(), NOW());

-- Разрешенные расширения файлов
INSERT INTO reference_values ("Category", "Code", "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('file_extensions', 'PDF', 'PDF', 'Adobe PDF документы', 1, true, NOW(), NOW()),
('file_extensions', 'DOC', 'DOC', 'Microsoft Word', 2, true, NOW(), NOW()),
('file_extensions', 'DOCX', 'DOCX', 'Microsoft Word (новый формат)', 3, true, NOW(), NOW()),
('file_extensions', 'XLS', 'XLS', 'Microsoft Excel', 4, true, NOW(), NOW()),
('file_extensions', 'XLSX', 'XLSX', 'Microsoft Excel (новый формат)', 5, true, NOW(), NOW()),
('file_extensions', 'JPG', 'JPG', 'JPEG изображения', 6, true, NOW(), NOW()),
('file_extensions', 'JPEG', 'JPEG', 'JPEG изображения', 7, true, NOW(), NOW()),
('file_extensions', 'PNG', 'PNG', 'PNG изображения', 8, true, NOW(), NOW()),
('file_extensions', 'GIF', 'GIF', 'GIF изображения', 9, true, NOW(), NOW()),
('file_extensions', 'TXT', 'TXT', 'Текстовые файлы', 10, true, NOW(), NOW());

-- Создание администратора по умолчанию
-- Пароль: Admin123!
INSERT INTO users ("Login", "PasswordHash", "Role", "IsFirstLogin", "IsActive", "MfaEnabled", "CreatedAt")
VALUES ('admin', '$2a$11$vVZl5M3qK5YJ5X5K5X5X5.5X5X5X5X5X5X5X5X5X5X5X5X5X5X5X5Xe', 'Admin', true, true, false, NOW());

SELECT 'Справочники успешно заполнены!' as message,
       COUNT(*) as total_values
FROM reference_values;
