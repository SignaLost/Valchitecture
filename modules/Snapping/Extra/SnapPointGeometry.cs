using System.Collections.Generic;
using UnityEngine;
using Valchitecture.Common;

namespace Valchitecture.Modules.Snapping.Extra;

/// <summary>
///     Computes unnamed extra snap points from a piece's own, in local space with Y up.
///     It classifies the piece's points as a line, triangle, rectangle, roof or box, then adds centers, edge midpoints
///     and face grids for that shape, sizing poles and beams from their colliders; duplicates of existing points are
///     dropped, with any role they would have had passed on to the existing point.
/// </summary>
internal static class SnapPointGeometry
{
    /// <summary>Points nearer than this are one point.</summary>
    private const float Merge = 0.01f;

    /// <summary>Smallest grid spacing used, in meters.</summary>
    private const float MinGridSpacing = 0.5f;

    /// <summary>Most grid steps along one side of a face.</summary>
    private const int MaxGridSteps = 16;

    private static readonly List<Collider> _colliders = new();

    /// <summary>The shape a piece's own snap points form.</summary>
    internal enum Shape
    {
        /// <summary>Not recognized; the caller decides.</summary>
        Unknown,

        /// <summary>Recognized and given no points.</summary>
        Skip,

        Line,
        Triangle,
        Rectangle,
        Roof,
        Box
    }

    internal static Shape Classify(List<Transform> points, bool isRoof)
    {
        Bounds bounds = Bound(points);

        switch (points.Count)
        {
            case 0:
                return Shape.Unknown;
            case 1:
                return Shape.Skip;
            case 2:
                return bounds.size.sqrMagnitude > Merge * Merge ? Shape.Line : Shape.Unknown;
            case 3:
                return points.Collinear() ? Shape.Unknown : Shape.Triangle;
            case 4:
                return IsRectangle(points) ? Shape.Rectangle : Shape.Unknown;
            case 5:
                return IsCross(points, bounds) ? Shape.Skip : Shape.Unknown;
            case 6:
                return isRoof && bounds.size.y > Merge
                    && Ridge(points, bounds, out _, out _)
                        ? Shape.Roof
                        : Shape.Unknown;
            case 8:
                return IsBox(points, bounds) ? Shape.Box : Shape.Unknown;
            default:
                return Shape.Unknown;
        }
    }

    /// <summary>The points to add; roles for the piece's own points go to <paramref name="existingRoles" />.</summary>
    internal static SnapPointSpec[] PointsFor(GameObject prefab, Shape shape, List<Transform> points, Dictionary<Transform, SnapRole> existingRoles)
    {
        Bounds bounds = Bound(points);
        Points result = new(points, existingRoles);

        switch (shape)
        {
            case Shape.Line:
                AddThinBox(result, prefab, points.Count == 2 ? bounds : CenterLine(bounds));
                break;

            case Shape.Triangle:
                result.Add(points.Centroid(), SnapRole.Center);
                AddPairMidpoints(result, points);
                break;

            case Shape.Rectangle:
                AddFace(result, points);
                break;

            case Shape.Roof:
                AddRoof(result, points, bounds);
                break;

            case Shape.Box:
                AddBox(result, bounds);
                break;
        }

        return result.ToArray();
    }

    /// <summary>The center, a grid, then the midpoint of every corner pair; the center must be added first.</summary>
    private static void AddFace(Points result, List<Transform> points)
    {
        Vector3 corner = points[0].localPosition;
        int far = Farthest(points, corner);
        int sideA = far == 1 ? 2 : 1;

        result.Add((corner + points[far].localPosition) * 0.5f, SnapRole.Center);

        AddGrid(result, corner,
            points[sideA].localPosition - corner,
            points[6 - far - sideA].localPosition - corner);

        AddPairMidpoints(result, points);
    }

