using System.Diagnostics;
using ArcShared;

namespace ARC;

public sealed partial class MainPage : Page
{
    private readonly SessionContext _session;

    public MainPage()
    {
        this.InitializeComponent();

        _session = SessionStore.Load();
        SessionStore.Save(_session);

        RefreshInfoPanel();
    }

    private void RefreshInfoPanel()
    {
        ModeText.Text = _session.Mode == BootMode.ArcPe ? "ARC-PE" : "Live OS";
        DriveText.Text = _session.DriveLetter;
        HostnameText.Text = _session.Hostname;
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
