using System.Collections.Generic;
using UnityEngine;
using Valchitecture.Common;

using BuiltinRenderTextureType = UnityEngine.Rendering.BuiltinRenderTextureType;
using CameraEvent = UnityEngine.Rendering.CameraEvent;
using CommandBuffer = UnityEngine.Rendering.CommandBuffer;
using CompareFunction = UnityEngine.Rendering.CompareFunction;
using CullMode = UnityEngine.Rendering.CullMode;
using LightProbeUsage = UnityEngine.Rendering.LightProbeUsage;
using ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage;
using ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode;

namespace Valchitecture.Modules;

/// <summary>
///     The gizmo's three rotation rings around the placement ghost, shown in a mode while the Gizmo Visible setting is on.
///     The rings are arc meshes with the embedded ring shader, drawn by a command buffer after image effects with a
///     depth prime pass and a color pass; they draw over the scene, fade where behind the piece, and brighten the
///     ring being turned.
/// </summary>
internal static class GizmoDisplay
{
    private const float MajorRadius = 0.5f;
    private const float TubeRadius = 0.02f;

    private const int ArcsPerRing = 16;
    private const int StepsPerArc = 3;
    private const int TubeSides = 10;

    // Brightness tiers, and opacity where a ring is behind the piece.
    private const float BrightPicked = 0.85f;
    private const float BrightIdle = 0.70f;
    private const float BrightQuiet = 0.55f;
    private const float BehindPicked = 0.75f;
    private const float BehindIdle = 0.60f;
    private const float BehindQuiet = 0.45f;

    /// <summary>Scale of the ring being turned.</summary>
    private const float PickedScale = 1.12f;

    // After transparent geometry.
    private const int PrimeQueue = 3900;
    private const int ColorQueue = 3901;

    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int BehindAlphaProperty = Shader.PropertyToID("_BehindAlpha");

    private sealed class Ring
    {
        internal Vector3 Axis;
        internal Material Prime;
        internal Material Paint;
        internal readonly List<Renderer> Renderers = new();

        internal void Apply(Color color, float brightness, float behind, float scale = 1f)
        {
            float gain = Cfg.RingBrightness * brightness;
            Paint.SetColor(ColorProperty, new Color(color.r * gain, color.g * gain, color.b * gain, 1f));
            Paint.SetFloat(BehindAlphaProperty, behind);

            foreach (Renderer renderer in Renderers)
            {
                renderer.transform.localScale = Vector3.one * scale;
            }
        }
    }

    private static GameObject _root;
    private static readonly Ring[] _rings = new Ring[3];
    private static readonly List<Object> _owned = new();
    private static bool _loadFailed;

    private static Camera _worldCamera;

    /// <summary>Whether this class turned on the camera's depth texture.</summary>
    private static bool _addedDepth;
    private static CommandBuffer _overlay;
    private static Camera _overlaidOn;

    // Last emphasis applied.
    private static Vector3 _coloredFor;
    private static Vector3 _quietWhenColored;
    private static bool _turningWhenColored;
    private static bool _hasColored;

    internal static void Show(Vector3 position, Quaternion rotation, float scale, Vector3 activeAxis, Vector3 quietAxis, bool turning)
    {
        if (!Cfg.ShowGizmo.Value)
        {
            Hide();
            return;
        }

        if (!EnsureCreated())
        {
            return;
        }

        _root.transform.SetPositionAndRotation(position, rotation);
        _root.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        _root.SetActive(true);

        EnsureDepthTexture();
        ShowOverlay();
        Emphasize(activeAxis, quietAxis, turning);
    }

    internal static void Hide()
    {
        if (_root)
        {
            _root.SetActive(false);
        }

        HideOverlay();
    }

    /// <summary>Reapplies the configured colors.</summary>
    internal static void ApplyColors()
    {
        _hasColored = false;

        if (_root)
        {
            Emphasize(_coloredFor, _quietWhenColored, _turningWhenColored);
        }
    }

