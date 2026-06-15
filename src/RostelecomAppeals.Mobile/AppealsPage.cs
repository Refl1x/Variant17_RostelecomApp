using Microsoft.Maui.Controls.Shapes;
using RostelecomAppeals.Shared;
using System.Collections.ObjectModel;

namespace RostelecomAppeals.Mobile;

public sealed class AppealsPage : ContentPage
{
    private readonly ObservableCollection<AppealDto> _appeals = new();
    private List<AppealDto> _all = new();
    private readonly SearchBar _search = new() { Placeholder = "Поиск по ФИО, адресу, описанию" };
    private readonly Picker _status = new() { Title = "Статус" };
    private readonly Picker _priority = new() { Title = "Приоритет" };
    private readonly CollectionView _list;
    private readonly Label _stats = new() { TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 16 };
    private readonly VerticalStackLayout _statusChart = new() { Spacing = 10 };
    private readonly VerticalStackLayout _priorityChart = new() { Spacing = 10 };
    private List<DictionaryItem> _statuses = new();
    private List<DictionaryItem> _priorities = new();

    public AppealsPage()
    {
        Title = "Мои обращения";
        ApplyTheme();
        ThemeManager.ThemeChanged += ApplyTheme;

        _list = new CollectionView
        {
            ItemsSource = _appeals,
            SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() => CreateAppealCard()),
            Header = CreateHeader()
        };

        _list.SelectionChanged += List_SelectionChanged;
        _search.TextChanged += (_, _) => ApplyFilter();
        _status.SelectedIndexChanged += (_, _) => ApplyFilter();
        _priority.SelectedIndexChanged += (_, _) => ApplyFilter();

        Content = new RefreshView
        {
            Content = _list,
            Command = new Command(async () => await LoadAsync())
        };

