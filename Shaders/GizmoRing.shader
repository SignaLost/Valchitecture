// The gizmo rings, drawn in the game's own camera, by one copy of the geometry.
//
// Two problems have to be answered at once, and a stock shader can answer neither:
//
//   Which ring surface wins where two rings cross, or where one folds over itself.
//   That needs the rings resolved against each other, which needs a depth buffer
//   holding nothing but rings — and the game's buffer arrives full of the world.
//
//   Whether a surviving fragment is clear of the piece or inside it. That needs the
//   world's depth, which is the one thing the game's buffer does have.
//
// The trick that lets one buffer serve both is writing a remapped depth. Every ring
// fragment is squeezed into a thin slice at the very front of the depth range,
// keeping its order relative to the other rings but landing nearer than anything in
// the world. Ordinary depth testing then sorts the rings against each other and never
// rejects one for being behind a wall, because as far as the buffer is concerned no
// ring is behind anything. Whether it really is gets decided separately, per
// fragment, by reading the depth texture.
//
// This is used by two materials, not two passes of its own: a shader's passes are
// drawn per object, so a prime pass written here would prime one ring's arc and then
// paint it before the next arc had primed anything.
//
//   prime: ZTest LEqual, ZWrite On,  alpha 0  — paints nothing, settles the depth
//   color: ZTest Equal,  ZWrite Off, alpha 1  — paints only the surface that won
//
// Neither is drawn by the scene. A command buffer draws every ring's prime and then
// every ring's color, once the camera's image effects have finished. Running after
// them is what keeps the rings sharp: depth of field reads a copy of the depth taken
// before anything transparent is drawn, so it never finds a ring there, and blurs each
// one to the distance of whatever shows past it. Running in that order is what the
// render queue used to be for, and the command buffer is more direct about it.
//
// No keywords and no multi_compile: one variant per graphics API, nothing for variant
// stripping to remove, nothing conditional to go missing on a machine whose API was
// not the one the bundle was built on.

Shader "Valchitecture/GizmoRing"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)

        // Set from the mod per ring and per state, not authored here.
        _FrontAlpha  ("Alpha clear of the piece", Range(0, 1)) = 1.0
        _BehindAlpha ("Alpha inside the piece",   Range(0, 1)) = 0.6

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        _ZWrite ("ZWrite", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"           = "Transparent"
            "Queue"                = "Transparent"
            "IgnoreProjector"      = "True"
            "ForceNoShadowCasting" = "True"
        }

        Pass
        {
            Name "Ring"

            ZTest [_ZTest]
            ZWrite [_ZWrite]
            Cull [_Cull]
            Blend SrcAlpha OneMinusSrcAlpha
            Lighting Off
            Fog { Mode Off }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _CameraDepthTexture;

            float4 _Color;
            float  _FrontAlpha;
            float  _BehindAlpha;

            // How much of the depth range the rings are folded into, at the front.
            //
            // Small enough that no real geometry shares it — with a near plane of half
            // a metre, only something within about five millimetres of that plane
            // would reach this far forward, and the near plane has already clipped
            // anything that close. Large enough to keep the rings apart from each
            // other: a reversed depth buffer carries most of its precision up here, so
            // a hundredth of the range is thousands of distinguishable steps, far more
            // than three rings need.
            #define RING_SLICE 0.01

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float4 screen : TEXCOORD0;
                float  eye    : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screen = ComputeScreenPos(o.pos);

                // Distance from the eye along the view direction, which is what the
                // scene's depth linearises to, so the two are comparable without
                // caring how the platform stores depth.
                o.eye = -UnityObjectToViewPos(v.vertex).z;

                return o;
            }

            // Fold a window depth into the slice at the front, keeping its order.
            //
            // Taken from the rasterised position rather than recomputed, so the prime
            // and the color pass cannot arrive at even slightly different values —
            // the color pass tests for equality against what the prime wrote, and
            // slightly different would mean nothing is ever painted.
            float RingDepth(float window)
            {
            #if defined(UNITY_REVERSED_Z)
                // Nearer is larger here, and 1 is the near plane.
                return 1.0 - RING_SLICE * (1.0 - window);
            #else
                // Nearer is smaller, and 0 is the near plane.
                return RING_SLICE * window;
            #endif
            }

            void frag(v2f i, out fixed4 color : SV_Target, out float depth : SV_Depth)
            {
                depth = RingDepth(i.pos.z);

                float2 uv = i.screen.xy / max(i.screen.w, 1e-6);

                float raw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                float sceneEye = LinearEyeDepth(raw);

                // With no depth texture bound, raw reads as the far plane and every
                // fragment counts as clear of the world — the rings go solid rather
                // than invisible, which is a visible, diagnosable failure instead of a
                // silent one.
                float alpha = (i.eye <= sceneEye) ? _FrontAlpha : _BehindAlpha;

                color = fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }

    Fallback Off
}
