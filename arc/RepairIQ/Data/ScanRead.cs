using Microsoft.Data.Sqlite;

namespace RepairIQ.Data;

public static class ScanRead
{

    // Reads a scan report from the database based on the provided scan ID. Returns null if no report is found.
    public static ScanReport? ReadScan(long scanId)
    {
        using var connection = ScanStore.OpenConnection();
        using var getScanCommand = connection.CreateCommand();
        getScanCommand.CommandText = @"
        SELECT Scans.ScanID, Scans.WorkOrder, Scans.ScanStart, Scans.ScanEnd, 
        Hardware.HardwareEnd, Hardware.Prime95Status, Hardware.StorageStatus,
        Software.SoftwareEnd, Software.OSFailPoint,
        Antivirus.AntivirusEnd, Antivirus.VirusStatus
        FROM Scans
        LEFT JOIN Hardware ON Scans.ScanID = Hardware.ScanID
        LEFT JOIN Software ON Scans.ScanID = Software.ScanID
        LEFT JOIN Antivirus ON Scans.ScanID = Antivirus.ScanID
        WHERE Scans.ScanID = @scanId
        ";
        getScanCommand.Parameters.AddWithValue("@scanId", scanId);
        using var reader = getScanCommand.ExecuteReader();

        // Attempt to read the scan report from the database.
        if (reader.Read())
        {
            ScanReport report = new ScanReport
            {
                ScanId = (long)reader["ScanID"],
                WorkOrder = (long)reader["WorkOrder"],
                ScanStart = DateTime.ParseExact(reader["ScanStart"].ToString() ?? throw new InvalidOperationException("ScanStart cannot be null."), "yyyy-MM-dd HH:mm:ss", null),
                ScanEnd = reader["ScanEnd"] == DBNull.Value ? null : DateTime.ParseExact(reader["ScanEnd"].ToString() ?? throw new InvalidOperationException("ScanEnd cannot be null."), "yyyy-MM-dd HH:mm:ss", null),
                HardwareEnd = reader["HardwareEnd"] == DBNull.Value ? null : DateTime.ParseExact(reader["HardwareEnd"].ToString() ?? throw new InvalidOperationException("HardwareEnd cannot be null."), "yyyy-MM-dd HH:mm:ss", null),
                Prime95Status = reader["Prime95Status"] as string,
                StorageStatus = reader["StorageStatus"] as string,
                SoftwareEnd = reader["SoftwareEnd"] == DBNull.Value ? null : DateTime.ParseExact(reader["SoftwareEnd"].ToString() ?? throw new InvalidOperationException("SoftwareEnd cannot be null."), "yyyy-MM-dd HH:mm:ss", null),
                OSFailPoint = reader["OSFailPoint"] as string,
                AntivirusEnd = reader["AntivirusEnd"] == DBNull.Value ? null : DateTime.ParseExact(reader["AntivirusEnd"].ToString() ?? throw new InvalidOperationException("AntivirusEnd cannot be null."), "yyyy-MM-dd HH:mm:ss", null),
                VirusStatus = reader["VirusStatus"] as string
            };
            return report;
        }
        return null;
    }
}