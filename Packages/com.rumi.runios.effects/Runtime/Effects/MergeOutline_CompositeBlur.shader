Shader "Hidden/RuniOS/MergeOutline/CompositeBlur"
{
    Properties { 
        _MainTex("Blurred",2D)=""{} _OriginalMask("Mask",2D)=""{} _Color("Col",Color)=(1,1,0,1) 
    }
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
            float _Hardness;

            struct v2f { float4 p:SV_POSITION; float2 u:TEXCOORD0; };
            v2f vert(appdata_img v) { v2f o; o.p=UnityObjectToClipPos(v.vertex); o.u=v.texcoord; return o; }

            fixed4 frag(v2f i):SV_Target {
                float4 blurred = tex2D(_MainTex, i.u);
                float original = tex2D(_OriginalMask, i.u).r;

                // [수정 핵심] 곱셈 마스킹 (Multiplicative Masking)
                // original이 1이면 (1-1)=0이 되어 무조건 제거됨
                // original이 0이면 (1-0)=1이 되어 블러 유지
                float alpha = blurred.r * (1.0 - original);
                
                // [선명도 조절]
                alpha = pow(alpha, 0.5) * _Hardness;
                alpha = saturate(alpha);

                if(alpha < 0.01) discard;

                // [Depth Compare]
                float objZInv = blurred.g;
                if(objZInv > 0.0001) {
                    float objZ = 1.0 / objZInv;
                    float2 uv = i.u;
                    #if UNITY_UV_STARTS_AT_TOP
                    if(_MainTex_TexelSize.y<0) uv.y=1-uv.y;
                    #endif
                    float scnZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));

                    #if defined(MODE_OCCLUDED)
                        if(objZ <= scnZ + 0.1) discard;
                    #elif !defined(MODE_ALWAYS)
                        if(objZ > scnZ - 0.1) discard;
                    #endif
                }

                return float4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}