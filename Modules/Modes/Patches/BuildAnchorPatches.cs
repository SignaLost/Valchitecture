using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Valchitecture.Modules.Skill;

namespace Valchitecture.Modules.Modes.Patches;
/// <summary>Suppresses the game's station marker from level 75 in a mode.</summary>
[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.ShowAreaMarker))]
internal static class CraftingStation_ShowAreaMarker_Patch
{
    private static bool Prefix()
    {
        return !ModeManager.Active || !Unlocks.CanHopStations() || BuildAnchor.ShowingOwnRing;
    }
}
