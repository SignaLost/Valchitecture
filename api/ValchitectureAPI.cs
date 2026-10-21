using UnityEngine;
using Valchitecture.Common;
using Valchitecture.Core;
using Valchitecture.Modules;
using Valchitecture.Modules.Snapping;

namespace Valchitecture;

/// <summary>
///     Read access to Valchitecture's state for other mods, for the local player, from the main thread.
///     Values are safe to read before a world loads and return their "off" state.
/// </summary>
public static class ValchitectureAPI
{
    public const int Version = 1;

    public const string Guid = ValchitecturePlugin.PluginGuid;

    /// <summary>The mod's Enabled setting.</summary>
    public static bool Enabled => Cfg.Enabled != null && Cfg.Enabled.Value;

    /// <summary>The Building skill's id in Mimir; its console name is <c>building</c>.</summary>
    public const string SkillId = BuildingSkill.Id;

    public static bool InBuildMode => ModeManager.IsBuild;

    public static bool InEditMode => ModeManager.IsEdit;

    /// <summary>The build camera is away from the character.</summary>
    public static bool CameraDetached => ModeManager.CameraDetached;

    /// <summary>"Auto", "Manual", "Grid" or "Off".</summary>
    public static string CurrentSnapMode => SnapController.Mode.ToString();

    /// <summary>The Building level a piece was built or last upgraded at, 0 to 100; 0 for none.</summary>
    public static float BuiltAtLevel(WearNTear piece)
    {
        return piece ? Mathf.Round(PieceStrength.StoredFactor(piece) * 100f) : 0f;
    }

    /// <summary>
    ///     Whether another player is using the piece: an open container, a pulled cart, a seat or a bed.
    ///     This is what Guard Used refuses to remove.
    /// </summary>
    public static bool IsInUseByOther(Piece piece)
    {
        return piece && InUseGuard.UsedByOther(piece);
    }
}
