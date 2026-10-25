using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Valchitecture.Core;

namespace Valchitecture.Common;

/// <summary>Core settings.</summary>
internal static class Cfg
{
    private const string SecGeneral = "1. General";
    internal const string SecAbilities = "2. Abilities";
    internal const string SecProgression = "3. Progression";
    private const string SecKeys = "4. Keybinds";
    private const string SecTools = "5. Tools";
    internal const string SecSnapping = "6. Snapping";
    private const string SecRules = "7. Building Rules";

    internal const string SyncedTag = "[Synced with Server]";
    internal const string LocalTag = "[Not Synced with Server]";

    internal static ConfigEntry<Log.InfoLevel> Verbosity;
    internal static ConfigEntry<bool> ShowHints;

    // Mechanics a server may turn off.
    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<bool> EnableLongerReach;
    internal static ConfigEntry<bool> StrongerPieces;
    internal static ConfigEntry<bool> CheaperSwings;
    internal static ConfigEntry<bool> RepairUpgrades;

    internal static ConfigEntry<float> SkillGain;
    internal static ConfigEntry<float> DeathLoss;
    internal static ConfigEntry<bool> FortifyBuilding;
    internal static ConfigEntry<bool> HammersCall;
    internal static ConfigEntry<int> HammersCallLevel;
    internal static ConfigEntry<bool> GatheredHoard;
    internal static ConfigEntry<int> GatheredHoardLevel;
    internal static ConfigEntry<bool> HuginnsCounsel;
    internal static ConfigEntry<int> HuginnsCounselLevel;
    internal static ConfigEntry<bool> EirsTouch;
    internal static ConfigEntry<int> EirsTouchLevel;
    internal static ConfigEntry<bool> RavensView;
    internal static ConfigEntry<int> RavensViewLevel;
    internal static ConfigEntry<bool> LonghouseStores;
    internal static ConfigEntry<int> LonghouseStoresLevel;
    internal static ConfigEntry<bool> BifrostStep;
    internal static ConfigEntry<int> BifrostStepLevel;
    internal static ConfigEntry<bool> RavensReach;
    internal static ConfigEntry<int> RavensReachLevel;
    internal static ConfigEntry<bool> RootsOfTheHall;
    internal static ConfigEntry<int> RootsOfTheHallLevel;
    internal static ConfigEntry<bool> DvergrThrift;
    internal static ConfigEntry<int> DvergrThriftLevel;

    // Building rules a server may relax. Vanilla by default.
    internal static ConfigEntry<float> StationRange;
    internal static ConfigEntry<float> ExtensionRange;
    internal static ConfigEntry<float> BaseRange;
    internal static ConfigEntry<bool> EverythingSupports;
    internal static ConfigEntry<bool> FurnitureSupports;
    internal static ConfigEntry<bool> IgnoreClipping;
    internal static ConfigEntry<bool> IgnoreNoBuildZones;
    internal static ConfigEntry<bool> IgnoreBiomeLimits;
    internal static ConfigEntry<bool> GuardUsed;

    internal static ConfigEntry<float> CameraMoveSpeed;
    internal static ConfigEntry<bool> CameraCollidesWithTerrain;

    internal static ConfigEntry<bool> RotateAroundWorldAxes;
    internal static ConfigEntry<float> RotationStepDegrees;
    internal static ConfigEntry<bool> ShowGizmo;
    internal static ConfigEntry<bool> ShowSnapPoints;
    internal static ConfigEntry<bool> GizmoFitsPiece;
    internal static ConfigEntry<float> GizmoScale;
    internal static ConfigEntry<string> GizmoColorX;
    internal static ConfigEntry<string> GizmoColorY;
    internal static ConfigEntry<string> GizmoColorZ;

    internal static ConfigEntry<float> NudgeStepMeters;
    internal static ConfigEntry<float> GridSize;

