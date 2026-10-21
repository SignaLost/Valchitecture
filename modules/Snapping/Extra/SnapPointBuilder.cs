using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valchitecture.Common;

using Shape = Valchitecture.Modules.Snapping.Extra.SnapPointGeometry.Shape;

namespace Valchitecture.Modules.Snapping.Extra;

/// <summary>
///     Adds this mod's snap points to every build piece prefab, while the Extra Snap Points setting is on.
///     Each pass first restores every prefab, then adds a hand-placed layout for known pieces or one computed from the
///     piece's own points, by shape category and per piece switch; a pass runs again whenever a snap setting changes.
/// </summary>
internal static class SnapPointBuilder
{
    /// <summary>Points nearer than this are the same point.</summary>
    private const float SamePoint = 0.01f;

    internal static readonly SnapPointSpec[] Empty = Array.Empty<SnapPointSpec>();
    internal static readonly SnapPointSpec[] OriginOnly = { new SnapPointSpec(Vector3.zero, SnapRole.Center) };

    /// <summary>Pieces always left as the game made them.</summary>
    private static readonly HashSet<string> Untouched = new() { "piece_dvergr_spiralstair", "piece_dvergr_spiralstair_right" };

    /// <summary>Roles a computed layout assigns to the piece's own points; reset per prefab.</summary>
    private static readonly Dictionary<Transform, SnapRole> _existingRoles = new();

    /// <summary>Prefab hashes of pieces that ship without snap points.</summary>
    private static readonly HashSet<int> _shippedBare = new();

    /// <summary>Whether this piece or ghost shipped without snap points; its added points are for Manual only.</summary>
    internal static bool ShippedBare(Piece piece)
    {
        return _shippedBare.Count > 0 && _shippedBare.Contains(PrefabKey.Of(piece));
    }

    /// <summary>Whether this piece gets extra snap points and a per piece switch.</summary>
    internal static bool Takes(GameObject prefab)
    {
        return prefab.TakesSnapPoints() && !Untouched.Contains(prefab.name);
    }

    /// <summary>Restores every piece, then adds its points again.</summary>
    internal static void Rebuild(string reason, bool force = false)
    {
        if (!ZNetScene.instance || SceneManager.GetActiveScene().name != "main")
        {
            return;
        }

        if (!SnapConfig.RebuildDue && !force)
        {
            return;
        }

        SnapConfig.ClearRebuild();
        Log.LogInfo(reason, Log.InfoLevel.Medium);
        Log.LogInfo($"Removing {AddedSnapPoints.Count} added snap points.", Log.InfoLevel.Medium);
        AddedSnapPoints.RemoveAll();
        _shippedBare.Clear();

        // Must run before any setting can end this pass.
        PieceConfig.BindAll(true);

        if (!Cfg.Enabled.Value || !SnapConfig.EnableExtraSnapPoints.Value)
        {
            return;
        }

        List<GameObject> pieces = BuildPieces();

        Stopwatch watch = Stopwatch.StartNew();
        Stopwatch compute = new();
        Stopwatch apply = new();
        AddedSnapPoints.NamingTime.Reset();
        List<(long ms, string name)> slowest = new();

        foreach (GameObject prefab in pieces)
        {
            long start = watch.ElapsedMilliseconds;

            try
            {
                if (!PieceConfig.SnapAllowed(prefab))
                {
                    continue;
                }

                if (prefab.GetSnapPoints().Count == 0)
                {
                    _shippedBare.Add(PrefabKey.Of(prefab));
                }

                _existingRoles.Clear();
                compute.Start();
                SnapPointSpec[] layout = LayoutFor(prefab);
                compute.Stop();
                apply.Start();
                AddedSnapPoints.Add(prefab, layout, _existingRoles);
                apply.Stop();
            }
            catch (Exception e)
            {
                compute.Stop();
                apply.Stop();
                Log.LogWarning($"Could not add snap points to {prefab.name}: {e}");
            }

            Slowest(slowest, watch.ElapsedMilliseconds - start, prefab.name);
        }

        watch.Stop();

        long label = AddedSnapPoints.NamingTime.ElapsedMilliseconds;

        if (Log.Shows(Log.InfoLevel.Medium))
        {
            Log.LogInfo(
                $"Snap point time: {compute.ElapsedMilliseconds} ms computing, {apply.ElapsedMilliseconds - label} ms creating, " +
                $"{label} ms naming. Slowest: {string.Join(", ", slowest.Select(s => $"{s.name} {s.ms} ms"))}.",
                Log.InfoLevel.Medium);
        }

        long elapsed = watch.ElapsedMilliseconds;
        string summary = $"Snap points ready on {pieces.Count} pieces in {elapsed} ms.";

        if (elapsed > 3000)
        {
            Log.LogWarning(summary + " That is slow enough to look like a freeze on world load.");
        }
        else
        {
            Log.LogInfo(summary, Log.InfoLevel.Medium);
        }
    }

