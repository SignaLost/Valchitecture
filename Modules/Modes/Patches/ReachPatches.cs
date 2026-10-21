using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Valchitecture.Modules.Skill;
using Valchitecture.Modules.Snapping;

namespace Valchitecture.Modules.Modes.Patches;
/// <summary>At level 100 with the camera detached, also counts a station within its margin of the camera.</summary>
[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.HaveBuildStationInRange))]
internal static class CraftingStation_HaveBuildStationInRange_Patch
{
    // The original overwrites point.y while searching.
    private static void Prefix(Vector3 point, out bool __state)
    {
        Player player = Player.m_localPlayer;

        __state = ModeManager.CameraDetached && Unlocks.CanBuildFromCamera() && player && GameCamera.instance
                  && point == player.transform.position;
    }

    private static void Postfix(string name, bool __state, ref CraftingStation __result)
    {
        if (__state && !__result)
        {
            __result = BuildAnchor.Closest(GameCamera.instance.transform.position, BuildAnchor.Margin, name);
        }
    }
}

/// <summary>Fails the station requirement where <see cref="Reach.BenchHolds" /> refuses the ghost's position.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
internal static class Player_HaveRequirements_Bench_Patch
{
    private static void Postfix(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result)
    {
        GameObject ghost = __instance.m_placementGhost;

        // The build menu asks for every piece.
        if (__result && mode == Player.RequirementMode.CanBuild && ModeManager.CameraDetached && Unlocks.CanBuildFromCamera()
            && __instance == Player.m_localPlayer && piece == __instance.GetSelectedPiece() && ghost && ghost.activeSelf
            && !Reach.BenchHolds(__instance, piece, ghost.transform.position))
        {
            __result = false;
        }
    }
}

/// <summary>Fails the removal station check where <see cref="Reach.BenchHolds" /> refuses the piece.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.CheckCanRemovePiece))]
internal static class Player_CheckCanRemovePiece_Bench_Patch
{
    private static void Postfix(Player __instance, Piece piece, ref bool __result)
    {
        if (__result && ModeManager.CameraDetached && Unlocks.CanBuildFromCamera() && __instance == Player.m_localPlayer && piece
            && !Reach.BenchHolds(__instance, piece, piece.transform.position))
        {
            __instance.Message(MessageHud.MessageType.Center, "$msg_missingstation");
            __result = false;
        }
    }
}

/// <summary>Refuses placement points the widened reach allows but <see cref="Reach.Allows" /> does not.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.PieceRayTest))]
internal static class Player_PieceRayTest_Patch
{
    private static void Postfix(Player __instance, ref bool __result, ref Vector3 point)
    {
        if (!__result || !Reach.BuildFollowsCamera() || (Manipulator.GhostFrozen && !Manipulator.HeightLocked && PlacementWindow.GhostActive)
            || __instance != Player.m_localPlayer)
        {
            return;
        }

        Piece ghost = __instance.m_placementGhost ? GhostInfo.PieceOf(__instance.m_placementGhost) : null;

        if (!Reach.Allows(__instance, point, ghost ? ghost.m_extraPlacementDistance : 0f))
        {
            __result = false;
        }
    }
}

/// <summary>Applies <see cref="Reach.AimAllowed" /> to removing and copying.</summary>
[HarmonyPatch]
internal static class Player_RemoveOrCopy_Range_Patch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.DeclaredMethod(typeof(Player), nameof(Player.RemovePiece));
        yield return AccessTools.DeclaredMethod(typeof(Player), nameof(Player.CopyPiece));
    }

    private static bool Prefix(Player __instance, ref bool __result)
    {
        if (Reach.AimAllowed(__instance))
        {
            return true;
        }

        __result = false;
        return false;
    }
}

/// <summary>Applies <see cref="Reach.AimAllowed" /> to hovering, skipping the original when refused.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.UpdateWearNTearHover))]
internal static class Player_UpdateWearNTearHover_Range_Patch
{
    private static bool Prefix(Player __instance)
    {
        if (__instance != Player.m_localPlayer || Reach.AimAllowed(__instance))
        {
            return true;
        }

        __instance.m_hoveringPiece = null;
        return false;
    }
}
