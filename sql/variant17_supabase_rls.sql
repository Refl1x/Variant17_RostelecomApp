-- ===============================================================
-- Supabase RLS политики для варианта 17
-- Запускать в Supabase SQL Editor после schema и seed.
-- В обычном PostgreSQL этот файл не нужен, потому что использует auth.uid().
-- ===============================================================

CREATE SCHEMA IF NOT EXISTS app;

CREATE OR REPLACE FUNCTION app.current_profile_id()
RETURNS UUID
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public, app
AS $$
    SELECT profile_id
    FROM public.profiles
    WHERE auth_user_id = auth.uid()
      AND is_blocked = FALSE
    LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION app.has_role(p_role_code TEXT)
RETURNS BOOLEAN
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public, app
AS $$
    SELECT EXISTS (
        SELECT 1
        FROM public.profiles p
        JOIN public.roles r ON r.role_id = p.role_id
        WHERE p.auth_user_id = auth.uid()
          AND p.is_blocked = FALSE
          AND r.code = p_role_code
    );
$$;

CREATE OR REPLACE FUNCTION app.can_read_appeal(p_appeal_id UUID)
RETURNS BOOLEAN
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public, app
AS $$
    SELECT EXISTS (
        SELECT 1
        FROM public.appeals a
        WHERE a.appeal_id = p_appeal_id
          AND (
              app.has_role('admin') OR
              app.has_role('operator') OR
              a.assigned_specialist_id = app.current_profile_id()
          )
    );
$$;

-- Включаем RLS
ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE appeals ENABLE ROW LEVEL SECURITY;
ALTER TABLE appeal_comments ENABLE ROW LEVEL SECURITY;
ALTER TABLE appeal_flags ENABLE ROW LEVEL SECURITY;
ALTER TABLE appeal_status_history ENABLE ROW LEVEL SECURITY;
ALTER TABLE notifications ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE sync_queue ENABLE ROW LEVEL SECURITY;

-- Справочники доступны всем авторизованным пользователям на чтение
ALTER TABLE roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE appeal_types ENABLE ROW LEVEL SECURITY;
ALTER TABLE appeal_statuses ENABLE ROW LEVEL SECURITY;
ALTER TABLE priorities ENABLE ROW LEVEL SECURITY;
ALTER TABLE comment_visibility ENABLE ROW LEVEL SECURITY;
ALTER TABLE internal_flags ENABLE ROW LEVEL SECURITY;
ALTER TABLE log_levels ENABLE ROW LEVEL SECURITY;
ALTER TABLE action_types ENABLE ROW LEVEL SECURITY;

CREATE POLICY read_roles ON roles FOR SELECT TO authenticated USING (TRUE);
CREATE POLICY read_appeal_types ON appeal_types FOR SELECT TO authenticated USING (TRUE);
CREATE POLICY read_appeal_statuses ON appeal_statuses FOR SELECT TO authenticated USING (TRUE);
CREATE POLICY read_priorities ON priorities FOR SELECT TO authenticated USING (TRUE);
CREATE POLICY read_comment_visibility ON comment_visibility FOR SELECT TO authenticated USING (TRUE);
CREATE POLICY read_log_levels ON log_levels FOR SELECT TO authenticated USING (TRUE);
CREATE POLICY read_action_types ON action_types FOR SELECT TO authenticated USING (TRUE);
CREATE POLICY read_internal_flags_operator_only ON internal_flags
FOR SELECT TO authenticated USING (app.has_role('admin') OR app.has_role('operator'));

-- Профили
CREATE POLICY profiles_read_by_role ON profiles
FOR SELECT TO authenticated
USING (
    auth_user_id = auth.uid()
    OR app.has_role('admin')
    OR app.has_role('operator')
    OR app.has_role('specialist')
);

CREATE POLICY profiles_admin_insert ON profiles
FOR INSERT TO authenticated
WITH CHECK (app.has_role('admin'));

CREATE POLICY profiles_admin_update ON profiles
FOR UPDATE TO authenticated
USING (app.has_role('admin'))
WITH CHECK (app.has_role('admin'));

