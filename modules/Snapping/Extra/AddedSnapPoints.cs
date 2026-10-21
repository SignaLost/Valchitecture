using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Valchitecture.Common;

namespace Valchitecture.Modules.Snapping.Extra;

/// <summary>
///     Tracks every snap point added to a prefab, and every name and child order changed, for restoring.
///     Added points are inactive tagged children marked with <see cref="AddedSnapPoint" />; a per prefab mask records
///     which points, in child order, were added.
/// </summary>
internal static class AddedSnapPoints
{
    private static readonly List<GameObject> _created = new();
    private static readonly List<(Transform Point, string Original)> _renamed = new();
    private static readonly List<(Transform Point, int Index)> _moved = new();

    /// <summary>Per prefab hash, which of its snap points, in child order, were added.</summary>
    private static readonly Dictionary<int, bool[]> _masks = new();

    private static readonly HashSet<Transform> _createdNow = new();

    /// <summary>Time spent naming and ordering.</summary>
    internal static readonly Stopwatch NamingTime = new();

    internal static int Count => _created.Count;

    /// <summary>How many build passes have started; instances made before the latest one may keep an older layout.</summary>
    internal static int Passes { get; private set; }

    /// <summary>Which of a prefab's snap points, in child order, were added; null when none were.</summary>
    internal static bool[] MaskOf(int prefabHash)
    {
        return _masks.TryGetValue(prefabHash, out bool[] mask) ? mask : null;
    }

    /// <summary>Adds the points to the prefab, then names and orders all its points, its own included, with <paramref name="roles" /> for the existing ones.</summary>
    internal static void Add(GameObject prefab, SnapPointSpec[] specs, Dictionary<Transform, SnapRole> roles)
    {
        Dictionary<Transform, SnapRole> allRoles = new(roles);

        foreach (Transform own in prefab.GetSnapPoints())
        {
            _moved.Add((own, own.GetSiblingIndex()));
        }

        _createdNow.Clear();

        foreach (SnapPointSpec spec in specs)
        {
            GameObject point = new(SnapPointNames.DEFAULT_NAME) { tag = SnapPointNames.TAG };
            point.AddComponent<AddedSnapPoint>();
            point.transform.SetParent(prefab.transform, false);
            point.transform.localPosition = spec.LocalPosition;

            // Piece.GetSnapPoints finds points by tag alone.
            point.SetActive(false);
            _created.Add(point);
            _createdNow.Add(point.transform);

            if (spec.Role != SnapRole.None)
            {
                allRoles[point.transform] = spec.Role;
            }
        }

        NamingTime.Start();

        try
        {
            SnapPointLabels.Arrange(prefab, allRoles, _renamed);
        }
        finally
        {
            NamingTime.Stop();

            if (_createdNow.Count > 0)
            {
                _masks[PrefabKey.Of(prefab)] = Mask(prefab);
            }
        }
    }

    /// <summary>Flags, per snap point in child order, whether it was created by this pass.</summary>
    private static bool[] Mask(GameObject prefab)
    {
        List<Transform> points = prefab.GetSnapPoints();
        bool[] mask = new bool[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            mask[i] = _createdNow.Contains(points[i]);
        }

        return mask;
    }

    /// <summary>Restores every name and child order, and removes every added point.</summary>
    internal static void RemoveAll()
    {
        Passes++;
        _masks.Clear();

        foreach ((Transform point, string original) in _renamed)
        {
            if (point)
            {
                point.name = original;
            }
        }

        foreach (GameObject point in _created)
        {
            if (point)
            {
                Object.DestroyImmediate(point);
            }
        }

        // Must be restored in ascending order.
        _moved.Sort((a, b) => a.Index.CompareTo(b.Index));

        foreach ((Transform point, int index) in _moved)
        {
            if (point)
            {
                point.SetSiblingIndex(index);
            }
        }

        _renamed.Clear();
        _created.Clear();
        _moved.Clear();
    }
}
