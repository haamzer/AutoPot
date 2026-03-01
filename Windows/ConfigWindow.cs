using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace AutoPot.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin) : base("AutoPot Settings###AutoPotConfig")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;
        Size = new Vector2(350, 160);
        SizeCondition = ImGuiCond.Always;

        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var autoSwap = configuration.AutoSwapOnDutyEnter;
        if (ImGui.Checkbox("Auto-swap potion on duty enter", ref autoSwap))
        {
            configuration.AutoSwapOnDutyEnter = autoSwap;
            configuration.Save();
        }

        var chatNotif = configuration.ShowChatNotification;
        if (ImGui.Checkbox("Show chat notification", ref chatNotif))
        {
            configuration.ShowChatNotification = chatNotif;
            configuration.Save();
        }

        var showWindow = configuration.ShowWindowOnDutyEnter;
        if (ImGui.Checkbox("Show AutoPot window on duty enter", ref showWindow))
        {
            configuration.ShowWindowOnDutyEnter = showWindow;
            configuration.Save();
        }
    }
}