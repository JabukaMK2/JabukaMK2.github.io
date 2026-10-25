using Microsoft.UI.Xaml.Automation;

namespace RepairIQ;

// The step tracker part of RepairIQ's MainPage: the test list on the left, the details pane on the
// right, the overall progress bar, and the SetStepState/ResetSteps helpers. It's a partial class,
// so this is still the same MainPage; it just lives in its own file to keep the UI code apart from
// the scan flow in MainPage.xaml.cs.
public sealed partial class MainPage
{
    // ==============================================================
    // Step tracker (UI only)
    // --------------------------------------------------------------
    // Call SetStepState from the scan flow to update a test's row and its details, e.g.:
    //   SetStepState("Storage", StepState.Running);
    //   SetStepState("Storage", StepState.Failed, "One or more disks reported unhealthy.");
    // Step names: Start, Storage, Prime95, Antivirus, Software, Bitlocker.
    // States: Waiting, Running, Passed (check), Failed (X), Warning (!), Unknown (?), Skipped (-).
    // A step that starts Running is selected automatically, so the details pane follows the scan.
    // ==============================================================

    public enum StepState { Waiting, Running, Passed, Failed, Warning, Unknown, Skipped }

    // What each test is, in list order. Glyph is the Phosphor icon shown while the test is waiting.
    private sealed record StepInfo(string Name, string Title, string Subtitle, string Description, string Glyph);

    private static readonly StepInfo[] StepInfos =
    [
        new("Start", "Start", "Scan record",
            "Creates the scan record for this work order and saves the device details ARC collected.",
            ""),  // clipboard-text
        new("Storage", "Storage", "Disk health",
            "Asks Windows for the health of every fixed drive. A failing drive stops the scan so nothing writes to it.",
            ""),  // hard-drives
        new("Prime95", "Prime95", "CPU & RAM stress",
            "Stress-tests the processor and memory with Prime95. A failure usually points to unstable RAM, " +
            "a weak CPU, or poor cooling.",
            ""),  // cpu
        new("Antivirus", "Antivirus", "Defender scan",
            "Runs a Microsoft Defender scan and flags anything that needs a tech to look at it.",
            ""),  // shield-check
        new("Software", "Software", "OS repair",
            "Cleans temp files, then runs DISM, SFC, and Check Disk to repair Windows system files.",
            ""),  // gear-six
        new("Bitlocker", "BitLocker", "Optional",
            "Checks BitLocker on the target drive. Only runs when booted into ARC-PE.",
            ""),  // lock-key
    ];

    // Phosphor glyphs for each finished state
    private const string CheckGlyph = "";
    private const string XGlyph = "";
    private const string WarningGlyph = "";
    private const string MinusGlyph = "";
    private const string QuestionGlyph = "";

    // One test's row in the list, plus its current state and results text.
    private sealed class StepView(StepInfo info, Button row, Border selectedBar, Border bubble,
                                  ProgressRing ring, FontIcon icon, TextBlock status)
    {
        public StepInfo Info { get; } = info;
        public Button Row { get; } = row;
        public Border SelectedBar { get; } = selectedBar;
        public Border Bubble { get; } = bubble;
        public ProgressRing Ring { get; } = ring;
        public FontIcon Icon { get; } = icon;
        public TextBlock Status { get; } = status;
        public StepState State { get; set; }
        public string Detail { get; set; } = string.Empty;
    }

    private Dictionary<string, StepView>? _steps;
    private string _selectedStep = "Start";
    private bool _resetting;
    private bool _scanStarted;

    private Dictionary<string, StepView> Steps => _steps ??= BuildStepList();

    private static Brush Res(string key) => (Brush)Application.Current.Resources[key];

    // Puts every step back to "Waiting" (BitLocker shows as skipped: it only runs from ARC-PE).
    private void ResetSteps()
    {
        _resetting = true;
        _scanStarted = false;
        foreach (var name in Steps.Keys)
        {
            SetStepState(name, StepState.Waiting, string.Empty);
        }
        SetStepState("Bitlocker", StepState.Skipped, "Only runs when booted into ARC-PE.");
        _resetting = false;

        SelectStep("Start");
        UpdateOverallProgress();
    }

