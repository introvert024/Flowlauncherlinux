using System.Diagnostics;
using FlowLauncher.Linux.Plugin;

namespace FlowLauncher.Linux.Plugins.Explorer;

public class Main : IPlugin
{
    private PluginInitContext _context = null!;
    private string _homeDir = string.Empty;
    private string? _locateCommand;

    private const string FolderIcon = "Images/folder.png";
    private const string FileIcon = "Images/file.png";

    public Task InitAsync(PluginInitContext context)
    {
        _context = context;
        _homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        DetectLocateCommand();
        return Task.CompletedTask;
    }

    public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        var raw = query.Search?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<Result>();
        }

        // Only activate if search starts with / or ~
        if (!raw.StartsWith('/') && !raw.StartsWith('~'))
        {
            return new List<Result>();
        }

        var results = new List<Result>();
        var seenPaths = new HashSet<string>(StringComparer.Ordinal);

        // Normalize ~ to user's home directory
        var expandedPath = ExpandHomePath(raw);

        // 1. Direct path completion / navigation
        AddDirectPathResults(expandedPath, results, seenPaths);

        if (token.IsCancellationRequested)
        {
            return results;
        }

        // 2. Locate / Find search if few results or user entered a specific search target
        if (results.Count < 20)
        {
            await AddSearchCommandResultsAsync(expandedPath, raw, results, seenPaths, token);
        }

        return results;
    }

    private string ExpandHomePath(string path)
    {
        if (path == "~")
        {
            return _homeDir;
        }

        if (path.StartsWith("~/"))
        {
            return Path.Combine(_homeDir, path[2..]);
        }

        if (path.StartsWith("~"))
        {
            return Path.Combine(_homeDir, path[1..]);
        }

        return path;
    }

    private string CollapseHomePath(string path)
    {
        if (!string.IsNullOrEmpty(_homeDir) && path.StartsWith(_homeDir, StringComparison.Ordinal))
        {
            return "~" + path[_homeDir.Length..];
        }

        return path;
    }

    private void AddDirectPathResults(string path, List<Result> results, HashSet<string> seenPaths)
    {
        try
        {
            // Case 1: Exact directory match
            if (Directory.Exists(path))
            {
                var dirInfo = new DirectoryInfo(path);
                var displayPath = CollapseHomePath(dirInfo.FullName);

                results.Add(CreateFolderResult(dirInfo.FullName, displayPath, 150));
                seenPaths.Add(dirInfo.FullName);

                // List contents inside this directory
                try
                {
                    var subDirs = dirInfo.EnumerateDirectories()
                        .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                        .Take(30);

                    foreach (var subDir in subDirs)
                    {
                        if (seenPaths.Add(subDir.FullName))
                        {
                            results.Add(CreateFolderResult(subDir.FullName, CollapseHomePath(subDir.FullName), 100));
                        }
                    }

                    var files = dirInfo.EnumerateFiles()
                        .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                        .Take(30);

                    foreach (var file in files)
                    {
                        if (seenPaths.Add(file.FullName))
                        {
                            results.Add(CreateFileResult(file.FullName, CollapseHomePath(file.FullName), file.Length, 90));
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Permission denied on children, but directory itself was already added
                }

                return;
            }

            // Case 2: Exact file match
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                var displayPath = CollapseHomePath(fileInfo.FullName);
                results.Add(CreateFileResult(fileInfo.FullName, displayPath, fileInfo.Length, 150));
                seenPaths.Add(fileInfo.FullName);
                return;
            }

            // Case 3: Partial path / completion in parent directory
            var parentDir = Path.GetDirectoryName(path);
            var searchPattern = Path.GetFileName(path);

            if (string.IsNullOrEmpty(parentDir) && path.StartsWith('/'))
            {
                parentDir = "/";
            }

            if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir))
            {
                var parentInfo = new DirectoryInfo(parentDir);

                try
                {
                    var matchingDirs = parentInfo.EnumerateDirectories()
                        .Where(d => d.Name.Contains(searchPattern, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(d => d.Name.StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase))
                        .ThenBy(d => d.Name)
                        .Take(25);

                    foreach (var dir in matchingDirs)
                    {
                        if (seenPaths.Add(dir.FullName))
                        {
                            var score = dir.Name.StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase) ? 100 : 80;
                            results.Add(CreateFolderResult(dir.FullName, CollapseHomePath(dir.FullName), score));
                        }
                    }

                    var matchingFiles = parentInfo.EnumerateFiles()
                        .Where(f => f.Name.Contains(searchPattern, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(f => f.Name.StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase))
                        .ThenBy(f => f.Name)
                        .Take(25);

                    foreach (var file in matchingFiles)
                    {
                        if (seenPaths.Add(file.FullName))
                        {
                            var score = file.Name.StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase) ? 95 : 75;
                            results.Add(CreateFileResult(file.FullName, CollapseHomePath(file.FullName), file.Length, score));
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Ignore inaccessible directories
                }
            }
        }
        catch
        {
            // Ignore path format errors
        }
    }

    private async Task AddSearchCommandResultsAsync(
        string expandedPath,
        string rawQuery,
        List<Result> results,
        HashSet<string> seenPaths,
        CancellationToken token)
    {
        var pattern = Path.GetFileName(expandedPath);
        if (string.IsNullOrWhiteSpace(pattern) || pattern.Length < 2)
        {
            return;
        }

        var lines = new List<string>();

        // Try locate first (fastest)
        if (!string.IsNullOrEmpty(_locateCommand))
        {
            lines = await RunProcessLinesAsync(_locateCommand, $"-i -l 25 -- \"{pattern}\"", token);
        }

        // Fallback to find if locate returned nothing or is unavailable
        if (lines.Count == 0 && !token.IsCancellationRequested)
        {
            var searchRoot = Path.GetDirectoryName(expandedPath);
            if (string.IsNullOrEmpty(searchRoot) || !Directory.Exists(searchRoot))
            {
                searchRoot = expandedPath.StartsWith(_homeDir) ? _homeDir : "/";
            }

            var findArgs = $"\"{searchRoot}\" -maxdepth 3 -iname \"*{pattern}*\" 2>/dev/null";
            lines = await RunProcessLinesAsync("/bin/sh", $"-c \"find {findArgs}\"", token);
        }

        foreach (var line in lines)
        {
            if (token.IsCancellationRequested) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fullPath = line.Trim();
            if (!seenPaths.Add(fullPath)) continue;

            try
            {
                if (Directory.Exists(fullPath))
                {
                    results.Add(CreateFolderResult(fullPath, CollapseHomePath(fullPath), 60));
                }
                else if (File.Exists(fullPath))
                {
                    var length = new FileInfo(fullPath).Length;
                    results.Add(CreateFileResult(fullPath, CollapseHomePath(fullPath), length, 55));
                }
            }
            catch
            {
                // File/folder might have been deleted or permission denied
            }
        }
    }

    private Result CreateFolderResult(string fullPath, string displayPath, int score)
    {
        var name = Path.GetFileName(fullPath);
        if (string.IsNullOrEmpty(name)) name = "/";

        return new Result
        {
            Title = name,
            SubTitle = displayPath,
            IcoPath = FolderIcon,
            Score = score,
            AutoCompleteText = displayPath.EndsWith('/') ? displayPath : displayPath + "/",
            Action = _ =>
            {
                OpenDirectory(fullPath);
                return ValueTask.FromResult(true);
            }
        };
    }

    private Result CreateFileResult(string fullPath, string displayPath, long sizeBytes, int score)
    {
        var name = Path.GetFileName(fullPath);
        var formattedSize = FormatFileSize(sizeBytes);

        return new Result
        {
            Title = name,
            SubTitle = $"{displayPath}  ({formattedSize})",
            IcoPath = GetFileIcon(fullPath),
            Score = score,
            AutoCompleteText = displayPath,
            Action = context =>
            {
                if (context.CtrlPressed)
                {
                    var parent = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        OpenDirectory(parent);
                        return ValueTask.FromResult(true);
                    }
                }

                OpenFile(fullPath);
                return ValueTask.FromResult(true);
            }
        };
    }

    private void OpenFile(string path)
    {
        try
        {
            _context.API.HideMainWindow();

            var psi = new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            _context.API.ShowError("Could not open file", ex.Message);
        }
    }

    private void OpenDirectory(string path)
    {
        try
        {
            _context.API.HideMainWindow();

            if (_context.API != null)
            {
                _context.API.OpenDirectory(path);
                return;
            }
        }
        catch
        {
            // Fallback
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            _context.API.ShowError("Could not open folder", ex.Message);
        }
    }

    private void DetectLocateCommand()
    {
        var candidates = new[] { "plocate", "locate", "mlocate" };
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "/usr/bin:/bin:/usr/local/bin";
        var dirs = pathEnv.Split(':', StringSplitOptions.RemoveEmptyEntries);

        foreach (var cmd in candidates)
        {
            foreach (var dir in dirs)
            {
                if (File.Exists(Path.Combine(dir, cmd)))
                {
                    _locateCommand = cmd;
                    return;
                }
            }
        }
    }

    private static async Task<List<string>> RunProcessLinesAsync(string filename, string arguments, CancellationToken token)
    {
        var results = new List<string>();
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.Start();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(TimeSpan.FromSeconds(2)); // Guard against long searches

            while (!proc.StandardOutput.EndOfStream)
            {
                if (cts.Token.IsCancellationRequested)
                {
                    try { proc.Kill(); } catch { }
                    break;
                }

                var line = await proc.StandardOutput.ReadLineAsync(cts.Token);
                if (!string.IsNullOrWhiteSpace(line))
                {
                    results.Add(line.Trim());
                    if (results.Count >= 25) break;
                }
            }

            await proc.WaitForExitAsync(cts.Token);
        }
        catch
        {
            // Ignore process timeouts or failures
        }

        return results;
    }

    private static string GetFileIcon(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".svg" or ".webp" => "Images/image.png",
            ".pdf" or ".doc" or ".docx" or ".odt" or ".txt" or ".md" => "Images/document.png",
            ".zip" or ".tar" or ".gz" or ".bz2" or ".xz" or ".7z" => "Images/archive.png",
            ".sh" or ".bash" or ".py" or ".cs" or ".js" or ".ts" or ".cpp" or ".c" or ".h" or ".rs" or ".go" => "Images/code.png",
            _ => FileIcon
        };
    }

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }
}
