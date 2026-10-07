Shader "Hidden/OutlineTest"
{
    Properties
    {
        _MainTex ("Source", 2D) = "black" {}
        _MaskTex ("Mask", 2D) = "black" {}
        _OutlineTex ("Outline", 2D) = "black" {}

        _OutlineColor ("Outline Color", Color) = (1, 0.4, 0, 1)

        _OutlineWidth ("Outline Width", Int) = 2
        _OutlineSoftness ("Outline Softness", Float) = 0.75
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        // ================================================================
        // Pass 0
        // Renderer -> binary mask
        // ================================================================

        Pass
        {
            Name "Mask"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                return 1;
            }

            ENDHLSL
        }

        // ================================================================
        // Pass 1
        // Horizontal dilation
        // ================================================================

        Pass
        {
            Name "Dilate Horizontal"

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            int _OutlineWidth;

            float4 Frag(v2f_img input) : SV_Target
            {
                float value = 0;

                [unroll]
                for (int i = -8; i <= 8; i++)
                {
                    if (abs(i) > _OutlineWidth)
                        continue;

                    float2 uv =
                        input.uv +
                        float2(
                            _MainTex_TexelSize.x * i,
                            0
                        );

                    value = max(
                        value,
                        tex2D(_MainTex, uv).r
                    );
                }

                return value.xxxx;
            }

            ENDHLSL
        }

        // ================================================================
        // Pass 2
        // Vertical dilation
        // ================================================================

        Pass
        {
            Name "Dilate Vertical"

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            int _OutlineWidth;

            float4 Frag(v2f_img input) : SV_Target
            {
                float value = 0;

                [unroll]
                for (int i = -8; i <= 8; i++)
                {
                    if (abs(i) > _OutlineWidth)
                        continue;

                    float2 uv =
                        input.uv +
                        float2(
                            0,
                            _MainTex_TexelSize.y * i
                        );

                    value = max(
                        value,
                        tex2D(_MainTex, uv).r
                    );

                }

                return value.xxxx;
            }

            ENDHLSL
        }

        // ================================================================
        // Pass 3
        // Horizontal Gaussian blur
        //
        // Width와 무관.
        // Softness만 sample spacing을 조절한다.
        // ================================================================

        Pass
        {
            Name "Blur Horizontal"

            HLSLPROGRAM

            #pragma vertex vert_img
            #pragma fragment Frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float _OutlineSoftness;

            float4 Frag(v2f_img input) : SV_Target
            {
                float2 step =
                    float2(
                        _MainTex_TexelSize.x * _OutlineSoftness,
                        0
                    );

                float value = 0;

                value += tex2D(_MainTex, input.uv - step * 4).r * 0.01621622;
                value += tex2D(_MainTex, input.uv - step * 3).r * 0.05405405;
                value += tex2D(_MainTex, input.uv - step * 2).r * 0.12162162;
                value += tex2D(_MainTex, input.uv - step    ).r * 0.19459459;

                value += tex2D(_MainTex, input.uv).r * 0.22702703;

                value += tex2D(_MainTex, input.uv + step    ).r * 0.19459459;
                value += tex2D(_MainTex, input.uv + step * 2).r * 0.12162162;
                value += tex2D(_MainTex, input.uv + step * 3).r * 0.05405405;
                value += tex2D(_MainTex, input.uv + step * 4).r * 0.01621622;

                return value.xxxx;
            }

            ENDHLSL
        }

        // ================================================================
        // Pass 4
        // Vertical Gaussian blur
        // ================================================================

        Pass
        {
            Name "Blur Vertical"

            HLSLPROGRAM

            #pragma vertex vert_img
            #pragma fragment Frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float _OutlineSoftness;

            float4 Frag(v2f_img input) : SV_Target
            {
                float2 step =
                    float2(
                        0,
                        _MainTex_TexelSize.y * _OutlineSoftness
                    );

                float value = 0;

                value += tex2D(_MainTex, input.uv - step * 4).r * 0.01621622;
                value += tex2D(_MainTex, input.uv - step * 3).r * 0.05405405;
                value += tex2D(_MainTex, input.uv - step * 2).r * 0.12162162;
                value += tex2D(_MainTex, input.uv - step    ).r * 0.19459459;

                value += tex2D(_MainTex, input.uv).r * 0.22702703;

                value += tex2D(_MainTex, input.uv + step    ).r * 0.19459459;
                value += tex2D(_MainTex, input.uv + step * 2).r * 0.12162162;
                value += tex2D(_MainTex, input.uv + step * 3).r * 0.05405405;
                value += tex2D(_MainTex, input.uv + step * 4).r * 0.01621622;

                return value.xxxx;
            }

            ENDHLSL
        }

        // ================================================================
        // Pass 5
        // Composite
        // ================================================================

        Pass
        {
            Name "Composite"

            HLSLPROGRAM

            #pragma vertex vert_img
            #pragma fragment Frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _MaskTex;
            sampler2D _OutlineTex;

            float4 _OutlineColor;

            float4 Frag(v2f_img input) : SV_Target
            {
                float4 scene =
                    tex2D(_MainTex, input.uv);

                float mask =
                    tex2D(_MaskTex, input.uv).r;

                float outline =
                    tex2D(_OutlineTex, input.uv).r;

                // 물체 내부 제거
                outline *= 1.0 - mask;

                float alpha =
                    saturate(outline * _OutlineColor.a);

                scene.rgb = lerp(
                    scene.rgb,
                    _OutlineColor.rgb,
                    alpha
                );

                return scene;
            }

            ENDHLSL
        }
    }

    Fallback Off
}