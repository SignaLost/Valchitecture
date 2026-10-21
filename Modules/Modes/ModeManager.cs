using UnityEngine;
using Valchitecture.Modules.BuildCamera;
using Valchitecture.Common.Configuration;
using Valchitecture.Modules.Gizmo;
using Valchitecture.Modules.Skill;
using Valchitecture.Modules.Snapping;
using Valchitecture.Modules.UI;
using Valchitecture.Common;

namespace Valchitecture.Modules.Modes;

internal enum BuildMode
{
    Off,
    Build,
    Edit
}

/// <summary>Build and Edit mode state: entry, exit and per-frame validation.</summary>
internal static class ModeManager
{
    internal static BuildMode Current { get; private set; } = BuildMode.Off;

    internal static bool Active => Current != BuildMode.Off;
    internal static bool IsBuild => Current == BuildMode.Build;
    internal static bool IsEdit => Current == BuildMode.Edit;

    private static bool _cameraDetached;
    private static ItemDrop.ItemData _tool;

    /// <summary>The player's build camera toggle, kept for the session.</summary>
    internal static bool BuildCameraWanted { get; private set; } = true;

    /// <summary>Keeps the camera attached until toggled; set when a mode is entered below level 100 with no station available.</summary>
    private static bool _heldOff;

    /// <summary>Whether the build camera is detached; updated each frame in <see cref="Validate" />.</summary>
    internal static bool CameraDetached => Active && _cameraDetached;

    /// <summary>Whether the build camera may detach now, ignoring the player's toggle.</summary>
    internal static bool BuildCameraAvailable(Player player)
    {
        if (!Unlocks.CanDetachCamera())
        {
            return false;
        }

        Vector3 camera = GameCamera.instance ? GameCamera.instance.transform.position : player.transform.position;
        return Unlocks.CanBuildFromCamera() || BuildAnchor.StationAvailable(player, camera);
    }

    /// <summary>True while the player is positioning the game's placement ghost.</summary>
    internal static bool GhostDrivesPlacement => IsBuild || EditSession.IsGhostDriven;

    /// <summary>The mode for the hammer's selection: Repair is Edit, Remove is none, anything else is Build.</summary>
    internal static BuildMode AvailableMode(Player player)
    {
        if (!player || player.IsDead() || !player.InPlaceMode() || !player.m_buildPieces)
        {
            return BuildMode.Off;
        }

        Piece selected = player.m_buildPieces.GetSelectedPiece();

        if (!selected)
        {
            return BuildMode.Off;
        }

        if (selected.m_repairPiece)
        {
            return BuildMode.Edit;
        }

        return selected.m_removePiece ? BuildMode.Off : BuildMode.Build;
    }

    internal static void HandleToggleInput(Player player)
    {
        if (InputGate.UiIsCapturingInput() || !Cfg.ToggleModeKey.Value.IsDown())
        {
            return;
        }

        if (Active)
        {
            Exit();
            return;
        }

        BuildMode wanted = AvailableMode(player);

        if (wanted == BuildMode.Off || !Unlocks.CanEnter(wanted))
        {
            return;
        }

        Enter(wanted, player);
    }

    /// <summary>Build camera toggle; only active in a mode.</summary>
    internal static void HandleCameraToggle()
    {
        if (!Active || InputGate.UiIsCapturingInput() || !Cfg.ToggleCameraKey.Value.IsDown())
        {
            return;
        }

        if (_heldOff)
        {
            _heldOff = false;
            BuildCameraWanted = true;
        }
        else
        {
            BuildCameraWanted = !BuildCameraWanted;
        }

        NotifyMinor(Names.Localize(BuildCameraWanted ? Tokens.CameraOn : Tokens.CameraOff));
    }

    /// <summary>Exits, or switches between Build and Edit, as the tool or selection changes.</summary>
    internal static void Validate(Player player)
    {
        if (!Active)
        {
            return;
        }

        if (!Cfg.Enabled.Value)
        {
            Exit();
            return;
        }

        EditSession.ReleaseIfLost();

        if (!player || player != Player.m_localPlayer || player.IsDead() || player.GetRightItem() != _tool)
        {
            Exit();
            return;
        }

        ValidateCamera(player);

        // A held piece selects its own entry instead of Repair.
        if (EditSession.IsGhostDriven)
        {
            return;
        }

        BuildMode available = AvailableMode(player);

        if (available == Current)
        {
            return;
        }

        if (available == BuildMode.Off || !Unlocks.CanEnter(available))
        {
            Exit();
            return;
        }

        SwitchTo(available);
    }

