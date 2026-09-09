Shader "Custom/IntegritySprite"
{
    Properties
    {
        // --- Base Sprite ---
        _MainTex ("Base Sprite", 2D) = "white" {}

        // --- Overlay Sprite ---
        _OverlayTex ("Overlay Sprite", 2D) = "white" {}

        // --- Integrity (0 = fully damaged, 1 = fully intact) ---
        _Integrity ("Integrity", Range(0, 1)) = 1.0

        // --- Colour Remapping ---
        // Replace up to 4 source colours with target colours.
        // Set _ColourThreshold to control how tightly a pixel must match.
        _SourceColour0 ("Source Colour 0", Color) = (1, 0, 0, 1)
        _TargetColour0 ("Target Colour 0", Color) = (0, 0, 1, 1)

        _SourceColour1 ("Source Colour 1", Color) = (0, 1, 0, 1)
        _TargetColour1 ("Target Colour 1", Color) = (1, 1, 0, 1)

        _SourceColour2 ("Source Colour 2", Color) = (0, 0, 1, 1)
        _TargetColour2 ("Target Colour 2", Color) = (0, 1, 1, 1)

        _SourceColour3 ("Source Colour 3", Color) = (1, 1, 0, 1)
        _TargetColour3 ("Target Colour 3", Color) = (1, 0, 1, 1)

        _ColourThreshold ("Colour Match Threshold", Range(0, 1)) = 0.1

        // --- Tint applied to the whole base sprite AFTER remapping ---
        _SpriteTint ("Sprite Tint", Color) = (1, 1, 1, 1)

        // --- Overlay blend mode: 0 = Alpha blend, 1 = Additive, 2 = Multiply ---
        [KeywordEnum(ALPHA, ADDITIVE, MULTIPLY)] _OverlayBlend ("Overlay Blend Mode", Float) = 0

        // --- Stencil / sorting support (standard Unity sprite fields) ---
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "PreviewType"     = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha   // Pre-multiplied alpha (Unity sprite standard)

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            // -------------------------------------------------------
            // Textures
            // -------------------------------------------------------
            sampler2D _MainTex;
            sampler2D _OverlayTex;
            float4    _MainTex_ST;
            float4    _OverlayTex_ST;

            // -------------------------------------------------------
            // Parameters
            // -------------------------------------------------------
            float  _Integrity;
            float  _ColourThreshold;
            float  _OverlayBlend;

            fixed4 _SpriteTint;

            fixed4 _SourceColour0; fixed4 _TargetColour0;
            fixed4 _SourceColour1; fixed4 _TargetColour1;
            fixed4 _SourceColour2; fixed4 _TargetColour2;
            fixed4 _SourceColour3; fixed4 _TargetColour3;

            // Standard Unity sprite renderer colour (set by SpriteRenderer.color)
            fixed4 _RendererColor;

            // -------------------------------------------------------
            // Structs
            // -------------------------------------------------------
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // -------------------------------------------------------
            // Vertex shader
            // -------------------------------------------------------
            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex   = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                // Combine per-vertex colour with the renderer's tint
                OUT.color    = IN.color * _RendererColor;
                return OUT;
            }

            // -------------------------------------------------------
            // Helpers
            // -------------------------------------------------------

            // Returns how closely 'col' matches 'source' (1 = perfect match, 0 = no match)
            float ColourMatch(fixed3 col, fixed3 source, float threshold)
            {
                float dist = length(col - source);
                // Smooth step so the transition isn't a hard cut
                return 1.0 - smoothstep(0.0, threshold + 0.001, dist);
            }

            // Remap a single colour slot; returns the blended output colour
            fixed3 RemapColour(fixed3 col, fixed3 src, fixed3 tgt, float threshold)
            {
                float w = ColourMatch(col, src, threshold);
                return lerp(col, tgt, w);
            }

            // -------------------------------------------------------
            // Fragment shader
            // -------------------------------------------------------
            fixed4 frag(v2f IN) : SV_Target
            {
                // ---- 1. Sample base sprite ----
                fixed4 base = tex2D(_MainTex, IN.texcoord);

                // Discard fully transparent pixels early
                clip(base.a - 0.001);

                // ---- 2. Colour remapping (RGB only; alpha preserved) ----
                fixed3 rgb = base.rgb;

                rgb = RemapColour(rgb, _SourceColour0.rgb, _TargetColour0.rgb, _ColourThreshold);
                rgb = RemapColour(rgb, _SourceColour1.rgb, _TargetColour1.rgb, _ColourThreshold);
                rgb = RemapColour(rgb, _SourceColour2.rgb, _TargetColour2.rgb, _ColourThreshold);
                rgb = RemapColour(rgb, _SourceColour3.rgb, _TargetColour3.rgb, _ColourThreshold);

                // Apply user tint and per-renderer colour
                fixed4 remapped = fixed4(rgb * _SpriteTint.rgb * IN.color.rgb, base.a * _SpriteTint.a * IN.color.a);

                // ---- 3. Sample overlay sprite ----
                fixed4 overlay = tex2D(_OverlayTex, IN.texcoord);

                // _Integrity == 1  →  overlayAlpha = 0  (no overlay visible)
                // _Integrity == 0  →  overlayAlpha = overlay.a  (fully visible)
                float overlayStrength = (1.0 - _Integrity) * overlay.a;

                // ---- 4. Blend overlay onto base according to blend mode ----
                fixed3 blendedRGB;

                if (_OverlayBlend < 0.5)
                {
                    // Alpha blend
                    blendedRGB = lerp(remapped.rgb, overlay.rgb, overlayStrength);
                }
                else if (_OverlayBlend < 1.5)
                {
                    // Additive
                    blendedRGB = remapped.rgb + overlay.rgb * overlayStrength;
                }
                else
                {
                    // Multiply
                    fixed3 multiplied = remapped.rgb * overlay.rgb;
                    blendedRGB = lerp(remapped.rgb, multiplied, overlayStrength);
                }

                fixed4 result = fixed4(blendedRGB, remapped.a);

                // ---- 5. Pre-multiply alpha (required by Blend One OneMinusSrcAlpha) ----
                result.rgb *= result.a;

                return result;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
