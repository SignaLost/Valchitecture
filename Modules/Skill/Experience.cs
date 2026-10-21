using Mimir;
using UnityEngine;
using Valchitecture.Common.Configuration;

namespace Valchitecture.Modules.Skill;
/// <summary>Building experience from Mimir's events; removing your own pieces adds a debt that later placements pay off instead of earning.</summary>
internal static class Experience
{
    private const int MaxDebt = 20;

    /// <summary>Placements owed after removing your own pieces.</summary>
    internal static int Debt;

    internal static void Subscribe()
    {
        Events.Placed += OnPlaced;
        Events.Removed += OnRemoved;
        Events.Repaired += OnRepaired;
    }

    private static void OnPlaced(Player player, Piece piece)
    {
        if (!Cfg.Enabled.Value)
        {
            return;
        }

        if (Debt > 0)
        {
            Debt--;
            return;
        }

        BuildingSkill.Raise(player, BuildingSkill.PlaceExperience);
    }

    private static void OnRemoved(Player player, Piece piece)
    {
        if (!Cfg.Enabled.Value)
        {
            return;
        }

        if (piece.IsCreator())
        {
            Debt = Mathf.Min(Debt + 1, MaxDebt);
            return;
        }

        BuildingSkill.Raise(player, BuildingSkill.DestroyExperience);
    }

    /// <summary>Called once per piece repaired.</summary>
    private static void OnRepaired(Player player, Piece piece)
    {
        if (Cfg.Enabled.Value)
        {
            BuildingSkill.Raise(player, BuildingSkill.RepairExperience);
        }
    }
}