    /// <summary>Attaches or detaches the camera as the toggle or station availability changes.</summary>
    private static void ValidateCamera(Player player)
    {
        bool available = BuildCameraAvailable(player);
        bool detached = BuildCameraWanted && !_heldOff && available;

        if (detached == _cameraDetached)
        {
            return;
        }

        _cameraDetached = detached;

        if (detached)
        {
            BuildAnchor.Bind(player.transform.position);
            BuildCam.OnEnterMode(player);
        }
        else
        {
            BuildCam.OnExitMode();
        }
    }

    private static void SwitchTo(BuildMode mode)
    {
        EditSession.OnExitMode();
        Current = mode;

        Manipulator.OnEnterMode();
        EditSession.OnEnterMode();
        GizmoDisplay.Hide();
        KeyHintController.OnModeChanged();

        Announce(mode);
    }

    private static void Enter(BuildMode mode, Player player)
    {
        Current = mode;
        _tool = player.GetRightItem();

        if (player.IsCrouching())
        {
            player.SetCrouch(false);
        }

        BuildAnchor.Bind(player.transform.position);

        bool available = BuildCameraAvailable(player);
        _heldOff = !available && Unlocks.CanDetachCamera() && !Unlocks.CanBuildFromCamera();
        _cameraDetached = BuildCameraWanted && available;

        if (_cameraDetached)
        {
            BuildCam.OnEnterMode(player);
        }

        Reach.Widen(player);

        player.m_manualSnapPoint = -1;
        SnapController.ClearTarget();

        Manipulator.AdoptVanillaRotation(player);
        Manipulator.OnEnterMode();
        EditSession.OnEnterMode();
        KeyHintController.OnModeChanged();

        Announce(mode);

        if (Log.Shows(Log.InfoLevel.Medium))
        {
            Log.LogInfo($"Entered {mode} mode.", Log.InfoLevel.Medium);
        }
    }

    private static void Announce(BuildMode mode)
    {
        Notify(Names.Localize(mode == BuildMode.Build ? Tokens.ModeBuild : Tokens.ModeEdit));
    }

    internal static void Exit()
    {
        if (!Active)
        {
            return;
        }

        Current = BuildMode.Off;
        _cameraDetached = false;
        _heldOff = false;
        _tool = null;

        EditSession.OnExitMode();
        Manipulator.OnExitMode();
        BuildCam.OnExitMode();
        BuildAnchor.Unbind();
        Reach.Restore(Player.m_localPlayer);
        SnapController.ClearTarget();
        GizmoDisplay.Hide();
        KeyHintController.OnModeChanged();
    }

    /// <summary>Center message, like the game's placement messages.</summary>
    internal static void Notify(string message)
    {
        Show(MessageHud.MessageType.Center, message);
    }

    /// <summary>Top-left message, for minor feedback.</summary>
    internal static void NotifyMinor(string message)
    {
        Show(MessageHud.MessageType.TopLeft, message);
    }

    /// <summary>Shows a message through <c>Player.Message</c>.</summary>
    private static void Show(MessageHud.MessageType type, string message)
    {
        if (Player.m_localPlayer)
        {
            Player.m_localPlayer.Message(type, message);
        }
    }

    /// <summary>The ghost being positioned, or null while the game hides it.</summary>
    internal static Transform ActiveTarget(Player player)
    {
        if (!GhostDrivesPlacement || !player || !player.m_placementGhost)
        {
            return null;
        }

        return player.m_placementGhost.activeInHierarchy ? player.m_placementGhost.transform : null;
    }

    /// <summary>The ghost, including while the game hides it.</summary>
    internal static Transform Ghost(Player player)
    {
        return GhostDrivesPlacement && player && player.m_placementGhost ? player.m_placementGhost.transform : null;
    }
}
