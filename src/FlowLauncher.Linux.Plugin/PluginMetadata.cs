namespace FlowLauncher.Linux.Plugin;

public class PluginMetadata
{
    public string ID { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Language { get; set; } = "csharp";
    public string Website { get; set; } = string.Empty;
    public string IcoPath { get; set; } = string.Empty;
    public string ExecuteFilePath { get; set; } = string.Empty;
    public string PluginDirectory { get; set; } = string.Empty;
    public string ActionKeyword { get; set; } = string.Empty;
    public List<string> ActionKeywords { get; set; } = new();
    public bool Disabled { get; set; }
    public int Priority { get; set; }
}
