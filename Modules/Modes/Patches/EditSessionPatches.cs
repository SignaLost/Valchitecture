using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Valchitecture.Common.Configuration;
using Valchitecture.Modules.Skill;
using Valchitecture.Common;

namespace Valchitecture.Modules.Modes.Patches;
/// <summary>Pauses local support updates while a piece is held.</summary>
[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.UpdateSupport))]
internal static class WearNTear_UpdateSupport_Hold_Patch
{
    private static bool Prefix()
    {
        return !EditSession.IsGhostDriven;
    }
}
