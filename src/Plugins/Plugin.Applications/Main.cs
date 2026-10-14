using System.Diagnostics;
using FlowLauncher.Linux.Infrastructure;
using FlowLauncher.Linux.Infrastructure.XdgApplications;
using FlowLauncher.Linux.Plugin;

namespace FlowLauncher.Linux.Plugins.Applications;

public class Main : IPlugin
{
    private List<XdgAppEntry> _apps = new();
    private IPublicAPI _api = null!;

    public Task InitAsync(PluginInitContext context)
    {
        _api = context.API;
        // Load apps in background
        Task.Run(LoadApplications);
        return Task.CompletedTask;
    }

    private void LoadApplications()
    {
        _apps = XdgApplicationsScanner.ScanAll();
    }

    public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(query.Search))
            return Task.FromResult(new List<Result>());

        var search = query.Search.ToLowerInvariant();
        var results = new List<Result>();

        foreach (var app in _apps)
        {
            if (token.IsCancellationRequested) break;

            // Score the app against the search term
            var score = ScoreApp(app, search);
            if (score <= 0) continue;

            var appCopy = app;
            results.Add(new Result
            {
                Title = app.Name,
                SubTitle = app.Comment.Length > 0 ? app.Comment : app.GenericName,
                IcoPath = ResolveIcon(app.Icon),
                Score = score,
                AutoCompleteText = app.Name,
                Action = ctx =>
                {
                    LaunchApp(appCopy);
                    return ValueTask.FromResult(true);
                }
            });
        }

        // Sort by score descending, take top 10
        results.Sort((a, b) => b.Score.CompareTo(a.Score));
        return Task.FromResult(results.Take(10).ToList());
    }

    private int ScoreApp(XdgAppEntry app, string search)
    {
        var name = app.Name.ToLowerInvariant();
        var genericName = app.GenericName.ToLowerInvariant();
        var keywords = app.Keywords.ToLowerInvariant();
        var comment = app.Comment.ToLowerInvariant();

        // Exact name match
        if (name == search) return 200;
        // Name starts with search
        if (name.StartsWith(search)) return 150 + search.Length;
        // Name contains search
        if (name.Contains(search)) return 100;
        // Generic name matches
        if (genericName.Contains(search)) return 80;
        // Keywords match
        if (keywords.Contains(search)) return 60;
        // Comment contains
        if (comment.Contains(search)) return 40;
        // Fuzzy: all search chars appear in name in order
        if (FuzzyMatch(name, search)) return 20;

        return 0;
    }

    private bool FuzzyMatch(string str, string pattern)
    {
        int pIdx = 0;
        foreach (char c in str)
        {
            if (pIdx < pattern.Length && c == pattern[pIdx]) pIdx++;
            if (pIdx == pattern.Length) return true;
        }
        return false;
    }

    private string ResolveIcon(string iconName)
    {
        if (string.IsNullOrEmpty(iconName)) return string.Empty;
        // If it's an absolute path
        if (iconName.StartsWith('/') && File.Exists(iconName)) return iconName;
        // Search in hicolor icon theme
        var sizes = new[] { "256x256", "128x128", "64x64", "48x48", "32x32", "scalable" };
        var exts = new[] { ".png", ".svg", ".xpm" };
        var iconDirs = new[]
        {
            "/usr/share/icons/hicolor",
            "/usr/share/icons/gnome",
            "/usr/share/icons/Adwaita",
            "/usr/share/pixmaps"
        };
        foreach (var dir in iconDirs)
        {
            foreach (var size in sizes)
            {
                foreach (var ext in exts)
                {
                    var path = Path.Combine(dir, size, "apps", iconName + ext);
                    if (File.Exists(path)) return path;
                }
            }
            // Pixmaps - no subdirectory
            foreach (var ext in exts)
            {
                var path = Path.Combine(dir, iconName + ext);
                if (File.Exists(path)) return path;
            }
        }
        return string.Empty;
    }

    private void LaunchApp(XdgAppEntry app)
    {
        try
        {
            var exec = app.GetCleanExec();
            var parts = exec.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;
            var psi = new ProcessStartInfo
            {
                FileName = parts[0],
                Arguments = parts.Length > 1 ? parts[1] : string.Empty,
                UseShellExecute = false
            };
            Process.Start(psi);
        }
        catch { }
    }
}
