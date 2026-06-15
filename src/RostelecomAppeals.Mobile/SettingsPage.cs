using Microsoft.Maui.Controls.Shapes;
using RostelecomAppeals.Shared;

namespace RostelecomAppeals.Mobile;

public sealed class SettingsPage : ContentPage
{
    private readonly Label _themeLabel = new() { TextColor = Color.FromArgb("#6F6684") };

    public SettingsPage()
    {
        Title = "Ещё";
        ApplyTheme();
        ThemeManager.ThemeChanged += ApplyTheme;

        var profile = AppServices.Client.CurrentProfile;
        var themeButton = new Button
        {
            Text = "Сменить фон",
            BackgroundColor = Color.FromArgb("#5312C8"),
            TextColor = Colors.White,
            CornerRadius = 16,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 48
        };
        themeButton.Clicked += (_, _) =>
        {
            ThemeManager.ToggleBackgroundTheme();
            UpdateThemeLabel();
        };

        var logoutButton = new Button
        {
            Text = "Выйти из аккаунта",
            BackgroundColor = Color.FromArgb("#FF4F12"),
            TextColor = Colors.White,
            CornerRadius = 16,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 48
        };
        logoutButton.Clicked += async (_, _) => await LogoutAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 14,
                Spacing = 12,
                Children =
                {
                    new Border
                    {
                        Padding = 18,
                        StrokeThickness = 0,
                        StrokeShape = new RoundRectangle { CornerRadius = 24 },
                        Background = new LinearGradientBrush(
                            new GradientStopCollection
                            {
                                new GradientStop(Color.FromArgb("#7700FF"), 0.0f),
                                new GradientStop(Color.FromArgb("#5312C8"), 0.55f),
                                new GradientStop(Color.FromArgb("#00AEEF"), 1.0f)
                            }, new Point(0,0), new Point(1,1)),
                        Content = new VerticalStackLayout
                        {
                            Spacing = 6,
                            Children =
                            {
                                new Label { Text = "Настройки и профиль", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                                new Label { Text = profile?.FullName ?? "Пользователь", TextColor = Colors.White, FontAttributes = FontAttributes.Bold },
                                new Label { Text = profile?.Email ?? string.Empty, TextColor = Color.FromArgb("#F4EEFF") }
                            }
                        }
                    },
                    Card(new VerticalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            new Label { Text = "Оформление", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                            _themeLabel,
                            themeButton
                        }
                    }),
                    Card(new VerticalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            new Label { Text = "Аккаунт", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                            logoutButton
                        }
                    })
                }
            }
        };

        UpdateThemeLabel();
    }

    private void ApplyTheme()
    {
        BackgroundColor = ThemeManager.BackgroundColor;
        UpdateThemeLabel();
    }

    private void UpdateThemeLabel()
    {
        _themeLabel.Text = ThemeManager.IsDarkBackground ? "Сейчас включён тёмный фон приложения." : "Сейчас включён светлый фон приложения.";
    }

    private async Task LogoutAsync()
    {
        await SecureStorage.SetAsync("access_token", string.Empty);
        await SecureStorage.SetAsync("refresh_token", string.Empty);
        Application.Current!.Windows[0].Page = new NavigationPage(new LoginPage())
        {
            BarBackgroundColor = Color.FromArgb("#7700FF"),
            BarTextColor = Colors.White
        };
    }

    private static Border Card(View child) => new()
    {
        BackgroundColor = Colors.White,
        Stroke = Color.FromArgb("#E7E0F2"),
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 24 },
        Padding = 16,
        Content = child,
        Shadow = new Shadow { Brush = Brush.Black, Offset = new Point(0, 3), Radius = 10, Opacity = 0.05f }
    };
}
