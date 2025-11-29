Shader "Hidden/RuniOS/MergeOutline/Min"
{
    Properties { _MainTex ("A", 2D)=""{} _SubTex ("B", 2D)=""{} }
    SubShader {
        Cull Off ZWrite Off ZTest Always
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _SubTex;
            v2f_img vert(appdata_img v) { v2f_img o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
            float4 frag(v2f_img i):SV_Target {
                float4 a = tex2D(_MainTex, i.uv);
                float4 b = tex2D(_SubTex, i.uv);
                return float4(min(a.r, b.r), max(a.g, b.g), 0, 1);
            }
            ENDCG
        }
    }
}