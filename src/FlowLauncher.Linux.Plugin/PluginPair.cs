namespace FlowLauncher.Linux.Plugin;

public class PluginPair
{
    public IPlugin Plugin { get; init; } = null!;
    public PluginMetadata Metadata { get; init; } = new();
    public bool IsEnabled => !Metadata.Disabled;
}
