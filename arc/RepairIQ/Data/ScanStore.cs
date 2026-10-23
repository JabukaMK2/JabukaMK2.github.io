using Microsoft.Data.Sqlite;

namespace RepairIQ.Data;

// TODO: No automated tests. SQL lives in strings the compiler can't check (e.g. wrong table or
//       column names, CHECK values with the wrong case). Add a test project that calls each method
//       against a throwaway database. The full flow has also not been run end to end on Windows yet.
// TODO: The timestamp format "yyyy-MM-dd HH:mm:ss" is repeated at every call site in MainPage.
//       Define it once so the columns can't drift out of sortable format.
public static class ScanStore
{
    // Path to the ScanStore database file.
    public static string DatabasePath => Path.Combine(AppContext.BaseDirectory, "ScanStore.db");

    // Opens a connection to the ScanStore database with foreign key support enabled.
    public static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();

        return connection;
    }

    // Creates the necessary tables for the ScanStore database if they do not already exist.
    // TODO: IF NOT EXISTS won't apply schema changes to tables that already exist in a .db file.
    //        Fine for development (delete the .db); research "schema migration" before shipping.
    public static void CreateTables()
    {
        using var connection = OpenConnection();

        // Create Scans table
        using var createTableCommand = connection.CreateCommand();
        createTableCommand.CommandText = 
        @"
        CREATE TABLE IF NOT EXISTS Scans (
            ScanID INTEGER PRIMARY KEY,
            WorkOrder INTEGER NOT NULL CHECK(WorkOrder > 0),
            SerialNumber TEXT NOT NULL,
            BitlockerStatus TEXT DEFAULT 'Unknown' CHECK(BitlockerStatus IN ('Enabled', 'Disabled', 'Unknown')) NOT NULL,
            Processor TEXT,
            Memory TEXT,
            Storage TEXT,
            OperatingSystem TEXT,
            ScanStart TEXT NOT NULL,
            ScanEnd TEXT
        ) STRICT;";
        createTableCommand.ExecuteNonQuery();

        // Create Hardware table
        using var createHardwareTableCommand = connection.CreateCommand();
        createHardwareTableCommand.CommandText = 
        @"
        CREATE TABLE IF NOT EXISTS Hardware (
            HardwareID INTEGER PRIMARY KEY,
            ScanID INTEGER NOT NULL UNIQUE,
            HardwareEnd TEXT NOT NULL,
            Prime95Status TEXT DEFAULT 'Unknown' CHECK(Prime95Status IN ('Pass', 'Fail', 'Unknown', 'Skipped')) NOT NULL,
            StorageStatus TEXT DEFAULT 'Unknown' CHECK(StorageStatus IN ('Pass', 'Fail', 'Unknown', 'Skipped')) NOT NULL,
            Prime95Path TEXT,
            FOREIGN KEY (ScanID) REFERENCES Scans(ScanID)
        ) STRICT;";
        createHardwareTableCommand.ExecuteNonQuery();

        // Create Software table
        using var createSoftwareTableCommand = connection.CreateCommand();
        createSoftwareTableCommand.CommandText = 
        @"
        CREATE TABLE IF NOT EXISTS Software (
            SoftwareID INTEGER PRIMARY KEY,
            ScanID INTEGER NOT NULL UNIQUE,
            SoftwareEnd TEXT NOT NULL,
            OSFailPoint TEXT DEFAULT 'Unknown' CHECK(OSFailPoint IN ('Temp', 'DISM', 'SFC', 'Chkdsk','Pass', 'Unknown')) NOT NULL,
            FOREIGN KEY (ScanID) REFERENCES Scans(ScanID)
        ) STRICT;";
        createSoftwareTableCommand.ExecuteNonQuery();

        // Create Antivirus table
        using var createAntivirusTableCommand = connection.CreateCommand();
        createAntivirusTableCommand.CommandText = 
        @"
        CREATE TABLE IF NOT EXISTS Antivirus (
            AntivirusID INTEGER PRIMARY KEY,
            ScanID INTEGER NOT NULL UNIQUE,
            AntivirusEnd TEXT NOT NULL,
            VirusStatus TEXT DEFAULT 'Unknown' CHECK(VirusStatus IN ('Pass', 'Attention', 'Unknown', 'Skipped')) NOT NULL,
            FOREIGN KEY (ScanID) REFERENCES Scans(ScanID)
        ) STRICT;";
        createAntivirusTableCommand.ExecuteNonQuery();
    }

    // Inserts a new scan record into the Scans table.
    public static long AddScan(string serialNumber, string scanStart, string bitlockerStatus, long workOrder, string operatingSystem, string processor, string memory, string storage)
    {
        using var connection = OpenConnection();
        using var addScanCommand = connection.CreateCommand();
        addScanCommand.CommandText = 
        @"
        INSERT INTO Scans (SerialNumber, ScanStart, BitlockerStatus, WorkOrder, OperatingSystem, Processor, Memory, Storage)
        VALUES ($serialNumber, $scanStart, $bitlockerStatus, $workOrder, $operatingSystem, $processor, $memory, $storage);
        SELECT last_insert_rowid();";
        addScanCommand.Parameters.AddWithValue("$serialNumber", serialNumber);
        addScanCommand.Parameters.AddWithValue("$scanStart", scanStart);
        addScanCommand.Parameters.AddWithValue("$bitlockerStatus", bitlockerStatus);
        addScanCommand.Parameters.AddWithValue("$workOrder", workOrder);
        addScanCommand.Parameters.AddWithValue("$operatingSystem", operatingSystem);
        addScanCommand.Parameters.AddWithValue("$processor", processor);
        addScanCommand.Parameters.AddWithValue("$memory", memory);
        addScanCommand.Parameters.AddWithValue("$storage", storage);

        // Validate the result of the insert operation.
        var result = addScanCommand.ExecuteScalar() ?? throw new InvalidOperationException("Failed to insert a new scan record.");
        return Convert.ToInt64(result);
    }

    // Updates an existing scan record in the Scans table.
    public static void UpdateScan(long scanID, string scanEnd)
    {
        using var connection = OpenConnection();
        using var updateScanCommand = connection.CreateCommand();
        updateScanCommand.CommandText =
        @"
        UPDATE Scans
        SET ScanEnd = $scanEnd
        WHERE ScanID = $scanID;";
        updateScanCommand.Parameters.AddWithValue("$scanEnd", scanEnd);
        updateScanCommand.Parameters.AddWithValue("$scanID", scanID);
        if (updateScanCommand.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("Failed to update the scan record.");
        }
    }

    // Adds a hardware scan record to the Hardware table.
    public static void AddHardwareScan(long scanId, string hardwareEnd, string prime95Status, string storageStatus, string? prime95Path)
    {
        using var connection = OpenConnection();
        using var addHardwareScanCommand = connection.CreateCommand();
        addHardwareScanCommand.CommandText =
        @"
        INSERT INTO Hardware (ScanID, HardwareEnd, Prime95Status, StorageStatus, Prime95Path)
        VALUES ($scanID, $hardwareEnd, $prime95Status, $storageStatus, $prime95Path);";
        addHardwareScanCommand.Parameters.AddWithValue("$scanID", scanId);
        addHardwareScanCommand.Parameters.AddWithValue("$hardwareEnd", hardwareEnd);
        addHardwareScanCommand.Parameters.AddWithValue("$prime95Status", prime95Status);
        addHardwareScanCommand.Parameters.AddWithValue("$storageStatus", storageStatus);
        addHardwareScanCommand.Parameters.AddWithValue("$prime95Path", (prime95Path ?? (object)DBNull.Value));
        addHardwareScanCommand.ExecuteNonQuery();
    }

    // Adds a antivirus scan record to the Antivirus table.
    public static void AddAntivirusScan(long scanId, string antivirusEnd, string virusStatus)
    {
        using var connection = OpenConnection();
        using var addAntivirusScanCommand = connection.CreateCommand();
        addAntivirusScanCommand.CommandText =
        @"
        INSERT INTO Antivirus (ScanID, AntivirusEnd, VirusStatus)
        VALUES ($scanID, $antivirusEnd, $virusStatus);";
        addAntivirusScanCommand.Parameters.AddWithValue("$scanID", scanId);
        addAntivirusScanCommand.Parameters.AddWithValue("$antivirusEnd", antivirusEnd);
        addAntivirusScanCommand.Parameters.AddWithValue("$virusStatus", virusStatus);
        addAntivirusScanCommand.ExecuteNonQuery();
    }

    // Adds a software scan record to the Software table.
    public static void AddSoftwareScan(long scanId, string softwareEnd, string OSFailPoint)
    {
        using var connection = OpenConnection();
        using var addSoftwareScanCommand = connection.CreateCommand();
        addSoftwareScanCommand.CommandText =
        @"
        INSERT INTO Software (ScanID, SoftwareEnd, OSFailPoint)
        VALUES ($scanID, $softwareEnd, $OSFailPoint);";
        addSoftwareScanCommand.Parameters.AddWithValue("$scanID", scanId);
        addSoftwareScanCommand.Parameters.AddWithValue("$softwareEnd", softwareEnd);
        addSoftwareScanCommand.Parameters.AddWithValue("$OSFailPoint", OSFailPoint);
        addSoftwareScanCommand.ExecuteNonQuery();
    }
}