using HarmonyLib;
using Mimir;
using UnityEngine;
using Valchitecture.Common.Configuration;

namespace Valchitecture.Modules.Skill.Patches;
/// <summary>Clears the debt for each new local character.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
internal static class Player_SetLocalPlayer_Debt_Patch
{
    private static void Postfix()
    {
        Experience.Debt = 0;
    }
}
