using System;
using UnityEngine;

namespace Valchitecture.Common;

/// <summary>Prefab name hash, as used by <c>ZNetScene</c> and ZDOs.</summary>
internal static class PrefabKey
{
    internal static int Of(GameObject prefab)
    {
        return prefab.name.GetStableHashCode();
    }

    private static Component _last;
    private static int _lastKey;

    private static GameObject _lastNamed;
    private static int _lastNamedKey;

    /// <summary>The ZDO prefab hash of an instance, or the hash of its name without "(Clone)" when it has no ZDO.</summary>
    internal static int Of(Component instance)
    {
        if (instance == _last && instance)
        {
            return _lastKey;
        }

        _last = instance;
        _lastKey = Compute(instance);
        return _lastKey;
    }

    /// <summary>The prefab hash of a piece, read from the view the game already resolved.</summary>
    internal static int Of(Piece piece)
    {
        ZNetView view = piece.m_nview;
        ZDO zdo = view is null ? null : view.GetZDO();
        return zdo != null ? zdo.GetPrefab() : OfName(piece.gameObject);
    }

    /// <summary>The prefab an instance was made from, or null outside a world.</summary>
    internal static GameObject PrefabOf(Component instance)
    {
        return instance && ZNetScene.instance ? ZNetScene.instance.GetPrefab(Of(instance)) : null;
    }

    private static int Compute(Component instance)
    {
        ZNetView view = instance.GetComponent<ZNetView>();
        ZDO zdo = view ? view.GetZDO() : null;

        return zdo != null ? zdo.GetPrefab() : OfName(instance.gameObject);
    }

    /// <summary>The hash of an object's name without "(Clone)", cached for the last object asked.</summary>
    private static int OfName(GameObject instance)
    {
        if (ReferenceEquals(instance, _lastNamed))
        {
            return _lastNamedKey;
        }

        string name = instance.name;
        int clone = name.IndexOf("(Clone)", StringComparison.Ordinal);

        _lastNamed = instance;
        _lastNamedKey = (clone < 0 ? name : name.Substring(0, clone)).GetStableHashCode();
        return _lastNamedKey;
    }
}
