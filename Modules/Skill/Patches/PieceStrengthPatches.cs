using HarmonyLib;
using Mimir;
using UnityEngine;
using Valchitecture.Common.Configuration;
using Valchitecture.Common;

namespace Valchitecture.Modules.Skill.Patches;
/// <summary>Records the builder's skill on a placed piece and applies its health.</summary>
[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.OnPlaced))]
internal static class WearNTear_OnPlaced_Patch
{
    private static void Postfix(WearNTear __instance)
    {
        ZDO zdo = __instance && __instance.m_nview ? __instance.m_nview.GetZDO() : null;
        float factor = BuildingSkill.Factor;

        if (!PieceStrength.Active || zdo == null || factor <= 0f)
        {
            return;
        }

        zdo.Set(PieceStrength.SkillKey, factor);
        PieceStrength.ApplyStored(__instance);

        if (Log.Shows(Log.InfoLevel.High))
        {
            Log.LogInfo(
                $"Placed {__instance.name}: skill {factor * 100f:0} -> health {__instance.m_health:0.#}, "
                + $"support loss x{PieceStrength.LossScale(factor):0.###}",
                Log.InfoLevel.High);
        }
    }
}

/// <summary>Applies stored health before anything compares against the maximum.</summary>
[HarmonyPatch(typeof(WearNTear), "Awake")]
internal static class WearNTear_Awake_Patch
{
    private static void Prefix(WearNTear __instance)
    {
        PieceStrength.ApplyStored(__instance, awakePending: true);
    }
}

/// <summary>Raises the piece to the repairer's skill, if higher, before the repair.</summary>
[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Repair))]
internal static class WearNTear_Repair_Upgrade_Patch
{
    private static void Prefix(WearNTear __instance)
    {
        PieceStrength.Upgrade(__instance);
    }
}

/// <summary>Reapplies stored health on the health broadcast that follows an upgrade.</summary>
[HarmonyPatch(typeof(WearNTear), "RPC_HealthChanged")]
internal static class WearNTear_RPC_HealthChanged_Patch
{
    private static void Prefix(WearNTear __instance)
    {
        PieceStrength.ApplyStored(__instance);
    }
}

/// <summary>Scales per-joint support loss by the stored skill, or the builder's current skill for a ghost.</summary>
[HarmonyPatch(typeof(WearNTear), "GetMaterialProperties")]
internal static class WearNTear_GetMaterialProperties_Patch
{
    private static void Postfix(WearNTear __instance, ref float horizontalLoss, ref float verticalLoss)
    {
        if (!PieceStrength.Active)
        {
            return;
        }

        ZDO zdo = __instance && __instance.m_nview ? __instance.m_nview.GetZDO() : null;
        float factor = zdo?.GetFloat(PieceStrength.SkillKey, 0f) ?? BuildingSkill.Factor;

        if (factor <= 0f)
        {
            return;
        }

        float loss = PieceStrength.LossScale(factor);
        horizontalLoss *= loss;
        verticalLoss *= loss;
    }
}
