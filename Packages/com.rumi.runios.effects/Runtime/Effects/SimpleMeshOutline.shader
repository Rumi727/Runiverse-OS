Shader "Custom/SimpleMeshOutline"
{
    Properties
    {
        _Color ("Outline Color", Color) = (1,1,0,1)
        _Width ("Width", Float) = 0.05
        _Offset ("Gap", Float) = 0.05
        
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Int) = 4
        
        [Toggle] _WidthUseScreen ("Width Screen Space", Float) = 0 
        [Toggle] _OffsetUseScreen ("Offset Screen Space", Float) = 0 
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        CGINCLUDE
        #include "UnityCG.cginc"

        fixed4 _Color;
        float _Width, _Offset;
        float _WidthUseScreen, _OffsetUseScreen;
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
            
            float3 viewPos = UnityObjectToViewPos(v.vertex);
            float dist = length(viewPos);
            float distScale = dist * 0.02;

            float finalOffset = offsetVal * (_OffsetUseScreen > 0.5 ? distScale : 1.0);
            float finalWidth = widthVal * (_WidthUseScreen > 0.5 ? distScale : 1.0);

            float totalDist = (finalOffset + finalWidth) * correction;
            
            float3 pos = v.vertex.xyz + norm * totalDist;
            return UnityObjectToClipPos(pos);
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