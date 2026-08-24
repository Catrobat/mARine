// URP stylized underwater hard-surface shader. No textures: colour ramp plus
// procedural mottling, bedding, turf and crust.
Shader "Custom/UnderwaterRock"
{
    Properties
    {
        _ColorLow ("Rock Colour (base/shade)", Color) = (0.16, 0.18, 0.22, 1)
        _ColorHigh ("Rock Colour (lit tops)", Color) = (0.45, 0.44, 0.42, 1)
        _StrataScale ("Strata Band Frequency", Float) = 2.2
        _StrataStrength ("Strata Band Strength", Range(0, 1)) = 0.45
        _DetailScale ("Surface Detail Scale", Float) = 8.0
        _MottleStrength ("Surface Mottling", Range(0, 1)) = 0.55

        _CausticsColor ("Caustics Colour", Color) = (0.85, 1.0, 0.95, 1)
        _CausticsIntensity ("Caustics Intensity", Range(0, 3)) = 0.7
        _CausticsScale ("Caustics Scale", Float) = 0.6
        _CausticsSpeed ("Caustics Speed", Float) = 0.5

        _RimColor ("Rim Colour", Color) = (0.4, 0.75, 0.8, 1)
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.6

        // Silt on upward faces. Set the colour to the biome's seabed so rock and
        // sand share a family instead of reading as two pasted-together materials.
        _SedimentColor ("Sediment Colour", Color) = (0.52, 0.60, 0.30, 1)
        _SedimentAmount ("Sediment on Up-Faces", Range(0, 1)) = 0

        // Per-pixel relief. The seabed has a normal map and the rock has none, so
        // without this the rock is the only smooth-shaded thing in the frame.
        _ReliefScale ("Surface Relief Scale", Float) = 3.5
        _ReliefStrength ("Surface Relief Strength", Range(0, 1)) = 0
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
                half4 _ColorLow;
                half4 _ColorHigh;
                half4 _CausticsColor;
                half4 _RimColor;
                half4 _SedimentColor;
                float _SedimentAmount;
                float _ReliefScale;
                float _ReliefStrength;
                float _StrataScale;
                float _StrataStrength;
                float _DetailScale;
                float _MottleStrength;
                float _CausticsIntensity;
                float _CausticsScale;
                float _CausticsSpeed;
                float _RimStrength;
            CBUFFER_END


            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                half4  color      : COLOR;
            };


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
                N = PerturbRockNormal(N, IN.positionWS, _ReliefScale, _ReliefStrength);
                half ao = IN.color.a;

                // Colour ramp: lit tops vs shaded flanks, plus sediment strata
                // bands so tall formations read as layered rock.
                half topness = saturate(N.y * 0.5 + 0.5);
                half3 albedo = lerp(_ColorLow.rgb, _ColorHigh.rgb, topness) * IN.color.rgb;
                albedo = RockSurfaceDetail(albedo, IN.positionWS, _DetailScale,
                                           _MottleStrength, _StrataScale, _StrataStrength);
                albedo = ApplySediment(albedo, N, _SedimentColor.rgb, _SedimentAmount);

                Light mainLight = GetMainLight();
                half halfLambert = saturate(dot(N, mainLight.direction) * 0.5 + 0.5);
                half3 ambient = SampleSH(N);
                half3 lighting = mainLight.color * halfLambert + ambient;

                half aoC = ao * (0.3 + 0.7 * ao);
                half3 color = albedo * lighting * aoC;

                float2 cuv = IN.positionWS.xz * _CausticsScale;
                half caustic = saturate(UnderwaterCaustics(cuv, _Time.y * _CausticsSpeed));
                color += _CausticsColor.rgb * mainLight.color *
                         (caustic * _CausticsIntensity * saturate(N.y) * aoC);

                half3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                half rim = pow(1.0 - saturate(dot(N, V)), 3.0);
                color += _RimColor.rgb * (rim * _RimStrength * ao);

                // Turf first, crust second: colonies grow on top of the living mat.
                color = ApplyAlgalTurf(color, IN.positionWS, N, aoC, ambient);
                color = ApplyEncrustation(color, IN.positionWS, N, aoC, lighting, ambient);

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
