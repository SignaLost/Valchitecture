using BepInEx.Configuration;
using Mimir;
using Valchitecture.Common;
using Valchitecture.Modules;

namespace Valchitecture.Core;

/// <summary>
///     The Building skill's abilities, registered with Mimir at server-set levels and each switched on or off by its
///     own setting; disabling one keeps the unlock.
///     The modes, build camera, area repair and container features ask here whether their ability is unlocked, and the
///     camera range, repair radius and reach multiplier grow from these abilities with the skill level.
/// </summary>
internal static class Unlocks
{
    /// <summary>Default levels; each is a setting under <see cref="Cfg.SecProgression" />.</summary>
    internal const int BuildLevel = 5;
    internal const int ContainerLevel = 15;
    internal const int EditLevel = 25;
    internal const int SweepingRepairLevel = 35;
    internal const int CameraLevel = 50;
    internal const int HoppingLevel = 75;
    internal const int NetworkLevel = 75;
    internal const int SalvageLevel = 85;
    internal const int TopLevel = 100;

    /// <summary>Levels per step of the camera range and the repair radius.</summary>
    private const int LevelsPerStep = 4;

    private const float RangeCeiling = 25f;

    private const float CameraRangeBase = 7f;
    private const float CameraRangeStep = 1.5f;

    private const float RepairRadiusBase = 5f;
    private const float RepairRadiusStep = 1.25f;

    /// <summary>Levels per step of build reach, from the camera unlock.</summary>
    private const int ReachLevelsPerStep = 10;

    private const float ReachPerStep = 0.2f;
    private const float ReachMax = 2f;

    /// <summary>Who disables an ability whose setting is off.</summary>
    private const string SettingSource = "valchitecture.settings";

    private static MimirSkill _skill;

    private static Ability _build;
    private static Ability _supply;
    private static Ability _edit;
    private static Ability _mend;
    private static Ability _camera;
    private static Ability _workshop;
    private static Ability _hop;
    private static Ability _reach;
    private static Ability _network;
    private static Ability _salvage;

    /// <summary>Gives the skill its abilities, each following its settings.</summary>
    internal static void Register(MimirSkill skill)
    {
        _skill = skill;

        _build = Add("build", Tokens.AbilityBuild, Tokens.AbilityBuildUnlocked, Cfg.HammersCall, Cfg.HammersCallLevel);
        _supply = Add("supply", Tokens.AbilitySupply, Tokens.AbilitySupplyUnlocked, Cfg.GatheredHoard, Cfg.GatheredHoardLevel);
        _edit = Add("edit", Tokens.AbilityEdit, Tokens.AbilityEditUnlocked, Cfg.HuginnsCounsel, Cfg.HuginnsCounselLevel);
        _mend = Add("mend", Tokens.AbilityMend, Tokens.AbilityMendUnlocked, Cfg.EirsTouch, Cfg.EirsTouchLevel);
        _camera = Add("camera", Tokens.AbilityCamera, Tokens.AbilityCameraUnlocked, Cfg.RavensView, Cfg.RavensViewLevel);
        _workshop = Add("workshop", Tokens.AbilityWorkshop, Tokens.AbilityWorkshopUnlocked, Cfg.LonghouseStores, Cfg.LonghouseStoresLevel);
        _hop = Add("hop", Tokens.AbilityHop, Tokens.AbilityHopUnlocked, Cfg.BifrostStep, Cfg.BifrostStepLevel);
        _reach = Add("reach", Tokens.AbilityReach, Tokens.AbilityReachUnlocked, Cfg.RavensReach, Cfg.RavensReachLevel);
        _network = Add("network", Tokens.AbilityNetwork, Tokens.AbilityNetworkUnlocked, Cfg.RootsOfTheHall, Cfg.RootsOfTheHallLevel);
        _salvage = Add("salvage", Tokens.AbilitySalvage, Tokens.AbilitySalvageUnlocked, Cfg.DvergrThrift, Cfg.DvergrThriftLevel);
    }

    /// <summary>Adds an ability that follows its level and on/off settings; off disables it through Mimir, keeping the unlock.</summary>
    private static Ability Add(string id, string name, string unlocked, ConfigEntry<bool> on, ConfigEntry<int> level)
    {
        Ability ability = _skill.AddAbility(id, name, level.Value, unlocked, name + "_description");

        if (ability == null)
        {
            return null;
        }

        level.SettingChanged += (_, _) => _skill.SetAbilityLevel(ability.Id, level.Value);
        Follow(ability, on);
        on.SettingChanged += (_, _) => Follow(ability, on);
        return ability;
    }

    private static void Follow(Ability ability, ConfigEntry<bool> on)
    {
        if (on.Value)
        {
            _skill.Enable(ability.Id, SettingSource);
        }
        else
        {
            _skill.Disable(ability.Id, SettingSource);
        }
    }

    private static bool Has(Ability ability) => _skill != null && _skill.Has(ability);

    /// <summary>The level an ability comes at; 101 when it is missing.</summary>
    private static float LevelOf(Ability ability) => ability?.Level ?? 101f;

    internal static bool CanEnter(BuildMode mode)
    {
        return Cfg.Enabled.Value && mode switch
        {
            BuildMode.Build => Has(_build),
            BuildMode.Edit => Has(_edit),
            _ => false
        };
    }

    internal static bool CanDetachCamera() => Cfg.Enabled.Value && Has(_camera);

    /// <summary>Whether the working station follows the camera between stations.</summary>
    internal static bool CanHopStations() => Has(_hop);

    internal static bool CanRepairArea() => Cfg.Enabled.Value && Has(_mend);

    /// <summary>Whether building is judged from the camera rather than the character.</summary>
    internal static bool CanBuildFromCamera() => Has(_reach);

    /// <summary>Building from containers within build reach.</summary>
    internal static bool CanPullFromContainers() => Cfg.Enabled.Value && Has(_supply);

    /// <summary>Containers within station range.</summary>
    internal static bool PullsFromStations() => Has(_workshop);

    /// <summary>Containers across stations linked by overlapping ranges.</summary>
    internal static bool PullsFromNetwork() => Has(_network);

    internal static bool CanPickUpRemovals() => Cfg.Enabled.Value && Has(_salvage);

    /// <summary>Camera range from the character; once hopping, build reach plus the station margin.</summary>
    internal static float CameraRange()
    {
        return CanHopStations()
            ? Reach.CharacterRadius() + BuildAnchor.Margin
            : Ramp.Stepped(BuildingSkill.Level, LevelOf(_camera), LevelsPerStep, CameraRangeBase, CameraRangeStep, RangeCeiling);
    }

    internal static float ReachMultiplier()
    {
        return Cfg.EnableLongerReach.Value
            ? Ramp.Stepped(BuildingSkill.Level, LevelOf(_camera), ReachLevelsPerStep, 1f, ReachPerStep, ReachMax, below: 1f)
            : 1f;
    }

    internal static float RepairRadius()
    {
        return Ramp.Stepped(BuildingSkill.Level, LevelOf(_mend), LevelsPerStep, RepairRadiusBase, RepairRadiusStep, RangeCeiling);
    }
}
