﻿#ifndef PJRP_UNIT_INPUT_INCLUDED
#define PJRP_UNLIT_INPUT_INCLUDED


#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)


TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)

    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)




struct InputConfig
{
    float2 baseUV;
};

InputConfig GetInputConfig(float2 baseUV)
{
    InputConfig c;
    c.baseUV = baseUV;
    return c;
}



inline float2 TransformBaseUV(float2 baseUV)
{
    float4 baseST = INPUT_PROP(_BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}

inline float4 GetBase(InputConfig c)
{
    float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, c.baseUV);
    float4 color = INPUT_PROP(_BaseColor);
    return map * color;
}

inline float GetCutoff(InputConfig c)
{
    return INPUT_PROP(_Cutoff);
}

inline float GetMetallic(InputConfig c)
{
    return 0.0f;
}

inline float GetSmoothness(InputConfig c)
{
    return 0.0f;
}

inline float GetFresnel(InputConfig c)
{
    return 0.0;
}

inline float3 GetEmission(InputConfig c)
{
    return GetBase(c).rgb;
}

inline float GetOcclusion(InputConfig c)
{
    return 1.0f;
}

#endif