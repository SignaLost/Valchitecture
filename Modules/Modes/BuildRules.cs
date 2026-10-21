using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Valchitecture.Common.Configuration;
using Valchitecture.Common;

namespace Valchitecture.Modules.Modes;
/// <summary>Server-relaxable building rules, vanilla by default and inactive on vanilla servers.</summary>
internal static class BuildRules
{
    /// <summary>Prefab hashes of flat-topped vanilla furniture covered by Furniture Supports.</summary>
    private static readonly HashSet<int> _furniture = Hashes(
        "piece_workbench", "piece_stonecutter", "piece_artisanstation", "fermenter",
        "piece_table", "piece_table_oak", "piece_table_round", "piece_blackmarble_table",
        "piece_table_runed", "piece_table_runed_small",
        "piece_cartographytable", "piece_preptable", "piece_magetable", "piece_magetable_ext",
        "piece_chair", "piece_chair02", "piece_chair03", "piece_chair_runed",
        "piece_bench01", "piece_blackmarble_bench", "piece_blackwood_bench01",
        "piece_bench_runed",
        "piece_throne01", "piece_throne02", "piece_blackmarble_throne", "piece_bone_throne",
        "piece_moose_throne",
        "piece_chest_wood", "piece_chest", "piece_chest_private", "piece_chest_blackmetal",
        "piece_chest_grausten", "piece_chest_warderobe", "piece_chest_barrel");

    private static bool Live => Cfg.Enabled.Value && ServerMod.Present;

    internal static bool IgnoreNoBuildZones => Cfg.IgnoreNoBuildZones.Value && Live;
    internal static bool IgnoreBiomeLimits => Cfg.IgnoreBiomeLimits.Value && Live;
    internal static bool IgnoreClipping => Cfg.IgnoreClipping.Value && Live;

    /// <summary>Whether any rule is relaxed in the config but inactive on a vanilla server.</summary>
    internal static bool HeldBack =>
        Cfg.Enabled.Value && !ServerMod.Present
        && (Cfg.EverythingSupports.Value || Cfg.FurnitureSupports.Value || Cfg.IgnoreNoBuildZones.Value || Cfg.IgnoreBiomeLimits.Value
            || Cfg.IgnoreClipping.Value || !Cfg.StationRange.Value.Equals(1f) || !Cfg.ExtensionRange.Value.Equals(1f)
            || !Cfg.BaseRange.Value.Equals(1f));

    /// <summary>
    ///     Scales a station's build range, marker and tied collider by Station Range and its
    ///     PlayerBase areas by Base Range, recomputed from the station's own values each time.
    /// </summary>
    internal static void ScaleStation(CraftingStation station, ScaledStation scaled)
    {
        float build = Live ? Cfg.StationRange.Value : 1f;
        float spawn = Live ? Cfg.BaseRange.Value : 1f;
        int extensions = station.GetExtentionCount(false);

        if (!scaled.Due(build, spawn, extensions))
        {
            return;
        }

        float unscaled = station.m_rangeBuild + extensions * station.m_extraRangePerLevel;
        float range = unscaled * build;

        station.m_buildRange = range;

        if (station.m_areaMarkerCircle)
        {
            station.m_areaMarkerCircle.m_radius = range;
        }

        // The game sizes this collider to the build range.
        Collider tied = scaled.Tied;

        if (tied)
        {
            float radius = unscaled * (scaled.TiedIsBase ? spawn : build);

            switch (tied)
            {
                case SphereCollider sphere:
                    sphere.radius = radius;
                    break;
                case CapsuleCollider capsule:
                    capsule.radius = radius;
                    break;
            }
        }

        for (int i = 0; i < scaled.Bases.Length; i++)
        {
            SphereCollider sphere = scaled.Bases[i];

            if (sphere)
            {
                sphere.radius = scaled.BaseRadii[i] * spawn;
            }
        }
    }

    /// <summary>Forces every station to refresh its range on next read.</summary>
    internal static void RefreshStations()
    {
        foreach (CraftingStation station in CraftingStation.m_allStations)
        {
            if (station)
            {
                station.m_updateExtensionTimer = 2f;
            }
        }
    }

    /// <summary>Replaces reads of <c>StationExtension.m_maxStationDistance</c>.</summary>
    internal static float ExtensionDistance(StationExtension extension)
    {
        return Live ? extension.m_maxStationDistance * Cfg.ExtensionRange.Value : extension.m_maxStationDistance;
    }

    /// <summary>Replaces reads of <c>WearNTear.m_supports</c>; runs inside the game's support loop.</summary>
    internal static bool Supports(WearNTear piece)
    {
        if (piece.m_supports || !Live)
        {
            return piece.m_supports;
        }

        if (Cfg.EverythingSupports.Value)
        {
            return true;
        }

        ZDO zdo = piece.m_nview ? piece.m_nview.GetZDO() : null;
        int hash = zdo != null ? zdo.GetPrefab() : PrefabKey.Of(piece);
        return (Cfg.FurnitureSupports.Value && _furniture.Contains(hash)) || SupportList.Allows(hash);
    }

