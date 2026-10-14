using FlowLauncher.Linux.Plugin;

namespace FlowLauncher.Linux.Core.Plugin;

public interface IPluginManager
{
    Task InitializeAsync();
    Task<List<Result>> QueryAsync(Query query, CancellationToken token);
    IEnumerable<PluginPair> GetAllPlugins();
    PluginPair? GetPluginForId(string id);
    Task ReloadPluginsAsync();
}
