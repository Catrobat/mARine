// URP underwater sand: world-space tiling, animated caustics, fade into the
// shared underwater fog/gradient. Globals come from UnderwaterEnvironment.cs.
Shader "Custom/UnderwaterSand"
{
    Properties
    {
        _BaseMap ("Sand Albedo", 2D) = "white" {}
        _BaseColor ("Sand Tint", Color) = (1.0, 0.96, 0.86, 1)
        [Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.6
        _WorldTiling ("Texture Tiling (tiles per metre)", Float) = 0.35
        _DetileStrength ("Anti-Tiling Blend", Range(0, 1)) = 0.4

        _DeepColor ("Deep Water Tint", Color) = (0.30, 0.65, 0.75, 1)
        _DepthTintStrength ("Depth Tint Strength", Range(0, 1)) = 0.5
        _DepthTintRange ("Depth Tint Range (m)", Float) = 5

        _CausticsColor ("Caustics Colour", Color) = (0.85, 1.0, 0.95, 1)
        _CausticsIntensity ("Caustics Intensity", Range(0, 3)) = 1.0
        _CausticsScale ("Caustics Scale", Float) = 0.6
        _CausticsSpeed ("Caustics Speed", Float) = 0.5
        _CausticsChroma ("Caustics Rainbow Split", Range(0, 2)) = 1
        _SparkleIntensity ("Sand Sparkle", Range(0, 2)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "UnderwaterCommon.hlsl"

            TEXTURE2D(_BaseMap);   SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _DeepColor;
                half4 _CausticsColor;
                float _NormalStrength;
                float _WorldTiling;
                float _DetileStrength;
                float _DepthTintStrength;
                float _DepthTintRange;
                float _CausticsIntensity;
                float _CausticsScale;
                float _CausticsSpeed;
                float _CausticsChroma;
                float _SparkleIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.positionWS.xz * _WorldTiling;

                // Two samples at different scales hide the texture repeat on endless sand.
                half3 albedo  = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
                half3 albedo2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv * 0.137 + 0.5).rgb;
                albedo = lerp(albedo, albedo2, _DetileStrength) * _BaseColor.rgb;

                // Normal mapping using a world-axis tangent frame (fine for a heightfield).
                half3 nTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv), _NormalStrength);
                half3 Ngeo = normalize(IN.normalWS);
                half3 T = normalize(cross(half3(0, 0, 1), Ngeo));
                half3 B = cross(Ngeo, T);
                half3 N = normalize(nTS.x * T + nTS.y * B + nTS.z * Ngeo);

                // Soft, wrapped diffuse — underwater light is diffuse and gentle.
                Light mainLight = GetMainLight();
                half  halfLambert = saturate(dot(N, mainLight.direction) * 0.5 + 0.5);
                half3 lighting = mainLight.color * halfLambert + SampleSH(N);
                half3 color = albedo * lighting;

                float2 cuv = IN.positionWS.xz * _CausticsScale;
                float  ct  = _Time.y * _CausticsSpeed;
                half3 caustic = UnderwaterCausticsRGB(cuv, ct, _CausticsChroma * 8.0);
                color += _CausticsColor.rgb * mainLight.color *
                         (caustic * _CausticsIntensity * saturate(Ngeo.y));

                float3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                half3 H = normalize(mainLight.direction + V);
                half spec = pow(saturate(dot(N, H)), 48.0);
                color += mainLight.color * (spec * _SparkleIntensity * (0.3 + caustic.g));

                float depthBelow = saturate((_UnderwaterLevel - IN.positionWS.y) /
                                            max(_DepthTintRange, 0.01));
                color = lerp(color, color * _DeepColor.rgb * 1.6,
                             depthBelow * _DepthTintStrength);

                color = ApplyUnderwaterMedium(color, IN.positionWS, V);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings  { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half frag(Varyings IN) : SV_Target { return IN.positionCS.z; }
            ENDHLSL
        }
    }
}
