// Inverted-hull outline: draw this on a copy of the mesh and only its back faces, pushed outwards, show up as a
// thick ink line around the silhouette. Thickness is in world units so non-uniformly scaled parts stay even.
Shader "MixedUp/Outline"
{
    Properties
    {
        _Color ("Ink", Color) = (0.05, 0.04, 0.04, 1)
        _Width ("Width (metres)", Float) = 0.028
        _DistanceScale ("Thicker when far", Range(0, 1)) = 0.4
        // 1 = push the hull along the smoothed normal baked into the mesh tangent (flat-shaded props: no cracks at their edges).
        _SmoothNormals ("Use baked smooth normals", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Width;
                float _DistanceScale;
                float _SmoothNormals;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 pushOS = lerp(input.normalOS, input.tangentOS.xyz, _SmoothNormals);
                float3 normalWS = normalize(TransformObjectToWorldNormal(pushOS));
                float distanceToCamera = distance(_WorldSpaceCameraPos, positionWS);
                float width = _Width * lerp(1.0, clamp(distanceToCamera * 0.18, 0.7, 3.0), _DistanceScale);
                output.positionCS = TransformWorldToHClip(positionWS + normalWS * width);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
