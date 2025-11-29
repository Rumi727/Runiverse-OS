Shader "Hidden/RuniOS/MergeOutline/Dilate"
{
    Properties { _MainTex ("Tex", 2D) = "black" {} }
    SubShader {
        Cull Off ZWrite Off ZTest Always
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            v2f_img vert(appdata_img v) { v2f_img o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
            sampler2D _MainTex; float4 _MainTex_TexelSize; float _Width; float2 _Direction;
            float4 frag (v2f_img i) : SV_Target {
                float4 maxV = tex2D(_MainTex, i.uv);
                int steps = ceil(_Width);
                for(int j=1; j<=steps; j++) {
                    float2 off = _Direction * _MainTex_TexelSize.xy * j;
                    float4 v1 = tex2D(_MainTex, i.uv + off);
                    float4 v2 = tex2D(_MainTex, i.uv - off);
                    maxV = max(maxV, max(v1, v2));
                }
                return maxV;
            }
            ENDCG
        }
    }
}