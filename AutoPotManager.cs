using System;
using System.Collections.Generic;
using System.Linq;
using AutoPot.Data;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;

using HotbarSlotType = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule.HotbarSlotType;

namespace AutoPot;

public enum DetectedDutyType
{
    None,
    Ultimate,   // Use cheapest that caps
    Savage,     // Use highest available
    NormalRaid, // Use highest available
}

public class AutoPotManager : IDisposable
{
    private readonly IClientState clientState;
    private readonly IPlayerState playerState;
    private readonly IDataManager dataManager;
    private readonly IFramework framework;
    private readonly IChatGui chatGui;
    private readonly IPluginLog log;
    private readonly Configuration configuration;

    // Current state
    public uint CurrentTerritoryId { get; private set; }
    public DutyInfo? CurrentUltimate { get; private set; }
    public DetectedDutyType CurrentDutyType { get; private set; } = DetectedDutyType.None;
    public string CurrentDutyName { get; private set; } = "";
    public MainStat? CurrentMainStat { get; private set; }
    public PotionTier? RecommendedTier { get; private set; }
    public uint? RecommendedItemId { get; private set; }
    public string StatusMessage { get; private set; } = "Waiting...";
    public string SwapMessage { get; private set; } = "";
    public bool HasSwapped { get; private set; } = false;

    private readonly HashSet<uint> allPotionItemIds;

    public AutoPotManager(IClientState clientState, IPlayerState playerState, IDataManager dataManager,
        IFramework framework, IChatGui chatGui, IPluginLog log, Configuration configuration)
    {
        this.clientState = clientState;
        this.playerState = playerState;
        this.dataManager = dataManager;
        this.framework = framework;
        this.chatGui = chatGui;
        this.log = log;
        this.configuration = configuration;

        allPotionItemIds = new HashSet<uint>();
        foreach (var entry in PotionData.PotionItemIds)
        {
            allPotionItemIds.Add(entry.Value);
            allPotionItemIds.Add(entry.Value + 1000000);
        }

        clientState.TerritoryChanged += OnTerritoryChanged;

        if (clientState.TerritoryType != 0)
            OnTerritoryChanged(clientState.TerritoryType);
    }

    internal void OnTerritoryChanged(ushort territoryId)
    {
        CurrentTerritoryId = territoryId;
        CurrentUltimate = null;
        CurrentDutyType = DetectedDutyType.None;
        CurrentDutyName = "";
        CurrentMainStat = null;
        RecommendedTier = null;
        RecommendedItemId = null;
        SwapMessage = "";
        HasSwapped = false;

        // Check if it's a hardcoded Ultimate first
        if (DutyData.Ultimates.TryGetValue(territoryId, out var ultimateInfo))
        {
            CurrentUltimate = ultimateInfo;
            CurrentDutyType = DetectedDutyType.Ultimate;
            CurrentDutyName = ultimateInfo.Name;
            log.Information($"Entered Ultimate: {ultimateInfo.Name} (Territory {territoryId})");
        }
        else
        {
            // Try to detect savage or normal raid via game data
            var detectedType = DetectDutyType(territoryId, out var dutyName);
            if (detectedType == DetectedDutyType.None)
            {
                StatusMessage = "Not in a tracked duty.";
                log.Information($"Territory {territoryId} is not a tracked duty.");
                return;
            }

            CurrentDutyType = detectedType;
            CurrentDutyName = dutyName;
            log.Information($"Entered {detectedType}: {dutyName} (Territory {territoryId})");
        }

        // Determine player's main stat
        if (!playerState.IsLoaded || !playerState.ClassJob.IsValid)
        {
            StatusMessage = "Player data not loaded yet.";
            return;
        }

        var jobId = playerState.ClassJob.RowId;
        if (!JobData.JobMainStat.TryGetValue(jobId, out var mainStat))
        {
            StatusMessage = $"Unknown job ID: {jobId}";
            log.Warning($"No main stat mapping for job ID {jobId}");
            return;
        }

        CurrentMainStat = mainStat;
        log.Information($"Job ID {jobId} -> {mainStat}");

        // Find best potion based on duty type
        if (CurrentDutyType == DetectedDutyType.Ultimate && CurrentUltimate != null)
            FindBestPotionForUltimate(CurrentUltimate, mainStat);
        else
            FindHighestPotion(mainStat);

        if (configuration.AutoSwapOnDutyEnter && RecommendedItemId.HasValue)
            SwapHotbarPotion();
    }

