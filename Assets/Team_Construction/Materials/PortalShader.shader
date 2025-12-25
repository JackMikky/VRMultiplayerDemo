Shader "Custom/PortalShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white"
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // 中心座標
                float2 center = float2(0.5, 0.5);
                float2 dir = uv - center;

                // 極座標変換
                float r = length(dir); // 半径
                float theta = atan2(dir.y, dir.x); // 角度（ラジアン）

                // 歪み（中心から放射状に揺らぎ）
                //uv += normalize(dir) * sin(r * 20 - _Time.z * 100) * 0.0025;

                // テクスチャカラー取得
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;

                // 開口部楕円形ににする場合以下を有効化
                if(false)
                {
                    // 揺らぎ付きフェード範囲
                    float fadeStart = 0.40;
                    float fadeEnd = 0.5;

                    // 極座標ベースの揺らぎ（角度と時間で変化）
                    float wobble = sin(theta * 6 + _Time.y * 2) * 0.02;

                    // 揺らぎを加えた距離で透明度を計算
                    float alpha = saturate(1.0 - ((r + wobble) - fadeStart) / (fadeEnd - fadeStart));
                    color.a *= alpha;
                }

                return color;
            }

            ENDHLSL
        }
    }
}
