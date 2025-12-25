Shader "Custom/UnlitBlink_OnlyShader"
{
    Properties
    {
        _BaseColor   ("Base Color", Color) = (1,1,1,1)
        _BlinkColor  ("Blink Color", Color) = (1,0,0,1)
        _PeriodSec   ("Blink Period (seconds)", Range(0.01, 10)) = 1.0
        _Duty        ("Blink Duty (ON ratio)", Range(0,1)) = 0.5
        _Smoothness  ("Edge Smoothness", Range(0,1)) = 0.0 // 0で完全な矩形、>0でエッジを滑らかに
        _MainTex     ("Main Tex", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _BaseColor;
            fixed4 _BlinkColor;
            float  _PeriodSec;
            float  _Duty;
            float  _Smoothness;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // 0..1 の sawtooth を生成: _Time.y は経過時間（秒）
            float Saw01(float time, float period)
            {
                // periodが極端に小さいと不安定になるので保護
                period = max(period, 0.001);
                return frac(time / period); // 0..1 をループ
            }

            // ステップを滑らかにする補助関数
            float SmoothStepEdge(float x, float edge, float smooth)
            {
                // smooth=0 なら硬い閾値、>0 なら緩やかに
                float a = edge - smooth * 0.5;
                float b = edge + smooth * 0.5;
                return saturate(smooth == 0 ? step(edge, x) : smoothstep(a, b, x));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texCol = tex2D(_MainTex, i.uv);
                fixed4 baseCol = texCol * _BaseColor;

                // 0..1 でループする時間
                float t01 = Saw01(_Time.y, _PeriodSec);

                // duty（ON割合）に基づき ON/OFF を決定
                // t01 < _Duty で ON（1）、それ以外は OFF（0）
                // _Smoothness を使ってエッジをなめらかにできる
                float onStrength = SmoothStepEdge(_Duty, t01, _Smoothness);
                // 上の関数は step(edge, x) 相当の向きにしているため、比較方向を揃える：
                // step(edge, x) は x >= edge で1。ONを t01 < _Duty にしたいので反転。
                onStrength = 1.0 - onStrength;

                // 色ブレンド
                fixed3 rgb = lerp(baseCol.rgb, _BlinkColor.rgb, onStrength);
                return fixed4(rgb, baseCol.a);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Color"
}