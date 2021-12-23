// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
Shader "MT/WavingGrass"
{
    Properties
    {
        _WavingTint ("Fade Color", Color) = (1, 1, 1, 1)
        _MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
		[Toggle(_NORMALMAP)]_NORMALMAP("Use Normal", Float) = 0
        _BumpMap("Normal Map", 2D) = "bump" {}
		_Cutoff("Cutoff", float) = 0.5
		_WindControl("Wind Control", Vector) = (1, 1, 1, 1)
		_WaveSpeed("Wave Speed", Float) = 1
		_Smoothness("Smoothness", Float) = 1.0
		_Transluency("Transluency", Range(0, 1)) = 0.5
		[Toggle(FORCE_UP_NORMAL)]FORCE_UP_NORMAL("Force Up Normal", Float) = 0
		[Toggle(INTERACTIVE)]INTERACTIVE("Interactive", Float) = 0
	}
	SubShader
	{
		Tags {"Queue" = "Geometry+200" "RenderType" = "Grass" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }//"DisableBatching"="True"
		Cull Off
		LOD 200
		AlphaTest Greater[_Cutoff]
		ColorMask RGB

		Pass
		{
			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			// -------------------------------------
			// Universal Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_fog

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			///////////my defined
			#pragma multi_compile _ FORCE_UP_NORMAL
			#pragma multi_compile _ INTERACTIVE
			#pragma multi_compile _ _NORMALMAP			
			#pragma vertex WavingGrassVertInstance
			#pragma fragment LitPassFragmentGrassInstance
			#define _ALPHATEST_ON
			
			#if defined(INTERACTIVE)
			uniform float4 _Grass_Press_Point;
			#endif

			#include "./MTWavingGrassInput.hlsl"
			#include "./MTWavingGrassLighting.hlsl"
			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float4, _PerInstanceColor)
			UNITY_INSTANCING_BUFFER_END(Props)

			GrassVertexOutput WavingGrassVertInstance(GrassVertexInput v)
			{
				GrassVertexOutput o = (GrassVertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			
				InitializeVertData(v, o);
				o.color = v.color;

				return o;
			}
			half4 LitPassFragmentGrassInstance(GrassVertexOutput input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input); 
				half4 perInstance_c = UNITY_ACCESS_INSTANCED_PROP(Props, _PerInstanceColor);
				
				SurfaceData surfaceData;
				surfaceData = (SurfaceData)0;

				float2 uv = input.uv;
				half4 diffuseAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex));
				surfaceData.alpha = diffuseAlpha.a;
				surfaceData.albedo = diffuseAlpha.rgb * perInstance_c.xyz * _WavingTint.rgb;
				#ifdef _NORMALMAP
				surfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
				#else
				surfaceData.normalTS = 0;
				#endif
				surfaceData.smoothness = _Smoothness;
				surfaceData.occlusion = 1.0;
				surfaceData.emission = 0.0;
				surfaceData.clearCoatMask = half(0.0);
				surfaceData.clearCoatSmoothness = half(0.0);

				AlphaDiscard(surfaceData.alpha, _Cutoff);

				InputData inputData;
				InitializeInputData(input, surfaceData.normalTS, inputData);
				#ifdef _NORMALMAP
				half4 color = FragmentTransluentPBR(inputData, surfaceData);
				#else
				half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);
				#endif
				color.rgb = MixFog(color.rgb, inputData.fogCoord);
				return color;
			}
			ENDHLSL
		}
    }
}
