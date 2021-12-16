Shader "MT/WavingGrassPreZ"
{
    Properties
    {
        _MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
		_Cutoff("Cutoff", float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue" = "Geometry+100"}
        LOD 100

        Pass
        {
            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex WavingGrassVertInstance
			#pragma fragment LitPassFragmentGrassInstance
			#define _ALPHATEST_ON

			#include "./MTWavingGrassInput.hlsl"
			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float4, _PerInstanceColor)
			UNITY_INSTANCING_BUFFER_END(Props)
			
			uniform float4 _Grass_Press_Point;

			GrassVertexOutput WavingGrassVertInstance(GrassVertexInput v)
			{
				GrassVertexOutput o = (GrassVertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			
				InitializeVertData(v, o);

				float3 worldPos = o.posWSShininess.xyz;
				float3 samplePos = worldPos.xyz / _WindControl.w;
				float waveT = dot(samplePos, samplePos) + _Time.y * _WaveSpeed;
				float3 waveOffset = sin(waveT) * _WindControl.xyz * v.color.a;
				//press
				float radius = max(_Grass_Press_Point.w, 0.01);
				float3 rangeV = worldPos - _Grass_Press_Point.xyz;
				float pressVal = clamp(dot(rangeV, rangeV), 0, radius);
				pressVal = lerp(1, 0, pressVal / radius);
				float3 pressOffset = radius * pow(pressVal, 2.0) * normalize(rangeV);
				//wave
				worldPos += waveOffset + pressOffset;				
				//
				o.posWSShininess.xyz = worldPos.xyz;
				o.clipPos = TransformWorldToHClip(worldPos);
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
				half alpha = diffuseAlpha.a;
				AlphaDiscard(alpha, _Cutoff);
				return 0;
			}
            ENDHLSL
        }
    }
}
