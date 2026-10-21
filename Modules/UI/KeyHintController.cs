using System.Collections.Generic;
using UnityEngine;
using Valchitecture.Common.Configuration;
using Valchitecture.Modules.Modes;
using Valchitecture.Modules.Skill;
using Valchitecture.Modules.Snapping;
using Valchitecture.Common;

namespace Valchitecture.Modules.UI;

/// <summary>Replaces the game's build hints with this mod's while a mode is active.</summary>
internal static class KeyHintController
{
    private const string TemplateHolderName = "ValchitectureHintTemplate";
    private const string RowPrefix = "VA_";

    private const string RowEnter = "VA_Enter";
    private const string RowPlace = "VA_Place";
    private const string RowSecondary = "VA_Secondary";
    private const string RowBuildMenu = "VA_BuildMenu";
    private const string RowCamera = "VA_Camera";
    private const string RowStep = "VA_Step";
    private const string RowRotate = "VA_Rotate";
    private const string RowNudge = "VA_Nudge";
    private const string RowNudgeMove = "VA_NudgeMove";
    private const string RowSnap = "VA_Snap";
    private const string RowSnapPoints = "VA_SnapPoints";
    private const string RowCopy = "VA_Copy";

    private static Transform _keyboardRoot;
    private static GameObject _template;
    private static GameObject _buildHints;
    private static KeyHints _owner;
    private static KeyHints _failedOn;
    private static bool _scrollCapAdopted;

    private static readonly List<GameObject> _vanillaRows = new();
    private static readonly HashSet<GameObject> _hiddenVanillaRows = new();

    /// <summary>Every row built, for hiding and destroying.</summary>
    private static readonly List<HintRow> _rows = new();

    private static HintRow _enter, _place, _secondary, _buildMenu, _camera, _step, _rotate;
    private static HintRow _nudge, _nudgeMove, _snap, _snapPoints, _copy;

    private static bool _built;
    private static bool _vanillaHidden;
    private static bool _needsTextRefresh = true;

    // Key caps per row.
    private static string[] _kToggleMode, _kPlace, _kRemove, _kCancel, _kBuildMenu, _kCamera, _kStep, _kRotate;
    private static string[] _kNudge, _kNudgeMove, _kHeightMove, _kSnapCycle, _kSnapPoints, _kCopy;

    /// <summary>Builds the mod's rows under the game's hint bar; <paramref name="retry" /> rebuilds even after a failure on this instance.</summary>
    internal static void Build(KeyHints keyHints, bool retry = false)
    {
        if (!keyHints || (keyHints == _failedOn && !retry) || (_built && _keyboardRoot && _owner == keyHints))
        {
            return;
        }

        Reset();
        _owner = keyHints;
        _failedOn = keyHints;

        _buildHints = ResolveBuildHints(keyHints);

        if (!_buildHints)
        {
            Log.LogWarning("Could not find the vanilla build hints; control hints disabled.");
            DumpChildren(keyHints.transform, "KeyHints");
            return;
        }

        _keyboardRoot = ResolveKeyboardRoot(_buildHints);

        if (!_keyboardRoot)
        {
            Log.LogWarning("Could not find the keyboard hint container; control hints disabled.");
            DumpChildren(_buildHints.transform, "BuildHints");
            return;
        }

        foreach (Transform child in _keyboardRoot)
        {
            _vanillaRows.Add(child.gameObject);
        }

        GameObject source = FindTemplateSource();

        if (!source)
        {
            Log.LogWarning("Could not find a hint element to clone; control hints disabled.");
            DumpChildren(_keyboardRoot, "BuildHints/Keyboard");
            return;
        }

        _template = Object.Instantiate(source, keyHints.transform, false);
        _template.name = TemplateHolderName;
        _template.SetActive(false);

        CreateRows();
        AdoptScrollCap();

        _built = true;
        _failedOn = null;
        _needsTextRefresh = true;

        SnapController.OnSnapModeChanged -= OnSnapModeChanged;
        SnapController.OnSnapModeChanged += OnSnapModeChanged;
    }

    /// <summary>Copies the game's rotate hint images as the scroll cap.</summary>
    private static void AdoptScrollCap()
    {
        Transform rotateRow = FindRow("rotate", "$hud_rotate");
        List<GameObject> icons = rotateRow ? ContentsOf(rotateRow) : null;

        if (icons == null || icons.Count == 0)
        {
            Log.LogWarning("Could not find what the game draws on its rotate hint; the scroll key falls back to the word.");
            DumpChildren(_keyboardRoot, "BuildHints/Keyboard");
            return;
        }

        if (_rotate != null)
        {
            _rotate.AdoptCap(0, icons);
            _scrollCapAdopted = true;
        }
    }

