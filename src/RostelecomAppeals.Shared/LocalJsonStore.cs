using System.Text.Json;

namespace RostelecomAppeals.Shared;

public sealed class LocalJsonStore
{
    private readonly string _folder;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public LocalJsonStore(string appFolderName = "RostelecomAppeals")
    {
        _folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appFolderName);
        Directory.CreateDirectory(_folder);
    }

    public string GetPath(string fileName) => Path.Combine(_folder, fileName);

    public async Task SaveAsync<T>(string fileName, T data, CancellationToken ct = default)
    {
        var path = GetPath(fileName);
        await using var fs = File.Create(path);
        await JsonSerializer.SerializeAsync(fs, data, _json, ct);
    }

    public async Task<T?> LoadAsync<T>(string fileName, CancellationToken ct = default) where T : class
    {
        var path = GetPath(fileName);
        if (!File.Exists(path)) return default;
        await using var fs = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(fs, _json, ct);
    }

    public async Task AppendLineAsync(string fileName, string line, CancellationToken ct = default)
    {
        var path = GetPath(fileName);
        await File.AppendAllTextAsync(path, line + Environment.NewLine, ct);
    }
}
