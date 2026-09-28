using System.Text.Json;

namespace ArcShared;

/// <summary>
/// Reads/writes the shared session JSON file that ARC.exe, RepairIQ, and DataIQ
/// use to pass context between their separate processes.
/// </summary>
public static class SessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Default location: session.json next to the running exe. All three tools
    /// are expected to ship side-by-side (on the flash drive or in ARC-PE), so a
    /// relative path avoids hardcoding a drive letter.
    /// </summary>
    public static string DefaultPath =>
        Path.Combine(AppContext.BaseDirectory, "session.json");

    public static SessionContext Load(string? path = null)
    {
        path ??= DefaultPath;

        if (!File.Exists(path))
            return new SessionContext();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SessionContext>(json) ?? new SessionContext();
    }

    public static void Save(SessionContext context, string? path = null)
    {
        path ??= DefaultPath;

        var json = JsonSerializer.Serialize(context, JsonOptions);
        File.WriteAllText(path, json);
    }
}
