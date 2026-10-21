using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Chestworks;
using Chests = Chestworks.API;
using HarmonyLib;
using Valchitecture.Common;

namespace Valchitecture.Modules.Storage.Patches;
/// <summary>Makes piece requirement checks count container stock.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
internal static class Player_HaveRequirements_Storage_Patch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        return StockReads.Redirect(instructions, original,
            (StockReads.CountItems, StockReads.Hook(nameof(StockReads.CountWithStock))),
            (StockReads.HaveItem, StockReads.Hook(nameof(StockReads.HaveWithStock))));
    }
}

/// <summary>Marks while the build HUD's requirement list is drawn.</summary>
[HarmonyPatch(typeof(Hud), "SetupPieceInfo")]
internal static class Hud_SetupPieceInfo_Storage_Patch
{
    private static void Prefix()
    {
        StockReads.InPieceInfo = true;
    }

    private static Exception Finalizer(Exception __exception)
    {
        StockReads.InPieceInfo = false;
        return __exception;
    }
}

/// <summary>Makes the build HUD's requirement row count container stock.</summary>
[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
internal static class InventoryGui_SetupRequirement_Storage_Patch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        return StockReads.Redirect(instructions, original,
            (StockReads.CountItems, StockReads.Hook(nameof(StockReads.CountForPieceInfo))));
    }
}

/// <summary>Takes a placed piece's shortfall from containers before the game takes the rest from the inventory.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
[HarmonyPriority(Priority.Last)]
internal static class Player_ConsumeResources_Storage_Patch
{
    private static void Prefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel, int itemQuality, int multiplier, bool __runOriginal)
    {
        // Crafting passes recipe requirements, not the selected piece's.
        Piece selected = __instance.GetSelectedPiece();

        if (!__runOriginal || requirements == null || !selected || selected.m_resources != requirements
            || __instance != Player.m_localPlayer || __instance.PlacementCostDisabled || !Chests.IsActive(Purposes.Building))
        {
            return;
        }

        CraftingStation station = __instance.GetCurrentCraftingStation();
        Inventory inventory = __instance.GetInventory();

        foreach (Piece.Requirement requirement in requirements)
        {
            // The game's own filter in ConsumeResources.
            bool counted = station ? station.m_upgrader == requirement.m_upgraderResource : !requirement.m_upgraderResource;

            if (!counted || !requirement.m_resItem)
            {
                continue;
            }

            int needed = requirement.GetAmount(qualityLevel) * multiplier;
            string name = requirement.m_resItem.m_itemData.m_shared.m_name;
            int shortfall = needed - inventory.CountItems(name, itemQuality);

            if (shortfall <= 0)
            {
                continue;
            }

            int taken = Chests.Take(Purposes.Building, name, shortfall, itemQuality);

            if (taken < shortfall)
            {
                Log.LogWarning($"Containers held {taken} of the {shortfall} {name} counted for {selected.name}.");
            }
            else if (Log.Shows(Log.InfoLevel.High))
            {
                Log.LogInfo($"Took {taken} {name} from containers for {selected.name}.", Log.InfoLevel.High);
            }
        }
    }
}

/// <summary>Marks a placed piece cheated when cheated materials in the containers could pay for it.</summary>
[HarmonyPatch(typeof(Inventory), nameof(Inventory.ItemCheated), typeof(Piece.Requirement[]), typeof(int), typeof(bool))]
internal static class Inventory_ItemCheated_Storage_Patch
{
    private static void Postfix(Inventory __instance, Piece.Requirement[] resources, int quality, bool matchWorldLevel, ref bool __result)
    {
        Player player = Player.m_localPlayer;

        if (__result || !player || __instance != player.m_inventory || !Chests.IsActive(Purposes.Building))
        {
            return;
        }

        // Crafting passes recipe requirements, not the selected piece's.
        Piece selected = player.GetSelectedPiece();

        if (selected && selected.m_resources == resources)
        {
            __result = Chests.HasCheated(Purposes.Building, resources, quality, matchWorldLevel);
        }
    }
}