    /// <summary>
    /// Detect if the current territory is a savage or normal raid using Lumina sheets.
    /// ContentType 5 = Raids. Savage raids have "(Savage)" in their name.
    /// </summary>
    private DetectedDutyType DetectDutyType(uint territoryId, out string dutyName)
    {
        dutyName = "";

        try
        {
            var territorySheet = dataManager.GetExcelSheet<TerritoryType>();
            if (!territorySheet.TryGetRow(territoryId, out var territory))
                return DetectedDutyType.None;

            var cfcId = territory.ContentFinderCondition.RowId;
            if (cfcId == 0)
                return DetectedDutyType.None;

            var cfcSheet = dataManager.GetExcelSheet<ContentFinderCondition>();
            if (!cfcSheet.TryGetRow(cfcId, out var cfc))
                return DetectedDutyType.None;

            var contentTypeId = cfc.ContentType.RowId;
            var name = cfc.Name.ToString();
            dutyName = name;

            // ContentType 5 = Raids (8-man)
            if (contentTypeId == 5)
            {
                if (name.Contains("(Savage)"))
                    return DetectedDutyType.Savage;
                else
                    return DetectedDutyType.NormalRaid;
            }
        }
        catch (Exception ex)
        {
            log.Error($"Error detecting duty type for territory {territoryId}: {ex.Message}");
        }

        return DetectedDutyType.None;
    }

