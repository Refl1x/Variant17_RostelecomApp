# Вариант 17 — Ростелеком. Готовый проект приложений

В пакете два приложения и общая библиотека:

- `RostelecomAppeals.Desktop` — Windows-приложение C# WPF для операторов и администратора.
- `RostelecomAppeals.Mobile` — Android-приложение .NET MAUI для специалистов клиентского сервиса.
- `RostelecomAppeals.Shared` — общий Supabase REST-клиент, модели, валидация, экспорт, локальная очередь синхронизации, логирование.

## Что уже настроено

В код уже вставлены твои данные Supabase:

```text
REST URL: https://dhltigzsmoymvbwzjlqn.supabase.co/rest/v1
Auth URL: https://dhltigzsmoymvbwzjlqn.supabase.co/auth/v1
Publishable key: sb_publishable_mFbd7Pkv0RXBSyNZLt-1BQ_rSRjS9cx
```

Service role key в приложение не добавлялся специально. Его нельзя хранить в клиентской программе.

## Что реализовано по требованиям

### ПК, WPF

- вход через Supabase Auth;
- роли `admin`, `operator`, `specialist`;
- список обращений;
- добавление, редактирование, поиск, фильтрация, мягкое удаление;
- назначение специалиста;
- комментарии: внутренние и внешние;
- экспорт реестра в `.xlsx` и `.docx` без дополнительных NuGet-пакетов;
- панель администратора: создание профиля через Supabase signup, блокировка, смена роли;
- локальный структурированный лог `operations.log`;
- кэш обращений и локальная очередь синхронизации при ошибке сети;
- индикатор подключения;
- переключение светлой/тёмной темы;
- оформление в стиле Ростелекома: фиолетовый, оранжевый, голубой.

### Телефон, .NET MAUI Android

- вход через Supabase Auth;
- отображение назначенных обращений;
- поиск и фильтры по статусу/приоритету;
- просмотр карточки обращения;
- смена статуса обращения;
- добавление внешнего комментария;
- локальный кэш и очередь синхронизации;
- всплывающее уведомление о высоком приоритете;
- переключение темы;
- оформление в едином стиле с ПК.

## Как запустить БД в Supabase

1. Открой Supabase → SQL Editor.
2. Выполни файлы по порядку:
   - `sql/variant17_schema.sql`
   - `sql/variant17_seed.sql`
   - `sql/variant17_supabase_rls.sql`
3. Открой Authentication → Users.
4. Создай пользователей, например:
   - `admin@demo.local`
   - `operator@demo.local`
   - `specialist@demo.local`
5. Скопируй UUID каждого пользователя из Authentication.
6. Открой `sql/variant17_after_auth_link_TEMPLATE.sql`, подставь UUID и выполни в SQL Editor.

Без заполненного `profiles.auth_user_id` вход в приложение пройдет, но профиль и роль не найдутся.

## Как открыть проект

1. Установи Visual Studio 2022.
2. При установке выбери нагрузки:
   - `.NET desktop development`;
   - `.NET Multi-platform App UI development`;
   - Android SDK/Emulator для MAUI.
3. Открой файл `Variant17_RostelecomApp.sln`.
4. Сделай Restore NuGet packages.
5. Для ПК запусти проект `RostelecomAppeals.Desktop`.
6. Для телефона выбери проект `RostelecomAppeals.Mobile`, цель Android Emulator или физический телефон и нажми Run.

## Где менять подключение

ПК:

```text
src/RostelecomAppeals.Desktop/appsettings.json
```

Телефон:

```text
src/RostelecomAppeals.Mobile/AppServices.cs
```

## Важное ограничение

Создание пользователей в панели администратора работает через обычный `signup` Supabase. Если в Supabase включено обязательное подтверждение email, новый пользователь появится, но вход может быть невозможен до подтверждения почты. Для учебного проекта проще временно отключить подтверждение email в Authentication settings.

## Файлы экспорта

WPF-приложение создаёт настоящие `.xlsx` и `.docx` файлы с табличной структурой. Дополнительные библиотеки не нужны.


## Исправление от ошибок сборки

В этой версии исправлены ошибки:
- `CS8978: T не может быть nullable` — добавлены ограничения `where T : class` для generic-методов.
- `MC3074: 0 не существует в пространстве имен XML` — исправлен `StringFormat` в `CommentsWindow.xaml`.
- `NETSDK1202 / net8.0-android не поддерживается` — проекты переведены на .NET 9: `net9.0-windows`, `net9.0`, `net9.0-android`.

Если Visual Studio попросит Android SDK API 35, откройте Visual Studio Installer → Modify → Mobile development with .NET и установите актуальный Android SDK.
