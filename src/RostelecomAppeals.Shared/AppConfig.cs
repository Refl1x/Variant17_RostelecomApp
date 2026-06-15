namespace RostelecomAppeals.Shared;

public sealed class AppConfig
{
    public string SupabaseRestUrl { get; set; } = "https://dhltigzsmoymvbwzjlqn.supabase.co/rest/v1";
    public string SupabaseAuthUrl { get; set; } = "https://dhltigzsmoymvbwzjlqn.supabase.co/auth/v1";
    public string SupabasePublishableKey { get; set; } = "sb_publishable_mFbd7Pkv0RXBSyNZLt-1BQ_rSRjS9cx";
    public string AppName { get; set; } = "Ростелеком Обращения";
    public string DeviceName { get; set; } = Environment.MachineName;

    public static AppConfig FromProjectUrl(string projectUrlOrRestUrl, string publishableKey)
    {
        var raw = projectUrlOrRestUrl.Trim().TrimEnd('/');
        var baseUrl = raw;
        if (baseUrl.EndsWith("/rest/v1", StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl[..^"/rest/v1".Length];
        return new AppConfig
        {
            SupabaseRestUrl = baseUrl + "/rest/v1",
            SupabaseAuthUrl = baseUrl + "/auth/v1",
            SupabasePublishableKey = publishableKey
        };
    }
}
