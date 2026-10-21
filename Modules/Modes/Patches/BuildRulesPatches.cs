using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Valchitecture.Common.Configuration;
using Valchitecture.Common;

namespace Valchitecture.Modules.Modes.Patches;
/// <summary>Redirects support, biome and clipping reads in placement and the support calculation.</summary>
[HarmonyPatch]
internal static class BuildRules_Placement_Patch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Player), "UpdatePlacementGhost");
        yield return AccessTools.Method(typeof(WearNTear), "UpdateSupport");
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        return RuleReads.Redirect(instructions, original);
    }
}

/// <summary>Redirects extension distance reads.</summary>
[HarmonyPatch]
internal static class BuildRules_Extension_Patch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(StationExtension), nameof(StationExtension.FindExtensions));
        yield return AccessTools.Method(typeof(StationExtension), nameof(StationExtension.FindStationsInRange));
        yield return AccessTools.Method(typeof(StationExtension), nameof(StationExtension.FindClosestStationInRange));
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        return RuleReads.Redirect(instructions, original);
    }
}

/// <summary>Applies Ignore No-Build Zones.</summary>
[HarmonyPatch(typeof(Location), nameof(Location.IsInsideNoBuildLocation))]
[HarmonyPriority(Priority.Last)]
internal static class Location_IsInsideNoBuildLocation_Patch
{
    private static void Postfix(ref bool __result)
    {
        if (BuildRules.IgnoreNoBuildZones)
        {
            __result = false;
        }
    }
}

/// <summary>Scales station ranges after the game's 2 s refresh.</summary>
[HarmonyPatch(typeof(CraftingStation), "GetExtensions")]
[HarmonyPriority(Priority.Last)]
internal static class CraftingStation_GetExtensions_Patch
{
    private static void Postfix(CraftingStation __instance)
    {
        // The timer is 0 right after a refresh, and always on ghosts.
        if (__instance.m_updateExtensionTimer == 0f)
        {
            BuildRules.ScaleStation(__instance, ScaledStation.Of(__instance));
        }
    }
}
