//===========================================================
//  Author  :しょった
//  Summary :頂点カラーを用いてメッシュを表示するシェーダ
//===========================================================

Shader "Custom/Color"
{
	CGINCLUDE
	#include "UnityCG.cginc"

	struct appdata
	{
		float4 vertex : POSITION;
		float4 color  : COLOR;
	};

	struct v2f
	{
		float4 vertex : SV_POSITION;
		float4 color  : COLOR;
	};

	v2f vert (appdata v)
	{
		v2f o;
		o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
		o.color = v.color;
		return o;
	}
	
	fixed4 frag (v2f i) : COLOR
	{
		return i.color;
	}
	ENDCG
	SubShader
	{
		Cull Off Lighting Off ZWrite On
		Tags { "RenderType" = "Transparent" "Queue"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG

		}
	}
}
