using System;
using System.Linq;
using System.Numerics;
using AutoPot.Data;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AutoPot.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    private int selectedUltimateIndex = 0;
    private readonly string[] ultimateNames;
    private readonly uint[] ultimateTerritoryIds;

    private int manualTerritoryId = 0;

    public MainWindow(Plugin plugin)
        : base("AutoPot##MainWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(430, 520),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;

        var ultimates = DutyData.Ultimates.ToList();
        ultimateNames = ultimates.Select(d => d.Value.Name).ToArray();
        ultimateTerritoryIds = ultimates.Select(d => d.Key).ToArray();
    }

    public void Dispose() { }

    public override void Draw()
    {
        var manager = plugin.AutoPotManager;

        ImGui.Text("AutoPot - Automatic Potion Selector");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text($"Territory: {manager.CurrentTerritoryId}");

        if (manager.CurrentDutyType != DetectedDutyType.None)
        {
            var typeLabel = manager.CurrentDutyType switch
            {
                DetectedDutyType.Ultimate => "Ultimate",
                DetectedDutyType.Savage => "Savage",
                DetectedDutyType.NormalRaid => "Normal Raid",
                _ => "Unknown"
            };
            ImGui.Text($"Duty: {manager.CurrentDutyName} [{typeLabel}]");

            if (manager.CurrentDutyType == DetectedDutyType.Ultimate)
                ImGui.TextColored(new Vector4(0.6f, 0.8f, 1.0f, 1.0f), "Mode: Cheapest potion that caps stats");
            else
                ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.4f, 1.0f), "Mode: Highest available potion");
        }

        if (manager.CurrentMainStat.HasValue)
            ImGui.Text($"Main Stat: {manager.CurrentMainStat.Value}");

        ImGui.Spacing();

        if (manager.RecommendedTier.HasValue && manager.CurrentMainStat.HasValue)
        {
            var tierName = PotionData.GetTierName(manager.RecommendedTier.Value);
            var statName = PotionData.GetStatName(manager.CurrentMainStat.Value);

            bool isGreen;
            if (manager.CurrentDutyType == DetectedDutyType.Ultimate && manager.CurrentUltimate != null)
                isGreen = (int)manager.RecommendedTier.Value >= (int)manager.CurrentUltimate.OptimalTier;
            else
                isGreen = true;

            var color = isGreen
                ? new Vector4(0.0f, 1.0f, 0.0f, 1.0f)
                : new Vector4(1.0f, 1.0f, 0.0f, 1.0f);

            ImGui.TextColored(color, $"Recommended: {tierName} of {statName}");

            if (!isGreen)
                ImGui.TextColored(new Vector4(1.0f, 1.0f, 0.0f, 1.0f), "Warning: Below optimal tier, stats won't cap!");
        }
        else
        {
            ImGui.Text($"Status: {manager.StatusMessage}");
        }

        ImGui.Spacing();

        if (manager.RecommendedItemId.HasValue && !manager.HasSwapped)
        {
            if (ImGui.Button("Swap Hotbar Potion"))
                manager.SwapHotbarPotion();
        }

        if (!string.IsNullOrEmpty(manager.SwapMessage))
        {
            var swapColor = manager.HasSwapped
                ? new Vector4(0.0f, 1.0f, 0.0f, 1.0f)
                : new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
            ImGui.TextColored(swapColor, manager.SwapMessage);
        }

        if (ImGui.Button("Refresh"))
            manager.Refresh();

        ImGui.SameLine();
        if (ImGui.Button("Settings"))
            plugin.ToggleConfigUi();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Test Mode");
        ImGui.Spacing();

        ImGui.Text("Simulate Ultimate:");
        ImGui.SetNextItemWidth(200);
        ImGui.Combo("##UltimateCombo", ref selectedUltimateIndex, ultimateNames, ultimateNames.Length);
        ImGui.SameLine();
        if (ImGui.Button("Simulate##Ultimate"))
        {
            if (selectedUltimateIndex >= 0 && selectedUltimateIndex < ultimateTerritoryIds.Length)
                manager.SimulateDuty(ultimateTerritoryIds[selectedUltimateIndex]);
        }

        ImGui.Spacing();

        ImGui.Text("Simulate by Territory ID:");
        ImGui.SetNextItemWidth(120);
        ImGui.InputInt("##TerritoryInput", ref manualTerritoryId);
        ImGui.SameLine();
        if (ImGui.Button("Simulate##Manual"))
        {
            if (manualTerritoryId > 0)
                manager.SimulateDuty((uint)manualTerritoryId);
        }

        ImGui.Spacing();
        if (ImGui.Button("Clear Simulation"))
            manager.ClearSimulation();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Ultimate Potion Reference"))
        {
            if (ImGui.BeginTable("UltimateTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Duty", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Optimal Tier", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Territory", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableHeadersRow();

                foreach (var (territoryId, duty) in DutyData.Ultimates)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(duty.Name);
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(PotionData.GetTierName(duty.OptimalTier));
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(territoryId.ToString());
                }

                ImGui.EndTable();
            }

            ImGui.Spacing();
            ImGui.TextWrapped("Ultimates use the cheapest potion that caps stats. Savage and Normal raids automatically use the highest potion in your inventory.");
        }
    }
}