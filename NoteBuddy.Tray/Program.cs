namespace NoteBuddy.Tray;

/// <summary>
/// Entry point for the NoteBuddy system tray application.
/// </summary>
static class Program
{
    /// <summary>
    /// Application entry point. Initializes Windows Forms and runs the tray application context.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}