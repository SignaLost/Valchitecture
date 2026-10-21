using System.Collections.Generic;
using UnityEngine;
using Valchitecture.Common.Configuration;
using Valchitecture.Modules.Modes;
using Valchitecture.Common;
using Valchitecture.Modules.Snapping.Extra;

namespace Valchitecture.Modules.Snapping;

internal enum SnapMode
{
    Auto,
    Manual,
    Grid,
    Off
}

/// <summary>
///     Snap modes: Auto is the game's pairing, Manual pairs chosen points exactly, Grid aligns
///     unsnapped pieces to a world grid, and Off holds the game's alternative placement.
/// </summary>
internal static class SnapController
{
    private static readonly List<Transform> _targetPoints = new();
    private static readonly List<Transform> _ghostPoints = new();

    /// <summary>How much nearer another point must be before the cursor's choice moves to it.</summary>
    private const float CursorLoyalty = 0.1f;

    /// <summary>Chosen point on the target piece; -1 means the point nearest the cursor.</summary>
    private static int _targetIndex = -1;

    /// <summary>The piece <see cref="_targetIndex" /> was chosen on.</summary>
    private static Piece _targetPiece;

    private static Transform _lastCursorTarget;

    internal static SnapMode Mode { get; private set; } = SnapMode.Auto;

    private static string ModeToken => Mode switch
    {
        SnapMode.Auto => Tokens.SnapModeAuto,
        SnapMode.Manual => Tokens.SnapModeManual,
        SnapMode.Grid => Tokens.SnapModeGrid,
        _ => Tokens.SnapModeOff
    };

    internal static event System.Action OnSnapModeChanged;

    /// <summary>Only Manual has a chosen point.</summary>
    internal static bool PinSnapPointToAuto => Mode != SnapMode.Manual;

    internal static bool SnappingDisabled => Mode == SnapMode.Off;

    /// <summary>Whether this mod's Manual pairing decides the snap, rather than the game's.</summary>
    internal static bool ManualInCharge =>
        Mode == SnapMode.Manual && ModeManager.GhostDrivesPlacement && !Manipulator.NudgeActive;

    internal static bool TargetModifierHeld =>
        Cfg.TargetSnapModifier.Value != KeyCode.None && Input.GetKey(Cfg.TargetSnapModifier.Value);

    internal static bool IsCycleButton(string buttonName)
    {
        return buttonName is "TabLeft" or "TabRight" or "JoyPrevSnap" or "JoyNextSnap";
    }

    /// <summary>Whether a stepping key is withheld from the game.</summary>
    internal static bool SuppressCycleButton(string buttonName)
    {
        return PinSnapPointToAuto && IsCycleButton(buttonName);
    }

    internal static bool SuppressCycleTimer(string buttonName)
    {
        return PinSnapPointToAuto && buttonName is ("JoyPrevSnap" or "JoyNextSnap");
    }

    internal static void HandleInput(Player player)
    {
        if (!ModeManager.GhostDrivesPlacement || Manipulator.NudgeActive || Hud.IsPieceSelectionVisible() || Console.IsVisible())
        {
            return;
        }

        if (Mode == SnapMode.Manual && player && Input.GetKeyDown(Cfg.SnapResetKey.Value))
        {
            player.m_manualSnapPoint = -1;
            ClearTarget();
            ModeManager.Notify(Names.Localize(Tokens.SnapPointsAuto));
            return;
        }

        if (!Input.GetKeyDown(Cfg.CycleSnapModeKey.Value))
        {
            return;
        }

        Mode = Mode switch
        {
            SnapMode.Auto => SnapMode.Manual,
            SnapMode.Manual => SnapMode.Grid,
            SnapMode.Grid => SnapMode.Off,
            _ => SnapMode.Auto
        };

        if (PinSnapPointToAuto && player)
        {
            player.m_manualSnapPoint = -1;
        }

        if (Mode != SnapMode.Manual)
        {
            ClearTarget();
        }

        ModeManager.Notify(Names.Localize(ModeToken));
        OnSnapModeChanged?.Invoke();
    }

    internal static void ClearTarget()
    {
        _targetIndex = -1;
        _targetPiece = null;
        _lastCursorTarget = null;
    }

    /// <summary>Forgets a chosen target point when the aimed piece changes.</summary>
    private static void Track(Piece target)
    {
        if (target != _targetPiece)
        {
            ClearTarget();
            _targetPiece = target;
        }
    }

    /// <summary>Steps the chosen target point, wrapping back to automatic at either end.</summary>
    internal static void StepTarget(string buttonName)
    {
        if (Mode != SnapMode.Manual)
        {
            return;
        }

        int delta = buttonName is "TabRight" or "JoyNextSnap" ? 1 : -1;
        Piece target = ResolveTargetPiece(Player.m_localPlayer);

        if (!target)
        {
            return;
        }

        Track(target);
        CollectPoints(target, _targetPoints);

        if (_targetPoints.Count == 0)
        {
            return;
        }

        _targetIndex += delta;

        if (_targetIndex < -1)
        {
            _targetIndex = _targetPoints.Count - 1;
        }
        else if (_targetIndex >= _targetPoints.Count)
        {
            _targetIndex = -1;
        }

        ModeManager.Notify(_targetIndex < 0
            ? Names.Localize(Tokens.TargetAuto)
            : Names.Localize(Tokens.Target, Describe(_targetPoints[_targetIndex], _targetIndex)));
    }

