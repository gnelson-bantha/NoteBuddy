using System.Reflection;
using System.Runtime.InteropServices;
using static NoteBuddy.Tray.Platforms.Mac.ObjCRuntime;

namespace NoteBuddy.Tray.Platforms.Mac;

/// <summary>
/// macOS menu-bar application using AppKit NSStatusItem via Objective-C runtime P/Invoke.
/// No Xcode or macOS workload required.
/// </summary>
public class MacTrayApp
{
    private IntPtr _statusItem;
    private IntPtr _app;
    private readonly ServerManager _serverManager = new();

    // Must prevent GC collection of callback delegates
    private static ActionDelegate? _openCallback;
    private static ActionDelegate? _quitCallback;
    private static ServerManager? _staticServerManager;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ActionDelegate(IntPtr self, IntPtr selector, IntPtr sender);

    /// <summary>
    /// Initializes the macOS application, creates the status bar item, starts the server, and runs the event loop.
    /// </summary>
    public void Run()
    {
        _staticServerManager = _serverManager;

        // Load AppKit framework so its classes are available to the ObjC runtime
        NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");

        var nsAppClass = objc_getClass("NSApplication");
        _app = Send(nsAppClass, sel_registerName("sharedApplication"));

        // NSApplicationActivationPolicyAccessory = 1 (menu bar only, no Dock icon)
        SendVoid(_app, sel_registerName("setActivationPolicy:"), 1);

        CreateStatusItem();

        if (!_serverManager.StartServer())
        {
            ShowAlert("Could not find the NoteBuddy server executable. Make sure it is in the same directory as this application.");
        }

        // Run the application event loop (blocks)
        SendVoid(_app, sel_registerName("run"));
    }

    private void CreateStatusItem()
    {
        var nsStatusBarClass = objc_getClass("NSStatusBar");
        var systemStatusBar = Send(nsStatusBarClass, sel_registerName("systemStatusBar"));

        // NSVariableStatusItemLength = -1.0
        _statusItem = SendWithDouble(systemStatusBar, sel_registerName("statusItemWithLength:"), -1.0);

        var button = Send(_statusItem, sel_registerName("button"));

        var icon = LoadIcon();
        if (icon != IntPtr.Zero)
        {
            SendVoid(button, sel_registerName("setImage:"), icon);
        }
        else
        {
            SendVoid(button, sel_registerName("setTitle:"), CreateNSString("NB"));
        }

        SendVoid(_statusItem, sel_registerName("setMenu:"), CreateMenu());
    }

    /// <summary>
    /// Loads the menu bar icon from embedded resources. Uses template mode for automatic light/dark adaptation.
    /// </summary>
    private static IntPtr LoadIcon()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream("NoteBuddy.Tray.Resources.tray-icon.png");
        if (stream == null)
            return IntPtr.Zero;

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();

        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            var nsDataClass = objc_getClass("NSData");
            var data = Send(nsDataClass, sel_registerName("dataWithBytes:length:"),
                handle.AddrOfPinnedObject(), new IntPtr(bytes.Length));

            var nsImageClass = objc_getClass("NSImage");
            var imgAlloc = Send(nsImageClass, sel_registerName("alloc"));
            var image = Send(imgAlloc, sel_registerName("initWithData:"), data);

            if (image == IntPtr.Zero)
                return IntPtr.Zero;

            // 18pt is the standard macOS menu bar icon size; the 36px PNG provides @2x Retina
            SendVoidWithSize(image, sel_registerName("setSize:"), new NativeSize(18, 18));

            // Template mode: auto-adapts to light/dark menu bar
            SendVoid(image, sel_registerName("setTemplate:"), 1);