    // Updates one step's row (spinner, check, X, ...), and the details pane if that step is selected.
    // detail = null keeps the step's current results text; an empty string clears it.
    private void SetStepState(string step, StepState state, string? detail = null)
    {
        var view = Steps[step];
        view.State = state;
        if (detail is not null)
        {
            view.Detail = detail;
        }

        ApplyBadge(view.Bubble, view.Ring, view.Icon, state, view.Info.Glyph);
        view.Status.Text = StatusText(state);
        view.Status.Foreground = StatusBrush(state);
        AutomationProperties.SetName(view.Row, $"{view.Info.Title}: {StatusText(state)}");

        if (!_resetting)
        {
            _scanStarted = true;
            UpdateOverallProgress();
        }

        if (state == StepState.Running)
        {
            SelectStep(step);
        }
        else if (step == _selectedStep)
        {
            ShowStepDetails(view);
        }
    }

    // Highlights a row and shows that test in the details pane.
    private void SelectStep(string step)
    {
        _selectedStep = step;
        foreach (var (name, view) in Steps)
        {
            var selected = name == step;
            view.Row.Background = selected ? Res("ArcSurface") : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            // Opacity instead of Visibility keeps the bar's space, so rows don't shift when selected
            view.SelectedBar.Opacity = selected ? 1 : 0;
        }
        ShowStepDetails(Steps[step]);
    }

    private void ShowStepDetails(StepView view)
    {
        ApplyBadge(DetailBubble, DetailRing, DetailIcon, view.State, view.Info.Glyph);
        DetailTitle.Text = view.Info.Title;
        DetailStatus.Text = StatusText(view.State);
        DetailStatus.Foreground = StatusBrush(view.State);
        DetailDescription.Text = view.Info.Description;

        // An empty card gets a gray placeholder so it never looks unfinished.
        var hasDetail = !string.IsNullOrWhiteSpace(view.Detail);
        DetailText.Text = hasDetail ? view.Detail : view.State switch
        {
            StepState.Waiting => "Results will appear here after this test runs.",
            StepState.Running => "This test is running. Results will appear here when it finishes.",
            _                 => "No details were reported for this test.",
        };
        // Results text takes the step's color (green pass, red fail, amber warning); placeholders stay gray.
        DetailText.Foreground = hasDetail ? ResultBrush(view.State) : Res("ArcTextSecondary");
    }

    // Progress bar and "x of y tests complete" under the header.
    private void UpdateOverallProgress()
    {
        var total = Steps.Count;
        var done = Steps.Values.Count(v => v.State is not (StepState.Waiting or StepState.Running));

        OverallProgress.Maximum = total;
        OverallProgress.Value = _scanStarted ? done : 0;
        OverallProgressText.Text = !_scanStarted ? "Ready when you are."
            : done == total ? "Scan complete"
            : $"{done} of {total} tests complete";
    }

    // Colors a status circle and picks its icon (or spinner) for the given state:
    // a pale tint of the status color with the icon in the full color, no border.
    private static void ApplyBadge(Border bubble, ProgressRing ring, FontIcon icon, StepState state, string waitingGlyph)
    {
        var (background, glyph, iconBrush) = state switch
        {
            StepState.Running => (Res("ArcPrimarySurface"), string.Empty, Res("ArcPrimary")),
            StepState.Passed  => (Res("ArcSuccessSurface"), CheckGlyph, Res("ArcSuccess")),
            StepState.Failed  => (Res("ArcErrorSurface"), XGlyph, Res("ArcError")),
            StepState.Warning => (Res("ArcWarningSurface"), WarningGlyph, Res("ArcWarning")),
            StepState.Unknown => (Res("ArcUnknownSurface"), QuestionGlyph, Res("ArcUnknown")),
            StepState.Skipped => (Res("ArcSurface"), MinusGlyph, Res("ArcMuted")),
            _                 => (Res("ArcSurface"), waitingGlyph, Res("ArcTextSecondary")),
        };

        bubble.Background = background;
        ring.IsActive = state == StepState.Running;
        ring.Visibility = state == StepState.Running ? Visibility.Visible : Visibility.Collapsed;
        icon.Glyph = glyph;
        icon.Foreground = iconBrush;
    }

