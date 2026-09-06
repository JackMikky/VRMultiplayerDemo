Shader "Hidden/Custom/SelectionOutlineComposite"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.45, 0, 1)
        _Thickness ("Thickness", Float) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
        }

        Pass
        {
            Name "SelectionOutlineComposite"

            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // 重要: Blit.hlsl より先に Core.hlsl を include する
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_MaskTex);
            SAMPLER(sampler_MaskTex);

            float4 _OutlineColor;
            float _Thickness;
            float4 _MaskTex_TexelSize;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                half4 sceneColor = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                );

                float center = SAMPLE_TEXTURE2D_X(
                    _MaskTex,
                    sampler_MaskTex,
                    uv
                ).r;

                float around = 0.0;

                [loop]
                for (int x = -8; x <= 8; x++)
                {
                    [loop]
                    for (int y = -8; y <= 8; y++)
                    {
                        float2 offsetPixels = float2(x, y);
                        float dist = length(offsetPixels);

                        if (dist <= _Thickness)
                        {
                            float2 offsetUV = offsetPixels * _MaskTex_TexelSize.xy;

                            float sampleMask = SAMPLE_TEXTURE2D_X(
                                _MaskTex,
                                sampler_MaskTex,
                                uv + offsetUV
                            ).r;

                            around = max(around, sampleMask);
                        }
                    }
                }

                // 選択オブジェクトの外側だけを残す
                float outline = saturate(around - center);

                half4 col = sceneColor;
                col.rgb = lerp(col.rgb, _OutlineColor.rgb, outline * _OutlineColor.a);
                return col;
            }

            ENDHLSL
        }
    }
}