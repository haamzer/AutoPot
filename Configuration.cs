using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace AutoPot;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool AutoSwapOnDutyEnter { get; set; } = true;

    public bool ShowChatNotification { get; set; } = true;

    public bool ShowWindowOnDutyEnter { get; set; } = false;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}