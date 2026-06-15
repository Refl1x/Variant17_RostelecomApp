using RostelecomAppeals.Shared;
using System.IO;
using System.Text.Json;

namespace RostelecomAppeals.Desktop;

public static class AppServices
{
    public static AppConfig Config { get; }
    public static LocalJsonStore Store { get; }
    public static AppLogger Logger { get; }
    public static SupabaseClient Client { get; }
    public static AppealRepository Repository { get; }
    public static ExportService Exporter { get; }

    static AppServices()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        Config = File.Exists(path)
            ? JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new AppConfig()
            : AppConfig.FromProjectUrl("https://dhltigzsmoymvbwzjlqn.supabase.co/rest/v1", "sb_publishable_mFbd7Pkv0RXBSyNZLt-1BQ_rSRjS9cx");
        Store = new LocalJsonStore("RostelecomAppealsDesktop");
        Logger = new AppLogger(Store);
        Client = new SupabaseClient(Config);
        Repository = new AppealRepository(Client, Store, Logger);
        Exporter = new ExportService();
    }
}
