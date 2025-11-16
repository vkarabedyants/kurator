-- Включение расширения для шифрования
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Создание типов (enums)
CREATE TYPE user_role AS ENUM ('Admin', 'Curator', 'BackupCurator', 'ThreatAnalyst');
CREATE TYPE block_status AS ENUM ('Active', 'Archived');
CREATE TYPE influence_status AS ENUM ('A', 'B', 'C', 'D');
CREATE TYPE influence_type AS ENUM ('Navigational', 'Interpretational', 'Functional', 'Reputational', 'Analytical');
CREATE TYPE risk_level AS ENUM ('Low', 'Medium', 'High', 'Critical');
CREATE TYPE monitoring_frequency AS ENUM ('Weekly', 'Monthly', 'Quarterly', 'AdHoc');
CREATE TYPE faq_visibility AS ENUM ('All', 'CuratorsOnly', 'AdminOnly');
CREATE TYPE audit_action_type AS ENUM ('CreateContact', 'UpdateContact', 'DeleteContact', 'CreateInteraction', 'UpdateInteraction', 'DeleteInteraction', 'ChangeInfluenceStatus', 'CreateBlock', 'UpdateBlock', 'CreateUser', 'UpdateUser', 'DeleteUser');
