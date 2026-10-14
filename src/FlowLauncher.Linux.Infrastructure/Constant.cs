using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FlowLauncher.Linux.Infrastructure;

public static class Constant
{
    public const string FlowLauncher = "flow-launcher";
    public const string FlowLauncherFullName = "Flow Launcher";
    public const string Plugins = "Plugins";
    public const string PluginMetadataFileName = "plugin.json";
    public const string ApplicationFileName = FlowLauncher;

    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    public static readonly string ProgramDirectory = Path.GetDirectoryName(_assembly.Location) ?? AppContext.BaseDirectory;
    public static readonly string ExecutablePath = Path.Combine(ProgramDirectory, FlowLauncher);
    public static readonly string DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "flow-launcher");
    public static readonly string PreinstalledDirectory = Path.Combine(ProgramDirectory, Plugins);
    public static readonly string UserPluginsDirectory = Path.Combine(DataDirectory, Plugins);
    public const string IssuesUrl = "https://github.com/Flow-Launcher/Flow.Launcher/issues";
    public static readonly string Version = FileVersionInfo.GetVersionInfo(_assembly.Location).ProductVersion ?? "1.0.0";
    private static readonly string ImagesDirectory = Path.Combine(ProgramDirectory, "Images");
    public static readonly string DefaultIcon = Path.Combine(ImagesDirectory, "app.png");
    public static readonly string ErrorIcon = Path.Combine(ImagesDirectory, "app_error.png");
    public static readonly string MissingImgIcon = Path.Combine(ImagesDirectory, "app_missing_img.png");
    public const string DefaultTheme = "Light";
    public const string Light = "Light";
    public const string Dark = "Dark";
    public const string System = "System";
    public const string Themes = "Themes";
    public const string Settings = "Settings";
    public const string Logs = "Logs";
    public const string Cache = "Cache";
    public const string Website = "https://flowlauncher.com";
    public const string GitHub = "https://github.com/Flow-Launcher/Flow.Launcher";
    public const string Docs = "https://flowlauncher.com/docs";
}
