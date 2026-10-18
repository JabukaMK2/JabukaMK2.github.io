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

        // Create Scans table
        using var createTableCommand = connection.CreateCommand();
        createTableCommand.CommandText = 
        @"
        CREATE TABLE IF NOT EXISTS Scans (
            ScanID INTEGER PRIMARY KEY,
            WorkOrder INTEGER NOT NULL CHECK(WorkOrder > 0),
            SerialNumber TEXT NOT NULL,
            BitlockerStatus TEXT DEFAULT 'Unknown' CHECK(BitlockerStatus IN ('Enabled', 'Disabled', 'Unknown')) NOT NULL,
            CPU TEXT,
            RAM TEXT,
            Storage TEXT,
            OS TEXT,
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
    public static long AddScan(string serialNumber, string scanStart, string bitlockerStatus, long workOrder)
    {
        using var connection = OpenConnection();
        using var addScanCommand = connection.CreateCommand();
        addScanCommand.CommandText = 
        @"
        INSERT INTO Scans (SerialNumber, ScanStart, BitlockerStatus, WorkOrder)
        VALUES ($serialNumber, $scanStart, $bitlockerStatus, $workOrder);
        SELECT last_insert_rowid();";
        addScanCommand.Parameters.AddWithValue("$serialNumber", serialNumber);
        addScanCommand.Parameters.AddWithValue("$scanStart", scanStart);
        addScanCommand.Parameters.AddWithValue("$bitlockerStatus", bitlockerStatus);
        addScanCommand.Parameters.AddWithValue("$workOrder", workOrder);

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

    // Deletes a scan record from the Scans table.
    // #TODO: Fails with "FOREIGN KEY constraint failed" when Hardware/Software/Antivirus rows
    //        reference this scan. Decide: delete the child rows first, or ON DELETE CASCADE?
    public static void DeleteScan(long scanID)
    {
        using var connection = OpenConnection();
        using var deleteScanCommand = connection.CreateCommand();
        deleteScanCommand.CommandText =
        @"
        DELETE FROM Scans
        WHERE ScanID = $scanID;";
        deleteScanCommand.Parameters.AddWithValue("$scanID", scanID);
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
    public static SqliteDataReader GetScan(long scanID)
    {
        using var connection = OpenConnection();
        using var getScanCommand = connection.CreateCommand();
        getScanCommand.CommandText =
        @"
        SELECT * FROM Scans
        WHERE ScanID = $scanID;";
        getScanCommand.Parameters.AddWithValue("$scanID", scanID);
        var reader = getScanCommand.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException("Scan record not found.");
        }
        return reader;
    }

    // #TODO: Methods to insert/update step results (Hardware, Software, Antivirus), including Prime95Path.
    // #TODO: Choose a testing approach (temporary call from MainPage vs. an xUnit test project).
};