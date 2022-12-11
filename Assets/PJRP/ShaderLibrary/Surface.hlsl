﻿#ifndef PJRP_SURFACE_INCLUDED
#define PJRP_SURFACE_INCLUDED

struct Surface
{
	float3 position;
    float3 normal;
	float3 interpolatedNormal; // Original surface normal, World-Space
	float3 viewDirection;
	float depth;
    float3 color;
    float alpha;
    float metallic;
	float occlusion;
    float smoothness;
	float fresnelStrength;
    float dither;
};

#endif