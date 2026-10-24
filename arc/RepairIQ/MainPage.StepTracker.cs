namespace RepairIQ;

// The step tracker part of RepairIQ's MainPage: the bubbles, report cards, and the
// SetStepState/ResetSteps helpers. It's a partial class, so this is still the same MainPage;
// it just lives in its own file to keep the UI code apart from the scan flow in MainPage.xaml.cs.
public sealed partial class MainPage
{
    // ==============================================================
    // Step tracker (UI only)
    // --------------------------------------------------------------
    // Call SetStepState from the scan flow to update a bubble and its report card, e.g.:
    //   SetStepState("Storage", StepState.Running);
    //   SetStepState("Storage", StepState.Failed, "One or more disks reported unhealthy.");
    // Step names: Start, Storage, Prime95, Antivirus, Software, Bitlocker.
    // States: Waiting, Running, Passed (check), Failed (X), Warning (!), Unknown (?), Skipped (-).
    // ==============================================================

    public enum StepState { Waiting, Running, Passed, Failed, Warning, Unknown, Skipped }

    private sealed record StepView(Border Bubble, ProgressRing Ring, FontIcon Icon, TextBlock Status, TextBlock Detail);

    private Dictionary<string, StepView>? _steps;

    // Phosphor regular glyphs for each finished state
    private const string CheckGlyph = "\uE182";
    private const string XGlyph = "\uE4F6";
    private const string WarningGlyph = "\uE4E0";
    private const string MinusGlyph = "\uE32A";
    private const string QuestionGlyph = "\uE3E9";

    private Dictionary<string, StepView> Steps => _steps ??= new()
    {
        ["Start"] = new(StartBubble, StartRing, StartIcon, StartStatus, StartDetail),
        ["Storage"] = new(StorageBubble, StorageRing, StorageIcon, StorageStatus, StorageDetail),
        ["Prime95"] = new(Prime95Bubble, Prime95Ring, Prime95Icon, Prime95Status, Prime95Detail),
        ["Antivirus"] = new(AntivirusBubble, AntivirusRing, AntivirusIcon, AntivirusStatus, AntivirusDetail),
        ["Software"] = new(SoftwareBubble, SoftwareRing, SoftwareIcon, SoftwareStatus, SoftwareDetail),
        ["Bitlocker"] = new(BitlockerBubble, BitlockerRing, BitlockerIcon, BitlockerStatus, BitlockerDetail),
    };

    // Puts every step back to "Waiting" (BitLocker shows as skipped: it only runs from ARC-PE).
    private void ResetSteps()
    {
        foreach (var name in Steps.Keys)
        {
            SetStepState(name, StepState.Waiting, string.Empty);
        }
        SetStepState("Bitlocker", StepState.Skipped, "Only runs when booted into ARC-PE.");
    }

    // Updates one step's bubble (spinner, check, X, ...), its status line, and its report card.
    private void SetStepState(string step, StepState state, string? detail = null)
    {
        var view = Steps[step];
        var theme = Application.Current.Resources;
        Brush Res(string key) => (Brush)theme[key];

        var (background, border, glyph, status) = state switch
        {
            StepState.Running => (Res("ArcPageBackground"), Res("ArcPrimary"), string.Empty, "In progress..."),
            StepState.Passed  => (Res("ArcSuccess"), Res("ArcSuccess"), CheckGlyph, "Passed"),
            StepState.Failed  => (Res("ArcError"), Res("ArcError"), XGlyph, "Failed"),
            StepState.Warning => (Res("ArcWarning"), Res("ArcWarning"), WarningGlyph, "Needs attention"),
            StepState.Unknown => (Res("ArcUnknown"), Res("ArcUnknown"), QuestionGlyph, "Unknown"),
            StepState.Skipped => (Res("ArcMutedSurface"), Res("ArcMuted"), MinusGlyph, "Skipped"),
            _                 => (Res("ArcPageBackground"), Res("ArcMuted"), string.Empty, "Waiting"),
        };

        view.Bubble.Background = background;
        view.Bubble.BorderBrush = border;
        view.Ring.IsActive = state == StepState.Running;
        view.Ring.Visibility = state == StepState.Running ? Visibility.Visible : Visibility.Collapsed;
        view.Icon.Glyph = glyph;
        view.Icon.Foreground = state == StepState.Skipped ? Res("ArcMuted") : new SolidColorBrush(Microsoft.UI.Colors.White);
        view.Status.Text = status;
        view.Status.Foreground = state switch
        {
            StepState.Passed  => Res("ArcSuccess"),
            StepState.Failed  => Res("ArcError"),
            StepState.Warning => Res("ArcWarning"),
            StepState.Running => Res("ArcPrimary"),
            _                 => Res("ArcTextSecondary"),
        };

        if (detail is not null)
        {
            // An empty card gets a gray placeholder so it never looks unfinished.
            var hasDetail = !string.IsNullOrWhiteSpace(detail);
            view.Detail.Text = hasDetail ? detail : "Results will appear here after this step runs.";
            view.Detail.Foreground = hasDetail ? Res("ArcTextPrimary") : Res("ArcTextSecondary");
        }
    }
}
