Shader "Hidden/RuniOS/MergeOutline/JFA_Init"
{
    Properties { _MainTex ("Mask", 2D) = "black" {} }
    SubShader {
        Cull Off ZWrite Off ZTest Always
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            v2f_img vert(appdata_img v) { v2f_img o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
            sampler2D _MainTex;
            float4 frag (v2f_img i) : SV_Target {
                float4 m = tex2D(_MainTex, i.uv);
                return m.r > 0.5 ? float4(i.uv.x, i.uv.y, m.g, 1) : float4(-1, -1, 0, 0);
            }
            ENDCG
        }
    }
}