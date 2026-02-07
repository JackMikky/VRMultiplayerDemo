Shader "CustomRenderFeature/HeartMosaic"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Speed ("Speed", Range(0,5)) = 1.0
        _MosaicIntensity ("Mosaic Intensity", Float) = 10.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
        
        Pass
        {
            Name "HeartMosaicPass"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/VRMPAssets/Shaders/ShaderFunctionScripts/ShaderUtility.hlsl"
            #include "Assets/VRMPAssets/Shaders/ShaderFunctionScripts/HeartMosic.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float _Speed;
                float _MosaicIntensity;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            
            float4 HeartMosaic(float2 uv, float2 resolution)
            {
                float time = remap(sin(_Time.y * _Speed), -1.0, 1.0, 0.0, 1.5);
                float mosaicSize = _MosaicIntensity * time;
                float4 outp = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                
                if (mosaicSize > 1.0)
                {
                    float2 tex = uv * resolution;
                    float2 g_v2MosaicSize = float2(mosaicSize, mosaicSize);
                    
                    float2 center = float2(
                        round(tex.x / g_v2MosaicSize.x) * g_v2MosaicSize.x,
                        round(tex.y / g_v2MosaicSize.y) * g_v2MosaicSize.y
                    );
                    
                    center += g_v2MosaicSize / 2.0;
                    tex -= center;
                    
                    if (inHeart(tex, mosaicSize / 2.4) > 0.0)
                    {
                        outp = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, center / resolution);
                    }
                }
                
                return outp;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float2 resolution = float2(1.0 / _MainTex_TexelSize.x, 1.0 / _MainTex_TexelSize.y);
                return HeartMosaic(input.uv, resolution);
            }
            ENDHLSL
        }
    }
}