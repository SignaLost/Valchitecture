using UnityEngine;
using Valchitecture.Common;
using Valchitecture.Core;

namespace Valchitecture.Modules.BuildCamera;

/// <summary>
///     The detached build camera, unlocked by the Building skill's camera ability and toggled with the camera key in a mode.
///     It flies with the movement keys and looks like the character's camera, held within the character's camera range
///     or the working station's leash, short of where the character's zone would unload, and optionally sliding along
///     terrain (the Camera Terrain Collision setting).
/// </summary>
internal static class BuildCam
{
    private static readonly int TerrainMask = LayerMask.GetMask("terrain");

    /// <summary>Distance kept from where the character's zone would unload.</summary>
    private const float ZoneMargin = 20f;

    /// <summary>How far inside a station's leash the camera is held.</summary>
    private const float LeashSlack = 0.01f;

    private const int SlideIterations = 3;
    private const float SkinWidth = 0.02f;

    private static float _yaw;
    private static float _pitch;
    private static Vector3 _lastSafePosition;
    private static bool _hasSafePosition;

    private static int _discReachFor = -1;
    private static float _discReach;

    internal static void OnEnterMode(Player player)
    {
        Quaternion eye = player.m_eye ? player.m_eye.rotation : player.transform.rotation;
        float pitch = eye.eulerAngles.x;

        _pitch = pitch > 180f ? pitch - 360f : pitch;
        _yaw = eye.eulerAngles.y;

        if (GameCamera.instance)
        {
            _lastSafePosition = GameCamera.instance.transform.position;
            _hasSafePosition = true;
        }
    }

    internal static void OnExitMode()
    {
        _hasSafePosition = false;
        CameraLight.Release();
    }

    internal static void UpdateCamera(float dt, GameCamera camera)
    {
        Player player = Player.m_localPlayer;

        if (!player || !camera)
        {
            return;
        }

        Transform view = camera.transform;

        // Must run before the input gate.
        CameraLight.Update(view);

        if (Hud.IsPieceSelectionVisible() || InputGate.UiIsCapturingInput())
        {
            return;
        }

        Vector3 movement = ReadMovementInput(player, dt);

        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(view.up, Vector3.up);
        }

        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        movement = right * movement.x + Vector3.up * movement.y + forward * movement.z;

        float ceiling = SafeRange();
        float earned = Mathf.Min(Unlocks.CameraRange(), ceiling);
        Vector3 anchor = player.transform.position + Vector3.up;

        Vector3 wanted = ClampToRange(anchor, view.position + movement, ceiling, earned);
        Vector3 next = Cfg.CameraCollidesWithTerrain.Value ? ClampToTerrain(view.position, wanted) : wanted;

