Shader "UI/RoundedCorners"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        
        _Width("Width", Float) = 100
        _Height("Height", Float) = 100
        _Radius("Radius", Vector) = (0,0,0,0)
        _Softness("Softness", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            
            float _Width;
            float _Height;
            float4 _Radius; 
            float _Softness;

            v2f vert(appdata_t v)
            {
                v2f IN;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(IN);
                IN.worldPosition = v.vertex;
                IN.vertex = UnityObjectToClipPos(IN.worldPosition);
                IN.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                IN.color = v.color * _Color;
                return IN;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                float2 pixelPos = (IN.texcoord - 0.5) * float2(_Width, _Height);
                float2 halfSize = float2(_Width, _Height) * 0.5;

                float2 r = (pixelPos.x > 0.0) ? _Radius.yw : _Radius.xz; 
                float radius = (pixelPos.y > 0.0) ? r.x : r.y;

                float2 q = abs(pixelPos) - halfSize + radius;
                float dist = min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius;

                // [수정됨] Softness 기준점 변경 로직
                // fwidth(dist)는 1픽셀 당 거리 변화량
                // _Softness가 1.0일 때 => 실제 범위가 -0.5 ~ 0.5 픽셀이 되도록 * 0.5 적용
                // 결과: Softness 1.0 = 1픽셀 AA (가장 깔끔함)
                float delta = fwidth(dist); 
                float range = delta * _Softness * 0.5;
                
                float smoothedAlpha = 1.0 - smoothstep(-range, range, dist);

                color.a *= smoothedAlpha;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                clip(color.a - 0.01);

                return color;
            }
            ENDCG
        }
    }
}