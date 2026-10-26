using System.Collections.Generic;
using UnityEngine;
using Valchitecture.Common;
using Valchitecture.Core;
using Valchitecture.Modules.Snapping;

using BuiltinRenderTextureType = UnityEngine.Rendering.BuiltinRenderTextureType;
using CameraEvent = UnityEngine.Rendering.CameraEvent;
using CommandBuffer = UnityEngine.Rendering.CommandBuffer;
using CompareFunction = UnityEngine.Rendering.CompareFunction;
using CullMode = UnityEngine.Rendering.CullMode;

namespace Valchitecture.Modules;

/// <summary>
///     Marks the snap points of the held piece and of the piece under the crosshair in a mode, when Show Snap Points is on,
///     in Auto and Manual only; in Auto the target's points show only when the held piece has points of its own.
///     Each point is a small camera-facing bubble, clear in the middle and fading out to a soft white rim, sized by its
///     distance so it reads the same near and far. In Manual, the points stepped to show green. They are drawn after the
///     game's image effects with the snap point shader, so they show through pieces, dimmer where something is in front.
///     A bundle without that shader falls back to the ring shader, with each bubble built from rings of stepped alpha.
/// </summary>
internal static class SnapPointDisplay
{
    // With the ring shader only: rings the bubble is built from inside its rim; more is a smoother fade.
    private const int Bands = 10;

    // Where the rim is brightest, as a share of the bubble's radius, and how opaque it gets there (0 to 1).
    private const float RimAt = 0.85f;
    private const float RimAlpha = 0.45f;

    // How the rim fades toward the center: 1 is an even fade, higher keeps the center clear longer.
    private const float FadePower = 1.6f;

    // How much of a bubble still shows where a piece is in front of it (0 to 1).
    private const float BehindAlpha = 0.5f;

    private static readonly Color Unselected = Color.white;
    private static readonly Color SelectedColor = new(0.45f, 1f, 0.45f, 1f);

    // Bubble width per meter from the camera, within these bounds, in meters.
    private const float WidthPerMeter = 0.018f;
    private const float MinWidth = 0.06f;
    private const float MaxWidth = 0.3f;

    // Segments around each ring.
    private const int Segments = 24;

    private static readonly List<Transform> Points = new();
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int RimAtId = Shader.PropertyToID("_RimAt");
    private static readonly int RimAlphaId = Shader.PropertyToID("_RimAlpha");
    private static readonly int FadePowerId = Shader.PropertyToID("_FadePower");
    private static readonly int FrontAlphaId = Shader.PropertyToID("_FrontAlpha");
    private static readonly int BehindAlphaId = Shader.PropertyToID("_BehindAlpha");

    // One submesh per property block: a single quad with the snap point shader, or one per ring with the ring shader.
    private static Mesh _bubble;
    private static Material _material;
    private static MaterialPropertyBlock[] _white;
    private static MaterialPropertyBlock[] _green;
    private static bool _failed;

    private static CommandBuffer _buffer;
    private static Camera _drawnOn;

    /// <summary>Draws the bubbles for this frame; called after the game has placed the ghost.</summary>
    internal static void Draw(Player player)
    {
        SnapMode mode = SnapController.Mode;

        if (!Cfg.ShowSnapPoints.Value || mode is SnapMode.Grid or SnapMode.Off || !EnsureCreated())
        {
            Hide();
            return;
        }

        Camera camera = GameCamera.instance ? GameCamera.instance.m_camera : Camera.main;

        if (!camera)
        {
            return;
        }

        Attach(camera);
        Points.Clear();
        GameObject ghost = player.m_placementGhost;

        if (ModeManager.GhostDrivesPlacement && ghost && ghost.activeSelf && GhostInfo.PieceOf(ghost) is Piece held && held)
        {
            held.GetSnapPoints(Points);
        }

        // In Auto a target's points only matter when the held piece has points to pair with them.
        Piece target = mode == SnapMode.Manual || Points.Count > 0 ? SnapController.TargetPiece(player) : null;

        if (target)
        {
            target.GetSnapPoints(Points);
        }

        Transform selectedHeld = SnapController.SelectedHeldPoint(player);
        Transform selectedTarget = SnapController.SelectedTargetPoint(target);

        _buffer.Clear();
        _buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

        foreach (Transform point in Points)
        {
            if (point)
            {
                DrawPoint(camera, point.position, point == selectedHeld || point == selectedTarget ? _green : _white);
            }
        }
    }

    /// <summary>Stops drawing, such as on leaving a mode.</summary>
    internal static void Hide()
    {
        if (_drawnOn is null)
        {
            return;
        }

        if (_drawnOn && _buffer != null)
        {
            _drawnOn.RemoveCommandBuffer(CameraEvent.AfterImageEffects, _buffer);
        }

        _buffer?.Clear();
        _drawnOn = null;
    }

    /// <summary>Puts the command buffer on the camera, and turns on the depth texture the shader reads to dim hidden points, as the gizmo does.</summary>
    private static void Attach(Camera camera)
    {
        if (_drawnOn == camera)
        {
            return;
        }

        Hide();
        _buffer ??= new CommandBuffer { name = "Valchitecture snap points" };
        camera.AddCommandBuffer(CameraEvent.AfterImageEffects, _buffer);

        camera.depthTextureMode |= DepthTextureMode.Depth;

        _drawnOn = camera;
    }

