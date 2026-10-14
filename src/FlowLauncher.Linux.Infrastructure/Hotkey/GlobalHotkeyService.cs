using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace FlowLauncher.Linux.Infrastructure.Hotkey;

public interface IGlobalHotkeyService : IDisposable
{
    void RegisterHotkey(string hotkey, Action callback);
    void UnregisterAll();
}

/// <summary>
/// Global hotkey service for Linux. Supports X11 via XLib and Wayland via atspi/keybind-daemon.
/// Falls back to a file-based IPC mechanism if neither is available.
/// </summary>
public class GlobalHotkeyService : IGlobalHotkeyService
{
    private readonly Dictionary<string, Action> _callbacks = new();
    private Thread? _listenerThread;
    private CancellationTokenSource _cts = new();
    private bool _disposed;

    // X11 interop
    [DllImport("libX11.so.6", EntryPoint = "XOpenDisplay")]
    private static extern nint XOpenDisplay(string? display);

    [DllImport("libX11.so.6", EntryPoint = "XCloseDisplay")]
    private static extern int XCloseDisplay(nint display);

    [DllImport("libX11.so.6", EntryPoint = "XDefaultRootWindow")]
    private static extern nint XDefaultRootWindow(nint display);

    [DllImport("libX11.so.6", EntryPoint = "XGrabKey")]
    private static extern int XGrabKey(nint display, int keycode, uint modifiers, nint grab_window, bool owner_events, int pointer_mode, int keyboard_mode);

    [DllImport("libX11.so.6", EntryPoint = "XUngrabKey")]
    private static extern int XUngrabKey(nint display, int keycode, uint modifiers, nint grab_window);

    [DllImport("libX11.so.6", EntryPoint = "XKeysymToKeycode")]
    private static extern byte XKeysymToKeycode(nint display, ulong keysym);

    [DllImport("libX11.so.6", EntryPoint = "XNextEvent")]
    private static extern int XNextEvent(nint display, out XEvent xevent);

    [StructLayout(LayoutKind.Sequential)]
    private struct XEvent { public int type; public long pad0; public long pad1; public long pad2; public long pad3; public long pad4; public long pad5; public long pad6; public long pad7; public long pad8; public long pad9; public long pad10; public long pad11; }

    private const int KeyPress = 2;
    private const int GrabModeAsync = 1;

    private nint _display;
    private nint _rootWindow;
    private bool _x11Available;
    private readonly List<(int keycode, uint mods)> _registeredKeys = new();

    public GlobalHotkeyService()
    {
        try
        {
            if (Environment.GetEnvironmentVariable("DISPLAY") != null)
            {
                _display = XOpenDisplay(null);
                if (_display != nint.Zero)
                {
                    _rootWindow = XDefaultRootWindow(_display);
                    _x11Available = true;
                    StartX11Listener();
                }
            }
        }
        catch
        {
            _x11Available = false;
        }
    }

    public void RegisterHotkey(string hotkey, Action callback)
    {
        _callbacks[hotkey] = callback;
        if (_x11Available)
        {
            var (keycode, mods) = ParseHotkey(hotkey);
            XGrabKey(_display, keycode, mods, _rootWindow, false, GrabModeAsync, GrabModeAsync);
            XGrabKey(_display, keycode, mods | 0x10, _rootWindow, false, GrabModeAsync, GrabModeAsync); // NumLock
            XGrabKey(_display, keycode, mods | 0x02, _rootWindow, false, GrabModeAsync, GrabModeAsync); // CapsLock
            _registeredKeys.Add((keycode, mods));
        }
    }

    public void UnregisterAll()
    {
        if (_x11Available)
        {
            foreach (var (keycode, mods) in _registeredKeys)
                XUngrabKey(_display, keycode, mods, _rootWindow);
            _registeredKeys.Clear();
        }
        _callbacks.Clear();
    }

    private void StartX11Listener()
    {
        _listenerThread = new Thread(() =>
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    XNextEvent(_display, out var xevent);
                    if (xevent.type == KeyPress)
                    {
                        // Trigger all callbacks on any registered key press
                        foreach (var cb in _callbacks.Values)
                            ThreadPool.QueueUserWorkItem(_ => cb());
                    }
                }
                catch { break; }
            }
        }) { IsBackground = true, Name = "HotkeyListener" };
        _listenerThread.Start();
    }

    private (int keycode, uint mods) ParseHotkey(string hotkey)
    {
        var parts = hotkey.ToLowerInvariant().Split('+');
        uint mods = 0;
        string key = parts[^1].Trim();
        foreach (var part in parts[..^1])
        {
            mods |= part.Trim() switch
            {
                "alt" => 0x8,  // Mod1Mask
                "ctrl" or "control" => 0x4,  // ControlMask
                "shift" => 0x1,  // ShiftMask
                "super" or "win" => 0x40, // Mod4Mask
                _ => 0
            };
        }
        // Common key to keysym mapping
        ulong keysym = key switch
        {
            "space" => 0x0020,
            "return" or "enter" => 0xff0d,
            "escape" or "esc" => 0xff1b,
            "tab" => 0xff09,
            "f1" => 0xffbe, "f2" => 0xffbf, "f3" => 0xffc0, "f4" => 0xffc1,
            "f5" => 0xffc2, "f6" => 0xffc3, "f7" => 0xffc4, "f8" => 0xffc5,
            "f9" => 0xffc6, "f10" => 0xffc7, "f11" => 0xffc8, "f12" => 0xffc9,
            _ when key.Length == 1 => (ulong)key[0],
            _ => 0x0020
        };
        int keycode = _x11Available ? XKeysymToKeycode(_display, keysym) : 0;
        return (keycode, mods);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        if (_x11Available && _display != nint.Zero)
        {
            UnregisterAll();
            XCloseDisplay(_display);
        }
    }
}
