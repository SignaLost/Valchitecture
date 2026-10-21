using UnityEngine;
using Valchitecture.Common.Configuration;
using Valchitecture.Modules.Modes;

namespace Valchitecture.Modules.Snapping;
/// <summary>Grid mode: moves an unsnapped ghost to the nearest world grid point on the ground plane.</summary>
internal static class GridPlacement
{
    /// <summary>Whether this frame's placement ray hit terrain.</summary>
    internal static bool OnTerrain;

    internal static void Align(Player player, Transform ghost)
    {
        if (SnapController.Mode != SnapMode.Grid || !PlacementWindow.GhostActive || !ModeManager.GhostDrivesPlacement
            || player != Player.m_localPlayer || !ghost || (Manipulator.GhostFrozen && !Manipulator.HeightLocked))
        {
            return;
        }

        float size = Cfg.GridSize.Value;
        Vector3 from = ghost.position;
        Vector3 to = new(Mathf.Round(from.x / size) * size, from.y, Mathf.Round(from.z / size) * size);

        if (OnTerrain && ZoneSystem.instance)
        {
            to.y += ZoneSystem.instance.GetGroundHeight(to) - ZoneSystem.instance.GetGroundHeight(from);
        }

        ghost.position = to;
    }
}
