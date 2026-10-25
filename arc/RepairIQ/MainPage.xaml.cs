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
        ResetSteps();

        // TODO: An exception here (e.g. read-only drive) crashes the page. Report it to the tech
        //        the way RunScanButton_Click does with LogText?
        ScanStore.CreateTables();
    }


    // Event handler for the "Run Scan" button click. Executes the main scan script and updates the UI with the results.
    private async void RunScanButton_Click(object sender, RoutedEventArgs e)
    {
        // Reset the UI steps to their initial state before running the scan.
        ResetSteps();
        LogText.Text = string.Empty;
        ViewSummaryButton.Visibility = Visibility.Collapsed;

        // Check for session work order validity before proceeding with the scan.
        if (_session.WorkOrder == 0)
        {
            LogText.Text = "Please enter a valid work order before running a system scan.";
            return;
        }

        LiveOutputText.Text = string.Empty;
        OutputBox.Text = string.Empty;
        RunScanButton.IsEnabled = false;
        long scanId = 0;

        // Start the scan process and handle its output asynchronously.
        try
        {
            try
            {
                SetStepState("Start", StepState.Running);
                scanId = ScanStore.AddScan(
                    _session.SerialNumber,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    _session.BitlockerStatus,
                    _session.WorkOrder,
                    _session.OperatingSystem,
                    _session.Processor,
                    _session.Memory,
                    _session.Storage
                );
                SetStepState("Start", StepState.Passed, $"Scan record saved for work order {_session.WorkOrder}.");
            }
            catch (Exception ex)
            {
                const string reason = "Skipped because the scan record couldn't be created.";
                LogErrorDetails(ex);
                SetStepState("Start", StepState.Failed,
                    "The scan record couldn't be saved, so no tests were run. Close any other copy of RepairIQ and try again.");
                SetStepState("Storage", StepState.Skipped, reason);
                SetStepState("Prime95", StepState.Skipped, reason);
                SkipRemainingSteps(reason);
                return;
            }



            // ==============================================================
            // Running hardware scan
            // --------------------------------------------------------------
            // Execute the hardware test script and capture its exit code.
            // ==============================================================
            try
            {
                SetStepState("Storage", StepState.Running);
                SetStepState("Prime95", StepState.Running);
                
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
                
                SetStepState("Storage", StatusToStepState(storageStatus), StorageMessage(storageStatus));
                SetStepState("Prime95", StatusToStepState(prime95Status), Prime95Message(prime95Status));

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
                // Storage and Prime95 already show their results above; only the later steps change here.
                if (storageStatus == "Fail" || prime95Status == "Fail")
                {
                    SkipRemainingSteps(storageStatus == "Fail"
                        ? "Skipped because the storage test failed."
                        : "Skipped because the Prime95 test failed.");
                    return;
                }
            }
            // The script couldn't run (missing file or an unexpected error). ScriptErrorMessage tells those
            // apart for the tech; either way the results are "Unknown", on screen and in the database.
            catch (Exception ex)
            {
                var message = ScriptErrorMessage("hardware test", ex);
                SetStepState("Storage", StepState.Unknown, message);
                SetStepState("Prime95", StepState.Unknown, message);
                SaveSafely(() => ScanStore.AddHardwareScan(
                    scanId, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Unknown", "Unknown", null));
                SkipRemainingSteps("Skipped because the hardware test couldn't run.");
                return;
            }
            


            // ==============================================================
            // Running antivirus scan
            // --------------------------------------------------------------
            // Execute the antivirus test script and capture its exit code.
            // ==============================================================

            try
            {
                SetStepState("Antivirus", StepState.Running);
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

                SetStepState("Antivirus", StatusToStepState(antivirusStatus), AntivirusMessage(antivirusStatus));

                // Adds the antivirus scan results to the scan store.
                ScanStore.AddAntivirusScan(
                    scanId,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    antivirusStatus
                );
            }
            // The script couldn't run: save "Unknown" so the report matches the screen, then carry on to Software.
            catch (Exception ex)
            {
                SetStepState("Antivirus", StepState.Unknown, ScriptErrorMessage("antivirus scan", ex));
                SaveSafely(() => ScanStore.AddAntivirusScan(
                    scanId, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Unknown"));
            }



            // ==============================================================
            // Running software scan
            // --------------------------------------------------------------
            // Execute the software test script and capture its exit code.
            // ==============================================================

            try
            {
                SetStepState("Software", StepState.Running);
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

                SetStepState("Software", StatusToStepState(softwareStatus), SoftwareMessage(softwareStatus));

                // Adds the software scan results to the scan store.
                ScanStore.AddSoftwareScan(
                    scanId,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    softwareStatus
                );

                // TODO: Reboot after a full successful pass (main.cmd used to run "shutdown /r").
                //       Decide where it goes now that UpdateScan runs in the finally block.
            }
            // The script couldn't run: save "Unknown" so the report matches the screen.
            catch (Exception ex)
            {
                SetStepState("Software", StepState.Unknown, ScriptErrorMessage("Windows repair", ex));
                SaveSafely(() => ScanStore.AddSoftwareScan(
                    scanId, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Unknown"));
            }
        }
        // Catch any exceptions that occur during the scan process.
        catch (Exception ex)
        {
            LogErrorDetails(ex);
            AddLogLine("The scan stopped because of an unexpected error. Open Advanced below for the details.");
        }

        // Ensures that the scan record is updated with the completion timestamp even if an exception occurs.
        finally
        {
            if (scanId != 0)
            {
                // Its own try: an exception thrown from a finally block isn't caught by anything,
                // and in an async void handler that would crash the app.
                try
                {
                    ScanStore.UpdateScan(scanId, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    // Read the saved scan back to confirm the results actually made it into the database.
                    var report = ScanRead.ReadScan(scanId);
                    if (report is null)
                    {
                        AddLogLine("The scan finished, but its results couldn't be found in the database.");
                    }
                    else
                    {
                        AddLogLine($"Results saved. The scan took {ScanSummary.FormatDuration(report.ScanStart, report.ScanEnd)}.");
                        ShowSummary(report);
                    }
                }
                catch (Exception ex)
                {
                    LogErrorDetails(ex);
                    AddLogLine("The scan finished, but its results couldn't be saved. Open Advanced below for the details.");
                }
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

    // Shows the latest line of script output under the step tracker, and appends it to the
    // full terminal log inside the "Advanced" expander.
    private void AppendOutput(string? line)
    {
        if (line is null)
        {
            return;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                LiveOutputText.Text = line;
            }
            OutputBox.Text += line + Environment.NewLine;
            OutputScroll.ChangeView(null, OutputScroll.ScrollableHeight, null);
        });
    }

    // Converts a status string to the corresponding StepState enum value.
    private StepState StatusToStepState(string? step)
    {
        return step switch
        {
            "Pass" => StepState.Passed,
            "Fail" or "Temp" or "DISM" or "SFC" or "Chkdsk" => StepState.Failed,
            "Attention" => StepState.Warning,
            "Unknown" => StepState.Unknown,
            "Skipped" => StepState.Skipped,
            null => StepState.Skipped,
            _ => StepState.Unknown
        };
    }

    // Marks the steps that never got to run (Antivirus and Software) as skipped, with the reason in their cards.
    private void SkipRemainingSteps(string message)
    {
        SetStepState("Software", StepState.Skipped, message);
        SetStepState("Antivirus", StepState.Skipped, message);
    }

    // The last saved scan, so "View summary" can reopen its summary after the tech goes back.
    private ScanReport? _lastReport;

    // Opens the summary page for a saved scan (built from the report read back from the database).
    private void ShowSummary(ScanReport report)
    {
        _lastReport = report;
        ViewSummaryButton.Visibility = Visibility.Visible;
        Frame.Navigate(typeof(ScanSummaryPage), new ScanSummaryArgs(report, _session));
    }

    private void ViewSummaryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastReport is not null)
        {
            ShowSummary(_lastReport);
        }
    }

    // Saves a step's result from inside a catch block. If the database is what failed, saving again
    // would throw a second time and abandon the rest of the scan, so that error goes to Advanced instead.
    private void SaveSafely(Action save)
    {
        try
        {
            save();
        }
        catch (Exception ex)
        {
            LogErrorDetails(ex);
        }
    }

    // Adds a line under the progress bar without erasing what's already there
    // (so an error from the catch block isn't replaced by the summary in finally).
    private void AddLogLine(string message)
    {
        LogText.Text = string.IsNullOrEmpty(LogText.Text) ? message : $"{LogText.Text}{Environment.NewLine}{message}";
    }
}
