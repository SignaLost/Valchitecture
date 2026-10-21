using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Entities;
using UnityEngine;
using Valchitecture.Common.Configuration;
using Valchitecture.Modules.Modes;
using Valchitecture.Modules.Snapping;
using Valchitecture.Common;
using Valchitecture.Modules.Snapping.Extra;
using Valchitecture.Modules.Skill;

namespace Valchitecture.Modules.Commands;

/// <summary>The <c>valchitecture &lt;subcommand&gt;</c> console command, readouts only.</summary>
internal sealed class ValchitectureCommand : ConsoleCommand
{
    private const string Rule = "--------------------";

    private static readonly string[] Subcommands = { "help", "stats", "piece", "snap", "world" };

    public override string Name => "valchitecture";

    public override string Help => Names.Localize(Tokens.CmdHelp);

    public override List<string> CommandOptionList()
    {
        return Subcommands.ToList();
    }

    public override void Run(string[] args, Terminal context)
    {
        if (context == null)
        {
            return;
        }

        string sub = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

        switch (sub)
        {
            case "help":
                ShowHelp(context);
                break;
            case "stats":
                ShowStats(context);
                break;
            case "piece":
                ShowPiece(context);
                break;
            case "snap":
                ShowSnap(context);
                break;
            case "world":
                ShowWorld(context);
                break;
            default:
                Out(context, Names.Localize(Tokens.CmdUnknown, sub));
                ShowHelp(context);
                break;
        }
    }

    /// <summary>One message line to the console and the log file.</summary>
    private static void Out(Terminal context, string line)
    {
        context.AddString(line);
        Log.ToFile(line);
    }

    /// <summary>A readout: lines go to the console as added (unless log-only), then to the log file as one titled block.</summary>
    private sealed class Report
    {
        private readonly Terminal _context;
        private readonly string _title;
        private readonly List<string> _log = new();

        internal Report(Terminal context, string title)
        {
            _context = context;
            _title = title;
        }

        internal void Line(string text, bool console = true)
        {
            if (console)
            {
                _context.AddString(text);
            }

            _log.Add(text);
        }

        internal void Field(string labelToken, string value, bool console = true)
        {
            Line($"{Names.Localize(labelToken)}: {value}", console);
        }

        /// <summary>A setting under its config window name, translated when available.</summary>
        internal void Setting(ConfigEntryBase entry, string value)
        {
            ConfigurationManagerAttributes attributes = entry.Description.Tags.OfType<ConfigurationManagerAttributes>().FirstOrDefault();
            Line($"{attributes?.DispName ?? entry.Definition.Key}: {value}");
        }

        internal void Heading(string token, bool console = true)
        {
            HeadingText(Names.Localize(token), console);
        }

        /// <summary>A heading from literal text, for the English log-only sections.</summary>
        internal void HeadingText(string text, bool console)
        {
            if (_log.Count > 0)
            {
                Line(string.Empty, console);
            }

            Line(text + ":", console);
            Line(Rule, console);
        }

        internal void Done()
        {
            Log.ToFile($"{_title} command output:\n\n{string.Join("\n", _log)}\n");
        }
    }

    private static string OnOff(bool on)
    {
        return Names.Localize(on ? Tokens.CmdOn : Tokens.CmdOff);
    }

    private static string TrueFalse(bool value)
    {
        return Names.Localize(value ? Tokens.CmdTrue : Tokens.CmdFalse);
    }

    private static void ShowHelp(Terminal context)
    {
        Out(context, Names.Localize(Tokens.CmdHelpStats));
        Out(context, Names.Localize(Tokens.CmdHelpPiece));
        Out(context, Names.Localize(Tokens.CmdHelpSnap));
        Out(context, Names.Localize(Tokens.CmdHelpWorld));
        Out(context, Names.Localize(Tokens.CmdHelpMimir));
    }

    private static void ShowStats(Terminal context)
    {
        Report report = new(context, "Stats");

        report.Heading(Tokens.CmdHeadingGeneral);
        report.Field(Tokens.CmdCurrentLevel, Number(BuildingSkill.Level, "0.#"));
        report.Field(Tokens.CmdRepairRange, Number(Unlocks.RepairRadius(), "0.#") + "m");
        report.Field(Tokens.CmdCameraDistance, Number(Unlocks.CanDetachCamera() ? Unlocks.CameraRange() : 0f, "0.#") + "m");

        report.Heading(Tokens.CmdHeadingAbilities);

        if (BuildingSkill.Instance != null)
        {
            foreach (Mimir.Ability ability in BuildingSkill.Instance.Abilities)
            {
                string mark = BuildingSkill.Instance.IsUnlocked(ability.Id) ? "[x]" : "[ ]";
                report.Line($"{mark} {Names.Localize(ability.Name)}");
            }
        }

        if (Cfg.Enabled.Value && Cfg.StrongerPieces.Value && !ServerMod.Present)
        {
            report.Line(string.Empty);
            report.Line(Names.Localize(Tokens.CmdStrengthOff));
        }

        report.Done();
    }

