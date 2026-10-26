using System;
using System.IO;
using System.Reflection;
using System.Threading;
using BepInEx;
using Chestworks;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using Mimir;
using Valchitecture.Common;
using Valchitecture.Core;
using Valchitecture.Modules;
using Valchitecture.Modules.HintBar;
using Valchitecture.Modules.Snapping.Extra;
using Valchitecture.Modules.Storage;

namespace Valchitecture;

/// <summary>
///     The BepInEx entry point: Build and Edit modes, a build camera, gizmo rotation, extra snap points and building
///     from nearby containers, all gated by a Building skill kept by Mimir.
///     On load it binds the configuration, registers the skill, translations and console command, and applies each
///     Harmony patch class; each frame it reloads an edited config file and rebuilds snap points after snap setting changes.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
[BepInProcess("valheim.x86_64")]
[BepInProcess("valheim_server.exe")]
[BepInProcess("valheim_server.x86_64")]
[BepInDependency(Jotunn.Main.ModGuid)]
[BepInDependency(ChestworksAPI.Guid)]
[BepInDependency(MimirAPI.Guid)]
// Vanilla servers are not checked; see ServerMod.
[NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
public sealed class ValchitecturePlugin : BaseUnityPlugin
{
    public const string PluginName = "Valchitecture";
    public const string PluginGuid = "NullSignal.Valchitecture";
    public const string PluginVersion = "1.1.1";

    private const string EnglishFile = "valchitecture.json";
    private const string EnglishResource = "Valchitecture.Translations.English.json";

    private readonly Harmony _harmony = new(PluginGuid);
    private FileSystemWatcher _configWatcher;

    /// <summary>Quiet time after the last file change event before a reload.</summary>
    private static readonly TimeSpan ReloadQuiet = TimeSpan.FromSeconds(1);

    /// <summary>UTC ticks after which to reload; 0 when none is pending.</summary>
    private long _reloadDue;

    private void Awake()
    {
        Log.Init(Logger);

        Config.SaveOnConfigSet = false;
        Cfg.Bind(Config);
        SnapConfig.Bind(Config);
        PieceConfig.Init(Config);
        ConfigText.Init(Config);
        Cfg.StationRange.SettingChanged += (_, _) => BuildRules.RefreshStations();
        Cfg.BaseRange.SettingChanged += (_, _) => BuildRules.RefreshStations();
        Cfg.Enabled.SettingChanged += (_, _) => BuildRules.RefreshStations();
        Config.SaveOnConfigSet = true;
        Config.Save();

        SubscribeToConfigChanges();

        RegisterEnglish();
        SupportList.Init();
        BuildingReach.Register();

        // Must be registered before patching.
        BuildingSkill.Register();
        CommandManager.Instance.AddConsoleCommand(new ValchitectureCommand());

        ApplyPatches();

        Game.isModded = true;
        SetUpConfigWatcher();

        Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void Update()
    {
        long due = Interlocked.Read(ref _reloadDue);

        if (due != 0 && DateTime.UtcNow.Ticks >= due && Interlocked.CompareExchange(ref _reloadDue, 0, due) == due)
        {
            ReloadConfig();
        }

        if (SnapConfig.RebuildDue)
        {
            SnapPointBuilder.Rebuild("Snap settings changed; rebuilding snap points.");
        }

        ConfigText.ApplyIfDirty();
    }

    private void OnDestroy()
    {
        ModeManager.Exit();
        GizmoDisplay.Destroy();
        SnapPointDisplay.Hide();
        KeyHintController.Reset();
        AddedSnapPoints.RemoveAll();

        if (_configWatcher != null)
        {
            _configWatcher.EnableRaisingEvents = false;
            _configWatcher.Changed -= OnConfigFileChanged;
            _configWatcher.Created -= OnConfigFileChanged;
            _configWatcher.Renamed -= OnConfigFileRenamed;
            _configWatcher.Dispose();
            _configWatcher = null;
        }

        _harmony?.UnpatchSelf();
        Config.Save();
    }

    /// <summary>Adds the embedded English translations when no <c>Translations/English</c> file is on disk.</summary>
    private void RegisterEnglish()
    {
        string onDisk = Path.Combine(Path.GetDirectoryName(Info.Location) ?? "", "Translations", "English", EnglishFile);

        if (File.Exists(onDisk))
        {
            return;
        }

        byte[] bytes = EmbeddedResources.Read(EnglishResource);

        if (bytes != null)
        {
            LocalizationManager.Instance.GetLocalization().AddJsonFile("English", System.Text.Encoding.UTF8.GetString(bytes));
        }
    }

    /// <summary>Patches each class separately.</summary>
    private void ApplyPatches()
    {
        int applied = 0;
        int failed = 0;

        foreach (Type type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
        {
            try
            {
                PatchClassProcessor processor = _harmony.CreateClassProcessor(type);

                if (processor.Patch() is { Count: > 0 })
                {
                    applied++;
                }
            }
            catch (Exception e)
            {
                failed++;
                Logger.LogError($"Patch failed: {type.Name} - {e.Message}");
            }
        }

        Logger.LogInfo($"Applied {applied} patch classes" + (failed > 0 ? $", {failed} failed." : "."));
    }

    private void SubscribeToConfigChanges()
    {
        Cfg.GizmoColorX.SettingChanged += (_, _) => GizmoDisplay.ApplyColors();
        Cfg.GizmoColorY.SettingChanged += (_, _) => GizmoDisplay.ApplyColors();
        Cfg.GizmoColorZ.SettingChanged += (_, _) => GizmoDisplay.ApplyColors();

        Cfg.ToggleModeKey.SettingChanged += OnHintKeyChanged;
        Cfg.ToggleCameraKey.SettingChanged += OnHintKeyChanged;
        Cfg.RotateTiltModifier.SettingChanged += OnHintKeyChanged;
        Cfg.RotateSpinModifier.SettingChanged += OnHintKeyChanged;
        Cfg.ToggleNudgeKey.SettingChanged += OnHintKeyChanged;
        Cfg.IncreaseSpeedKey.SettingChanged += OnHintKeyChanged;
        Cfg.DecreaseSpeedKey.SettingChanged += OnHintKeyChanged;
        Cfg.RotationStepModifier.SettingChanged += OnHintKeyChanged;
        Cfg.NudgeVerticalModifier.SettingChanged += OnHintKeyChanged;
        Cfg.NudgeLeftKey.SettingChanged += OnHintKeyChanged;
        Cfg.NudgeRightKey.SettingChanged += OnHintKeyChanged;
        Cfg.NudgeForwardKey.SettingChanged += OnHintKeyChanged;
        Cfg.NudgeBackKey.SettingChanged += OnHintKeyChanged;
        Cfg.CycleSnapModeKey.SettingChanged += OnHintKeyChanged;
        Cfg.TargetSnapModifier.SettingChanged += OnHintKeyChanged;
        Cfg.CancelEditKey.SettingChanged += OnHintKeyChanged;
        Cfg.CopyPieceModifier.SettingChanged += OnHintKeyChanged;
        Cfg.CopyPieceIgnoreRotationModifier.SettingChanged += OnHintKeyChanged;
        Cfg.CopyPieceButton.SettingChanged += OnHintKeyChanged;
    }

    private static void OnHintKeyChanged(object sender, EventArgs e)
    {
        KeyHintController.MarkDirty();
    }

    private void SetUpConfigWatcher()
    {
        string fileName = Path.GetFileName(Config.ConfigFilePath);
        string directory = Path.GetDirectoryName(Config.ConfigFilePath);

        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return;
        }

        _configWatcher = new FileSystemWatcher(directory, fileName)
        {
            IncludeSubdirectories = false,
            SynchronizingObject = ThreadingHelper.SynchronizingObject,
            EnableRaisingEvents = true
        };

        _configWatcher.Changed += OnConfigFileChanged;
        _configWatcher.Created += OnConfigFileChanged;
        _configWatcher.Renamed += OnConfigFileRenamed;
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e) => QueueReload();

    private void OnConfigFileRenamed(object sender, RenamedEventArgs e) => QueueReload();

    private void QueueReload()
    {
        Interlocked.Exchange(ref _reloadDue, (DateTime.UtcNow + ReloadQuiet).Ticks);
    }

    private void ReloadConfig()
    {
        if (!File.Exists(Config.ConfigFilePath))
        {
            return;
        }

        try
        {
            Config.Reload();
            Log.LogInfo("Reloaded configuration.", Log.InfoLevel.High);
        }
        catch (Exception e)
        {
            Logger.LogError($"Could not reload the configuration file: {e.Message}");
        }
    }
}
