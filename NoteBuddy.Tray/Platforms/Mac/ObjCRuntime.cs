using System.Runtime.InteropServices;

namespace NoteBuddy.Tray.Platforms.Mac;

/// <summary>
/// Low-level Objective-C runtime interop for calling macOS AppKit APIs via P/Invoke.
/// Avoids the need for the net10.0-macos TFM, Xcode, and the macOS workload.
/// </summary>
internal static class ObjCRuntime
{
    private const string ObjCLib = "/usr/lib/libobjc.A.dylib";

    [DllImport(ObjCLib)]
    public static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCLib)]
    public static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCLib)]
    public static extern IntPtr objc_allocateClassPair(IntPtr superclass, string name, IntPtr extraBytes);

    [DllImport(ObjCLib)]
    public static extern void objc_registerClassPair(IntPtr cls);

    [DllImport(ObjCLib)]
    public static extern bool class_addMethod(IntPtr cls, IntPtr sel, IntPtr imp, string types);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern IntPtr Send(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern IntPtr SendWithDouble(IntPtr receiver, IntPtr selector, double arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern void SendVoid(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern void SendVoid(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern void SendVoid(IntPtr receiver, IntPtr selector, long arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    public static extern void SendVoidWithSize(IntPtr receiver, IntPtr selector, NativeSize size);

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeSize
    {
        public double Width;
        public double Height;

        public NativeSize(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Creates an autoreleased NSString from a C# string.
    /// </summary>
    public static IntPtr CreateNSString(string str)
    {
        var nsStringClass = objc_getClass("NSString");
        var alloc = Send(nsStringClass, sel_registerName("alloc"));
        var utf8Ptr = Marshal.StringToCoTaskMemUTF8(str);
        try
        {
            return Send(alloc, sel_registerName("initWithUTF8String:"), utf8Ptr);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8Ptr);
        }
    }
}
