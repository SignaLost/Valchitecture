using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valchitecture.Modules.Modes;

/// <summary>Repeats the checks of <c>Player.UpdatePlacementGhost</c> at the frozen position, skipping rules tied to the aimed surface.</summary>
internal static class FrozenPlacement
{
    /// <summary>The game's per-vertex clamp on leveling, in meters.</summary>
    private const float MaxLevelDelta = 8f;

    /// <summary>How far a ground-only piece may be nudged off the terrain surface.</summary>
    private const float MaxGroundGap = 0.5f;

    private static int _pieceMask;
    private static Collider[] _hits = new Collider[64];

    private static ZNetView _mustConnectTo;
    private static string _mustConnectToName;

    /// <summary>Object names of views by prefab hash, as the game compares them.</summary>
    private static readonly Dictionary<int, string> _viewNames = new();

    internal static void Revalidate(Player player, GameObject ghost)
    {
        Piece piece = GhostInfo.PieceOf(ghost);

        if (!piece)
        {
            return;
        }

        Vector3 position = ghost.transform.position;

        if (!Reach.InReach(player, position, piece)
            || !Reach.Allows(player, position, piece.m_extraPlacementDistance))
        {
            player.m_placementStatus = Player.PlacementStatus.NoRayHits;
            player.SetPlacementGhostValid(false);
            return;
        }

        Player.PlacementStatus status = Player.PlacementStatus.Valid;

        StationExtension extension = GhostInfo.ExtensionOf(ghost);

        if (extension)
        {
            CraftingStation station = extension.FindClosestStationInRange(position);

            if (station)
            {
                extension.StartConnectionEffect(station);
            }
            else
            {
                extension.StopConnectionEffect();
                status = Player.PlacementStatus.ExtensionMissingStation;
            }

            if (extension.OtherExtensionInRange(piece.m_spaceRequirement))
            {
                status = Player.PlacementStatus.MoreSpace;
            }
        }

        if (Blocked(piece, position))
        {
            status = Player.PlacementStatus.MoreSpace;
        }

        if (piece.m_mustConnectTo && !Connected(player, piece, position))
        {
            status = Player.PlacementStatus.Invalid;
        }

        status = CheckWater(piece, position, status);
        status = CheckGround(player, ghost, piece, position, status);

        if (piece.m_onlyInTeleportArea && !EffectArea.IsPointInsideArea(position, EffectArea.Type.Teleport))
        {
            status = Player.PlacementStatus.NoTeleportArea;
        }

        if (!piece.m_allowedInDungeons && player.InInterior()
            && !EnvMan.instance.CheckInteriorBuildingOverride()
            && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.DungeonBuild))
        {
            status = Player.PlacementStatus.NotInDungeon;
        }

        if (Location.IsInsideNoBuildLocation(position))
        {
            status = Player.PlacementStatus.NoBuildZone;
        }

        PrivateArea area = GhostInfo.AreaOf(ghost);
        float radius = area ? area.m_radius : 0f;

        if (!PrivateArea.CheckAccess(position, radius, false, area != null))
        {
            status = Player.PlacementStatus.PrivateZone;
        }

        if (player.CheckPlacementGhostVSPlayers())
        {
            status = Player.PlacementStatus.BlockedbyPlayer;
        }

        Heightmap.Biome onlyIn = BuildRules.OnlyInBiome(piece);

        if (onlyIn != Heightmap.Biome.None && (Heightmap.FindBiome(position) & onlyIn) == 0)
        {
            status = Player.PlacementStatus.WrongBiome;
        }

        if (BuildRules.NoClipping(piece) && player.TestGhostClipping(ghost, 0.2f))
        {
            status = Player.PlacementStatus.Invalid;
        }