    private static void AddPairMidpoints(Points result, List<Transform> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                result.Add((points[i].localPosition + points[j].localPosition) * 0.5f);
            }
        }
    }

    /// <summary>The midpoints of the twelve edges, and a grid on each of the six faces; never the inside center.</summary>
    private static void AddBox(Points result, Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        for (int axis = 0; axis < 3; axis++)
        {
            int a = (axis + 1) % 3;
            int b = (axis + 2) % 3;

            for (int corner = 0; corner < 4; corner++)
            {
                Vector3 edge = bounds.center;
                edge[a] = (corner & 1) == 0 ? min[a] : max[a];
                edge[b] = (corner & 2) == 0 ? min[b] : max[b];
                result.Add(edge);
            }

            Vector3 sideA = Vector3.zero;
            Vector3 sideB = Vector3.zero;
            sideA[a] = max[a] - min[a];
            sideB[b] = max[b] - min[b];

            foreach (float offset in new[] { min[axis], max[axis] })
            {
                Vector3 origin = min;
                origin[axis] = offset;

                Vector3 face = bounds.center;
                face[axis] = offset;

                result.Add(face, SnapRole.Center);
                AddGrid(result, origin, sideA, sideB);
            }
        }
    }

    /// <summary>
    ///     Poles and beams, with length from the snap points and cross-section from the colliders:
    ///     both ends, and both ends and the middle of each of the four side faces.
    /// </summary>
    private static void AddThinBox(Points result, GameObject prefab, Bounds axis)
    {
        if (!AxisOf(axis.size, out int along))
        {
            result.Add(axis.center, SnapRole.Center);
            return;
        }

        if (!SolidExtent(prefab, out Bounds solid))
        {
            Log.LogInfo(
                $"{prefab.name} spans a line but has no collider to measure, so only its middle is added.",
                Log.InfoLevel.Medium);
            result.Add(axis.center, SnapRole.Center);
            return;
        }

        Vector3 center = solid.center;
        Vector3 size = solid.size;
        center[along] = axis.center[along];
        size[along] = axis.size[along];

        if (size.x < Merge || size.y < Merge || size.z < Merge)
        {
            result.Add(axis.center, SnapRole.Center);
            return;
        }

        Bounds box = new(center, size);
        Vector3 min = box.min;
        Vector3 max = box.max;

        // Vertical pieces' ends are named top and bottom by position.
        SnapRole endRole = along == 1 ? SnapRole.None : SnapRole.End;

        foreach (float end in new[] { min[along], max[along] })
        {
            Vector3 p = center;
            p[along] = end;
            result.Add(p, endRole);
        }

        for (int face = 0; face < 4; face++)
        {
            int normal = (face & 2) == 0 ? (along + 1) % 3 : (along + 2) % 3;
            bool high = (face & 1) != 0;

            SnapRole role = normal != 1 ? SnapRole.Side : high ? SnapRole.Top : SnapRole.Bottom;

            foreach (float station in new[] { min[along], center[along], max[along] })
            {
                Vector3 p = center;
                p[normal] = high ? max[normal] : min[normal];
                p[along] = station;
                result.Add(p, role);
            }
        }
    }

    /// <summary>Largest beam cross-section, and the minimum length as a multiple of it.</summary>
    private const float BeamThickest = 0.75f;
    private const float BeamSlenderness = 2.5f;

    /// <summary>Whether three or more points mark a beam: all at the two ends of a long axis, on a small, roughly square cross-section.</summary>
    internal static bool IsEndPointBeam(GameObject prefab, List<Transform> points)
    {
        return points.Count >= 3 && BeamTest(prefab, points, null);
    }

    /// <summary>The beam test, writing its steps to <paramref name="why" /> when given.</summary>
    internal static bool BeamTest(GameObject prefab, List<Transform> points, List<string> why)
    {
        if (points.Count < 2)
        {
            why?.Add($"Result: Only {points.Count} Point{(points.Count == 1 ? string.Empty : "s")}");
            return false;
        }

        Bounds bounds = Bound(points);
        int along = Longest(bounds.size);
        bool hasSolid = SolidExtent(prefab, out Bounds solid);
        float a = solid.size[(along + 1) % 3];
        float b = solid.size[(along + 2) % 3];
        float thick = Mathf.Max(a, b);

        why?.Add($"Points Span: {bounds.size}");
        why?.Add(hasSolid ? $"Colliders Span: {solid.size} around {solid.center}" : "Colliders Span: None");
        why?.Add($"Longest Axis: {"XYZ"[along]}");

        if (hasSolid)
        {
            why?.Add($"Cross-section: {a:0.###} by {b:0.###}");
        }

        if (points.Count == 2)
        {
            why?.Add("Result: Line [two points need no test]");
            return true;
        }

        foreach (Transform point in points)
        {
            float at = point.localPosition[along];

            if (Mathf.Abs(at - bounds.min[along]) > Merge && Mathf.Abs(at - bounds.max[along]) > Merge)
            {
                why?.Add($"Result: Not A Beam [{point.localPosition} is at neither end]");
                return false;
            }
        }

        if (!hasSolid)
        {
            why?.Add("Result: Not A Beam [no collider to measure]");
            return false;
        }

        bool pass = thick > Merge
            && thick <= BeamThickest
            && Mathf.Min(a, b) >= thick * 0.5f
            && bounds.size[along] >= thick * BeamSlenderness;

        why?.Add("Result: " + (thick > BeamThickest ? $"Too Thick [{thick:0.###} > {BeamThickest}]"
            : Mathf.Min(a, b) < thick * 0.5f ? $"Too Flat [{Mathf.Min(a, b):0.###} < {thick * 0.5f:0.###}]"
            : bounds.size[along] < thick * BeamSlenderness ? $"Too Short [{bounds.size[along]:0.###} < {thick * BeamSlenderness:0.###}]"
            : "Passes"));

        return pass;
    }

    private static int Longest(Vector3 size)
    {
        return size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
    }

    /// <summary>The bounds narrowed to their center line along the longest axis.</summary>
    private static Bounds CenterLine(Bounds bounds)
    {
        int along = Longest(bounds.size);
        Vector3 size = Vector3.zero;
        size[along] = bounds.size[along];
        return new Bounds(bounds.center, size);
    }

    /// <summary>The single axis a span runs along; false if it runs along more than one.</summary>
    private static bool AxisOf(Vector3 size, out int axis)
    {
        axis = -1;

        for (int i = 0; i < 3; i++)
        {
            if (size[i] <= Merge)
            {
                continue;
            }

            if (axis >= 0)
            {
                return false;
            }

            axis = i;
        }

        return axis >= 0;
    }

    /// <summary>The piece's collider extent in local space; triggers count only when nothing else exists.</summary>
    internal static bool SolidExtent(GameObject prefab, out Bounds extent)
    {
        return Extent(prefab, false, out extent) || Extent(prefab, true, out extent);
    }

    private static bool Extent(GameObject prefab, bool triggers, out Bounds extent)
    {
        extent = new Bounds();
        _colliders.Clear();
        prefab.GetComponentsInChildren(true, _colliders);
        bool found = false;

        foreach (Collider collider in _colliders)
        {
            if (!collider || collider.isTrigger != triggers || !Box(collider, out Vector3 offset, out Vector3 half))
            {
                continue;
            }

            Bounds local = LocalBounds(prefab.transform, collider.transform, offset, half);

            if (found)
            {
                extent.Encapsulate(local);
            }
            else
            {
                extent = local;
                found = true;
            }
        }

        _colliders.Clear();
        return found;
    }

    /// <summary>A collider as a box in its own space; a sphere as its bounding cube.</summary>
    private static bool Box(Collider collider, out Vector3 offset, out Vector3 half)
    {
        switch (collider)
        {
            case BoxCollider box:
                offset = box.center;
                half = box.size * 0.5f;
                return true;
            case SphereCollider sphere:
                offset = sphere.center;
                half = Vector3.one * sphere.radius;
                return true;
            case MeshCollider mesh when mesh.sharedMesh:
                offset = mesh.sharedMesh.bounds.center;
                half = mesh.sharedMesh.bounds.extents;
                return true;
            default:
                offset = Vector3.zero;
                half = Vector3.zero;
                return false;
        }
    }

    /// <summary>A box's extent in the root's local space, via its corners.</summary>
    private static Bounds LocalBounds(Transform root, Transform from, Vector3 offset, Vector3 half)
    {
        Bounds bounds = new(root.InverseTransformPoint(from.TransformPoint(offset)), Vector3.zero);

        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 p = offset + new Vector3(
                (corner & 1) == 0 ? -half.x : half.x,
                (corner & 2) == 0 ? -half.y : half.y,
                (corner & 4) == 0 ? -half.z : half.z);

            bounds.Encapsulate(root.InverseTransformPoint(from.TransformPoint(p)));
        }

        return bounds;
    }

    /// <summary>The ridge center, and the midpoint of each eave below it.</summary>
    private static void AddRoof(Points result, List<Transform> points, Bounds bounds)
    {
        if (!Ridge(points, bounds, out Vector3 first, out Vector3 second))
        {
            return;
        }

        int along = Near(first.x, second.x) ? 2 : 0;
        int across = 2 - along;

        result.Add((first + second) * 0.5f, SnapRole.Center);

        foreach (float edge in new[] { bounds.min[across], bounds.max[across] })
        {
            Vector3 eave = bounds.center;
            eave.y = bounds.min.y;
            eave[across] = edge;
            result.Add(eave);
        }
    }

    /// <summary>Points at every grid crossing and cell center across a rectangle, about <see cref="SnapConfig.GridSpacing" /> apart; a spacing of 0 makes the face one cell.</summary>
    private static void AddGrid(Points result, Vector3 corner, Vector3 sideA, Vector3 sideB)
    {
        float spacing = SnapConfig.GridSpacing.Value;
        int stepsA = GridSteps(sideA.magnitude, spacing);
        int stepsB = GridSteps(sideB.magnitude, spacing);

        for (int i = 0; i <= stepsA; i++)
        {
            for (int j = 0; j <= stepsB; j++)
            {
                bool cornerA = i == 0 || i == stepsA;
                bool cornerB = j == 0 || j == stepsB;

                if (!(cornerA && cornerB))
                {
                    result.Add(corner + sideA * ((float)i / stepsA) + sideB * ((float)j / stepsB));
                }

                if (i < stepsA && j < stepsB)
                {
                    result.Add(corner + sideA * ((i + 0.5f) / stepsA) + sideB * ((j + 0.5f) / stepsB), SnapRole.Center);
                }
            }
        }
    }

    /// <summary>Whole steps along a side, with spacing floored and steps capped.</summary>
    private static int GridSteps(float length, float spacing)
    {
        if (spacing <= 0f)
        {
            return 1;
        }

        int steps = Mathf.RoundToInt(length / Mathf.Max(spacing, MinGridSpacing));
        return Mathf.Clamp(steps, 1, MaxGridSteps);
    }

    /// <summary>Whether four points form a rectangle: equal diagonals bisecting each other.</summary>
    private static bool IsRectangle(List<Transform> points)
    {
        if (!points.Coplanar())
        {
            return false;
        }

        Vector3 corner = points[0].localPosition;
        int far = Farthest(points, corner);
        Vector3 a = points[far == 1 ? 2 : 1].localPosition;
        Vector3 b = points[6 - far - (far == 1 ? 2 : 1)].localPosition;

        float across = (points[far].localPosition - corner).magnitude;

        return across > Merge
            && Mathf.Abs(across - (b - a).magnitude) < Merge
            && (corner + points[far].localPosition - a - b).magnitude < Merge;
    }

    private static bool IsCross(List<Transform> points, Bounds bounds)
    {
        int corners = 0;
        int middles = 0;

        foreach (Transform point in points)
        {
            if (OnCorner(point.localPosition, bounds, out _))
            {
                corners++;
            }
            else if ((point.localPosition - bounds.center).magnitude < Merge)
            {
                middles++;
            }
        }

        return corners == 4 && middles == 1;
    }

    /// <summary>Whether the points are four base points and two ridge points centered above them; returns the ridge pair.</summary>
    private static bool Ridge(List<Transform> points, Bounds bounds, out Vector3 first, out Vector3 second)
    {
        first = Vector3.zero;
        second = Vector3.zero;
        int found = 0;

        foreach (Transform point in points)
        {
            Vector3 p = point.localPosition;

            if (Near(p.y, bounds.max.y))
            {
                if (found == 0)
                {
                    first = p;
                }
                else if (found == 1)
                {
                    second = p;
                }

                found++;
            }
            else if (!Near(p.y, bounds.min.y))
            {
                return false;
            }
        }

        if (found != 2)
        {
            return false;
        }

        int along = Near(first.x, second.x) ? 2 : 0;
        int across = 2 - along;

        return !Near(first[along], second[along])
            && Near(first[across], bounds.center[across])
            && Near(second[across], bounds.center[across]);
    }

    /// <summary>Eight points on the eight corners of their bounding box.</summary>
    private static bool IsBox(List<Transform> points, Bounds bounds)
    {
        Vector3 size = bounds.size;

        if (size.x < Merge || size.y < Merge || size.z < Merge)
        {
            return false;
        }

        int filled = 0;

        foreach (Transform point in points)
        {
            if (!OnCorner(point.localPosition, bounds, out int corner))
            {
                return false;
            }

            filled |= 1 << corner;
        }

        return filled == 0xFF;
    }

    private static bool OnCorner(Vector3 p, Bounds bounds, out int corner)
    {
        corner = 0;

        for (int axis = 0; axis < 3; axis++)
        {
            bool high = Near(p[axis], bounds.max[axis]);

            if (!high && !Near(p[axis], bounds.min[axis]))
            {
                return false;
            }

            if (high)
            {
                corner |= 1 << axis;
            }
        }

        return true;
    }

    /// <summary>The points' bounding box; empty at the origin for none.</summary>
    private static Bounds Bound(List<Transform> points)
    {
        if (points.Count == 0)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        Bounds bounds = new(points[0].localPosition, Vector3.zero);

        for (int i = 1; i < points.Count; i++)
        {
            bounds.Encapsulate(points[i].localPosition);
        }

        return bounds;
    }

    private static int Farthest(List<Transform> points, Vector3 from)
    {
        int farthest = 1;

        for (int i = 2; i < points.Count; i++)
        {
            if ((points[i].localPosition - from).sqrMagnitude
                > (points[farthest].localPosition - from).sqrMagnitude)
            {
                farthest = i;
            }
        }

        return farthest;
    }

    private static bool Near(float a, float b)
    {
        return Mathf.Abs(a - b) < Merge;
    }

    /// <summary>Collects points, dropping duplicates by rounded position.</summary>
    private sealed class Points
    {
        private readonly List<SnapPointSpec> _points = new();
        private readonly HashSet<(int, int, int)> _taken = new();
        private readonly Dictionary<(int, int, int), Transform> _existing = new();
        private readonly Dictionary<Transform, SnapRole> _existingRoles;

        /// <summary>The piece's own points count as held; a role for one of them is recorded in <c>existingRoles</c> instead.</summary>
        internal Points(List<Transform> existing, Dictionary<Transform, SnapRole> existingRoles)
        {
            _existingRoles = existingRoles;

            foreach (Transform point in existing)
            {
                if (point)
                {
                    (int, int, int) key = Key(point.localPosition);
                    _taken.Add(key);
                    _existing[key] = point;
                }
            }
        }

        internal void Add(Vector3 position, SnapRole role = SnapRole.None)
        {
            (int, int, int) key = Key(position);

            if (_taken.Add(key))
            {
                _points.Add(new SnapPointSpec(position, role));
            }
            else if (role != SnapRole.None && _existing.TryGetValue(key, out Transform held))
            {
                _existingRoles[held] = role;
            }
        }

        internal SnapPointSpec[] ToArray()
        {
            return _points.ToArray();
        }

        private static (int, int, int) Key(Vector3 position)
        {
            return (
                Mathf.RoundToInt(position.x / Merge),
                Mathf.RoundToInt(position.y / Merge),
                Mathf.RoundToInt(position.z / Merge));
        }
    }
}
