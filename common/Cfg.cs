using BepInEx.Configuration;
using UnityEngine;
using Valchitecture.Core;

namespace Valchitecture.Common;

/// <summary>Core settings.</summary>
internal static class Cfg
{
    private const string SecGeneral = "1. General";
    internal const string SecAbilityUnlocks = "2. Abilities";
    private const string SecKeys = "3. Keybinds";
    private const string SecTools = "4. Tools";
    internal const string SecSnapping = "5. Snapping";
    private const string SecRules = "6. Building Rules";

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
        Enabled = config.Bind(SecGeneral, "Enabled", true,
            Server("Enables mod functionality."));

        Verbosity = config.Bind(SecGeneral, "Verbosity", Log.InfoLevel.Low,
            Local("How much this mod writes to the log."));
        Log.Verbosity = Verbosity;

        ShowHints = config.Bind(SecGeneral, "Show Hints", true,
            Local("Show this mod's control hints."));

        SkillGain = config.Bind(SecGeneral, "Skill Gain", 1f,
            Server("How fast the Building skill levels; 2 is twice as fast.",
                new AcceptableValueRange<float>(0.1f, 5f)));
        EnableLongerReach = config.Bind(SecGeneral, "Longer Reach", true,
            Server("Increased build reach with skill increase."));
        StrongerPieces = config.Bind(SecGeneral, "Stronger Pieces", true,
            Server("Increased piece health and support with skill increase."));
        CheaperSwings = config.Bind(SecGeneral, "Cheaper Swings", true,
            Server("Decreased stamina usage with skill increase."));
        RepairUpgrades = config.Bind(SecGeneral, "Repair Upgrades", true,
            Server("Repairing a piece raises its health and support to your current skill."));

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

        HammersCall = config.Bind(SecAbilityUnlocks, "Hammers Call", true,
            Server("Build mode: gizmo, nudging, snapping and wider reach."));
        HammersCallLevel = config.Bind(SecAbilityUnlocks, "Hammers Call Level", Unlocks.BuildLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        GatheredHoard = config.Bind(SecAbilityUnlocks, "Gathered Hoard", true,
            Server("Building takes missing materials from chests within reach."));
        GatheredHoardLevel = config.Bind(SecAbilityUnlocks, "Gathered Hoard Level", Unlocks.ContainerLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        HuginnsCounsel = config.Bind(SecAbilityUnlocks, "Huginns Counsel", true,
            Server("Edit mode: pick up built pieces and place them again."));
        HuginnsCounselLevel = config.Bind(SecAbilityUnlocks, "Huginns Counsel Level", Unlocks.EditLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        EirsTouch = config.Bind(SecAbilityUnlocks, "Eirs Touch", true,
            Server("One repair swing repairs every damaged piece nearby."));
        EirsTouchLevel = config.Bind(SecAbilityUnlocks, "Eirs Touch Level", Unlocks.SweepingRepairLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        RavensView = config.Bind(SecAbilityUnlocks, "Ravens View", true,
            Server("A build camera that leaves your character to work around the workbench."));
        RavensViewLevel = config.Bind(SecAbilityUnlocks, "Ravens View Level", Unlocks.CameraLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        LonghouseStores = config.Bind(SecAbilityUnlocks, "Longhouse Stores", true,
            Server("Chests in a workbench's range supply building."));
        LonghouseStoresLevel = config.Bind(SecAbilityUnlocks, "Longhouse Stores Level", Unlocks.CameraLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        BifrostStep = config.Bind(SecAbilityUnlocks, "Bifrost Step", true,
            Server("The build camera can move from one workbench to the next."));
        BifrostStepLevel = config.Bind(SecAbilityUnlocks, "Bifrost Step Level", Unlocks.HoppingLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        RavensReach = config.Bind(SecAbilityUnlocks, "Ravens Reach", true,
            Server("Pieces are placed where the build camera is."));
        RavensReachLevel = config.Bind(SecAbilityUnlocks, "Ravens Reach Level", Unlocks.TopLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        RootsOfTheHall = config.Bind(SecAbilityUnlocks, "Roots of the Hall", true,
            Server("Workbenches linked by overlapping ranges share their chests."));
        RootsOfTheHallLevel = config.Bind(SecAbilityUnlocks, "Roots of the Hall Level", Unlocks.TopLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
        DvergrThrift = config.Bind(SecAbilityUnlocks, "Dvergr Thrift", true,
            Server("Materials of removed pieces go to chests or your inventory."));
        DvergrThriftLevel = config.Bind(SecAbilityUnlocks, "Dvergr Thrift Level", Unlocks.TopLevel,
            Server("Building level that unlocks it.", new AcceptableValueRange<int>(0, 100)));
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

