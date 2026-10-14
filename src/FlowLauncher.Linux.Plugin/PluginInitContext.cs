namespace FlowLauncher.Linux.Plugin;

public class PluginInitContext
{
    public PluginMetadata Metadata { get; init; } = new();
    public IPublicAPI API { get; init; } = null!;
}
