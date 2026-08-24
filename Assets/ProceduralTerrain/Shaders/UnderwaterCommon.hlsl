#ifndef UNDERWATER_COMMON_INCLUDED
#define UNDERWATER_COMMON_INCLUDED

half4 _UnderwaterFogColor;
half4 _UnderwaterColorSurface;
half4 _UnderwaterColorDeep;
half4 _UnderwaterSunGlow;
float _UnderwaterFogDensity;
float _UnderwaterFadeStart;
float _UnderwaterFadeEnd;
float _UnderwaterLevel;
float _UnderwaterAbsorbTint;
float _UnderwaterSurgeAmp;
float _UnderwaterSurgeSpeed;
float4 _UnderwaterSurgeDir;
half4 _UnderwaterEncrustA;
half4 _UnderwaterEncrustB;
half4 _UnderwaterEncrustC;
float _UnderwaterEncrustAmount;
float _UnderwaterEncrustScale;
float _UnderwaterEncrustRelief;
half4 _UnderwaterTurfColor;
half4 _UnderwaterTurfTip;
float _UnderwaterTurfAmount;
float _UnderwaterTurfScale;
float _UnderwaterTurfUpBias;
float _UnderwaterTurfRelief;

half3 UnderwaterBackground(float3 viewDir)
{
    half3 col = lerp(_UnderwaterFogColor.rgb, _UnderwaterColorSurface.rgb,
                     smoothstep(0.0, 0.7, viewDir.y));
    col = lerp(col, _UnderwaterColorDeep.rgb,
               smoothstep(0.0, 0.6, -viewDir.y));

    float3 L = _MainLightPosition.xyz;
    float sunAmount = saturate(dot(viewDir, L));
    col += _UnderwaterSunGlow.rgb *
           (pow(sunAmount, 12.0) * 0.5 + pow(sunAmount, 90.0) * 0.8);
    return col;
}

float3 UnderwaterAbsorption()
{
    float3 w = _UnderwaterFogColor.rgb;
    float peak = max(max(w.r, w.g), max(w.b, 1e-4));
    float3 hue = saturate(w / peak);
    return _UnderwaterFogDensity * (1.0 + _UnderwaterAbsorbTint * (1.0 - hue));
}

float UnderwaterHardFade(float dist)
{
    float fadeEnd   = _UnderwaterFadeEnd > 0.01 ? _UnderwaterFadeEnd : 1e5;
    float fadeStart = min(_UnderwaterFadeStart, fadeEnd - 0.01);
    return smoothstep(fadeStart, fadeEnd, dist);
}

float UnderwaterFog(float3 positionWS)
{
    float dist   = distance(positionWS, _WorldSpaceCameraPos);
    float expFog = 1.0 - exp(-pow(dist * _UnderwaterFogDensity, 2.0));
    return max(expFog, UnderwaterHardFade(dist));
}

half3 ApplyUnderwaterMedium(half3 color, float3 positionWS, float3 viewDirWS)
{
    float dist = distance(positionWS, _WorldSpaceCameraPos);
    half3 bg   = UnderwaterBackground(-viewDirWS);

    float3 a = UnderwaterAbsorption();
    float3 T = exp(-pow(dist * a, 2.0));

    half3 lit = color * T + bg * (1.0 - T);
    return lerp(lit, bg, UnderwaterHardFade(dist));
}

half UnderwaterCaustics(float2 uv, float time)
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
    c = 1.17 - pow(abs(c / 3.0), 1.4);
    return pow(abs(c), 8.0);
}

half3 UnderwaterCausticsRGB(float2 uv, float time, float chroma)
{
    half c    = saturate(UnderwaterCaustics(uv, time));
    half edge = saturate(fwidth(c) * chroma);
    return half3(saturate(c + edge), c, saturate(c - edge));
}

float UwHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float UwValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = UwHash21(i);
    float b = UwHash21(i + float2(1.0, 0.0));
    float c = UwHash21(i + float2(0.0, 1.0));
    float d = UwHash21(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// Two octaves, written out rather than looped: the loop bound would have to be a
// compile-time constant to unroll anyway, and this shades every rock pixel.
float UwFbm2(float2 p)
{
    return (UwValueNoise(p) + UwValueNoise(p * 2.1 + 5.3) * 0.5) * (1.0 / 1.5);
}

// Height field of the crust itself: a coarse lumpiness with a finer grain on top.
float UwCrustHeight(float2 q)
{
    return UwValueNoise(q * 3.1) * 0.62 + UwValueNoise(q * 9.0 + 3.3) * 0.38;
}

half3 RockSurfaceDetail(half3 albedo, float3 positionWS, float scale,
                        float mottleStrength, float strataScale, float strataStrength)
{
    float2 q = float2(positionWS.x + positionWS.y * 0.6,
                      positionWS.z - positionWS.y * 0.6) * scale;

    float nBroad = UwValueNoise(q);
    float nFine  = UwValueNoise(q * 3.1 + 4.7);
    float nLow   = UwValueNoise(q * 0.32 + 19.3);

    float mottle = nBroad * 0.62 + nFine * 0.38;
    albedo *= lerp(1.0, 0.58 + 0.88 * mottle, mottleStrength);

    float band   = 0.5 + 0.5 * sin((positionWS.y + (nLow - 0.5) * 1.6) * strataScale);
    float bedded = saturate(nLow * 1.7);
    albedo *= lerp(1.0, 0.76 + 0.38 * band, strataStrength * bedded);

    return albedo;
}

// Silt settling on upward-facing rock. Underwater nothing stays clean:
half3 PerturbRockNormal(half3 N, float3 positionWS, float scale, float strength)
{
    if (strength <= 0.001) return N;

    float2 q = positionWS.xz * scale + positionWS.y * (scale * 0.6);
    const float e = 0.35;
    float n0 = UwValueNoise(q);
    float gx = UwValueNoise(q + float2(e, 0.0)) - n0;
    float gz = UwValueNoise(q + float2(0.0, e)) - n0;

    // Project the gradient into the surface plane so the bump tilts N without
    // pulling it off the surface.
    float3 g = float3(gx, 0.0, gz) / e;
    g -= dot(g, N) * N;
    return normalize(N - g * strength);
}

half3 ApplySediment(half3 albedo, half3 N, half3 sedimentColor, float amount)
{
    if (amount <= 0.001) return albedo;
    float settle = pow(saturate(N.y), 1.5) * amount;
    return lerp(albedo, sedimentColor, settle);
}

// Encrusting life on rock. In the photic zone bare rock effectively does not exist:
// ── Algal turf ───────────────────────────────────────────────────────────────
// The continuous filamentous mat that covers the lit side of any rock that has sat in
// shallow water — the fuzzy green coat on a boulder in the surf, not the discrete.
half3 ApplyAlgalTurf(half3 color, float3 positionWS, half3 N, half ao,
                     half3 ambient)
{
    if (_UnderwaterTurfAmount <= 0.001) return color;

    float scale = max(_UnderwaterTurfScale, 0.01);
    float2 q = positionWS.xz * scale + positionWS.y * (scale * 0.55);

    // Light-limited coverage: strongly up-facing keeps the mat, undersides stay
    // bare rock. TurfUpBias 0 = grows anywhere, 1 = only near-horizontal tops.
    float up = saturate(N.y * 0.5 + 0.5);
    float upWeight = lerp(1.0, smoothstep(0.30, 0.80, up), saturate(_UnderwaterTurfUpBias));

    // Two octaves at different scales so the mat's edge frays at more than one
    // size — a single noise contour reads as a painted-on stencil.
    float edge = UwFbm2(q) * 0.65 + UwValueNoise(q * 4.3 + 11.7) * 0.35;
    float cover = saturate(_UnderwaterTurfAmount * upWeight * (0.55 + 0.95 * edge));
    if (cover <= 0.001) return color;

    // Filament grain. Much finer than the crust's lumps: turf is threads, not
    // knobs, so the texture has to sit near the limit of what a pixel resolves.
    float f0 = UwValueNoise(q * 7.0);
    float fil = f0 * 0.6 + UwValueNoise(q * 19.0 + 5.1) * 0.4;

    // Wide remap on purpose.
    half3 turf = lerp(_UnderwaterTurfColor.rgb, _UnderwaterTurfTip.rgb,
                      smoothstep(0.24, 0.94, fil));

    // Relight with a fuzzed normal so the mat has depth of its own. Without this
    // the grain lives in the albedo only and the rock goes flat under it.
    half3 turfN = N;
    if (_UnderwaterTurfRelief > 0.001)
    {
        const float e = 0.22;
        float gx = UwValueNoise((q + float2(e, 0.0)) * 7.0) - f0;
        float gz = UwValueNoise((q + float2(0.0, e)) * 7.0) - f0;
        float3 g = float3(gx, 0.0, gz) / e;
        g -= dot(g, N) * N;
        turfN = normalize(N - g * (_UnderwaterTurfRelief * cover));
    }

    // Wrapped diffuse, no specular: filaments scatter light rather than reflect
    // it, and they self-shade at the base of the pile.
    half wrap = saturate(dot(turfN, _MainLightPosition.xyz) * 0.5 + 0.5);
    half3 turfLight = _MainLightColor.rgb * (wrap * wrap) + ambient;
    half pile = 0.82 + 0.18 * fil;

    return lerp(color, turf * turfLight * ao * pile, cover);
}

half3 ApplyEncrustation(half3 color, float3 positionWS, half3 N, half ao,
                        half3 lighting, half3 ambient)
{
    if (_UnderwaterEncrustAmount <= 0.001) return color;

    float scale = max(_UnderwaterEncrustScale, 0.01);
    float2 q = positionWS.xz * scale + positionWS.y * (scale * 0.35);

    // Coverage. Two octaves so a patch edge is ragged at more than one scale
    // instead of being a single smooth noise contour.
    float cover = saturate(_UnderwaterEncrustAmount * (0.75 + 0.55 * UwFbm2(q)));

    float h0 = UwCrustHeight(q);
    float h  = h0 * cover;

    // Which community. One low-frequency tap picks the patch;
    float sel = UwValueNoise(q * 0.45 + float2(13.0, -7.0));
    float lit = saturate(N.y * 0.5 + 0.5);

    float wA = saturate(1.0 - abs(sel - 0.30) * 3.2) * lit;
    float wB = saturate(1.0 - abs(sel - 0.62) * 3.2) * lit;
    float wC = saturate(1.0 - abs(sel - 0.85) * 3.2) * (1.15 - lit);
    float wsum = wA + wB + wC + 1e-4;

    half3 crust = (_UnderwaterEncrustA.rgb * wA +
                   _UnderwaterEncrustB.rgb * wB +
                   _UnderwaterEncrustC.rgb * wC) / wsum;

    // Tips catch the light, hollows stay dark, so one patch is not one flat colour.
    crust *= (0.80 + 0.40 * h);

    // Relight the crust with its own normal. Without this the relief exists in the
    // albedo only, which is exactly the flat look this is meant to fix.
    half3 crustN = N;
    if (_UnderwaterEncrustRelief > 0.001)
    {
        const float e = 0.35;
        float gx = UwCrustHeight(q + float2(e, 0.0)) - h0;
        float gz = UwCrustHeight(q + float2(0.0, e)) - h0;
        float3 g = float3(gx, 0.0, gz) / e;
        g -= dot(g, N) * N;          // keep the bump in the surface plane
        crustN = normalize(N - g * (_UnderwaterEncrustRelief * cover));
    }

    half halfLambert = saturate(dot(crustN, _MainLightPosition.xyz) * 0.5 + 0.5);
    half3 crustLight = _MainLightColor.rgb * halfLambert + ambient;

    // The crust fills hollows, so its own low points self-shadow.
    half cavity = saturate(1.0 - (1.0 - h) * 0.55 * cover);

    return lerp(color, crust * crustLight * ao * cavity, cover);
}

float3 UnderwaterSurgeWorld(float heightAboveBase, float3 pivotWS, float flexibility)
{
    float2 dir = normalize(_UnderwaterSurgeDir.xy + 1e-5);
    float phase = dot(pivotWS.xz, float2(0.7, 1.3));
    float t = _Time.y * _UnderwaterSurgeSpeed + phase;

    float s = sin(t) * 0.75 + sin(t * 2.13 + 1.7) * 0.25;
    float bend = max(heightAboveBase, 0.0) * flexibility * _UnderwaterSurgeAmp;
    return float3(dir.x * s, 0.0, dir.y * s) * bend;
}

float3 UnderwaterObjectPivot()
{
    return float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
}

#endif
