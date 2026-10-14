namespace FlowLauncher.Linux.Plugin;

public class Result
{
    public string Title { get; set; } = string.Empty;
    public string SubTitle { get; set; } = string.Empty;
    public string IcoPath { get; set; } = string.Empty;
    public int Score { get; set; }
    public Func<ActionContext, ValueTask<bool>>? Action { get; set; }
    public List<Result> ContextData { get; set; } = new();
    public bool CopyText { get; set; }
    public bool RoundedIcon { get; set; }
    public string? Glyph { get; set; }
    public string? AutoCompleteText { get; set; }
    public PluginMetadata? OriginPlugin { get; set; }
}
