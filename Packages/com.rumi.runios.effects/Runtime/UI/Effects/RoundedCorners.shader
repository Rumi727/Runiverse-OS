Shader "Hidden/RuniOS/RoundedCorners"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

            struct appdata_ui
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0; 
                float2 uv1      : TEXCOORD1; // Local Pos
                float2 uv2      : TEXCOORD2; // Rect Size
                float2 uv3      : TEXCOORD3; // x:Width, y:Softness
                float4 tangent  : TANGENT;   // Radii
                float3 normal   : NORMAL;    // x:IsOutline, y:BodySoft
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 uiPos    : TEXCOORD1;
                float4 rectInfo : TEXCOORD4; 
                float4 outlineInfo : TEXCOORD5; 
                float4 radii    : TEXCOORD6; 
                float4 worldPosition : TEXCOORD7;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            
            v2f vert(appdata_ui v)
            {
                v2f IN;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(IN);
                
                IN.worldPosition = v.vertex;
                IN.vertex = UnityObjectToClipPos(v.vertex);
                IN.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                IN.color = v.color;
                
                IN.uiPos = v.uv1;
                IN.rectInfo = float4(v.uv2.x, v.uv2.y, 0, 0);
                // Unpack: x=Width, y=OutSoft, z=IsOutline, w=BodySoft
                IN.outlineInfo = float4(v.uv3.x, v.uv3.y, v.normal.x, v.normal.y);
                IN.radii = v.tangent;

                return IN;
            }

            float CalcRoundedBox(float2 p, float2 size, float4 radius)
            {
                float2 r = (p.x > 0.0) ? radius.yw : radius.xz; 
                float rad = (p.y > 0.0) ? r.x : r.y;
                float2 q = abs(p) - size + rad;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - rad;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 pixelPos = IN.uiPos;
                float2 halfSize = IN.rectInfo.xy;
                float dist = CalcRoundedBox(pixelPos, halfSize, IN.radii);
                float delta = fwidth(pixelPos.x) * 0.5; 
                
                float outlineWidth = IN.outlineInfo.x;
                float outlineSoftness = IN.outlineInfo.y;
                float isOutline = IN.outlineInfo.z;
                float bodySoftness = IN.outlineInfo.w;

                half4 finalColor;

                // Branching (Coherent)
                if (isOutline < 0.5) 
                {
                    // Body
                    half4 texColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                    
                    float softRange = delta * outlineSoftness; // Body uses its own softness
                    float alpha = 1.0 - smoothstep(-softRange, softRange, dist);
                    
                    texColor.a *= alpha;
                    finalColor = texColor;
                }
                else
                {
                    // Outline
                    float innerSoftRange = delta * bodySoftness;
                    float overlap = innerSoftRange * 0.5; // Gap fix
                    
                    float innerHole = smoothstep(-innerSoftRange - overlap, innerSoftRange - overlap, dist); 
                    float outerSoftRange = delta * outlineSoftness; 
                    float outerBorder = 1.0 - smoothstep(outlineWidth - outerSoftRange, outlineWidth + outerSoftRange, dist);

                    finalColor = IN.color;
                    finalColor.a *= (innerHole * outerBorder);
                }

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                clip(finalColor.a - 0.001);

                return finalColor;
            }
            ENDCG
        }
    }
}