using Microsoft.Maui.Controls.Shapes;
using RostelecomAppeals.Shared;

namespace RostelecomAppeals.Mobile;

public sealed class StatisticsPage : ContentPage
{
    private readonly VerticalStackLayout _summaryHost = new() { Spacing = 10 };
    private readonly VerticalStackLayout _statusHost = new() { Spacing = 10 };
    private readonly VerticalStackLayout _priorityHost = new() { Spacing = 10 };
    private readonly VerticalStackLayout _typeHost = new() { Spacing = 10 };

    public StatisticsPage()
    {
        Title = "Статистика";
        ApplyTheme();
        ThemeManager.ThemeChanged += ApplyTheme;

        var refresh = new Button
        {
            Text = "Обновить статистику",
            BackgroundColor = Color.FromArgb("#7700FF"),
            TextColor = Colors.White,
            CornerRadius = 16,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 48
        };
        refresh.Clicked += async (_, _) => await LoadAsync();

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
                            Spacing = 8,
                            Children =
                            {
                                new Label { Text = "Статистика обращений", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                                new Label { Text = "Отдельный экран аналитики по вашим обращениям.", TextColor = Color.FromArgb("#F4EEFF") },
                                refresh
                            }
                        }
                    },
                    Card(new VerticalStackLayout { Spacing = 10, Children = { new Label { Text = "Сводка", FontSize = 18, FontAttributes = FontAttributes.Bold }, _summaryHost } }),
                    Card(new VerticalStackLayout { Spacing = 10, Children = { new Label { Text = "По статусам", FontSize = 18, FontAttributes = FontAttributes.Bold }, _statusHost } }),
                    Card(new VerticalStackLayout { Spacing = 10, Children = { new Label { Text = "По приоритетам", FontSize = 18, FontAttributes = FontAttributes.Bold }, _priorityHost } }),
                    Card(new VerticalStackLayout { Spacing = 10, Children = { new Label { Text = "По типам обращений", FontSize = 18, FontAttributes = FontAttributes.Bold }, _typeHost } })
                }
            }
        };

        Loaded += async (_, _) => await LoadAsync();
    }

    private void ApplyTheme()
    {
        BackgroundColor = ThemeManager.BackgroundColor;
    }

    private async Task LoadAsync()
    {
        try
        {
            var appeals = await AppServices.Repository.GetAppealsAsync();
            BuildSummary(appeals);
            BuildBars(_statusHost, appeals.GroupBy(x => x.StatusName).Select(g => (g.Key, g.Count(), "#7700FF")).OrderByDescending(x => x.Item2).ToList());
            BuildBars(_priorityHost, appeals.GroupBy(x => x.PriorityName).Select(g => (g.Key, g.Count(), g.Key == "Высокий" ? "#FF4F12" : g.Key == "Средний" ? "#00AEEF" : "#8E84A8")).OrderByDescending(x => x.Item2).ToList());
            BuildBars(_typeHost, appeals.GroupBy(x => x.TypeName).Select(g => (g.Key, g.Count(), "#5312C8")).OrderByDescending(x => x.Item2).ToList());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Статистика", ex.Message, "OK");
        }
    }

    private void BuildSummary(List<AppealDto> appeals)
    {
        _summaryHost.Children.Clear();
        var total = appeals.Count;
        var resolved = appeals.Count(x => x.StatusName == "Решено");
        var high = appeals.Count(x => x.PriorityName == "Высокий");
        var active = appeals.Count(x => x.StatusName != "Решено" && x.StatusName != "Отклонено");

        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            ColumnSpacing = 10,
            RowSpacing = 10
        };

        var c1 = StatCard("Всего", total.ToString(), "#7700FF");
        var c2 = StatCard("Решено", resolved.ToString(), "#00AEEF");
        var c3 = StatCard("Активные", active.ToString(), "#5312C8");
        var c4 = StatCard("Высокий приоритет", high.ToString(), "#FF4F12");
        grid.Children.Add(c1); grid.SetRow(c1,0); grid.SetColumn(c1,0);
        grid.Children.Add(c2); grid.SetRow(c2,0); grid.SetColumn(c2,1);
        grid.Children.Add(c3); grid.SetRow(c3,1); grid.SetColumn(c3,0);
        grid.Children.Add(c4); grid.SetRow(c4,1); grid.SetColumn(c4,1);
        _summaryHost.Children.Add(grid);
    }

    private static Border StatCard(string title, string value, string color) => new()
    {
        BackgroundColor = Color.FromArgb("#FBFAFE"),
        Stroke = Color.FromArgb("#E7E0F2"),
        StrokeThickness = 1,
        StrokeShape = new RoundRectangle { CornerRadius = 18 },
        Padding = 14,
        Content = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, TextColor = Color.FromArgb("#6F6684"), FontSize = 13 },
                new Label { Text = value, TextColor = Color.FromArgb(color), FontSize = 24, FontAttributes = FontAttributes.Bold }
            }
        }
    };

    private static void BuildBars(VerticalStackLayout host, List<(string Name, int Count, string Color)> items)
    {
        host.Children.Clear();
        if (items.Count == 0)
        {
            host.Children.Add(new Label { Text = "Нет данных", TextColor = Color.FromArgb("#6F6684") });
            return;
        }

        var max = Math.Max(1, items.Max(x => x.Count));
        foreach (var item in items)
        {
            var left = new Label { Text = item.Name, FontAttributes = FontAttributes.Bold };
            var right = new Label { Text = item.Count.ToString(), TextColor = Color.FromArgb("#6F6684") };
            var top = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, Children = { left, right } };
            top.SetColumn(right, 1);
            host.Children.Add(new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    top,
                    new ProgressBar { Progress = item.Count / (double)max, ProgressColor = Color.FromArgb(item.Color), BackgroundColor = Color.FromArgb("#EEE7FA"), HeightRequest = 8 }
                }
            });
        }
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
