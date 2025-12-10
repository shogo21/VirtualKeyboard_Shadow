Shader "Hidden/CropAndRotate"
{
	Properties {}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"


			sampler2D _MainTex;
			float4 _UVRect; // x, y, width, height in UV (0..1), left-bottom origin
			float _Angle; // radians, positive = CCW


			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};


			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};


			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}


			float2 rotate_uv(float2 uv, float2 center, float angle)
			{
				float s = sin(angle);
				float c = cos(angle);


				uv -= center;
				float x = uv.x * c - uv.y * s;
				float y = uv.x * s + uv.y * c;
				return float2(x, y) + center;
			}


			fixed4 frag(v2f i) : SV_Target
			{
				// map full-screen quad uv (0..1) into the requested UV rect
				float2 uv = _UVRect.xy + i.uv * _UVRect.zw;


				// compute center in UV space and rotate around it
				float2 center = _UVRect.xy + _UVRect.zw * 0.5;
				uv = rotate_uv(uv, center, _Angle);


				// sample texture with possible out-of-range uv
				// clamp to edge so we don't sample garbage outside.
				float2 uvClamped = clamp(uv, 0.0, 1.0);
				return tex2D(_MainTex, uvClamped);
			}
			ENDCG
		}
	}
}