// URP bioluminescent prop shader (glow anemones, deep-biome accents):
Shader "Custom/UnderwaterGlow"
{
    Properties
    {
        _BaseColor ("Body Colour", Color) = (0.09, 0.11, 0.16, 1)
        _GlowColor ("Glow Colour", Color) = (0.2, 0.95, 0.9, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 4)) = 1.6
        _PulseSpeed ("Pulse Speed", Range(0, 4)) = 0.8
        _PulseDepth ("Pulse Depth", Range(0, 1)) = 0.45
        _FogPunch ("Glow Through Fog", Range(0, 1)) = 0.55
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

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GlowColor;
                float _GlowIntensity;
                float _PulseSpeed;
                float _PulseDepth;
                float _FogPunch;
            CBUFFER_END

            // Globals driven by UnderwaterEnvironment.cs

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                half4  color      : COLOR;   // a = glow mask baked by the mesh library
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                half4  color      : COLOR;
            };


            // Must stay identical to Custom/UnderwaterSkybox (see that file).

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.color      = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 N = normalize(IN.normalWS);
                half3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);

                Light mainLight = GetMainLight();
                half halfLambert = saturate(dot(N, mainLight.direction) * 0.5 + 0.5);
                half3 color = _BaseColor.rgb * IN.color.rgb *
                              (mainLight.color * halfLambert + SampleSH(N));

                // De-phased pulse per instance from the object's world origin.
                float3 origin = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                float phase = dot(origin, float3(1.7, 9.1, 4.3));
                half pulse = 1.0 - _PulseDepth * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed * TWO_PI + phase));

                half3 emission = _GlowColor.rgb * (_GlowIntensity * pulse * IN.color.a);

                float fog = UnderwaterFog(IN.positionWS);
                color = ApplyUnderwaterMedium(color, IN.positionWS, V);
                // Bioluminescence is its own light source: let a fraction of it
                // survive the fog so distant glows still read in the dark.
                color += emission * lerp(1.0, 1.0 - fog, 1.0 - _FogPunch);
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