    private static void ShowPiece(Terminal context)
    {
        Player player = Player.m_localPlayer;
        Piece piece = player ? player.GetHoveringPiece() : null;

        if (!piece)
        {
            Out(context, Names.Localize(Tokens.CmdNoPiece));
            return;
        }

        Report report = new(context, "Piece");
        report.Field(Tokens.CmdPiece, $"{Names.Localize(piece.m_name)} [{PrefabName(piece)}]");

        if (piece.TryGetComponent(out WearNTear wear))
        {
            float skill = PieceStrength.StoredFactor(wear);

            report.Field(Tokens.CmdHealth, $"{Number(wear.m_health * wear.GetHealthPercentage(), "0")}/{Number(wear.m_health, "0")}");
            report.Field(Tokens.CmdSupport, $"{Number(wear.GetSupport(), "0.#")}/{Number(wear.GetMinSupport(), "0.#")}");
            report.Field(Tokens.CmdBuildLevel, skill > 0f ? Number(skill * 100f, "0.#") : Names.Localize(Tokens.CmdNone));
            report.Field(Tokens.CmdMaterial, wear.m_materialType.ToString());
            report.Field(Tokens.CmdCanSupport, TrueFalse(BuildRules.Supports(wear)));
        }

        report.Done();
    }

    /// <summary>Snap settings and the aimed piece's analysis. The beam test and base points go to the log only.</summary>
    private static void ShowSnap(Terminal context)
    {
        Report report = new(context, "Snap");

        report.Field(Tokens.CmdExtraPointsEnabled, TrueFalse(SnapConfig.EnableExtraSnapPoints.Value));

        report.Heading(Tokens.CmdHeadingGeneral);
        report.Setting(SnapConfig.GridSpacing, Number(SnapConfig.GridSpacing.Value, "0.##"));

        report.Heading(Tokens.CmdHeadingShapeCategories);
        report.Field(Tokens.CmdShapeBox, OnOff(SnapConfig.EnableBoxSnapPoints.Value));
        report.Field(Tokens.CmdShapeTriangle, OnOff(SnapConfig.EnableTriangleSnapPoints.Value));
        report.Field(Tokens.CmdShapeRectangle, OnOff(SnapConfig.EnableRect2DSnapPoints.Value));
        report.Field(Tokens.CmdShapePoleBeam, OnOff(SnapConfig.EnablePoleBeamSnapPoints.Value));
        report.Field(Tokens.CmdShapeRooftop, OnOff(SnapConfig.EnableRoofTopSnapPoints.Value));
        report.Field(Tokens.CmdShapeTerrain, OnOff(SnapConfig.EnableTerrainOpSnapPoints.Value));
        report.Field(Tokens.CmdShapeCustom, OnOff(SnapConfig.EnableHandPlacedSnapPoints.Value));

        report.Heading(Tokens.CmdHeadingPieceInfo);

        Player player = Player.m_localPlayer;
        Piece piece = player ? player.GetHoveringPiece() : null;
        GameObject prefab = piece ? PrefabKey.PrefabOf(piece) : null;

        if (!prefab)
        {
            report.Line(Names.Localize(Tokens.CmdNoPiece));
            report.Done();
            return;
        }

        List<Transform> all = prefab.GetSnapPoints();
        List<Transform> own = all.Where(point => !point.GetComponent<AddedSnapPoint>()).ToList();
        List<string> beamTest = new();
        bool beam = SnapPointGeometry.BeamTest(prefab, own, beamTest);
        SnapPointGeometry.Shape shape = SnapPointGeometry.Classify(own, prefab.IsRoof());
        bool custom = CuratedSnapPoints.For(prefab) != null;

        report.Field(Tokens.CmdPiece, $"{Names.Localize(piece.m_name)} [{prefab.name}]");
        report.Field(Tokens.CmdSnapPoints, Names.Localize(Tokens.CmdSnapPointsValue, own.Count.ToString(), (all.Count - own.Count).ToString()));
        report.Field(Tokens.CmdShape, shape.ToString());
        report.Field(Tokens.CmdEnabled, TrueFalse(PieceConfig.SnapAllowed(prefab)));
        report.Field(Tokens.CmdIsCustom, TrueFalse(custom));
        report.Field(Tokens.CmdIsRoof, TrueFalse(prefab.IsRoof()));
        report.Field(Tokens.CmdIsBeam, TrueFalse(beam && (shape is SnapPointGeometry.Shape.Line or SnapPointGeometry.Shape.Rectangle or SnapPointGeometry.Shape.Unknown)));

        if (custom || own.Count == 0)
        {
            report.Done();
            return;
        }

        report.HeadingText("Beam Test", console: false);

        foreach (string line in beamTest)
        {
            report.Line(line, console: false);
        }

        report.HeadingText("Base Points", console: false);

        foreach (Transform point in own)
        {
            report.Line($"{Names.Localize(point.name)}: {point.localPosition.ToString("F3")}", console: false);
        }

        report.Done();
    }

    /// <summary>The server-synced settings.</summary>
    private static void ShowWorld(Terminal context)
    {
        Report report = new(context, "World");

        report.Field(Tokens.CmdModEnabled, TrueFalse(Cfg.Enabled.Value));

        report.Heading(Tokens.CmdHeadingGeneral);
        report.Setting(Cfg.CameraCollidesWithTerrain, TrueFalse(Cfg.CameraCollidesWithTerrain.Value));
        report.Setting(Cfg.StationRange, Number(Cfg.StationRange.Value, "0.##"));
        report.Setting(Cfg.BaseRange, Number(Cfg.BaseRange.Value, "0.##"));
        report.Setting(Cfg.ExtensionRange, Number(Cfg.ExtensionRange.Value, "0.##"));

        report.Setting(Cfg.SkillGain, Number(Cfg.SkillGain.Value, "0.##"));
        report.Setting(Cfg.CheaperSwings, OnOff(Cfg.CheaperSwings.Value));
        report.Setting(Cfg.EnableLongerReach, OnOff(Cfg.EnableLongerReach.Value));

        report.Heading(Tokens.CmdHeadingAbilities);
        report.Setting(Cfg.HammersCall, OnOff(Cfg.HammersCall.Value));
        report.Setting(Cfg.HammersCallLevel, Cfg.HammersCallLevel.Value.ToString());
        report.Setting(Cfg.GatheredHoard, OnOff(Cfg.GatheredHoard.Value));
        report.Setting(Cfg.GatheredHoardLevel, Cfg.GatheredHoardLevel.Value.ToString());
        report.Setting(Cfg.HuginnsCounsel, OnOff(Cfg.HuginnsCounsel.Value));
        report.Setting(Cfg.HuginnsCounselLevel, Cfg.HuginnsCounselLevel.Value.ToString());
        report.Setting(Cfg.EirsTouch, OnOff(Cfg.EirsTouch.Value));
        report.Setting(Cfg.EirsTouchLevel, Cfg.EirsTouchLevel.Value.ToString());
        report.Setting(Cfg.RavensView, OnOff(Cfg.RavensView.Value));
        report.Setting(Cfg.RavensViewLevel, Cfg.RavensViewLevel.Value.ToString());
        report.Setting(Cfg.LonghouseStores, OnOff(Cfg.LonghouseStores.Value));
        report.Setting(Cfg.LonghouseStoresLevel, Cfg.LonghouseStoresLevel.Value.ToString());
        report.Setting(Cfg.BifrostStep, OnOff(Cfg.BifrostStep.Value));
        report.Setting(Cfg.BifrostStepLevel, Cfg.BifrostStepLevel.Value.ToString());
        report.Setting(Cfg.RavensReach, OnOff(Cfg.RavensReach.Value));
        report.Setting(Cfg.RavensReachLevel, Cfg.RavensReachLevel.Value.ToString());
        report.Setting(Cfg.RootsOfTheHall, OnOff(Cfg.RootsOfTheHall.Value));
        report.Setting(Cfg.RootsOfTheHallLevel, Cfg.RootsOfTheHallLevel.Value.ToString());
        report.Setting(Cfg.DvergrThrift, OnOff(Cfg.DvergrThrift.Value));
        report.Setting(Cfg.DvergrThriftLevel, Cfg.DvergrThriftLevel.Value.ToString());

        report.Heading(Tokens.CmdHeadingPieceSettings);
        report.Setting(Cfg.StrongerPieces, OnOff(Cfg.StrongerPieces.Value));
        report.Setting(Cfg.RepairUpgrades, OnOff(Cfg.RepairUpgrades.Value));
        report.Setting(Cfg.EverythingSupports, TrueFalse(Cfg.EverythingSupports.Value));
        report.Setting(Cfg.FurnitureSupports, TrueFalse(Cfg.FurnitureSupports.Value));

        report.Heading(Tokens.CmdHeadingBuildRestrictions);
        report.Setting(Cfg.IgnoreClipping, TrueFalse(Cfg.IgnoreClipping.Value));
        report.Setting(Cfg.IgnoreNoBuildZones, TrueFalse(Cfg.IgnoreNoBuildZones.Value));
        report.Setting(Cfg.IgnoreBiomeLimits, TrueFalse(Cfg.IgnoreBiomeLimits.Value));
        report.Setting(Cfg.GuardUsed, OnOff(Cfg.GuardUsed.Value));

        if (BuildRules.HeldBack)
        {
            report.Line(string.Empty);
            report.Line(Names.Localize(Tokens.CmdRulesOff));
        }

        report.Done();
    }

    private static string PrefabName(Piece piece)
    {
        GameObject prefab = PrefabKey.PrefabOf(piece);
        return prefab ? prefab.name : piece.gameObject.name;
    }

    /// <summary>Formatted in the player's culture.</summary>
    private static string Number(float value, string format)
    {
        return value.ToString(format, CultureInfo.CurrentCulture);
    }
}
