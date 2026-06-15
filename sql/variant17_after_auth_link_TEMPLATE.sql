-- После создания пользователей в Supabase Authentication > Users
-- скопируйте UUID каждого пользователя и подставьте вместо 00000000-0000-0000-0000-000000000000.

UPDATE profiles
SET auth_user_id = '00000000-0000-0000-0000-000000000000'
WHERE email = 'admin@demo.local';

UPDATE profiles
SET auth_user_id = '00000000-0000-0000-0000-000000000000'
WHERE email = 'operator@demo.local';

UPDATE profiles
SET auth_user_id = '00000000-0000-0000-0000-000000000000'
WHERE email = 'specialist@demo.local';
