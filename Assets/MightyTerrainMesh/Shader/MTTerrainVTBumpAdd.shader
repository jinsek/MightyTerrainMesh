Shader "MT/VTBumpAdd"
{
	Properties{
		[HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
		[HideInInspector] _Normal3("Normal 3 (A)", 2D) = "bump" {}
		[HideInInspector] _Normal2("Normal 2 (B)", 2D) = "bump" {}
		[HideInInspector] _Normal1("Normal 1 (G)", 2D) = "bump" {}
		[HideInInspector] _Normal0("Normal 0 (R)", 2D) = "bump" {}
		[HideInInspector] _NormalScale0("NormalScale 0", Range(0.0, 16.0)) = 1.0
		[HideInInspector] _NormalScale1("NormalScale 1", Range(0.0, 16.0)) = 1.0
		[HideInInspector] _NormalScale2("NormalScale 2", Range(0.0, 16.0)) = 1.0
		[HideInInspector] _NormalScale3("NormalScale 3", Range(0.0, 16.0)) = 1.0
        [HideInInspector] _Mask3("Mask 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Mask2("Mask 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Mask1("Mask 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Mask0("Mask 0 (R)", 2D) = "grey" {}
        [HideInInspector][Gamma] _Metallic0("Metallic 0", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic1("Metallic 1", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic2("Metallic 2", Range(0.0, 1.0)) = 0.0
        [HideInInspector][Gamma] _Metallic3("Metallic 3", Range(0.0, 1.0)) = 0.0
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
			Name "ForwardLit"
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

			#include "MTTerrainVTBumpPasses.hlsl"
			
			ENDHLSL
		}
	}
}
