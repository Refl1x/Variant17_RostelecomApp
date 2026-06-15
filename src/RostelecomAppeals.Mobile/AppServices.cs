using RostelecomAppeals.Shared;

namespace RostelecomAppeals.Mobile;

public static class AppServices
{
    public static AppConfig Config { get; } = new()
    {
        SupabaseRestUrl = "https://dhltigzsmoymvbwzjlqn.supabase.co/rest/v1",
        SupabaseAuthUrl = "https://dhltigzsmoymvbwzjlqn.supabase.co/auth/v1",
        SupabasePublishableKey = "sb_publishable_mFbd7Pkv0RXBSyNZLt-1BQ_rSRjS9cx",
        DeviceName = DeviceInfo.Name
    };
    public static LocalJsonStore Store { get; } = new("RostelecomAppealsMobile");
    public static AppLogger Logger { get; } = new(Store);
    public static SupabaseClient Client { get; } = new(Config);
    public static AppealRepository Repository { get; } = new(Client, Store, Logger);
}
