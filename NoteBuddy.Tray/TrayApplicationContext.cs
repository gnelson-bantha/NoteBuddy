using System.Diagnostics;
using System.Reflection;

namespace NoteBuddy.Tray;

/// <summary>
/// Manages the system tray icon and the NoteBuddy backend server lifecycle.
/// </summary>
public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private Process? _serverProcess;
    private const string ServerUrl = "http://localhost:5150";

    /// <summary>
    /// Initializes the tray icon with a context menu and starts the NoteBuddy server.
    /// </summary>
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

    /// <summary>
    /// Loads the tray icon from embedded resources, falling back to the default application icon.
    /// </summary>
    /// <returns>The loaded <see cref="Icon"/> instance.</returns>
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

    /// <summary>
    /// Creates the right-click context menu for the tray icon with Open and Exit items.
    /// </summary>
    /// <returns>A configured <see cref="ContextMenuStrip"/>.</returns>
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

    /// <summary>
    /// Locates and launches the NoteBuddy server as a background process.
    /// </summary>
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

    /// <summary>
    /// Searches for the NoteBuddy server executable in the application directory and nearby locations.
    /// </summary>
    /// <returns>The full path to the executable, or <c>null</c> if not found.</returns>
    private static string? FindServerExecutable()
    {
        var appDir = AppContext.BaseDirectory;

        // Look for NoteBuddy.exe in same directory (flat install layout)
        var sameDirPath = Path.Combine(appDir, "NoteBuddy.exe");
        if (File.Exists(sameDirPath)) return sameDirPath;

        // Look in sibling Server directory (installer layout: Tray\ and Server\ under Program Files)
        var serverSubdirPath = Path.Combine(appDir, "..", "Server", "NoteBuddy.exe");
        if (File.Exists(serverSubdirPath)) return Path.GetFullPath(serverSubdirPath);

        // Look in sibling NoteBuddy directory (development layout)
        var siblingPath = Path.Combine(appDir, "..", "NoteBuddy", "NoteBuddy.exe");
        if (File.Exists(siblingPath)) return Path.GetFullPath(siblingPath);

        // Look in parent directory
        var parentPath = Path.Combine(appDir, "..", "NoteBuddy.exe");
        if (File.Exists(parentPath)) return Path.GetFullPath(parentPath);

        return null;
    }

    /// <summary>
    /// Handles the server process exit event by showing a warning balloon if it exited abnormally.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The event data.</param>
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

    /// <summary>
    /// Opens the NoteBuddy web UI in the default browser.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The event data.</param>
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

    /// <summary>
    /// Handles the Exit menu click by stopping the server and closing the application.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The event data.</param>
    private void OnExitClicked(object? sender, EventArgs e)
    {
        StopServer();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    /// <summary>
    /// Gracefully stops the NoteBuddy server process and releases its resources.
    /// </summary>
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

    /// <summary>
    /// Displays an error message dialog to the user.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    private static void ShowError(string message)
    {
        MessageBox.Show(message, "NoteBuddy", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>
    /// Releases managed resources including the server process and tray icon.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release managed resources; otherwise <c>false</c>.</param>
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
