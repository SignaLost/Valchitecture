using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Valchitecture.Modules.UI.Patches;
/// <summary>Builds the hint rows on the game's hint setup, in Awake and Start.</summary>
[HarmonyPatch]
internal static class KeyHints_Init_Patch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        MethodBase awake = AccessTools.DeclaredMethod(typeof(KeyHints), "Awake");

        if (awake != null)
        {
            yield return awake;
        }

        MethodBase start = AccessTools.DeclaredMethod(typeof(KeyHints), "Start");

        if (start != null)
        {
            yield return start;
        }
    }

    private static void Postfix(KeyHints __instance)
    {
        KeyHintController.Build(__instance, retry: true);
    }
}

/// <summary>Updates this mod's hints after the game's per-frame hint update.</summary>
[HarmonyPatch(typeof(KeyHints), nameof(KeyHints.UpdateHints))]
internal static class KeyHints_UpdateHints_Patch
{
    private static void Postfix(KeyHints __instance)
    {
        KeyHintController.UpdateHints(__instance);
    }
}
