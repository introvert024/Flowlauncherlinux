using System.Diagnostics;
using FlowLauncher.Linux.Plugin;

namespace FlowLauncher.Linux.Plugins.Sys;

public class Main : IPlugin
{
    private PluginInitContext _context = null!;

    private record SystemCommand(
        string Id,
        string Title,
        string SubTitle,
        string IcoPath,
        string[] Keywords,
        Func<Main, Task> Execute);

    private List<SystemCommand> _commands = new();

    public Task InitAsync(PluginInitContext context)
    {
        _context = context;
        InitializeCommands();
        return Task.CompletedTask;
    }

    public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        var rawSearch = query.Search?.Trim();
        if (string.IsNullOrWhiteSpace(rawSearch))
        {
            return Task.FromResult(new List<Result>());
        }

        var results = new List<Result>();
        var isSystemKeyword = rawSearch.Equals("sys", StringComparison.OrdinalIgnoreCase) ||
                              rawSearch.Equals("system", StringComparison.OrdinalIgnoreCase);

        foreach (var cmd in _commands)
        {
            int score = 0;

            if (isSystemKeyword)
            {
                score = 80;
            }
            else
            {
                // Exact title match
                if (cmd.Title.Equals(rawSearch, StringComparison.OrdinalIgnoreCase))
                {
                    score = 100;
                }
                // Exact keyword match
                else if (cmd.Keywords.Any(k => k.Equals(rawSearch, StringComparison.OrdinalIgnoreCase)))
                {
                    score = 95;
                }
                // Title starts with search
                else if (cmd.Title.StartsWith(rawSearch, StringComparison.OrdinalIgnoreCase))
                {
                    score = 90;
                }
                // Keyword starts with search
                else if (cmd.Keywords.Any(k => k.StartsWith(rawSearch, StringComparison.OrdinalIgnoreCase)))
                {
                    score = 85;
                }
                // Title contains search
                else if (cmd.Title.Contains(rawSearch, StringComparison.OrdinalIgnoreCase))
                {
                    score = 75;
                }
                // Keyword contains search
                else if (cmd.Keywords.Any(k => k.Contains(rawSearch, StringComparison.OrdinalIgnoreCase)))
                {
                    score = 70;
                }
                // Subtitle contains search
                else if (cmd.SubTitle.Contains(rawSearch, StringComparison.OrdinalIgnoreCase))
                {
                    score = 50;
                }
            }

            if (score > 0)
            {
                var currentCmd = cmd;
                results.Add(new Result
                {
                    Title = currentCmd.Title,
                    SubTitle = currentCmd.SubTitle,
                    IcoPath = currentCmd.IcoPath,
                    Score = score,
                    Action = async _ =>
                    {
                        _context.API.HideMainWindow();
                        await currentCmd.Execute(this);
                        return true;
                    }
                });
            }
        }

