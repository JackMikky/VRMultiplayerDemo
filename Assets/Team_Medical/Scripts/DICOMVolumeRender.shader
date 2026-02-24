Shader "Custom/NewUnlitUniversalRenderPipelineShader"
{
    Properties
    {
        [Header(Rendering)]
        _Volume("Volume", 3D) = "" {}
        _Transfer("Transfer", 2D) = "" {}
        _Iteration("Iteration", Int) = 10
        _Intensity("Intensity", Range(0.0, 1.0)) = 0.1
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc ("Blend Src", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDst ("Blend Dst", Float) = 10

        [Header(Ranges)]
        _MinX("MinX", Range(0, 1)) = 0.0
        _MaxX("MaxX", Range(0, 1)) = 1.0
        _MinY("MinY", Range(0, 1)) = 0.0
        _MaxY("MaxY", Range(0, 1)) = 1.0
        _MinZ("MinZ", Range(0, 1)) = 0.0
        _MaxZ("MaxZ", Range(0, 1)) = 1.0

        [Header(Optimization)]
    _IterationMax("Iteration Max", Int) = 16    // サンプル上限（必要に応じて調整）
    _AlphaStop("Early Exit Alpha", Range(0.9, 0.999)) = 0.97 // 早期終了閾値
    }

    SubShader
    {
        Tags 
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent" 
            "IgnoreProjector" = "True"
            // "RenderPipeline" = "UniversalPipeline" 
        }
     Pass
    {
        Cull Back
        ZWrite Off
        ZTest LEqual
        Blend One OneMinusSrcAlpha
        Lighting Off

        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // 頂点入力
        struct appdata
        {
            float4 vertex : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        // 頂点→フラグメント（ステレオも含む）
        struct v2f
        {
            float4 vertex   : SV_POSITION;
            float4 localPos : TEXCOORD0;
            float4 worldPos : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        // ユニフォーム
        sampler3D _Volume;
        sampler2D _Transfer;

        int   _Iteration;
        int   _IterationMax;
        float _Intensity;
        float _AlphaStop;

        float _MinX, _MaxX, _MinY, _MaxY, _MinZ, _MaxZ;

        // レイ構造体
        struct Ray
        {
            float3 from;
            float3 dir;
            float  tmax;
        };

        // 頂点シェーダ
        v2f vert(appdata v)
        {
            v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            o.vertex   = TransformObjectToHClip(v.vertex);
            o.localPos = v.vertex;
            o.worldPos = mul(unity_ObjectToWorld, v.vertex);
            return o;
        }

        // ボックスとの交差（ローカル座標で -0.5..+0.5 の立方体）
        void intersection(inout Ray ray)
        {
            float3 invDir = 1.0 / ray.dir;
            float3 t1     = (-0.5 - ray.from) * invDir;
            float3 t2     = (+0.5 - ray.from) * invDir;
            float3 tmax3  = max(t1, t2);
            float2 tmax2  = min(tmax3.xx, tmax3.yz);
            ray.tmax      = min(tmax2.x, tmax2.y);
        }

        // ボリュームサンプル（範囲内のみ有効）
        inline float sampleVolume(float3 pos)
        {
            float x = step(pos.x, _MaxX) * step(_MinX, pos.x);
            float y = step(pos.y, _MaxY) * step(_MinY, pos.y);
            float z = step(pos.z, _MaxZ) * step(_MinZ, pos.z);
            return tex3D(_Volume, pos).r * (x * y * z);
        }

        inline float4 transferFunction(float t)
        {
            // 1D相当（V軸0固定）
            return tex2D(_Transfer, float2(t, 0));
        }

        // フラグメント
        float4 frag(v2f i) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

            // 視点と方向
            float3 cameraPos = GetCameraPositionWS();
            float3 worldDir  = i.worldPos.xyz - cameraPos;
            float3 localDir  = normalize(mul(unity_WorldToObject, worldDir));

            // レイ初期化
            Ray ray;
            ray.from = i.localPos.xyz;
            ray.dir  = localDir;
            intersection(ray);
            if (ray.tmax <= 0) return 0;

            // 距離に応じてサンプル数を抑制（近距離で削減）
            int iterBase = _Iteration;
            float tmax   = ray.tmax;

            if (tmax < 0.25)      iterBase = max(4,  _Iteration / 4);
            else if (tmax < 0.5)  iterBase = max(8,  _Iteration / 2);

            // 上限・下限
            int n = clamp(iterBase, 1, max(1, _IterationMax));

            float3 localStep = localDir * (tmax / n);
            float3 localPos  = ray.from;

            float4 accum = 0;

            [loop]
            for (int k = 0; k < n; ++k)
            {
                float volume = sampleVolume(localPos + 0.5);
                float4 color = transferFunction(volume);

                // Premultiplied alpha：色にアルファを前積
                color.a  *= (volume * _Intensity);
                color.rgb *= color.a;

                // 透過合成（accum = accum + (1-a)*color）
                accum += (1.0 - accum.a) * color;

                // 早期終了
                if (accum.a >= _AlphaStop) break;

                localPos += localStep;
            }

            return accum;
        }

        ENDHLSL
    }
        // Pass
        // {
        //     Cull Back
        //     ZWrite Off
        //     ZTest LEqual
        //     Blend [_BlendSrc] [_BlendDst]
        //     Lighting Off

        //     HLSLPROGRAM

        //     #pragma vertex vert
        //     #pragma fragment frag

        //     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        //     struct appdata
        //     {
        //         float4 vertex : POSITION;
        //         UNITY_VERTEX_INPUT_INSTANCE_ID
        //     };

        //     struct v2f
        //     {
        //         float4 vertex : SV_POSITION;
        //         float4 localPos : TEXCOORD0;
        //         float4 worldPos : TEXCOORD1;
        //         UNITY_VERTEX_OUTPUT_STEREO
        //     };

        //     sampler3D _Volume;
        //     sampler2D _Transfer;
        //     int _Iteration;
        //     float _Intensity;
        //     float _MinX, _MaxX, _MinY, _MaxY, _MinZ, _MaxZ;

        //     struct Ray
        //     {
        //         float3 from;
        //         float3 dir;
        //         float tmax;
        //     };

        //     void intersection(inout Ray ray)
        //     {
        //         float3 invDir = 1.0 / ray.dir;
        //         float3 t1 = (-0.5 - ray.from) * invDir;
        //         float3 t2 = (+0.5 - ray.from) * invDir;
        //         float3 tmax3 = max(t1, t2);
        //         float2 tmax2 = min(tmax3.xx, tmax3.yz);
        //         ray.tmax = min(tmax2.x, tmax2.y);
        //     }

        //     inline float sampleVolume(float3 pos)
        //     {
        //         float x = step(pos.x, _MaxX) * step(_MinX, pos.x);
        //         float y = step(pos.y, _MaxY) * step(_MinY, pos.y);
        //         float z = step(pos.z, _MaxZ) * step(_MinZ, pos.z);
        //         return tex3D(_Volume, pos).r * (x * y * z);
        //     }

        //     inline float4 transferFunction(float t)
        //     {
        //         return tex2D(_Transfer, float2(t, 0));
        //     }

        //     v2f vert(appdata v)
        //     {
        //         v2f o;
        //         UNITY_SETUP_INSTANCE_ID(v);
        //         UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

        //         o.vertex = TransformObjectToHClip(v.vertex);
        //         o.localPos = v.vertex;
        //         o.worldPos = mul(unity_ObjectToWorld, v.vertex);
        //         return o;
        //     }

        //     float4 frag(v2f i) : SV_Target
        //     {
        //         UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
        //         float3 cameraPos = GetCameraPositionWS();

        //         float3 worldDir = i.worldPos - cameraPos;
        //         float3 localDir = normalize(mul(unity_WorldToObject, worldDir));

        //         Ray ray;
        //         ray.from = i.localPos;
        //         ray.dir = localDir;
        //         intersection(ray);

        //         int n = _Iteration * ray.tmax / sqrt(3);
        //         float3 localStep = localDir * ray.tmax / n;
        //         float3 localPos = i.localPos;
        //         float4 output = 0;

        //         [loop]
        //         for(int i = 0; i < n; ++i)
        //         {
        //             float volume = sampleVolume(localPos + 0.5);
        //             float4 color = transferFunction(volume) * volume * _Intensity;
        //             output += (1.0 - output.a) * color;
        //             localPos += localStep;
        //         }

        //         return output;
        //     }

        //     ENDHLSL
        // }
    }
}
