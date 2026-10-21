using UnityEngine;

namespace Valchitecture.Core;

/// <summary>Components of the placement ghost, resolved once per ghost object.</summary>
internal static class GhostInfo
{
    private static GameObject _ghost;
    private static Transform _root;
    private static Piece _piece;
    private static TerrainOp _op;
    private static bool _heightTool;
    private static StationExtension _extension;
    private static PrivateArea _area;

    internal static Piece PieceOf(GameObject ghost)
    {
        Resolve(ghost);
        return _piece;
    }

    internal static Piece PieceOf(Transform ghost)
    {
        Resolve(ghost);
        return _piece;
    }

    internal static TerrainOp TerrainOpOf(GameObject ghost)
    {
        Resolve(ghost);
        return _op;
    }

    internal static StationExtension ExtensionOf(GameObject ghost)
    {
        Resolve(ghost);
        return _extension;
    }

    internal static PrivateArea AreaOf(GameObject ghost)
    {
        Resolve(ghost);
        return _area;
    }

    internal static bool IsTerrainTool(Transform ghost)
    {
        Resolve(ghost);
        return _op;
    }

    /// <summary>A terrain tool that levels or smooths.</summary>
    internal static bool IsHeightTool(Transform ghost)
    {
        Resolve(ghost);
        return _heightTool;
    }

    private static void Resolve(GameObject ghost)
    {
        if (ReferenceEquals(ghost, _ghost))
        {
            return;
        }

        _ghost = ghost;
        _root = ghost ? ghost.transform : null;
        Fetch();
    }

    private static void Resolve(Transform ghost)
    {
        if (ReferenceEquals(ghost, _root))
        {
            return;
        }

        _root = ghost;
        _ghost = ghost ? ghost.gameObject : null;
        Fetch();
    }

    private static void Fetch()
    {
        _piece = _ghost ? _ghost.GetComponent<Piece>() : null;
        _op = _ghost ? _ghost.GetComponent<TerrainOp>() : null;
        _heightTool = _op && (_op.m_settings.m_level || _op.m_settings.m_smooth);
        _extension = _ghost ? _ghost.GetComponent<StationExtension>() : null;
        _area = _ghost ? _ghost.GetComponent<PrivateArea>() : null;
    }
}
