using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Valchitecture.Common;
using Valchitecture.Core;
using Valchitecture.Modules.BuildCamera;

namespace Valchitecture.Patch;
/// <summary>Replaces the game's camera update while the build camera is detached.</summary>
[HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
[HarmonyPriority(Priority.VeryHigh)]
[HarmonyBefore("Azumatt.FirstPersonMode")]
internal static class GameCamera_UpdateCamera_Patch
{
    private static void Prefix(float dt, GameCamera __instance, ref bool __runOriginal)
    {
        if (ModeManager.CameraDetached)
        {
            BuildCam.UpdateCamera(dt, __instance);
            __runOriginal = false;
        }
    }
}

/// <summary>Loads and syncs the world around the camera instead of the character.</summary>
[HarmonyPatch(typeof(ZNet), nameof(ZNet.SetReferencePosition))]
internal static class ZNet_SetReferencePosition_Patch
{
    private static void Prefix(ref Vector3 pos)
    {
        if (ModeManager.CameraDetached && GameCamera.instance)
        {
            pos = GameCamera.instance.transform.position;
        }
    }
}

/// <summary>Exits the mode when the local character is destroyed; logs an error if it unloaded during play with the camera detached.</summary>
[HarmonyPatch(typeof(Player), "OnDestroy")]
internal static class Player_OnDestroy_Patch
{
    private static void Prefix(Player __instance)
    {
        if (__instance != Player.m_localPlayer || !ModeManager.Active)
        {
            return;
        }

        bool shuttingDown = Game.instance && Game.instance.IsShuttingDown();

        if (ModeManager.CameraDetached && !shuttingDown && !__instance.IsDead())
        {
            Vector3 reference = ZNet.instance ? ZNet.instance.GetReferencePosition() : Vector3.zero;
            float apart = Vector3.Distance(__instance.transform.position, reference);

            Log.LogError($"The character was unloaded while a build mode was active, {apart:F0}m from the world's reference position.");
        }

        ModeManager.Exit();
    }
}

/// <summary>Moves the audio listener to the camera.</summary>
[HarmonyPatch(typeof(GameCamera), "UpdateListner")]
internal static class GameCamera_UpdateListner_Patch
{
    private static void Postfix(GameCamera __instance)
    {
        if (ModeManager.CameraDetached && __instance.m_listner)
        {
            __instance.m_listner.transform.localPosition = Vector3.zero;
        }
    }
}

[HarmonyPatch(typeof(ClutterSystem), "LateUpdate")]
internal static class ClutterSystem_LateUpdate_Patch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return FollowsCamera.Swap(instructions, "The foliage system");
    }
}

[HarmonyPatch(typeof(FollowPlayer), "LateUpdate")]
internal static class FollowPlayer_LateUpdate_Patch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return FollowsCamera.Swap(instructions, "The effect-following system");
    }
}

[HarmonyPatch(typeof(LightLod), "GetLightReferencePoint")]
internal static class LightLod_GetLightReferencePoint_Patch
{
    private static void Postfix(ref Vector3 __result)
    {
        if (ModeManager.CameraDetached && GameCamera.instance)
        {
            __result = GameCamera.instance.transform.position;
        }
    }
}

/// <summary>Moves the Wisplight's wisp to the camera.</summary>
[HarmonyPatch(typeof(SE_Demister), nameof(SE_Demister.UpdateStatusEffect))]
internal static class SE_Demister_FollowCamera_Patch
{
    private static void Postfix(SE_Demister __instance)
    {
        if (!ModeManager.CameraDetached || !GameCamera.instance || !Player.m_localPlayer || __instance.m_character != Player.m_localPlayer)
        {
            return;
        }

        GameObject ball = __instance.m_ballInstance;

        if (!ball)
        {
            return;
        }

        ball.transform.position = GameCamera.instance.transform.position + Vector3.up * __instance.m_offset.y;

        __instance.m_ballVel = Vector3.zero;
    }
}
