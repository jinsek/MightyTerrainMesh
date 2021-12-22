#ifndef WATER_INPUT_INCLUDED
#define WATER_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
float4 _BumpMap_ST;
half4 _BaseColor;
half _Smoothness;
half _BumpScale;
half _MaxDepth;
half _WaterAbsorption;
half _WavesSpeed;
half _RefractionStrength;
CBUFFER_END

inline void InitializeStandardLitSurfaceData(float2 uv, float3 screenUV, float3 positionWS, out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;
    //anim water normal uv
    float2 bumpUV = uv;
    float wave_uv_offset = _Time.y * _WavesSpeed;
    float2 wave_uv_offset1 = wave_uv_offset;
    float2 wave_uv_offset2 = (1.0 - wave_uv_offset); 
    outSurfaceData.normalTS = SampleNormal(uv + wave_uv_offset1, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    outSurfaceData.normalTS += SampleNormal(uv + wave_uv_offset2, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);    
    //depth transparent
    float2 screen_uv = (screenUV.xy / screenUV.z);
    float depth = SampleSceneDepth(screen_uv);
    float3 depthWSPos = ComputeWorldSpacePosition(screen_uv, depth, UNITY_MATRIX_I_VP);
    float waterDepth = length(positionWS - depthWSPos);
    float waterDepth01 = clamp(waterDepth / _MaxDepth, 0, 1);
    half alpha = _BaseColor.a * waterDepth01;
    //depth color
    float3 backgroundColor = SampleSceneColor(screen_uv + outSurfaceData.normalTS.xz * _RefractionStrength);
    float depthAbsorption = exp2(-_WaterAbsorption * waterDepth01);
    half3 albedo =  lerp(backgroundColor, _BaseColor.rgb, depthAbsorption);
    //
    outSurfaceData.alpha = alpha;
    outSurfaceData.albedo = albedo;
    outSurfaceData.smoothness = _Smoothness;
    outSurfaceData.occlusion = 1.0;
    outSurfaceData.emission = 0.0;
    outSurfaceData.clearCoatMask = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(0.0);
}

#endif // WATER_INPUT_INCLUDED
