Shader "TextMeshPro/Mobile/Bitmap Outline"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _Color ("Text Color", Color) = (1,1,1,1)
        _DiffusePower ("Diffuse Power", Range(1.0,4.0)) = 1.0

        [HDR] _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width (Texels)", Range(0, 4)) = 1

        [Enum(Four Directions, 4, Eight Directions, 8)]
        _OutlineMode ("Outline Mode", Float) = 8

        // TMP가 glyph quad의 여백을 계산할 때 사용함.
        // Outline Width 이상으로 잡는 것을 권장.
        _Padding ("Padding", Float) = 1

        _VertexOffsetX ("Vertex Offset X", Float) = 0
        _VertexOffsetY ("Vertex Offset Y", Float) = 0

        _MaskSoftnessX ("Mask Softness X", Float) = 0
        _MaskSoftnessY ("Mask Softness Y", Float) = 0

        _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Lighting Off
        Cull Off
        ZTest [unity_GUIZTestMode]
        ZWrite Off

        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM

            #pragma target 3.0

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float4 mask : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            fixed4 _Color;
            float _DiffusePower;

            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineMode;

            float _VertexOffsetX;
            float _VertexOffsetY;

            float4 _ClipRect;
            float _MaskSoftnessX;
            float _MaskSoftnessY;

            v2f vert(appdata_t input)
            {
                v2f output;

                float4 vertex = input.vertex;
                vertex.x += _VertexOffsetX;
                vertex.y += _VertexOffsetY;

                vertex.xy += (vertex.w * 0.5) / _ScreenParams.xy;

                output.vertex = UnityPixelSnap(UnityObjectToClipPos(vertex));

                output.color = input.color * _Color;
                output.color.rgb *= _DiffusePower;

                output.texcoord0 = input.texcoord0;

                float2 pixelSize = output.vertex.w;

                float4 clampedRect = clamp(
                    _ClipRect,
                    -2e10,
                    2e10
                );

                output.mask = float4(
                    vertex.xy * 2 - clampedRect.xy - clampedRect.zw,
                    0.25 / (
                        0.25 * half2(_MaskSoftnessX, _MaskSoftnessY)
                        + pixelSize
                    )
                );

                return output;
            }

            fixed SampleAlpha(float2 uv)
            {
                return tex2D(_MainTex, uv).a;
            }

            fixed SampleOutline4(float2 uv, float2 offset)
            {
                fixed alpha = 0;

                // ← →
                alpha = max(alpha, SampleAlpha(uv + float2(-offset.x, 0)));
                alpha = max(alpha, SampleAlpha(uv + float2( offset.x, 0)));

                // ↓ ↑
                alpha = max(alpha, SampleAlpha(uv + float2(0, -offset.y)));
                alpha = max(alpha, SampleAlpha(uv + float2(0,  offset.y)));

                return alpha;
            }

            fixed SampleOutline8(float2 uv, float2 offset)
            {
                fixed alpha = SampleOutline4(uv, offset);

                // ↙ ↘
                alpha = max(alpha, SampleAlpha(
                    uv + float2(-offset.x, -offset.y)
                ));

                alpha = max(alpha, SampleAlpha(
                    uv + float2( offset.x, -offset.y)
                ));

                // ↖ ↗
                alpha = max(alpha, SampleAlpha(
                    uv + float2(-offset.x, offset.y)
                ));

                alpha = max(alpha, SampleAlpha(
                    uv + float2( offset.x, offset.y)
                ));

                return alpha;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = input.texcoord0;

                fixed faceCoverage = SampleAlpha(uv);

                float2 outlineOffset =
                    _MainTex_TexelSize.xy * _OutlineWidth;

                fixed expandedCoverage;

                if (_OutlineMode >= 8)
                    expandedCoverage = SampleOutline8(uv, outlineOffset);
                else
                    expandedCoverage = SampleOutline4(uv, outlineOffset);

                // 원본 glyph 내부에서는 outline이 나오지 않게 함.
                fixed outlineCoverage =
                    saturate(expandedCoverage - faceCoverage);

                fixed faceAlpha =
                    faceCoverage * input.color.a;

                fixed outlineAlpha =
                    outlineCoverage
                    * _OutlineColor.a
                    * input.color.a;

                // Face와 Outline은 사실상 서로 다른 coverage 영역이므로
                // 이 둘을 합성한다.
                fixed finalAlpha =
                    faceAlpha + outlineAlpha;

                fixed3 premultipliedColor =
                    input.color.rgb * faceAlpha
                    + _OutlineColor.rgb * outlineAlpha;

                fixed3 finalColor =
                    premultipliedColor
                    / max(finalAlpha, 0.0001);

                fixed4 color =
                    fixed4(finalColor, finalAlpha);

                #if UNITY_UI_CLIP_RECT
                half2 mask = saturate(
                    (_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy))
                    * input.mask.zw
                );

                color *= mask.x * mask.y;
                #endif

                #if UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }

            ENDCG
        }
    }
}