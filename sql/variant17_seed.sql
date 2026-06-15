-- ===============================================================
-- Начальные данные для варианта 17
-- Запускать после variant17_schema.sql
-- ===============================================================

INSERT INTO roles(code, name) VALUES
('admin', 'Администратор'),
('operator', 'Оператор'),
('specialist', 'Специалист отдела клиентского сервиса')
ON CONFLICT (code) DO NOTHING;

INSERT INTO appeal_types(code, name) VALUES
('complaint', 'Жалоба'),
('suggestion', 'Предложение'),
('application', 'Заявление'),
('consultation', 'Консультация'),
('thanks', 'Благодарность')
ON CONFLICT (code) DO NOTHING;

INSERT INTO appeal_statuses(code, name, is_final) VALUES
('accepted', 'Принято', FALSE),
('reviewing', 'На рассмотрении', FALSE),
('in_work', 'Направлено в работу', FALSE),
('resolved', 'Решено', TRUE),
('rejected', 'Отклонено', TRUE)
ON CONFLICT (code) DO NOTHING;

INSERT INTO priorities(code, name, sort_order) VALUES
('low', 'Низкий', 1),
('medium', 'Средний', 2),
('high', 'Высокий', 3)
ON CONFLICT (code) DO NOTHING;

INSERT INTO comment_visibility(code, name, visible_for_mobile, max_length) VALUES
('internal', 'Внутренний комментарий', FALSE, 400),
('external', 'Внешний комментарий для специалиста', TRUE, 250)
ON CONFLICT (code) DO NOTHING;

INSERT INTO internal_flags(code, name, visible_for_mobile) VALUES
('legal_approval', 'Требует согласования с юридическим отделом', FALSE),
('manager_escalation', 'Требует эскалации руководителю', FALSE)
ON CONFLICT (code) DO NOTHING;

INSERT INTO log_levels(code, name) VALUES
('INFO', 'Информация'),
('WARNING', 'Предупреждение'),
('ERROR', 'Ошибка')
ON CONFLICT (code) DO NOTHING;

INSERT INTO action_types(code, name) VALUES
('LOGIN', 'Вход в систему'),
('CREATE_APPEAL', 'Добавление обращения'),
('UPDATE_APPEAL', 'Редактирование обращения'),
('DELETE_APPEAL', 'Удаление обращения'),
('CHANGE_STATUS', 'Изменение статуса'),
('ADD_COMMENT', 'Добавление комментария'),
('ASSIGN_SPECIALIST', 'Назначение специалиста'),
('EXPORT_REPORT', 'Экспорт отчета')
ON CONFLICT (code) DO NOTHING;

-- Демонстрационные пользователи. В реальном Supabase auth_user_id нужно заменить на UUID из Authentication > Users.
INSERT INTO profiles(full_name, email, phone, role_id, password_hash)
SELECT 'Администратор системы', 'admin@demo.local', '+79000000001', r.role_id, '$2b$12$demo_hash_replace_me'
FROM roles r WHERE r.code = 'admin'
ON CONFLICT (email) DO NOTHING;

INSERT INTO profiles(full_name, email, phone, role_id, password_hash)
SELECT 'Оператор Иванова Ирина', 'operator@demo.local', '+79000000002', r.role_id, '$2b$12$demo_hash_replace_me'
FROM roles r WHERE r.code = 'operator'
ON CONFLICT (email) DO NOTHING;

INSERT INTO profiles(full_name, email, phone, role_id, password_hash)
SELECT 'Специалист Петров Алексей', 'specialist@demo.local', '+79000000003', r.role_id, '$2b$12$demo_hash_replace_me'
FROM roles r WHERE r.code = 'specialist'
ON CONFLICT (email) DO NOTHING;

-- Пример обращения
INSERT INTO appeals(
    public_number, applicant_name, contact_phone, connection_address, description,
    type_id, status_id, priority_id, registered_at, assigned_specialist_id, created_by, updated_by
)
SELECT
    NULL,
    'Иванов Сергей Петрович',
    '+79001234567',
    'г. Новокузнецк, ул. Ленина, д. 10',
    'Жалоба на нестабильную работу интернета',
    t.type_id,
    s.status_id,
    p.priority_id,
    now(),
    spec.profile_id,
    op.profile_id,
    op.profile_id
FROM appeal_types t, appeal_statuses s, priorities p, profiles spec, profiles op
WHERE t.code = 'complaint'
  AND s.code = 'accepted'
  AND p.code = 'high'
  AND spec.email = 'specialist@demo.local'
  AND op.email = 'operator@demo.local'
LIMIT 1;
