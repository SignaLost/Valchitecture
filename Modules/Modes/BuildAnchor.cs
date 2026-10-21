using System.Collections.Generic;
using UnityEngine;
using Valchitecture.Modules.Skill;

namespace Valchitecture.Modules.Modes;
/// <summary>Station lookups for the build camera, and the range rings drawn on the ground.</summary>
internal static class BuildAnchor
{
    /// <summary>Distance past a station's build range that still counts for the camera.</summary>
    internal const float Margin = 5f;

    /// <summary>Camera height limit above a station, as a multiple of its build range.</summary>
    private const float CeilingFactor = 1.5f;

    private const string WorkbenchPrefab = "piece_workbench";

    /// <summary>Minimum dash count for the character ring.</summary>
    private const int MinSegments = 8;

    private static readonly Color CharacterRingColor = new(0.3f, 1f, 0.4f);
    private static readonly Color OutOfBoundsColor = new(1f, 0.3f, 0.3f);
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int TintProperty = Shader.PropertyToID("_TintColor");
    private static readonly List<Renderer> _renderers = new();

    /// <summary>Interval between pokes of the station marker, which hides 0.5 s after each.</summary>
    private const float MarkerPokeInterval = 0.25f;

    private static CraftingStation _bound;

    private static GameObject _characterRing;
    private static CircleProjector _characterRingCircle;
    private static MaterialPropertyBlock _tint;
    private static int _patternSegments;
    private static float _patternRadius;
    private static CraftingStation _tintedStation;

    private static CraftingStation _pokedStation;
    private static float _nextPoke;

    private static readonly TintState _stationTint = new();
    private static readonly TintState _characterTint = new();

    private static int _workingFrame = -1;
    private static int _workingStations;
    private static Vector3 _workingFor;
    private static CraftingStation _working;

    private static int _availableFrame = -1;
    private static int _availableStations;
    private static Vector3 _availableFor;
    private static Vector3 _availableFrom;
    private static bool _available;

    /// <summary>What a ring's segments were last tinted with.</summary>
    private sealed class TintState
    {
        public int Count = -1;
        public GameObject First;
        public Color Color;
    }

    /// <summary>True while this class calls ShowAreaMarker.</summary>
    internal static bool ShowingOwnRing { get; private set; }

    /// <summary>Binds the station covering the point, else the nearest.</summary>
    internal static void Bind(Vector3 from)
    {
        CraftingStation covering = Closest(from, 0f);
        _bound = covering ? covering : Closest(from);
        Forget();
    }

    internal static void Unbind()
    {
        _bound = null;
        _pokedStation = null;
        Forget();
        ReleaseCharacterRing();
        UntintStation();
    }

    /// <summary>Drops the per-frame station lookups.</summary>
    private static void Forget()
    {
        _workingFrame = -1;
        _availableFrame = -1;
    }

    /// <summary>The station the camera works from: the bound one, or from level 75 the nearest to the camera; cached per frame and position.</summary>
    internal static CraftingStation Working(Vector3 cameraPosition)
    {
        int stations = CraftingStation.m_allStations.Count;

        if (_workingFrame == Time.frameCount && _workingStations == stations && _workingFor == cameraPosition)
        {
            return _working;
        }

        _workingFrame = Time.frameCount;
        _workingStations = stations;
        _workingFor = cameraPosition;
        _working = FindWorking(cameraPosition);
        return _working;
    }

    private static CraftingStation FindWorking(Vector3 cameraPosition)
    {
        if (Unlocks.CanHopStations())
        {
            CraftingStation station = Closest(cameraPosition, 0f);

            if (!station)
            {
                station = Closest(cameraPosition, Margin);
            }

            return station ? station : Closest(cameraPosition);
        }

        if (!_bound)
        {
            _bound = Closest(cameraPosition);
        }

        return _bound;
    }

