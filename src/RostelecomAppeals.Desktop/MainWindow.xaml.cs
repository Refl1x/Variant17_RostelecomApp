using Microsoft.Win32;
using RostelecomAppeals.Shared;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace RostelecomAppeals.Desktop;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<AppealDto> _appeals = new();
    private List<AppealDto> _allAppeals = new();
    private List<DictionaryItem> _types = new();
    private List<DictionaryItem> _statuses = new();
    private List<DictionaryItem> _priorities = new();
    private bool _dark;

    public MainWindow()
    {
        InitializeComponent();
        AppealsGrid.ItemsSource = _appeals;
        Loaded += async (_, _) => await LoadDictionariesSilentAsync();
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SyncStatusText.Text = "Вход...";
            var session = await AppServices.Client.SignInAsync(EmailBox.Text.Trim(), PasswordBox.Password);
            await AppServices.Store.SaveAsync("session.json", session);
            var profile = AppServices.Client.CurrentProfile;
            if (profile == null) throw new InvalidOperationException("Профиль не найден. Заполните таблицу profiles и auth_user_id.");
            if (profile.IsBlocked) throw new InvalidOperationException("Учётная запись заблокирована.");
            LoginPanel.Visibility = Visibility.Collapsed;
            AdminButton.Visibility = profile.RoleCode == "admin" ? Visibility.Visible : Visibility.Collapsed;
            SyncStatusText.Text = $"Онлайн • {profile.FullName} • {profile.RoleName}";
            await AppServices.Logger.InfoAsync("LOGIN", profile.Email ?? profile.FullName);
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            SyncStatusText.Text = "Ошибка входа";
            MessageBox.Show(ex.Message, "Вход", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadDictionariesSilentAsync()
    {
        try
        {
            _types = await AppServices.Repository.GetTypesAsync();
            _statuses = await AppServices.Repository.GetStatusesAsync();
            _priorities = await AppServices.Repository.GetPrioritiesAsync();
            FillFilter(TypeFilter, _types, "Все типы");
            FillFilter(StatusFilter, _statuses, "Все статусы");
            FillFilter(PriorityFilter, _priorities, "Все приоритеты");
        }
        catch { }
    }

    private static void FillFilter(System.Windows.Controls.ComboBox box, List<DictionaryItem> items, string allText)
    {
        var list = new List<DictionaryItem> { new() { Name = allText } };
        list.AddRange(items);
        box.ItemsSource = list;
        box.SelectedIndex = 0;
    }

    private async Task LoadAllAsync()
    {
        await LoadDictionariesSilentAsync();
        await AppServices.Repository.RetryQueuedAsync();
        _allAppeals = await AppServices.Repository.GetAppealsAsync();
        ApplyFilter();
        await LoadStatsAsync();
        var online = await AppServices.Client.PingAsync();
        SyncStatusText.Text = online ? SyncStatusText.Text.Replace("Оффлайн", "Онлайн") : "Оффлайн • данные из кэша";
    }

    private async Task LoadStatsAsync()
    {
        StatsTotal.Text = $"Всего: {_allAppeals.Count}";
        StatusStats.ItemsSource = _allAppeals.GroupBy(x => x.StatusName).Select(x => $"{x.Key}: {x.Count()}").ToList();
        PriorityStats.ItemsSource = _allAppeals.GroupBy(x => x.PriorityName).Select(x => $"{x.Key}: {x.Count()}").ToList();
        await Task.CompletedTask;
    }

    private void ApplyFilter()
    {
        var query = SearchBox.Text?.Trim().ToLowerInvariant() ?? "";
        var type = TypeFilter.SelectedItem as DictionaryItem;
        var status = StatusFilter.SelectedItem as DictionaryItem;
        var priority = PriorityFilter.SelectedItem as DictionaryItem;
        var filtered = _allAppeals.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(a =>
                a.ApplicantName.ToLowerInvariant().Contains(query) ||
                a.ConnectionAddress.ToLowerInvariant().Contains(query) ||
                a.Description.ToLowerInvariant().Contains(query) ||
                (a.PublicNumber ?? "").ToLowerInvariant().Contains(query));
        }
        if (type?.TypeId > 0) filtered = filtered.Where(a => a.TypeId == type.TypeId);
        if (status?.StatusId > 0) filtered = filtered.Where(a => a.StatusId == status.StatusId);
        if (priority?.PriorityId > 0) filtered = filtered.Where(a => a.PriorityId == priority.PriorityId);
        _appeals.Clear();
        foreach (var a in filtered) _appeals.Add(a);
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        try { await LoadAllAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Обновление", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilter();

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var win = new AppealEditWindow(null, _types, _statuses, _priorities) { Owner = this };
        if (win.ShowDialog() == true) await LoadAllAsync();
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (AppealsGrid.SelectedItem is not AppealDto appeal) return;
        var win = new AppealEditWindow(appeal, _types, _statuses, _priorities) { Owner = this };
        if (win.ShowDialog() == true) await LoadAllAsync();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (AppealsGrid.SelectedItem is not AppealDto appeal) return;
        if (MessageBox.Show($"Удалить обращение {appeal.PublicNumber}?", "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await AppServices.Repository.SoftDeleteAppealAsync(appeal);
        await LoadAllAsync();
    }

    private void Comments_Click(object sender, RoutedEventArgs e)
    {
        if (AppealsGrid.SelectedItem is not AppealDto appeal) return;
        new CommentsWindow(appeal) { Owner = this }.ShowDialog();
    }

    private async void ExportXlsx_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = "reestr_obrashcheniy.xlsx" };
        if (dlg.ShowDialog() == true)
        {
            await AppServices.Exporter.ExportXlsxAsync(dlg.FileName, _appeals);
            await AppServices.Logger.InfoAsync("EXPORT_XLSX", dlg.FileName);
            MessageBox.Show("Excel-файл сохранён.", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void ExportDocx_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "Word (*.docx)|*.docx", FileName = "otchet_obrashcheniy.docx" };
        if (dlg.ShowDialog() == true)
        {
            await AppServices.Exporter.ExportDocxAsync(dlg.FileName, _appeals);
            await AppServices.Logger.InfoAsync("EXPORT_DOCX", dlg.FileName);
            MessageBox.Show("Word-файл сохранён.", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Admin_Click(object sender, RoutedEventArgs e) => new AdminWindow { Owner = this }.ShowDialog();

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        _dark = !_dark;
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_dark ? "#171226" : "#F5F3FB"));
    }
}