    internal static void Destroy()
    {
        HideOverlay();

        if (_overlay != null)
        {
            _overlay.Release();
            _overlay = null;
        }

        if (_root)
        {
            Object.Destroy(_root);
            _root = null;
        }

        ReleaseOwned();

        if (_addedDepth && _worldCamera)
        {
            _worldCamera.depthTextureMode &= ~DepthTextureMode.Depth;
        }

        _addedDepth = false;
        _worldCamera = null;
        _hasColored = false;
    }

    private static bool EnsureCreated()
    {
        if (_root)
        {
            return true;
        }

        if (_loadFailed)
        {
            return false;
        }

        Shader shader = RingShader.Resolve();
        if (!shader)
        {
            _loadFailed = true;
            return false;
        }

        // A scene load destroys the root but not its meshes and materials.
        ReleaseOwned();

        _root = new GameObject("ValchitectureGizmo");
        _rings[0] = BuildRing(shader, Vector3.right);
        _rings[1] = BuildRing(shader, Vector3.up);
        _rings[2] = BuildRing(shader, Vector3.forward);

        BuildOverlay();

        _hasColored = false;
        Emphasize(_coloredFor, _quietWhenColored, false);

        _root.SetActive(false);
        return true;
    }

    private static Ring BuildRing(Shader shader, Vector3 axis)
    {
        Ring ring = new()
        {
            Axis = axis,
            Prime = CreateMaterial(shader, prime: true),
            Paint = CreateMaterial(shader, prime: false)
        };

        Material[] both = { ring.Prime, ring.Paint };

        Vector3 across = Mathf.Abs(axis.y) < 0.9f
            ? Vector3.Cross(axis, Vector3.up).normalized
            : Vector3.Cross(axis, Vector3.right).normalized;
        Vector3 along = Vector3.Cross(axis, across);

        for (int arc = 0; arc < ArcsPerRing; arc++)
        {
            Mesh mesh = BuildArc(axis, across, along, arc);
            _owned.Add(mesh);

            GameObject piece = new($"Ring{Name(axis)}_Arc{arc}");
            piece.transform.SetParent(_root.transform, false);
            piece.AddComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer renderer = piece.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = both;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.allowOcclusionWhenDynamic = false;

            // Drawn only by the command buffer.
            renderer.enabled = false;

            ring.Renderers.Add(renderer);
        }

        return ring;
    }

