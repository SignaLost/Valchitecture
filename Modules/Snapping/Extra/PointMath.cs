using System.Collections.Generic;
using UnityEngine;

namespace Valchitecture.Modules.Snapping.Extra;

/// <summary>Tests on a piece's snap points, in its local space.</summary>
internal static class PointMath
{
    /// <summary>Tolerance in meters for hand-placed snap point positions.</summary>
    private const float Hair = 0.0001f;

    /// <summary>Whether all points lie on the plane through the first three; true for fewer than four.</summary>
    internal static bool Coplanar(this List<Transform> points)
    {
        if (points.Count < 4)
        {
            return true;
        }

        Vector3 origin = points[0].localPosition;
        Vector3 normal = Vector3.Cross(points[1].localPosition - origin, points[2].localPosition - origin);

        if (normal.sqrMagnitude < Hair * Hair)
        {
            return true;
        }

        normal.Normalize();

        for (int i = 3; i < points.Count; i++)
        {
            if (Mathf.Abs(Vector3.Dot(points[i].localPosition - origin, normal)) > Hair)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Whether all points lie on the line through the first two.</summary>
    internal static bool Collinear(this List<Transform> points)
    {
        if (points.Count < 3)
        {
            return true;
        }

        Vector3 origin = points[0].localPosition;
        Vector3 direction = points[1].localPosition - origin;

        if (direction.sqrMagnitude < Hair * Hair)
        {
            return true;
        }

        direction.Normalize();

        for (int i = 2; i < points.Count; i++)
        {
            Vector3 offset = points[i].localPosition - origin;

            if ((offset - Vector3.Dot(offset, direction) * direction).sqrMagnitude > Hair * Hair)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool HasOrigin(this List<Transform> points)
    {
        return points.Exists(point => point.localPosition.sqrMagnitude < Hair * Hair);
    }

    /// <summary>The average position; the origin for no points.</summary>
    internal static Vector3 Centroid(this List<Transform> points)
    {
        Vector3 sum = Vector3.zero;

        foreach (Transform point in points)
        {
            sum += point.localPosition;
        }

        return points.Count > 0 ? sum / points.Count : Vector3.zero;
    }
}