        results.Sort((a, b) => b.Score.CompareTo(a.Score));
        return Task.FromResult(results);
    }

    private void InitializeCommands()
    {
        _commands = new List<SystemCommand>
        {
            new(
                "shutdown",
                "Shutdown",
                "Shut down the computer (systemctl poweroff)",
                "Images/power.png",
                new[] { "shutdown", "power off", "poweroff", "halt", "turn off" },
                m => m.ExecuteProcessAsync("systemctl", "poweroff", "shutdown -h now")
            ),
            new(
                "reboot",
                "Restart",
                "Restart the computer (systemctl reboot)",
                "Images/restart.png",
                new[] { "restart", "reboot", "reset" },
                m => m.ExecuteProcessAsync("systemctl", "reboot", "reboot")
            ),
            new(
                "lock",
                "Lock Screen",
                "Lock the current session (loginctl lock-session)",
                "Images/lock.png",
                new[] { "lock", "lock screen", "screen lock" },
                m => m.LockScreenAsync()
            ),
            new(
                "logout",
                "Log Out",
                "Log out of current user session",
                "Images/logout.png",
                new[] { "logout", "log out", "sign out", "signout", "exit" },
                m => m.LogOutAsync()
            ),
            new(
                "sleep",
                "Sleep / Suspend",
                "Suspend computer to RAM (systemctl suspend)",
                "Images/sleep.png",
                new[] { "sleep", "suspend", "standby" },
                m => m.ExecuteProcessAsync("systemctl", "suspend")
            ),
            new(
                "hibernate",
                "Hibernate",
                "Save session and suspend computer to disk (systemctl hibernate)",
                "Images/sleep.png",
                new[] { "hibernate", "deep sleep" },
                m => m.ExecuteProcessAsync("systemctl", "hibernate")
            ),
            new(
                "empty_trash",
                "Empty Trash",
                "Permanently delete all items in trash",
                "Images/trash.png",
                new[] { "trash", "empty trash", "clear trash", "clean trash" },
                m => m.EmptyTrashAsync()
            ),
            new(
                "file_manager",
                "Open File Manager",
                "Open home folder in default file manager",
                "Images/folder.png",
                new[] { "file manager", "files", "explorer", "nautilus", "dolphin", "thunar" },
                m => m.OpenFileManagerAsync()
            ),
            new(
                "terminal",
                "Open Terminal",
                "Open default terminal emulator",
                "Images/terminal.png",
                new[] { "terminal", "console", "shell", "bash" },
                m => m.OpenTerminalAsync()
            )
        };
    }

    private async Task ExecuteProcessAsync(string command, string arguments, string? fallbackCommand = null)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                await proc.WaitForExitAsync();
                if (proc.ExitCode == 0) return;
            }
        }
        catch
        {
            // Try fallback
        }

        if (!string.IsNullOrEmpty(fallbackCommand))
        {
            try
            {
                var parts = fallbackCommand.Split(' ', 2);
                var psi = new ProcessStartInfo
                {
                    FileName = parts[0],
                    Arguments = parts.Length > 1 ? parts[1] : string.Empty,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc != null) await proc.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                _context.API.ShowError("System Command Failed", ex.Message);
            }
        }
    }

    private async Task LockScreenAsync()
    {
        var lockCommands = new[]
        {
            ("loginctl", "lock-session"),
            ("xdg-screensaver", "lock"),
            ("dm-tool", "lock"),
            ("gnome-screensaver-command", "-l")
        };

        foreach (var (cmd, args) in lockCommands)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = cmd,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    await proc.WaitForExitAsync();
                    if (proc.ExitCode == 0) return;
                }
            }
            catch
            {
                // Try next
            }
        }

        _context.API.ShowError("Lock Screen Failed", "Could not find a screen locker on this system.");
    }

    private async Task LogOutAsync()
    {
        var sessionId = Environment.GetEnvironmentVariable("XDG_SESSION_ID");
        var logoutCommands = new List<(string, string)>();

        if (!string.IsNullOrEmpty(sessionId))
        {
            logoutCommands.Add(("loginctl", $"terminate-session {sessionId}"));
        }

        logoutCommands.Add(("loginctl", "terminate-session self"));
        logoutCommands.Add(("gnome-session-quit", "--logout --no-prompt"));
        logoutCommands.Add(("pkill", $"-u {Environment.UserName}"));

        foreach (var (cmd, args) in logoutCommands)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = cmd,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    await proc.WaitForExitAsync();
                    if (proc.ExitCode == 0) return;
                }
            }
            catch
            {
                // Try next
            }
        }

        _context.API.ShowError("Log Out Failed", "Could not terminate user session.");
    }

    private async Task EmptyTrashAsync()
    {
        try
        {
            // Try gio trash --empty
            var psi = new ProcessStartInfo
            {
                FileName = "gio",
                Arguments = "trash --empty",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                await proc.WaitForExitAsync();
                if (proc.ExitCode == 0)
                {
                    _context.API.ShowMsg("Trash Emptied", "All items in trash have been deleted", "Images/trash.png");
                    return;
                }
            }
        }
        catch
        {
            // Fallback to manual removal in user Trash directory
        }

        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var trashFiles = Path.Combine(home, ".local", "share", "Trash", "files");
            var trashInfo = Path.Combine(home, ".local", "share", "Trash", "info");

            if (Directory.Exists(trashFiles))
            {
                foreach (var file in Directory.EnumerateFileSystemEntries(trashFiles))
                {
                    try
                    {
                        if (Directory.Exists(file)) Directory.Delete(file, true);
                        else if (File.Exists(file)) File.Delete(file);
                    }
                    catch { }
                }
            }

            if (Directory.Exists(trashInfo))
            {
                foreach (var file in Directory.EnumerateFileSystemEntries(trashInfo))
                {
                    try
                    {
                        if (Directory.Exists(file)) Directory.Delete(file, true);
                        else if (File.Exists(file)) File.Delete(file);
                    }
                    catch { }
                }
            }

            _context.API.ShowMsg("Trash Emptied", "All items in trash have been deleted", "Images/trash.png");
        }
        catch (Exception ex)
        {
            _context.API.ShowError("Failed to Empty Trash", ex.Message);
        }
    }

    private Task OpenFileManagerAsync()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        try
        {
            if (_context.API != null)
            {
                _context.API.OpenDirectory(home);
                return Task.CompletedTask;
            }
        }
        catch { }

        try
        {
            Process.Start(new ProcessStartInfo("xdg-open", $"\"{home}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _context.API.ShowError("Could not open file manager", ex.Message);
        }

        return Task.CompletedTask;
    }

    private Task OpenTerminalAsync()
    {
        var envTerminal = Environment.GetEnvironmentVariable("TERMINAL");
        var candidates = new List<string>();
        if (!string.IsNullOrEmpty(envTerminal)) candidates.Add(envTerminal);
        candidates.AddRange(new[] { "x-terminal-emulator", "gnome-terminal", "konsole", "xfce4-terminal", "alacritty", "kitty", "xterm" });

        foreach (var term in candidates)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = term,
                    UseShellExecute = true
                };
                Process.Start(psi);
                return Task.CompletedTask;
            }
            catch
            {
                // Try next
            }
        }

        _context.API.ShowError("Could not open terminal", "No terminal emulator was found on this system.");
        return Task.CompletedTask;
    }
}
