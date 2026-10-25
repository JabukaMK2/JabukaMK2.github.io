using ArcShared;
using Microsoft.UI.Xaml.Navigation;
using RepairIQ.Data;
using Windows.ApplicationModel.DataTransfer;

namespace RepairIQ;

// What the scan page hands to the summary page when it opens it.
public sealed record ScanSummaryArgs(ScanReport Report, SessionContext Session);

// The page that opens when a scan finishes: each component's tests with pass/fail, plus
// plain-text notes the tech can copy into the customer's work order.
public sealed partial class ScanSummaryPage : Page
{
    // Phosphor glyphs (same font and codepoints as the scan page)
    private const string CheckGlyph = "";
    private const string XGlyph = "";
    private const string WarningGlyph = "";
    private const string MinusGlyph = "";
    private const string QuestionGlyph = "";

    public ScanSummaryPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is not ScanSummaryArgs args)
        {
            return;
        }

        var summary = new ScanSummary(args.Report);
        var finished = args.Report.ScanEnd ?? args.Report.ScanStart;
        SubtitleText.Text = $"Work order #{args.Report.WorkOrder}  ·  {finished:MMM d, yyyy, h:mm tt}  ·  " +
                            $"took {ScanSummary.FormatDuration(args.Report.ScanStart, args.Report.ScanEnd)}";

        ShowVerdict(summary);
        BuildTestList(summary);
        NotesBox.Text = summary.ToWorkOrderText(args.Session);
    }

    private static Brush Res(string key) => (Brush)Application.Current.Resources[key];

    // Green banner when everything passed, red when something failed, gray when tests are missing.
    private void ShowVerdict(ScanSummary summary)
    {
        var (surface, color, glyph) = summary.IssueCount > 0 ? ("ArcErrorSurface", "ArcError", WarningGlyph)
            : summary.Overall == "All tests passed." ? ("ArcSuccessSurface", "ArcSuccess", CheckGlyph)
            : ("ArcUnknownSurface", "ArcTextPrimary", MinusGlyph);

        VerdictBanner.Background = Res(surface);
        VerdictIcon.Glyph = glyph;
        VerdictIcon.Foreground = Res(color);
        VerdictText.Text = summary.Overall;
        VerdictText.Foreground = Res(color);
    }

    // A small heading per component, then a row per test:  (badge)  Test name   Outcome
    private void BuildTestList(ScanSummary summary)
    {
        TestList.Children.Clear();
        var first = true;

        foreach (var component in summary.Tests.GroupBy(t => t.Component))
        {
            TestList.Children.Add(new TextBlock
            {
                Text = component.Key.ToUpperInvariant(),
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                CharacterSpacing = 60,
                Foreground = Res("ArcTextSecondary"),
                Margin = new Thickness(0, first ? 0 : 16, 0, 6),
            });
            first = false;

            foreach (var test in component)
            {
                TestList.Children.Add(BuildTestRow(test));
            }
        }
    }

    private static Grid BuildTestRow(SummaryTest test)
    {
        var (surface, color, glyph) = test.Outcome switch
        {
            TestOutcome.Passed => ("ArcSuccessSurface", "ArcSuccess", CheckGlyph),
            TestOutcome.Failed => ("ArcErrorSurface", "ArcError", XGlyph),
            TestOutcome.NeedsAttention => ("ArcWarningSurface", "ArcWarningText", WarningGlyph),
            TestOutcome.CouldNotTest => ("ArcUnknownSurface", "ArcUnknown", QuestionGlyph),
            _ => ("ArcSurface", "ArcMuted", MinusGlyph),
        };

        var badge = new Border
        {
            Width = 30, Height = 30, CornerRadius = new CornerRadius(15),
            Background = Res(surface),
            Child = new FontIcon
            {
                FontFamily = (FontFamily)Application.Current.Resources["ArcIconFont"],
                FontSize = 14, Glyph = glyph, Foreground = Res(color),
            },
        };

        var name = new TextBlock
        {
            Text = test.Name, FontSize = 14, Foreground = Res("ArcTextPrimary"),
            VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetColumn(name, 1);

        var outcome = new TextBlock
        {
            Text = ScanSummary.OutcomeText(test.Outcome), FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = Res(test.Outcome == TestOutcome.NotRun ? "ArcTextSecondary" : color),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(outcome, 2);

        var row = new Grid { ColumnSpacing = 12, Padding = new Thickness(0, 6, 0, 6) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(badge);
        row.Children.Add(name);
        row.Children.Add(outcome);
        return row;
    }

    private async void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        var package = new DataPackage();
        package.SetText(NotesBox.Text);
        Clipboard.SetContent(package);

        // Brief confirmation next to the button
        CopiedText.Opacity = 1;
        await Task.Delay(2000);
        CopiedText.Opacity = 0;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }
}
