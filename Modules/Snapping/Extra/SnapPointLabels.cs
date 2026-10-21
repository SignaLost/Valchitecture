using System.Collections.Generic;
using UnityEngine;
using static Valchitecture.Modules.Snapping.Extra.SnapPointNames;

namespace Valchitecture.Modules.Snapping.Extra;

/// <summary>Names and orders all of a piece's snap points by position, with a role overriding position where given.</summary>
internal static class SnapPointLabels
{
    private const float Epsilon = 0.001f;

    private enum Group
    {
        Edge,
        Corner,
        Top,
        TopCenter,
        Bottom,
        BottomCenter,
        Side,
        SideCorner,
        End,
        Center,
        Inner
    }

    private sealed class Entry
    {
        public Transform Point;
        public Group Place;
        public bool OnTop;
        public bool OnBottom;
        public Vector3 Position;
        public bool Paired;
    }

    internal static void Arrange(GameObject prefab, Dictionary<Transform, SnapRole> roles, List<(Transform point, string name)> renamed)
    {
        List<Transform> points = prefab.GetSnapPoints();

        if (points.Count == 0)
        {
            return;
        }

        Vector3 min = new(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new(float.MinValue, float.MinValue, float.MinValue);

        foreach (Transform point in points)
        {
            min = Vector3.Min(min, point.localPosition);
            max = Vector3.Max(max, point.localPosition);
        }

        bool flat = !Spans(min.y, max.y);
        List<Entry> entries = new(points.Count);

        foreach (Transform point in points)
        {
            Vector3 p = point.localPosition;
            bool onTop = !flat && OnEnd(p.y, max.y);
            bool onBottom = !flat && OnEnd(p.y, min.y);

            roles.TryGetValue(point, out SnapRole role);
            Group place = GroupOf(p, min, max, flat, onTop || onBottom, role);

            entries.Add(new Entry
            {
                Point = point,
                Position = p,
                OnTop = onTop,
                OnBottom = onBottom,
                Place = place,
                Paired = !flat && (place is Group.Edge or Group.Corner || role == SnapRole.Side)
            });
        }

        entries.Sort(Compare);
        Name(entries, flat, renamed);

        foreach (Entry entry in entries)
        {
            entry.Point.SetAsLastSibling();
        }
    }

    private static Group GroupOf(Vector3 p, Vector3 min, Vector3 max, bool flat, bool onCap, SnapRole role)
    {
        int spanning = (Spans(min.x, max.x) ? 1 : 0) + (Spans(min.z, max.z) ? 1 : 0);
        int extremes = (Spans(min.x, max.x) && OnEnd(p.x, min.x, max.x) ? 1 : 0)
            + (Spans(min.z, max.z) && OnEnd(p.z, min.z, max.z) ? 1 : 0);

        switch (role)
        {
            case SnapRole.Side:
                return Group.Side;
            case SnapRole.End:
                return Group.End;
            case SnapRole.Top:
                return extremes == 0 ? Group.TopCenter : Group.Top;
            case SnapRole.Bottom:
                return extremes == 0 ? Group.BottomCenter : Group.Bottom;
        }

        bool center = role == SnapRole.Center;

        if (flat || onCap)
        {
            bool top = flat || OnEnd(p.y, max.y);

            if (extremes == 2)
            {
                return Group.Corner;
            }

            if (extremes == 1 && spanning == 2)
            {
                return Group.Edge;
            }

            if (extremes == 1)
            {
                return Group.Corner;
            }

            if (center)
            {
                return top ? Group.TopCenter : Group.BottomCenter;
            }

            return top ? Group.Top : Group.Bottom;
        }

        if (extremes == 2)
        {
            return Group.SideCorner;
        }

        if (extremes > 0)
        {
            return Group.Side;
        }

        return center ? Group.Center : Group.Inner;
    }

    /// <summary>By group, then top to bottom (per place for paired groups).</summary>
    private static int Compare(Entry a, Entry b)
    {
        int byGroup = a.Place.CompareTo(b.Place);

        if (byGroup != 0)
        {
            return byGroup;
        }

        if (!a.Paired)
        {
            int byHeight = b.Position.y.CompareTo(a.Position.y);

            if (byHeight != 0)
            {
                return byHeight;
            }
        }

        int byDepth = a.Position.z.CompareTo(b.Position.z);

        if (byDepth != 0)
        {
            return byDepth;
        }

        int byX = a.Position.x.CompareTo(b.Position.x);
        return byX != 0 ? byX : b.Position.y.CompareTo(a.Position.y);
    }

    /// <summary>Numbers places within each group, then names every point; single-place groups are not numbered.</summary>
    private static void Name(List<Entry> entries, bool flat, List<(Transform point, string name)> renamed)
    {
        Dictionary<Group, int> counts = new();
        int[] numbers = new int[entries.Count];
        Vector3 lastPlace = new(float.NaN, 0f, float.NaN);
        int number = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];

            // Top and bottom of one edge or corner share a number.
            if (!entry.Paired || !SamePlace(entry.Position, lastPlace))
            {
                lastPlace = entry.Paired ? entry.Position : new Vector3(float.NaN, 0f, float.NaN);
                counts.TryGetValue(entry.Place, out number);
                counts[entry.Place] = ++number;
            }

            numbers[i] = number;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            string label = counts[entry.Place] > 1 ? " " + numbers[i] : string.Empty;

            renamed.Add((entry.Point, entry.Point.name));
            entry.Point.name = NameFor(entry, flat, label);
        }
    }

    private static string NameFor(Entry entry, bool flat, string n)
    {
        string cap = entry.OnTop ? TOP : BOTTOM;

        return entry.Place switch
        {
            Group.Edge => entry.Paired ? $"{EDGE}{n} {cap}" : $"{EDGE}{n}",
            Group.Corner => entry.Paired ? $"{CORNER}{n} {cap}" : $"{CORNER}{n}",
            Group.Top => $"{TOP}{n}",
            Group.TopCenter => flat ? $"{CENTER}{n}" : $"{TOP} {CENTER}{n}",
            Group.Bottom => $"{BOTTOM}{n}",
            Group.BottomCenter => $"{BOTTOM} {CENTER}{n}",
            Group.Side => entry.Paired && (entry.OnTop || entry.OnBottom) ? $"{SIDE}{n} {cap}" : $"{SIDE}{n}",
            Group.SideCorner => $"{SIDE} {CORNER}{n}",
            Group.End => $"{END}{n}",
            Group.Center => $"{CENTER}{n}",
            _ => $"{INNER}{n}"
        };
    }

    private static bool SamePlace(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(a.x - b.x) < Epsilon && Mathf.Abs(a.z - b.z) < Epsilon;
    }

    private static bool Spans(float min, float max)
    {
        return max - min >= Epsilon;
    }

    private static bool OnEnd(float value, float end)
    {
        return Mathf.Abs(value - end) < Epsilon;
    }

    private static bool OnEnd(float value, float min, float max)
    {
        return OnEnd(value, min) || OnEnd(value, max);
    }
}
