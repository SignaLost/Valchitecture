namespace Valchitecture.Modules.Modes;

/// <summary>The game's message for a placement status, as in <c>Player.TryPlacePiece</c>.</summary>
internal static class PlacementMessage
{
    internal static void Show(Player player, Player.PlacementStatus status)
    {
        string token = TokenFor(status);

        if (player && !string.IsNullOrEmpty(token))
        {
            player.Message(MessageHud.MessageType.Center, token);
        }
    }

    private static string TokenFor(Player.PlacementStatus status)
    {
        return status switch
        {
            Player.PlacementStatus.Valid => null,
            Player.PlacementStatus.NoBuildZone => "$msg_nobuildzone",
            Player.PlacementStatus.BlockedbyPlayer => "$msg_blocked",
            Player.PlacementStatus.PrivateZone => "$msg_privatezone",
            Player.PlacementStatus.MoreSpace => "$msg_needspace",
            Player.PlacementStatus.NoTeleportArea => "$msg_noteleportarea",
            Player.PlacementStatus.ExtensionMissingStation => "$msg_extensionmissingstation",
            Player.PlacementStatus.WrongBiome => "$msg_wrongbiome",
            Player.PlacementStatus.NeedCultivated => "$msg_needcultivated",
            Player.PlacementStatus.NeedDirt => "$msg_needdirt",
            Player.PlacementStatus.NotInDungeon => "$msg_notindungeon",
            Player.PlacementStatus.DeepSnow => "$msg_snowtoodeep",
            Player.PlacementStatus.NoSnow => "$msg_nosnow",
            _ => "$msg_invalidplacement"
        };
    }
}
