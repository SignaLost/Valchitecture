using System;
using System.Collections.Generic;
using Chestworks;
using UnityEngine;
using Valchitecture.Common;

namespace Valchitecture.Modules;
/// <summary>
///     Keeps a piece another player is using (an open container, a pulled cart, a seat or a bed) from being removed or
///     moved, while the Guard Used setting is on. Storage use comes from Chestworks; a seat or bed counts as used when
///     another player is at its attach or spawn point.
/// </summary>
internal static class InUseGuard
{
    /// <summary>How far a player may be from a seat or bed point and still count as in it.</summary>
    private const float SeatTolerance = 0.25f;

    private const float BedTolerance = 1f;

    /// <summary>True while the local player's hammer removal runs.</summary>
    internal static bool Removing;

    private static readonly List<Chair> _chairs = new();
    private static readonly List<Bed> _beds = new();

    internal static bool Active => Cfg.Enabled.Value && Cfg.GuardUsed.Value;

    internal static bool UsedByOther(Piece piece)
    {
        if (ChestworksAPI.IsStorageInUse(piece))
        {
            return true;
        }

        Player self = Player.m_localPlayer;
        bool used = false;

        piece.GetComponentsInChildren(false, _chairs);

        foreach (Chair chair in _chairs)
        {
            if (chair.m_attachPoint && OtherAt(self, chair.m_attachPoint.position, SeatTolerance, false))
            {
                used = true;
                break;
            }
        }

        _chairs.Clear();

        if (used)
        {
            return true;
        }

        piece.GetComponentsInChildren(false, _beds);

        foreach (Bed bed in _beds)
        {
            if (bed.m_spawnPoint && OtherAt(self, bed.m_spawnPoint.position, BedTolerance, true))
            {
                used = true;
                break;
            }
        }

        _beds.Clear();
        return used;
    }

    /// <summary>Whether another player is within tolerance of the point, and asleep when required.</summary>
    private static bool OtherAt(Player self, Vector3 point, float tolerance, bool asleep)
    {
        foreach (Player other in Player.GetAllPlayers())
        {
            if (other && other != self && (!asleep || other.InBed())
                && Vector3.Distance(other.transform.position, point) < tolerance)
            {
                return true;
            }
        }

        return false;
    }
}
