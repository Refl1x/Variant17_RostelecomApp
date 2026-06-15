using RostelecomAppeals.Shared;
using System.Windows;

namespace RostelecomAppeals.Desktop;

public partial class CommentsWindow : Window
{
    private readonly AppealDto _appeal;
    private List<DictionaryItem> _visibilities = new();

    public CommentsWindow(AppealDto appeal)
    {
        InitializeComponent();
        _appeal = appeal;
        TitleText.Text = $"Комментарии: {appeal.PublicNumber}";
        SubText.Text = appeal.ApplicantName;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _visibilities = await AppServices.Repository.GetCommentVisibilitiesAsync();
        if (AppServices.Client.CurrentProfile?.RoleCode == "specialist")
            _visibilities = _visibilities.Where(x => x.Code == "external").ToList();
        VisibilityBox.ItemsSource = _visibilities;
        VisibilityBox.SelectedIndex = 0;
        CommentsList.ItemsSource = await AppServices.Repository.GetCommentsAsync(_appeal.AppealId);
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (VisibilityBox.SelectedItem is not DictionaryItem visibility) return;
            await AppServices.Repository.AddCommentAsync(_appeal.AppealId, CommentBox.Text.Trim(), visibility.VisibilityId, visibility.Code == "internal");
            CommentBox.Clear();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Комментарий", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
