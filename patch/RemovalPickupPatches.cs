using System;
using Chestworks;
using HarmonyLib;
using UnityEngine;
using Valchitecture.Modules.Storage;

namespace Valchitecture.Patch;
/// <summary>Marks the local player's hammer removal.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.RemovePiece))]
internal static class Player_RemovePiece_Pickup_Patch
{
    private static void Prefix(Player __instance)
    {
        RemovalPickup.Removing = RemovalPickup.Active(__instance);
    }

    private static Exception Finalizer(Exception __exception)
    {
        RemovalPickup.Removing = false;
        return __exception;
    }
}

/// <summary>Takes ownership of the piece being removed, except pieces holding a container.</summary>
[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Remove))]
internal static class WearNTear_Remove_Pickup_Patch
{
    private static void Prefix(WearNTear __instance)
    {
        ZNetView view = __instance.m_nview;

        if (RemovalPickup.Removing && view && view.IsValid() && !view.IsOwner()
            && !__instance.GetComponentInChildren<Container>())
        {
            view.ClaimOwnership();
        }
    }
}

/// <summary>Distributes a removed piece's resources instead of dropping them, unless another mod cancels the drop.</summary>
[HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
[HarmonyPriority(Priority.Last)]
internal static class Piece_DropResources_Pickup_Patch
{
    private static bool Prefix(Piece __instance, HitData hitData, bool __runOriginal)
    {
        if (!__runOriginal)
        {
            return false;
        }

        if (!RemovalPickup.Removing || !__instance.m_nview || !__instance.m_nview.IsOwner())
        {
            return true;
        }

        return !RemovalPickup.Distribute(__instance, hitData);
    }
}
