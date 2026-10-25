using System;
using Jotunn.Utils;
using Mimir;
using UnityEngine;
using Valchitecture.Common;
using Valchitecture.Modules;

namespace Valchitecture.Core;

/// <summary>
///     The Building skill, registered with and stored by Mimir, with its experience values and gain rate setting.
///     Registration also wires the skill's unlocks, experience events, the stamina bonus (the Cheaper Swings
///     setting) and the piece strength perks shown on Mimir's Skill Abilities page.
/// </summary>
internal static class BuildingSkill
{
    /// <summary>The skill's id in Mimir, hashed into the saved skill type; changing it orphans every character's progress.</summary>
    internal const string Id = "valchitecture.building";

    private const string IconResource = "Valchitecture.Resources.building-icon.png";

    // Experience per action, in the game's units; vanilla pays 1.0 per placement.
    internal const float PlaceExperience = 1f;
    internal const float MoveExperience = 0.75f;
    internal const float DestroyExperience = 0.5f;
    internal const float RepairExperience = 0.125f;

    /// <summary>Floor on the effective cost of early levels, in points.</summary>
    private const float EarlyLevelCost = 20f;

    // The level curve: (level + 1)^1.25 × 0.9 + 0.5, flatter than the game's ^1.5 × 0.5 so late levels cost less.
    private const float CurvePower = 1.25f;
    private const float CurveScale = 0.9f;

    // In percent.
    private const float StaminaReliefPerStep = 2f;
    private const int StaminaLevelsPerStep = 4;

    internal static MimirSkill Instance { get; private set; }

    internal static void Register()
    {
        Instance = MimirAPI.Register(new SkillDefinition
        {
            Id = Id,
            Name = Tokens.Skill,
            Description = Tokens.SkillDescription,
            Icon = LoadIcon(),
            EarlyLevelCost = EarlyLevelCost,
            LevelCurve = level => Mathf.Pow(level + 1, CurvePower) * CurveScale + 0.5f
        });

        if (Instance == null)
        {
            Log.LogError("The Building skill could not be registered; every unlock reads as level 0.");
            return;
        }

        Instance.GainRate = Cfg.SkillGain.Value;
        Cfg.SkillGain.SettingChanged += (_, _) => Instance.GainRate = Cfg.SkillGain.Value;

        Instance.DeathLoss = Cfg.DeathLoss.Value;
        Cfg.DeathLoss.SettingChanged += (_, _) => Instance.DeathLoss = Cfg.DeathLoss.Value;
        FollowFortify();
        Cfg.FortifyBuilding.SettingChanged += (_, _) => FollowFortify();

        Unlocks.Register(Instance);
        Experience.Subscribe();
        AddCheaperSwings();
        AddPiecePerks();
    }

    /// <summary>Adds a Mimir bonus lowering stamina per swing 2% every fourth level.</summary>
    private static void AddCheaperSwings()
    {
        Bonus swings = Instance.AddBonus(Stat.BuildStamina, BonusType.Percent,
            level => Ramp.Stepped(level, 0f, StaminaLevelsPerStep, 0f, -StaminaReliefPerStep, -100f));

        void Follow() => swings.Enabled = Cfg.Enabled.Value && Cfg.CheaperSwings.Value;

        Follow();
        Cfg.Enabled.SettingChanged += (_, _) => Follow();
        Cfg.CheaperSwings.SettingChanged += (_, _) => Follow();
    }

    /// <summary>Fortifies the skill while Fortify Building is on; off leaves the choice to Mimir's own setting.</summary>
    private static void FollowFortify()
    {
        Instance.Fortify.Enabled = Cfg.FortifyBuilding.Value ? true : null;
    }

    /// <summary>Lists piece health and support loss among the skill's perks on Mimir's Skill Abilities page.</summary>
    private static void AddPiecePerks()
    {
        Instance.AddPerk(Tokens.PerkPieceHealth, level => PieceStrength.Active ? (PieceStrength.HealthScale(level / 100f) - 1f) * 100f : 0f, "%");
        Instance.AddPerk(Tokens.PerkSupportLoss, level => PieceStrength.Active ? (PieceStrength.LossScale(level / 100f) - 1f) * 100f : 0f, "%");
    }

    /// <summary>Loads the skill icon through Jotunn; Texture2D.LoadImage is in a module this project cannot reference.</summary>
    private static Sprite LoadIcon()
    {
        try
        {
            byte[] data = EmbeddedResources.Read(IconResource);
            Texture2D texture = data != null ? AssetUtils.LoadImage(data) : null;

            if (!texture)
            {
                Log.LogError("The skill's icon could not be decoded.");
                return null;
            }

            texture.name = "ValchitectureBuildingIcon";
            texture.wrapMode = TextureWrapMode.Clamp;

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        catch (Exception e)
        {
            Log.LogError($"The skill's icon could not be loaded: {e.Message}");
            return null;
        }
    }

    /// <summary>Current level with status effects.</summary>
    internal static float Level => Instance?.Level ?? 0f;

    internal static float Factor => Instance?.Factor ?? 0f;

    internal static void Raise(Player player, float amount)
    {
        Instance?.Raise(player, amount);
    }
}
