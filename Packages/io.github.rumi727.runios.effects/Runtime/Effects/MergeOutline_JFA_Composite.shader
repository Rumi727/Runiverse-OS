Shader "Hidden/RuniOS/MergeOutline/JFA_Composite"
{
    Properties { _MainTex ("JFA", 2D)=""{} _OriginalMask ("Orig", 2D)=""{} _Color ("Col", Color)=(1,1,0,1) }
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
            float _Width, _Softness;
            v2f_img vert(appdata_img v) { v2f_img o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord; return o; }
            fixed4 frag (v2f_img i) : SV_Target {
                if (tex2D(_OriginalMask, i.uv).r > 0.5) discard;
                float4 jfa = tex2D(_MainTex, i.uv);
                if (jfa.x == -1) discard;

                float2 diff = (i.uv - jfa.xy);
                diff.x *= _ScreenParams.x / _ScreenParams.y;
                float dist = length(diff) * _ScreenParams.y;

                if (dist <= _Width) {
                    float objZInv = jfa.z;
                    if (objZInv <= 0.0001) discard;
                    
                    float objZ = 1.0 / objZInv;
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

                    float edge0 = _Width * (1.0 - _Softness);
                    float alpha = 1.0 - smoothstep(edge0, _Width, dist);
                    return float4(_Color.rgb, _Color.a * alpha);
                }
                discard; return 0;
            }
            ENDCG
        }
    }
}