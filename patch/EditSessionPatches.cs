using HarmonyLib;
using Valchitecture.Modules;

namespace Valchitecture.Patch;
/// <summary>Pauses local support updates while a piece is held.</summary>
[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.UpdateSupport))]
internal static class WearNTear_UpdateSupport_Hold_Patch
{
    private static bool Prefix()
    {
        return !EditSession.IsGhostDriven;
    }
}
