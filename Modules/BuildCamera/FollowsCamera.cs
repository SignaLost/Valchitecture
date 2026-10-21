using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Valchitecture.Modules.Modes;
using Valchitecture.Common;

namespace Valchitecture.Modules.BuildCamera;
/// <summary>Makes foliage and following effects track the camera by replacing InFreeFly calls.</summary>
internal static class FollowsCamera
{
    internal static bool Instead()
    {
        return GameCamera.InFreeFly() || (ModeManager.CameraDetached && GameCamera.instance);
    }

    /// <summary>Replaces every InFreeFly call in a method with <see cref="Instead" />.</summary>
    internal static IEnumerable<CodeInstruction> Swap(IEnumerable<CodeInstruction> instructions, string where)
    {
        MethodInfo asked = AccessTools.Method(typeof(GameCamera), nameof(GameCamera.InFreeFly));
        MethodInfo answer = AccessTools.Method(typeof(FollowsCamera), nameof(Instead));

        int swapped = 0;
        List<CodeInstruction> code = new(instructions);

        foreach (CodeInstruction instruction in code)
        {
            if (instruction.Calls(asked))
            {
                instruction.operand = answer;
                swapped++;
            }
        }

        if (swapped == 0)
        {
            Log.LogError($"{where}: no InFreeFly call found; it will keep following the character.");
        }

        return code;
    }
}
