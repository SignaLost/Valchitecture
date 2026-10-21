using System.Collections.Generic;
using UnityEngine;
using Valchitecture.Common;
using Valchitecture.Core;

namespace Valchitecture.Modules;
/// <summary>
///     Edit mode, unlocked by the Building skill's edit ability: picks up a built piece, drives a ghost of it through
///     the game's placement, and recreates it where the ghost lands.
///     The move recreates the piece from a copy of its ZDO, keeping its stored data, and charges the stamina and wear
///     of a removal plus a placement; cancelling returns to the Repair entry with the piece untouched.
/// </summary>
internal static class EditSession
{
    private static Piece _grabbed;
    private static ZNetView _grabbedView;
    private static Vector3 _initialPosition;
    private static bool _ghostDriven;

    private static readonly List<Collider> _suspendedColliders = new();
    private static readonly List<Renderer> _hiddenRenderers = new();
    private static readonly List<Collider> _foundColliders = new();
    private static readonly List<Renderer> _foundRenderers = new();

    internal static bool HasGrabbed => _grabbed;

    /// <summary>True while the game's placement ghost represents a held piece.</summary>
    internal static bool IsGhostDriven => _ghostDriven && _grabbed;

    internal static void OnEnterMode()
    {
        Release(restoreSelection: false);
    }

    /// <summary>Cancels any move in progress.</summary>
    internal static void OnExitMode()
    {
        Release(restoreSelection: true);
    }

    /// <summary>Ends a move whose piece was destroyed elsewhere.</summary>
    internal static void ReleaseIfLost()
    {
        if (_ghostDriven && !_grabbed)
        {
            Release(restoreSelection: true);
        }
    }

    private static void Release(bool restoreSelection)
    {
        // Selecting a piece creates a ghost.
        bool shuttingDown = Game.instance && Game.instance.IsShuttingDown();

        if (_ghostDriven && restoreSelection && Player.m_localPlayer && !shuttingDown)
        {
            HammerTable.SelectRepair(Player.m_localPlayer);
        }

        _ghostDriven = false;
        RestoreRenderers();
        RestoreColliders();
        _grabbed = null;
        _grabbedView = null;
    }

    /// <summary>Handles edit input; returns true when this frame's input was consumed.</summary>
    internal static bool HandleInput(Player player)
    {
        if (!ModeManager.IsEdit || Hud.IsPieceSelectionVisible())
        {
            return false;
        }

        bool place = ZInput.GetButtonDown("Attack") || ZInput.GetButtonDown("JoyPlace");

        bool cancel = Input.GetKeyDown(Cfg.CancelEditKey.Value)
                      || (!Manipulator.CopyModifierHeld && (ZInput.GetButtonDown("Remove") || ZInput.GetButtonDown("JoyRemove")));

        if (!_grabbed)
        {
            return place && TryGrab(player);
        }

        if (cancel)
        {
            // The game removes on release.
            Manipulator.SwallowRemoveUntilRelease();
            Release(restoreSelection: true);
            return true;
        }

        if (place)
        {
            Commit(player);
            return true;
        }

        return false;
    }

    private static bool TryGrab(Player player)
    {
        Piece piece = player.GetHoveringPiece();

        if (!piece || !piece.m_canBeRemoved || !CanEdit(player, piece))
        {
            return false;
        }

        ZNetView netView = piece.m_nview;

        if (!netView || !netView.IsValid() || !HammerTable.CanSelect(player, piece))
        {
            return false;
        }

        netView.ClaimOwnership();

        _grabbed = piece;
        _grabbedView = netView;
        _initialPosition = piece.transform.position;

        if (!HammerTable.Select(player, piece))
        {
            _grabbed = null;
            _grabbedView = null;
            return false;
        }

        _ghostDriven = true;

        Manipulator.AdoptRotation(piece.transform.rotation, player);

        HideRenderers(piece);
        SuspendColliders(piece);

        ModeManager.NotifyMinor(Names.Localize(Tokens.Moving, Names.Of(piece)));
        return true;
    }

    /// <summary>The game's removal checks, except that containers holding items may be moved.</summary>
    private static bool CanEdit(Player player, Piece piece)
    {
        Vector3 position = piece.transform.position;

        if (Location.IsInsideNoBuildLocation(position))
        {
            PlacementMessage.Show(player, Player.PlacementStatus.NoBuildZone);
            return false;
        }

        if (!PrivateArea.CheckAccess(position))
        {
            PlacementMessage.Show(player, Player.PlacementStatus.PrivateZone);
            return false;
        }

        if (InUse(piece))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_inuse");
            return false;
        }

