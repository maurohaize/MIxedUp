// The character's face: one cell of an expression atlas, picked per character with the _Cell property (column, row counted
// from the top). Unlit and alpha blended; the patch hovers a hair above the head so a small depth offset keeps it clean.
Shader "MixedUp/FaceAtlas"
{
    Properties
    {
        _MainTex ("Atlas", 2D) = "white" {}
        _Grid ("Columns, rows", Vector) = (4, 4, 0, 0)
        _Cell ("Column, row (from the top)", Vector) = (0, 0, 0, 0)
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Face"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Grid;
                float4 _Cell;
                half4 _Tint;
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
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float row = _Grid.y - 1.0 - _Cell.y;
                output.uv = (input.uv + float2(_Cell.x, row)) / _Grid.xy;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Tint;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