    private static string StatusText(StepState state) => state switch
    {
        StepState.Running => "In progress...",
        StepState.Passed  => "Passed",
        StepState.Failed  => "Failed",
        StepState.Warning => "Needs attention",
        StepState.Unknown => "Unknown",
        StepState.Skipped => "Skipped",
        _                 => "Waiting",
    };

    private static Brush StatusBrush(StepState state) => state switch
    {
        StepState.Passed  => Res("ArcSuccess"),
        StepState.Failed  => Res("ArcError"),
        StepState.Warning => Res("ArcWarningText"),
        StepState.Running => Res("ArcPrimary"),
        _                 => Res("ArcTextSecondary"),
    };

    // Text color for the Results card: tinted like the status, but unknown results stay dark
    // enough to read comfortably (they usually explain what the tech should check by hand).
    private static Brush ResultBrush(StepState state) => state switch
    {
        StepState.Passed  => Res("ArcSuccess"),
        StepState.Failed  => Res("ArcError"),
        StepState.Warning => Res("ArcWarningText"),
        StepState.Skipped => Res("ArcTextSecondary"),
        _                 => Res("ArcTextPrimary"),
    };

    // Opening the Advanced terminal scrolls the page down so the terminal is in view.
    private void AdvancedExpander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
    {
        DispatcherQueue.TryEnqueue(() => PageScroll.ChangeView(null, PageScroll.ScrollableHeight, null));
    }

    // Builds one clickable row per test into StepList:  [bar] (icon)  Title / Subtitle   Status
    private Dictionary<string, StepView> BuildStepList()
    {
        var steps = new Dictionary<string, StepView>();

        foreach (var info in StepInfos)
        {
            var selectedBar = new Border
            {
                Width = 3, Height = 28, CornerRadius = new CornerRadius(2),
                Background = Res("ArcPrimary"), Opacity = 0,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var ring = new ProgressRing
            {
                Width = 20, Height = 20, IsActive = false, Visibility = Visibility.Collapsed,
                Foreground = Res("ArcPrimary"),
            };
            var icon = new FontIcon { FontFamily = (FontFamily)Application.Current.Resources["ArcIconFont"], FontSize = 18 };
            var bubbleContent = new Grid();
            bubbleContent.Children.Add(ring);
            bubbleContent.Children.Add(icon);
            var bubble = new Border
            {
                Width = 40, Height = 40, CornerRadius = new CornerRadius(20),
                Child = bubbleContent,
            };
            Grid.SetColumn(bubble, 1);

            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 2 };
            text.Children.Add(new TextBlock
            {
                Text = info.Title, FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = Res("ArcTextPrimary"),
            });
            text.Children.Add(new TextBlock { Text = info.Subtitle, FontSize = 12, Foreground = Res("ArcTextSecondary") });
            Grid.SetColumn(text, 2);

            var status = new TextBlock
            {
                FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(status, 3);

            var content = new Grid { ColumnSpacing = 12 };
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.Children.Add(selectedBar);
            content.Children.Add(bubble);
            content.Children.Add(text);
            content.Children.Add(status);

            var row = new Button
            {
                Content = content,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(8, 8, 16, 8),
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            };
            // Soft hover/pressed colors instead of the default button chrome
            row.Resources["ButtonBackgroundPointerOver"] = Res("ArcSurface");
            row.Resources["ButtonBackgroundPressed"] = Res("ArcMutedSurface");
            row.Resources["ButtonBorderBrushPointerOver"] = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            row.Resources["ButtonBorderBrushPressed"] = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            row.Click += (_, _) => SelectStep(info.Name);

            StepList.Children.Add(row);
            steps[info.Name] = new StepView(info, row, selectedBar, bubble, ring, icon, status);
        }

        return steps;
    }
}
