/*
    This file defines the multiple built-in elements
    that unity offers and we are going to use in our
    render pipeline.
*/
#ifndef PJRP_COMMON_INCLUDED
#define PJRP_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

#include "UnityInput.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection

#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
    #define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"


inline float Square(float v)
{
    return v * v;
}

inline float DistanceSquared(float3 pA, float3 pB)
{
	return dot(pA - pB, pA - pB);
}


/*
    When LOD Groups are marked to transition objects using Cross Fade,
    we will clip certain pixels of both the higher and lower LOD objects,
    so we can smoothly transition from one to the other.
*/
inline void ClipLOD(float2 positionCS, float fade)
{
#if defined(LOD_FADE_CROSSFADE)
    float dither = InterleavedGradientNoise(positionCS.xy, 0);
    clip(fade + (fade < 0.0 ? dither : -dither)); // Fade will be negative for the upcoming object
#endif
}


inline float3 DecodeNormal(float4 sample, float scale)
{
#if defined(UNITY_NO_DXT5nm)
    return UnpackNormalRGB(sample, scale);
#else
    return UnpackNormalmapRGorAG(sample, scale);
#endif
}


inline float3 NormalTangentToWorld(float3 normalTS, float3 normalWS, float4 tangentWS)
{
    float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w); // Unity Core RP function
    return TransformTangentToWorld(normalTS, tangentToWorld); // Unity Core RP function
}

#endif