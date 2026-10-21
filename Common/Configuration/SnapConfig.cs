using System;
using BepInEx.Configuration;
using UnityEngine;

namespace Valchitecture.Common.Configuration;

/// <summary>Extra snap point settings, synced from the server.</summary>
internal static class SnapConfig
{
    private const string SecExtra = Cfg.SecSnapping;

    internal static ConfigEntry<bool> EnableExtraSnapPoints;
    internal static ConfigEntry<bool> EnablePoleBeamSnapPoints;
    internal static ConfigEntry<bool> EnableTriangleSnapPoints;
    internal static ConfigEntry<bool> EnableRect2DSnapPoints;
    internal static ConfigEntry<bool> EnableRoofTopSnapPoints;
    internal static ConfigEntry<bool> EnableBoxSnapPoints;
    internal static ConfigEntry<bool> EnableTerrainOpSnapPoints;
    internal static ConfigEntry<bool> EnableHandPlacedSnapPoints;
    internal static ConfigEntry<float> GridSpacing;

    /// <summary>Settle time in seconds before a requested rebuild runs.</summary>
    private const float RebuildDelay = 0.5f;

    private static float _rebuildAt = -1f;

    /// <summary>Whether a requested rebuild has settled and should run.</summary>
    internal static bool RebuildDue => _rebuildAt >= 0f && Time.realtimeSinceStartup >= _rebuildAt;

    /// <summary>Requests a snap point rebuild, delayed until settings stop changing.</summary>
    internal static void RequestRebuild()
    {
        _rebuildAt = Time.realtimeSinceStartup + RebuildDelay;
    }

    internal static void ClearRebuild()
    {
        _rebuildAt = -1f;
    }

    internal static void Bind(ConfigFile config)
    {
        EnableExtraSnapPoints = config.Bind(SecExtra, "Extra Snap Points", true,
            Cfg.Server("Enables adding extra snap points to build pieces."));
        EnablePoleBeamSnapPoints = config.Bind(SecExtra, "Extra Snap Points: Poles And Beams", true,
            Cfg.Server("Ends and points along each side of poles, beams and other pieces with two snap points."));
        EnableTriangleSnapPoints = config.Bind(SecExtra, "Extra Snap Points: Triangle", true,
            Cfg.Server("Edges and centers on triangular pieces."));
        EnableRect2DSnapPoints = config.Bind(SecExtra, "Extra Snap Points: 2D-Rectangle", true,
            Cfg.Server("Edges and centers on floors, walls and other flat pieces."));
        EnableRoofTopSnapPoints = config.Bind(SecExtra, "Extra Snap Points: Roof Top", true,
            Cfg.Server("Ridges on roof pieces."));
        EnableBoxSnapPoints = config.Bind(SecExtra, "Extra Snap Points: Box", true,
            Cfg.Server("Face centers and edge midpoints on block pieces."));
        EnableHandPlacedSnapPoints = config.Bind(SecExtra, "Extra Snap Points: Hand-Placed", true,
            Cfg.Server("Fixed layouts for fences, stakes, furniture and other pieces the shapes do not suit."));
        EnableTerrainOpSnapPoints = config.Bind(SecExtra, "Extra Snap Points: Terrain", true,
            Cfg.Server("A snap point on leveling and smoothing tools, so they can snap to a floor edge."));
        GridSpacing = config.Bind(SecExtra, "Extra Snap Points: Grid Spacing", 2f,
            Cfg.Server("Spacing of points across large flat pieces, in meters. Minimum 0.5; 0 turns the grid off.",
                new AcceptableValueRange<float>(0f, 8f)));

        Cfg.Enabled.SettingChanged += OnSnapSettingChanged;
        EnableExtraSnapPoints.SettingChanged += OnSnapSettingChanged;
        EnablePoleBeamSnapPoints.SettingChanged += OnSnapSettingChanged;
        EnableTriangleSnapPoints.SettingChanged += OnSnapSettingChanged;
        EnableRect2DSnapPoints.SettingChanged += OnSnapSettingChanged;
        EnableRoofTopSnapPoints.SettingChanged += OnSnapSettingChanged;
        EnableBoxSnapPoints.SettingChanged += OnSnapSettingChanged;
        EnableTerrainOpSnapPoints.SettingChanged += OnSnapSettingChanged;
        EnableHandPlacedSnapPoints.SettingChanged += OnSnapSettingChanged;
        GridSpacing.SettingChanged += OnSnapSettingChanged;
    }

    private static void OnSnapSettingChanged(object sender, EventArgs e)
    {
        RequestRebuild();
    }
}
