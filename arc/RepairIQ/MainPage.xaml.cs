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

        // TODO: An exception here (e.g. read-only drive) crashes the page. Report it to the tech
        //        the way RunScanButton_Click does with LogText?
        ScanStore.CreateTables();
    }


    // Event handler for the "Run Scan" button click. Executes the main scan script and updates the UI with the results.
    private async void RunScanButton_Click(object sender, RoutedEventArgs e)
    {
        // Check for session work order validity before proceeding with the scan.
        if (_session.WorkOrder == 0)
        {
            LogText.Text = "Please enter a valid work order before running a system scan.";
            return;
        }

        OutputBox.Text = string.Empty;
        RunScanButton.IsEnabled = false;
        LogText.Text = "Running scan...";
        long scanId = 0;

        // Start the scan process and handle its output asynchronously.
        try
        {
            scanId = ScanStore.AddScan(
                _session.SerialNumber,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                _session.BitlockerStatus,
                _session.WorkOrder
            );


            // ==============================================================
            // Running hardware scan
            // --------------------------------------------------------------
            // Execute the hardware test script and capture its exit code.
            // ==============================================================

            int exitCode = await RunScriptAsync("hardware-test.cmd");
            
            var (storageStatus, prime95Status) = exitCode switch
            {
                // 0: All passed
                0 => ("Pass", "Pass"),

                // 1: PowerShell missing
                1 => ("Unknown", "Skipped"),

                // 2: Storage test failed
                2 => ("Fail", "Skipped"),

                // 3: Storage test error
                3 => ("Unknown", "Skipped"),

                // 4: Prime95 test failed
                4 => ("Pass", "Fail"),

                // 5: Prime95 test error
                5 => ("Pass", "Unknown"),

                // 6: Prime95 skipped
                6 => ("Pass", "Skipped"),

                // 7: Prime95 missing
                7 => ("Pass", "Unknown"),

                // Any other exit code is not defined by the hardware test script.
                _ => ("Unknown", "Unknown")
            };

            LogText.Text = $"Scan completed. Storage: {storageStatus}, Prime95: {prime95Status}";

            // Adds the hardware scan results to the scan store.
            ScanStore.AddHardwareScan(
                scanId,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                prime95Status,
                storageStatus,
                // TODO: "results.txt" is not a usable report path. hardware-test.cmd deletes
                //       Services\tools\prime95\results.txt at the start of every run, so older scans
                //       would point at a newer report, and this isn't the path relative to the app.
                //       Keep a per-scan copy of the report, or pass null until that exists.
                "results.txt"
            );

            // If either the storage or Prime95 test failed, cancel the scan.
            if (storageStatus == "Fail" || prime95Status == "Fail")
            {
                LogText.Text = $"Hardware test failed. Storage Status: {storageStatus}, Prime95 Status: {prime95Status}. Scan ID: {scanId}";
                return;
            }


            // ==============================================================
            // Running antivirus scan
            // --------------------------------------------------------------
            // Execute the antivirus test script and capture its exit code.
            // ==============================================================

            int antivirusExitCode = await RunScriptAsync("antivirus-scan.cmd");

            string antivirusStatus = antivirusExitCode switch
            {
                // 0: Antivirus test ran with no issues found
                0 => "Pass",

                // 1: Defender not found
                1 => "Unknown",

                // 2: Antivirus requires manual intervention
                2 => "Attention",

                // 3: Antivirus unexpected error
                3 => "Unknown",

                // 4: Antivirus skipped
                4 => "Skipped",
                
                // Any other exit code is not defined by the antivirus test script.
                _ => "Unknown"
            };

            LogText.Text = $"Scan completed. Antivirus: {antivirusStatus}";

            // Adds the antivirus scan results to the scan store.
            ScanStore.AddAntivirusScan(
                scanId,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                antivirusStatus
            );


            // ==============================================================
            // Running software scan
            // --------------------------------------------------------------
            // Execute the software test script and capture its exit code.
            // ==============================================================

            int softwareExitCode = await RunScriptAsync("repair-os.cmd");

            string softwareStatus = softwareExitCode switch
            {
                // 0: Everything ran properly
                0 => "Pass",

                // 1: Temp files did not clean successfully
                1 => "Temp",

                // 2: DISM did not run successfully
                2 => "DISM",

                // 3: SFC did not run successfully
                3 => "SFC",

                // 4: CheckDisk did not run successfully
                4 => "Chkdsk",

                // Any other exit code is not defined by the software test script.
                _ => "Unknown"
            };

            LogText.Text = $"Scan completed. Software: {softwareStatus}";

            // Adds the software scan results to the scan store.
            ScanStore.AddSoftwareScan(
                scanId,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                softwareStatus
            );

            // TODO: Reboot after a full successful pass (main.cmd used to run "shutdown /r").
            //       Decide where it goes now that UpdateScan runs in the finally block.
            // TODO: LogText is overwritten after each step, so the tech only sees the last result.
            //       Consider one summary of all three steps at the end.
        }


        // Catch any exceptions that occur during the scan process.
        catch (Exception ex)
        {
            LogText.Text = $"Failed to run scan: {ex.Message}";
        }

        // Ensures that the scan record is updated with the completion timestamp even if an exception occurs.
        finally
        {
            // TODO: UpdateScan can throw here (e.g. the same locked database that caused the catch above).
            //       Nothing catches exceptions thrown from finally, which can crash an async void handler.
            if (scanId != 0)
            {
                ScanStore.UpdateScan(scanId, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            RunScanButton.IsEnabled = true;
        }
    }
    
    // TODO: Scripts always run with their defaults (hardware-test MIN=10, antivirus-scan TYPE=1).
    //       There is no way yet to pass arguments, e.g. to skip a step.
    private async Task<int> RunScriptAsync(string scriptName)
    {
        string scriptPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Services","commands", scriptName);
        if (!System.IO.File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Could not find {scriptPath}.");
        }
        
        // Redirected output requires UseShellExecute = false, so the script inherits this
        // app's elevation. main.cmd needs admin, so RepairIQ itself must run elevated.
        using var process = new Process
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

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        return process.ExitCode;
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
