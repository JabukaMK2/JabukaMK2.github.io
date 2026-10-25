namespace RepairIQ;

// The text RepairIQ shows the tech in each test's Results card. Kept in one place so the wording
// stays consistent, and so the scan flow in MainPage.xaml.cs only decides *what* happened.
// Messages say what happened and what to do next; internal details (scan IDs, file paths) stay out.
public sealed partial class MainPage
{
    // Results card text for each status string the scan saves (the same strings StatusToStepState reads).
    private static string StorageMessage(string? status) => status switch
    {
        "Pass" => "Every drive reported healthy.",
        "Fail" => "At least one drive reported a problem. Back up the customer's data before doing anything else on this PC.",
        "Unknown" => "Windows couldn't report drive health, so the drives weren't checked. Check them manually.",
        _ => "The storage test didn't run.",
    };

    private static string Prime95Message(string? status) => status switch
    {
        "Pass" => "The processor and memory stayed stable through the whole stress test.",
        "Fail" => "Prime95 found errors under load. This usually points to bad RAM, an unstable CPU, or overheating.",
        "Unknown" => "Prime95 couldn't finish (it may be missing), so the processor and memory weren't fully tested.",
        _ => "Prime95 didn't run for this scan.",
    };

    private static string AntivirusMessage(string? status) => status switch
    {
        "Pass" => "Microsoft Defender finished the scan and found no threats.",
        "Attention" => "Defender found something that needs a tech to review. Open Windows Security > Protection history.",
        "Unknown" => "Defender couldn't finish the scan. It may be missing or turned off on this PC.",
        _ => "The antivirus scan didn't run.",
    };

    private static string SoftwareMessage(string? status) => status switch
    {
        "Pass" => "Temp files were cleaned, and DISM, SFC, and Check Disk all finished without problems.",
        "Temp" => "Temp files couldn't be deleted, so the Windows repair stopped before DISM, SFC, and Check Disk.",
        "DISM" => "DISM couldn't repair the Windows image, so SFC and Check Disk weren't run.",
        "SFC" => "System File Checker couldn't finish, so Check Disk wasn't run.",
        "Chkdsk" => "Check Disk couldn't finish. Temp cleanup, DISM, and SFC completed.",
        _ => "The Windows repair ended with an unexpected result. Open Advanced below to see the full log.",
    };

    // A step's script couldn't run at all (missing file or an unexpected error). The card gets a plain
    // sentence; the technical error goes to the Advanced terminal, where it's there if someone needs it.
    private string ScriptErrorMessage(string testName, Exception ex)
    {
        LogErrorDetails(ex);
        return ex is FileNotFoundException
            ? $"The {testName} script is missing, so this test couldn't run. Make sure ARC's Services folder is complete, then scan again."
            : $"The {testName} couldn't run because of an unexpected error. Open Advanced below for the details.";
    }

    // Writes an exception's technical details to the Advanced terminal instead of the tech-facing text.
    private void LogErrorDetails(Exception ex)
    {
        AppendOutput($"[RepairIQ error] {ex.GetType().Name}: {ex.Message}");
    }
}