-- Обращения
CREATE POLICY appeals_read_by_role ON appeals
FOR SELECT TO authenticated
USING (
    app.has_role('admin') OR
    app.has_role('operator') OR
    assigned_specialist_id = app.current_profile_id()
);

CREATE POLICY appeals_operator_insert ON appeals
FOR INSERT TO authenticated
WITH CHECK (app.has_role('admin') OR app.has_role('operator'));

CREATE POLICY appeals_operator_update_all ON appeals
FOR UPDATE TO authenticated
USING (app.has_role('admin') OR app.has_role('operator'))
WITH CHECK (app.has_role('admin') OR app.has_role('operator'));

CREATE POLICY appeals_specialist_update_assigned ON appeals
FOR UPDATE TO authenticated
USING (assigned_specialist_id = app.current_profile_id())
WITH CHECK (assigned_specialist_id = app.current_profile_id());

-- Удаление лучше делать мягко через deleted_at. Полное удаление только администратору.
CREATE POLICY appeals_admin_delete ON appeals
FOR DELETE TO authenticated
USING (app.has_role('admin'));

-- Комментарии: внутренние видит только оператор/админ, внешние видят назначенные специалисты.
CREATE POLICY comments_read_by_visibility ON appeal_comments
FOR SELECT TO authenticated
USING (
    app.has_role('admin') OR
    app.has_role('operator') OR
    (
        app.can_read_appeal(appeal_id) AND visibility_id IN (
            SELECT visibility_id FROM comment_visibility WHERE visible_for_mobile = TRUE
        )
    )
);

CREATE POLICY comments_insert_by_role ON appeal_comments
FOR INSERT TO authenticated
WITH CHECK (
    app.has_role('admin') OR
    app.has_role('operator') OR
    (
        author_id = app.current_profile_id()
        AND app.can_read_appeal(appeal_id)
        AND visibility_id IN (SELECT visibility_id FROM comment_visibility WHERE code = 'external')
    )
);

CREATE POLICY comments_update_author_or_operator ON appeal_comments
FOR UPDATE TO authenticated
USING (app.has_role('admin') OR app.has_role('operator') OR author_id = app.current_profile_id())
WITH CHECK (app.has_role('admin') OR app.has_role('operator') OR author_id = app.current_profile_id());

-- Скрытые метки видит и меняет только оператор/админ
CREATE POLICY flags_operator_all ON appeal_flags
FOR ALL TO authenticated
USING (app.has_role('admin') OR app.has_role('operator'))
WITH CHECK (app.has_role('admin') OR app.has_role('operator'));

-- История статусов доступна тем, кто видит обращение
CREATE POLICY status_history_read ON appeal_status_history
FOR SELECT TO authenticated
USING (app.can_read_appeal(appeal_id));

CREATE POLICY status_history_insert_system ON appeal_status_history
FOR INSERT TO authenticated
WITH CHECK (app.can_read_appeal(appeal_id));

-- Уведомления пользователь читает только свои
CREATE POLICY notifications_read_own ON notifications
FOR SELECT TO authenticated
USING (recipient_id = app.current_profile_id() OR app.has_role('admin'));

CREATE POLICY notifications_update_own ON notifications
FOR UPDATE TO authenticated
USING (recipient_id = app.current_profile_id())
WITH CHECK (recipient_id = app.current_profile_id());

-- Аудит: читать только администратору, добавлять могут авторизованные клиенты
CREATE POLICY audit_admin_read ON audit_logs
FOR SELECT TO authenticated
USING (app.has_role('admin'));

CREATE POLICY audit_insert_authenticated ON audit_logs
FOR INSERT TO authenticated
WITH CHECK (profile_id = app.current_profile_id() OR app.has_role('admin'));

-- Очередь синхронизации: пользователь видит свои записи, админ видит все
CREATE POLICY sync_queue_own ON sync_queue
FOR ALL TO authenticated
USING (profile_id = app.current_profile_id() OR app.has_role('admin'))
WITH CHECK (profile_id = app.current_profile_id() OR app.has_role('admin'));
