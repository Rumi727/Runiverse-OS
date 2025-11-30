Shader "Hidden/RuniOS/MergeOutline/Blur"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        
        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        float _Width;

        struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
        v2f vert(appdata_img v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o; }

        // [수정] 가중치 합계 조정 (Sum ~ 1.0)
        // Center(0.3) + Side1(0.25*2) + Side2(0.1*2) = 1.0
        static const float weights[3] = { 0.3, 0.25, 0.1 };

        float4 frag_blur(v2f i, float2 dir)
        {
            float4 color = tex2D(_MainTex, i.uv) * weights[0];
            
            for (int j = 1; j < 3; j++)
            {
                float2 offset = dir * _MainTex_TexelSize.xy * (j * _Width);
                float4 c1 = tex2D(_MainTex, i.uv + offset);
                float4 c2 = tex2D(_MainTex, i.uv - offset);
                
                // R(Mask): 가중치 합산
                color.r += (c1.r + c2.r) * weights[j];
                
                // G(Depth): Max 전파 (가까운 깊이 유지)
                color.g = max(color.g, max(c1.g, c2.g));
            }
            return float4(color.r, color.g, 0, 1);
        }
        ENDCG

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(v2f i):SV_Target { return frag_blur(i, float2(1,0)); }
            ENDCG
        }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(v2f i):SV_Target { return frag_blur(i, float2(0,1)); }
            ENDCG
        }
    }
}