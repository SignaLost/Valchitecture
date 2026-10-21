using System.Collections.Generic;
using UnityEngine;
using Valchitecture.Common;
using Valchitecture.Modules;

namespace Valchitecture.Core;

/// <summary>
///     Rotation, nudging and copy for the placement ghost, in Build mode and while an Edit session drives the ghost.
///     The scroll wheel turns the ghost around the vertical, tilt or spin ring of the gizmo; the nudge toggle freezes the
///     ghost so keys move it within reach, or holds a leveling tool's height. One step rate scales rotation and nudge
///     steps, and the mod's copy keys stand in for the game's copy piece.
/// </summary>
internal static class Manipulator
{
    private const float AxisHighlightSeconds = 0.4f;

    private static Quaternion _placeRotation = Quaternion.identity;

    /// <summary>Heading the tilt and spin rings follow.</summary>
    private static float _yaw;

    private static int _speedLevel = 1;
    private static bool _nudgeActive;
    private static bool _heightLock;
    private static bool _hasFrozenPosition;
    private static Vector3 _frozenPosition;
    private static Vector3 _highlightedAxis = Vector3.up;
    private static float _axisHighlightUntil;
    private static bool _swallowRemoveUntilRelease;

    private static readonly Dictionary<KeyCode, float> _nextRepeat = new();
    private static readonly List<Renderer> _renderers = new();

    private static Transform _fitTarget;
    private static float _fitScale = 1f;

    internal static bool NudgeActive => _nudgeActive;

    /// <summary>True while the ghost holds its position instead of following the cursor.</summary>
    internal static bool GhostFrozen => ModeManager.GhostDrivesPlacement && _nudgeActive && _hasFrozenPosition;

    /// <summary>True while a leveling tool follows the cursor at a held height.</summary>
    internal static bool HeightLocked => GhostFrozen && _heightLock;

    internal static Vector3 FrozenPosition => _frozenPosition;

    internal static float RotationStep => Cfg.RotationStepDegrees.Value * _speedLevel;
    internal static float NudgeStep => Cfg.NudgeStepMeters.Value * _speedLevel;

    /// <summary>Absolute rotation to place new pieces at.</summary>
    internal static Quaternion PlaceRotation => _placeRotation;

    /// <summary>Ring frame: world axes when fixed, else the piece's heading only.</summary>
    internal static Quaternion RingFrame =>
        Cfg.RotateAroundWorldAxes.Value ? Quaternion.identity : Quaternion.Euler(0f, _yaw, 0f);

    internal static bool CopyModifierHeld =>
        Input.GetKey(Cfg.CopyPieceModifier.Value) || Input.GetKey(Cfg.CopyPieceIgnoreRotationModifier.Value);

    internal static void OnEnterMode()
    {
        _speedLevel = Cfg.StartingSpeedLevel;
        _nudgeActive = false;
        _hasFrozenPosition = false;
        _highlightedAxis = Vector3.up;
    }

    internal static void OnExitMode()
    {
        _nudgeActive = false;
        _hasFrozenPosition = false;
    }

    /// <summary>Ends nudging once the piece is placed, except for height-locked tools.</summary>
    internal static void OnPiecePlaced()
    {
        if (_heightLock)
        {
            return;
        }

        _nudgeActive = false;
        _hasFrozenPosition = false;
    }

    /// <summary>Starts from a picked-up piece's rotation.</summary>
    internal static void AdoptRotation(Quaternion rotation, Player player)
    {
        _placeRotation = rotation;
        _yaw = YawOf(rotation);
        SyncVanillaRotation(player);
    }

    /// <summary>Starts from the game's placement yaw.</summary>
    internal static void AdoptVanillaRotation(Player player)
    {
        if (!player)
        {
            return;
        }

        _placeRotation = Quaternion.Euler(0f, player.m_placeRotationDegrees * player.m_placeRotation, 0f);
        _yaw = YawOf(_placeRotation);
    }

    /// <summary>Writes the yaw back to the game's rotation step.</summary>
    private static void SyncVanillaRotation(Player player)
    {
        if (player && player.m_placeRotationDegrees != 0)
        {
            player.m_placeRotation = Mathf.RoundToInt(_placeRotation.eulerAngles.y / player.m_placeRotationDegrees);
        }
    }

    internal static void ResetRotation()
    {
        _placeRotation = Quaternion.identity;
        _yaw = 0f;
    }

    internal static void HandleInput(Player player)
    {
        if (Hud.IsPieceSelectionVisible() || Console.IsVisible())
        {
            return;
        }

        HandleSpeedKeys();
        HandleNudgeToggle(player);

        Transform target = ModeManager.ActiveTarget(player);

        _heightLock = _nudgeActive && IsHeightTool(ModeManager.Ghost(player));

        if (!target)
        {
            return;
        }

        HandleRotation(player, target);
        HandleNudge(player, target);
    }

