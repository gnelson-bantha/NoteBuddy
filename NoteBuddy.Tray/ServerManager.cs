using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NoteBuddy.Tray;

/// <summary>
/// Manages the NoteBuddy server process lifecycle and provides shared utilities
/// for platform-specific tray implementations.
/// </summary>
public class ServerManager : IDisposable
{
    private Process? _serverProcess;

    /// <summary>
    /// The URL where the NoteBuddy server listens.
    /// </summary>
    public const string ServerUrl = "http://localhost:5150";

    /// <summary>
    /// Raised when the server process exits with a non-zero exit code.
    /// </summary>
    public event EventHandler? ServerExitedUnexpectedly;

    /// <summary>
    /// Locates and launches the NoteBuddy server as a background process.
    /// </summary>
    /// <returns><c>true</c> if the server started successfully; otherwise <c>false</c>.</returns>
    public bool StartServer()
    {
        try
        {
            var exePath = FindServerExecutable();
            if (exePath == null)
                return false;

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
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gracefully stops the NoteBuddy server process and releases its resources.
    /// </summary>
    public void StopServer()
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
    /// Opens the NoteBuddy web UI in the default browser.
    /// </summary>
    public static void OpenBrowser()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = ServerUrl,
            UseShellExecute = true
        });
    }

    /// <summary>
    /// Searches for the NoteBuddy server executable in the application directory and nearby locations.
    /// </summary>
    /// <returns>The full path to the executable, or <c>null</c> if not found.</returns>
    private static string? FindServerExecutable()
    {
        var appDir = AppContext.BaseDirectory;
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "NoteBuddy.exe"
            : "NoteBuddy";

        // Same directory (flat install layout)
        var sameDirPath = Path.Combine(appDir, exeName);
        if (File.Exists(sameDirPath)) return sameDirPath;

        // Sibling Server directory (installer layout: Tray/ and Server/ under Program Files)
        var serverSubdirPath = Path.Combine(appDir, "..", "Server", exeName);
        if (File.Exists(serverSubdirPath)) return Path.GetFullPath(serverSubdirPath);

        // Sibling NoteBuddy directory (development layout)
        var siblingPath = Path.Combine(appDir, "..", "NoteBuddy", exeName);
        if (File.Exists(siblingPath)) return Path.GetFullPath(siblingPath);

        // Parent directory
        var parentPath = Path.Combine(appDir, "..", exeName);
        if (File.Exists(parentPath)) return Path.GetFullPath(parentPath);

        return null;
    }

    private void OnServerExited(object? sender, EventArgs e)
    {
        if (_serverProcess?.ExitCode != 0)
        {
            ServerExitedUnexpectedly?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        StopServer();
        GC.SuppressFinalize(this);
    }
}
