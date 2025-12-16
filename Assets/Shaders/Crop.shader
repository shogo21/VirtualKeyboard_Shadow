Shader "Hidden/Crop"
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

            // x, y = left-bottom UV
            // z, w = width, height (UV space)
            float4 _UVRect;

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

            fixed4 frag (v2f i) : SV_Target
            {
                // フルスクリーン quad (0..1) を
                // 指定された UV 矩形にマッピング
                float2 uv = _UVRect.xy + i.uv * _UVRect.zw;

                // 範囲外を防止
                uv = clamp(uv, 0.0, 1.0);

                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}
