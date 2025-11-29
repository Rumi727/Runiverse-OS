Shader "Hidden/RuniOS/MergeOutline/Mask"
{
    Properties { _ObjectTex ("Tex", 2D) = "white" {} _Cutoff ("Cut", Range(0,1)) = 0.5 }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 p:SV_POSITION; float2 u:TEXCOORD0; float d:TEXCOORD1; };
            sampler2D _ObjectTex; float4 _ObjectTex_ST; float _Cutoff;
            v2f vert (appdata_base v) {
                v2f o; o.p = UnityObjectToClipPos(v.vertex);
                o.u = TRANSFORM_TEX(v.texcoord, _ObjectTex);
                COMPUTE_EYEDEPTH(o.d); return o;
            }
            float4 frag (v2f i) : SV_Target {
                clip(tex2D(_ObjectTex, i.u).a - _Cutoff);
                return float4(1, 1.0 / max(i.d, 0.00001), 0, 1);
            }
            ENDCG
        }
    }
}