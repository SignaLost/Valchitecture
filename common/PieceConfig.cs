using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using Valchitecture.Core;
using Valchitecture.Modules.Snapping.Extra;

namespace Valchitecture.Common;

/// <summary>Per piece snap point and support switches, bound only while Enable Per Piece is on.</summary>
internal static class PieceConfig
{
    internal const string Section = "7. Per Piece Configs";
    private const string VanillaSection = "7.1 Per Piece Configs: Valheim";
    private const string ModSectionPrefix = "7.2 Per Piece Configs: ";
    private const string SnapSuffix = ": Snap Points";
    private const string SupportSuffix = ": Supports";

    private static readonly char[] _invalid = { '=', '\n', '\t', '\\', '"', '\'', '[', ']' };

    private static readonly Dictionary<string, ConfigEntry<bool>> _snap = new();
    private static readonly Dictionary<int, ConfigEntry<bool>> _support = new();
    private static readonly HashSet<string> _bound = new();

    private static ConfigFile _config;
    private static PropertyInfo _savedProperty;

    /// <summary>Set while binding when an entry is not yet in the file.</summary>
    private static bool _unsaved;

    internal static ConfigEntry<bool> Enabled;

    private static bool On => Enabled.Value;

    /// <summary>ModQuery can only attribute prefabs while a world is loaded.</summary>
    private static bool InWorld => ZNetScene.instance;

    /// <summary>Binds the per piece section; must be called after all other sections are bound.</summary>
    internal static void Init(ConfigFile config)
    {
        _config = config;

        Enabled = config.Bind(Section, "Enable Per Piece", false,
            Cfg.Local("Enables toggles for snapping and support per piece."));

        // Must be enabled before prefabs register.
        ModQuery.Enable();

        PieceManager.OnPiecesRegistered += () => BindAll(false);
        Enabled.SettingChanged += OnSwitched;

        // The plugin saves once all sections are bound.
        if (!On)
        {
            ForgetSaved();
        }
    }

    /// <summary>Whether the mod may add and rename snap points on this piece.</summary>
    internal static bool SnapAllowed(GameObject prefab)
    {
        if (!On || !prefab)
        {
            return true;
        }

        Bind(prefab);

        return !_snap.TryGetValue(prefab.name, out ConfigEntry<bool> entry) || entry.Value;
    }

    /// <summary>Whether this prefab is set to support in the local file.</summary>
    internal static bool Supports(int prefab)
    {
        return On && _support.TryGetValue(prefab, out ConfigEntry<bool> entry) && entry.Value;
    }

    internal static List<int> SupportingPieces()
    {
        return On ? _support.Where(pair => pair.Value.Value).Select(pair => pair.Key).ToList() : new List<int>();
    }

    private static void OnSwitched(object sender, EventArgs e)
    {
        if (On)
        {
            BindAll(false);
        }
        else
        {
            RemoveAll();
        }

        SnapConfig.RequestRebuild();
        SupportList.Broadcast();
    }

    /// <summary>
    ///     Binds every unbound piece in the build tables and saves once; with pruneMissing, also drops
    ///     entries for pieces no longer in any build table.
    /// </summary>
    internal static void BindAll(bool pruneMissing)
    {
        if (!On || !InWorld)
        {
            return;
        }

        bool saveOnSet = _config.SaveOnConfigSet;
        _config.SaveOnConfigSet = false;
        int before = _bound.Count;
        _unsaved = false;
        Stopwatch watch = Stopwatch.StartNew();

        try
        {
            foreach (GameObject prefab in PieceTables.AllPieces())
            {
                Bind(prefab);
            }
        }
        finally
        {
            _config.SaveOnConfigSet = saveOnSet;
        }

        long binding = watch.ElapsedMilliseconds;
        int removed = pruneMissing ? ForgetSaved() : 0;

        if (_unsaved || removed > 0)
        {
            _config.Save();
        }

        if (_bound.Count == before && removed == 0)
        {
            return;
        }

        SupportList.Broadcast();

        Log.LogInfo(
            $"Per piece settings: {_snap.Count} snap point and {_support.Count} support switches, " +
            $"{_bound.Count - before} pieces bound, {removed} removed, " +
            $"{binding} ms binding, {watch.ElapsedMilliseconds - binding} ms saving.",
            Log.InfoLevel.Medium);
    }

