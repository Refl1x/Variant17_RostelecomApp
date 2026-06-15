using RostelecomAppeals.Shared;
using System.Windows;

namespace RostelecomAppeals.Desktop;

public partial class AdminWindow : Window
{
    private List<RoleDto> _roles = new();
    public AdminWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _roles = await AppServices.Repository.GetRolesAsync();
        RoleBox.ItemsSource = _roles;
        ChangeRoleBox.ItemsSource = _roles;
        if (_roles.Count > 0) { RoleBox.SelectedIndex = 0; ChangeRoleBox.SelectedIndex = 0; }
        ProfilesGrid.ItemsSource = await AppServices.Repository.GetProfilesAsync();
    }

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (RoleBox.SelectedItem is not RoleDto role) return;
            await AppServices.Repository.CreateUserProfileAsync(EmailBox.Text.Trim(), PasswordBox.Password, FullNameBox.Text.Trim(), PhoneBox.Text.Trim(), role.RoleId);
            MessageBox.Show("Пользователь создан. Если в Supabase включено подтверждение email, подтвердите почту в Auth.", "Администратор", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Администратор", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async void ChangeRole_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ProfilesGrid.SelectedItem is not ProfileDto profile || ChangeRoleBox.SelectedItem is not RoleDto role) return;
            await AppServices.Repository.SetProfileRoleAsync(profile, role.RoleId);
            await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Роль", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async void Block_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ProfilesGrid.SelectedItem is not ProfileDto profile) return;
            await AppServices.Repository.SetProfileBlockedAsync(profile, !profile.IsBlocked);
            await LoadAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
}