        player.m_placementStatus = status;
        player.SetPlacementGhostValid(status == Player.PlacementStatus.Valid);
    }

    /// <summary>The game's blocking-piece test (<c>m_blockRadius</c>).</summary>
    private static bool Blocked(Piece piece, Vector3 position)
    {
        if (piece.m_blockRadius <= 0f || piece.m_blockingPieces.Count == 0)
        {
            return false;
        }

        if (_pieceMask == 0)
        {
            _pieceMask = LayerMask.GetMask("piece");
        }

        int count = Overlap(position, piece.m_blockRadius, _pieceMask);
        bool blocked = false;

        for (int i = 0; i < count && !blocked; i++)
        {
            Piece other = _hits[i].GetComponentInParent<Piece>();

            if (!other || other == piece)
            {
                continue;
            }

            foreach (Piece blocking in piece.m_blockingPieces)
            {
                if (blocking.m_name == other.m_name)
                {
                    blocked = true;
                    break;
                }
            }
        }

        Array.Clear(_hits, 0, count);
        return blocked;
    }

    /// <summary>The game's <c>m_mustConnectTo</c> test.</summary>
    private static bool Connected(Player player, Piece piece, Vector3 position)
    {
        string wanted = MustConnectToName(piece);
        int count = Overlap(position, piece.m_connectRadius, Physics.DefaultRaycastLayers);
        bool connected = false;

        for (int i = 0; i < count && !connected; i++)
        {
            ZNetView view = _hits[i].GetComponentInParent<ZNetView>();

            if (!view || view == player.m_nview || !NameOf(view).Contains(wanted))
            {
                continue;
            }

            if (piece.m_mustBeAboveConnected
                && (!Physics.Raycast(position, Vector3.down, out RaycastHit below) || below.transform.GetComponentInParent<ZNetView>() != view))
            {
                continue;
            }

            connected = true;
        }

        Array.Clear(_hits, 0, count);
        return connected;
    }

    /// <summary>Colliders within the sphere, into <see cref="_hits" />, grown when it fills.</summary>
    private static int Overlap(Vector3 position, float radius, int mask)
    {
        int count = Physics.OverlapSphereNonAlloc(position, radius, _hits, mask);

        while (count == _hits.Length)
        {
            _hits = new Collider[_hits.Length * 2];
            count = Physics.OverlapSphereNonAlloc(position, radius, _hits, mask);
        }

        return count;
    }

    /// <summary>The name of the piece's required neighbor prefab, read once per prefab.</summary>
    private static string MustConnectToName(Piece piece)
    {
        if (!ReferenceEquals(piece.m_mustConnectTo, _mustConnectTo))
        {
            _mustConnectTo = piece.m_mustConnectTo;
            _mustConnectToName = _mustConnectTo.name;
        }

        return _mustConnectToName;
    }

    /// <summary>A view's object name, cached per prefab for objects with a ZDO.</summary>
    private static string NameOf(ZNetView view)
    {
        ZDO zdo = view.GetZDO();

        if (zdo == null)
        {
            return view.name;
        }

        int prefab = zdo.GetPrefab();

        if (!_viewNames.TryGetValue(prefab, out string name))
        {
            name = view.name;
            _viewNames[prefab] = name;
        }

        return name;
    }

    /// <summary>Water rules, judged by whether the terrain below lies under sea level.</summary>
    private static Player.PlacementStatus CheckWater(Piece piece, Vector3 position, Player.PlacementStatus status)
    {
        if (!piece.m_waterPiece && !piece.m_noInWater)
        {
            return status;
        }

        float sea = ZoneSystem.instance.m_waterLevel;
        bool overWater = ZoneSystem.instance.GetGroundHeight(position) < sea;

        if (piece.m_waterPiece && !overWater)
        {
            return Player.PlacementStatus.Invalid;
        }

        return piece.m_noInWater && overWater && position.y < sea ? Player.PlacementStatus.Invalid : status;
    }

    /// <summary>Ground and snow rules at the piece.</summary>
    private static Player.PlacementStatus CheckGround(Player player, GameObject ghost, Piece piece, Vector3 position, Player.PlacementStatus status)
    {
        bool groundOnly = piece.m_groundOnly || piece.m_cultivatedGroundOnly || piece.m_vegetationGroundOnly;
        bool snowRule = !piece.m_allowedInDeepSnow || piece.m_requireDeepSnow;

        if (!groundOnly && !piece.m_groundPiece && !snowRule)
        {
            return status;
        }

        Heightmap heightmap = Heightmap.FindHeightmap(position);

        if (!heightmap || !heightmap.GetWorldHeight(position, out float ground))
        {
            return groundOnly || piece.m_groundPiece || piece.m_requireDeepSnow ? Player.PlacementStatus.Invalid : status;
        }

        // Stands in for the game's ray having hit terrain.
        bool onGround = Mathf.Abs(position.y - ground) <= MaxGroundGap;

        if (groundOnly && !onGround && !Manipulator.HeightLocked)
        {
            return Player.PlacementStatus.Invalid;
        }

        if (piece.m_cultivatedGroundOnly && !heightmap.IsCultivated(position))
        {
            status = Player.PlacementStatus.NeedCultivated;
        }

        if (piece.m_vegetationGroundOnly)
        {
            float vegetation = heightmap.GetVegetationMask(position);
            bool bare = heightmap.GetBiome(position) == Heightmap.Biome.AshLands ? vegetation > 0.1f : vegetation < 0.25f;

            if (bare)
            {
                status = Player.PlacementStatus.NeedDirt;
            }
        }

        bool deepNorth = player.m_currentBiome == Heightmap.Biome.DeepNorth;

        if (!piece.m_allowedInDeepSnow && deepNorth && onGround && heightmap.GetCultivationMask(position) > player.m_deepSnowBuildHeight)
        {
            status = Player.PlacementStatus.DeepSnow;
        }

        if (piece.m_requireDeepSnow && (!deepNorth || !onGround || heightmap.GetCultivationMask(position) <= 0f))
        {
            status = Player.PlacementStatus.NoSnow;
        }

        TerrainOp op = GhostInfo.TerrainOpOf(ghost);

        if (op && op.m_settings.m_level
            && Mathf.Abs(position.y + op.m_settings.m_levelOffset - ground) > MaxLevelDelta)
        {
            status = Player.PlacementStatus.Invalid;
        }

        return status;
    }
}
