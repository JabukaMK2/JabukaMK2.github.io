using Microsoft.Data.Sqlite;
using System.IO;

namespace RepairIQ.Data;

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

        // Create Devices table
        using var createDevicesTableCommand = connection.CreateCommand();
        createDevicesTableCommand.CommandText = 
        @"
        CREATE TABLE IF NOT EXISTS Devices (
            SerialNumber TEXT PRIMARY KEY,
            BitlockerStatus TEXT CHECK(BitlockerStatus IN ('Enabled', 'Disabled', 'Unknown')) NOT NULL,
            CPU TEXT,
            RAM TEXT,
            Storage TEXT,
            OS TEXT
        ) STRICT;";
        createDevicesTableCommand.ExecuteNonQuery();

        // Create Scans table
        using var createTableCommand = connection.CreateCommand();
        createTableCommand.CommandText = 
        @"
        CREATE TABLE IF NOT EXISTS Scans (
            ScanID INTEGER PRIMARY KEY,
            SerialNumber TEXT NOT NULL,
            ScanStart TEXT NOT NULL,
            ScanEnd TEXT,
            FOREIGN KEY (SerialNumber) REFERENCES Devices(SerialNumber)
        );";
        createTableCommand.ExecuteNonQuery();

        // Create Hardware table
        using var createHardwareTableCommand = connection.CreateCommand();
        createHardwareTableCommand.CommandText = 
        @"
        CREATE TABLE IF NOT EXISTS Hardware (
            HardwareID INTEGER PRIMARY KEY,
            ScanID INTEGER NOT NULL UNIQUE,
            HardwareStart TEXT NOT NULL,
            HardwareEnd TEXT,
            CPUStatus TEXT DEFAULT 'Pending' CHECK(CPUStatus IN ('Pass', 'Fail', 'Unknown', 'Pending')) NOT NULL,
            RAMStatus TEXT DEFAULT 'Pending' CHECK(RAMStatus IN ('Pass', 'Fail', 'Unknown', 'Pending')) NOT NULL,
            StorageStatus TEXT DEFAULT 'Pending' CHECK(StorageStatus IN ('Pass', 'Fail', 'Unknown', 'Pending')) NOT NULL,
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
            SoftwareStart TEXT NOT NULL,
            SoftwareEnd TEXT,
            OSStatus TEXT DEFAULT 'Pending' CHECK(OSStatus IN ('Repaired', 'Incomplete', 'Clean', 'Unknown', 'Pending')) NOT NULL,
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
            AntivirusStart TEXT NOT NULL,
            AntivirusEnd TEXT,
            VirusCount INTEGER CHECK(VirusCount >= 0),
            VirusStatus TEXT DEFAULT 'Pending' CHECK(VirusStatus IN ('Complete', 'Incomplete', 'Unknown', 'Pending')) NOT NULL,
            FOREIGN KEY (ScanID) REFERENCES Scans(ScanID)
        ) STRICT;";
        createAntivirusTableCommand.ExecuteNonQuery();
    }

    // Inserts a new scan record into the Scans table.
    public static long AddScan(string serialNumber, string scanStart)
    {
        using var connection = OpenConnection();
        using var addScanCommand = connection.CreateCommand();
        addScanCommand.CommandText = 
        @"
        INSERT INTO Scans (SerialNumber, ScanStart)
        VALUES ($serialNumber, $scanStart);
        SELECT last_insert_rowid();";
        addScanCommand.Parameters.AddWithValue("$serialNumber", serialNumber);
        addScanCommand.Parameters.AddWithValue("$scanStart", scanStart);

        // Validate the result of the insert operation.
        var result = addScanCommand.ExecuteScalar() ?? throw new InvalidOperationException("Failed to insert a new scan record.");
        return Convert.ToInt64(result);
    }

    // Updates an existing scan record in the Scans table.
    public static void UpdateScan(long scanId, string scanEnd)
    {
        using var connection = OpenConnection();
        using var updateScanCommand = connection.CreateCommand();
        updateScanCommand.CommandText =
        @"
        UPDATE Scans
        SET ScanEnd = $scanEnd
        WHERE ScanID = $scanId;";
        updateScanCommand.Parameters.AddWithValue("$scanEnd", scanEnd);
        updateScanCommand.Parameters.AddWithValue("$scanId", scanId);
        if (updateScanCommand.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("Failed to update the scan record.");
        }
    }

    // Deletes a scan record from the Scans table.
    // #TODO: Fails with "FOREIGN KEY constraint failed" when Hardware/Software/Antivirus rows
    //        reference this scan. Decide: delete the child rows first, or ON DELETE CASCADE?
    public static void DeleteScan(long scanId)
    {
        using var connection = OpenConnection();
        using var deleteScanCommand = connection.CreateCommand();
        deleteScanCommand.CommandText =
        @"
        DELETE FROM Scans
        WHERE ScanID = $scanId;";
        deleteScanCommand.Parameters.AddWithValue("$scanId", scanId);
        if (deleteScanCommand.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("Failed to delete the scan record.");
        }
    }

    // Retrieves a scan record from the Scans table by scan ID.
    // #TODO: Move to a dedicated ScanRepository class.
    // #TODO: The returned reader is already closed, because `using` disposes the connection on return
    //        ("Invalid attempt to call ... when reader is closed"). Return a Scan model instead
    //        (compare SessionStore.Load() returning SessionContext).
    public static SqliteDataReader GetScan(long scanId)
    {
        using var connection = OpenConnection();
        using var getScanCommand = connection.CreateCommand();
        getScanCommand.CommandText =
        @"
        SELECT * FROM Scans
        WHERE ScanID = $scanId;";
        getScanCommand.Parameters.AddWithValue("$scanId", scanId);
        var reader = getScanCommand.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException("Scan record not found.");
        }
        return reader;
    }

    // Checks if a device exists and updates it otherwise inserts a new device record.
    // #TODO: Currently causes an error if any of the optional parameters are null.
    //        ("Value must be set": C# null is not SQL NULL. Research DBNull.Value.)
    // #TODO: The OS column is not included. Intentional?
    // #TODO: Decide whether a failed poll (null) should overwrite the last known value (COALESCE?).
    public static void UpsertDevice(string serialNumber, string bitlockerStatus, string? cpu, string? ram, string? storage)
    {
        using var connection = OpenConnection();
        using var upsertDeviceCommand = connection.CreateCommand();
        upsertDeviceCommand.CommandText =
        @"
        INSERT INTO Devices (SerialNumber, BitlockerStatus, CPU, RAM, Storage)
        VALUES ($serialNumber, $bitlockerStatus, $cpu, $ram, $storage)
        ON CONFLICT(SerialNumber) DO UPDATE SET BitlockerStatus = $bitlockerStatus, CPU = $cpu, RAM = $ram, Storage = $storage;";
        upsertDeviceCommand.Parameters.AddWithValue("$serialNumber", serialNumber);
        upsertDeviceCommand.Parameters.AddWithValue("$bitlockerStatus", bitlockerStatus);
        upsertDeviceCommand.Parameters.AddWithValue("$cpu", cpu);
        upsertDeviceCommand.Parameters.AddWithValue("$ram", ram);
        upsertDeviceCommand.Parameters.AddWithValue("$storage", storage);
        if (upsertDeviceCommand.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("Failed to upsert the device record.");
        }
    }

    // #TODO: Methods to insert/update step results (Hardware, Software, Antivirus), including Prime95Path.
    // #TODO: Choose a testing approach (temporary call from MainPage vs. an xUnit test project).
};