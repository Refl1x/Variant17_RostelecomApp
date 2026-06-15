using Microsoft.Maui.Controls.Shapes;
using RostelecomAppeals.Shared;
using System.Collections.ObjectModel;

namespace RostelecomAppeals.Mobile;

public sealed class CommentsPage : ContentPage
{
    private readonly AppealDto _appeal;
    private readonly ObservableCollection<AppealCommentDto> _comments = new();
    private readonly Editor _comment = new()
    {
        Placeholder = "Введите внешний комментарий для оператора",
        MaxLength = 250,
        HeightRequest = 110,
        TextColor = Color.FromArgb("#17141F"),
        PlaceholderColor = Color.FromArgb("#6F6684")
    };

    private short _externalVisibilityId;

    public CommentsPage(AppealDto appeal)
    {
        _appeal = appeal;
        Title = "Комментарии";
        ApplyTheme();
        ThemeManager.ThemeChanged += ApplyTheme;

        var list = new CollectionView
        {
            ItemsSource = _comments,
            SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(() =>
            {
                var text = new Label { TextColor = Color.FromArgb("#17141F"), FontSize = 15 };
                text.SetBinding(Label.TextProperty, nameof(AppealCommentDto.CommentText));

                var meta = new Label { TextColor = Color.FromArgb("#6F6684"), FontSize = 12 };
                meta.BindingContextChanged += (_, _) =>
                {
                    if (meta.BindingContext is AppealCommentDto c)
                        meta.Text = $"{c.AuthorName} • {c.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}";
                };

                return new Border
                {
                    BackgroundColor = Colors.White,
                    Stroke = Color.FromArgb("#E7E0F2"),
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = 18 },
                    Padding = 12,
                    Margin = new Thickness(0, 6),
                    Content = new VerticalStackLayout { Spacing = 6, Children = { text, meta } }
                };
            })
        };

        var add = new Button
        {
            Text = "Отправить",
            BackgroundColor = Color.FromArgb("#FF4F12"),
            TextColor = Colors.White,
            CornerRadius = 16,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 48,
            Padding = new Thickness(16, 12)
        };
        add.Clicked += Add_Clicked;

        Content = new Grid
        {
            Padding = 14,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            },
            Children =
            {
                new Border
                {
                    Padding = 18,
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 24 },
                    BackgroundColor = Colors.White,
                    Content = new VerticalStackLayout
                    {
                        Spacing = 6,
                        Children =
                        {
                            new Label { Text = "Комментарии по обращению", FontSize = 22, FontAttributes = FontAttributes.Bold },
                            new Label { Text = _appeal.PublicNumber, TextColor = Color.FromArgb("#7700FF"), FontAttributes = FontAttributes.Bold },
                            new Label { Text = _appeal.Description, TextColor = Color.FromArgb("#6F6684") }
                        }
                    }
                },
                list,
                new Border
                {
                    Padding = 14,
                    BackgroundColor = Colors.White,
                    Stroke = Color.FromArgb("#E7E0F2"),
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = 24 },
                    Content = new VerticalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            new Label { Text = "Новый комментарий", FontAttributes = FontAttributes.Bold, FontSize = 18 },
                            new Border
                            {
                                Stroke = Color.FromArgb("#E7E0F2"),
                                StrokeThickness = 1,
                                BackgroundColor = Color.FromArgb("#FBFAFE"),
                                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                                Padding = new Thickness(12, 8),
                                Content = _comment
                            },
                            add
                        }
                    }
                }
            }
        }.Also(grid =>
        {
            grid.SetRow(grid.Children[0], 0);
            grid.SetRow(grid.Children[1], 1);
            grid.SetRow(grid.Children[2], 2);
        });

        Loaded += async (_, _) => await LoadAsync();
    }

    private void ApplyTheme()
    {
        BackgroundColor = ThemeManager.BackgroundColor;
    }

    private async Task LoadAsync()
    {
        var visibility = (await AppServices.Repository.GetCommentVisibilitiesAsync()).FirstOrDefault(x => x.Code == "external");
        _externalVisibilityId = visibility?.VisibilityId ?? 2;
        var data = await AppServices.Repository.GetCommentsAsync(_appeal.AppealId);
        _comments.Clear();
        foreach (var c in data)
            _comments.Add(c);
    }

    private async void Add_Clicked(object? sender, EventArgs e)
    {
        try
        {
            var text = (_comment.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                await DisplayAlert("Комментарий", "Введите текст комментария.", "OK");
                return;
            }

            await AppServices.Repository.AddCommentAsync(_appeal.AppealId, text, _externalVisibilityId, false);
            _comment.Text = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Комментарий", ex.Message, "OK");
        }
    }
}

file static class CommentPageExtensions
{
    public static T Also<T>(this T value, Action<T> action)
    {
        action(value);
        return value;
    }
}
