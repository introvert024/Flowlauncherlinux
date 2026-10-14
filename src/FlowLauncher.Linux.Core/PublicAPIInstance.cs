using System.Diagnostics;
using FlowLauncher.Linux.Core.Plugin;
using FlowLauncher.Linux.Infrastructure;
using FlowLauncher.Linux.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace FlowLauncher.Linux.Core;

public class PublicAPIInstance : IPublicAPI
{
    private readonly IServiceProvider _serviceProvider;
    private IPluginManager? _pluginManager;
    private IPluginManager PluginManager => _pluginManager ??= _serviceProvider.GetRequiredService<IPluginManager>();

    public static event Action? RequestShowMainWindow;
    public static event Action? RequestHideMainWindow;
    public static event Action<string, bool>? RequestChangeQuery;

    public PublicAPIInstance(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void ShowMsg(string title, string subTitle = "", string iconPath = "")
    {
        // On Linux, use notify-send for desktop notifications
        try
        {
            var args = $"\"{title}\" \"{subTitle}\"";
            Process.Start(new ProcessStartInfo { FileName = "notify-send", Arguments = args, UseShellExecute = false, CreateNoWindow = true });
        }
        catch { /* notify-send not available */ }
    }

    public void OpenSettingDialog() { /* TODO: Open settings window */ }
    public void ChangeQuery(string query, bool requery = false) => RequestChangeQuery?.Invoke(query, requery);
    public void RestartApp()
    {
        var exe = System.Reflection.Assembly.GetEntryAssembly()?.Location ?? string.Empty;
        Process.Start(new ProcessStartInfo { FileName = exe, UseShellExecute = true });
        Environment.Exit(0);
    }
    public void ShowMainWindow() => RequestShowMainWindow?.Invoke();
    public void HideMainWindow() => RequestHideMainWindow?.Invoke();
    public string GetTranslation(string key) => key; // TODO: Internationalization
    public List<PluginPair> GetAllPlugins() => PluginManager.GetAllPlugins().ToList();
    public Task<List<Result>> QueryAsync(string query) => PluginManager.QueryAsync(Query.Parse(query), CancellationToken.None);
    public void OpenUrl(string url) => LinuxHelper.OpenUrl(url);
    public void OpenDirectory(string directoryPath, string? fileName = null) => LinuxHelper.OpenFileManager(directoryPath);
    public void StartProcess(ProcessStartInfo info) { try { Process.Start(info); } catch { } }
    public void SavePluginSettings() { }
    public void ReloadAllPluginData() => _ = PluginManager.ReloadPluginsAsync();
    public void ShowError(string title, string subTitle = "") => ShowMsg(title, subTitle);
}
