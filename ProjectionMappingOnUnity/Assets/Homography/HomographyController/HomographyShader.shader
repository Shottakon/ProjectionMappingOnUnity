//===========================================================
//  Author  :しょった
//  Summary :ホモグラフィ変換のシェーダ
//===========================================================

Shader "Custom/HomographyConverter" {
	Properties {
		_MainTex        ("Main Tex",	2D		) = "white" {}
	}
	
	CGINCLUDE
	#include "UnityCG.cginc"
	struct v2f {
		float4 position : POSITION;
		float2 texcoord : TEXCOORD0;
	};
	
	float4x4 _HomographyMatrix;
	uniform sampler2D _MainTex;
	
	v2f vert (appdata_img v) {
		v2f o;
		o.position = mul (UNITY_MATRIX_MVP, v.vertex);
		o.texcoord = v.texcoord.xy;
		return o;
	}
	
	uniform half4 mBackColor;
	
	half4 frag (v2f i) : COLOR {
		
		float4  uvCoord  =  mul(_HomographyMatrix ,float4(i.texcoord.xy,1.0,1.0));
		
		float3  uv  =  float3(uvCoord.x/uvCoord.w,uvCoord.y/uvCoord.w,1.0);
		
		half4 texColor;

		texColor = tex2D(_MainTex, uv);
		return  texColor;
	}
	ENDCG
	
	SubShader {
		//Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		//Blend SrcAlpha OneMinusSrcAlpha
		//AlphaTest Greater .01
		Cull Off Lighting Off ZWrite On
		Pass {
			CGPROGRAM
			#pragma vertex   vert
			#pragma fragment frag
			ENDCG
		}
	}
	FallBack off
	
}