    /// <summary>Whether a station's build range plus <paramref name="beyond" /> covers the point, measured flat.</summary>
    internal static bool Covers(CraftingStation station, Vector3 point, float beyond = 0f)
    {
        return station && Utils.DistanceXZ(station.transform.position, point) < station.GetStationBuildRange() + beyond;
    }

    /// <summary>Nearest station whose build range plus <paramref name="beyond" /> covers the point, optionally filtered by name.</summary>
    internal static CraftingStation Closest(Vector3 point, float beyond = float.PositiveInfinity, string name = null)
    {
        CraftingStation closest = null;
        float best = float.MaxValue;

        foreach (CraftingStation station in CraftingStation.m_allStations)
        {
            if (!station || (name != null && station.m_name != name))
            {
                continue;
            }

            float distance = Utils.DistanceXZ(station.transform.position, point);

            if (distance < best && distance < station.GetStationBuildRange() + beyond)
            {
                best = distance;
                closest = station;
            }
        }

        return closest;
    }

    /// <summary>Whether the build camera has a station to work from; cached per frame and camera position.</summary>
    internal static bool StationAvailable(Player player, Vector3 cameraPosition)
    {
        if (!player)
        {
            return false;
        }

        int stations = CraftingStation.m_allStations.Count;
        Vector3 character = player.transform.position;

        if (_availableFrame == Time.frameCount && _availableStations == stations && _availableFor == cameraPosition && _availableFrom == character)
        {
            return _available;
        }

        _availableFrame = Time.frameCount;
        _availableStations = stations;
        _availableFor = cameraPosition;
        _availableFrom = character;
        _available = FindAvailable(character, cameraPosition);
        return _available;
    }

    private static bool FindAvailable(Vector3 character, Vector3 cameraPosition)
    {
        if (Closest(character, 0f))
        {
            return true;
        }

        if (!Unlocks.CanHopStations())
        {
            return false;
        }

        return Closest(character, Margin) || Covers(Working(cameraPosition), cameraPosition, Margin);
    }

    internal static float LeashOf(CraftingStation station)
    {
        return station ? station.GetStationBuildRange() + Margin : 0f;
    }

    internal static float CeilingOf(CraftingStation station)
    {
        return station ? station.GetStationBuildRange() * CeilingFactor : 0f;
    }

    /// <summary>Draws the range rings in force for the current camera state and level.</summary>
    internal static void ShowRange(Vector3 from)
    {
        CraftingStation station = Working(from);

        bool roaming = ModeManager.CameraDetached;
        bool benchGoverns = !roaming || (Reach.BuildFollowsCamera() && !Reach.CharacterIsAnchor());

        if (benchGoverns)
        {
            HideCharacterRing();
        }
        else
        {
            ShowCharacterRing(station);
        }

        if (station && (benchGoverns || !Reach.BuildFollowsCamera()))
        {
            ShowStationRing(station);
        }
        else
        {
            UntintStation();
        }
    }

    /// <summary>
    ///     Shows the station's own marker, which hides 0.5 s after each call; below level 100 it is
    ///     tinted red unless the character stands inside it.
    /// </summary>
    private static void ShowStationRing(CraftingStation station)
    {
        if (!ReferenceEquals(station, _pokedStation) || Time.time >= _nextPoke)
        {
            _pokedStation = station;
            _nextPoke = Time.time + MarkerPokeInterval;

            ShowingOwnRing = true;
            station.ShowAreaMarker();
            ShowingOwnRing = false;
        }

        Player player = Player.m_localPlayer;
        bool home = !player || Covers(station, player.transform.position);

        if (Unlocks.CanBuildFromCamera() || home)
        {
            UntintStation();
            return;
        }

        if (_tintedStation != station)
        {
            UntintStation();
            _tintedStation = station;
        }

        Tint(station.m_areaMarker, station.m_areaMarkerCircle, OutOfBoundsColor, _stationTint);
    }

    private static void UntintStation()
    {
        if (_tintedStation && _tintedStation.m_areaMarker)
        {
            SetBlock(_tintedStation.m_areaMarker, null);
        }

        _tintedStation = null;
        _stationTint.Count = -1;
    }