    internal static ConfigEntry<KeyboardShortcut> ToggleModeKey;
    internal static ConfigEntry<KeyboardShortcut> ToggleCameraKey;
    internal static ConfigEntry<KeyCode> CancelEditKey;
    internal static ConfigEntry<KeyCode> CopyPieceButton;
    internal static ConfigEntry<KeyCode> CopyPieceModifier;
    internal static ConfigEntry<KeyCode> CopyPieceIgnoreRotationModifier;
    internal static ConfigEntry<KeyCode> IncreaseSpeedKey;
    internal static ConfigEntry<KeyCode> DecreaseSpeedKey;
    internal static ConfigEntry<KeyCode> BigSpeedStepModifier;
    internal static ConfigEntry<KeyCode> CameraUpKey;
    internal static ConfigEntry<KeyCode> CameraDownKey;
    internal static ConfigEntry<KeyCode> RotateTiltModifier;
    internal static ConfigEntry<KeyCode> RotateSpinModifier;
    internal static ConfigEntry<KeyCode> ResetRotationKey;
    internal static ConfigEntry<KeyCode> CopyRotationKey;
    internal static ConfigEntry<KeyCode> ToggleNudgeKey;
    internal static ConfigEntry<KeyCode> NudgeLeftKey;
    internal static ConfigEntry<KeyCode> NudgeRightKey;
    internal static ConfigEntry<KeyCode> NudgeForwardKey;
    internal static ConfigEntry<KeyCode> NudgeBackKey;
    internal static ConfigEntry<KeyCode> NudgeVerticalModifier;
    internal static ConfigEntry<KeyCode> CycleSnapModeKey;
    internal static ConfigEntry<KeyCode> TargetSnapModifier;
    internal static ConfigEntry<KeyCode> SnapResetKey;

    // Fixed tuning, not configurable.

    /// <summary>Ring brightness multiplier on the configured colors.</summary>
    internal const float RingBrightness = 0.8f;

    /// <summary>Vertical camera speed as a fraction of horizontal.</summary>
    internal const float CameraVerticalFactor = 0.5f;

    /// <summary>Build reach in a mode, as a multiple of the game's own, before the skill adds to it.</summary>
    internal const float PlaceDistanceFactor = 1.5f;

    /// <summary>Clearance kept between the camera and terrain.</summary>
    internal const float CameraTerrainClearance = 0.6f;

    /// <summary>Step rate multiplies both the rotation and nudge steps.</summary>
    internal const int StartingSpeedLevel = 1;
    internal const int MaxSpeedLevel = 30;
    internal const int BigSpeedStep = 10;

    /// <summary>Key repeat for held nudge keys.</summary>
    internal const float NudgeRepeatDelay = 0.3f;
    internal const float NudgeRepeatRate = 0.05f;

    /// <summary>Ring colors, parsed once per setting change.</summary>
    internal static Color RingColorX => _ringColorX ??= RingColor(GizmoColorX, FallbackX);

    internal static Color RingColorY => _ringColorY ??= RingColor(GizmoColorY, FallbackY);
    internal static Color RingColorZ => _ringColorZ ??= RingColor(GizmoColorZ, FallbackZ);

    private static readonly Color FallbackX = new(1f, 0.125f, 0.125f);
    private static readonly Color FallbackY = new(0.125f, 1f, 0.125f);
    private static readonly Color FallbackZ = new(0.125f, 0.125f, 1f);

    private static Color? _ringColorX;
    private static Color? _ringColorY;
    private static Color? _ringColorZ;

    private static string _unreadableColor;

