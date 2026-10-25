using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Valchitecture.Common;
using Valchitecture.Core;
using Valchitecture.Modules;
using Valchitecture.Modules.Snapping;

namespace Valchitecture.Patch;
/// <summary>Runs the mod's per-frame input from the local player's update.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.Update))]
internal static class Player_Update_Patch
{
    private static void Prefix(Player __instance)
    {
        if (!IsLocal(__instance))
        {
            return;
        }

        ModeManager.Validate(__instance);

        if (!ModeManager.Active)
        {
            return;
        }

        ModeManager.HandleCameraToggle();

        // Must run before the input gate.
        BuildAnchor.ShowRange(GameCamera.instance ? GameCamera.instance.transform.position : __instance.transform.position);

        if (InputGate.UiIsCapturingInput())
        {
            return;
        }

        Reach.Widen(__instance);
        SnapController.HandleInput(__instance);
        Manipulator.HandleInput(__instance);

        // Edit mode must handle the click before copy.
        bool consumedByEdit = EditSession.HandleInput(__instance);
        bool consumedByCopy = !consumedByEdit && Manipulator.HandleCopyPiece(__instance);

        Player_UpdatePlacement_Patch.SkipThisFrame = consumedByEdit || consumedByCopy;

        Manipulator.BlockRemoveIfClaimed(__instance);
    }

    private static void Postfix(Player __instance)
    {
        if (!IsLocal(__instance))
        {
            return;
        }

        Player_UpdatePlacement_Patch.SkipThisFrame = false;
        ModeManager.HandleToggleInput(__instance);
    }

    internal static bool IsLocal(Player player)
    {
        return player && player == Player.m_localPlayer;
    }
}

/// <summary>Withholds consumed clicks from the game's placement and blocks its scroll rotation in a mode.</summary>
[HarmonyPatch]
internal static class Player_UpdatePlacement_Patch
{
    internal static bool SkipThisFrame;

    private static MethodBase TargetMethod()
    {
        return AccessTools.DeclaredMethod(typeof(Player), "UpdatePlacement");
    }

    private static void Prefix(Player __instance, ref bool takeInput, out int __state)
    {
        __state = __instance.m_placeRotation;

        if (SkipThisFrame && ModeManager.Active && Player_Update_Patch.IsLocal(__instance))
        {
            takeInput = false;
        }
    }

    private static void Postfix(Player __instance, int __state)
    {
        if (ModeManager.Active && Player_Update_Patch.IsLocal(__instance))
        {
            __instance.m_placeRotation = __state;
        }
    }
}

/// <summary>Replaces the ghost's placement rotation with the gizmo's.</summary>
[HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
internal static class Player_UpdatePlacementGhost_Rotation_Patch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo euler = AccessTools.Method(typeof(Quaternion), nameof(Quaternion.Euler), new[] { typeof(float), typeof(float), typeof(float) });
        MethodInfo hook = AccessTools.Method(typeof(Player_UpdatePlacementGhost_Rotation_Patch), nameof(OverridePlaceRotation));
        FieldInfo placeRotation = AccessTools.Field(typeof(Player), nameof(Player.m_placeRotation));

        return new CodeMatcher(instructions)
            .MatchStartForward(new CodeMatch(OpCodes.Ldfld, placeRotation))
            .ThrowIfInvalid("Valchitecture: could not find Player.m_placeRotation in UpdatePlacementGhost.")
            .MatchStartForward(new CodeMatch(instruction => instruction.Calls(euler)))
            .ThrowIfInvalid("Valchitecture: could not find the place-rotation Quaternion.Euler call.")
            .Advance(1)
            .Insert(new CodeInstruction(OpCodes.Call, hook))
            .InstructionEnumeration();
    }

    private static Quaternion OverridePlaceRotation(Quaternion vanillaRotation)
    {
        if (!ModeManager.GhostDrivesPlacement)
        {
            return vanillaRotation;
        }

        Player player = Player.m_localPlayer;
        return player && !player.CanRotatePiece() ? vanillaRotation : Manipulator.PlaceRotation;
    }
}

