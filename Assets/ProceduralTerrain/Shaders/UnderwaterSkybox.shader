// URP underwater backdrop: vertical gradient + sun glow + god rays. Matches the
// fog/gradient the terrain and water fade into. Globals from UnderwaterEnvironment.cs.
Shader "Custom/UnderwaterSkybox"
{
    Properties
    {
        _GodRayIntensity ("God Ray Intensity", Range(0, 1)) = 0.3
        _GodRaySpeed ("God Ray Drift Speed", Range(0, 2)) = 0.35
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "UnderwaterCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _GodRayIntensity;
                float _GodRaySpeed;
            CBUFFER_END


            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDirWS  : TEXCOORD0;
            };


            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.viewDirWS  = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 V = normalize(IN.viewDirWS);
                half3 col = UnderwaterBackground(V);

                // God rays: animated streaks of brightness fanning out around the
                // sun direction, visible only when looking up toward the light.
                float3 L  = _MainLightPosition.xyz;
                float3 t1 = normalize(cross(float3(0, 1, 0), L) + float3(1e-4, 0, 0));
                float3 t2 = cross(L, t1);
                float  az = atan2(dot(V, t2), dot(V, t1));
                float  t  = _Time.y * _GodRaySpeed;

                float rays = (0.5 + 0.5 * sin(az * 16.0 + t))
                           * (0.5 + 0.5 * sin(az * 9.0 - t * 0.63));
                rays = pow(rays, 3.0);

                float mask = pow(saturate(dot(V, L)), 5.0) *
                             smoothstep(0.03, 0.45, V.y);
                col += _UnderwaterSunGlow.rgb * (rays * mask * _GodRayIntensity);

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