    /// <summary>Parses a ring color from hex text or a color name; returns the fallback when unreadable.</summary>
    private static Color RingColor(ConfigEntry<string> entry, Color fallback)
    {
        string text = entry?.Value?.Trim();

        if (string.IsNullOrEmpty(text))
        {
            return fallback;
        }

        // The parser wants "#" before hex and nothing before a color name.
        if (ColorUtility.TryParseHtmlString(text, out Color parsed)
            || ColorUtility.TryParseHtmlString("#" + text, out parsed))
        {
            parsed.a = 1f;
            return parsed;
        }

        if (_unreadableColor != text)
        {
            _unreadableColor = text;
            Log.LogWarning($"\"{text}\" is not a color. Ring colors are six hex digits, as in FF2020. Using the default.");
        }

        return fallback;
    }

    internal static void Bind(ConfigFile config)
    {
        Migrate(config);

        Enabled = config.Bind(SecGeneral, "Enabled", true,
            Server("Enables mod functionality."));

        Verbosity = config.Bind(SecGeneral, "Verbosity", Log.InfoLevel.Low,
            Local("How much this mod writes to the log."));
        Log.Verbosity = Verbosity;

        ShowHints = config.Bind(SecGeneral, "Show Hints", true,
            Local("Show this mod's control hints."));

        StationRange = config.Bind(SecRules, "Station Range", 1f,
            Server("Multiplier on crafting station build range.",
                new AcceptableValueRange<float>(0.25f, 5f)));
        ExtensionRange = config.Bind(SecRules, "Extension Range", 1f,
            Server("Multiplier on allowed extension distance from the station.",
                new AcceptableValueRange<float>(0.25f, 5f)));
        BaseRange = config.Bind(SecRules, "Base Range", 1f,
            Server("Multiplier on bases safe range.",
                new AcceptableValueRange<float>(0.25f, 5f)));
        EverythingSupports = config.Bind(SecRules, "Everything Supports", false,
            Server("Allows all pieces to support pieces above it."));
        FurnitureSupports = config.Bind(SecRules, "Furniture Supports", false,
            Server("Allows base game furniture to support pieces above it."));
        IgnoreClipping = config.Bind(SecRules, "Ignore Clipping", false,
            Server("Allows pieces to be built inside of others."));
        IgnoreNoBuildZones = config.Bind(SecRules, "Ignore No-Build Zones", false,
            Server("Allows building inside locations that forbid it."));
        IgnoreBiomeLimits = config.Bind(SecRules, "Ignore Biome Limits", false,
            Server("Allows biome-specific pieces to be built in any biome."));
        GuardUsed = config.Bind(SecRules, "Guard Used", true,
            Server("Blocks removing pieces another player is using. Off is vanilla."));

        ToggleModeKey = config.Bind(SecKeys, "Build Mode Toggle",
            new KeyboardShortcut(KeyCode.B, KeyCode.LeftShift),
            Local("Enter or leave Build/Edit mode."));

        ToggleCameraKey = config.Bind(SecKeys, "Camera Toggle",
            new KeyboardShortcut(KeyCode.B, KeyCode.LeftAlt),
            Local("Turn the build camera on or off while in a mode."));

        CameraDownKey = config.Bind(SecKeys, "Camera Down", KeyCode.LeftControl,
            Local("Lower the camera."));

        CameraUpKey = config.Bind(SecKeys, "Camera Up", KeyCode.Space,
            Local("Raise the camera."));

        CopyPieceButton = config.Bind(SecKeys, "Copy Button", KeyCode.Mouse2,
            Local("Held with a copy modifier to copy the targeted piece."));

        CopyPieceIgnoreRotationModifier = config.Bind(SecKeys, "Copy Modifier, No Rotation", KeyCode.LeftAlt,
            Local("Copy the targeted piece, keeping your current rotation."));

        CopyPieceModifier = config.Bind(SecKeys, "Copy Modifier, With Rotation", KeyCode.LeftShift,
            Local("Copy the targeted piece and its rotation."));

        CancelEditKey = config.Bind(SecKeys, "Edit Cancel", KeyCode.Mouse1,
            Local("Put a held piece back where it came from."));

        NudgeBackKey = config.Bind(SecKeys, "Nudge Back", KeyCode.DownArrow,
            Local("Move the piece closer."));

        NudgeForwardKey = config.Bind(SecKeys, "Nudge Forward", KeyCode.UpArrow,
            Local("Move the piece away."));

        NudgeLeftKey = config.Bind(SecKeys, "Nudge Left", KeyCode.LeftArrow,
            Local("Move the piece left."));

        NudgeRightKey = config.Bind(SecKeys, "Nudge Right", KeyCode.RightArrow,
            Local("Move the piece right."));

        ToggleNudgeKey = config.Bind(SecKeys, "Nudge Toggle", KeyCode.N,
            Local("Freeze the piece so the nudge keys move it."));

        NudgeVerticalModifier = config.Bind(SecKeys, "Nudge Vertical Modifier", KeyCode.LeftShift,
            Local("Hold with Forward/Back to nudge up and down."));

        CopyRotationKey = config.Bind(SecKeys, "Rotation Copy", KeyCode.C,
            Local("Take the rotation of the targeted piece."));

        ResetRotationKey = config.Bind(SecKeys, "Rotation Reset", KeyCode.V,
            Local("Return the piece to unrotated."));

        RotateSpinModifier = config.Bind(SecKeys, "Rotation Spin Modifier", KeyCode.LeftAlt,
            Local("Hold while scrolling to spin the piece in the plane you face."));

        RotateTiltModifier = config.Bind(SecKeys, "Rotation Tilt Modifier", KeyCode.LeftShift,
            Local("Hold while scrolling to tilt the piece toward or away."));

        CycleSnapModeKey = config.Bind(SecKeys, "Snap Mode Cycle", KeyCode.T,
            Local("Cycle Auto, Manual, Grid and Off."));

        TargetSnapModifier = config.Bind(SecKeys, "Snap Target Modifier", KeyCode.LeftShift,
            Local("Hold to cycle snap points on the targeted piece."));

        // Not in the hint bar; G is unbound in the game's defaults.
        SnapResetKey = config.Bind(SecKeys, "Snap Point Reset", KeyCode.G,
            Local("In Manual, set both snap points back to automatic."));

        DecreaseSpeedKey = config.Bind(SecKeys, "Step Rate Decrease", KeyCode.Minus,
            Local("Smaller rotation and nudge steps."));

        IncreaseSpeedKey = config.Bind(SecKeys, "Step Rate Increase", KeyCode.Equals,
            Local("Larger rotation and nudge steps."));

        BigSpeedStepModifier = config.Bind(SecKeys, "Step Rate Large Modifier", KeyCode.LeftShift,
            Local("Hold to change the step rate in larger jumps."));

        CameraMoveSpeed = config.Bind(SecTools, "Camera Move Speed", 6f,
            Local("Camera speed, in meters per second.",
                new AcceptableValueRange<float>(1f, 60f)));

        CameraCollidesWithTerrain = config.Bind(SecTools, "Camera Terrain Collision", true,
            Server("Stop the camera passing through the ground."));

        RotateAroundWorldAxes = config.Bind(SecTools, "Rotation World Axes", false,
            Local("Rotate around world axes instead of the piece's own."));

        RotationStepDegrees = config.Bind(SecTools, "Rotation Step", 5f,
            Local("Degrees per scroll notch at step rate 1.",
                new AcceptableValueRange<float>(0.1f, 45f)));

        ShowGizmo = config.Bind(SecTools, "Gizmo Visible", true,
            Local("Show the three rotation rings."));

        GizmoFitsPiece = config.Bind(SecTools, "Gizmo Fits Piece", true,
            Local("Scale the rings to enclose the piece."));

        ShowSnapPoints = config.Bind(SecSnapping, "Show Snap Points", false,
            Local("In a mode, mark the snap points of the held piece and the piece you aim at."));

        GizmoScale = config.Bind(SecTools, "Gizmo Scale", 1f,
            Local("Multiplies the ring size.",
                new AcceptableValueRange<float>(0.1f, 5f)));

        GizmoColorX = config.Bind(SecTools, "Gizmo Color X", "FF2020",
            Local("Color of the pitch ring, as hex."));

        GizmoColorY = config.Bind(SecTools, "Gizmo Color Y", "20FF20",
            Local("Color of the yaw ring, as hex."));

        GizmoColorZ = config.Bind(SecTools, "Gizmo Color Z", "2020FF",
            Local("Color of the roll ring, as hex."));

        GizmoColorX.SettingChanged += (_, _) => _ringColorX = null;
        GizmoColorY.SettingChanged += (_, _) => _ringColorY = null;
        GizmoColorZ.SettingChanged += (_, _) => _ringColorZ = null;

        NudgeStepMeters = config.Bind(SecTools, "Nudge Step", 0.05f,
            Local("Meters per nudge at step rate 1.",
                new AcceptableValueRange<float>(0.01f, 1f)));

        GridSize = config.Bind(SecTools, "Grid Size", 0.5f,
            Local("Grid spacing in meters for Grid snap mode.",
                new AcceptableValueRange<float>(0.5f, 2f)));

        SkillGain = config.Bind(SecProgression, "Skill Gain", 1f,
            Server("How fast the Building skill levels; 2 is twice as fast.",
                new AcceptableValueRange<float>(0.1f, 5f)));
        DeathLoss = config.Bind(SecProgression, "Death Loss", 1f,
            Server("Share of the game's death penalty the Building skill takes; 0 keeps it. Unused while it fortifies.",
                new AcceptableValueRange<float>(0f, 1f)));
        FortifyBuilding = config.Bind(SecProgression, "Fortify Building", false,
            Server("The Building skill fortifies through Mimir, so a death drops it at most a few levels. Off leaves it to Mimir's Fortify setting."));

        EnableLongerReach = config.Bind(SecAbilities, "Longer Reach", true,
            Server("Increased build reach with skill increase."));
        StrongerPieces = config.Bind(SecAbilities, "Stronger Pieces", true,
            Server("Increased piece health and support with skill increase."));
        CheaperSwings = config.Bind(SecAbilities, "Cheaper Swings", true,
            Server("Decreased stamina usage with skill increase."));
        RepairUpgrades = config.Bind(SecAbilities, "Repair Upgrades", true,
            Server("Repairing a piece raises its health and support to your current skill."));

        HammersCall = config.Bind(SecAbilities, "Hammers Call", true,
            Server("Build mode: gizmo, nudging, snapping and wider reach."));
        HammersCallLevel = config.Bind(SecProgression, "Hammers Call Level", Unlocks.BuildLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        GatheredHoard = config.Bind(SecAbilities, "Gathered Hoard", true,
            Server("Building takes missing materials from chests within reach."));
        GatheredHoardLevel = config.Bind(SecProgression, "Gathered Hoard Level", Unlocks.ContainerLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        HuginnsCounsel = config.Bind(SecAbilities, "Huginns Counsel", true,
            Server("Edit mode: pick up built pieces and place them again."));
        HuginnsCounselLevel = config.Bind(SecProgression, "Huginns Counsel Level", Unlocks.EditLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        EirsTouch = config.Bind(SecAbilities, "Eirs Touch", true,
            Server("One repair swing repairs every damaged piece nearby."));
        EirsTouchLevel = config.Bind(SecProgression, "Eirs Touch Level", Unlocks.SweepingRepairLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        RavensView = config.Bind(SecAbilities, "Ravens View", true,
            Server("A build camera that leaves your character to work around the workbench."));
        RavensViewLevel = config.Bind(SecProgression, "Ravens View Level", Unlocks.CameraLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        LonghouseStores = config.Bind(SecAbilities, "Longhouse Stores", true,
            Server("Chests in a workbench's range supply building."));
        LonghouseStoresLevel = config.Bind(SecProgression, "Longhouse Stores Level", Unlocks.CameraLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        BifrostStep = config.Bind(SecAbilities, "Bifrost Step", true,
            Server("The build camera can move from one workbench to the next."));
        BifrostStepLevel = config.Bind(SecProgression, "Bifrost Step Level", Unlocks.HoppingLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        RavensReach = config.Bind(SecAbilities, "Ravens Reach", true,
            Server("Pieces are placed where the build camera is."));
        RavensReachLevel = config.Bind(SecProgression, "Ravens Reach Level", Unlocks.TopLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        RootsOfTheHall = config.Bind(SecAbilities, "Roots of the Hall", true,
            Server("Workbenches linked by overlapping ranges share their chests."));
        RootsOfTheHallLevel = config.Bind(SecProgression, "Roots of the Hall Level", Unlocks.NetworkLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        DvergrThrift = config.Bind(SecAbilities, "Dvergr Thrift", true,
            Server("Materials of removed pieces go to chests or your inventory."));
        DvergrThriftLevel = config.Bind(SecProgression, "Dvergr Thrift Level", Unlocks.SalvageLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
    }

    /// <summary>
    ///     Moves values saved by 1.0.0 to where 1.1.0 keeps them, before anything is bound: ability levels and Skill Gain to
    ///     Progression, the skill effect switches to Abilities, and the later sections one number down.
    /// </summary>
    private static void Migrate(ConfigFile config)
    {
        PropertyInfo property = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");

        if (property?.GetValue(config) is not Dictionary<ConfigDefinition, string> saved)
        {
            return;
        }

        foreach (ConfigDefinition old in saved.Keys.ToList())
        {
            string section = MigratedSection(old);

            if (section == null)
            {
                continue;
            }

            ConfigDefinition moved = new(section, old.Key);

            if (!saved.ContainsKey(moved))
            {
                saved[moved] = saved[old];
            }

            saved.Remove(old);
        }
    }

    /// <summary>The 1.1.0 section of a value saved by 1.0.0, or null when it has not moved.</summary>
    private static string MigratedSection(ConfigDefinition saved)
    {
        string section = saved.Section;

        switch (section)
        {
            case "1. General" when saved.Key == "Skill Gain":
                return SecProgression;
            case "1. General" when saved.Key is "Longer Reach" or "Stronger Pieces" or "Cheaper Swings" or "Repair Upgrades":
                return SecAbilities;
            case "2. Abilities" when saved.Key.EndsWith(" Level", StringComparison.Ordinal):
                return SecProgression;
            case "3. Keybinds":
                return SecKeys;
            case "4. Tools":
                return SecTools;
            case "5. Snapping":
                return SecSnapping;
            case "6. Building Rules":
                return SecRules;
        }

        // Per piece sections: "7. Per Piece Configs", "7.1 Per Piece Configs: Valheim", "7.2 Per Piece Configs: <mod>".
        bool perPiece = section.StartsWith("7. Per Piece Configs", StringComparison.Ordinal)
                        || section.StartsWith("7.1 Per Piece Configs", StringComparison.Ordinal)
                        || section.StartsWith("7.2 Per Piece Configs", StringComparison.Ordinal);
        return perPiece ? "8" + section.Substring(1) : null;
    }

    /// <summary>Synced from the server and locked for non-admins.</summary>
    internal static ConfigDescription Server(string description, AcceptableValueBase range = null)
    {
        return new ConfigDescription(description + " " + SyncedTag, range,
            new ConfigurationManagerAttributes { IsAdminOnly = true });
    }

    /// <summary>Per player, with config manager attributes.</summary>
    internal static ConfigDescription Local(string description, AcceptableValueBase range = null)
    {
        return new ConfigDescription(description + " " + LocalTag, range, new ConfigurationManagerAttributes());
    }
}

