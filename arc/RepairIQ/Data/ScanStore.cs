using Microsoft.Data.Sqlite;

namespace RepairIQ.Data;

public static class ScanStore
{
    public static string DatabasePath => Path.Combine(AppContext.BaseDirectory, "ScanStore.db");
    public static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();

        return connection;
    }

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
};