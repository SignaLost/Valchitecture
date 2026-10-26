using HarmonyLib;
using Valchitecture.Core;

namespace Valchitecture.Patch;
/// <summary>Suppresses the game's station marker in a mode once station hopping is unlocked.</summary>
[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.ShowAreaMarker))]
internal static class CraftingStation_ShowAreaMarker_Patch
{
    private static bool Prefix()
    {
        return !ModeManager.Active || !Unlocks.CanHopStations() || BuildAnchor.ShowingOwnRing;
    }
}
