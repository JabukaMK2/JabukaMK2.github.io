using System.Management;

namespace ARC.Data;

// Provides methods to scan system information that require elevated privileges
public static class SystemScan
{

    // Retrieves the system's BIOS serial number, requires elevated privileges
    public static string GetSerialNumber()
    {

        // Return "Unknown" if the operating system is not Windows
        if (!OperatingSystem.IsWindows())
            return "Unknown";

        // Ensure the method is executed with elevated privileges
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
            foreach (var obj in searcher.Get())
            {

                // Return the serial number if available
                if (!string.IsNullOrWhiteSpace(obj["SerialNumber"].ToString()))
                    return obj["SerialNumber"]?.ToString() ?? "Unknown";
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    // Retrieves the BitLocker protection status, requires elevated privileges
    public static string GetBitlockerStatus(string driveLetter)
    {

        // 
        if (!OperatingSystem.IsWindows())
            return "Unknown";

        // Try to retrieve the BitLocker protection status for the specified drive
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\CIMV2\Security\MicrosoftVolumeEncryption", 
                "SELECT ProtectionStatus FROM Win32_EncryptableVolume" + 
                $" WHERE DriveLetter = '{driveLetter}'");

            // If no results are returned, the drive may not be BitLocker-protected
            foreach (var obj in searcher.Get())
            {

                // Return the protection status if available
                return (obj["ProtectionStatus"]?.ToString()) switch
                {
                    "0" => "Disabled",
                    "1" => "Enabled",
                    _ => "Unknown",
                };
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    // Retrieves the hostname of the computer, requires elevated privileges
    public static string GetHostname()
    {
        if (!OperatingSystem.IsWindows())
            return "Unknown";

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
            {
                if (!string.IsNullOrWhiteSpace(obj["Name"]?.ToString()))
                    return obj["Name"]?.ToString() ?? "Unknown";
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    // Retrieves the processor name, requires elevated privileges
    public static string GetProcessor()
    {
        if (!OperatingSystem.IsWindows())
            return "Unknown";

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                if (!string.IsNullOrWhiteSpace(obj["Name"]?.ToString()))
                    return obj["Name"]?.ToString() ?? "Unknown";
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    // Retrieves the total storage size of the system, requires elevated privileges
    public static string GetStorage()
    {
        if (!OperatingSystem.IsWindows())
            return "Unknown";

        try
        {
            List<string> storageInfo = new List<string>();
            using var searcher = new ManagementObjectSearcher($"SELECT Model, Size FROM Win32_DiskDrive WHERE MediaType = 'Fixed hard disk media'");
            foreach (var obj in searcher.Get())
            {
                string model = "Unknown";
                string size = "Unknown";

                // Prefer the model name if available
                if (obj["Model"] != null)
                    model = obj["Model"]?.ToString() ?? "Unknown";

                // Fallback to the size if the model is not available
                if (obj["Size"] != null)
                    size = FormatGigabytes(obj["Size"]?.ToString() ?? "Unknown");

                storageInfo.Add(model != "Unknown" ? model : size);
            }
            return storageInfo.Count > 0 ? string.Join(", ", storageInfo) : "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    // Retrieves the total physical memory of the system, requires elevated privileges
    public static string GetMemory()
    {
        if (!OperatingSystem.IsWindows())
            return "Unknown";

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
            {
                if (obj["TotalPhysicalMemory"] != null)
                    return FormatGigabytes(obj["TotalPhysicalMemory"]?.ToString() ?? "Unknown");
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    // Retrieves the operating system caption, requires elevated privileges
    // TODO: Add logic for booting to ARC-PE if necessary
    public static string GetOperatingSystem()
    {
        if (!OperatingSystem.IsWindows())
            return "Unknown";

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get())
            {
                if (!string.IsNullOrWhiteSpace(obj["Caption"]?.ToString()))
                    return obj["Caption"]?.ToString() ?? "Unknown";
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    // Retrieves the drive letter of the primary logical disk, requires elevated privileges
    public static string GetDriveLetter()
    {
        if (!OperatingSystem.IsWindows())
            return "Unknown";

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SystemDrive FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get())
            {
                if (!string.IsNullOrWhiteSpace(obj["SystemDrive"]?.ToString()))
                    return obj["SystemDrive"]?.ToString() ?? "Unknown";
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    // Helper method to format bytes into gigabytes with one decimal place
    public static string FormatGigabytes(string bytes)
    {
        if (double.TryParse(bytes, out var byteValue))
        {
            return Math.Round(byteValue / 1024 / 1024 / 1024, 1).ToString() + " GB";
        }
        return "Unknown";
    }
}