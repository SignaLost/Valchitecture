using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Valchitecture.Common;
using Valchitecture.Core;

namespace Valchitecture.Modules;
/// <summary>
///     Server-relaxable building rules: which pieces support others, no-build zones, biome limits, clipping, and the
///     scaled station, extension and base ranges.
///     All are vanilla by default and take effect only with the mod on the server; the game's reads of them are
///     redirected here by <see cref="RuleReads" /> and the build rule patches.
/// </summary>
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

    /// <summary>A station extension's reach, scaled by Extension Range.</summary>
    internal static float ExtensionDistance(float distance)
    {
        return Live ? distance * Cfg.ExtensionRange.Value : distance;
    }

    /// <summary>Whether a piece supports others; runs inside the game's support loop.</summary>
    internal static bool Supports(WearNTear piece, bool supports)
    {
        if (supports || !Live)
        {
            return supports;
        }

        if (Cfg.EverythingSupports.Value)
        {
            return true;
        }

        ZDO zdo = piece.m_nview ? piece.m_nview.GetZDO() : null;
        int hash = zdo != null ? zdo.GetPrefab() : PrefabKey.Of(piece);
        return (Cfg.FurnitureSupports.Value && _furniture.Contains(hash)) || SupportList.Allows(hash);
    }

    /// <summary>The biomes a piece is limited to; none with Ignore Biome Limits.</summary>
    internal static Heightmap.Biome OnlyInBiome(Heightmap.Biome biomes)
    {
        return IgnoreBiomeLimits ? Heightmap.Biome.None : biomes;
    }

    /// <summary>Whether a piece may not clip into others; never with Ignore Clipping.</summary>
    internal static bool NoClipping(bool noClipping)
    {
        return noClipping && !IgnoreClipping;
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

/// <summary>
///     Applies the building rules where the game reads them. Each read stays in place and its value is adjusted right
///     after, so other mods' transpilers that look for the same reads still find them. The support check needs the piece,
///     which is often a neighbor rather than <c>this</c>, so it is duplicated on the stack before the read.
/// </summary>
internal static class RuleReads
{
    private static Dictionary<FieldInfo, (MethodInfo Hook, bool TakesOwner)> _hooks;

    internal static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        _hooks ??= new Dictionary<FieldInfo, (MethodInfo, bool)>
        {
            [AccessTools.Field(typeof(WearNTear), nameof(WearNTear.m_supports))] =
                (AccessTools.Method(typeof(BuildRules), nameof(BuildRules.Supports)), true),
            [AccessTools.Field(typeof(Piece), nameof(Piece.m_onlyInBiome))] =
                (AccessTools.Method(typeof(BuildRules), nameof(BuildRules.OnlyInBiome)), false),
            [AccessTools.Field(typeof(Piece), nameof(Piece.m_noClipping))] =
                (AccessTools.Method(typeof(BuildRules), nameof(BuildRules.NoClipping)), false),
            [AccessTools.Field(typeof(StationExtension), nameof(StationExtension.m_maxStationDistance))] =
                (AccessTools.Method(typeof(BuildRules), nameof(BuildRules.ExtensionDistance)), false)
        };

        List<CodeInstruction> codes = new();
        int adjusted = 0;

        foreach (CodeInstruction code in instructions)
        {
            if (code.opcode != OpCodes.Ldfld || code.operand is not FieldInfo field || !_hooks.TryGetValue(field, out (MethodInfo Hook, bool TakesOwner) read))
            {
                codes.Add(code);
                continue;
            }

            if (read.TakesOwner)
            {
                // A jump to the read must now land on the dup, or the stack would be one short.
                CodeInstruction dup = new(OpCodes.Dup);
                code.MoveLabelsTo(dup);
                codes.Add(dup);
            }

            codes.Add(code);
            codes.Add(new CodeInstruction(OpCodes.Call, read.Hook));
            adjusted++;
        }

        if (adjusted == 0)
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
