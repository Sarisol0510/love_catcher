Shader "Custom/NeonGlassOutline"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 0.05)
        [HDR] _OutlineColor ("Neon Outline Color", Color) = (0.839, 0.110, 0.439, 1)
        _OutlinePower ("Outline Power (Thickness)", Range(0.1, 10.0)) = 3.0
        _OutlineIntensity ("Outline Intensity (Glow)", Range(0.0, 10.0)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 viewDirWS    : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _OutlineColor;
                float _OutlinePower;
                float _OutlineIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Fresnel calculation for outline (Rim Light)
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                fresnel = pow(fresnel, _OutlinePower);
                
                // Base transparent glass color
                half4 color = _BaseColor;
                
                // Add neon outline emission (HDR glow)
                half3 emission = _OutlineColor.rgb * fresnel * _OutlineIntensity;
                
                // Final color composition
                color.rgb += emission;
                // Increase alpha at the edges to make the outline visible on transparent glass
                color.a = saturate(color.a + (fresnel * 0.8));
                
                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/Diffuse"
}
