using Microsoft.Maui.Controls.Shapes;
using RostelecomAppeals.Shared;

namespace RostelecomAppeals.Mobile;

public sealed class LoginPage : ContentPage
{
    private readonly Entry _email = new()
    {
        Placeholder = "Введите email",
        Text = "specialist@demo.local",
        Keyboard = Keyboard.Email,
        TextColor = Color.FromArgb("#17141F"),
        PlaceholderColor = Color.FromArgb("#6F6684")
    };

    private readonly Entry _password = new()
    {
        Placeholder = "Введите пароль",
        IsPassword = true,
        TextColor = Color.FromArgb("#17141F"),
        PlaceholderColor = Color.FromArgb("#6F6684")
    };

    private readonly Label _status = new()
    {
        TextColor = Color.FromArgb("#17141F"),
        FontAttributes = FontAttributes.Bold,
        FontSize = 14
    };

    public LoginPage()
    {
        Title = "Авторизация";
        ApplyTheme();
        ThemeManager.ThemeChanged += ApplyTheme;

        var loginButton = PrimaryButton("Войти", "#7700FF");
        loginButton.Clicked += Login_Clicked;

        var demoLabel = new Label
        {
            Text = "Тестовый логин: specialist@demo.local",
            TextColor = Color.FromArgb("#6F6684"),
            FontSize = 13
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(18, 24),
                Spacing = 18,
                Children =
                {
                    new Border
                    {
                        Padding = 24,
                        StrokeThickness = 0,
                        StrokeShape = new RoundRectangle { CornerRadius = 30 },
                        Background = new LinearGradientBrush(
                            new GradientStopCollection
                            {
                                new GradientStop(Color.FromArgb("#7700FF"), 0.0f),
                                new GradientStop(Color.FromArgb("#5312C8"), 0.55f),
                                new GradientStop(Color.FromArgb("#00AEEF"), 1.0f)
                            },
                            new Point(0, 0),
                            new Point(1, 1)),
                        Content = new VerticalStackLayout
                        {
                            Spacing = 8,
                            Children =
                            {
                                new Label { Text = "Ростелеком", FontSize = 32, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                                new Label { Text = "Система учета обращений граждан и клиентов", FontSize = 16, TextColor = Color.FromArgb("#F4EEFF") },
                                new HorizontalStackLayout
                                {
                                    Spacing = 8,
                                    Children =
                                    {
                                        Chip("Мобильная версия", "#F8EC3A", Colors.Black),
                                        Chip("Supabase", "#F8EC3A", Colors.Black)
                                    }
                                }
                            }
                        }
                    },
                    Card(new VerticalStackLayout
                    {
                        Spacing = 14,
                        Children =
                        {
                            new Label { Text = "Вход в приложение", FontSize = 24, FontAttributes = FontAttributes.Bold },
                            new Label { Text = "Введите данные своей учетной записи. Поля ввода теперь видимы и читаемы на светлой теме.", TextColor = Color.FromArgb("#6F6684") },
                            Field("Email", _email),
                            Field("Пароль", _password),
                            demoLabel,
                            loginButton,
                            _status
                        }
                    })
                }
            }
        };
    }

    private void ApplyTheme()
    {
        BackgroundColor = ThemeManager.BackgroundColor;
    }

    private async void Login_Clicked(object? sender, EventArgs e)
    {
        try
        {
            _status.Text = "Выполняется вход...";
            var session = await AppServices.Client.SignInAsync(_email.Text?.Trim() ?? "", _password.Text ?? "");
            await SecureStorage.SetAsync("access_token", session.AccessToken);
            await SecureStorage.SetAsync("refresh_token", session.RefreshToken);
            var profile = AppServices.Client.CurrentProfile;
            if (profile == null)
                throw new InvalidOperationException("Профиль пользователя не найден в таблице profiles.");

            if (profile.RoleCode != "specialist" && profile.RoleCode != "admin")
            {
                await DisplayAlert("Роль", "Мобильное приложение рассчитано на роль specialist. Вход разрешён, но часть данных может быть скрыта RLS.", "OK");
            }

            _status.Text = "Вход выполнен";
            await AppServices.Logger.InfoAsync("MOBILE_LOGIN", profile.Email ?? profile.FullName);
            Application.Current!.Windows[0].Page = new AppRootPage();
        }
        catch (Exception ex)
        {
            _status.Text = "Ошибка входа";
            await DisplayAlert("Вход", ex.Message, "OK");
        }
    }

    private static View Field(string title, View field) => new VerticalStackLayout
    {
        Spacing = 6,
        Children =
        {
            new Label { Text = title, FontAttributes = FontAttributes.Bold },
            new Border
            {
                Stroke = Color.FromArgb("#E7E0F2"),
                StrokeThickness = 1,
                BackgroundColor = Color.FromArgb("#FBFAFE"),
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(14, 4),
                Content = field
            }
        }
    };

    private static Border Card(View child) => new()
    {
        BackgroundColor = Colors.White,
        Stroke = Color.FromArgb("#E7E0F2"),
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 26 },
        Padding = 18,
        Content = child,
        Shadow = new Shadow { Brush = Brush.Black, Offset = new Point(0, 4), Radius = 12, Opacity = 0.08f }
    };

    private static Button PrimaryButton(string text, string color) => new()
    {
        Text = text,
        BackgroundColor = Color.FromArgb(color),
        TextColor = Colors.White,
        CornerRadius = 16,
        Padding = new Thickness(16, 14),
        FontAttributes = FontAttributes.Bold,
        HeightRequest = 52
    };

    private static Border Chip(string text, string background, Color textColor) => new()
    {
        BackgroundColor = Color.FromArgb(background),
        StrokeThickness = 0,
        StrokeShape = new RoundRectangle { CornerRadius = 999 },
        Padding = new Thickness(12, 6),
        Content = new Label { Text = text, FontSize = 12, TextColor = textColor }
    };
}
