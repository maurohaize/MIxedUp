Shader "MixedUp/GradientSky"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.32, 0.62, 0.88, 1)
        _HorizonColor ("Horizon", Color) = (0.72, 0.93, 0.95, 1)
        _BottomColor ("Bottom", Color) = (0.75, 0.85, 0.80, 1)
        _Exponent ("Top falloff", Range(0.1, 3)) = 0.7
    }

    SubShader
    {
        Tags { "RenderType" = "Background" "Queue" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _BottomColor;
                float _Exponent;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.dir = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float h = normalize(input.dir).y;
                half3 up = lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(h), _Exponent));
                half3 down = lerp(_HorizonColor.rgb, _BottomColor.rgb, saturate(-h * 4.0));
                return half4(h >= 0 ? up : down, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
