Shader "Hidden/RuniOS/Waveform"
{
    Properties
    {
        _MainTex ("Min Max", 2D) = "black" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _TargetHeight ("Target Height", Float) = 1
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            // x: 열 하나당 텍셀 하나, y: 채널 하나당 텍셀 하나, 텍셀 값은 (min, max)
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float _TargetHeight;

            float4 frag(v2f_img i) : SV_Target
            {
                int channelCount = (int)_MainTex_TexelSize.w;
                int laneHeight = (int)_TargetHeight / channelCount;
                int y = (int)floor(i.uv.y * _TargetHeight);

                int lane = laneHeight > 0 ? y / laneHeight : channelCount;
                if (lane >= channelCount)
                    return 0;

                int channel = channelCount - lane - 1;
                float2 minMax = tex2Dlod(_MainTex, float4(i.uv.x, (channel + 0.5) * _MainTex_TexelSize.y, 0, 0)).rg;
                if (minMax.x > minMax.y)
                    return 0;

                int laneStart = lane * laneHeight;
                int laneEnd = laneStart + laneHeight - 1;
                int center = laneStart + (laneHeight / 2);

                int yMin = clamp(center + (int)(minMax.x * (laneHeight / 2.0)), laneStart, laneEnd);
                int yMax = clamp(center + (int)(minMax.y * (laneHeight / 2.0)), laneStart, laneEnd);

                return y >= yMin && y <= yMax ? _Color : 0;
            }
            ENDCG
        }
    }
}
