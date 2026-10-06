// The river surface: still the faceted two-tone palette look, but alive. Gentle vertex waves, a smooth moving normal for
// the sun sparkle, streaks of foam drifting with the current and a frothy line along both banks.
Shader "MixedUp/Water"
{
    Properties
    {
        _BaseMap ("Palette", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.9
        _WaveHeight ("Wave height", Float) = 0.04
        _WaveSpeed ("Wave speed", Float) = 1.0
        _FlowDirection ("Flow (x, z)", Vector) = (0.55, 0, 0, 0)
        _FoamColor ("Foam", Color) = (0.95, 1, 1, 1)
        _BankMin ("South bank z", Float) = 5
        _BankMax ("North bank z", Float) = 13
        _SkyColor ("Sky reflection", Color) = (0.72, 0.9, 0.95, 1)
        _Sparkle ("Sun sparkle", Float) = 0.9
        _DeepColor ("Deep colour", Color) = (0.22, 0.45, 0.45, 1)
        _DeepMix ("Share of deep colour", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Tint;
                half _Opacity;
                float _WaveHeight;
                float _WaveSpeed;
                float4 _FlowDirection;
                half4 _FoamColor;
                float _BankMin;
                float _BankMax;
                half4 _SkyColor;
                float _Sparkle;
                half4 _DeepColor;
                half _DeepMix;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i), b = Hash21(i + float2(1, 0)), c = Hash21(i + float2(0, 1)), d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Height of the swell at a point; its slope becomes the surface normal in the fragment shader.
            float Swell(float2 p, float t)
            {
                return sin(p.x * 0.62 + t * 1.3) * cos(p.y * 0.85 + t * 1.1)
                     + 0.5 * sin(p.x * 1.7 - p.y * 1.3 + t * 2.1)
                     + 0.25 * sin(p.x * 3.1 + p.y * 2.3 - t * 3.3);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 wp = TransformObjectToWorld(input.positionOS.xyz);
                float t = _Time.y * _WaveSpeed;
                wp.y += Swell(wp.xz, t) * _WaveHeight;
                output.positionWS = wp;
                output.positionCS = TransformWorldToHClip(wp);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y * _WaveSpeed;
                float3 wp = input.positionWS;
                half3 baseColor = lerp(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb, _DeepColor.rgb, _DeepMix) * _Tint.rgb;

                // Normal from the swell slope (finite differences), exaggerated so the highlights move visibly.
                float e = 0.15;
                float h0 = Swell(wp.xz, t);
                float hx = Swell(wp.xz + float2(e, 0), t);
                float hz = Swell(wp.xz + float2(0, e), t);
                float3 n = normalize(float3(-(hx - h0) / e * 0.35, 1.0, -(hz - h0) / e * 0.35));

                float3 v = normalize(_WorldSpaceCameraPos - wp);
                float3 l = normalize(_MainLightPosition.xyz);
                float ndl = saturate(dot(n, l));
                half3 lit = baseColor * (0.78 + 0.32 * ndl) * lerp(1.0, _MainLightColor.rgb, 0.4);

                // Sky reflection grows towards the horizon.
                float fresnel = pow(1.0 - saturate(dot(n, v)), 3.0);
                lit = lerp(lit, _SkyColor.rgb, saturate(fresnel * 0.65));

                // Sparkles: glints where the wavelets mirror the sun.
                float3 h = normalize(l + v);
                float spec = pow(saturate(dot(n, h)), 90.0);
                float glints = smoothstep(0.55, 0.9, ValueNoise(wp.xz * 2.6 + t * 0.7));
                lit += _MainLightColor.rgb * spec * (0.25 + glints) * _Sparkle;

                // Foam: lines hugging both banks plus streaks drifting with the current.
                float2 flow = _FlowDirection.xy * t;
                float bank = min(wp.z - _BankMin, _BankMax - wp.z);
                float wobble = ValueNoise(float2(wp.x * 0.9 - flow.x * 1.2, wp.z * 0.7)) * 0.55;
                float shore = 1.0 - smoothstep(0.1, 0.5 + wobble, bank);
                float streakNoise = ValueNoise(float2(wp.x * 0.45 - flow.x, wp.z * 2.2));
                float streaks = smoothstep(0.78, 0.94, streakNoise) * 0.4;
                float foam = saturate(shore + streaks);
                lit = lerp(lit, _FoamColor.rgb, foam * 0.7);

                float alpha = saturate(_Opacity + foam * 0.2 + fresnel * 0.1);
                return half4(lit, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