    private static void HandleSpeedKeys()
    {
        int step = Input.GetKey(Cfg.BigSpeedStepModifier.Value) ? Cfg.BigSpeedStep : 1;
        bool up = Input.GetKeyDown(Cfg.IncreaseSpeedKey.Value);
        bool down = Input.GetKeyDown(Cfg.DecreaseSpeedKey.Value);

        if (!up && !down)
        {
            return;
        }

        _speedLevel += (up ? step : 0) - (down ? step : 0);
        _speedLevel = Mathf.Clamp(_speedLevel, 1, Cfg.MaxSpeedLevel);

        ModeManager.NotifyMinor($"{RotationStep:0.##}° / {NudgeStep:0.###}m");
    }

    private static void HandleNudgeToggle(Player player)
    {
        if (!ModeManager.GhostDrivesPlacement || !Input.GetKeyDown(Cfg.ToggleNudgeKey.Value))
        {
            return;
        }

        Transform ghost = ModeManager.Ghost(player);

        if (IsTerrainTool(ghost) && !IsHeightTool(ghost))
        {
            return;
        }

        _nudgeActive = !_nudgeActive;
        _hasFrozenPosition = false;

        if (_nudgeActive && player && player.m_placementGhost)
        {
            _frozenPosition = player.m_placementGhost.transform.position;
            _hasFrozenPosition = true;
        }

        ModeManager.NotifyMinor(Names.Localize(_nudgeActive ? Tokens.NudgeOn : Tokens.NudgeOff));
    }

