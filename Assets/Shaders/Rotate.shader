Shader "Hidden/Rotate"
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
            float _Angle;   // radians, CCW

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
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
                float2 r;
                r.x = uv.x * c - uv.y * s;
                r.y = uv.x * s + uv.y * c;
                return r + center;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);

                float2 uv = rotate_uv(i.uv, center, _Angle);

                // 画像外アクセス防止
                uv = clamp(uv, 0.0, 1.0);

                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}
