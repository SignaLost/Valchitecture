using System.Collections.Generic;
using UnityEngine;

namespace Valchitecture.Common;

/// <summary>Build pieces reachable in game: those in the piece tables of ObjectDB's items.</summary>
internal static class PieceTables
{
    /// <summary>Each piece prefab once; empty without ObjectDB.</summary>
    internal static List<GameObject> AllPieces()
    {
        List<GameObject> pieces = new();

        if (!ObjectDB.instance)
        {
            return pieces;
        }

        HashSet<PieceTable> tables = new();
        HashSet<GameObject> seen = new();

        foreach (GameObject item in ObjectDB.instance.m_items)
        {
            PieceTable table = item && item.TryGetComponent(out ItemDrop drop) ? drop.m_itemData?.m_shared?.m_buildPieces : null;

            if (!table || !tables.Add(table))
            {
                continue;
            }

            foreach (GameObject prefab in table.m_pieces)
            {
                if (prefab && seen.Add(prefab))
                {
                    pieces.Add(prefab);
                }
            }
        }

        return pieces;
    }
}