    private static void HandleRotation(Player player, Transform target)
    {
        if (!player.CanRotatePiece())
        {
            return;
        }

        if (Input.GetKeyDown(Cfg.ResetRotationKey.Value))
        {
            ResetRotation();
            SyncVanillaRotation(player);
        }

        if (Input.GetKeyDown(Cfg.CopyRotationKey.Value))
        {
            CopyRotationFromHover(player);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        int direction = scroll > 0f ? 1 : scroll < 0f ? -1 : 0;

        if (direction == 0)
        {
            return;
        }

        Vector3 axis = CurrentAxis();

        // Terrain mods use modifier plus scroll for radius and shape.
        if (axis != Vector3.up && IsTerrainTool(target))
        {
            return;
        }

        _highlightedAxis = axis;
        _axisHighlightUntil = Time.time + AxisHighlightSeconds;

        float angle = RotationStep * direction;

        Vector3 turned = RingFrame * axis;

        if (!Cfg.RotateAroundWorldAxes.Value && axis != Vector3.up && GameCamera.instance)
        {
            Transform view = GameCamera.instance.transform;
            Vector3 reference = axis == TiltAxis()
                ? Vector3.Cross(Vector3.up, FlatHeading(view))
                : FlatHeading(view);

            if (Vector3.Dot(turned, reference) < 0f)
            {
                angle = -angle;
            }
        }

        _placeRotation = Quaternion.AngleAxis(angle, turned) * _placeRotation;
        SyncVanillaRotation(player);

        if (axis == Vector3.up)
        {
            _yaw += angle;
        }
    }

    /// <summary>Copies the rotation of the game's hovered piece.</summary>
    private static void CopyRotationFromHover(Player player)
    {
        Piece hovered = player.GetHoveringPiece();

        if (!hovered)
        {
            return;
        }

        _placeRotation = hovered.transform.rotation;
        _yaw = YawOf(_placeRotation);
        SyncVanillaRotation(player);

        ModeManager.NotifyMinor(Names.Localize(Tokens.Copied, Names.Of(hovered)));
    }

    /// <summary>Keeps the game from removing on this click; the claim lasts until the button is released.</summary>
    internal static void SwallowRemoveUntilRelease()
    {
        _swallowRemoveUntilRelease = true;
    }

    /// <summary>Sets the game's block-remove flag while claimed or a copy modifier is held. The game clears it every update.</summary>
    internal static void BlockRemoveIfClaimed(Player player)
    {
        if (_swallowRemoveUntilRelease && !RemoveButtonHeld())
        {
            _swallowRemoveUntilRelease = false;
        }

        if (player && (_swallowRemoveUntilRelease || CopyModifierHeld))
        {
            player.m_blockRemove = true;
        }
    }

    private static bool RemoveButtonHeld()
    {
        return Input.GetKey(Cfg.CopyPieceButton.Value) || ZInput.GetButton("Remove") || ZInput.GetButton("JoyRemove");
    }

    /// <summary>The game's copy piece, rebound to the mod's copy keys.</summary>
    internal static bool HandleCopyPiece(Player player)
    {
        if (!player || EditSession.HasGrabbed || _nudgeActive || Hud.IsPieceSelectionVisible() || !CopyModifierHeld || !Input.GetKeyDown(Cfg.CopyPieceButton.Value))
        {
            return false;
        }

        SwallowRemoveUntilRelease();

        bool keepOwnRotation = Input.GetKey(Cfg.CopyPieceIgnoreRotationModifier.Value);
        Quaternion before = _placeRotation;
        float beforeYaw = _yaw;

        // The game reports missing requirements itself.
        if (!player.CopyPiece())
        {
            return true;
        }

        if (keepOwnRotation)
        {
            _placeRotation = before;
            _yaw = beforeYaw;
            SyncVanillaRotation(player);
        }
        else
        {
            AdoptVanillaRotation(player);
        }

        ModeManager.NotifyMinor(Names.Localize(Tokens.Copied, Names.Of(player.GetSelectedPiece())));
        return true;
    }

    /// <summary>Which axis the scroll wheel turns, by held modifier.</summary>
    internal static Vector3 CurrentAxis()
    {
        if (Input.GetKey(Cfg.RotateTiltModifier.Value))
        {
            return TiltAxis();
        }

        return Input.GetKey(Cfg.RotateSpinModifier.Value) ? SpinAxis() : Vector3.up;
    }

    /// <summary>World X on fixed axes, else the horizontal frame axis across the view.</summary>
    internal static Vector3 TiltAxis()
    {
        if (Cfg.RotateAroundWorldAxes.Value)
        {
            return Vector3.right;
        }

        return FacingAxis() == Vector3.right ? Vector3.forward : Vector3.right;
    }

    /// <summary>World Z on fixed axes, else the horizontal frame axis along the view.</summary>
    internal static Vector3 SpinAxis()
    {
        return Cfg.RotateAroundWorldAxes.Value ? Vector3.forward : FacingAxis();
    }

    /// <summary>The frame's horizontal axis closest to the camera's heading: the ring seen face-on.</summary>
    private static Vector3 FacingAxis()
    {
        if (!GameCamera.instance)
        {
            return Vector3.right;
        }

        Vector3 heading = FlatHeading(GameCamera.instance.transform);
        Quaternion frame = RingFrame;

        float facingX = Mathf.Abs(Vector3.Dot(frame * Vector3.right, heading));
        float facingZ = Mathf.Abs(Vector3.Dot(frame * Vector3.forward, heading));

        return facingX >= facingZ ? Vector3.right : Vector3.forward;
    }

    /// <summary>Camera heading on the horizontal plane; its up axis when looking straight down.</summary>
    private static Vector3 FlatHeading(Transform view)
    {
        Vector3 heading = Vector3.ProjectOnPlane(view.forward, Vector3.up);

        if (heading.sqrMagnitude < 1e-4f)
        {
            heading = Vector3.ProjectOnPlane(view.up, Vector3.up);
        }

        return heading.normalized;
    }

    private static void HandleNudge(Player player, Transform target)
    {
        if (!_nudgeActive || !GameCamera.instance)
        {
            return;
        }

        bool vertical = Input.GetKey(Cfg.NudgeVerticalModifier.Value);
        Vector3 forward = FlatHeading(GameCamera.instance.transform);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float step = NudgeStep;

        Vector3 delta = Vector3.zero;

        if (HeldOrRepeating(Cfg.NudgeLeftKey.Value)) delta -= right * step;
        if (HeldOrRepeating(Cfg.NudgeRightKey.Value)) delta += right * step;
        if (HeldOrRepeating(Cfg.NudgeForwardKey.Value)) delta += (vertical ? Vector3.up : forward) * step;
        if (HeldOrRepeating(Cfg.NudgeBackKey.Value)) delta -= (vertical ? Vector3.up : forward) * step;

        if (delta == Vector3.zero)
        {
            return;
        }

        if (!_hasFrozenPosition)
        {
            _frozenPosition = target.position;
            _hasFrozenPosition = true;
        }

        if (_heightLock)
        {
            if (vertical)
            {
                _frozenPosition.y += delta.y;
            }

            return;
        }

        _frozenPosition = WithinReach(player, target, _frozenPosition, delta);
    }

    internal static bool IsTerrainTool(Transform target)
    {
        return target && GhostInfo.IsTerrainTool(target);
    }

    /// <summary>Leveling and smoothing tools.</summary>
    internal static bool IsHeightTool(Transform target)
    {
        return target && GhostInfo.IsHeightTool(target);
    }

    /// <summary>Refuses nudges out of reach; nudging closer is always allowed.</summary>
    private static Vector3 WithinReach(Player player, Transform target, Vector3 from, Vector3 delta)
    {
        Vector3 proposed = from + delta;

        if (!player)
        {
            return proposed;
        }

        Piece piece = target ? GhostInfo.PieceOf(target) : null;
        float extra = piece ? piece.m_extraPlacementDistance : 0f;
        Vector3 origin = Reach.Eye(player);

        bool inReach = Reach.InReach(player, proposed, piece) && Reach.Allows(player, proposed, extra);

        return inReach || Vector3.Distance(origin, proposed) < Vector3.Distance(origin, from) ? proposed : from;
    }

    /// <summary>True on the press, then at a steady rate while held.</summary>
    private static bool HeldOrRepeating(KeyCode key)
    {
        if (key == KeyCode.None)
        {
            return false;
        }

        if (Input.GetKeyDown(key))
        {
            _nextRepeat[key] = Time.time + Cfg.NudgeRepeatDelay;
            return true;
        }

        if (!Input.GetKey(key))
        {
            _nextRepeat.Remove(key);
            return false;
        }

        if (_nextRepeat.TryGetValue(key, out float due) && Time.time >= due)
        {
            _nextRepeat[key] = Time.time + Cfg.NudgeRepeatRate;
            return true;
        }

        return false;
    }

    /// <summary>Positions the rings on the ghost and highlights the active axis.</summary>
    internal static void UpdateGizmoVisual(Player player)
    {
        Transform target = ModeManager.ActiveTarget(player);

        if (!target || IsTerrainTool(target) || (ModeManager.GhostDrivesPlacement && player && !player.CanRotatePiece()))
        {
            GizmoDisplay.Hide();
            return;
        }

        Vector3 active = CurrentAxis();
        bool turning = Time.time < _axisHighlightUntil && _highlightedAxis == active;

        Vector3 quiet = Cfg.RotateAroundWorldAxes.Value ? Vector3.zero : SpinAxis();

        float scale = Cfg.GizmoScale.Value;

        if (Cfg.GizmoFitsPiece.Value)
        {
            scale *= FitScale(target);
        }

        GizmoDisplay.Show(target.position, RingFrame, scale, active, quiet, turning);
    }

    /// <summary>Ring size for the piece, cached per ghost.</summary>
    private static float FitScale(Transform target)
    {
        if (target != _fitTarget)
        {
            _fitTarget = target;
            _fitScale = TryGetVisualBounds(target, out Bounds bounds) ? Mathf.Clamp(bounds.extents.magnitude, 0.5f, 6f) : 1f;
        }

        return _fitScale;
    }

    /// <summary>Renderer bounds in the piece's frame, excluding range rings and ghost-only previews.</summary>
    private static bool TryGetVisualBounds(Transform target, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        Quaternion toPiece = Quaternion.Inverse(target.rotation);

        CircleProjector[] rings = target.GetComponentsInChildren<CircleProjector>(true);
        Transform preview = target.Find("_GhostOnly");

        target.GetComponentsInChildren(false, _renderers);

        foreach (Renderer renderer in _renderers)
        {
            if (!renderer || !renderer.enabled || renderer is TrailRenderer or LineRenderer || IsDecoration(renderer.transform, rings, preview))
            {
                continue;
            }

            // ParticleSystemRenderer is in a module this project does not reference.
            if (renderer.GetType().Name == "ParticleSystemRenderer")
            {
                continue;
            }

            Bounds own = renderer.localBounds;
            Transform part = renderer.transform;

            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 local = own.center + Vector3.Scale(own.extents, new Vector3(
                    (corner & 1) == 0 ? -1f : 1f,
                    (corner & 2) == 0 ? -1f : 1f,
                    (corner & 4) == 0 ? -1f : 1f));
                Vector3 inPiece = toPiece * (part.TransformPoint(local) - target.position);

                if (!found)
                {
                    bounds = new Bounds(inPiece, Vector3.zero);
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(inPiece);
                }
            }
        }

        _renderers.Clear();
        return found;
    }

    private static bool IsDecoration(Transform part, CircleProjector[] rings, Transform preview)
    {
        if (preview && part.IsChildOf(preview))
        {
            return true;
        }

        foreach (CircleProjector ring in rings)
        {
            if (ring && part.IsChildOf(ring.transform))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A rotation's heading; zero when pointing straight up or down.</summary>
    private static float YawOf(Quaternion rotation)
    {
        Vector3 facing = Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up);

        if (facing.sqrMagnitude < 1e-6f)
        {
            facing = Vector3.ProjectOnPlane(rotation * Vector3.up, Vector3.up);
        }

        return facing.sqrMagnitude < 1e-6f ? 0f : Quaternion.LookRotation(facing.normalized, Vector3.up).eulerAngles.y;
    }
}
