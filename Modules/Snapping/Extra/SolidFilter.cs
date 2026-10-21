using System.Collections.Generic;
using UnityEngine;

namespace Valchitecture.Modules.Snapping.Extra;

/// <summary>Drops computed points buried deeper than every existing point in the piece's box and sphere colliders.</summary>
internal static class SolidFilter
{
    /// <summary>Tolerance over an existing point's depth before a point counts as deeper.</summary>
    private const float Tolerance = 0.05f;

    private static readonly List<Collider> _colliders = new();
    private static readonly List<Collider> _solids = new();
    private static readonly List<Vector3> _existingDepth = new();

    internal static SnapPointSpec[] RemoveInterior(GameObject prefab, SnapPointSpec[] points)
    {
        if (!prefab || points.Length == 0)
        {
            return points;
        }

        Collect(prefab);

        if (_solids.Count == 0)
        {
            return points;
        }

        List<SnapPointSpec> kept = null;

        for (int i = 0; i < points.Length; i++)
        {
            bool interior = IsBuried(prefab.transform.TransformPoint(points[i].LocalPosition));

            if (interior && kept == null)
            {
                kept = new List<SnapPointSpec>(points.Length);

                for (int j = 0; j < i; j++)
                {
                    kept.Add(points[j]);
                }
            }

            if (!interior)
            {
                kept?.Add(points[i]);
            }
        }

        _solids.Clear();
        _existingDepth.Clear();
        return kept == null ? points : kept.ToArray();
    }

    /// <summary>Solid colliders, each with the deepest an existing point reaches per axis.</summary>
    private static void Collect(GameObject prefab)
    {
        _colliders.Clear();
        _solids.Clear();
        _existingDepth.Clear();
        prefab.GetComponentsInChildren(true, _colliders);

        List<Transform> existing = prefab.GetSnapPoints();

        foreach (Collider collider in _colliders)
        {
            if (!collider || collider.isTrigger || collider is not (BoxCollider or SphereCollider))
            {
                continue;
            }

            Vector3 deepest = Vector3.zero;

            foreach (Transform point in existing)
            {
                deepest = Vector3.Max(deepest, Depth(collider, point.position));
            }

            _solids.Add(collider);
            _existingDepth.Add(deepest);
        }

        _colliders.Clear();
    }

    private static bool IsBuried(Vector3 world)
    {
        for (int i = 0; i < _solids.Count; i++)
        {
            Vector3 depth = Depth(_solids[i], world);
            Vector3 limit = _existingDepth[i];

            if (depth.x > limit.x + Tolerance && depth.y > limit.y + Tolerance && depth.z > limit.z + Tolerance)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Depth of a world point inside the collider per local axis (radial for spheres); zero or less is outside.</summary>
    private static Vector3 Depth(Collider collider, Vector3 world)
    {
        if (collider is BoxCollider box)
        {
            Vector3 p = box.transform.InverseTransformPoint(world) - box.center;
            Vector3 h = box.size * 0.5f;
            return new Vector3(h.x - Mathf.Abs(p.x), h.y - Mathf.Abs(p.y), h.z - Mathf.Abs(p.z));
        }

        SphereCollider sphere = (SphereCollider)collider;
        float radial = sphere.radius - (sphere.transform.InverseTransformPoint(world) - sphere.center).magnitude;
        return new Vector3(radial, radial, radial);
    }
}
