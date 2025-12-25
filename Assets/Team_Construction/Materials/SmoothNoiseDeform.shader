Shader "Custom/SmoothNoiseDeform"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _MainTex("Albedo", 2D) = "white" {}
        _Speed("Animation Speed", Float) = 0.1
        _Power("Animation Power", Float) = 1
        _UVScale("Texture Tiling", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geo
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityStandardUtils.cginc"
            
            // Shader uniforms
            float4 _Color;
            float _Speed;
            float _Power;
            sampler2D _MainTex;
            float _UVScale;

            // vert Shader → Geometry Shader に渡すための構造体
            struct appdata
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            // Geometry Shader → Fragment Shader に渡すための構造体

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 normal : NORMAL;
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };


            //
            // vert shader
            //
            appdata vert(appdata input)
            {
                input.position = mul(unity_ObjectToWorld, input.position);
                input.normal = UnityObjectToWorldNormal(input.normal);
                return input;
            }

            // Geometry Shaderの出力用の構造体を用意するための関数
            v2f vertOutput(float3 wpos, half3 wnrm, float2 uv)
            {
                v2f o;
                o.position = UnityWorldToClipPos(float4(wpos, 1));
                o.normal = wnrm;
                o.worldPos = wpos;
                o.uv = uv;
                return o;
            }

            // 3点から法線ベクトルを求めるための関数
            float3 ConstructNormal(float3 v1, float3 v2, float3 v3)
            {
                return normalize(cross(v2 - v1, v3 - v1));
            }

            // ノイズ用のハッシュ関数
            float hash(float3 p)
            {
                return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453);
            }

            // 3Dパーリンノイズ関数
            float noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x),
                         lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                    lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                         lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y), f.z);
            }

            // SmoothStep
            float smoothstep(float min, float max, float x) 
            {
                float t = saturate((x - min) / (max - min));
                return t * t * (3.0 - 2.0 * t);
            }

            // geometry shader
            [maxvertexcount(15)]
            void geo(triangle appdata input[3], uint pid : SV_PrimitiveID, inout TriangleStream<v2f> outStream)
            {
                // vert inputs
                float3 wp0 = input[0].position.xyz;
                float3 wp1 = input[1].position.xyz;
                float3 wp2 = input[2].position.xyz;

                // Extrusion amount
                float ext = _Power * noise(wp0 + _Speed * _Time.xyz);
                ext *= smoothstep(0, 10, length(wp0) - 10);

                // Extrusion points
                float3 offs = ConstructNormal(wp0, wp1, wp2) * ext;
                float3 wp3 = wp0 + offs;
                float3 wp4 = wp1 + offs;
                float3 wp5 = wp2 + offs;

                // Cap triangle
                float3 wn = ConstructNormal(wp3, wp4, wp5);
                float np = saturate(ext * 10);
                //各頂点での法線を再計算
                float3 wn0 = lerp(input[0].normal, wn, np);
                float3 wn1 = lerp(input[1].normal, wn, np);
                float3 wn2 = lerp(input[2].normal, wn, np);

                outStream.Append(vertOutput(wp3, wn0, input[0].uv));
                outStream.Append(vertOutput(wp4, wn1, input[1].uv));
                outStream.Append(vertOutput(wp5, wn2, input[2].uv));
                outStream.RestartStrip();

                // Side faces
                wn = ConstructNormal(wp3, wp0, wp4);
                outStream.Append(vertOutput(wp3, wn, float2(0,0)));
                outStream.Append(vertOutput(wp0, wn, float2(0,0)));
                outStream.Append(vertOutput(wp4, wn, float2(0,0)));
                outStream.Append(vertOutput(wp1, wn, float2(0,0)));
                outStream.RestartStrip();

                wn = ConstructNormal(wp4, wp1, wp5);
                outStream.Append(vertOutput(wp4, wn, float2(0,0)));
                outStream.Append(vertOutput(wp1, wn, float2(0,0)));
                outStream.Append(vertOutput(wp5, wn, float2(0,0)));
                outStream.Append(vertOutput(wp2, wn, float2(0,0)));
                outStream.RestartStrip();

                wn = ConstructNormal(wp5, wp2, wp3);
                outStream.Append(vertOutput(wp5, wn, float2(0,0)));
                outStream.Append(vertOutput(wp2, wn, float2(0,0)));
                outStream.Append(vertOutput(wp3, wn, float2(0,0)));
                outStream.Append(vertOutput(wp0, wn, float2(0,0)));
                outStream.RestartStrip();
            }
            
            // Fragment shader
            float4 frag(v2f input) : COLOR
            {
                // 白色光
                float3 light = float3(1,1,1);
                
                // 横向きの面は暗くする
                float3 up = float3(0, 1, 0);
                float alignment = abs(dot(normalize(input.normal), up)); // y軸と水平 = 1, 垂直 = 0
                light *= alignment;
              
                // とはいえ多少は照らしてあげる
                light += saturate(input.worldPos.y + 0.3); 
                light = saturate(light);
                

                // テクスチャカラー取得
                float2 uv = (input.uv - float2(0.5, 0.5)) * _UVScale;
                float4 col = tex2D(_MainTex, uv) * _Color;

                // ライティング反映
                col.rgb *= light;

                return col;
            }
            ENDCG
        }
    }
}