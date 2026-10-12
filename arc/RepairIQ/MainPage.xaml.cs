using ArcShared;
using System.Diagnostics;
using RepairIQ.Data;

namespace RepairIQ;

// Main page for the RepairIQ application. Handles UI interactions and runs the main scan script.
public sealed partial class MainPage : Page
{
    private readonly SessionContext _session;

    public MainPage()
    {
        this.InitializeComponent();

        _session = SessionStore.Load();
        TargetText.Text = $"Target: {_session.DriveLetter} ({_session.Hostname}, {_session.Mode})";

        // #TODO: An exception here (e.g. read-only drive) crashes the page. Report it to the tech
        //        the way RunScanButton_Click does with LogText?
        ScanStore.CreateTables();
    }

    // Event handler for the "Run Scan" button click. Executes the main scan script and updates the UI with the results.
    // #TODO: Record the scan in ScanStore: UpsertDevice -> AddScan (keep the returned ID) -> step results -> UpdateScan.
    private async void RunScanButton_Click(object sender, RoutedEventArgs e)
    {
        string scriptPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Services","commands", "main.cmd");
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

        // Start the scan process and handle its output asynchronously.
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
    
    // Appends a line of output from the scan process to the OutputBox in the UI.
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
