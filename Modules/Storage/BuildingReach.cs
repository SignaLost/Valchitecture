using Chestworks;
using UnityEngine;
using Valchitecture.Modules.Modes;
using Valchitecture.Modules.Skill;

namespace Valchitecture.Modules.Storage;

/// <summary>The building purpose's reach in Chestworks, growing with the skill's unlocks.</summary>
internal static class BuildingReach
{
    /// <summary>Filled in place each time Chestworks asks; Chestworks does not keep it.</summary>
    private static readonly ReachRules _rules = new();

    internal static void Register()
    {
        Chestworks.API.RegisterReach(Purposes.Building, ValchitecturePlugin.PluginGuid, Rules);
    }

    private static ReachRules Rules(Player player)
    {
        if (!Unlocks.CanPullFromContainers())
        {
            return null;
        }

        GameObject ghost = player.m_placementGhost;

        _rules.Radius = ModeManager.Active ? Reach.CharacterRadius() : player.m_maxPlaceDistance;
        _rules.Stations = Unlocks.PullsFromStations();
        _rules.Linked = Unlocks.PullsFromNetwork();
        _rules.Point = ghost && ghost.activeSelf ? ghost.transform.position : null;
        return _rules;
    }
}
