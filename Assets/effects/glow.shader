Shader "Custom/glow"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Heat ("Heat", Range(0, 1)) = 0
        _GlowColor ("Glow Color", Color) = (1, 0, 0, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 2.0
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnorProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _Heat;
                float4 _GlowColor;
                float  _GlowIntensity;
                float  _EdgeSoftness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                // UV.y = 0 is bottom, 1 is top — flip so glow starts at top
                float fromTop = 1.0 - IN.uv.x;

                // How far the glow reaches down: heat=0 means no glow, heat=1 fills whole sprite
                // smoothstep creates a soft edge at the glow boundary
                float glowMask = 1.0 - smoothstep(
                    (_Heat - _EdgeSoftness) - _EdgeSoftness,
                    (_Heat - _EdgeSoftness) + _EdgeSoftness,
                    fromTop
                );

                // Blend glow color on top of sprite, modulated by glow mask and texture alpha
                half3 glowContribution = _GlowColor.rgb * _GlowIntensity * glowMask * texColor.a;
                half3 finalColor = texColor.rgb + glowContribution;

                return half4(finalColor, texColor.a);
            }
            ENDHLSL
        }
    }
}
