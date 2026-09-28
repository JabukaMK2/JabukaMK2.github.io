namespace ArcShared;

/// <summary>
/// The shared session/config state written by ARC.exe and read by RepairIQ/DataIQ.
/// Kept intentionally flat and human-readable, since it's also the capstone's
/// "Databases" artifact and gets inspected while debugging.
/// </summary>
public class SessionContext
{
    public BootMode Mode { get; set; } = BootMode.LiveOS;

    /// <summary>Drive letter of the target Windows install (e.g. "C:").</summary>
    public string DriveLetter { get; set; } = "C:";

    public string Hostname { get; set; } = Environment.MachineName;

    /// <summary>Free-form extra fields tools can stash without a schema change.</summary>
    public Dictionary<string, string> Extra { get; set; } = new();
}
