using System.Diagnostics;
using ArcShared;
using ARC.Data;

namespace ARC;

public sealed partial class MainPage : Page
{
    private readonly SessionContext _session;

    public MainPage()
    {
        this.InitializeComponent();

        _session = SessionStore.Load();
        _session.SerialNumber = SystemScan.GetSerialNumber();
        _session.DriveLetter = SystemScan.GetDriveLetter();
        _session.BitlockerStatus = SystemScan.GetBitlockerStatus(_session.DriveLetter);
        _session.OperatingSystem = SystemScan.GetOperatingSystem();
        _session.Processor = SystemScan.GetProcessor();
        _session.Memory = SystemScan.GetMemory();
        _session.Storage = SystemScan.GetStorage();
        _session.Hostname = SystemScan.GetHostname();
        SessionStore.Save(_session);

        RefreshInfoPanel();
    }

    private void RefreshInfoPanel()
    {
        ModeText.Text = _session.Mode == BootMode.ArcPe ? "ARC-PE" : "Live OS";
        DriveText.Text = _session.DriveLetter;
        HostnameText.Text = _session.Hostname;
        BitlockerText.Text = _session.BitlockerStatus;
        SerialNumberText.Text = _session.SerialNumber;
        OperatingSystemText.Text = _session.OperatingSystem;
        ProcessorText.Text = _session.Processor;
        MemoryText.Text = _session.Memory;
        StorageText.Text = _session.Storage;
    }

    private void RepairIQButton_Click(object sender, RoutedEventArgs e) =>
        LaunchTool("RepairIQ.exe");

    private void DataIQButton_Click(object sender, RoutedEventArgs e) =>
        LaunchTool("DataIQ.exe");

    private void UninstallerButton_Click(object sender, RoutedEventArgs e) =>
        LaunchTool(Path.Combine("RevoUninstaller", "RevoUPort.exe"));

    private void LaunchTool(string relativeExePath)
    {
        var exePath = Path.Combine(AppContext.BaseDirectory, relativeExePath);

        if (!File.Exists(exePath))
        {
            StatusText.Text = $"Could not find {relativeExePath}.";
            return;
        }

        SessionStore.Save(_session);
        Process.Start(new ProcessStartInfo(exePath)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exePath)
        });
        StatusText.Text = string.Empty;
    }
}
