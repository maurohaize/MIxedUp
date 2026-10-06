// A drifting wall of mist that marks the edge of the playable world. Thickest at the ground, thinning towards the top,
// and it fades away near the camera so it never smothers the screen while you walk up to it.
Shader "MixedUp/EdgeMist"
{
    Properties
    {
        _Color ("Mist", Color) = (0.86, 0.95, 0.97, 1)
        _Height ("Height", Float) = 9
        _Density ("Density", Range(0, 1)) = 0.85
        _NearFade ("Fade distance", Float) = 9
        _Drift ("Drift speed", Float) = 0.12
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Mist"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Height;
                half _Density;
                float _NearFade;
                float _Drift;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
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
                return lerp(lerp(Hash21(i), Hash21(i + float2(1, 0)), f.x), lerp(Hash21(i + float2(0, 1)), Hash21(i + float2(1, 1)), f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 wp = input.positionWS;
                float t = _Time.y * _Drift;
                // Billows: two layers of noise sliding sideways at different speeds.
                float along = wp.x + wp.z;
                float n = ValueNoise(float2(along * 0.16 + t * 3.0, wp.y * 0.35 + t)) * 0.6
                        + ValueNoise(float2(along * 0.45 - t * 4.5, wp.y * 0.8 - t * 2.0)) * 0.4;
                float vertical = pow(saturate(1.0 - wp.y / _Height), 1.35);
                float alpha = vertical * (0.35 + 0.9 * n) * _Density;

                float distanceToCamera = distance(_WorldSpaceCameraPos, wp);
                alpha *= smoothstep(1.5, _NearFade, distanceToCamera);
                return half4(_Color.rgb, saturate(alpha));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
