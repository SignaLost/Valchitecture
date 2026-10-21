using System.Collections.Generic;
using UnityEngine;

namespace Valchitecture.Modules.Snapping.Extra;

/// <summary>Snap point related traits of a build piece prefab.</summary>
internal static class PieceTraits
{
    /// <summary>Tag on roof pieces' colliders; the game's code does not read it.</summary>
    private const string RoofTag = "roof";

    /// <summary>Whether this is a static build piece.</summary>
    internal static bool TakesSnapPoints(this GameObject prefab)
    {
        if (!prefab || !prefab.TryGetComponent(out Piece piece) || piece.m_repairPiece)
        {
            return false;
        }

        return !prefab.GetComponentInChildren<Ship>()
            && !prefab.GetComponentInChildren<Vagon>()
            && !prefab.GetComponent<Character>()
            && !prefab.GetComponent<ItemDrop>()
            && !prefab.GetComponent<Projectile>()
            && !prefab.GetComponent<TerrainModifier>();
    }

    internal static bool IsTerrainOp(this GameObject prefab)
    {
        return prefab.GetComponent<TerrainOp>();
    }

    /// <summary>A terrain tool that levels or smooths.</summary>
    internal static bool IsHeightTool(this GameObject prefab)
    {
        return prefab.TryGetComponent(out TerrainOp op) && (op.m_settings.m_level || op.m_settings.m_smooth);
    }

    /// <summary>A torch, brazier or other piece with a fire.</summary>
    internal static bool IsFixture(this GameObject prefab)
    {
        return prefab.GetComponentInChildren<Fireplace>(true);
    }

    internal static bool IsRoof(this GameObject prefab)
    {
        foreach (Collider collider in prefab.GetComponentsInChildren<Collider>(true))
        {
            if (collider.CompareTag(RoofTag))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The piece's snap points: its direct children with the game's snap point tag.</summary>
    internal static List<Transform> GetSnapPoints(this GameObject prefab)
    {
        List<Transform> points = new();

        foreach (Transform child in prefab.transform)
        {
            if (child.CompareTag(SnapPointNames.TAG))
            {
                points.Add(child);
            }
        }

        return points;
    }

    internal static bool HasOriginSnapPoint(this GameObject prefab)
    {
        return prefab.GetSnapPoints().HasOrigin();
    }
}
