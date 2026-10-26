using System;
using HarmonyLib;
using Valchitecture.Modules;

namespace Valchitecture.Patch;
/// <summary>Marks the local player's hammer removal, in or out of a mode.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.RemovePiece))]
internal static class Player_RemovePiece_InUse_Patch
{
    private static void Prefix(Player __instance)
    {
        InUseGuard.Removing = __instance == Player.m_localPlayer;
    }

    private static Exception Finalizer(Exception __exception)
    {
        InUseGuard.Removing = false;
        return __exception;
    }
}

/// <summary>Refuses removal of a used piece during hammer removal only; the game also reads this check to pause weather wear.</summary>
[HarmonyPatch(typeof(Piece), nameof(Piece.CanBeRemoved))]
internal static class Piece_CanBeRemoved_InUse_Patch
{
    private static void Postfix(Piece __instance, ref bool __result)
    {
        if (__result && InUseGuard.Removing && InUseGuard.Active && InUseGuard.UsedByOther(__instance))
        {
            __result = false;
        }
    }
}
