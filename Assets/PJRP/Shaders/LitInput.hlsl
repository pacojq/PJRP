#ifndef PJRP_LIT_INPUT_INCLUDED
#define PJRP_LIT_INPUT_INCLUDED


#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)


TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

// Combined mask with Metallic, Occlusion, Detail, and Smoothness data,
// stored in channels RGBA respectively.
TEXTURE2D(_MaskMap);

TEXTURE2D(_NormalMap);

TEXTURE2D(_EmissionMap);

// We can add finer detail to objects when looked up close. Will be controlled
// by the Blue channel in _MaskMap.
TEXTURE2D(_DetailMap);
SAMPLER(sampler_DetailMap);

TEXTURE2D(_DetailNormalMap);


UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)

    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)

	UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)

    UNITY_DEFINE_INSTANCED_PROP(float4, _DetailMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float, _DetailAlbedo)
    UNITY_DEFINE_INSTANCED_PROP(float, _DetailSmoothness)
    UNITY_DEFINE_INSTANCED_PROP(float, _DetailNormalScale)

    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
	UNITY_DEFINE_INSTANCED_PROP(float, _Occlusion)
    UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)

    UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)

    UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)



inline float4 GetMask(float2 baseUV)
{
    return SAMPLE_TEXTURE2D(_MaskMap, sampler_BaseMap, baseUV);
}

inline float2 TransformDetailUV(float2 detailUV)
{
    float4 detailST = INPUT_PROP(_DetailMap_ST);
    return detailUV * detailST.xy + detailST.zw;
}

inline float4 GetDetail(float2 detailUV)
{
    const float4 map = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, detailUV);
    return map * 2.0 - 1.0;
}

inline float2 TransformBaseUV(float2 baseUV)
{
    float4 baseST = INPUT_PROP(_BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}

inline float4 GetBase(float2 baseUV, float2 detailUV = 0.0)
{
    float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
    float4 color = INPUT_PROP(_BaseColor);

    float mask = GetMask(baseUV).b;
    float detail = GetDetail(detailUV).r * INPUT_PROP(_DetailAlbedo);
    map.rgb = lerp(sqrt(map.rgb), detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
	map.rgb *= map.rgb;
    
    return map * color;
}

inline float GetCutoff(float2 baseUV)
{
    return INPUT_PROP(_Cutoff);
}

inline float GetMetallic(float2 baseUV)
{
    float metallic = INPUT_PROP(_Metallic);
    metallic *= GetMask(baseUV).r;
    return metallic;
}

inline float GetSmoothness(float2 baseUV, float2 detailUV = 0.0)
{
    float smoothness = INPUT_PROP(_Smoothness);
    smoothness *= GetMask(baseUV).a;

    float detail = GetDetail(detailUV).b * INPUT_PROP(_DetailSmoothness);
    float mask = GetMask(baseUV).b;
    smoothness = lerp(smoothness, detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
    
    return smoothness;
}

inline float GetFresnel(float2 baseUV)
{
    return INPUT_PROP(_Fresnel);
}

inline float3 GetNormalTS(float2 baseUV, float2 detailUV = 0.0)
{
    float4 map = SAMPLE_TEXTURE2D(_NormalMap, sampler_BaseMap, baseUV);
    float scale = INPUT_PROP(_NormalScale);
    float3 normal = DecodeNormal(map, scale);

    map = SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailMap, detailUV);
    scale = INPUT_PROP(_DetailNormalScale) * GetMask(baseUV).b;
    float3 detail = DecodeNormal(map, scale);
    normal = BlendNormalRNM(normal, detail);
    
    return normal;
}

inline float3 GetEmission(float2 baseUV)
{
    float4 map = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, baseUV);
    float4 color = INPUT_PROP(_EmissionColor);
    return map.rgb * color.rgb;
}

inline float GetOcclusion(float2 baseUV)
{
    const float strength = INPUT_PROP(_Occlusion);
    float occlusion = GetMask(baseUV).g;
    occlusion = lerp(occlusion, 1.0, strength);
    return occlusion;
}

#endif