    /// <summary>Replaces reads of <c>Piece.m_onlyInBiome</c>.</summary>
    internal static Heightmap.Biome OnlyInBiome(Piece piece)
    {
        return IgnoreBiomeLimits ? Heightmap.Biome.None : piece.m_onlyInBiome;
    }

    /// <summary>Replaces reads of <c>Piece.m_noClipping</c>.</summary>
    internal static bool NoClipping(Piece piece)
    {
        return piece.m_noClipping && !IgnoreClipping;
    }

    private static HashSet<int> Hashes(params string[] names)
    {
        HashSet<int> set = new();

        foreach (string name in names)
        {
            set.Add(name.GetStableHashCode());
        }

        return set;
    }
}

/// <summary>Replaces rule field reads with static calls into <see cref="BuildRules" />.</summary>
internal static class RuleReads
{
    private static Dictionary<FieldInfo, MethodInfo> _hooks;

    internal static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        _hooks ??= new Dictionary<FieldInfo, MethodInfo>
        {
            [AccessTools.Field(typeof(WearNTear), nameof(WearNTear.m_supports))] =
                AccessTools.Method(typeof(BuildRules), nameof(BuildRules.Supports)),
            [AccessTools.Field(typeof(Piece), nameof(Piece.m_onlyInBiome))] =
                AccessTools.Method(typeof(BuildRules), nameof(BuildRules.OnlyInBiome)),
            [AccessTools.Field(typeof(Piece), nameof(Piece.m_noClipping))] =
                AccessTools.Method(typeof(BuildRules), nameof(BuildRules.NoClipping)),
            [AccessTools.Field(typeof(StationExtension), nameof(StationExtension.m_maxStationDistance))] =
                AccessTools.Method(typeof(BuildRules), nameof(BuildRules.ExtensionDistance))
        };

        List<CodeInstruction> codes = new(instructions);
        int replaced = 0;

        foreach (CodeInstruction code in codes)
        {
            if (code.opcode == OpCodes.Ldfld && code.operand is FieldInfo field && _hooks.TryGetValue(field, out MethodInfo hook))
            {
                code.opcode = OpCodes.Call;
                code.operand = hook;
                replaced++;
            }
        }

        if (replaced == 0)
        {
            throw new InvalidOperationException(
                $"Valchitecture: no building rule reads found in {original.DeclaringType?.Name}.{original.Name}.");
        }

        return codes;
    }
}

/// <summary>Per station cache of the colliders it scales, their unscaled radii, and what they were last scaled with.</summary>
internal sealed class ScaledStation : MonoBehaviour
{
    private static CraftingStation _lastStation;
    private static ScaledStation _lastScaled;

    private static readonly List<EffectArea> _areas = new();
    private static readonly List<SphereCollider> _spheres = new();
    private static readonly List<float> _radii = new();

    /// <summary>The collider the game sizes to the build range, or null.</summary>
    internal Collider Tied;

    internal bool TiedIsBase;

    /// <summary>PlayerBase area spheres other than <see cref="Tied" />.</summary>
    internal SphereCollider[] Bases;

    internal float[] BaseRadii;

    /// <summary>Whether the station is a placement ghost, whose refresh timer never advances.</summary>
    private bool _ghost;

    private int _frame = -1;
    private float _build;
    private float _spawn;
    private int _extensions;

    internal static ScaledStation Of(CraftingStation station)
    {
        if (ReferenceEquals(station, _lastStation))
        {
            return _lastScaled;
        }

        if (!station.TryGetComponent(out ScaledStation scaled))
        {
            scaled = station.gameObject.AddComponent<ScaledStation>();
            scaled.Resolve(station);
        }

        _lastStation = station;
        _lastScaled = scaled;
        return scaled;
    }

    private void Resolve(CraftingStation station)
    {
        // A ghost's view is destroyed on creation; the game's own timer check uses the same test.
        ZNetView view = station.GetComponent<ZNetView>();
        _ghost = !view || !view.IsValid();
        Tied = station.m_effectAreaCollider;
        TiedIsBase = Tied && Tied.TryGetComponent(out EffectArea effect) && (effect.m_type & EffectArea.Type.PlayerBase) != 0;

        station.GetComponentsInChildren(true, _areas);

        foreach (EffectArea area in _areas)
        {
            if (!area
                || (area.m_type & EffectArea.Type.PlayerBase) == 0
                || area.GetComponent<Collider>() == Tied
                || area.GetComponent<SphereCollider>() is not { } sphere)
            {
                continue;
            }

            _spheres.Add(sphere);
            _radii.Add(sphere.radius);
        }

        Bases = _spheres.ToArray();
        BaseRadii = _radii.ToArray();

        _areas.Clear();
        _spheres.Clear();
        _radii.Clear();
    }

    /// <summary>Whether to scale now: once per frame, and for a ghost only when the ranges or extension count changed.</summary>
    internal bool Due(float build, float spawn, int extensions)
    {
        if (_frame == Time.frameCount)
        {
            return false;
        }

        if (_ghost && _frame >= 0 && build == _build && spawn == _spawn && extensions == _extensions)
        {
            return false;
        }

        _frame = Time.frameCount;
        _build = build;
        _spawn = spawn;
        _extensions = extensions;
        return true;
    }
}
