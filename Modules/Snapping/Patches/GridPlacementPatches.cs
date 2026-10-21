using HarmonyLib;
using UnityEngine;
using Valchitecture.Common.Configuration;
using Valchitecture.Modules.Modes;

namespace Valchitecture.Modules.Snapping.Patches;
/// <summary>Records whether the placement ray hit terrain, for <see cref="GridPlacement" />.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.PieceRayTest))]
internal static class Player_PieceRayTest_Grid_Patch
{
    private static void Postfix(Player __instance, bool __result, ref Heightmap heightmap)
    {
        if (PlacementWindow.GhostActive && __instance == Player.m_localPlayer)
        {
            GridPlacement.OnTerrain = __result && heightmap;
        }
    }
}
