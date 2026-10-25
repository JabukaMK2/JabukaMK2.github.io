using System.Text;
using ArcShared;
using RepairIQ.Data;

namespace RepairIQ;

// How one individual test turned out, in customer-friendly terms.
public enum TestOutcome { Passed, Failed, NeedsAttention, NotRun, CouldNotTest }

// One test line on the summary, e.g. ("Hardware", "Drive health check", Passed).
public sealed record SummaryTest(string Component, string Name, TestOutcome Outcome);

// Turns a saved scan (the ScanReport read back from the database) into a summary for the
// customer's work order: one line per individual test, an overall verdict, and short notes.
public sealed class ScanSummary
{
    public ScanReport Report { get; }
    public IReadOnlyList<SummaryTest> Tests { get; }

    public ScanSummary(ScanReport report)
    {
        Report = report;
        Tests = BuildTests(report);
    }

    // Number of tests that found a problem (failed or need a tech to look at them).
    public int IssueCount => Tests.Count(t => t.Outcome is TestOutcome.Failed or TestOutcome.NeedsAttention);

    // The one-line verdict shown at the top of the page and the notes.
    public string Overall => IssueCount switch
    {
        0 when Tests.All(t => t.Outcome is TestOutcome.NotRun or TestOutcome.CouldNotTest)
            => "None of the tests finished, so nothing was checked.",
        0 when Tests.Any(t => t.Outcome is TestOutcome.NotRun or TestOutcome.CouldNotTest)
            => "No problems found, but some tests didn't run.",
        0 => "All tests passed.",
        1 => "1 issue found.",
        _ => $"{IssueCount} issues found.",
    };

    // One line per test, grouped by component. Software is split into its four repair steps
    // using OSFailPoint: everything before the fail point passed, the fail point failed,
    // and anything after it never ran.
    private static List<SummaryTest> BuildTests(ScanReport report)
    {
        var tests = new List<SummaryTest>
        {
            new("Hardware", "Drive health check", FromStatus(report.StorageStatus)),
            new("Hardware", "CPU and memory stress test (Prime95)", FromStatus(report.Prime95Status)),
            new("Security", "Virus scan (Microsoft Defender)", FromStatus(report.VirusStatus)),
        };

        string[] repairPoints = ["Temp", "DISM", "SFC", "Chkdsk"];
        string[] repairNames =
        [
            "Temporary file cleanup",
            "Windows image repair (DISM)",
            "System file check (SFC)",
            "Disk check (CHKDSK)",
        ];
        var failIndex = Array.IndexOf(repairPoints, report.OSFailPoint);

        for (var i = 0; i < repairPoints.Length; i++)
        {
            var outcome = report.OSFailPoint switch
            {
                null => TestOutcome.NotRun,
                "Pass" => TestOutcome.Passed,
                _ when failIndex < 0 => TestOutcome.CouldNotTest,   // "Unknown" or anything unexpected
                _ when i < failIndex => TestOutcome.Passed,
                _ when i == failIndex => TestOutcome.Failed,
                _ => TestOutcome.NotRun,
            };
            tests.Add(new("Windows repair", repairNames[i], outcome));
        }

        return tests;
    }

    // The status words saved in the Hardware and Antivirus tables.
    private static TestOutcome FromStatus(string? status) => status switch
    {
        "Pass" => TestOutcome.Passed,
        "Fail" => TestOutcome.Failed,
        "Attention" => TestOutcome.NeedsAttention,
        "Unknown" => TestOutcome.CouldNotTest,
        _ => TestOutcome.NotRun,   // "Skipped", or no row because the step never ran
    };

    public static string OutcomeText(TestOutcome outcome) => outcome switch
    {
        TestOutcome.Passed => "Passed",
        TestOutcome.Failed => "Failed",
        TestOutcome.NeedsAttention => "Needs attention",
        TestOutcome.CouldNotTest => "Could not be tested",
        _ => "Not run",
    };

    // Short, plain-language explanations for anything that didn't pass.
    public IEnumerable<string> Notes()
    {
        if (Tests[0].Outcome == TestOutcome.Failed)
        {
            yield return "A drive reported a health problem. We recommend backing up your data and replacing the drive.";
        }
        if (Tests[1].Outcome == TestOutcome.Failed)
        {
            yield return "The stress test found errors under load. This usually points to faulty memory, an unstable processor, or overheating.";
        }
        if (Tests[2].Outcome == TestOutcome.NeedsAttention)
        {
            yield return "The virus scan found items that a technician needs to review.";
        }
        if (Tests.FirstOrDefault(t => t.Component == "Windows repair" && t.Outcome == TestOutcome.Failed) is { } repair)
        {
            yield return $"The Windows repair could not complete the {char.ToLowerInvariant(repair.Name[0])}{repair.Name[1..]} step.";
        }
        if (Tests.Any(t => t.Outcome == TestOutcome.NotRun) && Tests.Take(2).Any(t => t.Outcome == TestOutcome.Failed))
        {
            yield return "Some tests were not run because a hardware test failed.";
        }
        if (Tests.Any(t => t.Outcome == TestOutcome.CouldNotTest))
        {
            yield return "Some tests could not be completed and may need to be checked by hand.";
        }
    }

    // The plain-text version for pasting into the customer's work order.
    public string ToWorkOrderText(SessionContext session)
    {
        var text = new StringBuilder();
        var finished = Report.ScanEnd ?? Report.ScanStart;

        text.AppendLine($"Diagnostic summary - Work order #{Report.WorkOrder}");
        text.AppendLine($"Completed {finished:MMM d, yyyy} at {finished:h:mm tt}");

        var device = DeviceLine(session);
        if (device.Length > 0)
        {
            text.AppendLine($"Device: {device}");
        }

        text.AppendLine();
        text.AppendLine($"Result: {Overall}");

        foreach (var component in Tests.GroupBy(t => t.Component))
        {
            text.AppendLine();
            text.AppendLine(component.Key);
            foreach (var test in component)
            {
                text.AppendLine($"- {test.Name}: {OutcomeText(test.Outcome)}");
            }
        }

        var notes = Notes().ToList();
        if (notes.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Notes");
            foreach (var note in notes)
            {
                text.AppendLine($"- {note}");
            }
        }

        return text.ToString().TrimEnd();
    }

    // Turns a scan's start and end times into "12 min 30 sec". Shared by the scan page and the summary page.
    public static string FormatDuration(DateTime start, DateTime? end)
    {
        if (end is null)
        {
            return "an unknown time";
        }

        var duration = end.Value - start;
        return duration.TotalHours >= 1 ? $"{(int)duration.TotalHours} hr {duration.Minutes} min"
            : duration.TotalMinutes >= 1 ? $"{duration.Minutes} min {duration.Seconds} sec"
            : $"{duration.Seconds} sec";
    }

    // "Serial ABC123, Windows 11 Pro, 16 GB RAM", leaving out anything ARC couldn't read.
    private static string DeviceLine(SessionContext session)
    {
        var parts = new List<string>();
        void Add(string value, string format)
        {
            if (!string.IsNullOrWhiteSpace(value) && value != "Unknown")
            {
                parts.Add(string.Format(format, value));
            }
        }

        Add(session.SerialNumber, "Serial {0}");
        Add(session.OperatingSystem, "{0}");
        Add(session.Processor, "{0}");
        Add(session.Memory, "{0} RAM");
        return string.Join(", ", parts);
    }
}
