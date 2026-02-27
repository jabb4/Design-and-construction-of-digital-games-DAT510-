#ifndef DISSOLVE_COMMON_INCLUDED
#define DISSOLVE_COMMON_INCLUDED

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;
    float  _Cutoff;
    float  _AlphaClip;
    half   _Metallic;
    half   _Smoothness;
    half   _BumpScale;
    half   _OcclusionStrength;
    half4  _EmissionColor;
    float  _DissolveAmount;
    float  _EdgeWidth;
    half4  _EdgeInnerColor;
    half4  _EdgeColor;
    half4  _EdgeCoolColor;
    float  _NoiseScale;
    float  _EdgeSoftness;
    float  _EdgePulseSpeed;
    float  _EdgePulseAmount;
    float  _EdgeFresnelPower;
    float  _EdgeFresnelBoost;
    float  _HeatExponent;
    float  _DissolveHeightMin;
    float  _DissolveHeightInvRange;
    float  _DissolveHeightStrength;
    float  _DissolveNoiseWarp;
    float  _NoiseScrollSpeed;
    float  _DissolveSeed;
    float  _FlakeBandWidth;
    float  _FlakeCutout;
    float  _FlakeNoiseScale;
    float  _FlakeFlickerSpeed;
    float  _FlakeSparkDensity;
    half4  _FlakeSparkColor;
    float  _FlakeSparkIntensity;
CBUFFER_END

float DissolveHash3D(float3 p)
{
    p = frac(p * float3(443.8975, 397.2973, 491.1871));
    p += dot(p, p.yzx + 19.19);
    return frac((p.x + p.y) * p.z);
}

float DissolveValueNoise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float n000 = DissolveHash3D(i);                    float n100 = DissolveHash3D(i + float3(1,0,0));
    float n010 = DissolveHash3D(i + float3(0,1,0));    float n110 = DissolveHash3D(i + float3(1,1,0));
    float n001 = DissolveHash3D(i + float3(0,0,1));    float n101 = DissolveHash3D(i + float3(1,0,1));
    float n011 = DissolveHash3D(i + float3(0,1,1));    float n111 = DissolveHash3D(i + float3(1,1,1));

    return lerp(lerp(lerp(n000,n100,f.x), lerp(n010,n110,f.x), f.y),
                lerp(lerp(n001,n101,f.x), lerp(n011,n111,f.x), f.y), f.z);
}

float DissolveNoise(float3 p, float scale)
{
    p *= scale;
    return DissolveValueNoise3D(p) * 0.6
         + DissolveValueNoise3D(p * 2.13) * 0.3
         + DissolveValueNoise3D(p * 4.37) * 0.1;
}

// Returns the dissolve threshold. Clips the pixel if below threshold.
// Also outputs distToEdge for edge glow calculations.
void DissolveClip(float3 positionWS, out float distToEdge)
{
    float timeOffset = (_Time.y + _DissolveSeed * 17.173) * _NoiseScrollSpeed;
    float3 scrollVec = float3(0.37, 0.53, 0.71) * timeOffset;

    float baseNoise = DissolveNoise(positionWS + scrollVec, _NoiseScale);
    float warpNoise = DissolveNoise(positionWS + 29.37 + scrollVec * 0.43, _NoiseScale * 1.93);
    float warpedNoise = saturate(baseNoise + (warpNoise - 0.5) * _DissolveNoiseWarp);

    float height01 = saturate((positionWS.y - _DissolveHeightMin) * _DissolveHeightInvRange);
    float noise = lerp(warpedNoise, height01, _DissolveHeightStrength);

    float threshold = _DissolveAmount;
    float dist = noise - threshold;

    // Secondary high-frequency threshold near the frontier to chip the silhouette.
    float flakeBand = saturate(1.0 - abs(dist) / max(_FlakeBandWidth, 0.0001));
    float flakeTime = (_Time.y + _DissolveSeed * 5.13) * _FlakeFlickerSpeed;
    float3 flakePos = positionWS * _FlakeNoiseScale
                    + float3(flakeTime, -flakeTime * 0.61, flakeTime * 1.37);
    float flakeNoise = DissolveValueNoise3D(flakePos);
    float flakeCut = step(flakeNoise, _FlakeCutout);
    dist -= flakeCut * flakeBand * _FlakeBandWidth;

    clip(dist);
    distToEdge = dist;
}

#endif
