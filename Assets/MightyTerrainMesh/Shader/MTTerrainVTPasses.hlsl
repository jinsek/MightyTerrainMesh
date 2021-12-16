#ifndef UNIVERSAL_MTTERRAIN_VT_PASSES_INCLUDED
#define UNIVERSAL_MTTERRAIN_VT_PASSES_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(_Terrain)
float4 _Control_ST;
float4 _Control_TexelSize;
half4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;
half4 _DiffuseRemapScale0, _DiffuseRemapScale1, _DiffuseRemapScale2, _DiffuseRemapScale3;
half _Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3;
half _HasMask0, _HasMask1, _HasMask2, _HasMask3;
float4 _BakeScaleOffset;
CBUFFER_END
TEXTURE2D(_Control);    SAMPLER(sampler_Control);
TEXTURE2D(_Splat0);     SAMPLER(sampler_Splat0);
TEXTURE2D(_Splat1);
TEXTURE2D(_Splat2);
TEXTURE2D(_Splat3);
TEXTURE2D(_Mask0);      SAMPLER(sampler_Mask0);
TEXTURE2D(_Mask1);
TEXTURE2D(_Mask2);
TEXTURE2D(_Mask3);


struct Attributes
{
	float4 positionOS       : POSITION;
	float2 texcoord               : TEXCOORD0;
};

struct Varyings
{
	float4 uvSplat01                : TEXCOORD1; // xy: splat0, zw: splat1
	float4 uvSplat23                : TEXCOORD2; // xy: splat2, zw: splat3
	float4 uv						: TEXCOORD0;
	float4 vertex : SV_POSITION;
};

void SplatmapMix(float4 uvSplat01, float4 uvSplat23, inout half4 splatControl, out half weight, out half4 mixedDiffuse, out half4 defaultSmoothness)
{
	half4 diffAlbedo[4];

	diffAlbedo[0] = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, uvSplat01.xy);
	diffAlbedo[1] = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, uvSplat01.zw);
	diffAlbedo[2] = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, uvSplat23.xy);
	diffAlbedo[3] = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, uvSplat23.zw);

	defaultSmoothness = half4(diffAlbedo[0].a, diffAlbedo[1].a, diffAlbedo[2].a, diffAlbedo[3].a);
    defaultSmoothness *= half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);

	// Now that splatControl has changed, we can compute the final weight and normalize
	weight = dot(splatControl, 1.0h);

//#ifdef TERRAIN_SPLAT_ADDPASS
//	clip(weight <= 0.005h ? -1.0h : 1.0h);
//#endif
	mixedDiffuse = 0.0h;
	mixedDiffuse += diffAlbedo[0] * half4(_DiffuseRemapScale0.rgb * splatControl.rrr, 1.0h);
	mixedDiffuse += diffAlbedo[1] * half4(_DiffuseRemapScale1.rgb * splatControl.ggg, 1.0h);
	mixedDiffuse += diffAlbedo[2] * half4(_DiffuseRemapScale2.rgb * splatControl.bbb, 1.0h);
	mixedDiffuse += diffAlbedo[3] * half4(_DiffuseRemapScale3.rgb * splatControl.aaa, 1.0h);
}

half ComputeSmoothness(float4 uvSplat01, float4 uvSplat23, half4 splatControl, half4 defaultSmoothness)
{
	half4 masks[4];
    masks[0] = 0.5h;
    masks[1] = 0.5h;
    masks[2] = 0.5h;
    masks[3] = 0.5h;
    half4 hasMask = half4(_HasMask0, _HasMask1, _HasMask2, _HasMask3);
	
    masks[0] = lerp(masks[0], SAMPLE_TEXTURE2D(_Mask0, sampler_Mask0, uvSplat01.xy), hasMask.x);
    masks[1] = lerp(masks[1], SAMPLE_TEXTURE2D(_Mask1, sampler_Mask0, uvSplat01.zw), hasMask.y);
    masks[2] = lerp(masks[2], SAMPLE_TEXTURE2D(_Mask2, sampler_Mask0, uvSplat23.xy), hasMask.z);
    masks[3] = lerp(masks[3], SAMPLE_TEXTURE2D(_Mask3, sampler_Mask0, uvSplat23.zw), hasMask.w);
	
    half4 maskSmoothness = half4(masks[0].a, masks[1].a, masks[2].a, masks[3].a);
    defaultSmoothness = lerp(defaultSmoothness, maskSmoothness, hasMask);
    return dot(splatControl, defaultSmoothness);
}

Varyings vert(Attributes v)
{
	Varyings o = (Varyings)0;
	o.uv.zw = v.texcoord;
	v.texcoord.xy = v.texcoord.xy * _BakeScaleOffset.xy + _BakeScaleOffset.zw;

	VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
	o.vertex = vertexInput.positionCS;
	o.uv.xy = v.texcoord;
	o.uvSplat01.xy = TRANSFORM_TEX(v.texcoord, _Splat0);
	o.uvSplat01.zw = TRANSFORM_TEX(v.texcoord, _Splat1);
	o.uvSplat23.xy = TRANSFORM_TEX(v.texcoord, _Splat2);
	o.uvSplat23.zw = TRANSFORM_TEX(v.texcoord, _Splat3);

	return o;
}

half4 frag(Varyings IN) : SV_Target
{
	float2 splatUV = (IN.uv.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
	half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);

	half weight;
	half4 mixedDiffuse;
	half4 defaultSmoothness;
	SplatmapMix(IN.uvSplat01, IN.uvSplat23, splatControl, weight, mixedDiffuse, defaultSmoothness);
	half smoothness = ComputeSmoothness(IN.uvSplat01, IN.uvSplat23, splatControl,defaultSmoothness);

	return half4(mixedDiffuse.rgb, smoothness);
}

half4 fragAdd(Varyings IN) : SV_Target
{
	float2 splatUV = (IN.uv.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
	half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);

	half weight;
	half4 mixedDiffuse;
	half4 defaultSmoothness;
	SplatmapMix(IN.uvSplat01, IN.uvSplat23, splatControl, weight, mixedDiffuse, defaultSmoothness);
    clip(weight <= 0.005h ? -1.0h : 1.0h);
	half smoothness = ComputeSmoothness(IN.uvSplat01, IN.uvSplat23, splatControl, defaultSmoothness);
	
	return half4(mixedDiffuse.rgb, smoothness * weight);
}

#endif