/// <summary>Holds the ghost while nudging and marks when the game is positioning it.</summary>
[HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
internal static class Player_UpdatePlacementGhost_Position_Patch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(Player __instance)
    {
        if (!ModeManager.Active || !Player_Update_Patch.IsLocal(__instance))
        {
            return;
        }

        if (SnapController.PinSnapPointToAuto)
        {
            __instance.m_manualSnapPoint = -1;
        }

        PlacementWindow.GhostActive = true;
    }

    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if (!ModeManager.GhostDrivesPlacement || !Player_Update_Patch.IsLocal(__instance))
        {
            return;
        }

        GameObject ghost = __instance.m_placementGhost;

        if (!ghost)
        {
            return;
        }

        if (Manipulator.GhostFrozen)
        {
            Freeze(__instance, ghost);
        }
    }

    /// <summary>Moves the ghost to the frozen position and revalidates it there.</summary>
    private static void Freeze(Player player, GameObject ghost)
    {
        if (Manipulator.HeightLocked)
        {
            if (!ghost.activeSelf)
            {
                return;
            }

            Vector3 position = ghost.transform.position;
            position.y = Manipulator.FrozenPosition.y;
            ghost.transform.position = position;
        }
        else
        {
            ghost.transform.position = Manipulator.FrozenPosition;

            if (player.m_placementMarkerInstance)
            {
                player.m_placementMarkerInstance.SetActive(false);
            }
        }

        FrozenPlacement.Revalidate(player, ghost);
    }

    /// <summary>Closes the window after the postfix, and on exceptions.</summary>
    [HarmonyFinalizer]
    private static Exception Finalizer(Exception __exception)
    {
        PlacementWindow.GhostActive = false;
        return __exception;
    }
}

/// <summary>Makes the placement ray return the frozen position while frozen.</summary>
[HarmonyPatch(typeof(Player), "PieceRayTest")]
internal static class Player_PieceRayTest_Frozen_Patch
{
    private static bool Prefix(ref bool __result, out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface)
    {
        point = Vector3.zero;
        normal = Vector3.zero;
        piece = null;
        heightmap = null;
        waterSurface = null;

        if (!PlacementWindow.GhostActive || !Manipulator.GhostFrozen || Manipulator.HeightLocked)
        {
            return true;
        }

        point = Manipulator.FrozenPosition;
        normal = Vector3.up;
        heightmap = Heightmap.FindHeightmap(point);
        __result = true;
        return false;
    }
}

/// <summary>Updates the gizmo and the snap point marks after the game finalizes the ghost in LateUpdate.</summary>
[HarmonyPatch]
internal static class Player_LateUpdate_Gizmo_Patch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.DeclaredMethod(typeof(Player), "LateUpdate");
    }

    private static void Postfix(Player __instance)
    {
        if (!Player_Update_Patch.IsLocal(__instance))
        {
            return;
        }

        if (ModeManager.Active)
        {
            Manipulator.UpdateGizmoVisual(__instance);
            SnapPointDisplay.Draw(__instance);
        }
        else
        {
            SnapPointDisplay.Hide();
        }
    }
}

/// <summary>Blocks character movement and attacks while the camera is detached.</summary>
[HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeInput))]
internal static class PlayerController_TakeInput_Patch
{
    private static void Prefix(ref bool __result, ref bool __runOriginal)
    {
        if (ModeManager.CameraDetached)
        {
            __result = false;
            __runOriginal = false;
        }
    }
}

/// <summary>Blocks crouching in a mode.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
internal static class Player_SetControls_Crouch_Patch
{
    private static void Prefix(Player __instance, ref bool crouch)
    {
        if (ModeManager.Active && Player_Update_Patch.IsLocal(__instance))
        {
            crouch = false;
        }
    }
}

/// <summary>Exits the mode when a new local player spawns.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
internal static class Player_SetLocalPlayer_Patch
{
    private static void Postfix()
    {
        ModeManager.Exit();
    }
}

/// <summary>Blocks the game's placement while a held piece is moved.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
internal static class Player_TryPlacePiece_Guard
{
    private static void Prefix(ref bool __result, ref bool __runOriginal)
    {
        if (EditSession.IsGhostDriven)
        {
            __result = false;
            __runOriginal = false;
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
internal static class Player_PlacePiece_Guard
{
    private static void Prefix(ref bool __runOriginal)
    {
        if (EditSession.IsGhostDriven)
        {
            __runOriginal = false;
        }
    }
}

/// <summary>Ends nudging once a piece is actually placed.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
internal static class Player_TryPlacePiece_Nudge_Patch
{
    private static void Postfix(bool __result)
    {
        if (__result && ModeManager.GhostDrivesPlacement)
        {
            Manipulator.OnPiecePlaced();
        }
    }
}
