using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RostelecomAppeals.Shared;

public sealed class SupabaseClient
{
    private readonly HttpClient _http;
    private readonly AppConfig _config;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AuthSession? Session { get; private set; }
    public ProfileDto? CurrentProfile { get; private set; }

    public SupabaseClient(AppConfig config, HttpClient? httpClient = null)
    {
        _config = config;
        _http = httpClient ?? new HttpClient();
    }

    public void SetSession(AuthSession? session, ProfileDto? profile = null)
    {
        Session = session;
        CurrentProfile = profile;
    }

    public async Task<AuthSession> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _config.SupabaseAuthUrl.TrimEnd('/') + "/token?grant_type=password");
        ApplyApiKey(request);
        request.Content = JsonContent.Create(new { email, password }, options: _json);
        using var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Ошибка входа");
        var session = await response.Content.ReadFromJsonAsync<AuthSession>(_json, ct) ?? throw new InvalidOperationException("Supabase не вернул сессию.");
        session.SavedAtUtc = DateTime.UtcNow;
        Session = session;
        CurrentProfile = await GetProfileByAuthUserAsync(session.User!.Id, ct);
        return session;
    }

    public async Task<AuthSession> SignUpAsync(string email, string password, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _config.SupabaseAuthUrl.TrimEnd('/') + "/signup");
        ApplyApiKey(request);
        request.Content = JsonContent.Create(new { email, password }, options: _json);
        using var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, "Ошибка создания аккаунта в Supabase Auth");
        var session = await response.Content.ReadFromJsonAsync<AuthSession>(_json, ct) ?? throw new InvalidOperationException("Supabase не вернул аккаунт.");
        session.SavedAtUtc = DateTime.UtcNow;
        return session;
    }

    public async Task<ProfileDto?> GetProfileByAuthUserAsync(Guid authUserId, CancellationToken ct = default)
    {
        var items = await GetListAsync<ProfileDto>("profiles", $"select=*,roles(*)&auth_user_id=eq.{authUserId}&limit=1", ct);
        return items.FirstOrDefault();
    }

    public async Task<List<T>> GetListAsync<T>(string tableOrView, string query = "select=*", CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, BuildRestUrl(tableOrView, query));
        ApplyAuth(request);
        using var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, $"Ошибка чтения {tableOrView}");
        return await response.Content.ReadFromJsonAsync<List<T>>(_json, ct) ?? new List<T>();
    }

    public async Task<T?> InsertAsync<T>(string table, object payload, CancellationToken ct = default) where T : class
    {
        var request = new HttpRequestMessage(HttpMethod.Post, BuildRestUrl(table, ""));
        ApplyAuth(request);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = JsonContent.Create(payload, options: _json);
        using var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, $"Ошибка добавления в {table}");
        var items = await response.Content.ReadFromJsonAsync<List<T>>(_json, ct);
        return items?.FirstOrDefault();
    }

    public async Task<T?> PatchAsync<T>(string table, string filterQuery, object payload, CancellationToken ct = default) where T : class
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, BuildRestUrl(table, filterQuery));
        ApplyAuth(request);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = JsonContent.Create(payload, options: _json);
        using var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, $"Ошибка обновления {table}");
        var items = await response.Content.ReadFromJsonAsync<List<T>>(_json, ct);
        return items?.FirstOrDefault();
    }

    public async Task DeleteAsync(string table, string filterQuery, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, BuildRestUrl(table, filterQuery));
        ApplyAuth(request);
        using var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, $"Ошибка удаления {table}");
    }

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            await GetListAsync<DictionaryItem>("appeal_types", "select=type_id&limit=1", ct);
            return true;
        }
        catch { return false; }
    }

    private string BuildRestUrl(string tableOrView, string query)
    {
        var url = _config.SupabaseRestUrl.TrimEnd('/') + "/" + tableOrView.TrimStart('/');
        if (!string.IsNullOrWhiteSpace(query)) url += "?" + query.TrimStart('?');
        return url;
    }

    private void ApplyApiKey(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("apikey", _config.SupabasePublishableKey);
    }

    private void ApplyAuth(HttpRequestMessage request)
    {
        ApplyApiKey(request);
        if (!string.IsNullOrWhiteSpace(Session?.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.AccessToken);
        else
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.SupabasePublishableKey);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string message)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"{message}. HTTP {(int)response.StatusCode}: {body}");
    }
}
