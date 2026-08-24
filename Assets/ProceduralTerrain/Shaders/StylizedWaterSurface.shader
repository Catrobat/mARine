// URP translucent water surface seen from below: waves, fresnel, animated
// pattern, fades into the shared underwater fog at distance.
Shader "Custom/StylizedWaterSurface"
{
    Properties
    {
        _WaterColor ("Water Colour", Color) = (0.16, 0.62, 0.72, 1)
        _Opacity ("Base Opacity", Range(0, 1)) = 0.45

        _WaveAmplitude ("Wave Amplitude (m)", Float) = 0.12
        _WaveLength ("Wave Length (m)", Float) = 4.0
        _WaveSpeed ("Wave Speed", Float) = 0.8

        [Enum(Caustic Interference,0,Ripple Web,1,Directional Streaks,2)]
        _PatternStyle ("Light Pattern Style", Float) = 0
        _PatternScale ("Light Pattern Scale", Float) = 0.3
        _PatternIntensity ("Light Pattern Intensity", Range(0, 2)) = 0.7
        _PatternSpeed ("Light Pattern Speed", Float) = 0.5

        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "UnderwaterCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _WaterColor;
                float _Opacity;
                float _WaveAmplitude;
                float _WaveLength;
                float _WaveSpeed;
                float _PatternStyle;
                float _PatternScale;
                float _PatternIntensity;
                float _PatternSpeed;
                float _FresnelPower;
            CBUFFER_END

            // Globals driven by UnderwaterEnvironment.cs

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            // Same interference pattern as the sand caustics — visual continuity.
            half Caustics(float2 uv, float time)
            {
                const float sharpness = 0.005;
                float2 p = fmod(uv, TWO_PI) - 250.0;
                float2 i = p;
                float  c = 1.0;

                UNITY_UNROLL
                for (int n = 0; n < 3; n++)
                {
                    float t = time * (1.0 - (3.5 / float(n + 1)));
                    i = p + float2(cos(t - i.x) + sin(t + i.y),
                                   sin(t - i.y) + cos(t + i.x));
                    c += 1.0 / length(float2(p.x / (sin(i.x + t) / sharpness),
                                             p.y / (cos(i.y + t) / sharpness)));
                }
                c = 1.17 - pow(c / 3.0, 1.4);
                return pow(abs(c), 8.0);
            }

            // Voronoi F2-F1 with slowly drifting cell centres.
            float VoronoiEdge(float2 p, float time)
            {
                float2 ip = floor(p);
                float2 fp = p - ip;
                float f1 = 8.0;
                float f2 = 8.0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 g = float2(x, y);
                        float  hx = UwHash21(ip + g);
                        float  hy = UwHash21(ip + g + 17.3);
                        float2 o = 0.5 + 0.45 * float2(sin(time + hx * TWO_PI),
                                                       sin(time * 1.17 + hy * TWO_PI));
                        float2 d = g + o - fp;
                        float sq = dot(d, d);
                        if (sq < f1) { f2 = f1; f1 = sq; }
                        else         { f2 = min(f2, sq); }
                    }
                }
                return sqrt(f2) - sqrt(f1);
            }

            // Style 1 — ripple web. _PatternScale is tuned for the caustic field, where one unit
            // is a fine filament;
            half PatternRipple(float2 uv, float time)
            {
                float2 q = uv * 0.18;
                float edge = VoronoiEdge(q, time * 0.5);
                half  web  = 1.0 - smoothstep(0.0, 0.12, edge);
                web *= 0.45 + 0.55 * UwValueNoise(q * 0.6 + float2(time * 0.08, 0.0));
                return saturate(web * 1.25);
            }

            // Style 2 — directional streaks:
            half PatternStreaks(float2 uv, float time)
            {
                float2 dir = normalize(_UnderwaterSurgeDir.xy + 1e-5);
                float2 q = float2(dot(uv, dir) * 0.25,
                                  dot(uv, float2(-dir.y, dir.x)) * 2.5);
                half n = UwValueNoise(q + float2(time * 0.6, 0.0)) * 0.7
                       + UwValueNoise(q * 2.3 + float2(time * 0.9, 4.1)) * 0.3;
                return saturate(pow(n, 2.5) * 1.8);
            }

            // Branch on a material constant: uniform across the draw, so there is
            // no wavefront divergence on mobile.
            half SurfacePattern(float2 uv, float time)
            {
                if (_PatternStyle < 0.5)      return saturate(Caustics(uv, time));
                else if (_PatternStyle < 1.5) return PatternRipple(uv, time);
                else                          return PatternStreaks(uv, time);
            }


            // Must stay identical to Custom/UnderwaterSkybox (see that file).

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);

                // Two world-space sine waves; world-space phase means the surface can
                // follow the viewer without the waves visibly "swimming" with it.
                float  k  = TWO_PI / max(_WaveLength, 0.01);
                float  t  = _Time.y * _WaveSpeed;
                float2 d1 = normalize(float2(1.0, 0.6));
                float2 d2 = normalize(float2(-0.7, 1.0));
                float  p1 = dot(posWS.xz, d1) * k + t;
                float  p2 = dot(posWS.xz, d2) * (k * 1.7) - t * 1.3;

                posWS.y += (sin(p1) + 0.6 * sin(p2)) * _WaveAmplitude;

                // Analytic wave normal (derivative of the height function).
                float dx = (cos(p1) * d1.x * k + 0.6 * cos(p2) * d2.x * k * 1.7) * _WaveAmplitude;
                float dz = (cos(p1) * d1.y * k + 0.6 * cos(p2) * d2.y * k * 1.7) * _WaveAmplitude;
                OUT.normalWS = normalize(float3(-dx, 1.0, -dz));

                OUT.positionWS = posWS;
                OUT.positionCS = TransformWorldToHClip(posWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                half3 N = normalize(IN.normalWS);

                // abs() so the fresnel works identically from above and below.
                half ndv     = abs(dot(N, V));
                half fresnel = pow(1.0 - ndv, _FresnelPower);

                half pattern = SurfacePattern(IN.positionWS.xz * _PatternScale,
                                              _Time.y * _PatternSpeed);

                Light mainLight = GetMainLight();
                half3 color = _WaterColor.rgb * (0.6 + 0.4 * mainLight.color);
                color += pattern * _PatternIntensity * mainLight.color * 0.6;
                color += fresnel * _WaterColor.rgb * 0.5;

                half alpha = saturate(_Opacity + fresnel * 0.35 + pattern * 0.15);

                // Melt into the same view-dependent backdrop the skybox draws
                // (alpha -> 1 so the horizon band is solid and seamless).
                float fog = UnderwaterFog(IN.positionWS);
                color = ApplyUnderwaterMedium(color, IN.positionWS, V);
                alpha = lerp(alpha, 1.0, fog);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