    private static void DrawPoint(Camera camera, Vector3 at, MaterialPropertyBlock[] bands)
    {
        Vector3 eye = camera.transform.position;
        float distance = Vector3.Distance(eye, at);
        float width = Mathf.Clamp(distance * WidthPerMeter, MinWidth, MaxWidth);

        // Pulled toward the camera by its radius, so a point on a surface counts as in front of it.
        Vector3 toward = distance > 0.001f ? (eye - at) / distance : Vector3.zero;
        Matrix4x4 matrix = Matrix4x4.TRS(at + toward * (width * 0.5f), camera.transform.rotation, Vector3.one * width);

        for (int band = 0; band < bands.Length; band++)
        {
            _buffer.DrawMesh(_bubble, matrix, _material, band, 0, bands[band]);
        }
    }

    private static bool EnsureCreated()
    {
        if (_bubble && _material)
        {
            return true;
        }

        if (_failed)
        {
            return false;
        }

        if (ModShaders.SnapPoint is Shader snapPoint)
        {
            _material = new Material(snapPoint) { name = "ValchitectureSnapPoint", hideFlags = HideFlags.HideAndDontSave };
            _bubble = BuildQuad();
            _white = new[] { ShaderBlock(Unselected) };
            _green = new[] { ShaderBlock(SelectedColor) };
            return true;
        }

        Shader ring = ModShaders.Ring;

        if (!ring)
        {
            _failed = true;
            return false;
        }

        _material = new Material(ring) { name = "ValchitectureSnapPointRings", hideFlags = HideFlags.HideAndDontSave };

        // Drawn over everything; the shader still dims the parts with a piece in front.
        _material.SetFloat("_ZTest", (float)CompareFunction.Always);
        _material.SetFloat("_ZWrite", 0f);
        _material.SetFloat("_Cull", (float)CullMode.Off);
        _material.SetFloat(FrontAlphaId, 1f);
        _material.SetFloat(BehindAlphaId, BehindAlpha);

        _bubble = BuildBubble();
        _white = Blocks(Unselected);
        _green = Blocks(SelectedColor);
        return true;
    }

    /// <summary>The snap point shader's values for one color; the shader works out the fade per pixel.</summary>
    private static MaterialPropertyBlock ShaderBlock(Color color)
    {
        MaterialPropertyBlock block = new();
        block.SetColor(ColorId, color);
        block.SetFloat(RimAtId, RimAt);
        block.SetFloat(RimAlphaId, RimAlpha);
        block.SetFloat(FadePowerId, FadePower);
        block.SetFloat(FrontAlphaId, 1f);
        block.SetFloat(BehindAlphaId, BehindAlpha);
        return block;
    }

    /// <summary>A quad of width 1 with UVs from corner to corner, for the snap point shader.</summary>
    private static Mesh BuildQuad()
    {
        Mesh mesh = new()
        {
            name = "ValchitectureSnapPointQuad",
            hideFlags = HideFlags.HideAndDontSave,
            vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
            },
            uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) },
            triangles = new[] { 0, 2, 1, 2, 3, 1 }
        };

        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>For the ring shader: one color per ring, its alpha following the fade from the clear center to the rim and past it.</summary>
    private static MaterialPropertyBlock[] Blocks(Color color)
    {
        MaterialPropertyBlock[] blocks = new MaterialPropertyBlock[Bands + 1];

        for (int band = 0; band < blocks.Length; band++)
        {
            blocks[band] = new MaterialPropertyBlock();
            blocks[band].SetColor(ColorId, new Color(color.r, color.g, color.b, AlphaAt(Middle(band))));
        }

        return blocks;
    }

    private static float AlphaAt(float r)
    {
        return r >= 1f
            ? 0f
            : r > RimAt
                ? RimAlpha * Mathf.SmoothStep(1f, 0f, (r - RimAt) / (1f - RimAt))
                : RimAlpha * Mathf.Pow(r / RimAt, FadePower);
    }

    // The first Bands rings share the inside up to the rim evenly; the last runs from the rim to the edge.
    private static float Inner(int band) => band < Bands ? RimAt * band / Bands : RimAt;
    private static float Outer(int band) => band < Bands ? RimAt * (band + 1) / Bands : 1f;
    private static float Middle(int band) => (Inner(band) + Outer(band)) * 0.5f;

    /// <summary>For the ring shader: a flat disc of radius 0.5 split into rings, one submesh each.</summary>
    private static Mesh BuildBubble()
    {
        List<Vector3> vertices = new();
        List<int[]> rings = new();

        for (int band = 0; band < Bands + 1; band++)
        {
            float inner = Inner(band) * 0.5f;
            float outer = Outer(band) * 0.5f;
            int first = vertices.Count;
            int[] triangles = new int[Segments * 6];

            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                Vector3 direction = new(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                vertices.Add(direction * inner);
                vertices.Add(direction * outer);
            }

            for (int i = 0; i < Segments; i++)
            {
                int a = first + i * 2;
                int b = first + (i + 1) % Segments * 2;
                int t = i * 6;
                triangles[t] = a;
                triangles[t + 1] = a + 1;
                triangles[t + 2] = b;
                triangles[t + 3] = b;
                triangles[t + 4] = a + 1;
                triangles[t + 5] = b + 1;
            }

            rings.Add(triangles);
        }

        Mesh mesh = new() { name = "ValchitectureSnapPointBubble", hideFlags = HideFlags.HideAndDontSave };
        mesh.SetVertices(vertices);
        mesh.subMeshCount = rings.Count;

        for (int band = 0; band < rings.Count; band++)
        {
            mesh.SetTriangles(rings[band], band);
        }

        mesh.RecalculateBounds();
        return mesh;
    }
}
