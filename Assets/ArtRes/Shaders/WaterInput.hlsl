#ifndef WATER_INPUT_INCLUDED
#define WATER_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
float4 _BumpMap_ST;
half4 _BaseColor;
half _Smoothness;
half _BumpScale;
half _MaxDepth;
half _WavesSpeed;
CBUFFER_END

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;
    //anim water normal uv
    float2 bumpUV = uv;
    float wave_uv_offset = _Time.y * _WavesSpeed;
    float2 wave_uv_offset1 = wave_uv_offset;
    float2 wave_uv_offset2 = (1.0 - wave_uv_offset); 
    outSurfaceData.normalTS = SampleNormal(uv + wave_uv_offset1, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    outSurfaceData.normalTS += SampleNormal(uv + wave_uv_offset2, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    //
    outSurfaceData.alpha = _BaseColor.a;
    outSurfaceData.albedo = _BaseColor.rgb;
    outSurfaceData.smoothness = _Smoothness;
    outSurfaceData.occlusion = 1.0;
    outSurfaceData.emission = 0.0;
    outSurfaceData.clearCoatMask = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(0.0);
}

#endif // WATER_INPUT_INCLUDED
