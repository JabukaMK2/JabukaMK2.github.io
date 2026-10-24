namespace RepairIQ.Data;
public class ScanReport
{
    public long ScanId { get; set; }

    public long WorkOrder { get; set; }

    public DateTime ScanStart { get; set; }

    public DateTime? ScanEnd { get; set; }

    public DateTime? HardwareEnd { get; set; }

    public DateTime? SoftwareEnd { get; set; }

    public DateTime? AntivirusEnd { get; set; }

    public string? Prime95Status { get; set; }

    public string? StorageStatus { get; set; }

    public string? OSFailPoint { get; set; }

    public string? VirusStatus { get; set; }
}