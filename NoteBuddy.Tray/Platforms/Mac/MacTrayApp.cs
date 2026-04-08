using System.Reflection;
using AppKit;
using CoreGraphics;
using Foundation;

namespace NoteBuddy.Tray.Platforms.Mac;

/// <summary>
/// macOS menu-bar application using AppKit NSStatusItem.
/// Manages the status bar icon, menu, and NoteBuddy server lifecycle.
/// </summary>
public class MacTrayApp
{
    private NSStatusItem? _statusItem;
    private readonly ServerManager _serverManager = new();

    /// <summary>
    /// Initializes the macOS application, creates the status bar item, starts the server, and runs the event loop.
    /// </summary>
    public void Run()
    {
        NSApplication.Init();
        var app = NSApplication.SharedApplication;

        // Run as an accessory app (menu bar only, no Dock icon)
        app.ActivationPolicy = NSApplicationActivationPolicy.Accessory;

        CreateStatusItem();

        _serverManager.ServerExitedUnexpectedly += OnServerExitedUnexpectedly;

        if (!_serverManager.StartServer())
        {
            ShowAlert("Could not find the NoteBuddy server executable. Make sure it is in the same directory as this application.");
        }

        app.Run();
    }

    /// <summary>
    /// Creates the status bar item with an icon and context menu.
    /// </summary>
    private void CreateStatusItem()
    {
        _statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(NSStatusItemLength.Variable);

        var icon = LoadIcon();
        if (icon != null)
        {
            _statusItem.Button.Image = icon;
        }
        else
        {
            _statusItem.Button.Title = "NB";
        }

        _statusItem.Menu = CreateMenu();
    }

    /// <summary>
    /// Loads the menu bar icon from embedded resources. Uses template mode for automatic light/dark adaptation.
    /// </summary>
    private static NSImage? LoadIcon()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream("NoteBuddy.Tray.Resources.tray-icon.png");
        if (stream == null)
            return null;

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        var data = NSData.FromArray(memoryStream.ToArray());
        var image = new NSImage(data);

        // 18pt is the standard macOS menu bar icon size; the 36px PNG provides @2x Retina resolution
        image.Size = new CGSize(18, 18);
        image.Template = true;
        return image;
    }

    /// <summary>
    /// Creates the dropdown menu for the status bar item.
    /// </summary>
    private NSMenu CreateMenu()
    {
        var menu = new NSMenu();

        var openItem = new NSMenuItem("Open NoteBuddy", OpenClicked);
        var attrs = new NSStringAttributes { Font = NSFont.BoldSystemFontOfSize(0) };
        openItem.AttributedTitle = new NSAttributedString("Open NoteBuddy", attrs);
        menu.AddItem(openItem);

        menu.AddItem(NSMenuItem.SeparatorItem);

        var quitItem = new NSMenuItem("Quit", QuitClicked);
        quitItem.KeyEquivalent = "q";
        menu.AddItem(quitItem);

        return menu;
    }

    private void OpenClicked(object? sender, EventArgs e)
    {
        try
        {
            ServerManager.OpenBrowser();
        }
        catch (Exception ex)
        {
            ShowAlert($"Failed to open browser: {ex.Message}");
        }
    }

    private void QuitClicked(object? sender, EventArgs e)
    {
        _serverManager.Dispose();

        if (_statusItem != null)
        {
            NSStatusBar.SystemStatusBar.RemoveStatusItem(_statusItem);
            _statusItem = null;
        }

        NSApplication.SharedApplication.Terminate(null);
    }

    private void OnServerExitedUnexpectedly(object? sender, EventArgs e)
    {
        // Dispatch to main thread for UI operations
        NSApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            ShowAlert("The NoteBuddy server stopped unexpectedly. You can quit from the menu bar icon.");
        });
    }

    private static void ShowAlert(string message)
    {
        var alert = new NSAlert
        {
            MessageText = "NoteBuddy",
            InformativeText = message,
            AlertStyle = NSAlertStyle.Warning
        };
        alert.AddButton("OK");
        alert.RunModal();
    }
}