    /// <summary>Binds both switches of a piece.</summary>
    private static void Bind(GameObject prefab)
    {
        if (!InWorld || _bound.Contains(prefab.name))
        {
            return;
        }

        if (_config.SaveOnConfigSet)
        {
            _config.SaveOnConfigSet = false;

            _unsaved = false;

            try
            {
                Bind(prefab);
            }
            finally
            {
                _config.SaveOnConfigSet = true;
            }

            if (_unsaved)
            {
                _config.Save();
            }

            return;
        }

        _bound.Add(prefab.name);

        string key = Clean(prefab.name);

        if (key.Length == 0)
        {
            return;
        }

        string section = SectionFor(prefab);

        if (SnapPointBuilder.Takes(prefab))
        {
            NoteIfUnsaved(section, key + SnapSuffix);
            ConfigEntry<bool> snap = _config.Bind(section, key + SnapSuffix, true,
                Cfg.Local("Adds extra snap points and names this piece's points. Off leaves it as shipped."));
            snap.SettingChanged += (_, _) => SnapConfig.RequestRebuild();
            ConfigText.Apply(snap);
            _snap[prefab.name] = snap;
        }

        WearNTear wear = prefab.GetComponent<WearNTear>();

        if (wear && !wear.m_supports)
        {
            NoteIfUnsaved(section, key + SupportSuffix);
            ConfigEntry<bool> support = _config.Bind(section, key + SupportSuffix, false,
                Cfg.Server("Allows this piece to support pieces above it."));
            support.SettingChanged += (_, _) => SupportList.Broadcast();
            ConfigText.Apply(support);
            _support[PrefabKey.Of(prefab)] = support;
        }
    }

    private static string SectionFor(GameObject prefab)
    {
        BepInPlugin mod = ModQuery.GetPrefab(prefab.name)?.SourceMod;

        if (mod == null)
        {
            return VanillaSection;
        }

        string name = Clean(mod.Name);
        string id = name.Length > 0 ? name : Clean(mod.GUID);
        return id.Length == 0 ? VanillaSection : ModSectionPrefix + id;
    }

    internal static bool IsPieceSection(string section)
    {
        return section == VanillaSection || section.StartsWith(ModSectionPrefix, StringComparison.Ordinal);
    }

    private static void RemoveAll()
    {
        foreach (ConfigEntryBase entry in _snap.Values.Concat<ConfigEntryBase>(_support.Values))
        {
            _config.Remove(entry.Definition);
        }

        _snap.Clear();
        _support.Clear();
        _bound.Clear();
        ForgetSaved();
        _config.Save();
    }

    /// <summary>Values read from the file but not yet bound. BepInEx exposes these only by reflection.</summary>
    private static Dictionary<ConfigDefinition, string> Saved()
    {
        _savedProperty ??= AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
        return _savedProperty?.GetValue(_config) as Dictionary<ConfigDefinition, string>;
    }

    /// <summary>Flags a save if the file lacks this entry; must be called before binding it.</summary>
    private static void NoteIfUnsaved(string section, string key)
    {
        Dictionary<ConfigDefinition, string> saved = Saved();

        if (saved == null || !saved.ContainsKey(new ConfigDefinition(section, key)))
        {
            _unsaved = true;
        }
    }

    /// <summary>Drops unbound per piece values and returns the count; does not save.</summary>
    private static int ForgetSaved()
    {
        Dictionary<ConfigDefinition, string> saved = Saved();

        if (saved == null)
        {
            return 0;
        }

        List<ConfigDefinition> ours = saved.Keys.Where(key => IsPieceSection(key.Section)).ToList();

        foreach (ConfigDefinition key in ours)
        {
            saved.Remove(key);
        }

        return ours.Count;
    }

    /// <summary>A name BepInEx accepts as a section or key.</summary>
    private static string Clean(string name)
    {
        return new string(name.Where(c => Array.IndexOf(_invalid, c) < 0).ToArray()).Trim();
    }
}
