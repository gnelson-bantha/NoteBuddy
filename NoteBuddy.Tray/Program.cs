namespace NoteBuddy.Tray;

/// <summary>
/// Entry point for the NoteBuddy system tray application.
/// Uses conditional compilation to select the platform-specific tray implementation.
/// </summary>
static class Program
{
    [STAThread]
    static void Main()
    {
#if PLATFORM_WINDOWS
        ApplicationConfiguration.Initialize();
        Application.Run(new Platforms.Windows.WindowsTrayApp());
#elif PLATFORM_MAC
        new Platforms.Mac.MacTrayApp().Run();
#else
        Console.Error.WriteLine("NoteBuddy.Tray is not supported on this platform.");
        Environment.Exit(1);
#endif
    }
}