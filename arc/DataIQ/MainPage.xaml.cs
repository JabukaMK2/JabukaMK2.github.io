using ArcShared;

namespace DataIQ;

public sealed partial class MainPage : Page
{
    private readonly SessionContext _session;

    public MainPage()
    {
        this.InitializeComponent();

        _session = SessionStore.Load();
        TargetText.Text = $"Target: {_session.DriveLetter} ({_session.Hostname}, {_session.Mode})";

        LoadRootListing();
    }

    private void LoadRootListing()
    {
        try
        {
            var root = _session.DriveLetter.TrimEnd('\\') + "\\";
            foreach (var entry in Directory.EnumerateFileSystemEntries(root))
                FileList.Items.Add(Path.GetFileName(entry));
        }
        catch (Exception ex)
        {
            FileList.Items.Add($"Could not list {_session.DriveLetter}: {ex.Message}");
        }
    }
}