    /// <summary>Finds a game hint row by name fragment or label, matching the token or its translation.</summary>
    private static Transform FindRow(string nameFragment, string labelToken)
    {
        string translated = Names.Localize(labelToken)?.Trim();

        foreach (Transform candidate in CandidateRows())
        {
            if (candidate.name.ToLowerInvariant().Contains(nameFragment))
            {
                return candidate;
            }

            Transform labelTransform = candidate.Find("Text");
            TMPro.TMP_Text label = labelTransform ? labelTransform.GetComponent<TMPro.TMP_Text>() : null;

            if (!label || string.IsNullOrEmpty(label.text))
            {
                continue;
            }

            string text = label.text.Trim();

            if (text == labelToken
                || (!string.IsNullOrEmpty(translated) && !translated.StartsWith("$")
                    && text.Equals(translated, System.StringComparison.OrdinalIgnoreCase)))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>A hint row's visible children other than its label.</summary>
    private static List<GameObject> ContentsOf(Transform row)
    {
        List<GameObject> contents = new();

        foreach (Transform child in row)
        {
            if (child && child.name != "Text" && child.gameObject.activeSelf)
            {
                contents.Add(child.gameObject);
            }
        }

        return contents;
    }

    /// <summary>The game's rows: descendants of the keyboard root with a label.</summary>
    private static IEnumerable<Transform> CandidateRows()
    {
        if (!_keyboardRoot)
        {
            yield break;
        }

        foreach (Transform descendant in _keyboardRoot.GetComponentsInChildren<Transform>(true))
        {
            if (descendant && descendant != _keyboardRoot && !descendant.name.StartsWith(RowPrefix) && descendant.Find("Text"))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>Logs a hierarchy's children.</summary>
    private static void DumpChildren(Transform root, string label)
    {
        if (!root)
        {
            return;
        }

        List<string> names = new();

        foreach (Transform child in root)
        {
            names.Add(child.name);
        }

        Log.LogWarning($"{label} children: {(names.Count > 0 ? string.Join(", ", names) : "(none)")}");
    }

    private static GameObject ResolveBuildHints(KeyHints keyHints)
    {
        if (keyHints.m_buildHints)
        {
            return keyHints.m_buildHints;
        }

        Transform found = keyHints.transform.Find("BuildHints");
        return found ? found.gameObject : null;
    }

    private static Transform ResolveKeyboardRoot(GameObject buildHints)
    {
        if (buildHints.TryGetComponent(out UIInputHint inputHint) && inputHint.m_mouseKeyboardHint)
        {
            return inputHint.m_mouseKeyboardHint.transform;
        }

        return buildHints.transform.Find("Keyboard");
    }

    /// <summary>Any vanilla row with both a label and a key cap.</summary>
    private static GameObject FindTemplateSource()
    {
        Transform preferred = _keyboardRoot.Find("AltPlace");

        if (preferred && IsUsableTemplate(preferred))
        {
            return preferred.gameObject;
        }

        foreach (Transform child in _keyboardRoot)
        {
            if (IsUsableTemplate(child))
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static bool IsUsableTemplate(Transform candidate)
    {
        if (!candidate.Find("Text"))
        {
            return false;
        }

        foreach (Transform child in candidate)
        {
            if (child.name.StartsWith("key_bkg") && child.Find("Key"))
            {
                return true;
            }
        }

        return false;
    }

    private static void CreateRows()
    {
        _place = AddRow(RowPlace, keyCount: 1);
        _secondary = AddRow(RowSecondary, keyCount: 1);
        _buildMenu = AddRow(RowBuildMenu, keyCount: 1);
        _camera = AddRow(RowCamera, keyCount: 3);
        _step = AddRow(RowStep, keyCount: 2);

        _enter = AddRow(RowEnter, keyCount: 3);
        PlaceEnterRow();

        // Sibling order is display order.
        _snap = AddRow(RowSnap, keyCount: 1);
        _snapPoints = AddRow(RowSnapPoints, keyCount: 3);
        _nudge = AddRow(RowNudge, keyCount: 1);
        _nudgeMove = AddRow(RowNudgeMove, keyCount: 5);
        _copy = AddRow(RowCopy, keyCount: 3);
        _rotate = AddRow(RowRotate, keyCount: 3);
    }

    /// <summary>Creates a row under the keyboard root; null when the clone failed.</summary>
    private static HintRow AddRow(string key, int keyCount)
    {
        HintRow row = HintRow.Create(_template, _keyboardRoot, key, keyCount);

        if (!row.IsValid)
        {
            return null;
        }

        _rows.Add(row);
        return row;
    }

    private static void OnSnapModeChanged()
    {
        _needsTextRefresh = true;
    }

    internal static void OnModeChanged()
    {
        _needsTextRefresh = true;
    }

    /// <summary>Forces the text to be rewritten.</summary>
    internal static void MarkDirty()
    {
        _needsTextRefresh = true;
    }

    /// <summary>Updates the rows; runs after the game's per-frame hint update.</summary>
    internal static void UpdateHints(KeyHints keyHints)
    {
        if (!_built || !_keyboardRoot)
        {
            Build(keyHints);

            if (!_built)
            {
                return;
            }
        }

        bool shouldShow = ModeManager.Active
                          && Cfg.ShowHints.Value
                          && _buildHints && _buildHints.activeSelf
                          && !InputGate.UiIsCapturingInput()
                          && !Hud.IsPieceSelectionVisible();

        if (!shouldShow)
        {
            if (_vanillaHidden)
            {
                RestoreVanillaRows();
                HideAllRows();
                _vanillaHidden = false;
            }

            RefreshEnterRow();
            return;
        }

        HideVanillaRows();

        if (!_vanillaHidden)
        {
            _vanillaHidden = true;
            _needsTextRefresh = true;
        }

        RefreshRows();
    }

    /// <summary>Hides the game's active rows, remembering which.</summary>
    private static void HideVanillaRows()
    {
        foreach (GameObject row in _vanillaRows)
        {
            if (row && row.activeSelf)
            {
                row.SetActive(false);
                _hiddenVanillaRows.Add(row);
            }
        }
    }

    private static void RestoreVanillaRows()
    {
        foreach (GameObject row in _hiddenVanillaRows)
        {
            if (row)
            {
                row.SetActive(true);
            }
        }

        _hiddenVanillaRows.Clear();
    }

    private static void PlaceEnterRow()
    {
        if (_enter == null || !_enter.IsValid)
        {
            return;
        }

        Transform buildMenu = FindRow("buildmenu", "$hud_buildmenu");

        if (buildMenu && buildMenu.parent == _keyboardRoot)
        {
            _enter.SetSiblingIndex(buildMenu.GetSiblingIndex() + 1);
        }
    }

    /// <summary>The mode entry hint on the game's bar, shown only for an unlocked mode.</summary>
    private static void RefreshEnterRow()
    {
        if (_enter == null)
        {
            return;
        }

        if (_needsTextRefresh)
        {
            CacheKeyStrings();
        }

        bool offered = Cfg.ShowHints.Value && !ModeManager.Active && !Hud.IsPieceSelectionVisible() && !InputGate.UiIsCapturingInput();
        BuildMode available = offered ? ModeManager.AvailableMode(Player.m_localPlayer) : BuildMode.Off;

        if (available == BuildMode.Off || !Unlocks.CanEnter(available))
        {
            _enter.SetVisible(false);
            return;
        }

        _enter.SetLabel(available == BuildMode.Edit ? Tokens.HintEditMode : Tokens.HintBuildMode);
        _enter.SetKeys(_kToggleMode);
        _enter.SetVisible(true);
    }

    private static void HideAllRows()
    {
        foreach (HintRow row in _rows)
        {
            row.SetVisible(false);
        }
    }

    /// <summary>Rebuilds the key cap strings; game bindings use "$KEY_" tokens.</summary>
    private static void CacheKeyStrings()
    {
        string remove = "$KEY_Remove";
        string nudgeVertical = KeyText.ForKey(Cfg.NudgeVerticalModifier);
        string nudgeForward = KeyText.ForKey(Cfg.NudgeForwardKey);
        string nudgeBack = KeyText.ForKey(Cfg.NudgeBackKey);

        _kToggleMode = KeyText.ForShortcut(Cfg.ToggleModeKey);
        _kPlace = new[] { "$KEY_Attack" };
        _kRemove = new[] { remove };
        _kCancel = new[] { KeyText.ForKey(Cfg.CancelEditKey) };
        _kBuildMenu = new[] { "$KEY_BuildMenu" };
        _kCamera = KeyText.ForShortcut(Cfg.ToggleCameraKey);
        _kStep = new[] { KeyText.ForKey(Cfg.DecreaseSpeedKey), KeyText.ForKey(Cfg.IncreaseSpeedKey) };

        // Null leaves the adopted scroll cap in place.
        _kRotate = new[] { _scrollCapAdopted ? null : Tokens.KeyScroll, KeyText.ForKey(Cfg.RotateTiltModifier), KeyText.ForKey(Cfg.RotateSpinModifier) };

        _kNudge = new[] { KeyText.ForKey(Cfg.ToggleNudgeKey) };
        _kNudgeMove = new[] { KeyText.ForKey(Cfg.NudgeLeftKey), KeyText.ForKey(Cfg.NudgeRightKey), nudgeForward, nudgeBack, nudgeVertical };
        _kHeightMove = new[] { nudgeVertical, nudgeForward, nudgeBack };
        _kSnapCycle = new[] { KeyText.ForKey(Cfg.CycleSnapModeKey) };
        _kSnapPoints = new[] { "$KEY_TabLeft", "$KEY_TabRight", KeyText.ForKey(Cfg.TargetSnapModifier) };
        _kCopy = new[] { KeyText.ForKey(Cfg.CopyPieceModifier), KeyText.ForKey(Cfg.CopyPieceIgnoreRotationModifier), KeyText.ForKey(Cfg.CopyPieceButton) };

        _needsTextRefresh = false;
    }

    private const string RotateLabel = "$hud_rotate / " + Tokens.HintTilt + " / " + Tokens.HintSpin;

    /// <summary>Writes every row's label, keys and visibility.</summary>
    private static void RefreshRows()
    {
        if (_needsTextRefresh)
        {
            CacheKeyStrings();
        }

        bool editing = ModeManager.IsEdit;
        bool holding = EditSession.HasGrabbed;
        bool nudging = Manipulator.NudgeActive;

        SetRow(_place, editing && !holding ? Tokens.HintSelect : "$hud_place", _kPlace);
        SetRow(_secondary, editing && holding ? Tokens.HintCancel : "$hud_remove", editing && holding ? _kCancel : _kRemove);
        SetRow(_buildMenu, "$hud_buildmenu", _kBuildMenu);
        SetRow(_camera, Tokens.HintCamera, _kCamera);
        SetRow(_step, Tokens.HintStep, _kStep);
        SetRow(_rotate, Cfg.RotateAroundWorldAxes.Value ? "$hud_rotate / X / Z" : RotateLabel, _kRotate);

        Transform ghost = ModeManager.Ghost(Player.m_localPlayer);
        bool terrain = Manipulator.IsTerrainTool(ghost);
        bool heightTool = Manipulator.IsHeightTool(ghost);

        SetRow(_nudge, terrain ? Tokens.HintHeightLock : Tokens.HintNudge, _kNudge);
        SetRow(_nudgeMove, terrain ? Tokens.HintRaiseLower : Tokens.HintMove, terrain ? _kHeightMove : _kNudgeMove);
        SetRow(_snap, Tokens.HintSnapMode, _kSnapCycle);
        SetRow(_snapPoints, "$hud_cyclesnap / " + Tokens.HintTarget, _kSnapPoints);
        SetRow(_copy, "$hud_copypiece / " + Tokens.HintWithoutRotation, _kCopy);

        SetVisible(_enter, false);
        SetVisible(_place, true);
        SetVisible(_secondary, true);
        SetVisible(_buildMenu, !editing && !nudging);
        SetVisible(_camera, ModeManager.BuildCameraAvailable(Player.m_localPlayer));
        bool nudgeable = !terrain || heightTool;
        SetVisible(_step, ModeManager.GhostDrivesPlacement && nudgeable);
        SetVisible(_rotate, !ModeManager.GhostDrivesPlacement || !Player.m_localPlayer || Player.m_localPlayer.CanRotatePiece());
        SetVisible(_nudge, ModeManager.GhostDrivesPlacement && nudgeable);
        SetVisible(_nudgeMove, nudging);
        bool snappable = ModeManager.GhostDrivesPlacement && !nudging;
        SetVisible(_snap, snappable);
        SetVisible(_snapPoints, snappable && SnapController.Mode == SnapMode.Manual);
        SetVisible(_copy, !holding && !nudging && !terrain);
    }

    private static void SetRow(HintRow row, string label, string[] keys)
    {
        if (row != null)
        {
            row.SetLabel(label);
            row.SetKeys(keys);
        }
    }

    private static void SetVisible(HintRow row, bool visible)
    {
        if (row != null)
        {
            row.SetVisible(visible);
        }
    }

    internal static void Reset()
    {
        SnapController.OnSnapModeChanged -= OnSnapModeChanged;

        foreach (HintRow row in _rows)
        {
            row.Destroy();
        }

        if (_template)
        {
            Object.Destroy(_template);
        }

        RestoreVanillaRows();
        _rows.Clear();
        _enter = _place = _secondary = _buildMenu = _camera = _step = _rotate = null;
        _nudge = _nudgeMove = _snap = _snapPoints = _copy = null;
        _vanillaRows.Clear();
        _keyboardRoot = null;
        _template = null;
        _buildHints = null;
        _owner = null;
        _scrollCapAdopted = false;
        _built = false;
        _vanillaHidden = false;
        _needsTextRefresh = true;
    }
}
