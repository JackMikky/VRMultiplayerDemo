Shader "Custom/SimpleColorFilter"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    TEXTURE2D(_BlitTexture);
    SAMPLER(sampler_LinearClamp);

    Varyings Vert(Attributes input)
    {
        Varyings output;
        
        // 生成全屏三角形
        float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
        output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
        
        #if UNITY_UV_STARTS_AT_TOP
        uv.y = 1.0 - uv.y;
        #endif
        
        output.uv = uv;
        return output;
    }

    half4 Frag(Varyings input) : SV_Target
    {
        // 采样相机颜色
        half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.uv);
        
        // 将红色通道设为 0
        color.r = 0;
        
        return color;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "SimpleColorFilter"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}