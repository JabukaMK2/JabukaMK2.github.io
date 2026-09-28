using ArcShared;

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

    private void RunScanButton_Click(object sender, RoutedEventArgs e)
    {
        // Placeholder: hardware tests, AV scan, and corruption checks land here.
        LogText.Text = "Scan not yet implemented.";
    }
}
