using System.Runtime.InteropServices;
using Windows.Graphics;

namespace ARC;

// Window sizing for ARC's MainPage: makes the window tall enough on launch that the
// "This device" sidebar isn't cut off (long processor names and several drives wrap onto
// extra lines, so the height it needs changes from PC to PC).
public sealed partial class MainPage
{
    // Grows the window by however much the sidebar overflows, without going past the screen's
    // work area (the screen minus the taskbar). If it still doesn't fit, the sidebar scrolls.
    private void FitWindowToSidebar()
    {
        if (((App)Application.Current).MainWindow is not { } window)
        {
            return;
        }

        // How much taller the device info is than the space the sidebar has (in layout units)
        var overflow = SidebarPanel.ActualHeight - SidebarScroll.ActualHeight;
        if (overflow <= 0)
        {
            return;
        }

        // AppWindow sizes are in physical pixels, layout units are scaled by the display scale
        var appWindow = window.AppWindow;
        var scale = XamlRoot?.RasterizationScale ?? 1;
        var height = appWindow.Size.Height + (int)Math.Ceiling((overflow + 8) * scale);
        var top = appWindow.Position.Y;

        if (OperatingSystem.IsWindows() && SystemParametersInfo(SPI_GETWORKAREA, 0, out var workArea, 0))
        {
            height = Math.Min(height, workArea.Bottom - workArea.Top);
            top = Math.Clamp(top, workArea.Top, workArea.Bottom - height);
        }

        appWindow.Resize(new SizeInt32 { Width = appWindow.Size.Width, Height = height });
        appWindow.Move(new PointInt32 { X = appWindow.Position.X, Y = top });
    }

    // Windows API: the primary screen's work area, in physical pixels
    private const uint SPI_GETWORKAREA = 0x0030;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out RECT pvParam, uint fWinIni);
}
