namespace ArcShared;


// The shared session/config state written by ARC.exe and read by RepairIQ/DataIQ.
// Kept intentionally flat and human-readable, since it's also the capstone's
// "Databases" artifact and gets inspected while debugging.

public class SessionContext
{
    public BootMode Mode { get; set; } = BootMode.LiveOS;

    // Drive letter of the target Windows install (e.g. "C:").
    public string DriveLetter { get; set; } = "C:";

    // Hostname of the system.
    public string Hostname { get; set; } = Environment.MachineName;

    // Serial number of the system's BIOS.
    public string SerialNumber { get; set; } = "Unknown";

    // BitLocker protection status of the system drive.
    public string BitlockerStatus { get; set; } = "Unknown";

    // Indicates whether the system is running Windows.
    public string OperatingSystem { get; set; } = "Unknown";

    // Processor name of the system.
    public string Processor { get; set; } = "Unknown";

    // Total physical memory of the system.
    public string Memory { get; set; } = "Unknown";

    // Total storage size of the system.
    public string Storage { get; set; } = "Unknown";

    // Work order associated with this session. If not set, defaults to 0 and if set, must be greater than 0.
    public long WorkOrder { get; set; } = 0;

    // Free-form extra fields tools can stash without a schema change.
    public Dictionary<string, string> Extra { get; set; } = new();
}