    /// <summary>Manual pairing: the chosen ghost point (or the best mate) with the chosen target point (or the one nearest the cursor).</summary>
    internal static bool ResolvePair(Player player, Transform ghost, ref Transform a, ref Transform b)
    {
        if (Mode != SnapMode.Manual || !player || !ghost)
        {
            return false;
        }

        Piece target = ResolveTargetPiece(player, out Vector3 aim);

        if (!target)
        {
            return false;
        }

        Track(target);
        CollectPoints(target, _targetPoints);

        if (_targetPoints.Count == 0)
        {
            return false;
        }

        Transform chosen;

        if (_targetIndex >= 0)
        {
            _targetIndex = Mathf.Min(_targetIndex, _targetPoints.Count - 1);
            chosen = _targetPoints[_targetIndex];
        }
        else
        {
            chosen = NearestToCursor(_targetPoints, aim);
        }

        if (!chosen)
        {
            return false;
        }

        // The game fills its pair only within its 0.5 m threshold.
        Transform held = ChosenGhostPoint(player, ghost) ?? MatingGhostPoint(player, ghost, target, chosen);

        if (!held)
        {
            return false;
        }

        a = held;
        b = chosen;
        return true;
    }

    /// <summary>The ghost point the player stepped to with the game's keys, if any.</summary>
    private static Transform ChosenGhostPoint(Player player, Transform ghost)
    {
        Piece piece = player.m_manualSnapPoint >= 0 ? GhostInfo.PieceOf(ghost) : null;

        if (!piece)
        {
            return null;
        }

        CollectPoints(piece, _ghostPoints);
        return player.m_manualSnapPoint < _ghostPoints.Count ? _ghostPoints[player.m_manualSnapPoint] : null;
    }

    /// <summary>The target point nearest the cursor, kept until another is nearer by <see cref="CursorLoyalty" />.</summary>
    private static Transform NearestToCursor(List<Transform> points, Vector3 aim)
    {
        Transform nearest = Nearest(points, aim);

        if (_lastCursorTarget && nearest && _lastCursorTarget != nearest && points.Contains(_lastCursorTarget)
            && Vector3.Distance(_lastCursorTarget.position, aim) <= Vector3.Distance(nearest.position, aim) + CursorLoyalty)
        {
            return _lastCursorTarget;
        }

        _lastCursorTarget = nearest;
        return nearest;
    }

    /// <summary>The piece under the crosshair, by the game's placement ray, and where the ray meets it.</summary>
    private static Piece ResolveTargetPiece(Player player, out Vector3 aim)
    {
        aim = Vector3.zero;

        if (!player)
        {
            return null;
        }

        Piece ghostPiece = player.m_placementGhost ? GhostInfo.PieceOf(player.m_placementGhost) : null;
        bool water = ghostPiece && (ghostPiece.m_waterPiece || ghostPiece.m_noInWater);

        // A false result means out of reach; the hit is still filled in.
        if (!player.PieceRayTest(out aim, out _, out Piece hit, out _, out _, water))
        {
            return null;
        }

        return hit == ghostPiece ? null : hit;
    }

    private static Piece ResolveTargetPiece(Player player)
    {
        return ResolveTargetPiece(player, out _);
    }

    private static Transform Nearest(List<Transform> points, Vector3 to)
    {
        Transform best = null;
        float bestDistance = float.MaxValue;

        foreach (Transform point in points)
        {
            float distance = point ? Vector3.Distance(point.position, to) : float.MaxValue;

            if (distance < bestDistance)
            {
                best = point;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary>The ghost point whose offset from origin best cancels the chosen target point's, skipping pairings the game would reject as duplicates.</summary>
    private static Transform MatingGhostPoint(Player player, Transform ghost, Piece target, Transform chosen)
    {
        Piece piece = GhostInfo.PieceOf(ghost);

        if (!piece)
        {
            return null;
        }

        CollectPoints(piece, _ghostPoints);

        Vector3 asked = chosen.position - target.transform.position;
        Transform best = null;
        Transform closest = null;
        float bestFit = float.MaxValue;
        float closestFit = float.MaxValue;

        foreach (Transform candidate in _ghostPoints)
        {
            if (!candidate)
            {
                continue;
            }

            float fit = (candidate.position - ghost.position + asked).sqrMagnitude;

            if (fit < closestFit)
            {
                closest = candidate;
                closestFit = fit;
            }

            if (fit < bestFit && !Duplicates(player, ghost, piece, chosen, candidate))
            {
                best = candidate;
                bestFit = fit;
            }
        }

        return best ?? closest;
    }

    /// <summary>Whether this pairing lands on a piece of the same kind, by the game's <c>IsOverlappingOtherPiece</c>.</summary>
    private static bool Duplicates(Player player, Transform ghost, Piece piece, Transform target, Transform candidate)
    {
        Vector3 landing = target.position - (candidate.position - ghost.position);
        return player.IsOverlappingOtherPiece(landing, ghost.rotation, ghost.gameObject.name, player.m_tempPieces, piece.m_allowRotatedOverlap);
    }

    private static void CollectPoints(Piece piece, List<Transform> into)
    {
        into.Clear();
        piece.GetSnapPoints(into);
    }

    /// <summary>A point's localized name, or a number for unnamed points.</summary>
    private static string Describe(Transform point, int index)
    {
        string name = point ? point.name : null;
        bool named = !string.IsNullOrEmpty(name) && !name.StartsWith(SnapPointNames.DEFAULT_NAME);

        return named ? Names.Localize(name) : Names.Localize(Tokens.Point, (index + 1).ToString());
    }
}
