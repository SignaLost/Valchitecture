using UnityEngine;
using Valchitecture.Common.Configuration;
using Valchitecture.Modules.Skill;

namespace Valchitecture.Modules.Modes;

/// <summary>Build reach in a mode: widens <c>m_maxPlaceDistance</c> and applies the level 100 anchor limit.</summary>
internal static class Reach
{
    private const float Fallback = 5f;

    /// <summary>Length of the game's own placement and removal rays.</summary>
    internal const float GameRayLength = 50f;

    private static float _original;
    private static bool _held;

    private static int _aimFrame = -1;
    private static bool _aimAllowed;

    /// <summary>Widens the player's place distance; called every frame in a mode.</summary>
    internal static void Widen(Player player)
    {
        if (!player)
        {
            return;
        }

        if (!_held)
        {
            _original = player.m_maxPlaceDistance;
            _held = true;
        }

        float reach = CharacterRadius();

        // The game measures place distance in 3D; Allows does the exact, flat test.
        CraftingStation anchor = Mastered() ? AnchorStation() : null;

        if (anchor)
        {
            float span = anchor.GetStationBuildRange() + BuildAnchor.CeilingOf(anchor);
            reach = Mathf.Max(reach, Vector3.Distance(Eye(player), anchor.transform.position) + span);
        }

        player.m_maxPlaceDistance = reach;
    }

    internal static void Restore(Player player)
    {
        if (!_held)
        {
            return;
        }

        if (player)
        {
            player.m_maxPlaceDistance = _original;
        }

        _held = false;
    }

    /// <summary>Whether a point is within reach of the character's eye.</summary>
    internal static bool InReach(Player player, Vector3 point, Piece piece = null)
    {
        return Vector3.Distance(Eye(player), point) < Current(player, piece);
    }

    /// <summary>
    ///     Whether a point is inside the anchor station's range, or the character's reach without
    ///     one; always true below level 100 or with the camera attached.
    /// </summary>
    internal static bool Allows(Player player, Vector3 point, float extra)
    {
        if (!Mastered())
        {
            return true;
        }

        CraftingStation station = AnchorStation();

        return station
            ? BuildAnchor.Covers(station, point, extra)
            : Vector3.Distance(Eye(player), point) < CharacterRadius() + extra;
    }

    /// <summary>
    ///     Whether the point satisfies the level 100 station rule for a piece that needs one;
    ///     always true below level 100 or with the camera attached.
    /// </summary>
    internal static bool BenchHolds(Player player, Piece piece, Vector3 point)
    {
        if (!Unlocks.CanBuildFromCamera() || !ModeManager.CameraDetached || !player || !piece || !piece.m_craftingStation
            || player.PlacementCostDisabled || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench))
        {
            return true;
        }

        string name = piece.m_craftingStation.m_name;

        return BuildAnchor.Closest(player.transform.position, 0f, name) || BuildAnchor.Closest(point, 0f, name);
    }

    /// <summary>The station anchoring the detached camera, or null when the camera is past every station's margin.</summary>
    internal static CraftingStation AnchorStation()
    {
        if (!ModeManager.CameraDetached || !GameCamera.instance)
        {
            return null;
        }

        Vector3 camera = GameCamera.instance.transform.position;
        CraftingStation station = BuildAnchor.Working(camera);

        return BuildAnchor.Covers(station, camera, BuildAnchor.Margin) ? station : null;
    }

    internal static bool CharacterIsAnchor() => Mastered() && !AnchorStation();

    /// <summary>The character's reach: the game's, times the mode factor and the skill multiplier.</summary>
    internal static float CharacterRadius()
    {
        Player player = Player.m_localPlayer;
        float own = _held ? _original : player ? player.m_maxPlaceDistance : Fallback;
        return own * Cfg.PlaceDistanceFactor * Unlocks.ReachMultiplier();
    }

    /// <summary>Whether the point under the camera passes <see cref="Allows" />, cached per frame.</summary>
    internal static bool AimAllowed(Player player)
    {
        if (!Mastered() || !player)
        {
            return true;
        }

        if (_aimFrame == Time.frameCount)
        {
            return _aimAllowed;
        }

        _aimFrame = Time.frameCount;

        Transform view = GameCamera.instance.transform;

        _aimAllowed = !Physics.Raycast(view.position, view.forward, out RaycastHit hit, GameRayLength, player.m_removeRayMask)
                      || Allows(player, hit.point, 0f);

        return _aimAllowed;
    }

    /// <summary>Whether building is judged from the camera (level 100, camera detached).</summary>
    internal static bool BuildFollowsCamera() => Mastered();

    /// <summary>The reach in force, plus the piece's extra placement distance.</summary>
    internal static float Current(Player player, Piece piece = null)
    {
        float reach = player ? player.m_maxPlaceDistance : Fallback;
        return reach + (piece ? piece.m_extraPlacementDistance : 0);
    }

    internal static Vector3 Eye(Player player)
    {
        return player.m_eye ? player.m_eye.position : player.transform.position;
    }

    private static bool Mastered()
    {
        return ModeManager.CameraDetached && Unlocks.CanBuildFromCamera() && GameCamera.instance;
    }
}
