using System.Diagnostics;
using FlowLauncher.Linux.Plugin;

namespace FlowLauncher.Linux.Plugins.ProcessKiller;

public class Main : IPlugin
{
    private PluginInitContext _context = null!;
    private const string IconPath = "Images/kill.png";

    private record ProcessEntry(int Id, string Name, long WorkingSet64);

    public Task InitAsync(PluginInitContext context)
    {
        _context = context;
        return Task.CompletedTask;
    }

    public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        var searchTerm = query.Search?.Trim();
        var processEntries = GetRunningProcesses(token);

        if (token.IsCancellationRequested)
        {
            return Task.FromResult(new List<Result>());
        }

        var results = new List<Result>();

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            // Show top processes by memory usage
            var topProcesses = processEntries
                .OrderByDescending(p => p.WorkingSet64)
                .Take(30);

            foreach (var p in topProcesses)
            {
                results.Add(CreateProcessResult(p, 80));
            }

            return Task.FromResult(results);
        }

        bool isNumeric = int.TryParse(searchTerm, out var searchPid);

        foreach (var p in processEntries)
        {
            int score = 0;

            if (p.Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                score = 100;
            }
            else if (isNumeric && p.Id == searchPid)
            {
                score = 98;
            }
            else if (p.Name.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                score = 90;
            }
            else if (isNumeric && p.Id.ToString().StartsWith(searchTerm, StringComparison.Ordinal))
            {
                score = 85;
            }
            else if (p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                score = 75;
            }
            else if (p.Id.ToString().Contains(searchTerm, StringComparison.Ordinal))
            {
                score = 65;
            }

            if (score > 0)
            {
                results.Add(CreateProcessResult(p, score));
            }
        }

        results.Sort((a, b) =>
        {
            var cmp = b.Score.CompareTo(a.Score);
            return cmp != 0 ? cmp : string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
        });

        return Task.FromResult(results.Take(30).ToList());
    }

    private List<ProcessEntry> GetRunningProcesses(CancellationToken token)
    {
        var entries = new List<ProcessEntry>();
        var currentPid = Environment.ProcessId;

        Process[]? processes = null;
        try
        {
            processes = Process.GetProcesses();
            foreach (var p in processes)
            {
                if (token.IsCancellationRequested) break;

                try
                {
                    // Skip our own process
                    if (p.Id == currentPid) continue;

                    var name = p.ProcessName;
                    var mem = p.WorkingSet64;
                    entries.Add(new ProcessEntry(p.Id, name, mem));
                }
                catch
                {
                    // Ignore processes that exited or threw permission exceptions
                }
                finally
                {
                    p.Dispose();
                }
            }
        }
        catch
        {
            // Ignore failure to enumerate all processes
        }

        return entries;
    }

    private Result CreateProcessResult(ProcessEntry p, int score)
    {
        var formattedMem = FormatBytes(p.WorkingSet64);
        var entry = p;

        return new Result
        {
            Title = $"{entry.Name} (PID: {entry.Id})",
            SubTitle = $"Memory: {formattedMem} | Press Enter to terminate (Ctrl+Enter for force kill -9)",
            IcoPath = IconPath,
            Score = score,
            AutoCompleteText = entry.Name,
            Action = async context =>
            {
                return await KillProcessAsync(entry, context.CtrlPressed);
            }
        };
    }

    private async Task<bool> KillProcessAsync(ProcessEntry p, bool forceKill)
    {
        try
        {
            if (forceKill)
            {
                try
                {
                    using var killProc = Process.Start(new ProcessStartInfo
                    {
                        FileName = "kill",
                        Arguments = $"-9 {p.Id}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });

                    if (killProc != null)
                    {
                        await killProc.WaitForExitAsync();
                    }
                }
                catch
                {
                    using var proc = Process.GetProcessById(p.Id);
                    proc.Kill(entireProcessTree: true);
                }
            }
            else
            {
                using var proc = Process.GetProcessById(p.Id);
                proc.Kill(entireProcessTree: true);
            }

            _context.API.ShowMsg("Process Terminated", $"Successfully killed '{p.Name}' (PID: {p.Id})", IconPath);
            return true;
        }
        catch (ArgumentException)
        {
            _context.API.ShowMsg("Process Already Exited", $"Process '{p.Name}' (PID: {p.Id}) is no longer running.", IconPath);
            return true;
        }
        catch (Exception ex)
        {
            _context.API.ShowError($"Failed to Kill '{p.Name}'", $"{ex.Message}. You may need elevated permissions.");
            return false;
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "0 B";
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }
}
