Shader "MT/TerrainVTLit" 
{
	Properties{
		_Diffuse("Diffuse", 2D) = "grey" {}
		_Normal("Normal", 2D) = "grey" {}
	}
	HLSLINCLUDE
#pragma multi_compile __ _ALPHATEST_ON
	ENDHLSL

	SubShader
	{
		Tags { "Queue" = "Geometry-100" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "False"}

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }
			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 3.0

			#pragma vertex vert
			#pragma fragment frag

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
			#pragma multi_compile_instancing

			#pragma shader_feature_local _NORMALMAP

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			CBUFFER_START(UnityPerMaterial)
				half4 _Diffuse_ST;
			CBUFFER_END

			TEXTURE2D(_Diffuse);     SAMPLER(sampler_Diffuse);
			TEXTURE2D(_Normal);     SAMPLER(sampler_Normal);

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
			};

			struct Varyings
			{
				float4 uvMainAndLM              : TEXCOORD0; // xy: control, zw: lightmap

#if defined(_NORMALMAP)
				float4 normal                   : TEXCOORD1;    // xyz: normal, w: viewDir.x
				float4 tangent                  : TEXCOORD2;    // xyz: tangent, w: viewDir.y
				float4 bitangent                : TEXCOORD3;    // xyz: bitangent, w: viewDir.z
#else
				float3 normal                   : TEXCOORD1;
				float3 positionWS               : TEXCOORD2;
				half3 vertexSH                  : TEXCOORD3; // SH
#endif

				half4 fogFactorAndVertexLight   : TEXCOORD4; // x: fogFactor, yzw: vertex light
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				float4 shadowCoord              : TEXCOORD6;
#endif
				float4 clipPos                  : SV_POSITION;
			};
			Varyings vert(Attributes v)
			{
				Varyings o = (Varyings)0;
				
				VertexPositionInputs Attributes = GetVertexPositionInputs(v.positionOS.xyz);

				o.uvMainAndLM.xy = TRANSFORM_TEX(v.texcoord, _Diffuse);
				o.uvMainAndLM.zw = v.texcoord1 * unity_LightmapST.xy + unity_LightmapST.zw;

#if defined(_NORMALMAP)
				float4 vertexTangent = float4(cross(float3(0, 0, 1), v.normalOS), 1.0);
				VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS, vertexTangent);

				o.normal = half4(normalInput.normalWS, Attributes.positionWS.x);
				o.tangent = half4(normalInput.tangentWS, Attributes.positionWS.y);
				o.bitangent = half4(normalInput.bitangentWS, Attributes.positionWS.z);
#else
				o.normal = TransformObjectToWorldNormal(v.normalOS);
				o.positionWS = Attributes.positionWS;
				o.vertexSH = SampleSH(o.normal);
#endif
				o.fogFactorAndVertexLight.x = ComputeFogFactor(Attributes.positionCS.z);
				o.fogFactorAndVertexLight.yzw = VertexLighting(Attributes.positionWS, o.normal.xyz);
				o.clipPos = Attributes.positionCS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				o.shadowCoord = GetShadowCoord(Attributes);
#endif
				return o;
			}

			void InitializeInputData(Varyings IN, half3 normalTS, out InputData input)
			{
				input = (InputData)0;

				half3 SH = half3(0, 0, 0);

#if defined(_NORMALMAP)
				input.positionWS = half3(IN.normal.w, IN.tangent.w, IN.bitangent.w);
				input.normalWS = TransformTangentToWorld(normalTS, half3x3(-IN.tangent.xyz, IN.bitangent.xyz, IN.normal.xyz));
				SH = SampleSH(input.normalWS.xyz);
#else
				input.positionWS = IN.positionWS;
				input.normalWS = IN.normal;
				SH = IN.vertexSH;
#endif
				input.normalWS = SafeNormalize(input.normalWS);
				input.viewDirectionWS = SafeNormalize(GetCameraPositionWS() - input.positionWS);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				input.shadowCoord = IN.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
				input.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#else
				input.shadowCoord = float4(0, 0, 0, 0);
#endif

				input.fogCoord = IN.fogFactorAndVertexLight.x;
				input.vertexLighting = IN.fogFactorAndVertexLight.yzw;
				
				//NOTE SAMPLE_GI 会调用SampleSHPixel函数，这个函数有问题，手机上使用的是EVALUATE_SH_MIXED宏
				//pc使用的是EVALUATE_SH_VERTEX
				#if defined(LIGHTMAP_ON)
				input.bakedGI = SAMPLE_GI(IN.uvMainAndLM.zw, SH, input.normalWS);
				#else
				input.bakedGI = SH;
				#endif
			}
			// Used in Standard Terrain shader
			half4 frag(Varyings IN) : SV_TARGET
			{
				half4 mixedDiffuse = SAMPLE_TEXTURE2D(_Diffuse, sampler_Diffuse, IN.uvMainAndLM.xy);
				half4 mixedNormal = SAMPLE_TEXTURE2D(_Normal, sampler_Normal, IN.uvMainAndLM.xy);
				half3 normalTS = 0;
				normalTS.xy = mixedNormal.xy * 2 - 1;
				normalTS.z = sqrt(1 - normalTS.x * normalTS.x - normalTS.y * normalTS.y);
				half3 albedo = mixedDiffuse.rgb;

				half smoothness = mixedDiffuse.a;
				half metallic = mixedNormal.a;
				half occlusion = mixedNormal.b;
				half alpha = 1.0;

				InputData inputData;
				InitializeInputData(IN, normalTS, inputData);
				half4 color = UniversalFragmentPBR(inputData, albedo, metallic, /* specular */ half3(0.0h, 0.0h, 0.0h), smoothness, occlusion, /* emission */ half3(0, 0, 0), alpha);

				color.rgb = MixFog(color.rgb, inputData.fogCoord);

				return half4(color.rgb, 1.0h);
			}

			ENDHLSL
		}
	}
	Fallback "Hidden/Universal Render Pipeline/FallbackError"
}