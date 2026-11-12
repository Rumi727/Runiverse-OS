Shader "Hidden/RuniOS/Waveform"
{
    Properties
    {
        _MainTex ("Min Max", 2D) = "black" {}
        _BoundaryTex ("Boundary", 2D) = "black" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _TargetHeight ("Target Height", Float) = 1
        _Thickness ("Thickness", Float) = 1
        _Antialiasing ("Antialiasing", Float) = 1
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

            // x: 열 경계 하나당 텍셀 하나(열 개수 + 1), y: 채널 하나당 텍셀 하나, 텍셀 값은 경계 위치의 파형 값
            sampler2D _BoundaryTex;
            float4 _BoundaryTex_TexelSize;

            float4 _Color;
            float _TargetHeight;
            float _Thickness;
            float _Antialiasing;

            float SegmentDistance(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                float t = saturate(dot(p - a, ab) / max(dot(ab, ab), 1e-6));
                return length(p - a - (ab * t));
            }

            float4 frag(v2f_img i) : SV_Target
            {
                int width = (int)_MainTex_TexelSize.z;
                int channelCount = (int)_MainTex_TexelSize.w;
                int laneHeight = (int)_TargetHeight / channelCount;
                int y = (int)floor(i.uv.y * _TargetHeight);

                int lane = laneHeight > 0 ? y / laneHeight : channelCount;
                if (lane >= channelCount)
                    return 0;

                int channel = channelCount - lane - 1;
                float v = (channel + 0.5) * _MainTex_TexelSize.y;

                float halfLane = laneHeight / 2.0;
                float center = (lane * laneHeight) + halfLane;
                float2 pixel = float2(i.uv.x * width, y + 0.5);

                // 픽셀에서 굵기와 안티에일리어싱 폭의 절반 안에 들어오는 열만 선에 닿을 수 있습니다.
                float reach = (_Thickness + _Antialiasing) / 2;
                int firstColumn = max((int)floor(pixel.x - reach), 0);
                int lastColumn = min((int)floor(pixel.x + reach), width - 1);

                float nearest = 1e9;

                [loop]
                for (int column = firstColumn; column <= lastColumn; column++)
                {
                    float2 minMax = tex2Dlod(_MainTex, float4((column + 0.5) * _MainTex_TexelSize.x, v, 0, 0)).rg;
                    if (minMax.x > minMax.y)
                        continue;

                    float left = tex2Dlod(_BoundaryTex, float4((column + 0.5) * _BoundaryTex_TexelSize.x, v, 0, 0)).r;
                    float right = tex2Dlod(_BoundaryTex, float4((column + 1.5) * _BoundaryTex_TexelSize.x, v, 0, 0)).r;

                    /*
                     * 열 안의 파형을 왼쪽 경계 → 첫 극값 → 두 번째 극값 → 오른쪽 경계의 꺾은선으로 복원합니다.
                     * 극값의 순서는 세로 이동 거리가 짧은 쪽으로 정해, 가파른 단조 구간이 대각선 하나가 되게 합니다.
                     * 극값의 가로 위치는 알 수 없으므로 누적 세로 이동 거리에 비례해 놓습니다. 그 오차는 꼭짓점이 자기 열 안에서 가로로 밀리는 것뿐입니다.
                     */
                    bool minFirst = abs(left - minMax.x) + abs(right - minMax.y) <= abs(left - minMax.y) + abs(right - minMax.x);
                    float firstValue = minFirst ? minMax.x : minMax.y;
                    float secondValue = minFirst ? minMax.y : minMax.x;

                    float rise0 = abs(firstValue - left);
                    float rise1 = abs(secondValue - firstValue);
                    float rise2 = abs(right - secondValue);
                    float total = rise0 + rise1 + rise2;
                    float firstX = total > 0 ? rise0 / total : 1 / 3.0;
                    float secondX = total > 0 ? (rise0 + rise1) / total : 2 / 3.0;

                    float2 p0 = float2(column, center + (left * halfLane));
                    float2 p1 = float2(column + firstX, center + (firstValue * halfLane));
                    float2 p2 = float2(column + secondX, center + (secondValue * halfLane));
                    float2 p3 = float2(column + 1, center + (right * halfLane));

                    nearest = min(nearest, SegmentDistance(pixel, p0, p1));
                    nearest = min(nearest, SegmentDistance(pixel, p1, p2));
                    nearest = min(nearest, SegmentDistance(pixel, p2, p3));
                }

                // 선 가장자리 밖으로 벗어난 거리(안쪽은 음수)를 안티에일리어싱 폭에 걸쳐 알파로 바꿉니다.
                float outside = nearest - (_Thickness / 2);
                float alpha = _Antialiasing > 0 ? saturate(0.5 - (outside / _Antialiasing)) : outside <= 0;

                return float4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}
