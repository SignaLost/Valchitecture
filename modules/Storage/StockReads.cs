using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Chestworks;

namespace Valchitecture.Modules.Storage;
/// <summary>
///     Inventory reads that also count the containers building pulls from, through Chestworks.
///     Patches swap the game's requirement reads for these hooks; only the local player's inventory gains container
///     stock, and only while Chestworks has the building purpose active.
/// </summary>
internal static class StockReads
{
    /// <summary>True while the build HUD lists a piece's requirements.</summary>
    internal static bool InPieceInfo;

    internal static int CountWithStock(Inventory inventory, string name, int quality, bool matchWorldLevel)
    {
        int own = inventory.CountItems(name, quality, matchWorldLevel);

        // Chestworks always matches world level.
        return Applies(inventory) && matchWorldLevel ? own + ChestworksAPI.Stock(Purposes.Building, name, quality) : own;
    }

    internal static bool HaveWithStock(Inventory inventory, string name, bool matchWorldLevel)
    {
        return inventory.HaveItem(name, matchWorldLevel)
               || (Applies(inventory) && matchWorldLevel && ChestworksAPI.Stock(Purposes.Building, name) > 0);
    }

    /// <summary><see cref="CountWithStock" /> for the shared requirement row, only while the build HUD draws it.</summary>
    internal static int CountForPieceInfo(Inventory inventory, string name, int quality, bool matchWorldLevel)
    {
        return InPieceInfo ? CountWithStock(inventory, name, quality, matchWorldLevel) : inventory.CountItems(name, quality, matchWorldLevel);
    }

    private static bool Applies(Inventory inventory)
    {
        Player player = Player.m_localPlayer;
        return player && inventory == player.m_inventory && ChestworksAPI.IsActive(Purposes.Building);
    }

    /// <summary>Replaces the named Inventory calls in a method with hooks that take the inventory first.</summary>
    internal static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions, MethodBase original, params (MethodInfo From, MethodInfo To)[] swaps)
    {
        List<CodeInstruction> codes = new(instructions);
        int replaced = 0;

        foreach (CodeInstruction code in codes)
        {
            foreach ((MethodInfo from, MethodInfo to) in swaps)
            {
                if (code.Calls(from))
                {
                    code.opcode = OpCodes.Call;
                    code.operand = to;
                    replaced++;
                    break;
                }
            }
        }

        if (replaced == 0)
        {
            throw new InvalidOperationException(
                $"Valchitecture: no inventory reads found in {original.DeclaringType?.Name}.{original.Name}.");
        }

        return codes;
    }

    internal static MethodInfo CountItems => AccessTools.Method(typeof(Inventory), nameof(Inventory.CountItems), new[] { typeof(string), typeof(int), typeof(bool) });

    internal static MethodInfo HaveItem => AccessTools.Method(typeof(Inventory), nameof(Inventory.HaveItem), new[] { typeof(string), typeof(bool) });

    internal static MethodInfo Hook(string name)
    {
        return AccessTools.Method(typeof(StockReads), name);
    }
}
