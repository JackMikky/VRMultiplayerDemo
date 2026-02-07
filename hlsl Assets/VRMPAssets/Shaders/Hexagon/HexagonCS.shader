Shader "Unlit/HexagonCS"
{
	Properties
	{
		_Segment("Shape Segment",RANGE(3,24)) = 6
		[Toggle]_Invert("Invert Hex",FLOAT) = 0
		_Thickness("Line Thickness",RANGE(0,5))=0.1
		[Toggle]_UseLineColor("UseLineColor",FLOAT) = 0
		[HDR]_LineColor("Line Color",Color)=(0,2,0.232858658,1)
		_T("Fade Value",RANGE(0,1)) = 0
	}

	HLSLINCLUDE
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	#include "Assets/VRMPAssets/Shaders/ShaderFunctionScripts/Hex.hlsl"

	struct appdata
	{
		float4 positionOS : POSITION;
		float2 uv : TEXCOORD0;
		uint vertexID : SV_VertexID;
	};

	struct v2f
	{
		float4 positionCS : SV_POSITION;
		float2 uv : TEXCOORD0;
	};

	TEXTURE2D(_BlitTexture);
	SAMPLER(sampler_LinearClamp);

	int _Segment;
	float _Invert;
	float _Thickness;
	float4 _LineColor;
	float _UseLineColor;
	float _T;

	v2f vert(appdata v)
	{
		v2f o;
		
		float2 uv = float2((v.vertexID << 1) & 2, v.vertexID & 2);
		o.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
		
		#if UNITY_UV_STARTS_AT_TOP
		uv.y = 1.0 - uv.y;
		#endif
		
		o.uv = uv;
		return o;
	}

	half4 frag(v2f i) : SV_Target
	{
		float4 lineColor = float4(0,0,0,1);
		float2 Hex;
		float2 HexUV;
		float HexPos;
		float2 HexIndex;
		
		float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, i.uv);
		
		float2 uv = i.uv;
		uv.x *= _ScreenParams.x / _ScreenParams.y;
		
		Hexagon_noise_float(uv, _Segment, HexPos, HexIndex, HexUV, Hex);
		
		HexPos = smoothstep(-0.01, _Thickness, HexPos);
		
		if (_Invert) {
			HexPos = 1 - HexPos;
		}
		
		if (_UseLineColor && !_Invert) {
			lineColor = _LineColor * (1 - HexPos);
		}
		
		col *= HexPos;
		col += lineColor;
		
		col = lerp(col, float4(0, 0, 0, 1), _T);
		
		return col;
	}
	ENDHLSL

	SubShader
	{
		Tags 
		{ 
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
		}
		
		LOD 100
		Cull Off
		ZWrite Off
		ZTest Always
		
		Pass
		{
			Name "HexagonFullScreen"
			
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5
			ENDHLSL
		}
	}
	
	FallBack Off
}