            return image;
        }
        finally
        {
            handle.Free();
        }
    }

    private IntPtr CreateMenu()
    {
        var nsMenuClass = objc_getClass("NSMenu");
        var menu = Send(Send(nsMenuClass, sel_registerName("alloc")), sel_registerName("init"));

        var delegateClass = RegisterDelegateClass();
        var delegateInstance = Send(
            Send(delegateClass, sel_registerName("alloc")),
            sel_registerName("init"));

        var nsMenuItemClass = objc_getClass("NSMenuItem");
        var emptyStr = CreateNSString("");

        // "Open NoteBuddy" item
        var openItem = Send(
            Send(nsMenuItemClass, sel_registerName("alloc")),
            sel_registerName("initWithTitle:action:keyEquivalent:"),
            CreateNSString("Open NoteBuddy"), sel_registerName("openApp:"), emptyStr);
        SendVoid(openItem, sel_registerName("setTarget:"), delegateInstance);

        // Bold the "Open" item via NSAttributedString
        var boldFont = SendWithDouble(objc_getClass("NSFont"), sel_registerName("boldSystemFontOfSize:"), 0.0);
        var fontAttrKey = CreateNSString("NSFont");
        var attrs = Send(objc_getClass("NSDictionary"),
            sel_registerName("dictionaryWithObject:forKey:"), boldFont, fontAttrKey);
        var attrTitle = Send(
            Send(objc_getClass("NSAttributedString"), sel_registerName("alloc")),
            sel_registerName("initWithString:attributes:"), CreateNSString("Open NoteBuddy"), attrs);
        SendVoid(openItem, sel_registerName("setAttributedTitle:"), attrTitle);

        SendVoid(menu, sel_registerName("addItem:"), openItem);

        // Separator
        SendVoid(menu, sel_registerName("addItem:"),
            Send(nsMenuItemClass, sel_registerName("separatorItem")));

        // "Quit" item
        var quitItem = Send(
            Send(nsMenuItemClass, sel_registerName("alloc")),
            sel_registerName("initWithTitle:action:keyEquivalent:"),
            CreateNSString("Quit"), sel_registerName("quitApp:"), CreateNSString("q"));
        SendVoid(quitItem, sel_registerName("setTarget:"), delegateInstance);
        SendVoid(menu, sel_registerName("addItem:"), quitItem);

        return menu;
    }

    private static IntPtr RegisterDelegateClass()
    {
        // Check if already registered (safe for repeated calls)
        var existing = objc_getClass("NoteBuddyMenuDelegate");
        if (existing != IntPtr.Zero)
            return existing;

        var nsObjectClass = objc_getClass("NSObject");
        var cls = objc_allocateClassPair(nsObjectClass, "NoteBuddyMenuDelegate", IntPtr.Zero);

        // Pin delegates to prevent GC collection
        _openCallback = OnOpenClicked;
        _quitCallback = OnQuitClicked;

        // ObjC type encoding "v@:@" = void return, id self, SEL _cmd, id sender
        class_addMethod(cls, sel_registerName("openApp:"),
            Marshal.GetFunctionPointerForDelegate(_openCallback), "v@:@");
        class_addMethod(cls, sel_registerName("quitApp:"),
            Marshal.GetFunctionPointerForDelegate(_quitCallback), "v@:@");

        objc_registerClassPair(cls);
        return cls;
    }

    private static void OnOpenClicked(IntPtr self, IntPtr selector, IntPtr sender)
    {
        try
        {
            ServerManager.OpenBrowser();
        }
        catch
        {
            // Best effort
        }
    }

    private static void OnQuitClicked(IntPtr self, IntPtr selector, IntPtr sender)
    {
        _staticServerManager?.Dispose();

        var nsAppClass = objc_getClass("NSApplication");
        var app = Send(nsAppClass, sel_registerName("sharedApplication"));
        SendVoid(app, sel_registerName("terminate:"), IntPtr.Zero);
    }

    private static void ShowAlert(string message)
    {
        var nsAlertClass = objc_getClass("NSAlert");
        var alert = Send(Send(nsAlertClass, sel_registerName("alloc")), sel_registerName("init"));

        SendVoid(alert, sel_registerName("setMessageText:"), CreateNSString("NoteBuddy"));
        SendVoid(alert, sel_registerName("setInformativeText:"), CreateNSString(message));

        // NSAlertStyleWarning = 0
        SendVoid(alert, sel_registerName("setAlertStyle:"), 0);

        Send(alert, sel_registerName("addButtonWithTitle:"), CreateNSString("OK"));
        Send(alert, sel_registerName("runModal"));
    }
}
