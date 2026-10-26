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

    /// <summary>The tilt and spin within the heading's frame; the rotation is the heading times this.</summary>
    private static Quaternion _local = Quaternion.identity;

    /// <summary>The 24 orientations made of right angles, which a turn snaps to when it passes close.</summary>
    private static readonly Quaternion[] RightAngles = BuildRightAngles();

    private static readonly List<Transform> _points = new();

    // The game's own rotation step; resetting lands on the nearest of its angles.
    private const float VanillaStep = 22.5f;

    // How closely height must follow one horizontal axis for a piece to count as a ramp, from 0 to 1.
    private const float RampCorrelation = 0.7f;

    // How much narrower one side must be than the other for a piece to count as thin.
    private const float ThinRatio = 0.5f;

    // Every rotation step is a fraction or multiple of the game's own 22.5°, so turning always stays on angles the game
    // itself can reach.
    private static readonly float[] RotationSteps = { 2.8125f, 5.625f, 11.25f, 22.5f, 45f, 90f, 180f };
    private static readonly float[] NudgeSteps = { 0.05f, 0.1f, 0.2f, 0.25f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.75f, 0.8f, 0.9f, 1f };

    // The step rate keys change the nudge step while nudging, else, or with the rotation step modifier held, the rotation step.
    private static int _rotationLevel;
    private static int _nudgeLevel;
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

    internal static float RotationStep => RotationSteps[_rotationLevel];
    internal static float NudgeStep => NudgeSteps[_nudgeLevel];

    /// <summary>Absolute rotation to place new pieces at.</summary>
    internal static Quaternion PlaceRotation => _placeRotation;

    /// <summary>Ring frame: world axes when fixed, else the piece's heading only.</summary>
    internal static Quaternion RingFrame =>
        Cfg.RotateAroundWorldAxes.Value ? Quaternion.identity : Quaternion.Euler(0f, _yaw, 0f);

    internal static bool CopyModifierHeld =>
        Input.GetKey(Cfg.CopyPieceModifier.Value) || Input.GetKey(Cfg.CopyPieceIgnoreRotationModifier.Value);

    internal static void OnEnterMode()
    {
        _rotationLevel = 0;
        _nudgeLevel = 0;
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
        SetRotation(rotation);
        SyncVanillaRotation(player);
    }

    /// <summary>Starts from the game's placement yaw.</summary>
    internal static void AdoptVanillaRotation(Player player)
    {
        if (!player)
        {
            return;
        }

        SetRotation(Quaternion.Euler(0f, player.m_placeRotationDegrees * player.m_placeRotation, 0f));
    }

    /// <summary>Sets the rotation and splits it into the heading and the tilt within it.</summary>
    private static void SetRotation(Quaternion rotation)
    {
        // Rounded onto the finest step's grid, so a rotation copied from a crooked piece turns clean from the first step.
        float fine = RotationSteps[0];
        _yaw = Mathf.Repeat(Mathf.Round(YawOf(rotation) / fine) * fine, 360f);
        _local = SnapToRightAngles(Quaternion.Inverse(Quaternion.Euler(0f, _yaw, 0f)) * rotation, fine * 0.5f + 0.001f);
        _placeRotation = Quaternion.Euler(0f, _yaw, 0f) * _local;
    }

    /// <summary>Writes the yaw back to the game's rotation step.</summary>
    private static void SyncVanillaRotation(Player player)
    {
        if (player && player.m_placeRotationDegrees != 0)
        {
            player.m_placeRotation = Mathf.RoundToInt(_placeRotation.eulerAngles.y / player.m_placeRotationDegrees);
        }
    }

    /// <summary>Clears tilt and spin and turns the piece to the straight heading closest to where the camera faces.</summary>
    internal static void ResetRotation(Player player)
    {
        float heading = GameCamera.instance ? YawOf(Quaternion.LookRotation(FlatHeading(GameCamera.instance.transform), Vector3.up)) : 0f;
        Piece piece = player && player.m_placementGhost ? GhostInfo.PieceOf(player.m_placementGhost) : null;
        float yaw = heading - FrontYaw(piece);
        SetRotation(Quaternion.Euler(0f, Mathf.Repeat(Mathf.Round(yaw / VanillaStep) * VanillaStep, 360f), 0f));
    }

    /// <summary>
    ///     The heading, within the piece, of the side that should face away from the player, read from its snap points:
    ///     the uphill direction of a ramp such as stairs, else the face of a thin piece such as a wall, else its forward.
    /// </summary>
    private static float FrontYaw(Piece piece)
    {
        if (!piece)
        {
            return 0f;
        }

        _points.Clear();
        piece.GetSnapPoints(_points);

        if (_points.Count < 2)
        {
            return 0f;
        }

        Vector3 sum = Vector3.zero;
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        foreach (Transform point in _points)
        {
            Vector3 local = piece.transform.InverseTransformPoint(point.position);
            sum += local;
            min = Vector3.Min(min, local);
            max = Vector3.Max(max, local);
        }

        Vector3 mean = sum / _points.Count;
        float xy = 0f, zy = 0f, xx = 0f, zz = 0f, yy = 0f;

        foreach (Transform point in _points)
        {
            Vector3 d = piece.transform.InverseTransformPoint(point.position) - mean;
            xy += d.x * d.y;
            zy += d.z * d.y;
            xx += d.x * d.x;
            zz += d.z * d.z;
            yy += d.y * d.y;
        }

        // A ramp: height rises steadily along one horizontal axis.
        float xRamp = xx > 1e-4f && yy > 1e-4f ? xy / Mathf.Sqrt(xx * yy) : 0f;
        float zRamp = zz > 1e-4f && yy > 1e-4f ? zy / Mathf.Sqrt(zz * yy) : 0f;

        if (Mathf.Max(Mathf.Abs(xRamp), Mathf.Abs(zRamp)) >= RampCorrelation)
        {
            return Mathf.Abs(zRamp) >= Mathf.Abs(xRamp) ? (zRamp > 0f ? 0f : 180f) : (xRamp > 0f ? 90f : 270f);
        }

        // A thin piece shows its broad side: points spread along X face forward already; along Z they need a quarter turn.
        float width = max.x - min.x;
        float depth = max.z - min.z;
        return width < depth * ThinRatio ? 90f : 0f;
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
        bool up = Input.GetKeyDown(Cfg.IncreaseSpeedKey.Value);
        bool down = Input.GetKeyDown(Cfg.DecreaseSpeedKey.Value);

        if (!up && !down)
        {
            return;
        }

        int change = (up ? 1 : 0) - (down ? 1 : 0);

        if (_nudgeActive && !Input.GetKey(Cfg.RotationStepModifier.Value))
        {
            _nudgeLevel = Mathf.Clamp(_nudgeLevel + change, 0, NudgeSteps.Length - 1);
            ModeManager.NotifyMinor(Names.Localize(Tokens.NudgeStepMessage, $"{NudgeStep:0.##} m"));
        }
        else
        {
            _rotationLevel = Mathf.Clamp(_rotationLevel + change, 0, RotationSteps.Length - 1);
            ModeManager.NotifyMinor(Names.Localize(Tokens.RotationStepMessage, $"{RotationStep:0.#####}°"));
        }
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
            ResetRotation(player);
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

        // Every turn is relative to where the piece is, then lands on the finest step's grid, which holds every angle the
        // game itself uses, so the piece stays on the game's angles and can always be turned straight again.
        float fine = RotationSteps[0];
        float window = fine * 0.5f + 0.001f;

        if (axis == Vector3.up)
        {
            _yaw = Mathf.Repeat(Mathf.Round((_yaw + angle) / fine) * fine, 360f);
            _placeRotation = Quaternion.Euler(0f, _yaw, 0f) * _local;
        }
        else if (Cfg.RotateAroundWorldAxes.Value)
        {
            SetRotation(SnapToRightAngles(Quaternion.AngleAxis(angle, turned) * _placeRotation, window));
        }
        else
        {
            // Tilt and spin turn about the heading's own axes.
            _local = SnapToRightAngles(Quaternion.AngleAxis(angle, axis) * _local, window);
            _placeRotation = Quaternion.Euler(0f, _yaw, 0f) * _local;
        }

        SyncVanillaRotation(player);
    }

    /// <summary>The right-angle orientation within the window of this one, or this one when none is.</summary>
    private static Quaternion SnapToRightAngles(Quaternion rotation, float window)
    {
        Quaternion best = rotation;
        float closest = window;

        foreach (Quaternion straight in RightAngles)
        {
            float angle = Quaternion.Angle(rotation, straight);

            if (angle <= closest)
            {
                best = straight;
                closest = angle;
            }
        }

        return best;
    }

    private static Quaternion[] BuildRightAngles()
    {
        List<Quaternion> found = new();

        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    Quaternion candidate = Quaternion.Euler(x * 90f, y * 90f, z * 90f);

                    if (!found.Exists(each => Quaternion.Angle(each, candidate) < 0.1f))
                    {
                        found.Add(candidate);
                    }
                }
            }
        }

        return found.ToArray();
    }

    /// <summary>Copies the rotation of the game's hovered piece.</summary>
    private static void CopyRotationFromHover(Player player)
    {
        Piece hovered = player.GetHoveringPiece();

        if (!hovered)
        {
            return;
        }

        SetRotation(hovered.transform.rotation);
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

        // The game reports missing requirements itself.
        if (!player.CopyPiece())
        {
            return true;
        }

        if (keepOwnRotation)
        {
            SetRotation(before);
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

        // The way you face, rounded to the game's own 22.5° angles, so nudges follow straight lines through the build.
        float heading = YawOf(Quaternion.LookRotation(FlatHeading(GameCamera.instance.transform), Vector3.up));
        Quaternion facing = Quaternion.Euler(0f, Mathf.Round(heading / VanillaStep) * VanillaStep, 0f);
        Vector3 forward = facing * Vector3.forward;
        Vector3 right = facing * Vector3.right;
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
