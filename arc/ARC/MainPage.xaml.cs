using System.Diagnostics;
using ArcShared;
using ARC.Data;

namespace ARC;

public sealed partial class MainPage : Page
{
    private readonly SessionContext _session;

    // The session context for the current user session.
    public MainPage()
    {
        this.InitializeComponent();

        _session = SessionStore.Load();
        _session.SerialNumber = SystemScan.GetSerialNumber();
        _session.DriveLetter = SystemScan.GetDriveLetter();
        _session.BitlockerStatus = SystemScan.GetBitlockerStatus(_session.DriveLetter);
        _session.OperatingSystem = SystemScan.GetOperatingSystem();
        _session.Processor = SystemScan.GetProcessor();
        _session.Memory = SystemScan.GetMemory();
        _session.Storage = SystemScan.GetStorage();
        _session.Hostname = SystemScan.GetHostname();
        SessionStore.Save(_session);

        RefreshInfoPanel();

        NavView.SelectedItem = HomeItem;
        ShowPage(Pages["Home"]);
    }

    // Refreshes the information panel with the current session data.
    private void RefreshInfoPanel()
    {
        ModeText.Text = _session.Mode == BootMode.ArcPe ? "ARC-PE" : "Live OS";
        DriveText.Text = _session.DriveLetter;
        HostnameText.Text = _session.Hostname;
        BitlockerText.Text = _session.BitlockerStatus;
        SerialNumberText.Text = _session.SerialNumber;
        OperatingSystemText.Text = _session.OperatingSystem;
        ProcessorText.Text = _session.Processor;
        MemoryText.Text = _session.Memory;
        StorageText.Text = _session.Storage;
    }

    // Content for the shared page template. ExePath = null means the page has no Start button.
    private sealed record PageInfo(string Title, string Description, string Tagline, string? ExePath);

    // The version of the app.
    private static readonly string AppVersion =
        typeof(App).Assembly.GetName().Version?.ToString(3) ?? "unknown";

    // The collection of pages available in the app.
    private static readonly Dictionary<string, PageInfo> Pages = new()
    {
        ["Home"] = new(
            "Welcome, tech!",
            "ARC is your toolkit for diagnosing, repairing, and rescuing customer PCs. " +
            "Pick a tool from the left, and let's get fixing!",
            "No screwdrivers were harmed in the making of this app.",
            null),
        ["RepairIQ"] = new(
            "RepairIQ",
            "One click to stress-test the hardware, hunt down viruses, and patch up Windows. " +
            "Grab a coffee, Prime95 takes a while.",
            "Keep the work order sticker handy, you'll need it.",
            "RepairIQ.exe"),
        ["DataIQ"] = new(
            "DataIQ",
            "Move customer files and drive images safely, so nobody loses a single cat photo.",
            "Measure twice, copy once.",
            "DataIQ.exe"),
        ["Uninstaller"] = new(
            "Uninstaller",
            "Launches Revo Uninstaller Portable to evict the programs that overstayed their welcome.",
            "Bloatware, we hardly knew ye.",
            Path.Combine("RevoUninstaller", "RevoUPort.exe")),
        ["Info"] = new(
            "About ARC",
            $"ARC (Advanced Repair Console), version {AppVersion}. Release notes and app details coming soon.",
            "Built for techs, by a tech.",
            null),
    };

    // The currently displayed page.
    private PageInfo _currentPage = Pages["Home"];

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is string tag && Pages.TryGetValue(tag, out var page))
        {
            ShowPage(page);
        }
    }

    // Displays the specified page in the UI.
    private void ShowPage(PageInfo page)
    {
        _currentPage = page;
        PageTitleText.Text = page.Title;
        PageDescriptionText.Text = page.Description;
        PageTaglineText.Text = page.Tagline;
        StartButton.Visibility = page.ExePath is null ? Visibility.Collapsed : Visibility.Visible;
        StatusText.Text = string.Empty;
    }

    // Handles the click event for the Start button.
    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage.ExePath is not null)
        {
            if (_currentPage.Title == "RepairIQ")
            {
                var workOrder = await AskForWorkOrderAsync();
                if (workOrder is null)
                {
                    return;
                }
            }
            LaunchTool(_currentPage.ExePath);
        }
    }

    // Launches the specified tool executable.
    private void LaunchTool(string relativeExePath)
    {
        var exePath = Path.Combine(AppContext.BaseDirectory, relativeExePath);

        if (!File.Exists(exePath))
        {
            StatusText.Text = $"Could not find {relativeExePath}.";
            return;
        }

        SessionStore.Save(_session);
        Process.Start(new ProcessStartInfo(exePath)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exePath)
        });
        StatusText.Text = string.Empty;
    }

    // Asks the user for a work order number and returns it as a long if valid, otherwise null.
    private async Task<long?> AskForWorkOrderAsync()
    {

        var inputTextBox = new TextBox 
        { 
            PlaceholderText = "Work Order Number",
            Text = _session.WorkOrder > 0 ? _session.WorkOrder.ToString() : string.Empty
        };
        var textBlock = new TextBlock
        {
            Foreground = Application.Current.Resources["ArcError"] as SolidColorBrush,
            Visibility = Visibility.Collapsed
        };

        var dialog = new ContentDialog
        {
            Title = "Enter Work Order",
            Content = inputTextBox,
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        while (true)
        {
            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                return null;
            }

            if (!long.TryParse(inputTextBox.Text, out var workOrder) || workOrder <= 0)
            {
                inputTextBox.PlaceholderText = "Work order must be a positive number.";
                continue;
            }

            _session.WorkOrder = workOrder;
            return workOrder;
        }
    }
}
