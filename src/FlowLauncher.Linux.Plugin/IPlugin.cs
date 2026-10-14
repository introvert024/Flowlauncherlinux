namespace FlowLauncher.Linux.Plugin;

public interface IPlugin
{
    Task InitAsync(PluginInitContext context);
    Task<List<Result>> QueryAsync(Query query, CancellationToken token);
}
