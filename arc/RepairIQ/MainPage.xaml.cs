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

    private async void RunScanButton_Click(object sender, RoutedEventArgs e)
    {
        string scriptPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Services", "main.cmd");
        if (!System.IO.File.Exists(scriptPath))
        {
            LogText.Text = $"Could not find {scriptPath}.";
            return;
        }

        // Redirected output requires UseShellExecute = false, so the script inherits this
        // app's elevation. main.cmd needs admin, so RepairIQ itself must run elevated.
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{scriptPath}\"\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = System.IO.Path.GetDirectoryName(scriptPath)
            },
            EnableRaisingEvents = true
        };
        process.OutputDataReceived += (_, args) => AppendOutput(args.Data);
        process.ErrorDataReceived += (_, args) => AppendOutput(args.Data);

        OutputBox.Text = string.Empty;
        RunScanButton.IsEnabled = false;
        LogText.Text = "Running scan...";

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            LogText.Text = process.ExitCode == 0
                ? "Scan finished."
                : $"Scan failed (exit code {process.ExitCode}).";
        }
        catch (Exception ex)
        {
            LogText.Text = $"Failed to run scan: {ex.Message}";
        }
        finally
        {
            process.Dispose();
            RunScanButton.IsEnabled = true;
        }
    }

    private void AppendOutput(string? line)
    {
        if (line is null)
        {
            return;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            OutputBox.Text += line + Environment.NewLine;
            OutputScroll.ChangeView(null, OutputScroll.ScrollableHeight, null);
        });
    }
}
