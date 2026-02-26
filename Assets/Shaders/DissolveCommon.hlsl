#ifndef DISSOLVE_COMMON_INCLUDED
#define DISSOLVE_COMMON_INCLUDED

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;
    half   _Metallic;
    half   _Smoothness;
    half   _BumpScale;
    half   _OcclusionStrength;
    half4  _EmissionColor;
    float  _DissolveAmount;
    float  _EdgeWidth;
    half4  _EdgeColor;
    float  _NoiseScale;
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
void DissolveClip(float3 positionOS, out float distToEdge)
{
    float noise = DissolveNoise(positionOS, _NoiseScale);
    float threshold = _DissolveAmount * 1.05;
    clip(noise - threshold);
    distToEdge = noise - threshold;
}

#endif
