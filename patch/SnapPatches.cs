using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Valchitecture.Common;
using Valchitecture.Core;
using Valchitecture.Modules.Snapping;
using Valchitecture.Modules.Snapping.Extra;

namespace Valchitecture.Patch;
/// <summary>Adds the extra snap points once the world's prefabs exist.</summary>
[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
[HarmonyPriority(Priority.Last)]
internal static class ZoneSystem_Start_Patch
{
    private static void Postfix()
    {
        if (ZNet.instance && ZNet.instance.IsDedicated())
        {
            return;
        }

        SnapPointBuilder.Rebuild("Adding snap points.", true);
    }
}

/// <summary>In a mode, holds alternative placement only while positioning the ghost with snapping Off, and routes the snap stepping keys.</summary>
[HarmonyPatch]
internal static class ZInput_AltPlace_Patch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (MethodInfo method in typeof(ZInput).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.Name is not ("GetButton" or "GetButtonDown" or "GetButtonUp"))
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();

            if (parameters.Length >= 1 && parameters[0].ParameterType == typeof(string))
            {
                yield return method;
            }
        }
    }

    private static void Postfix(string name, ref bool __result, MethodBase __originalMethod)
    {
        if (!ModeManager.Active)
        {
            return;
        }

        switch (name)
        {
            case "TabLeft":
            case "TabRight":
            case "JoyPrevSnap":
            case "JoyNextSnap":
                // The keyboard stepping keys are also the build menu's category tabs.
                if (__result && !Hud.IsPieceSelectionVisible())
                {
                    CycleButton(name, ref __result, __originalMethod);
                }

                return;
            case "AltPlace":
            case "JoyAltPlace":
                __result = __originalMethod.Name == "GetButton" && PlacementWindow.GhostActive && SnapController.SnappingDisabled;
                return;
            case "BuildMenu":
            case "JoyBuildMenu":
                if (Manipulator.NudgeActive)
                {
                    __result = false;
                }

                return;
        }
    }

    /// <summary>Routes a pressed stepping key: to the target point with the modifier held, else withheld under the pinned modes.</summary>
    private static void CycleButton(string name, ref bool result, MethodBase original)
    {
        if (!ModeManager.GhostDrivesPlacement || Manipulator.NudgeActive)
        {
            result = false;
            return;
        }

        // The game steps keyboard on press and gamepad on release.
        bool stepped = name is "TabLeft" or "TabRight"
            ? original.Name == "GetButtonDown"
            : original.Name == "GetButtonUp";

        if (SnapController.TargetModifierHeld)
        {
            if (stepped)
            {
                SnapController.StepTarget(name);
            }

            result = false;
            return;
        }

        if (SnapController.SuppressCycleButton(name))
        {
            result = false;
        }
    }
}

/// <summary>Withholds gamepad snap stepping by reporting a stale hold timer.</summary>
[HarmonyPatch]
internal static class ZInput_SnapTimer_Patch
{
    private const float StaleHold = 10f;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (MethodInfo method in typeof(ZInput).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.ReturnType != typeof(float) || !method.Name.StartsWith("GetButton"))
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();

            if (parameters.Length >= 1 && parameters[0].ParameterType == typeof(string))
            {
                yield return method;
            }
        }
    }

    private static void Postfix(string name, ref float __result)
    {
        if (!ModeManager.Active)
        {
            return;
        }

        if (SnapController.IsCycleButton(name) && (SnapController.SuppressCycleTimer(name) || SnapController.TargetModifierHeld))
        {
            __result = StaleHold;
        }
    }
}

/// <summary>Replaces the game's snap pair with the Manual pair, or in Grid aligns an unpaired ghost to the grid.</summary>
[HarmonyPatch]
internal static class Player_FindClosestSnapPoints_Patch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.DeclaredMethod(typeof(Player), "FindClosestSnapPoints");
    }

    private static void Postfix(Player __instance, Transform ghost, ref Transform a, ref Transform b, ref bool __result)
    {
        if (SnapController.ManualInCharge && SnapController.ResolvePair(__instance, ghost, ref a, ref b))
        {
            __result = true;
        }
        else if (!__result)
        {
            GridPlacement.Align(__instance, ghost);
        }
    }
}

/// <summary>
///     Hides added snap points from the game's reads outside a mode, on bare pieces under Auto,
///     and while the feature is off; Manual sees them all.
/// </summary>
[HarmonyPatch(typeof(Piece), nameof(Piece.GetSnapPoints), typeof(List<Transform>))]
internal static class Piece_GetSnapPoints_Patch
{
    // The list may already hold other pieces' points.
    private static void Prefix(List<Transform> points, out int __state)
    {
        __state = points.Count;
    }

    private static void Postfix(Piece __instance, List<Transform> points, int __state)
    {
        if (points.Count == __state)
        {
            return;
        }

        // Instances made before a later pass may keep an older layout.
        bool firstPass = AddedSnapPoints.Passes <= 1;

        if (firstPass && AddedSnapPoints.Count == 0)
        {
            return;
        }

        if (Cfg.Enabled.Value && SnapConfig.EnableExtraSnapPoints.Value)
        {
            if (SnapController.ManualInCharge || (ModeManager.Active && !SnapPointBuilder.ShippedBare(__instance)))
            {
                return;
            }
        }

        bool[] mask = AddedSnapPoints.MaskOf(PrefabKey.Of(__instance));

        if (mask == null)
        {
            if (!firstPass)
            {
                Scan(points, __state);
            }

            return;
        }

        if (mask.Length != points.Count - __state || (!firstPass && !Flagged(points, __state, mask)))
        {
            Scan(points, __state);
            return;
        }

        for (int i = mask.Length - 1; i >= 0; i--)
        {
            if (mask[i])
            {
                points.RemoveAt(__state + i);
            }
        }
    }

    /// <summary>Whether every flagged point of the instance is one this mod added.</summary>
    private static bool Flagged(List<Transform> points, int first, bool[] mask)
    {
        for (int i = 0; i < mask.Length; i++)
        {
            if (mask[i] && !(points[first + i] && points[first + i].TryGetComponent<AddedSnapPoint>(out _)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Removes the added points among those appended, found by component.</summary>
    private static void Scan(List<Transform> points, int first)
    {
        // Added points are inactive.
        for (int i = points.Count - 1; i >= first; i--)
        {
            if (points[i] && !points[i].gameObject.activeSelf && points[i].TryGetComponent<AddedSnapPoint>(out _))
            {
                points.RemoveAt(i);
            }
        }
    }
}

/// <summary>Shows unnamed snap points ("_snappoint") as numbered points in the game's stepping message.</summary>
[HarmonyPatch(typeof(MessageHud), nameof(MessageHud.ShowMessage))]
internal static class MessageHud_ShowMessage_Patch
{
    private const string Snapping = "$msg_snapping ";

    private static void Prefix(ref string text)
    {
        if (!Cfg.Enabled.Value || text == null || !text.StartsWith(Snapping)
            || string.CompareOrdinal(text, Snapping.Length, SnapPointNames.DEFAULT_NAME, 0, SnapPointNames.DEFAULT_NAME.Length) != 0)
        {
            return;
        }

        Player player = Player.m_localPlayer;
        int index = player ? player.m_manualSnapPoint : -1;
        text = Snapping + (index >= 0
            ? Names.Localize(Tokens.Point, (index + 1).ToString())
            : Names.Localize(Tokens.PointUnnumbered));
    }
}
