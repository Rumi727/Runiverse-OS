Shader "Hidden/RuniOS/MergeOutline/Composite"
{
    Properties { _MainTex ("Dilated", 2D)=""{} _OriginalMask ("Original", 2D)=""{} _Color ("Color", Color)=(1,1,0,1) }
    SubShader {
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha 
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local __ MODE_ALWAYS MODE_OCCLUDED
            #include "UnityCG.cginc"
            sampler2D _MainTex, _OriginalMask, _CameraDepthTexture;
            float4 _MainTex_TexelSize, _Color;
            v2f_img vert(appdata_img v) { v2f_img o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
            fixed4 frag (v2f_img i) : SV_Target {
                float4 dil = tex2D(_MainTex, i.uv);
                float orig = tex2D(_OriginalMask, i.uv).r;
                if (saturate(dil.r - orig) < 0.01 || dil.g <= 0.0001) discard;

                float objZ = 1.0 / dil.g;
                float2 dUV = i.uv;
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0) dUV.y = 1 - dUV.y;
                #endif
                float scnZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, dUV));

                #if defined(MODE_OCCLUDED)
                    if (objZ <= scnZ + 0.05) discard;
                #elif !defined(MODE_ALWAYS)
                    if (objZ > scnZ - 0.05) discard;
                #endif
                return _Color;
            }
            ENDCG
        }
    }
}