        return player.CheckCanRemovePiece(piece);
    }

    /// <summary>Whether the piece is an open container, a ship with players aboard, or, with Guard Used, anything another player is using.</summary>
    private static bool InUse(Piece piece)
    {
        if (InUseGuard.Active && InUseGuard.UsedByOther(piece))
        {
            return true;
        }

        Container container = piece.GetComponentInChildren<Container>();
        ZDO zdo = container && container.m_nview ? container.m_nview.GetZDO() : null;

        if (zdo != null && zdo.GetInt(ZDOVars.s_inUse) == 1)
        {
            return true;
        }

        Ship ship = piece.GetComponentInChildren<Ship>();
        return ship && !ship.CanBeRemoved();
    }

    /// <summary>The station test of <c>Player.CheckCanRemovePiece</c>, plus the camera build station rule at the destination.</summary>
    private static bool HasBench(Player player, Piece piece, Vector3 point)
    {
        if (!piece.m_craftingStation || player.PlacementCostDisabled || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench))
        {
            return true;
        }

        if (CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, player.transform.position)
            && Reach.BenchHolds(player, piece, point))
        {
            return true;
        }

        player.Message(MessageHud.MessageType.Center, "$msg_missingstation");
        return false;
    }

    private static void Commit(Player player)
    {
        Piece piece = _grabbed;
        ZNetView netView = _grabbedView;
        GameObject ghost = player.m_placementGhost;

        if (!piece || !netView || !netView.IsValid() || !ghost)
        {
            Release(restoreSelection: true);
            return;
        }

        if (player.m_placementStatus != Player.PlacementStatus.Valid)
        {
            PlacementMessage.Show(player, player.m_placementStatus);
            return;
        }

        Vector3 position = ghost.transform.position;
        Quaternion rotation = ghost.transform.rotation;

        if (!Reach.InReach(player, position, piece)
            || !Reach.Allows(player, position, piece.m_extraPlacementDistance))
        {
            PlacementMessage.Show(player, Player.PlacementStatus.NoRayHits);
            return;
        }

        if (!HasBench(player, piece, position))
        {
            return;
        }

        // Must stay the last check.
        float cost = BuildCost.OfMove(player);

        if (!BuildCost.Afford(player, cost))
        {
            return;
        }

        GameObject moved = Relocate(netView, position, rotation);

        if (!moved)
        {
            Release(restoreSelection: true);
            return;
        }

        piece.m_placeEffect?.Create(position, rotation, moved.transform);
        Resettle(moved);

        BuildCost.Charge(player, cost);
        BuildCost.Wear(player, BuildCost.WearOfMove(player));
        BuildingSkill.Raise(player, BuildingSkill.MoveExperience);

        // This path bypasses TryPlacePiece.
        Manipulator.OnPiecePlaced();

        if (Log.Shows(Log.InfoLevel.Medium))
        {
            Log.LogInfo($"Moved {piece.name} from {_initialPosition} to {position}.", Log.InfoLevel.Medium);
        }

        Release(restoreSelection: true);
    }

    /// <summary>Recreates the piece at the new position from a copy of its ZDO.</summary>
    private static GameObject Relocate(ZNetView netView, Vector3 position, Quaternion rotation)
    {
        ZDO old = netView.GetZDO();

        if (old == null || !ZNetScene.instance || ZDOMan.instance == null)
        {
            return null;
        }

        netView.ClaimOwnership();

        ZPackage data = new();
        old.Serialize(data);
        data.SetPos(0);

        ZDO fresh = ZDOMan.instance.CreateNewZDO(position, old.GetPrefab());
        fresh.Deserialize(data);
        fresh.SetPosition(position);
        fresh.SetRotation(rotation);

        GameObject created = ZNetScene.instance.CreateObject(fresh);

        if (!created)
        {
            ZDOMan.instance.DestroyZDO(fresh);
            return null;
        }

        ZNetScene.instance.Destroy(netView.gameObject);
        return created;
    }

    /// <summary>The support refresh from <c>WearNTear.OnPlaced</c>, without calling it.</summary>
    private static void Resettle(GameObject moved)
    {
        if (moved.TryGetComponent(out WearNTear wear))
        {
            wear.m_createTime = -1f;
            wear.m_clearCachedSupport = true;
        }
    }

    private static void SuspendColliders(Piece piece)
    {
        RestoreColliders();
        piece.GetComponentsInChildren(true, _foundColliders);

        foreach (Collider collider in _foundColliders)
        {
            if (collider && collider.enabled && !collider.isTrigger)
            {
                collider.enabled = false;
                _suspendedColliders.Add(collider);
            }
        }

        _foundColliders.Clear();
    }

    private static void RestoreColliders()
    {
        foreach (Collider collider in _suspendedColliders)
        {
            if (collider)
            {
                collider.enabled = true;
            }
        }

        _suspendedColliders.Clear();
    }

    private static void HideRenderers(Piece piece)
    {
        RestoreRenderers();
        piece.GetComponentsInChildren(true, _foundRenderers);

        foreach (Renderer renderer in _foundRenderers)
        {
            if (renderer && renderer.enabled)
            {
                renderer.enabled = false;
                _hiddenRenderers.Add(renderer);
            }
        }

        _foundRenderers.Clear();
    }

    private static void RestoreRenderers()
    {
        foreach (Renderer renderer in _hiddenRenderers)
        {
            if (renderer)
            {
                renderer.enabled = true;
            }
        }

        _hiddenRenderers.Clear();
    }
}