        Loaded += async (_, _) => await LoadAsync();
    }

    private void ApplyTheme()
    {
        BackgroundColor = ThemeManager.BackgroundColor;
    }

    private View CreateHeader()
    {
        var refresh = ActionButton("Обновить", "#00AEEF");
        refresh.Clicked += async (_, _) => await LoadAsync();

        var changeStatus = ActionButton("Сменить статус", "#FF4F12");
        changeStatus.Clicked += ChangeStatus_Clicked;

        var comments = ActionButton("Комментарии", "#7700FF");
        comments.Clicked += AddComment_Clicked;


        var actionGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            ColumnSpacing = 10,
            RowSpacing = 10
        };
        actionGrid.Children.Add(refresh); actionGrid.SetRow(refresh, 0); actionGrid.SetColumn(refresh, 0);
        actionGrid.Children.Add(changeStatus); actionGrid.SetRow(changeStatus, 0); actionGrid.SetColumn(changeStatus, 1);
        actionGrid.Children.Add(comments); actionGrid.SetRow(comments, 1); actionGrid.SetColumn(comments, 0); actionGrid.SetColumnSpan(comments, 2);

        var filtersGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 10,
            Children = { _status, _priority }
        };
        filtersGrid.SetColumn(_priority, 1);

        return new VerticalStackLayout
        {
            Padding = new Thickness(14, 14, 14, 8),
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
                            new Label { Text = "Назначенные обращения", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                            new Label { Text = "Актуальный список ваших обращений с фильтрами, графиками и быстрыми действиями.", TextColor = Color.FromArgb("#F4EEFF") },
                            _stats
                        }
                    }
                },
                Card(new VerticalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        new Label { Text = "Поиск и фильтры", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                        _search,
                        filtersGrid
                    }
                }),
                Card(new VerticalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        new Label { Text = "Быстрые действия", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                        actionGrid
                    }
                }),
                Card(new VerticalStackLayout
                {
                    Spacing = 12,
                    Children =
                    {
                        new Label { Text = "График по статусам", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                        _statusChart,
                        new BoxView { HeightRequest = 1, Color = Color.FromArgb("#EEE7FA") },
                        new Label { Text = "График по приоритетам", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                        _priorityChart
                    }
                }),
                new Label { Text = "Список обращений", FontAttributes = FontAttributes.Bold, FontSize = 18, Margin = new Thickness(2, 6, 2, 0) }
            }
        };
    }

    private static View CreateAppealCard()
    {
        var number = new Label { FontAttributes = FontAttributes.Bold, FontSize = 17, TextColor = Color.FromArgb("#17141F") };
        number.SetBinding(Label.TextProperty, nameof(AppealDto.PublicNumber));

        var name = new Label { TextColor = Color.FromArgb("#6F6684"), FontSize = 13 };
        name.SetBinding(Label.TextProperty, nameof(AppealDto.ApplicantName));

        var desc = new Label { MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation, TextColor = Color.FromArgb("#17141F"), FontSize = 15 };
        desc.SetBinding(Label.TextProperty, nameof(AppealDto.Description));

        var statusChip = new Border { StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 999 }, Padding = new Thickness(10, 4), BackgroundColor = Color.FromArgb("#F0E8FF") };
        var statusLabel = new Label { FontSize = 12, TextColor = Color.FromArgb("#5312C8"), FontAttributes = FontAttributes.Bold };
        statusLabel.SetBinding(Label.TextProperty, nameof(AppealDto.StatusName));
        statusChip.Content = statusLabel;

        var priorityChip = new Border { StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 999 }, Padding = new Thickness(10, 4), BackgroundColor = Color.FromArgb("#FFF0EA") };
        var priorityLabel = new Label { FontSize = 12, TextColor = Color.FromArgb("#C74210"), FontAttributes = FontAttributes.Bold };
        priorityLabel.SetBinding(Label.TextProperty, nameof(AppealDto.PriorityName));
        priorityChip.Content = priorityLabel;

        var type = new Label { FontSize = 12, TextColor = Color.FromArgb("#00AEEF"), FontAttributes = FontAttributes.Bold };
        type.SetBinding(Label.TextProperty, nameof(AppealDto.TypeName));

        var address = new Label { FontSize = 13, TextColor = Color.FromArgb("#6F6684"), MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation };
        address.SetBinding(Label.TextProperty, nameof(AppealDto.ConnectionAddress));

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#E7E0F2"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Padding = 14,
            Margin = new Thickness(14, 6),
            Shadow = new Shadow { Brush = Brush.Black, Offset = new Point(0, 3), Radius = 10, Opacity = 0.06f },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    number,
                    name,
                    desc,
                    new HorizontalStackLayout { Spacing = 8, Children = { statusChip, priorityChip } },
                    new HorizontalStackLayout { Spacing = 6, Children = { type, new Label { Text = "•", TextColor = Color.FromArgb("#B0A7C5") }, address } }
                }
            }
        };
    }

    private async Task LoadAsync()
    {
        try
        {
            _statuses = await AppServices.Repository.GetStatusesAsync();
            _priorities = await AppServices.Repository.GetPrioritiesAsync();
            FillPicker(_status, _statuses.Select(x => x.Name).Prepend("Все статусы"));
            FillPicker(_priority, _priorities.Select(x => x.Name).Prepend("Все приоритеты"));
            await AppServices.Repository.RetryQueuedAsync();
            _all = await AppServices.Repository.GetAppealsAsync();
            if (_all.Any(a => a.PriorityName == "Высокий"))
                await DisplayAlert("Уведомление", "Есть обращения с высоким приоритетом.", "OK");
            ApplyFilter();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Загрузка", ex.Message, "OK");
        }
    }

    private static void FillPicker(Picker picker, IEnumerable<string> items)
    {
        var current = picker.SelectedItem?.ToString();
        picker.Items.Clear();
        foreach (var item in items)
            picker.Items.Add(item);

        picker.SelectedIndex = 0;
        if (!string.IsNullOrWhiteSpace(current))
        {
            var existing = picker.Items.IndexOf(current);
            if (existing >= 0)
                picker.SelectedIndex = existing;
        }
    }

    private void ApplyFilter()
    {
        var q = (_search.Text ?? string.Empty).Trim().ToLowerInvariant();
        var filtered = _all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            filtered = filtered.Where(a =>
                a.ApplicantName.ToLowerInvariant().Contains(q) ||
                a.ConnectionAddress.ToLowerInvariant().Contains(q) ||
                a.Description.ToLowerInvariant().Contains(q));
        }

        if (_status.SelectedIndex > 0)
            filtered = filtered.Where(a => a.StatusName == _status.SelectedItem?.ToString());

        if (_priority.SelectedIndex > 0)
            filtered = filtered.Where(a => a.PriorityName == _priority.SelectedItem?.ToString());

        var list = filtered.OrderByDescending(a => a.RegisteredAt).ToList();
        _appeals.Clear();
        foreach (var appeal in list)
            _appeals.Add(appeal);

        _stats.Text = $"Всего: {list.Count}   •   Решено: {list.Count(x => x.StatusName == "Решено")}   •   Высокий приоритет: {list.Count(x => x.PriorityName == "Высокий")}";
        BuildCharts(list);
    }

    private void BuildCharts(List<AppealDto> list)
    {
        _statusChart.Children.Clear();
        _priorityChart.Children.Clear();

        var statusData = list
            .GroupBy(x => x.StatusName)
            .OrderByDescending(x => x.Count())
            .Select(x => (Name: x.Key, Count: x.Count(), Color: "#7700FF"))
            .ToList();

        var priorityData = list
            .GroupBy(x => x.PriorityName)
            .OrderByDescending(x => x.Count())
            .Select(x => (Name: x.Key, Count: x.Count(), Color: x.Key == "Высокий" ? "#FF4F12" : x.Key == "Средний" ? "#00AEEF" : "#8E84A8"))
            .ToList();

        BuildChartSection(_statusChart, statusData);
        BuildChartSection(_priorityChart, priorityData);
    }

    private static void BuildChartSection(VerticalStackLayout host, List<(string Name, int Count, string Color)> items)
    {
        if (items.Count == 0)
        {
            host.Children.Add(new Label { Text = "Нет данных для отображения", TextColor = Color.FromArgb("#6F6684") });
            return;
        }

        var max = Math.Max(1, items.Max(x => x.Count));
        foreach (var item in items)
        {
            var name = new Label { Text = item.Name, FontAttributes = FontAttributes.Bold };
            var count = new Label { Text = item.Count.ToString(), TextColor = Color.FromArgb("#6F6684") };
            var top = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                Children = { name, count }
            };
            top.SetColumn(count, 1);

            host.Children.Add(new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    top,
                    new ProgressBar
                    {
                        Progress = item.Count / (double)max,
                        ProgressColor = Color.FromArgb(item.Color),
                        BackgroundColor = Color.FromArgb("#EEE7FA"),
                        HeightRequest = 8
                    }
                }
            });
        }
    }

    private async void List_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is AppealDto appeal)
            await Navigation.PushAsync(new AppealDetailPage(appeal));

        _list.SelectedItem = null;
    }

    private async void ChangeStatus_Clicked(object? sender, EventArgs e)
    {
        var appeal = await PickAppealAsync();
        if (appeal == null)
            return;

        var names = _statuses.Select(x => x.Name).ToArray();
        var chosen = await DisplayActionSheet("Новый статус", "Отмена", null, names);
        var status = _statuses.FirstOrDefault(x => x.Name == chosen);
        if (status == null)
            return;

        appeal.StatusId = status.StatusId;
        await AppServices.Repository.SaveAppealAsync(appeal);
        await LoadAsync();
    }

    private async void AddComment_Clicked(object? sender, EventArgs e)
    {
        var appeal = await PickAppealAsync();
        if (appeal == null)
            return;

        await Navigation.PushAsync(new CommentsPage(appeal));
    }

    private async Task<AppealDto?> PickAppealAsync()
    {
        var numbers = _appeals.Select(x => x.PublicNumber ?? x.AppealId.ToString()).ToArray();
        if (numbers.Length == 0)
            return null;

        var chosen = await DisplayActionSheet("Выберите обращение", "Отмена", null, numbers);
        return _appeals.FirstOrDefault(x => (x.PublicNumber ?? x.AppealId.ToString()) == chosen);
    }

    private static Button ActionButton(string text, string color) => new()
    {
        Text = text,
        BackgroundColor = Color.FromArgb(color),
        TextColor = Colors.White,
        CornerRadius = 16,
        FontAttributes = FontAttributes.Bold,
        FontSize = 14,
        Padding = new Thickness(10, 12),
        HeightRequest = 48,
        LineBreakMode = LineBreakMode.TailTruncation
    };

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

