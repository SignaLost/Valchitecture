
using Valchitecture.Common;

namespace Valchitecture.Core;

/// <summary>Build menu selection.</summary>
internal static class HammerTable
{
    /// <summary>Whether the piece has an entry in the player's build menu.</summary>
    internal static bool CanSelect(Player player, Piece piece)
    {
        return player && piece && player.m_buildPieces && player.m_buildPieces.GetPieceIndex(piece, out _, out _);
    }

    internal static bool Select(Player player, Piece piece)
    {
        return player && piece && player.SetSelectedPiece(piece);
    }

    /// <summary>Selects the Repair entry.</summary>
    internal static bool SelectRepair(Player player)
    {
        if (!player || !player.m_buildPieces)
        {
            return false;
        }

        foreach (Piece piece in player.m_buildPieces.m_availablePieces)
        {
            if (piece && piece.m_repairPiece)
            {
                return player.SetSelectedPiece(piece);
            }
        }

        Log.LogWarning("Could not find the Repair entry to return to.");
        return false;
    }
}
