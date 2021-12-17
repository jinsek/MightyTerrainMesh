// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
Shader "MT/WavingGrass"
{
    Properties
    {
        _WavingTint ("Fade Color", Color) = (1, 1, 1, 1)
        _MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
		_Cutoff("Cutoff", float) = 0.5
		_WindControl("Wind Control", Vector) = (1, 1, 1, 1)
		_WaveSpeed("Wave Speed", Float) = 1
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
			///////////my defined
			#pragma multi_compile _ FORCE_UP_NORMAL
			#pragma multi_compile _ INTERACTIVE
			#pragma vertex WavingGrassVertInstance
			#pragma fragment LitPassFragmentGrassInstance
			#define _ALPHATEST_ON
			
			#if defined(INTERACTIVE)
			uniform float4 _Grass_Press_Point;
			#endif

			#include "./MTWavingGrassInput.hlsl"
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

				float2 uv = input.uv;
				half4 diffuseAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex));
				half3 diffuse = diffuseAlpha.rgb * perInstance_c.xyz * _WavingTint.rgb;

				half alpha = diffuseAlpha.a;
				AlphaDiscard(alpha, _Cutoff);
				alpha *= input.color.a;

				half3 emission = 0;
				half4 specularGloss = 0.1;
				half shininess = input.posWSShininess.w;

				InputData inputData;
				InitializeInputData(input, inputData);
				half3 normalTS = 0.0;
				half4 color = UniversalFragmentBlinnPhong(inputData, diffuse, specularGloss, shininess, emission, alpha, normalTS);
				color.rgb = MixFog(color.rgb, inputData.fogCoord);
				return color;
			}
			ENDHLSL
		}
    }
}