    /// <summary>Keeps the five slowest pieces, slowest first.</summary>
    private static void Slowest(List<(long ms, string name)> slowest, long ms, string name)
    {
        int at = slowest.FindIndex(s => s.ms < ms);

        if (at < 0 && slowest.Count >= 5)
        {
            return;
        }

        slowest.Insert(at < 0 ? slowest.Count : at, (ms, name));

        if (slowest.Count > 5)
        {
            slowest.RemoveAt(5);
        }
    }

    /// <summary>Every build piece that takes snap points.</summary>
    internal static List<GameObject> BuildPieces()
    {
        List<GameObject> pieces = PieceTables.AllPieces();
        pieces.RemoveAll(prefab => !Takes(prefab));
        return pieces;
    }

    /// <summary>A hand-placed layout for known pieces, else one computed from the piece's own points, minus buried ones.</summary>
    private static SnapPointSpec[] LayoutFor(GameObject prefab)
    {
        SnapPointSpec[] handPlaced = SnapConfig.EnableHandPlacedSnapPoints.Value ? CuratedSnapPoints.For(prefab) : null;
        return handPlaced != null ? WithoutExisting(prefab, handPlaced) : SolidFilter.RemoveInterior(prefab, Computed(prefab));
    }

    /// <summary>Drops specs that coincide with a point the piece already has.</summary>
    private static SnapPointSpec[] WithoutExisting(GameObject prefab, SnapPointSpec[] specs)
    {
        List<Transform> existing = prefab.GetSnapPoints();

        if (existing.Count == 0)
        {
            return specs;
        }

        return specs.Where(spec => !existing.Exists(point => (point.localPosition - spec.LocalPosition).sqrMagnitude < SamePoint * SamePoint)).ToArray();
    }

    /// <summary>Points computed from the prefab's own snap point layout, or none.</summary>
    private static SnapPointSpec[] Computed(GameObject prefab)
    {
        if (prefab.IsTerrainOp() && !(SnapConfig.EnableTerrainOpSnapPoints.Value && prefab.IsHeightTool()))
        {
            return Empty;
        }

        List<Transform> existing = prefab.GetSnapPoints();

        if (existing.Count == 0)
        {
            return Fixture(prefab) ?? OriginOnly;
        }

        Shape shape = SnapPointGeometry.Classify(existing, prefab.IsRoof());

        if (shape == Shape.Skip)
        {
            return Empty;
        }

        // Log beams have end points laid out like a wall's corners.
        if ((shape is Shape.Rectangle or Shape.Unknown) && SnapPointGeometry.IsEndPointBeam(prefab, existing))
        {
            shape = Shape.Line;
        }

        if (Allowed(shape))
        {
            return SnapPointGeometry.PointsFor(prefab, shape, existing, _existingRoles);
        }

        return existing.HasOrigin() ? Empty : OriginOnly;
    }

    private static bool Allowed(Shape shape)
    {
        return shape switch
        {
            Shape.Line => SnapConfig.EnablePoleBeamSnapPoints.Value,
            Shape.Triangle => SnapConfig.EnableTriangleSnapPoints.Value,
            Shape.Rectangle => SnapConfig.EnableRect2DSnapPoints.Value,
            Shape.Roof => SnapConfig.EnableRoofTopSnapPoints.Value,
            Shape.Box => SnapConfig.EnableBoxSnapPoints.Value,
            _ => false
        };
    }

    /// <summary>Points for a torch or brazier without its own: the origin and the centers of its mesh bottom and top.</summary>
    private static SnapPointSpec[] Fixture(GameObject prefab)
    {
        if (!prefab.IsFixture() || !MeshExtent(prefab, out Bounds drawn))
        {
            return null;
        }

        List<SnapPointSpec> points = new() { new SnapPointSpec(Vector3.zero, SnapRole.Center) };

        foreach (float y in new[] { drawn.min.y, drawn.max.y })
        {
            Vector3 at = new(drawn.center.x, y, drawn.center.z);

            if (at.sqrMagnitude > 0.01f * 0.01f)
            {
                points.Add(new SnapPointSpec(at));
            }
        }

        return points.ToArray();
    }

    /// <summary>Bounds of the prefab's meshes, in its local space.</summary>
    private static bool MeshExtent(GameObject prefab, out Bounds extent)
    {
        extent = default;
        bool found = false;

        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;

            if (!mesh)
            {
                continue;
            }

            Bounds own = mesh.bounds;

            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 local = own.center + Vector3.Scale(own.extents, new Vector3(
                    (corner & 1) == 0 ? -1f : 1f,
                    (corner & 2) == 0 ? -1f : 1f,
                    (corner & 4) == 0 ? -1f : 1f));
                Vector3 inPrefab = prefab.transform.InverseTransformPoint(filter.transform.TransformPoint(local));

                if (!found)
                {
                    extent = new Bounds(inPrefab, Vector3.zero);
                    found = true;
                }
                else
                {
                    extent.Encapsulate(inPrefab);
                }
            }
        }

        return found;
    }
}
