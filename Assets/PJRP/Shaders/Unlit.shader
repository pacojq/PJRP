Shader "PJRP/Unlit"
{
	Properties
	{
		_BaseMap("Texture", 2D) = "white" {}
		[HDR] _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
		
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
		
		/// Unity has a hardcoded approach for baked transparency, looking at "_MainTex"
        /// and "_Color" material properties, and using "_Cutoff" for alpha clipping.
		[HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
		[HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)
	}
	SubShader
	{
		HLSLINCLUDE
		#include "../ShaderLibrary/Common.hlsl"
		#include "UnlitInput.hlsl"
		ENDHLSL
		
		Pass
		{
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			Cull [_Cull]
			
			HLSLPROGRAM
			#pragma target 3.5

			#pragma shader_feature _CLIPPING
			
			#pragma multi_compile_instancing
			
			
			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment
			
			#include "UnlitPass.hlsl"
			
			ENDHLSL	
		}
		
		Pass 
		{
			Tags 
			{
				"LightMode" = "Meta"
			}

			Cull Off

			HLSLPROGRAM
			#pragma target 3.5
			
			#pragma vertex MetaPassVertex
			#pragma fragment MetaPassFragment
			
			#include "MetaPass.hlsl"
			
			ENDHLSL
		}
	}
	
	CustomEditor "PJRP.Editor.PJRPShaderGUI"
}