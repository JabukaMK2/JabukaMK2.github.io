using ArcShared;
using System.Diagnostics;

namespace RepairIQ;

public sealed partial class MainPage : Page
{
    private readonly SessionContext _session;

    public MainPage()
    {
        this.InitializeComponent();

        _session = SessionStore.Load();
        TargetText.Text = $"Target: {_session.DriveLetter} ({_session.Hostname}, {_session.Mode})";
    }

    private void RunScanButton_Click(object sender, RoutedEventArgs e)
    {
        string scriptPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Services", "main.cmd");
        if (!System.IO.File.Exists(scriptPath))
        {
            LogText.Text = $"Could not find {scriptPath}.";
            return;
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = true,
                WorkingDirectory = System.IO.Path.GetDirectoryName(scriptPath)
            }
        };
        process.Start();
        LogText.Text = "Running scan...";
    }
}