    private static void ShowCharacterRing(CraftingStation source)
    {
        Player player = Player.m_localPlayer;

        if (!player)
        {
            HideCharacterRing();
            return;
        }

        if (!_characterRingCircle)
        {
            Build(source);
        }

        if (!_characterRingCircle)
        {
            return;
        }

        float radius = Reach.CharacterRadius();
        _characterRingCircle.m_radius = radius;
        _characterRingCircle.m_nrOfSegments = SegmentsFor(radius);
        _characterRing.transform.position = player.transform.position;
        _characterRing.SetActive(true);

        Tint(_characterRing, _characterRingCircle, CharacterRingColor, _characterTint);
    }

    /// <summary>Builds the character ring from a station marker's settings.</summary>
    private static void Build(CraftingStation source)
    {
        CircleProjector pattern = MarkerOf(source);

        if (!pattern && ZNetScene.instance)
        {
            GameObject workbench = ZNetScene.instance.GetPrefab(WorkbenchPrefab);
            pattern = MarkerOf(workbench ? workbench.GetComponent<CraftingStation>() : null);
        }

        if (!pattern || !pattern.m_prefab)
        {
            return;
        }

        _patternSegments = pattern.m_nrOfSegments;
        _patternRadius = pattern.m_radius;

        _characterRing = new GameObject("ValchitectureCharacterRange");
        _characterRingCircle = _characterRing.AddComponent<CircleProjector>();
        _characterRingCircle.m_prefab = pattern.m_prefab;
        _characterRingCircle.m_nrOfSegments = pattern.m_nrOfSegments;
        _characterRingCircle.m_mask = pattern.m_mask;
        _characterRingCircle.m_speed = pattern.m_speed;
        _characterRingCircle.m_turns = pattern.m_turns;
    }

    /// <summary>Dash count scaled with circumference.</summary>
    private static int SegmentsFor(float radius)
    {
        if (_patternRadius <= 0f)
        {
            return _patternSegments;
        }

        int segments = Mathf.RoundToInt(_patternSegments * radius / _patternRadius);
        return Mathf.Clamp(segments, MinSegments, _patternSegments);
    }

    private static CircleProjector MarkerOf(CraftingStation station)
    {
        return station && station.m_areaMarker ? station.m_areaMarker.GetComponent<CircleProjector>() : null;
    }

    /// <summary>Tints ring segments with a property block, leaving the shared material untouched; skipped while the segments and color are unchanged.</summary>
    private static void Tint(GameObject ring, CircleProjector circle, Color color, TintState state)
    {
        if (!ring)
        {
            return;
        }

        // The projector replaces every segment when its count changes.
        List<GameObject> segments = circle ? circle.m_segments : null;
        int count = segments?.Count ?? -1;
        GameObject first = count > 0 ? segments[0] : null;

        if (segments != null && state.Count == count && ReferenceEquals(state.First, first) && state.Color == color)
        {
            return;
        }

        state.Count = count;
        state.First = first;
        state.Color = color;

        _tint ??= new MaterialPropertyBlock();
        _tint.SetColor(ColorProperty, color);
        _tint.SetColor(TintProperty, color);
        SetBlock(ring, _tint);
    }

    private static void SetBlock(GameObject ring, MaterialPropertyBlock block)
    {
        ring.GetComponentsInChildren(true, _renderers);

        foreach (Renderer part in _renderers)
        {
            if (part)
            {
                part.SetPropertyBlock(block);
            }
        }

        _renderers.Clear();
    }

    private static void HideCharacterRing()
    {
        if (_characterRing)
        {
            _characterRing.SetActive(false);
        }
    }

    private static void ReleaseCharacterRing()
    {
        if (_characterRing)
        {
            Object.Destroy(_characterRing);
        }

        _characterRing = null;
        _characterRingCircle = null;
        _characterTint.Count = -1;
    }
}
