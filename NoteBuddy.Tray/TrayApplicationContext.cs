using System.Diagnostics;
using System.Reflection;

namespace NoteBuddy.Tray;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private Process? _serverProcess;
    private const string ServerUrl = "http://localhost:5150";

    public TrayApplicationContext()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = LoadEmbeddedIcon(),
            Text = "NoteBuddy",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        _trayIcon.DoubleClick += OnOpenClicked;

        StartServer();
    }

    private static Icon LoadEmbeddedIcon()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream("NoteBuddy.Tray.Resources.tray-icon.ico");
        if (stream != null)
        {
            return new Icon(stream);
        }
        return SystemIcons.Application;
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        var openItem = new ToolStripMenuItem("Open NoteBuddy");
        openItem.Click += OnOpenClicked;
        openItem.Font = new Font(openItem.Font, FontStyle.Bold);
        menu.Items.Add(openItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += OnExitClicked;
        menu.Items.Add(exitItem);

        return menu;
    }

    private void StartServer()
    {
        try
        {
            var exePath = FindServerExecutable();
            if (exePath == null)
            {
                ShowError("Could not find NoteBuddy.exe. Make sure it is in the same directory as this application.");
                return;
            }

            _serverProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = Path.GetDirectoryName(exePath)!,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                },
                EnableRaisingEvents = true
            };

            _serverProcess.Exited += OnServerExited;
            _serverProcess.Start();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to start NoteBuddy server: {ex.Message}");
        }
    }

    private static string? FindServerExecutable()
    {
        var appDir = AppContext.BaseDirectory;

        // Look for NoteBuddy.exe in same directory
        var sameDirPath = Path.Combine(appDir, "NoteBuddy.exe");
        if (File.Exists(sameDirPath)) return sameDirPath;

        // Look in sibling NoteBuddy directory
        var siblingPath = Path.Combine(appDir, "..", "NoteBuddy", "NoteBuddy.exe");
        if (File.Exists(siblingPath)) return Path.GetFullPath(siblingPath);

        // Look in parent directory
        var parentPath = Path.Combine(appDir, "..", "NoteBuddy.exe");
        if (File.Exists(parentPath)) return Path.GetFullPath(parentPath);

        return null;
    }

    private void OnServerExited(object? sender, EventArgs e)
    {
        if (_serverProcess?.ExitCode != 0)
        {
            _trayIcon.ShowBalloonTip(
                3000,
                "NoteBuddy",
                "The NoteBuddy server stopped unexpectedly. Right-click the tray icon to exit.",
                ToolTipIcon.Warning
            );
        }
    }

    private void OnOpenClicked(object? sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ServerUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowError($"Failed to open browser: {ex.Message}");
        }
    }

    private void OnExitClicked(object? sender, EventArgs e)
    {
        StopServer();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    private void StopServer()
    {
        try
        {
            if (_serverProcess is { HasExited: false })
            {
                _serverProcess.Kill(entireProcessTree: true);
                _serverProcess.WaitForExit(3000);
            }
        }
        catch
        {
            // Best effort
        }
        finally
        {
            _serverProcess?.Dispose();
            _serverProcess = null;
        }
    }

    private static void ShowError(string message)
    {
        MessageBox.Show(message, "NoteBuddy", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopServer();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
