Shader "Custom/URP_Liquid_FillHeight"
{
    Properties
    {
        _ColorShallow ("Shallow Color", Color) = (1,0.627,0.157,1)
        _ColorDeep    ("Deep Color", Color)    = (0.784,0.392,0.063,1)
        _Alpha        ("Alpha", Range(0,1))    = 0.65
        _BandWidth    ("Surface Band Width", Range(0,0.1)) = 0.02
        _SurfaceBoost ("Surface Highlight Boost", Range(0,2)) = 0.5
        _FillHeight   ("Fill Height (World Y)", Float) = 0.0
        _GravityUp    ("Gravity Up (World)", Vector) = (0,1,0,0)
        _NormalMap    ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0,1)) = 0.15
    }

    SubShader
    {
        Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off  // ← 両面描画に変更（水面上面も表示されるようになる）

        Pass
        {
            Name "Forward"
            Tags{ "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 tangentWS   : TEXCOORD2;
                float2 uv          : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorShallow;
                float4 _ColorDeep;
                float _Alpha;
                float _BandWidth;
                float _SurfaceBoost;
                float _FillHeight;
                float4 _GravityUp; // xyz used
                float _NormalStrength;
            CBUFFER_END

            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.positionWS = positionWS;
                OUT.normalWS = normalWS;

                float3 t = normalize(TransformObjectToWorldDir(IN.tangentOS.xyz));
                float3 n = normalize(normalWS);
                float3 b = normalize(cross(n, t) * IN.tangentOS.w);
                OUT.tangentWS = float4(t, IN.tangentOS.w);
                OUT.uv = IN.uv;
                return OUT;
            }

            float3 UnpackNormalScaleRG(in float4 packed, in float strength)
            {
                float3 n = UnpackNormal(packed);
                n.xy *= strength;
                n = normalize(n);
                return n;
            }

            half4 frag(Varyings IN, float facing : VFACE) : SV_Target
            {
                float3 upWS = normalize(_GravityUp.xyz);
                float3 planePointWS = float3(0.0, _FillHeight, 0.0);

                // signed distance: >0 = above plane, <0 = below plane
                float signedDist = dot(IN.positionWS - planePointWS, upWS);

                // 液面より上のフラグメントを破棄（側面用）
                // ただし水面ポリゴン（facing > 0 かつ signedDist ≈ 0）は残す
                clip(-signedDist + _BandWidth);

                float depthAlongUp = -signedDist;
                float depthFactor = saturate(depthAlongUp * 4.0);
                float3 col = lerp(_ColorShallow.rgb, _ColorDeep.rgb, depthFactor);

                float3 nWS = normalize(IN.normalWS);
                // Cull Off のとき裏面は法線を反転
                nWS *= (facing > 0.0 ? 1.0 : -1.0);

                float3 t = normalize(IN.tangentWS.xyz);
                float3 b = normalize(cross(nWS, t) * IN.tangentWS.w);
                float3x3 TBN = float3x3(t, b, nWS);
                float3 nTS = UnpackNormalScaleRG(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv), _NormalStrength);
                float3 n = normalize(mul(TBN, nTS));

                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float fres = pow(1.0 - saturate(dot(normalize(n), upWS)), 3.0);

                float band = 1.0 - smoothstep(0.0, _BandWidth, abs(signedDist));
                float3 surfaceTint = _ColorShallow.rgb * _SurfaceBoost * band;
                col = saturate(col + surfaceTint + fres * 0.08);

                return half4(col, _Alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}