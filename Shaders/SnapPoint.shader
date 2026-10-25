// The snap point bubbles, drawn in the game's own camera on a camera-facing quad.
//
// Built from the gizmo ring shader and drawn the same way: by a command buffer after the
// camera's image effects, so depth of field never blurs a bubble, and with ZTest Always,
// so no piece can hide one. Whether a bubble really is behind something is still read
// from the depth texture, per fragment, and only dims it.
//
// The bubble itself is worked out per pixel from the quad's UV: clear at the center,
// rising to _RimAlpha at _RimAt of the radius, then fading out to the edge. Every value
// comes from the mod per draw, so one material serves every color and state.
//
// No keywords and no multi_compile, for the same reason as the ring shader: one variant
// per graphics API, nothing for variant stripping to remove.

Shader "Valchitecture/SnapPoint"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)

        // Set from the mod per draw, not authored here.
        _RimAt       ("Rim, as a share of the radius", Range(0.01, 0.99)) = 0.85
        _RimAlpha    ("Alpha at the rim",               Range(0, 1))       = 0.45
        _FadePower   ("Fade toward the center",          Range(0.1, 8))     = 1.6
        _FrontAlpha  ("Alpha clear of pieces",           Range(0, 1))       = 1.0
        _BehindAlpha ("Alpha behind a piece",            Range(0, 1))       = 0.5
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
            Name "Bubble"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            Lighting Off
            Fog { Mode Off }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _CameraDepthTexture;

            float4 _Color;
            float  _RimAt;
            float  _RimAlpha;
            float  _FadePower;
            float  _FrontAlpha;
            float  _BehindAlpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float4 screen : TEXCOORD1;
                float  eye    : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screen = ComputeScreenPos(o.pos);

                // Distance along the view direction, comparable with the scene's linear depth.
                o.eye = -UnityObjectToViewPos(v.vertex).z;

                return o;
            }

            // Clear at the center, _RimAlpha at the rim, nothing at the edge.
            float Bubble(float r)
            {
                float inside = _RimAlpha * pow(saturate(r / _RimAt), _FadePower);
                float t = saturate((r - _RimAt) / max(1.0 - _RimAt, 1e-4));
                float outside = _RimAlpha * (1.0 - smoothstep(0.0, 1.0, t));
                return r <= _RimAt ? inside : outside;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float r = length(i.uv * 2.0 - 1.0);

                // Keeps the edge round and soft when the bubble is only a few pixels wide.
                float edge = saturate((1.0 - r) / max(fwidth(r), 1e-5));

                float2 uv = i.screen.xy / max(i.screen.w, 1e-6);
                float sceneEye = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));

                // With no depth texture bound every fragment counts as clear, so bubbles go
                // full strength rather than missing.
                float depthAlpha = (i.eye <= sceneEye) ? _FrontAlpha : _BehindAlpha;

                return fixed4(_Color.rgb, _Color.a * Bubble(r) * edge * depthAlpha);
            }
            ENDCG
        }
    }

    Fallback Off
}
