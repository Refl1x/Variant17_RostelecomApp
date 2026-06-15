using RostelecomAppeals.Shared;
using System.Windows;

namespace RostelecomAppeals.Desktop;

public partial class AppealEditWindow : Window
{
    private readonly AppealDto _appeal;
    private readonly bool _isNew;
    private List<ProfileDto> _specialists = new();

    public AppealEditWindow(AppealDto? appeal, List<DictionaryItem> types, List<DictionaryItem> statuses, List<DictionaryItem> priorities)
    {
        InitializeComponent();
        _isNew = appeal == null;
        _appeal = appeal == null ? new AppealDto() : Clone(appeal);
        TypeBox.ItemsSource = types;
        StatusBox.ItemsSource = statuses;
        PriorityBox.ItemsSource = priorities;
        Loaded += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        _specialists = await AppServices.Repository.GetSpecialistsAsync();
        _specialists.Insert(0, new ProfileDto { FullName = "Не назначен", ProfileId = Guid.Empty });
        SpecialistBox.ItemsSource = _specialists;

        ApplicantBox.Text = _appeal.ApplicantName;
        PhoneBox.Text = _appeal.ContactPhone;
        AddressBox.Text = _appeal.ConnectionAddress;
        DescriptionBox.Text = _appeal.Description;
        TypeBox.SelectedItem = TypeBox.Items.Cast<DictionaryItem>().FirstOrDefault(x => x.TypeId == _appeal.TypeId) ?? TypeBox.Items.Cast<DictionaryItem>().FirstOrDefault();
        StatusBox.SelectedItem = StatusBox.Items.Cast<DictionaryItem>().FirstOrDefault(x => x.StatusId == _appeal.StatusId) ?? StatusBox.Items.Cast<DictionaryItem>().FirstOrDefault();
        PriorityBox.SelectedItem = PriorityBox.Items.Cast<DictionaryItem>().FirstOrDefault(x => x.PriorityId == _appeal.PriorityId) ?? PriorityBox.Items.Cast<DictionaryItem>().FirstOrDefault();
        SpecialistBox.SelectedItem = _specialists.FirstOrDefault(x => x.ProfileId == (_appeal.AssignedSpecialistId ?? Guid.Empty)) ?? _specialists[0];
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _appeal.ApplicantName = ApplicantBox.Text.Trim();
            _appeal.ContactPhone = PhoneBox.Text.Trim();
            _appeal.ConnectionAddress = AddressBox.Text.Trim();
            _appeal.Description = DescriptionBox.Text.Trim();
            _appeal.TypeId = ((DictionaryItem)TypeBox.SelectedItem).TypeId;
            _appeal.StatusId = ((DictionaryItem)StatusBox.SelectedItem).StatusId;
            _appeal.PriorityId = ((DictionaryItem)PriorityBox.SelectedItem).PriorityId;
            var specialist = SpecialistBox.SelectedItem as ProfileDto;
            _appeal.AssignedSpecialistId = specialist?.ProfileId == Guid.Empty ? null : specialist?.ProfileId;
            await AppServices.Repository.SaveAppealAsync(_appeal);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Сохранение", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static AppealDto Clone(AppealDto a) => new()
    {
        AppealId = a.AppealId,
        PublicNumber = a.PublicNumber,
        ApplicantName = a.ApplicantName,
        ContactPhone = a.ContactPhone,
        ConnectionAddress = a.ConnectionAddress,
        Description = a.Description,
        TypeId = a.TypeId,
        StatusId = a.StatusId,
        PriorityId = a.PriorityId,
        RegisteredAt = a.RegisteredAt,
        AssignedSpecialistId = a.AssignedSpecialistId,
        CreatedBy = a.CreatedBy,
        UpdatedBy = a.UpdatedBy,
        Version = a.Version,
        UpdatedAt = a.UpdatedAt
    };
}
