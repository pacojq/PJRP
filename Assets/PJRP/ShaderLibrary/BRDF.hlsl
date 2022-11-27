#ifndef PJRP_BRDF_INCLUDED
#define PJRP_BRDF_INCLUDED

struct BRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
};


// Minimum amount of light bouncing off a dielectric surface.
// As average we'll have a value of 0.04 for nonmetals' reflectivity.
#define MIN_REFLECTIVITY 0.04

float OneMinusReflectivity(float metallic)
{
    float range = 1.0 - MIN_REFLECTIVITY;
    return range - metallic * range;
}


/* SPECULAR STRENGTH IS DEFINED AS:

                r^2
    ----------------------------
    d^2  max( 0.1, (L · H)^2 ) n

WHERE:
    - r   : roughness
    - N   : surface normal
    - L   : light direction
    - H   : L + V, normalized
    - V   : view direction
    - d   : d = (N · H)^2 (r^2 - 1) + 1.0001
*/
float SpecularStrength(Surface surface, BRDF brdf, Light light)
{
    const float3 h = SafeNormalize(light.direction + surface.viewDirection); // safe-normalize avoids division by zero
    const float nh2 = Square(saturate(dot(surface.normal, h)));
    const float lh2 = Square(saturate(dot(light.direction, h)));
    const float r2 = Square(brdf.roughness);
    const float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
    const float normalization = brdf.roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1, lh2) * normalization);
}



BRDF GetBRDF(inout Surface surface, bool applyAlphaToDiffuse = false)
{
    BRDF brdf;

    const float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * oneMinusReflectivity;
    if (applyAlphaToDiffuse) brdf.diffuse *= surface.alpha;
    
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
    
    const float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness); // Unity Core RP function
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness); // Unity Core RP function

    return brdf;
}


float3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

#endif