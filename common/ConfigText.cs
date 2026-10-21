using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Configuration;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Valchitecture.Common;

/// <summary>Translates config manager text through ConfigurationManagerAttributes; the .cfg file stays English.</summary>
internal static class ConfigText
{
    private const string Prefix = "valchitecture_cfg_";
    private const string English = "English";

    private static ConfigFile _config;
    private static CustomLocalization _localization;

    /// <summary>Translations for the current language, English included.</summary>
    private static IReadOnlyDictionary<string, string> _words;

    /// <summary>Whether translations changed since the last <see cref="ApplyAll" />.</summary>
    private static bool _dirty;

    /// <summary>Installs the translation handlers; must run from Awake, before Jotunn adds translations.</summary>
    internal static void Init(ConfigFile config)
    {
        _config = config;
        _localization = LocalizationManager.Instance.GetLocalization();
        LocalizationManager.OnLocalizationAdded += MarkDirty;
        Localization.OnLanguageChange += MarkDirty;
    }

    private static void MarkDirty()
    {
        _dirty = true;
    }

    /// <summary>Re-applies every entry once after any number of translation changes; called each frame.</summary>
    internal static void ApplyIfDirty()
    {
        if (_dirty)
        {
            _dirty = false;
            ApplyAll();
        }
    }

    private static void ApplyAll()
    {
        string language = Localization.instance != null ? Localization.instance.GetSelectedLanguage() : English;
        _words = _localization.GetTranslations(language);

        foreach (ConfigDefinition definition in _config.Keys.ToList())
        {
            Apply(_config[definition]);
        }
    }

    /// <summary>Applies translated text to one entry. Also called for entries bound later.</summary>
    internal static void Apply(ConfigEntryBase entry)
    {
        ConfigurationManagerAttributes attributes = entry.Description.Tags.OfType<ConfigurationManagerAttributes>().FirstOrDefault();

        if (attributes == null)
        {
            return;
        }

        if (_words == null)
        {
            // Null falls back to the entry's own text.
            attributes.DispName = null;
            attributes.Description = null;
            attributes.Category = null;
            return;
        }

        string section = entry.Definition.Section;
        string key = entry.Definition.Key;
        string owner = string.Empty;

        // Per piece keys are "prefab: setting".
        if (PieceConfig.IsPieceSection(section))
        {
            int split = key.LastIndexOf(": ", StringComparison.Ordinal);
            owner = key.Substring(0, split + 2);
            key = key.Substring(split + 2);
        }

        string slug = Slug(key);
        attributes.DispName = owner + (Word(slug + "_name") ?? key);
        attributes.Category = Section(section);

        string description = entry.Description.Description;
        bool synced = attributes.IsAdminOnly;
        string tag = synced ? Cfg.SyncedTag : Cfg.LocalTag;

        if (description.EndsWith(tag))
        {
            description = description.Substring(0, description.Length - tag.Length).TrimEnd();
        }

        attributes.Description = (Word(slug) ?? description) + " " + (Word(synced ? "synced" : "not_synced") ?? tag);
    }

    /// <summary>Translates a section name, keeping its number and any ": mod" suffix.</summary>
    private static string Section(string section)
    {
        int space = section.IndexOf(' ');
        string number = section.Substring(0, space + 1);
        string name = section.Substring(space + 1);
        string after = string.Empty;
        int split = name.IndexOf(": ", StringComparison.Ordinal);

        if (split >= 0)
        {
            after = name.Substring(split);
            name = name.Substring(0, split);
        }

        return number + (Word("section_" + Slug(name)) ?? name) + after;
    }

    private static string Word(string slug)
    {
        return _words != null && _words.TryGetValue(Prefix + slug, out string word) && !string.IsNullOrEmpty(word) ? word : null;
    }

    /// <summary>Lowercases text and joins its words with underscores.</summary>
    internal static string Slug(string text)
    {
        StringBuilder slug = new();

        foreach (char c in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                slug.Append(c);
            }
            else if (slug.Length > 0 && slug[slug.Length - 1] != '_')
            {
                slug.Append('_');
            }
        }

        return slug.ToString().TrimEnd('_');
    }
}
