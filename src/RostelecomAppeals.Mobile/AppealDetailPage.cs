using Microsoft.Maui.Controls.Shapes;
using RostelecomAppeals.Shared;

namespace RostelecomAppeals.Mobile;

public sealed class AppealDetailPage : ContentPage
{
    public AppealDetailPage(AppealDto appeal)
    {
        Title = appeal.PublicNumber ?? "Обращение";
        ApplyTheme();
        ThemeManager.ThemeChanged += ApplyTheme;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(14),
                Spacing = 12,
                Children =
                {
                    new Border
                    {
                        Padding = 18,
                        StrokeThickness = 0,
                        StrokeShape = new RoundRectangle { CornerRadius = 26 },
                        Background = new LinearGradientBrush(
                            new GradientStopCollection
                            {
                                new GradientStop(Color.FromArgb("#7700FF"), 0.0f),
                                new GradientStop(Color.FromArgb("#5312C8"), 0.55f),
                                new GradientStop(Color.FromArgb("#00AEEF"), 1.0f)
                            }, new Point(0, 0), new Point(1, 1)),
                        Content = new VerticalStackLayout
                        {
                            Spacing = 8,
                            Children =
                            {
                                new Label { Text = appeal.PublicNumber, FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                                new Label { Text = appeal.Description, TextColor = Color.FromArgb("#F4EEFF"), FontSize = 15 },
                                new HorizontalStackLayout
                                {
                                    Spacing = 8,
                                    Children =
                                    {
                                        Chip(appeal.StatusName, "#FFFFFF22", Colors.White),
                                        Chip(appeal.PriorityName, "#FFFFFF22", Colors.White),
                                        Chip(appeal.TypeName, "#FFFFFF22", Colors.White)
                                    }
                                }
                            }
                        }
                    },
                    Card(new VerticalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            new Label { Text = "Детали обращения", FontSize = 20, FontAttributes = FontAttributes.Bold },
                            Row("Заявитель", appeal.ApplicantName),
                            Row("Телефон", appeal.ContactPhone),
                            Row("Адрес", appeal.ConnectionAddress),
                            Row("Дата регистрации", appeal.RegisteredAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")),
                            Row("Специалист", appeal.SpecialistName)
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

    private static View Row(string key, string? value) => new Border
    {
        BackgroundColor = Color.FromArgb("#FBFAFE"),
        Stroke = Color.FromArgb("#E7E0F2"),
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 18 },
        Padding = new Thickness(14, 10),
        Content = new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = key, TextColor = Color.FromArgb("#6F6684"), FontSize = 12 },
                new Label { Text = value ?? string.Empty, TextColor = Color.FromArgb("#17141F"), FontSize = 16 }
            }
        }
    };

    private static Border Card(View child) => new()
    {
        BackgroundColor = Colors.White,
        Stroke = Color.FromArgb("#E7E0F2"),
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 24 },
        Padding = 18,
        Content = child,
        Shadow = new Shadow { Brush = Brush.Black, Offset = new Point(0, 4), Radius = 10, Opacity = 0.05f }
    };

    private static Border Chip(string text, string background, Color textColor) => new()
    {
        BackgroundColor = Color.FromArgb("#F8EC3A"),
        StrokeThickness = 0,
        StrokeShape = new RoundRectangle { CornerRadius = 999 },
        Padding = new Thickness(12, 6),
        Content = new Label { Text = text, FontSize = 12, TextColor = Colors.Black, FontAttributes = FontAttributes.Bold }
    };
}