    private static Mesh BuildArc(Vector3 axis, Vector3 across, Vector3 along, int arc)
    {
        const int Rings = StepsPerArc + 1;
        const int Sides = TubeSides + 1;

        float span = Mathf.PI * 2f / ArcsPerRing;
        float start = span * arc;

        Vector3[] vertices = new Vector3[Rings * Sides];
        Vector3[] normals = new Vector3[Rings * Sides];
        int[] triangles = new int[StepsPerArc * TubeSides * 6];

        for (int i = 0; i < Rings; i++)
        {
            float around = start + span * i / StepsPerArc;
            Vector3 outward = across * Mathf.Cos(around) + along * Mathf.Sin(around);
            Vector3 spine = outward * MajorRadius;

            for (int j = 0; j < Sides; j++)
            {
                float turn = Mathf.PI * 2f * j / TubeSides;
                Vector3 normal = outward * Mathf.Cos(turn) + axis * Mathf.Sin(turn);

                int index = i * Sides + j;
                normals[index] = normal;
                vertices[index] = spine + normal * TubeRadius;
            }
        }

        int next = 0;
        for (int i = 0; i < StepsPerArc; i++)
        {
            for (int j = 0; j < TubeSides; j++)
            {
                int here = i * Sides + j;
                int ahead = here + Sides;

                triangles[next++] = here;
                triangles[next++] = ahead;
                triangles[next++] = here + 1;
                triangles[next++] = here + 1;
                triangles[next++] = ahead;
                triangles[next++] = ahead + 1;
            }
        }

        Mesh mesh = new() { name = $"ValchitectureArc{Name(axis)}{arc}" };
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material CreateMaterial(Shader shader, bool prime)
    {
        Material material = new(shader)
        {
            name = prime ? "ValchitectureRing_Depth" : "ValchitectureRing_Color",
            renderQueue = prime ? PrimeQueue : ColorQueue
        };

        // The prime pass resolves depth; the color pass draws only matching fragments.
        material.SetFloat("_ZTest", (float)(prime ? CompareFunction.LessEqual : CompareFunction.Equal));
        material.SetFloat("_ZWrite", prime ? 1f : 0f);
        material.SetFloat("_Cull", (float)CullMode.Off);
        material.SetFloat("_FrontAlpha", 1f);
        material.SetFloat(BehindAlphaProperty, BehindIdle);

        if (prime)
        {
            material.SetColor(ColorProperty, new Color(0f, 0f, 0f, 0f));
        }

        _owned.Add(material);
        return material;
    }

    /// <summary>Records all prime passes, then all color passes, onto the camera target.</summary>
    private static void BuildOverlay()
    {
        _overlay ??= new CommandBuffer { name = "Valchitecture gizmo" };
        _overlay.Clear();
        _overlay.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

        foreach (Ring ring in _rings)
        {
            foreach (Renderer renderer in ring.Renderers)
            {
                _overlay.DrawRenderer(renderer, ring.Prime, 0, 0);
            }
        }

        foreach (Ring ring in _rings)
        {
            foreach (Renderer renderer in ring.Renderers)
            {
                _overlay.DrawRenderer(renderer, ring.Paint, 0, 0);
            }
        }
    }

    private static void ShowOverlay()
    {
        if (_overlay == null || !_worldCamera || _overlaidOn == _worldCamera)
        {
            return;
        }

        HideOverlay();
        _worldCamera.AddCommandBuffer(CameraEvent.AfterImageEffects, _overlay);
        _overlaidOn = _worldCamera;
    }

    /// <summary>Removes the command buffer from the camera.</summary>
    private static void HideOverlay()
    {
        if (_overlaidOn && _overlay != null)
        {
            _overlaidOn.RemoveCommandBuffer(CameraEvent.AfterImageEffects, _overlay);
        }

        _overlaidOn = null;
    }

    /// <summary>Turns on the camera's depth texture, which the ring shader reads.</summary>
    private static void EnsureDepthTexture()
    {
        if (!_worldCamera)
        {
            _worldCamera = GameCamera.instance ? GameCamera.instance.m_camera : null;
            _addedDepth = false;

            if (!_worldCamera)
            {
                _worldCamera = Camera.main;
            }

            if (!_worldCamera)
            {
                return;
            }
        }

        if ((_worldCamera.depthTextureMode & DepthTextureMode.Depth) == 0)
        {
            _worldCamera.depthTextureMode |= DepthTextureMode.Depth;
            _addedDepth = true;
        }
    }

    /// <summary>Brightens the ring being turned and dims the face-on (spin) ring.</summary>
    private static void Emphasize(Vector3 activeAxis, Vector3 quietAxis, bool turning)
    {
        if (_hasColored && _coloredFor == activeAxis && _quietWhenColored == quietAxis && _turningWhenColored == turning)
        {
            return;
        }

        _coloredFor = activeAxis;
        _quietWhenColored = quietAxis;
        _turningWhenColored = turning;
        _hasColored = true;

        foreach (Ring ring in _rings)
        {
            if (ring == null)
            {
                continue;
            }

            if (turning && ring.Axis == activeAxis)
            {
                ring.Apply(ColorFor(ring.Axis), BrightPicked, BehindPicked, PickedScale);
            }
            else if (ring.Axis == quietAxis)
            {
                ring.Apply(ColorFor(ring.Axis), BrightQuiet, BehindQuiet);
            }
            else
            {
                ring.Apply(ColorFor(ring.Axis), BrightIdle, BehindIdle);
            }
        }
    }

    private static Color ColorFor(Vector3 axis)
    {
        if (axis == Vector3.right)
        {
            return Cfg.RingColorX;
        }

        return axis == Vector3.up ? Cfg.RingColorY : Cfg.RingColorZ;
    }

    private static string Name(Vector3 axis)
    {
        return axis == Vector3.right ? "X" : axis == Vector3.up ? "Y" : "Z";
    }

    private static void ReleaseOwned()
    {
        foreach (Object owned in _owned)
        {
            if (owned)
            {
                Object.Destroy(owned);
            }
        }

        _owned.Clear();
        _overlay?.Clear();

        for (int i = 0; i < _rings.Length; i++)
        {
            _rings[i] = null;
        }
    }
}
