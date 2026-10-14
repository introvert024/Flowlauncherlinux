using System;
using System.IO;
using System.Threading.Tasks;

namespace FlowLauncher.Linux.Infrastructure;

public static class LinuxHelper
{
    public static bool IsX11 => Environment.GetEnvironmentVariable("DISPLAY") != null;
    public static bool IsWayland => Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null;
    public static string DesktopEnvironment => Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "Unknown";

    public static void OpenUrl(string url)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "xdg-open", Arguments = url, UseShellExecute = false }); }
        catch { }
    }

    public static void OpenFileManager(string path)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "xdg-open", Arguments = $"\"{path}\"", UseShellExecute = false }); }
        catch { }
    }

    public static string GetSystemApplicationsDirectory() => "/usr/share/applications";
    public static string GetUserApplicationsDirectory() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "applications");

    public static async Task<string> RunCommandAsync(string command, string args)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command, Arguments = args,
                RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null) return string.Empty;
            string output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return output.Trim();
        }
        catch { return string.Empty; }
    }

    public static void SetAutoStart(bool enable)
    {
        var autostartDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autostart");
        Directory.CreateDirectory(autostartDir);
        var desktopFile = Path.Combine(autostartDir, "flow-launcher.desktop");
        if (enable)
        {
            string execPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            File.WriteAllText(desktopFile, $"[Desktop Entry]\nType=Application\nName=Flow Launcher\nExec={execPath}\nHidden=false\nNoDisplay=false\nX-GNOME-Autostart-enabled=true\n");
        }
        else
        {
            if (File.Exists(desktopFile)) File.Delete(desktopFile);
        }
    }
}
