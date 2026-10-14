using System.Diagnostics;

namespace FlowLauncher.Linux.Plugin;

public interface IPublicAPI
{
    void ShowMsg(string title, string subTitle = "", string iconPath = "");
    void OpenSettingDialog();
    void ChangeQuery(string query, bool requery = false);
    void RestartApp();
    void ShowMainWindow();
    void HideMainWindow();
    string GetTranslation(string key);
    List<PluginPair> GetAllPlugins();
    Task<List<Result>> QueryAsync(string query);
    void OpenUrl(string url);
    void OpenDirectory(string directoryPath, string? fileName = null);
    void StartProcess(ProcessStartInfo info);
    void SavePluginSettings();
    void ReloadAllPluginData();
    void ShowError(string title, string subTitle = "");
}
