using System.Runtime.InteropServices;
namespace Yume.WindowsIntegration;
public sealed class TrayService : IDisposable
{
    private const uint Callback = 0x8001;
    private readonly nint hwnd;
    private readonly Subclass callback;
    private NotifyIconData data;
    private bool installed;
    private readonly uint taskbarCreated = RegisterWindowMessage("TaskbarCreated");
    public bool MediaKeys { get; set; } = true;
    public event Action<string>? Command;
    public TrayService(nint hwnd)
    {
        this.hwnd = hwnd;
        callback = WindowProc;
        if (!SetWindowSubclass(hwnd, callback, 1, 0)) throw new InvalidOperationException("Window integration unavailable.");
        data = new NotifyIconData { Size = (uint)Marshal.SizeOf<NotifyIconData>(), Window = hwnd, Id = 1, Flags = 7, CallbackMessage = Callback, Icon = LoadImage(0, Path.Combine(AppContext.BaseDirectory, "Assets", "Yume.ico"), 1, 32, 32, 0x10), Tip = "Yume", Info = "", InfoTitle = "" };
        installed = Shell_NotifyIcon(0, ref data);
        if (!installed) { DestroyIcon(data.Icon); RemoveWindowSubclass(hwnd, callback, 1); throw new InvalidOperationException("Tray unavailable."); }
    }
    private nint WindowProc(nint h, uint message, nuint w, nint l, nuint id, nuint reference)
    {
        if (message == taskbarCreated) { Shell_NotifyIcon(0, ref data); return 0; }
        if (message == Callback)
        {
            if ((uint)l == 0x203) Command?.Invoke("open");
            if ((uint)l == 0x205 || (uint)l == 0x7b) ShowMenu();
            return 0;
        }
        if (message == 0x319 && MediaKeys)
        {
            var command = ((long)l >> 16) & 0xfff;
            var action = command switch { 14 => "toggle", 11 => "next", 12 => "previous", 46 => "play", 47 => "pause", _ => null };
            if (action != null) { Command?.Invoke(action); return 1; }
        }
        return DefSubclassProc(h, message, w, l);
    }
    private void ShowMenu()
    {
        var menu = CreatePopupMenu();
        try
        {
            var labels = new[] { "Open", "Play / Pause", "Next", "Previous", "Exit" };
            for (var i = 0; i < labels.Length; i++) AppendMenu(menu, 0, (nuint)(i + 1), labels[i]);
            GetCursorPos(out var point);
            SetForegroundWindow(hwnd);
            var selected = TrackPopupMenu(menu, 0x100 | 2, point.X, point.Y, 0, hwnd, 0);
            var actions = new[] { "", "open", "toggle", "next", "previous", "exit" };
            if (selected > 0 && selected < actions.Length) Command?.Invoke(actions[selected]);
        }
        finally { DestroyMenu(menu); }
    }
    public void Dispose()
    {
        if (!installed) return;
        Shell_NotifyIcon(2, ref data);
        DestroyIcon(data.Icon);
        RemoveWindowSubclass(hwnd, callback, 1);
        installed = false;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint Size; public nint Window; public uint Id; public uint Flags; public uint CallbackMessage; public nint Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State; public uint StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle;
        public uint InfoFlags; public Guid Guid; public nint BalloonIcon;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X; public int Y; }
    private delegate nint Subclass(nint hwnd, uint msg, nuint wparam, nint lparam, nuint id, nuint reference);
    [DllImport("comctl32.dll")] private static extern bool SetWindowSubclass(nint hwnd, Subclass callback, nuint id, nuint reference);
    [DllImport("comctl32.dll")] private static extern bool RemoveWindowSubclass(nint hwnd, Subclass callback, nuint id);
    [DllImport("comctl32.dll")] private static extern nint DefSubclassProc(nint hwnd, uint msg, nuint wparam, nint lparam);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint LoadImage(nint instance, string name, uint type, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool DestroyIcon(nint icon);
    [DllImport("user32.dll")] private static extern nint CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool AppendMenu(nint menu, uint flags, nuint id, string text);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern int TrackPopupMenu(nint menu, uint flags, int x, int y, int reserved, nint hwnd, nint rectangle);
    [DllImport("user32.dll")] private static extern bool DestroyMenu(nint menu);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string message);
}
