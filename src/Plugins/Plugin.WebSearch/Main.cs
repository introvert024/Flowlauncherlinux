using System.Diagnostics;
using FlowLauncher.Linux.Plugin;

namespace FlowLauncher.Linux.Plugins.WebSearch;

public class Main : IPlugin
{
    private PluginInitContext _context = null!;

    private record SearchSource(
        string Name,
        string Domain,
        string UrlTemplate,
        string IconPath,
        string[] Keywords);

    private static readonly List<SearchSource> Sources = new()
    {
        new(
            "Google",
            "google.com",
            "https://www.google.com/search?q={0}",
            "Images/google.png",
            new[] { "g", "google" }
        ),
        new(
            "DuckDuckGo",
            "duckduckgo.com",
            "https://duckduckgo.com/?q={0}",
            "Images/duckduckgo.png",
            new[] { "?", "d", "ddg", "duckduckgo" }
        ),
        new(
            "Bing",
            "bing.com",
            "https://www.bing.com/search?q={0}",
            "Images/bing.png",
            new[] { "b", "bing" }
        ),
        new(
            "YouTube",
            "youtube.com",
            "https://www.youtube.com/results?search_query={0}",
            "Images/youtube.png",
            new[] { "yt", "youtube" }
        ),
        new(
            "GitHub",
            "github.com",
            "https://github.com/search?q={0}",
            "Images/github.png",
            new[] { "gh", "github" }
        )
    };

    public Task InitAsync(PluginInitContext context)
    {
        _context = context;
        return Task.CompletedTask;
    }

    public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        var rawSearch = query.Search?.Trim() ?? string.Empty;
        var actionKeyword = query.ActionKeyword?.Trim() ?? string.Empty;

        // Determine if first word is a specific provider shortcut (e.g., 'yt linux', 'gh avalonia')
        SearchSource? primarySource = null;
        var searchTerm = rawSearch;

        var firstSpaceIdx = rawSearch.IndexOf(' ');
        if (firstSpaceIdx > 0)
        {
            var firstWord = rawSearch[..firstSpaceIdx].ToLowerInvariant();
            var matched = Sources.FirstOrDefault(s => s.Keywords.Contains(firstWord));
            if (matched != null)
            {
                primarySource = matched;
                searchTerm = rawSearch[(firstSpaceIdx + 1)..].Trim();
            }
        }

        // If not matched by first word, determine by ActionKeyword
        if (primarySource == null)
        {
            if (actionKeyword == "?")
            {
                primarySource = Sources.First(s => s.Name == "DuckDuckGo");
            }
            else
            {
                primarySource = Sources.FirstOrDefault(s => s.Keywords.Contains(actionKeyword.ToLowerInvariant()))
                                ?? Sources.First(s => s.Name == "Google");
            }
        }

        var results = new List<Result>();

        // If search term is empty, display helpful entry points
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            results.Add(new Result
            {
                Title = $"Search with {primarySource.Name}",
                SubTitle = $"Type your query to search on {primarySource.Domain}",
                IcoPath = primarySource.IconPath,
                Score = 100
            });

            foreach (var other in Sources.Where(s => s != primarySource))
            {
                results.Add(new Result
                {
                    Title = $"Search with {other.Name}",
                    SubTitle = $"Keyword: {other.Keywords[0]} (e.g. '{other.Keywords[0]} search text')",
                    IcoPath = other.IconPath,
                    Score = 50,
                    AutoCompleteText = $"{other.Keywords[0]} "
                });
            }

            return Task.FromResult(results);
        }

        var encodedSearch = Uri.EscapeDataString(searchTerm);

        // 1. Primary provider result (Score = 100)
        var primaryUrl = string.Format(primarySource.UrlTemplate, encodedSearch);
        results.Add(new Result
        {
            Title = $"Search {primarySource.Name} for '{searchTerm}'",
            SubTitle = $"Open {primarySource.Domain} in default web browser",
            IcoPath = primarySource.IconPath,
            Score = 100,
            Action = _ =>
            {
                OpenBrowserUrl(primaryUrl);
                return ValueTask.FromResult(true);
            }
        });

        // 2. Secondary providers (Descending scores: 80, 75, 70, 65)
        var secondaryScore = 80;
        foreach (var other in Sources.Where(s => s != primarySource))
        {
            var otherUrl = string.Format(other.UrlTemplate, encodedSearch);
            var sourceName = other.Name;
            var sourceDomain = other.Domain;
            var sourceIcon = other.IconPath;

            results.Add(new Result
            {
                Title = $"Search {sourceName} for '{searchTerm}'",
                SubTitle = $"Open {sourceDomain} in default web browser",
                IcoPath = sourceIcon,
                Score = secondaryScore,
                Action = _ =>
                {
                    OpenBrowserUrl(otherUrl);
                    return ValueTask.FromResult(true);
                }
            });

            secondaryScore -= 5;
        }

        return Task.FromResult(results);
    }

    private void OpenBrowserUrl(string url)
    {
        try
        {
            _context.API.HideMainWindow();

            if (_context.API != null)
            {
                _context.API.OpenUrl(url);
                return;
            }
        }
        catch
        {
            // Fallback to manual process launch
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $"\"{url}\"",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            _context.API.ShowError("Could not open browser", ex.Message);
        }
    }
}
