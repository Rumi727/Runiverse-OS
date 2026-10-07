Shader "Custom/SimpleMeshOutline"
{
    Properties
    {
        _Color ("Outline Color", Color) = (1,1,0,1)
        _Width ("Width", Float) = 0.05
        _Offset ("Gap", Float) = 0.05
        
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Int) = 4
        
        [Toggle] _WidthInPixels ("Width (Pixels)", Float) = 0
        [Toggle] _GapInPixels ("Gap (Pixels)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        CGINCLUDE
        #include "UnityCG.cginc"

        fixed4 _Color;
        float _Width, _Offset;
        float _WidthInPixels, _GapInPixels;
        int _ZTest; // C#에서 전달받는 ZTest 값

        struct appdata {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float3 smoothNormal : TEXCOORD1;
        };
        struct v2f { float4 pos : SV_POSITION; };

        float4 ExpandVertex(appdata v, float offsetVal, float widthVal)
        {
            float3 norm = length(v.smoothNormal) > 0.1 ? v.smoothNormal : v.normal;
            
            float NdotD = dot(v.normal, norm);
            float correction = 1.0 / max(NdotD, 0.1);
            correction = min(correction, 5.0);

            float localDistance = offsetVal * (1.0 - _GapInPixels)
                + widthVal * (1.0 - _WidthInPixels);
            float screenDistance = offsetVal * _GapInPixels
                + widthVal * _WidthInPixels;

            float3 pos = v.vertex.xyz + norm * (localDistance * correction);
            float4 clipPos = UnityObjectToClipPos(float4(pos, 1.0));

            if (abs(screenDistance) > 0.0001 && abs(clipPos.w) > 0.000001)
            {
                float3 normalVS = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, norm));
                float2 projectedNormal = TransformViewToProjection(normalVS.xy);
                float2 pixelDirection = projectedNormal * _ScreenParams.xy;
                float directionLengthSq = dot(pixelDirection, pixelDirection);

                if (directionLengthSq > 0.0000000001)
                {
                    pixelDirection *= rsqrt(directionLengthSq);
                    clipPos.xy += pixelDirection * screenDistance
                        * (2.0 / _ScreenParams.xy) * clipPos.w;
                }
            }

            return clipPos;
        }
        ENDCG

        // --- Pass 0: Gap Mask ---
        Pass
        {
            Name "GapMask"
            Cull Front ZWrite Off ColorMask 0
            ZTest [_ZTest] // 전달받은 모드 적용
            Stencil { Ref 23 Comp Always Pass Replace }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            v2f vert(appdata v) { v2f o; o.pos = ExpandVertex(v, _Offset, 0); return o; }
            fixed4 frag(v2f i) : SV_Target { return 0; }
            ENDCG
        }

        // --- Pass 1: Outline ---
        Pass
        {
            Name "Outline"
            Cull Front ZWrite Off
            ZTest [_ZTest] // 전달받은 모드 적용
            Blend SrcAlpha OneMinusSrcAlpha
            Stencil { Ref 23 Comp NotEqual }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            v2f vert(appdata v) { v2f o; o.pos = ExpandVertex(v, _Offset, _Width); return o; }
            fixed4 frag(v2f i) : SV_Target { return _Color; }
            ENDCG
        }

        // --- Pass 2: Cleanup ---
        Pass
        {
            Name "Cleanup"
            Cull Front ZWrite Off ColorMask 0
            ZTest Always // Cleanup은 항상 수행해야 안전함
            Stencil { Ref 0 Comp Always Pass Replace }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            v2f vert(appdata v) { v2f o; o.pos = ExpandVertex(v, _Offset, 0); return o; }
            fixed4 frag(v2f i) : SV_Target { return 0; }
            ENDCG
        }
    }
}