        // Terrain sliding can push slightly past the limit.
        view.position = ClampToRange(anchor, next, ceiling, earned);
        view.rotation = UpdateLook(dt);
    }

    private static Vector3 ReadMovementInput(Player player, float dt)
    {
        Vector3 movement = Vector3.zero;

        if (ZInput.GetButton("Left")) movement -= Vector3.right;
        if (ZInput.GetButton("Right")) movement += Vector3.right;
        if (ZInput.GetButton("Forward")) movement += Vector3.forward;
        if (ZInput.GetButton("Backward")) movement -= Vector3.forward;
        if (Input.GetKey(Cfg.CameraUpKey.Value)) movement += Vector3.up;
        if (Input.GetKey(Cfg.CameraDownKey.Value)) movement -= Vector3.up;

        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        movement += Vector3.right * ZInput.GetJoyLeftStickX();
        movement -= Vector3.forward * ZInput.GetJoyLeftStickY();
        movement.y *= Cfg.CameraVerticalFactor;

        float speed = Cfg.CameraMoveSpeed.Value;

        if (ZInput.GetButton("Run") || ZInput.GetButton("JoyRun"))
        {
            speed *= SprintMultiplier(player);
        }

        return movement * (speed * dt);
    }

    /// <summary>The character's run-to-walk speed ratio, without the Run skill or encumbrance.</summary>
    private static float SprintMultiplier(Player player)
    {
        if (!player || player.m_speed <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp(player.m_runSpeed / player.m_speed, 1f, 10f);
    }

    /// <summary>Mouse and stick look, read as <c>PlayerController.LateUpdate</c> reads the character's.</summary>
    private static Quaternion UpdateLook(float dt)
    {
        Vector2 mouse = Vector2.zero;

        if (ZInput.IsMouseActive())
        {
            float mouseSens = ZInput.IsGamepadMouseActive() ? PlayerController.m_switchMouseSens : PlayerController.m_mouseSens;
            mouse = ZInput.GetMouseDelta() * mouseSens;

            if (PlayerController.m_invertMouse)
            {
                mouse.y *= -1f;
            }
        }

        float stickX = PlayerController.m_invertCameraX ? -1f : 1f;
        float stickY = PlayerController.m_invertCameraY ? -1f : 1f;

        // The game's stick rate, 110 degrees per second.
        float stick = 110f * dt * PlayerController.m_gamepadSens;

        _yaw += mouse.x + stickX * ZInput.GetJoyRightStickX() * stick;
        _pitch -= mouse.y - stickY * ZInput.GetJoyRightStickY() * stick;
        _pitch = Mathf.Clamp(_pitch, -89f, 89f);

        return Quaternion.Euler(0f, _yaw, 0f) * Quaternion.Euler(_pitch, 0f, 0f);
    }

    /// <summary>Maximum camera distance before the character's zone would unload.</summary>
    private static float SafeRange()
    {
        if (!ZNet.instance || !ZoneSystem.instance)
        {
            return 0f;
        }

        SimulationDistance simulation = ZNet.instance.GetSyncedSimulationDistance();
        int near = simulation.NearSimulationDistance;
        float zones = simulation.IsClassic ? near : DiscReach(near);

        return Mathf.Max(0f, zones * ZoneSystem.instance.m_zoneSize - ZoneMargin);
    }

    /// <summary>
    ///     The shortest distance, in zones, that can reach an offset the game does not load
    ///     (sqrt(a² + b²) &gt;= near + 0.5); cached for the last distance asked.
    /// </summary>
    private static float DiscReach(int near)
    {
        if (near == _discReachFor)
        {
            return _discReach;
        }

        _discReachFor = near;
        _discReach = ComputeDiscReach(near);
        return _discReach;
    }

    private static float ComputeDiscReach(int near)
    {
        float rejected = near + 0.5f;
        float closest = float.MaxValue;

        for (int a = 0; a <= near + 2; a++)
        {
            for (int b = 0; b <= near + 2; b++)
            {
                if (Mathf.Sqrt(a * a + b * b) < rejected)
                {
                    continue;
                }

                float x = Mathf.Max(0, a - 1);
                float y = Mathf.Max(0, b - 1);
                closest = Mathf.Min(closest, Mathf.Sqrt(x * x + y * y));
            }
        }

        return closest;
    }

    /// <summary>Clamps to the union of the character's camera range and the working station's leash, then to the zone limit.</summary>
    private static Vector3 ClampToRange(Vector3 anchor, Vector3 position, float ceiling, float earned)
    {
        Vector3 nearest = Hold(anchor, position, earned, earned);

        CraftingStation station = BuildAnchor.Working(position);

        if (station)
        {
            float leash = BuildAnchor.LeashOf(station) - LeashSlack;
            Vector3 viaStation = Hold(station.transform.position, position, leash, BuildAnchor.CeilingOf(station));

            if ((viaStation - position).sqrMagnitude < (nearest - position).sqrMagnitude)
            {
                nearest = viaStation;
            }
        }

        return Hold(anchor, nearest, ceiling, ceiling);
    }

    /// <summary>Clamps a point into a vertical cylinder around <paramref name="center" />.</summary>
    private static Vector3 Hold(Vector3 center, Vector3 position, float radius, float rise)
    {
        Vector3 offset = position - center;
        float height = offset.y;
        offset.y = 0f;

        float flatSquared = offset.sqrMagnitude;

        if (flatSquared > radius * radius && flatSquared > 1e-6f)
        {
            offset *= radius / Mathf.Sqrt(flatSquared);
        }

        offset.y = Mathf.Clamp(height, -rise, rise);
        return center + offset;
    }

    /// <summary>Moves toward the wanted position, sliding along terrain instead of stopping at it.</summary>
    private static Vector3 ClampToTerrain(Vector3 current, Vector3 wanted)
    {
        float clearance = Cfg.CameraTerrainClearance;
        Vector3 remaining = wanted - current;

        if (remaining.sqrMagnitude <= 1e-6f)
        {
            return current;
        }

        Vector3 position = current;

        for (int pass = 0; pass < SlideIterations; pass++)
        {
            float distance = remaining.magnitude;

            if (distance <= 1e-4f)
            {
                break;
            }

            Vector3 direction = remaining / distance;

            if (!Physics.SphereCast(position, clearance, direction, out RaycastHit hit, distance, TerrainMask, QueryTriggerInteraction.Ignore))
            {
                position += remaining;
                break;
            }

            float travelled = Mathf.Max(0f, hit.distance - SkinWidth);
            position += direction * travelled;
            remaining = Vector3.ProjectOnPlane(direction * (distance - travelled), hit.normal);
        }

        if (Physics.CheckSphere(position, clearance, TerrainMask, QueryTriggerInteraction.Ignore))
        {
            if (_hasSafePosition && !Physics.CheckSphere(_lastSafePosition, clearance, TerrainMask, QueryTriggerInteraction.Ignore))
            {
                return _lastSafePosition;
            }

            return current;
        }

        _lastSafePosition = position;
        _hasSafePosition = true;
        return position;
    }
}
