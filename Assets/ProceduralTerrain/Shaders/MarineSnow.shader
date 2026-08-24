Shader "Custom/MarineSnow"
{
    Properties
    {
        _Color ("Particle Colour", Color) = (0.86, 0.93, 0.95, 1)
        _BoxSize ("Wrap Box Size (m)", Float) = 24
        _SizeMin ("Particle Size Min (m)", Float) = 0.006
        _SizeMax ("Particle Size Max (m)", Float) = 0.022
        _DriftSpeed ("Drift Speed (m/s)", Float) = 0.05
        _SinkSpeed ("Sink Speed (m/s)", Float) = 0.03
        _Opacity ("Opacity", Range(0, 1)) = 0.5
        _NearFade ("Near Fade Distance (m)", Float) = 0.35
        _FarFade ("Far Fade Distance (m)", Float) = 12
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }

        Pass
        {
            Name "MarineSnow"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "UnderwaterCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _BoxSize;
                float _SizeMin;
                float _SizeMax;
                float _DriftSpeed;
                float _SinkSpeed;
                float _Opacity;
                float _NearFade;
                float _FarFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 corner     : TEXCOORD0;
                float2 particle   : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 corner     : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half   fade       : TEXCOORD2;
            };

            float3 SnowHash3(float s)
            {
                float3 p = frac(float3(s * 0.1031, s * 0.1030, s * 0.0973));
                p += dot(p, p.yzx + 33.33);
                return frac((p.xxy + p.yzz) * p.zyx);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float seed = IN.particle.x;
                float3 h = SnowHash3(seed);
                float3 h2 = SnowHash3(seed + 91.7);

                float box = max(_BoxSize, 0.5);
                float3 basePos = h * box;

                float t = _Time.y;
                float3 drift = float3(
                    sin(t * _DriftSpeed * 6.2831 * (0.3 + h2.x * 0.5) + h2.y * 6.2831) * 0.35,
                    -t * _SinkSpeed * (0.6 + h2.z * 0.8),
                    cos(t * _DriftSpeed * 6.2831 * (0.3 + h2.y * 0.5) + h2.x * 6.2831) * 0.35);

                float3 cam = _WorldSpaceCameraPos;
                float3 p = basePos + drift;
                p = cam + (frac((p - cam) / box + 0.5) - 0.5) * box;

                float size = lerp(_SizeMin, _SizeMax, h2.x);

                float3 right = float3(UNITY_MATRIX_V._m00, UNITY_MATRIX_V._m01, UNITY_MATRIX_V._m02);
                float3 up    = float3(UNITY_MATRIX_V._m10, UNITY_MATRIX_V._m11, UNITY_MATRIX_V._m12);

                float3 wpos = p + (IN.corner.x * right + IN.corner.y * up) * size;

                float d = distance(p, cam);
                half nearF = saturate((d - _NearFade) / max(_NearFade, 0.01));
                half farF  = 1.0 - saturate((d - _FarFade) / max(_FarFade * 0.5, 0.01));

                OUT.positionWS = wpos;
                OUT.positionCS = TransformWorldToHClip(wpos);
                OUT.corner     = IN.corner;
                OUT.fade       = nearF * farF * (0.5 + h2.z * 0.5);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half r2 = dot(IN.corner, IN.corner);
                half a = saturate(1.0 - r2);
                a *= a;

                Light mainLight = GetMainLight();
                half3 lit = _Color.rgb * (mainLight.color * 0.6 + SampleSH(float3(0, 1, 0)) * 0.8);

                float3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                half3 col = lerp(lit, UnderwaterBackground(-V), UnderwaterHardFade(
                    distance(IN.positionWS, _WorldSpaceCameraPos)));

                return half4(col, a * IN.fade * _Opacity);
            }
            ENDHLSL
        }
    }
}
