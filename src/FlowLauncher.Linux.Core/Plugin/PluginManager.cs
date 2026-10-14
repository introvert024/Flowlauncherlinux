using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using FlowLauncher.Linux.Infrastructure;
using FlowLauncher.Linux.Infrastructure.Logger;
using FlowLauncher.Linux.Infrastructure.Storage;
using FlowLauncher.Linux.Plugin;

namespace FlowLauncher.Linux.Core.Plugin;

public class PluginManager : IPluginManager
{
    private readonly List<PluginPair> _plugins = new();
    private readonly UserSettings _settings;
    private readonly IPublicAPI _api;

    public PluginManager(UserSettings settings, IPublicAPI api)
    {
        _settings = settings;
        _api = api;
    }

    public async Task InitializeAsync()
    {
        Log.Info(nameof(PluginManager), "Loading plugins...");
        await LoadPluginsFromDirectoryAsync(Constant.PreinstalledDirectory);
        await LoadPluginsFromDirectoryAsync(Constant.UserPluginsDirectory);
        Log.Info(nameof(PluginManager), $"Loaded {_plugins.Count} plugin(s)");
    }

    private async Task LoadPluginsFromDirectoryAsync(string directory)
    {
        if (!Directory.Exists(directory)) return;
        foreach (var pluginDir in Directory.GetDirectories(directory))
        {
            try
            {
                await LoadPluginAsync(pluginDir);
            }
            catch (Exception ex)
            {
                Log.Error(nameof(PluginManager), $"Failed to load plugin from {pluginDir}", ex);
            }
        }
    }

    private async Task LoadPluginAsync(string pluginDirectory)
    {
        var metadataPath = Path.Combine(pluginDirectory, Constant.PluginMetadataFileName);
        if (!File.Exists(metadataPath)) return;

        var json = await File.ReadAllTextAsync(metadataPath);
        var metadata = JsonSerializer.Deserialize<PluginMetadata>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (metadata == null) return;
        metadata.PluginDirectory = pluginDirectory;

        // Apply saved settings
        var savedSettings = _settings.Plugins.FirstOrDefault(p => p.ID == metadata.ID);
        if (savedSettings != null)
        {
            metadata.Disabled = savedSettings.Disabled;
            metadata.Priority = savedSettings.Priority;
            if (!string.IsNullOrEmpty(savedSettings.ActionKeyword))
                metadata.ActionKeyword = savedSettings.ActionKeyword;
        }

        if (metadata.Disabled) return;

        IPlugin? plugin = null;
        var lang = metadata.Language?.ToLower() ?? string.Empty;

        if (lang == AllowedLanguage.CSharp)
        {
            plugin = LoadAssemblyPlugin(metadata);
        }
        else if (lang == AllowedLanguage.Python || lang == AllowedLanguage.Python3)
        {
            plugin = new JsonRpcPlugin(metadata, "python3");
        }
        else if (lang == AllowedLanguage.JavaScript || lang == AllowedLanguage.TypeScript)
        {
            plugin = new JsonRpcPlugin(metadata, "node");
        }
        else if (lang == AllowedLanguage.Executable)
        {
            plugin = new JsonRpcPlugin(metadata, null);
        }

        if (plugin == null) return;

        var context = new PluginInitContext { Metadata = metadata, API = _api };
        await plugin.InitAsync(context);
        _plugins.Add(new PluginPair { Plugin = plugin, Metadata = metadata });
        Log.Info(nameof(PluginManager), $"Loaded plugin: {metadata.Name}");
    }

    private IPlugin? LoadAssemblyPlugin(PluginMetadata metadata)
    {
        var executeFile = Path.Combine(metadata.PluginDirectory, metadata.ExecuteFilePath);
        if (!File.Exists(executeFile)) return null;

        var alc = new AssemblyLoadContext(metadata.ID, isCollectible: true);
        var asm = alc.LoadFromAssemblyPath(executeFile);

        // Find IPlugin implementation
        var pluginType = asm.GetTypes().FirstOrDefault(t =>
            !t.IsAbstract && t.IsClass && typeof(IPlugin).IsAssignableFrom(t));

        if (pluginType == null) return null;
        return Activator.CreateInstance(pluginType) as IPlugin;
    }

    public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        var allResults = new List<Result>();
        var tasks = _plugins
            .Where(p => MatchesKeyword(p.Metadata, query))
            .Select(async pair =>
            {
                try
                {
                    var results = await pair.Plugin.QueryAsync(query, token);
                    return results ?? new List<Result>();
                }
                catch (OperationCanceledException) { return new List<Result>(); }
                catch (Exception ex)
                {
                    Log.Error(nameof(PluginManager), $"Plugin {pair.Metadata.Name} query error", ex);
                    return new List<Result>();
                }
            });

        var results = await Task.WhenAll(tasks);
        foreach (var r in results) allResults.AddRange(r);
        return allResults;
    }

    private bool MatchesKeyword(PluginMetadata metadata, Query query)
    {
        if (string.IsNullOrEmpty(metadata.ActionKeyword)) return true;
        if (metadata.ActionKeyword == "*") return true;
        return query.ActionKeyword.Equals(metadata.ActionKeyword, StringComparison.OrdinalIgnoreCase) ||
               query.RawQuery.StartsWith(metadata.ActionKeyword, StringComparison.OrdinalIgnoreCase);
    }

    public IEnumerable<PluginPair> GetAllPlugins() => _plugins;
    public PluginPair? GetPluginForId(string id) => _plugins.FirstOrDefault(p => p.Metadata.ID == id);
    public Task ReloadPluginsAsync() { _plugins.Clear(); return InitializeAsync(); }
}
