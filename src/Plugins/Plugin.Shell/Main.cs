using System.Diagnostics;
using FlowLauncher.Linux.Plugin;

namespace FlowLauncher.Linux.Plugins.Shell;

public class Main : IPlugin
{
    private PluginInitContext _context = null!;
    private const string IconPath = "Images/terminal.png";
    private string? _detectedTerminal;
    private string _terminalDisplayName = "Terminal";

    private static readonly string[] KnownTerminals =
    {
        "x-terminal-emulator",
        "gnome-terminal",
        "konsole",
        "xfce4-terminal",
        "alacritty",
        "kitty",
        "foot",
        "terminator",
        "tilix",
        "urxvt",
        "xterm"
    };

    public Task InitAsync(PluginInitContext context)
    {
        _context = context;
        DetectTerminal();
        return Task.CompletedTask;
    }

    public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        var command = query.Search?.Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            return Task.FromResult(new List<Result>
            {
                new()
                {
                    Title = "Run shell command...",
                    SubTitle = $"Type command to run in {_terminalDisplayName} (e.g. > top, > ls -la, > uname -a)",
                    IcoPath = IconPath,
                    Score = 100
                }
            });
        }

        var results = new List<Result>
        {
            // Primary result: Open in terminal
            new()
            {
                Title = command,
                SubTitle = $"Press Enter to run in {_terminalDisplayName}",
                IcoPath = IconPath,
                Score = 100,
                Action = _ =>
                {
                    RunInTerminal(command);
                    return ValueTask.FromResult(true);
                }
            },
            // Secondary result: Run in background (silent)
            new()
            {
                Title = $"Run silently: {command}",
                SubTitle = "Execute command in background without opening a terminal window",
                IcoPath = IconPath,
                Score = 80,
                Action = _ =>
                {
                    RunSilently(command);
                    return ValueTask.FromResult(true);
                }
            },
            // Tertiary result: Run with sudo in terminal
            new()
            {
                Title = $"Run as sudo: sudo {command}",
                SubTitle = $"Execute with root privileges in {_terminalDisplayName}",
                IcoPath = IconPath,
                Score = 70,
                Action = _ =>
                {
                    RunInTerminal($"sudo {command}");
                    return ValueTask.FromResult(true);
                }
            }
        };

        return Task.FromResult(results);
    }

    private void DetectTerminal()
    {
        // 1. Check user configured or standard environment variable
        var envTerminal = Environment.GetEnvironmentVariable("TERMINAL");
        if (!string.IsNullOrWhiteSpace(envTerminal) && IsCommandAvailable(envTerminal))
        {
            _detectedTerminal = envTerminal;
            _terminalDisplayName = Path.GetFileName(envTerminal);
            return;
        }

        // 2. Search known terminal emulators
        foreach (var terminal in KnownTerminals)
        {
            if (IsCommandAvailable(terminal))
            {
                _detectedTerminal = terminal;
                _terminalDisplayName = terminal;
                return;
            }
        }

        _detectedTerminal = null;
        _terminalDisplayName = "Terminal";
    }

    private static bool IsCommandAvailable(string command)
    {
        try
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "/usr/bin:/bin:/usr/local/bin";
            var paths = pathEnv.Split(':', StringSplitOptions.RemoveEmptyEntries);

            if (Path.IsPathRooted(command))
            {
                return File.Exists(command);
            }

            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, command);
                if (File.Exists(fullPath))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Ignore filesystem errors
        }

        return false;
    }

    private void RunInTerminal(string command)
    {
        try
        {
            _context.API.HideMainWindow();

            var terminal = _detectedTerminal ?? "xterm";
            var terminalName = Path.GetFileName(terminal).ToLowerInvariant();

            // Prepare command string: keep window open or prompt on exit
            var escapedCmd = command.Replace("\"", "\\\"");
            var fullScript = $"{escapedCmd}; echo; read -n 1 -s -r -p 'Press any key to close...'; exit";

            ProcessStartInfo psi;

            switch (terminalName)
            {
                case "gnome-terminal":
                    psi = new ProcessStartInfo
                    {
                        FileName = terminal,
                        Arguments = $"-- bash -c \"{fullScript}\"",
                        UseShellExecute = false
                    };
                    break;

                case "konsole":
                    psi = new ProcessStartInfo
                    {
                        FileName = terminal,
                        Arguments = $"-e bash -c \"{fullScript}\"",
                        UseShellExecute = false
                    };
                    break;

                case "xfce4-terminal":
                    psi = new ProcessStartInfo
                    {
                        FileName = terminal,
                        Arguments = $"-e \"bash -c '{command.Replace("'", "'\\''")}; echo; read -n 1 -s -r -p \\'Press any key to close...\\''\"",
                        UseShellExecute = false
                    };
                    break;

                case "alacritty":
                case "kitty":
                case "foot":
                    psi = new ProcessStartInfo
                    {
                        FileName = terminal,
                        Arguments = $"-e bash -c \"{fullScript}\"",
                        UseShellExecute = false
                    };
                    break;

                case "xterm":
                case "urxvt":
                    psi = new ProcessStartInfo
                    {
                        FileName = terminal,
                        Arguments = $"-hold -e bash -c \"{escapedCmd}\"",
                        UseShellExecute = false
                    };
                    break;

                default:
                    // Generic fallback for x-terminal-emulator and other terminals
                    psi = new ProcessStartInfo
                    {
                        FileName = terminal,
                        Arguments = $"-e bash -c \"{fullScript}\"",
                        UseShellExecute = false
                    };
                    break;
            }

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            _context.API.ShowError("Shell Execution Failed", ex.Message);
        }
    }

    private void RunSilently(string command)
    {
        try
        {
            _context.API.HideMainWindow();

            var psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(psi);
            _context.API.ShowMsg("Command Started", command, IconPath);
        }
        catch (Exception ex)
        {
            _context.API.ShowError("Command Execution Failed", ex.Message);
        }
    }
}
