Shader "MT/VTDiffuseAdd"
{
	Properties{
		[HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
		[HideInInspector] _Splat3("Layer 3 (A)", 2D) = "grey" {}
		[HideInInspector] _Splat2("Layer 2 (B)", 2D) = "grey" {}
		[HideInInspector] _Splat1("Layer 1 (G)", 2D) = "grey" {}
		[HideInInspector] _Splat0("Layer 0 (R)", 2D) = "grey" {}
		[HideInInspector] _DiffuseRemapScale0("_DiffuseRemapScale0", Color) = (1, 1, 1, 1)
		[HideInInspector] _DiffuseRemapScale1("_DiffuseRemapScale1", Color) = (1, 1, 1, 1)
		[HideInInspector] _DiffuseRemapScale2("_DiffuseRemapScale2", Color) = (1, 1, 1, 1)
		[HideInInspector] _DiffuseRemapScale3("_DiffuseRemapScale3", Color) = (1, 1, 1, 1)
        [HideInInspector] _Mask3("Mask 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Mask2("Mask 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Mask1("Mask 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Mask0("Mask 0 (R)", 2D) = "grey" {}
        [HideInInspector] _Smoothness0("Smoothness 0", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness1("Smoothness 1", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness2("Smoothness 2", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Smoothness3("Smoothness 3", Range(0.0, 1.0)) = 0.5
		[HideInInspector] _HasMask0("Has Mask 0", Float) = 0.0
		[HideInInspector] _HasMask1("Has Mask 1", Float) = 0.0
		[HideInInspector] _HasMask2("Has Mask 2", Float) = 0.0
		[HideInInspector] _HasMask3("Has Mask 3", Float) = 0.0
	}
	HLSLINCLUDE
#pragma multi_compile __ _ALPHATEST_ON
	ENDHLSL

	SubShader
	{
		Tags { "Queue" = "Geometry-99" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "False"}

		Pass
		{
			Name "ForwardAddLit"
			Tags { "LightMode" = "UniversalForward" }
			ZTest Always ZWrite Off Cull Off
			Blend One One
			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 3.0

			#pragma vertex vert
			#pragma fragment fragAdd

			#include "MTTerrainVTPasses.hlsl"
			
			ENDHLSL
		}
	}
}
