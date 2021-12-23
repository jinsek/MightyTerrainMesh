#ifndef MTWAVING_GRASS_INPUT_INCLUDED
#define MTWAVING_GRASS_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
half4 _WavingTint;
float4 _WindControl;    // wind speed x, y, z, scale
float _WaveSpeed;  
float4 _MainTex_ST;
half4 _BaseColor;
half4 _SpecColor;
half4 _EmissionColor;
half _Cutoff;
half _Smoothness;
half _Transluency;
CBUFFER_END

TEXTURE2D(_MainTex);            SAMPLER(sampler_MainTex);		
// ---- Grass helpers

struct GrassVertexInput
{
	float4 vertex       : POSITION;
	float3 normal       : NORMAL;
	float4 tangent      : TANGENT;
	half4 color         : COLOR;
	float2 texcoord     : TEXCOORD0;
	float2 lightmapUV   : TEXCOORD1;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct GrassVertexOutput
{
	float2 uv                       : TEXCOORD0;
	DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

	float4 posWSShininess           : TEXCOORD2;    // xyz: posWS, w: Shininess * 128

	half3 normalWS                  : TEXCOORD3;
    half4 tangentWS                 : TEXCOORD4;    // xyz: tangent, w: sign

	half4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light

#ifdef _MAIN_LIGHT_SHADOWS
	float4 shadowCoord              : TEXCOORD6;
#endif
	half4 color                     : TEXCOORD7;

	float4 clipPos                  : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
		UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(GrassVertexOutput input, half3 normalTS, out InputData inputData)
{
	inputData = (InputData)0;
	inputData.positionWS = input.posWSShininess.xyz;

	half3 viewDirWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);
#if SHADER_HINT_NICE_QUALITY
	viewDirWS = SafeNormalize(viewDirWS);
#endif

#ifdef _NORMALMAP
	float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
    inputData.tangentToWorld = tangentToWorld;
    inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
#else
	inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
#endif

	inputData.viewDirectionWS = viewDirWS;
#ifdef _MAIN_LIGHT_SHADOWS
	inputData.shadowCoord = input.shadowCoord;
#else
	inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
	inputData.fogCoord = input.fogFactorAndVertexLight.x;
	inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
	inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
}

float4 MyTransformWorldToShadowCoord(float3 positionWS)
{
#ifdef _MAIN_LIGHT_SHADOWS_CASCADE
    half cascadeIndex = ComputeCascadeIndex(positionWS);
#else
    half cascadeIndex = 0;
#endif

    float4 shadowCoord = mul(_MainLightWorldToShadow[cascadeIndex], float4(positionWS, 1.0));
	if (cascadeIndex > 0) {
		shadowCoord.z = -1;
	}

    return float4(shadowCoord.xyz, cascadeIndex);
}
void InitializeVertData(GrassVertexInput input, inout GrassVertexOutput vertData)
{
	VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
	
	float3 worldPos = vertexInput.positionWS;
	float3 samplePos = worldPos.xyz / _WindControl.w;
	float waveT = dot(samplePos, samplePos) + _Time.y * _WaveSpeed;
	float3 waveOffset = sin(waveT) * _WindControl.xyz * input.color.a;
	//press
	#if defined(INTERACTIVE)
	float radius = max(_Grass_Press_Point.w, 0.01);
	float3 rangeV = worldPos - _Grass_Press_Point.xyz;
	float pressVal = clamp(dot(rangeV, rangeV), 0, radius);
	pressVal = lerp(1, 0, pressVal / radius);
	float3 pressOffset = radius * pow(pressVal, 2.0) * normalize(rangeV) * input.color.a;
	#else
	float3 pressOffset = 0;
	#endif
	//wave
	vertexInput.positionWS = worldPos + waveOffset + pressOffset;	

	vertData.uv = input.texcoord;
	vertData.posWSShininess.xyz = vertexInput.positionWS;
	vertData.posWSShininess.w = 32;
	vertData.clipPos = TransformWorldToHClip(vertexInput.positionWS);

	#if defined(FORCE_UP_NORMAL)
		vertData.normalWS = float3(0, 1, 0); 
		vertData.tangentWS = float4(0, 0, 1, 1); 
	#else
		VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangent);
		vertData.normalWS = normalInput.normalWS;
		half sign = input.tangent.w * GetOddNegativeScale();
		half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
		vertData.tangentWS = tangentWS;	
	#endif

	// We either sample GI from lightmap or SH.
	// Lightmap UV and vertex SH coefficients use the same interpolator ("float2 lightmapUV" for lightmap or "half3 vertexSH" for SH)
	// see DECLARE_LIGHTMAP_OR_SH macro.
	// The following funcions initialize the correct variable with correct data
	OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, vertData.lightmapUV);
	OUTPUT_SH(vertData.normalWS, vertData.vertexSH);

	half3 vertexLight = VertexLighting(vertexInput.positionWS, vertData.normalWS.xyz);
	half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
	vertData.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#ifdef _MAIN_LIGHT_SHADOWS
	vertData.shadowCoord = TransformWorldToShadowCoord(vertexInput.positionWS);	
#endif
}

#endif