    /// <summary>
    /// For Ultimates: use cheapest that caps, fall back to lower tiers.
    /// </summary>
    private unsafe void FindBestPotionForUltimate(DutyInfo duty, MainStat stat)
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            StatusMessage = "Could not access inventory.";
            return;
        }

        var statName = PotionData.GetStatName(stat);

        // Search from optimal tier upward (cheapest first)
        var tiersUp = Enum.GetValues<PotionTier>()
            .Where(t => (int)t >= (int)duty.OptimalTier)
            .OrderBy(t => (int)t)
            .ToList();

        foreach (var tier in tiersUp)
        {
            if (!PotionData.PotionItemIds.TryGetValue((tier, stat), out var itemId))
                continue;

            if (HasItemInInventory(inventoryManager, itemId))
            {
                RecommendedTier = tier;
                RecommendedItemId = itemId;
                var tierName = PotionData.GetTierName(tier);

                StatusMessage = tier == duty.OptimalTier
                    ? $"{duty.Name}: Use {tierName} of {statName} (optimal - cheapest that caps!)"
                    : $"{duty.Name}: Use {tierName} of {statName} (caps, but cheaper option exists)";

                log.Information($"Found potion: {tierName} of {statName} (Item ID: {itemId})");
                return;
            }
        }

        // Fall back below optimal (won't cap)
        var tiersDown = Enum.GetValues<PotionTier>()
            .Where(t => (int)t < (int)duty.OptimalTier)
            .OrderByDescending(t => (int)t)
            .ToList();

        foreach (var tier in tiersDown)
        {
            if (!PotionData.PotionItemIds.TryGetValue((tier, stat), out var itemId))
                continue;

            if (HasItemInInventory(inventoryManager, itemId))
            {
                RecommendedTier = tier;
                RecommendedItemId = itemId;
                var tierName = PotionData.GetTierName(tier);
                StatusMessage = $"{duty.Name}: Using {tierName} of {statName} (below optimal - won't cap!)";
                log.Warning($"Using sub-optimal potion: {tierName} of {statName}");
                return;
            }
        }

        StatusMessage = $"{duty.Name}: No {statName} potions found in inventory!";
        log.Warning($"No potions found for {stat} in inventory.");
    }

    /// <summary>
    /// For Savage/Normal: use the highest available potion.
    /// </summary>
    private unsafe void FindHighestPotion(MainStat stat)
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
        {
            StatusMessage = "Could not access inventory.";
            return;
        }

        var statName = PotionData.GetStatName(stat);
        var typeLabel = CurrentDutyType == DetectedDutyType.Savage ? "Savage" : "Raid";

        var tiersDesc = Enum.GetValues<PotionTier>()
            .OrderByDescending(t => (int)t)
            .ToList();

        foreach (var tier in tiersDesc)
        {
            if (!PotionData.PotionItemIds.TryGetValue((tier, stat), out var itemId))
                continue;

            if (HasItemInInventory(inventoryManager, itemId))
            {
                RecommendedTier = tier;
                RecommendedItemId = itemId;
                var tierName = PotionData.GetTierName(tier);
                StatusMessage = $"{typeLabel}: Use {tierName} of {statName} (highest available)";
                log.Information($"Found best potion: {tierName} of {statName} (Item ID: {itemId})");
                return;
            }
        }

        StatusMessage = $"{typeLabel}: No {statName} potions found in inventory!";
        log.Warning($"No potions found for {stat} in inventory.");
    }

    private static unsafe bool HasItemInInventory(InventoryManager* inventoryManager, uint itemId)
    {
        var containers = new[]
        {
            InventoryType.Inventory1,
            InventoryType.Inventory2,
            InventoryType.Inventory3,
            InventoryType.Inventory4,
        };

        foreach (var containerType in containers)
        {
            var container = inventoryManager->GetInventoryContainer(containerType);
            if (container == null) continue;

            for (var i = 0; i < container->Size; i++)
            {
                var item = container->GetInventorySlot(i);
                if (item == null) continue;

                if (item->ItemId == itemId || item->ItemId == itemId + 1000000)
                    return true;
            }
        }

        return false;
    }

    public void SwapHotbarPotion()
    {
        if (!RecommendedItemId.HasValue)
        {
            SwapMessage = "No recommended potion to swap to.";
            return;
        }

        var targetItemId = RecommendedItemId.Value;

        framework.RunOnTick(() =>
        {
            unsafe
            {
                var hotbarModule = RaptureHotbarModule.Instance();
                if (hotbarModule == null)
                {
                    SwapMessage = "Could not access hotbar module.";
                    log.Error("RaptureHotbarModule instance is null.");
                    return;
                }

                if (IsPotionAlreadyOnHotbar(hotbarModule, targetItemId))
                {
                    SwapMessage = "Correct potion is already on hotbar!";
                    HasSwapped = true;
                    return;
                }

                var hqItemId = targetItemId + 1000000;

                // Scan all hotbars (0-9 normal, 10-17 cross)
                for (uint hotbarId = 0; hotbarId < 18; hotbarId++)
                {
                    ref var hotbar = ref hotbarModule->Hotbars[(int)hotbarId];

                    for (uint slotId = 0; slotId < hotbar.Slots.Length; slotId++)
                    {
                        ref var slot = ref hotbar.Slots[(int)slotId];

                        if (slot.CommandType == HotbarSlotType.Item && IsPotionItemId(slot.CommandId))
                        {
                            log.Information($"DEBUG BEFORE: hotbarId={hotbarId}, slotId={slotId}, " +
                                $"CommandType={slot.CommandType} ({(int)slot.CommandType}), " +
                                $"CommandId={slot.CommandId}");
                            log.Information($"DEBUG TARGET: HotbarSlotType.Item={(int)HotbarSlotType.Item}, " +
                                $"targetItemId={targetItemId}, hqItemId={hqItemId}");

                            // Try SetAndSaveSlot
                            hotbarModule->SetAndSaveSlot(hotbarId, slotId, HotbarSlotType.Item, hqItemId);

                            // Re-read slot to check if it changed
                            ref var slotAfter = ref hotbarModule->Hotbars[(int)hotbarId].Slots[(int)slotId];
                            log.Information($"DEBUG AFTER SetAndSaveSlot: " +
                                $"CommandType={slotAfter.CommandType} ({(int)slotAfter.CommandType}), " +
                                $"CommandId={slotAfter.CommandId}");

                            // If SetAndSaveSlot didn't change it, try Set() directly
                            if (slotAfter.CommandId != hqItemId)
                            {
                                log.Warning("SetAndSaveSlot did NOT change the slot! Trying Set() directly...");
                                slotAfter.Set(HotbarSlotType.Item, hqItemId);

                                ref var slotAfter2 = ref hotbarModule->Hotbars[(int)hotbarId].Slots[(int)slotId];
                                log.Information($"DEBUG AFTER Set(): " +
                                    $"CommandType={slotAfter2.CommandType} ({(int)slotAfter2.CommandType}), " +
                                    $"CommandId={slotAfter2.CommandId}");
                            }

                            var label = hotbarId < 10
                                ? $"hotbar {hotbarId + 1} slot {slotId + 1}"
                                : $"cross hotbar {hotbarId - 9} slot {slotId + 1}";

                            SwapMessage = $"Swapped {label}!";
                            HasSwapped = true;
                            log.Information($"Swapped {label} -> item {targetItemId} (HQ: {hqItemId})");

                            // Print to game chat
                            var tierName = RecommendedTier.HasValue ? PotionData.GetTierName(RecommendedTier.Value) : "Unknown";
                            var statName = CurrentMainStat.HasValue ? PotionData.GetStatName(CurrentMainStat.Value) : "Unknown";
                            chatGui.Print($"[AutoPot] Swapped to {tierName} of {statName} on {label}.");
                            return;
                        }
                    }
                }

                SwapMessage = "No potion found on any hotbar to replace.";
                log.Warning("No existing potion slot found on any hotbar.");
            }
        });
    }

    private bool IsPotionItemId(uint commandId) => allPotionItemIds.Contains(commandId);

    private unsafe bool IsPotionAlreadyOnHotbar(RaptureHotbarModule* hotbarModule, uint targetItemId)
    {
        for (uint hotbarId = 0; hotbarId < 18; hotbarId++)
        {
            var hotbar = hotbarModule->Hotbars[(int)hotbarId];

            for (uint slotId = 0; slotId < hotbar.Slots.Length; slotId++)
            {
                ref var slot = ref hotbar.Slots[(int)slotId];

                if (slot.CommandType == HotbarSlotType.Item &&
                    (slot.CommandId == targetItemId || slot.CommandId == targetItemId + 1000000))
                    return true;
            }
        }

        return false;
    }

    public void Refresh()
    {
        if (clientState.TerritoryType != 0)
            OnTerritoryChanged(clientState.TerritoryType);
    }

    public void SimulateDuty(uint territoryId)
    {
        log.Information($"[Test] Simulating territory {territoryId}");
        OnTerritoryChanged((ushort)territoryId);
    }

    public void ClearSimulation()
    {
        CurrentUltimate = null;
        CurrentDutyType = DetectedDutyType.None;
        CurrentDutyName = "";
        CurrentMainStat = null;
        RecommendedTier = null;
        RecommendedItemId = null;
        StatusMessage = "Not in a tracked duty.";
        SwapMessage = "";
        HasSwapped = false;
    }

    public void Dispose()
    {
        clientState.TerritoryChanged -= OnTerritoryChanged;
    }
}