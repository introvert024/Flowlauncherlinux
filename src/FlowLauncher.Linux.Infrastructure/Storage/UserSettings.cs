using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowLauncher.Linux.Infrastructure.Storage;

public class UserSettings
{
    public string Hotkey { get; set; } = "alt+space";
    public bool StartupAutoRun { get; set; } = false;
    public bool HideOnLostFocus { get; set; } = true;
    public bool RememberLastLaunchLocation { get; set; } = false;
    public double WindowLeft { get; set; } = -1;
    public double WindowTop { get; set; } = -1;
    public double MainWindowWidth { get; set; } = 580;
    public int MaxResultsToShow { get; set; } = 6;
    public string Theme { get; set; } = "Light";
    public bool ShowResultsIcons { get; set; } = true;
    public string Language { get; set; } = "en-us";
    public bool ShowOpenResultHotkey { get; set; } = true;
    public bool AlwaysPreview { get; set; } = false;
    public bool QuerySearchPrecision { get; set; } = false;
    public int AnimationSpeed { get; set; } = 300;
    public bool UsePinyin { get; set; } = false;
    public List<PluginSettings> Plugins { get; set; } = new();
}

public class PluginSettings
{
    public string ID { get; set; } = string.Empty;
    public bool Disabled { get; set; } = false;
    public int Priority { get; set; } = 0;
    public string ActionKeyword { get; set; } = string.Empty;
    public Dictionary<string, object?> Settings { get; set; } = new();
}
