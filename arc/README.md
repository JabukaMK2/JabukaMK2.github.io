# ARC — build scaffold

Uno Platform solution skeleton for the ARC capstone project. See [../docs/arc-capstone-technical-plan.md](../docs/arc-capstone-technical-plan.md) for the full plan.

Originally scaffolded as classic WPF; ported to [Uno Platform](https://platform.uno/) so the UI can actually be built and run on the Linux dev machine (Uno's Skia renderer targets Windows, Linux, and macOS from the same XAML/code), not just compiled.

## Layout

- `ArcShared/` — plain `net10.0` class library: `SessionContext` (the shared session model) and `SessionStore` (JSON load/save). Referenced by all three apps.
- `ARC/` — the dashboard/launcher. Shows the persistent system info panel and launches the other tools. `MainPage.xaml(.cs)` holds the actual UI/logic.
- `RepairIQ/` — placeholder scan tool page. Reads the session file on startup; `Run Scan` is not yet implemented.
- `DataIQ/` — placeholder file manager page. Lists the root of the target drive from the session file.

Each app targets `net10.0-desktop` via `Uno.Sdk` (`UnoFeatures: SkiaRenderer`). Everything outside `ArcShared`, `App.xaml(.cs)`, and `MainPage.xaml(.cs)` (icons, manifests, publish profiles, etc.) is Uno's standard project scaffolding — leave it alone unless you know you need to change it.

## Building and running

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) and the Uno Platform templates (`dotnet new install Uno.Templates`, already installed here).

```
cd arc
dotnet build ARC.sln
```

To run a specific app on this machine:

```
cd arc/ARC        # or RepairIQ / DataIQ
dotnet run -f net10.0-desktop
```

`ARC`'s buttons launch `RepairIQ.exe`/`DataIQ.exe` next to its own output — for now, copy their build output into the same folder to test the full launch flow, or set up a shared output directory once the layout is finalized.

### Linux notes

- Runs under X11 (including XWayland on a Wayland session).
- You may see `Uno.WinUI.Runtime.Skia.X11.X11NativeOverlappedPresenter` warnings about `_NET_WM_STATE` on window managers that aren't fully EWMH-compliant — cosmetic, not a functional issue.
- A `RemoteControlClient`/`DevServer` connection warning on startup is Uno's hot-reload dev-server failing to attach; harmless for a plain `dotnet run`.

### Windows

The same `net10.0-desktop` build runs on Windows too (Skia renderer), and is also what would eventually get packaged for ARC-PE. A native WinUI target is possible later if needed, but isn't set up yet.

## Not yet implemented

- RepairIQ's actual scan logic (hardware tests, AV, corruption checks)
- DataIQ's actual transfer/imaging features (currently just lists files)
- The Revo Uninstaller Portable launch path (`ARC/MainPage.xaml.cs` launches `RevoUninstaller/RevoUPort.exe` next to `ARC`'s own output)
- BitLocker prepare/decrypt flow
- ARC-PE (WinPE) boot integration
