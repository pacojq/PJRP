/*
    This file defines the multiple built-in elements
    that unity offers and we are going to use in our
    render pipeline.
*/
#ifndef PJRP_UNITY_INPUT_INCLUDED
#define PJRP_UNITY_INPUT_INCLUDED

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade;
    real4 unity_WorldTransformParams;


    // vv== LIGHTMAPING =================vv

	float4 unity_LightmapST;
	float4 unity_DynamicLightmapST;

    // Light probes
    float4 unity_SHAr;
    float4 unity_SHAg;
    float4 unity_SHAb;
    float4 unity_SHBr;
    float4 unity_SHBg;
    float4 unity_SHBb;
    float4 unity_SHC;

    // Light probe proxy volumes
    float4 unity_ProbeVolumeParams;
    float4x4 unity_ProbeVolumeWorldToObject;
    float4 unity_ProbeVolumeSizeInv;
    float4 unity_ProbeVolumeMin;

    // ^^== LIGHTMAPING =================^^

CBUFFER_END

float4x4 unity_MatrixVP; // View-projection matrix. Transform from World Space to Homogeneous Clip Space
float4x4 unity_MatrixV; // View matrix.
float4x4 glstate_matrix_projection; // Projection matrix.

float3 _WorldSpaceCameraPos;

#endif