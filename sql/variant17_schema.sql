-- ===============================================================
-- Вариант 17: БД для системы учета обращений граждан и клиентов
-- СУБД: PostgreSQL / Supabase
-- Нормализация: 3НФ
-- ===============================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;

-- -------------------------
-- 1. Справочники и роли
-- -------------------------
CREATE TABLE roles (
    role_id SMALLSERIAL PRIMARY KEY,
    code VARCHAR(30) NOT NULL UNIQUE,
    name VARCHAR(80) NOT NULL UNIQUE
);

CREATE TABLE appeal_types (
    type_id SMALLSERIAL PRIMARY KEY,
    code VARCHAR(30) NOT NULL UNIQUE,
    name VARCHAR(60) NOT NULL UNIQUE
);

CREATE TABLE appeal_statuses (
    status_id SMALLSERIAL PRIMARY KEY,
    code VARCHAR(40) NOT NULL UNIQUE,
    name VARCHAR(80) NOT NULL UNIQUE,
    is_final BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE priorities (
    priority_id SMALLSERIAL PRIMARY KEY,
    code VARCHAR(30) NOT NULL UNIQUE,
    name VARCHAR(60) NOT NULL UNIQUE,
    sort_order SMALLINT NOT NULL UNIQUE
);

CREATE TABLE comment_visibility (
    visibility_id SMALLSERIAL PRIMARY KEY,
    code VARCHAR(30) NOT NULL UNIQUE,
    name VARCHAR(80) NOT NULL UNIQUE,
    visible_for_mobile BOOLEAN NOT NULL DEFAULT TRUE,
    max_length SMALLINT NOT NULL CHECK (max_length BETWEEN 1 AND 500)
);

CREATE TABLE internal_flags (
    flag_id SMALLSERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(120) NOT NULL UNIQUE,
    visible_for_mobile BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE log_levels (
    level_id SMALLSERIAL PRIMARY KEY,
    code VARCHAR(20) NOT NULL UNIQUE,
    name VARCHAR(40) NOT NULL UNIQUE
);

CREATE TABLE action_types (
    action_type_id SMALLSERIAL PRIMARY KEY,
    code VARCHAR(40) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL UNIQUE
);

-- -------------------------
-- 2. Пользователи и сессии
-- -------------------------
CREATE TABLE profiles (
    profile_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    -- В Supabase сюда записывается auth.users.id. FK специально не ставится,
    -- чтобы схема оставалась переносимой и могла работать в обычном PostgreSQL.
    auth_user_id UUID UNIQUE,
    full_name VARCHAR(100) NOT NULL CHECK (length(trim(full_name)) > 0),
    email CITEXT UNIQUE,
    phone VARCHAR(12) CHECK (phone IS NULL OR phone ~ '^\+7[0-9]{10}$'),
    role_id SMALLINT NOT NULL REFERENCES roles(role_id),
    -- Для локальной десктопной версии можно хранить BCrypt/Argon2 hash.
    -- В Supabase лучше использовать Supabase Auth, а поле оставить NULL.
    password_hash TEXT,
    is_blocked BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    last_seen_at TIMESTAMPTZ
);

CREATE TABLE user_sessions (
    session_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    profile_id UUID NOT NULL REFERENCES profiles(profile_id) ON DELETE CASCADE,
    device_name VARCHAR(120),
    refresh_token_hash TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    last_activity_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ,
    CHECK (expires_at > created_at)
);

-- -------------------------
-- 3. Основная таблица обращений
-- -------------------------
CREATE SEQUENCE appeal_public_seq START 1;

CREATE TABLE appeals (
    appeal_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    public_number VARCHAR(20) NOT NULL UNIQUE,

    applicant_name VARCHAR(100) NOT NULL
        CHECK (length(trim(applicant_name)) > 0)
        CHECK (applicant_name ~ '^[A-Za-zА-Яа-яЁё0-9 .,"''№()/_:;!?+-]+$'),

    contact_phone VARCHAR(12) NOT NULL
        CHECK (contact_phone ~ '^\+7[0-9]{10}$'),

    connection_address VARCHAR(150) NOT NULL
        CHECK (length(trim(connection_address)) > 0)
        CHECK (connection_address ~ '^[A-Za-zА-Яа-яЁё0-9 .,"''№()/_:;!?+-]+$'),

    description VARCHAR(200) NOT NULL
        CHECK (length(trim(description)) > 0)
        CHECK (description ~ '^[A-Za-zА-Яа-яЁё0-9 .,"''№()/_:;!?+-]+$'),

    type_id SMALLINT NOT NULL REFERENCES appeal_types(type_id),
    status_id SMALLINT NOT NULL REFERENCES appeal_statuses(status_id),
    priority_id SMALLINT NOT NULL REFERENCES priorities(priority_id),

    registered_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    assigned_specialist_id UUID REFERENCES profiles(profile_id),
    created_by UUID REFERENCES profiles(profile_id),
    updated_by UUID REFERENCES profiles(profile_id),

    -- Для облачной синхронизации и LWW:
    version INTEGER NOT NULL DEFAULT 1 CHECK (version >= 1),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at TIMESTAMPTZ,
    last_synced_at TIMESTAMPTZ,

    CHECK (deleted_at IS NULL OR deleted_at >= registered_at)
);

-- -------------------------
-- 4. Комментарии, скрытые метки, история статусов
-- -------------------------
CREATE TABLE appeal_comments (
    comment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    appeal_id UUID NOT NULL REFERENCES appeals(appeal_id) ON DELETE CASCADE,
    author_id UUID NOT NULL REFERENCES profiles(profile_id),
    visibility_id SMALLINT NOT NULL REFERENCES comment_visibility(visibility_id),
    comment_text VARCHAR(400) NOT NULL CHECK (length(trim(comment_text)) > 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE appeal_flags (
    appeal_id UUID NOT NULL REFERENCES appeals(appeal_id) ON DELETE CASCADE,
    flag_id SMALLINT NOT NULL REFERENCES internal_flags(flag_id),
    created_by UUID REFERENCES profiles(profile_id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (appeal_id, flag_id)
);

CREATE TABLE appeal_status_history (
    status_history_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    appeal_id UUID NOT NULL REFERENCES appeals(appeal_id) ON DELETE CASCADE,
    old_status_id SMALLINT REFERENCES appeal_statuses(status_id),
    new_status_id SMALLINT NOT NULL REFERENCES appeal_statuses(status_id),
    changed_by UUID REFERENCES profiles(profile_id),
    changed_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    note VARCHAR(250)
);

-- -------------------------
-- 5. Аудит, уведомления и очередь синхронизации
-- -------------------------
CREATE TABLE audit_logs (
    audit_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    profile_id UUID REFERENCES profiles(profile_id),
    level_id SMALLINT NOT NULL REFERENCES log_levels(level_id),
    action_type_id SMALLINT NOT NULL REFERENCES action_types(action_type_id),
    entity_table VARCHAR(60) NOT NULL,
    entity_id UUID,
    details JSONB NOT NULL DEFAULT '{}'::jsonb,
    device_name VARCHAR(120),
    ip_address INET
);

CREATE TABLE notifications (
    notification_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    recipient_id UUID NOT NULL REFERENCES profiles(profile_id) ON DELETE CASCADE,
    appeal_id UUID REFERENCES appeals(appeal_id) ON DELETE CASCADE,
    title VARCHAR(100) NOT NULL,
    message VARCHAR(250) NOT NULL,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    read_at TIMESTAMPTZ
);

CREATE TABLE sync_queue (
    sync_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id VARCHAR(120) NOT NULL,
    profile_id UUID REFERENCES profiles(profile_id),
    entity_table VARCHAR(60) NOT NULL,
    entity_id UUID NOT NULL,
    operation VARCHAR(20) NOT NULL CHECK (operation IN ('INSERT', 'UPDATE', 'DELETE')),
    payload JSONB NOT NULL,
    client_updated_at TIMESTAMPTZ NOT NULL,
    server_received_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    synced_at TIMESTAMPTZ,
    retry_count INTEGER NOT NULL DEFAULT 0 CHECK (retry_count >= 0),
    status VARCHAR(20) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'synced', 'conflict', 'error')),
    last_error TEXT
);

-- -------------------------
-- 6. Триггеры
-- -------------------------
CREATE OR REPLACE FUNCTION generate_appeal_public_number()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.public_number IS NULL OR trim(NEW.public_number) = '' THEN
        NEW.public_number := 'ОБ-' || to_char(COALESCE(NEW.registered_at, now()), 'YYMMDD') || '-' ||
                             lpad(nextval('appeal_public_seq')::text, 3, '0');
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_generate_appeal_public_number
BEFORE INSERT ON appeals
FOR EACH ROW EXECUTE FUNCTION generate_appeal_public_number();

CREATE OR REPLACE FUNCTION set_updated_at_and_version()
RETURNS TRIGGER AS $$
BEGIN
    -- Если приложение явно передало client_updated_at для LWW, не затираем его.
    -- Если поле updated_at не меняли, ставим текущее серверное время.
    IF NEW.updated_at IS NOT DISTINCT FROM OLD.updated_at THEN
        NEW.updated_at := now();
    END IF;

    NEW.version := COALESCE(OLD.version, 0) + 1;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_appeals_updated
BEFORE UPDATE ON appeals
FOR EACH ROW EXECUTE FUNCTION set_updated_at_and_version();

CREATE OR REPLACE FUNCTION set_updated_at_only()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_profiles_updated
BEFORE UPDATE ON profiles
FOR EACH ROW EXECUTE FUNCTION set_updated_at_only();

CREATE TRIGGER trg_comments_updated
BEFORE UPDATE ON appeal_comments
FOR EACH ROW EXECUTE FUNCTION set_updated_at_only();

CREATE OR REPLACE FUNCTION check_comment_length_by_visibility()
RETURNS TRIGGER AS $$
DECLARE
    allowed_len SMALLINT;
BEGIN
    SELECT max_length INTO allowed_len
    FROM comment_visibility
    WHERE visibility_id = NEW.visibility_id;

    IF length(NEW.comment_text) > allowed_len THEN
        RAISE EXCEPTION 'Комментарий превышает допустимую длину: % символов', allowed_len;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_check_comment_length
BEFORE INSERT OR UPDATE ON appeal_comments
FOR EACH ROW EXECUTE FUNCTION check_comment_length_by_visibility();

CREATE OR REPLACE FUNCTION write_status_history()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO appeal_status_history(appeal_id, old_status_id, new_status_id, changed_by)
        VALUES (NEW.appeal_id, NULL, NEW.status_id, NEW.created_by);
    ELSIF OLD.status_id IS DISTINCT FROM NEW.status_id THEN
        INSERT INTO appeal_status_history(appeal_id, old_status_id, new_status_id, changed_by)
        VALUES (NEW.appeal_id, OLD.status_id, NEW.status_id, NEW.updated_by);
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_status_history_insert
AFTER INSERT ON appeals
FOR EACH ROW EXECUTE FUNCTION write_status_history();

CREATE TRIGGER trg_status_history_update
AFTER UPDATE OF status_id ON appeals
FOR EACH ROW EXECUTE FUNCTION write_status_history();

CREATE OR REPLACE FUNCTION create_priority_notification()
RETURNS TRIGGER AS $$
DECLARE
    priority_code TEXT;
    type_code TEXT;
BEGIN
    SELECT code INTO priority_code FROM priorities WHERE priority_id = NEW.priority_id;
    SELECT code INTO type_code FROM appeal_types WHERE type_id = NEW.type_id;

    IF NEW.assigned_specialist_id IS NOT NULL AND (priority_code = 'high' OR type_code = 'complaint') THEN
        INSERT INTO notifications(recipient_id, appeal_id, title, message)
        VALUES (
            NEW.assigned_specialist_id,
            NEW.appeal_id,
            'Новое срочное обращение',
            'Поступило обращение ' || NEW.public_number || ' с высоким приоритетом или типом "Жалоба"'
        );
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_priority_notification
AFTER INSERT OR UPDATE OF priority_id, type_id, assigned_specialist_id ON appeals
FOR EACH ROW EXECUTE FUNCTION create_priority_notification();

-- -------------------------
-- 7. Функция LWW для синхронизации обращений
-- -------------------------
CREATE OR REPLACE FUNCTION upsert_appeal_lww(
    p_appeal_id UUID,
    p_public_number VARCHAR,
    p_applicant_name VARCHAR,
    p_contact_phone VARCHAR,
    p_connection_address VARCHAR,
    p_description VARCHAR,
    p_type_id SMALLINT,
    p_status_id SMALLINT,
    p_priority_id SMALLINT,
    p_registered_at TIMESTAMPTZ,
    p_assigned_specialist_id UUID,
    p_actor_id UUID,
    p_client_updated_at TIMESTAMPTZ
)
RETURNS UUID AS $$
DECLARE
    current_updated_at TIMESTAMPTZ;
BEGIN
    SELECT updated_at INTO current_updated_at
    FROM appeals
    WHERE appeal_id = p_appeal_id;

    IF current_updated_at IS NULL THEN
        INSERT INTO appeals(
            appeal_id, public_number, applicant_name, contact_phone, connection_address,
            description, type_id, status_id, priority_id, registered_at,
            assigned_specialist_id, created_by, updated_by, updated_at, last_synced_at
        )
        VALUES (
            p_appeal_id, p_public_number, p_applicant_name, p_contact_phone, p_connection_address,
            p_description, p_type_id, p_status_id, p_priority_id, COALESCE(p_registered_at, now()),
            p_assigned_specialist_id, p_actor_id, p_actor_id, p_client_updated_at, now()
        );
    ELSIF p_client_updated_at >= current_updated_at THEN
        UPDATE appeals
        SET applicant_name = p_applicant_name,
            contact_phone = p_contact_phone,
            connection_address = p_connection_address,
            description = p_description,
            type_id = p_type_id,
            status_id = p_status_id,
            priority_id = p_priority_id,
            assigned_specialist_id = p_assigned_specialist_id,
            updated_by = p_actor_id,
            updated_at = p_client_updated_at,
            last_synced_at = now()
        WHERE appeal_id = p_appeal_id;
    END IF;

    RETURN p_appeal_id;
END;
$$ LANGUAGE plpgsql;

-- -------------------------
-- 8. Представления для экспорта, мобильного приложения и аналитики
-- -------------------------
CREATE OR REPLACE VIEW v_appeals_export AS
SELECT
    a.public_number AS identifier,
    a.applicant_name,
    a.contact_phone,
    a.connection_address,
    a.description,
    t.name AS appeal_type,
    s.name AS status,
    p.name AS priority,
    a.registered_at,
    sp.full_name AS assigned_specialist,
    COALESCE((
        SELECT string_agg(c.comment_text, '; ' ORDER BY c.created_at)
        FROM appeal_comments c
        JOIN comment_visibility cv ON cv.visibility_id = c.visibility_id
        WHERE c.appeal_id = a.appeal_id AND cv.code = 'external' AND c.deleted_at IS NULL
    ), '') AS external_comments,
    COALESCE((
        SELECT string_agg(c.comment_text, '; ' ORDER BY c.created_at)
        FROM appeal_comments c
        JOIN comment_visibility cv ON cv.visibility_id = c.visibility_id
        WHERE c.appeal_id = a.appeal_id AND cv.code = 'internal' AND c.deleted_at IS NULL
    ), '') AS internal_comments
FROM appeals a
JOIN appeal_types t ON t.type_id = a.type_id
JOIN appeal_statuses s ON s.status_id = a.status_id
JOIN priorities p ON p.priority_id = a.priority_id
LEFT JOIN profiles sp ON sp.profile_id = a.assigned_specialist_id
WHERE a.deleted_at IS NULL;

CREATE OR REPLACE VIEW v_mobile_appeals AS
SELECT
    a.appeal_id,
    a.public_number,
    a.applicant_name,
    a.contact_phone,
    a.connection_address,
    a.description,
    t.name AS appeal_type,
    s.name AS status,
    p.name AS priority,
    a.registered_at,
    a.assigned_specialist_id,
    a.updated_at,
    COALESCE((
        SELECT string_agg(c.comment_text, '; ' ORDER BY c.created_at)
        FROM appeal_comments c
        JOIN comment_visibility cv ON cv.visibility_id = c.visibility_id
        WHERE c.appeal_id = a.appeal_id AND cv.visible_for_mobile = TRUE AND c.deleted_at IS NULL
    ), '') AS visible_comments
FROM appeals a
JOIN appeal_types t ON t.type_id = a.type_id
JOIN appeal_statuses s ON s.status_id = a.status_id
JOIN priorities p ON p.priority_id = a.priority_id
WHERE a.deleted_at IS NULL;

CREATE OR REPLACE VIEW v_appeal_stats AS
SELECT
    date_trunc('day', a.registered_at)::date AS period_day,
    t.name AS appeal_type,
    s.name AS status,
    p.name AS priority,
    count(*) AS total_count
FROM appeals a
JOIN appeal_types t ON t.type_id = a.type_id
JOIN appeal_statuses s ON s.status_id = a.status_id
JOIN priorities p ON p.priority_id = a.priority_id
WHERE a.deleted_at IS NULL
GROUP BY date_trunc('day', a.registered_at)::date, t.name, s.name, p.name;

CREATE OR REPLACE VIEW v_specialist_stats AS
SELECT
    sp.profile_id AS specialist_id,
    sp.full_name AS specialist_name,
    count(*) FILTER (WHERE s.code IN ('resolved', 'rejected')) AS reviewed_count,
    count(*) FILTER (WHERE s.code = 'resolved') AS resolved_count,
    count(*) FILTER (WHERE s.code = 'rejected') AS rejected_count,
    count(*) AS assigned_total
FROM profiles sp
JOIN roles r ON r.role_id = sp.role_id AND r.code = 'specialist'
LEFT JOIN appeals a ON a.assigned_specialist_id = sp.profile_id AND a.deleted_at IS NULL
LEFT JOIN appeal_statuses s ON s.status_id = a.status_id
GROUP BY sp.profile_id, sp.full_name;

-- -------------------------
-- 9. Индексы для фильтрации, поиска и синхронизации
-- -------------------------
CREATE INDEX idx_appeals_type_status_priority ON appeals(type_id, status_id, priority_id);
CREATE INDEX idx_appeals_assigned_specialist ON appeals(assigned_specialist_id);
CREATE INDEX idx_appeals_registered_at ON appeals(registered_at DESC);
CREATE INDEX idx_appeals_updated_at ON appeals(updated_at DESC);
CREATE INDEX idx_comments_appeal ON appeal_comments(appeal_id, created_at DESC);
CREATE INDEX idx_status_history_appeal ON appeal_status_history(appeal_id, changed_at DESC);
CREATE INDEX idx_notifications_recipient ON notifications(recipient_id, is_read, created_at DESC);
CREATE INDEX idx_sync_queue_status ON sync_queue(status, server_received_at);
CREATE INDEX idx_audit_logs_entity ON audit_logs(entity_table, entity_id, occurred_at DESC);
CREATE INDEX idx_appeals_fulltext_search ON appeals USING GIN (
    to_tsvector('simple', applicant_name || ' ' || connection_address || ' ' || description)
);
