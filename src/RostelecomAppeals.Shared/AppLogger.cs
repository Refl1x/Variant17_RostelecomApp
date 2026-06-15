namespace RostelecomAppeals.Shared;

public sealed class AppLogger
{
    private readonly LocalJsonStore _store;
    public AppLogger(LocalJsonStore store) => _store = store;

    public Task InfoAsync(string action, string details = "") => WriteAsync("INFO", action, details);
    public Task WarnAsync(string action, string details = "") => WriteAsync("WARNING", action, details);
    public Task ErrorAsync(string action, string details = "") => WriteAsync("ERROR", action, details);

    private Task WriteAsync(string level, string action, string details)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{level}\t{action}\t{details}";
        return _store.AppendLineAsync("operations.log", line);
    }
}
