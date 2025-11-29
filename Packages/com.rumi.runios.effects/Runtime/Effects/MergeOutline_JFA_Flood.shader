Shader "Hidden/RuniOS/MergeOutline/JFA_Flood"
{
    Properties { _MainTex ("In", 2D) = "black" {} }
    SubShader {
        Cull Off ZWrite Off ZTest Always
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            v2f_img vert(appdata_img v) { v2f_img o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
            sampler2D _MainTex; float4 _MainTex_TexelSize; float _StepSize;
            float4 frag (v2f_img i) : SV_Target {
                float bestD = 9999.0;
                float4 bestVal = float4(-1,-1,0,0);
                for (int y=-1; y<=1; y++) {
                    for (int x=-1; x<=1; x++) {
                        float2 uv = i.uv + float2(x, y) * _MainTex_TexelSize.xy * _StepSize;
                        float4 d = tex2Dlod(_MainTex, float4(uv, 0, 0));
                        if (d.x != -1) {
                            float2 diff = (i.uv - d.xy);
                            diff.x *= _ScreenParams.x / _ScreenParams.y;
                            float dist = dot(diff, diff);
                            if (dist < bestD) { bestD = dist; bestVal = d; }
                        }
                    }
                }
                return bestVal;
            }
            ENDCG
        }
    }
}