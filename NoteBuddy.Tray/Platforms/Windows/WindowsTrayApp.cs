using System.Reflection;

namespace NoteBuddy.Tray.Platforms.Windows;

/// <summary>
/// Windows system tray application using WinForms NotifyIcon.
/// Manages the tray icon, context menu, and NoteBuddy server lifecycle.
/// </summary>
public class WindowsTrayApp : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ServerManager _serverManager = new();

    /// <summary>
    /// Initializes the tray icon with a context menu and starts the NoteBuddy server.
    /// </summary>
    public WindowsTrayApp()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = LoadEmbeddedIcon(),
            Text = "NoteBuddy",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        _trayIcon.DoubleClick += OnOpenClicked;

        _serverManager.ServerExitedUnexpectedly += OnServerExitedUnexpectedly;

        if (!_serverManager.StartServer())
        {
            ShowError("Could not find NoteBuddy.exe. Make sure it is in the same directory as this application.");
        }
    }

    /// <summary>
    /// Loads the tray icon from embedded resources, falling back to the default application icon.
    /// </summary>
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

    private void OnServerExitedUnexpectedly(object? sender, EventArgs e)
    {
        _trayIcon.ShowBalloonTip(
            3000,
            "NoteBuddy",
            "The NoteBuddy server stopped unexpectedly. Right-click the tray icon to exit.",
            ToolTipIcon.Warning
        );
    }

    /// <summary>
    /// Opens the NoteBuddy web UI in the default browser.
    /// </summary>
    private void OnOpenClicked(object? sender, EventArgs e)
    {
        try
        {
            ServerManager.OpenBrowser();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to open browser: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the Exit menu click by stopping the server and closing the application.
    /// </summary>
    private void OnExitClicked(object? sender, EventArgs e)
    {
        _serverManager.StopServer();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    private static void ShowError(string message)
    {
        MessageBox.Show(message, "NoteBuddy", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _serverManager.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
