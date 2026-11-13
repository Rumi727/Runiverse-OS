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
        _SamplesPerPixel ("Samples Per Pixel", Float) = 1
        _SubColumns ("Sub Columns", Float) = 1
        _Mode ("Mode", Float) = 0
        _ZoomedOutBlendEnd ("Zoomed Out Blend End", Float) = 2
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

            // 텍셀 값은 하위 열 하나의 (min, max)입니다. [(채널 * 열 개수 + 열) * 하위 열 개수 + 하위 열]의 선형 순서로 폭이 열 개수인 텍스처에 줄을 바꿔 가며 놓입니다.
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            // x: 열 경계 하나당 텍셀 하나(열 개수 + 1), y: 채널 하나당 텍셀 하나, 텍셀 값은 경계 위치의 파형 값
            sampler2D _BoundaryTex;
            float4 _BoundaryTex_TexelSize;

            float4 _Color;
            float _TargetHeight;
            float _Thickness;
            float _Antialiasing;

            // 열 하나가 덮는 원본 샘플 수입니다. 화면 전체에서 같으므로 두 그리기 방식을 섞는 비율을 정하는 데 씁니다.
            float _SamplesPerPixel;

            // 픽셀 하나를 나눈 하위 열의 개수입니다. 픽셀당 샘플이 2개 미만인 확대 상태에서는 1입니다.
            float _SubColumns;

            // WaveformRenderMode입니다. 0: 밀도, 1: 피크
            float _Mode;

            // 축소 상태 방식으로만 그리기 시작하는 픽셀당 샘플 수입니다. 픽셀당 샘플 1개부터 여기까지 선과 섞습니다.
            float _ZoomedOutBlendEnd;

            float2 FetchMinMax(int channel, int column, int subColumn, int width)
            {
                int index = (((channel * width) + column) * (int)_SubColumns) + subColumn;
                int row = index / width;
                return tex2Dlod(_MainTex, float4(((index - (row * width)) + 0.5) * _MainTex_TexelSize.x, (row + 0.5) * _MainTex_TexelSize.y, 0, 0)).rg;
            }

            // 하위 열들은 열 구간을 빈틈없이 나누므로, 그 min/max를 합치면 열 전체의 min/max가 됩니다.
            float2 FetchColumnMinMax(int channel, int column, int width)
            {
                float2 minMax = FetchMinMax(channel, column, 0, width);
                int subColumns = (int)_SubColumns;

                [loop]
                for (int subColumn = 1; subColumn < subColumns; subColumn++)
                {
                    float2 sub = FetchMinMax(channel, column, subColumn, width);
                    minMax = float2(min(minMax.x, sub.x), max(minMax.y, sub.y));
                }

                return minMax;
            }

            float SegmentDistance(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                float t = saturate(dot(p - a, ab) / max(dot(ab, ab), 1e-6));
                return length(p - a - (ab * t));
            }

            // 선 가장자리 밖으로 벗어난 거리(안쪽은 음수)를 안티에일리어싱 폭에 걸쳐 알파로 바꿉니다.
            float Coverage(float outside)
            {
                return _Antialiasing > 0 ? saturate(0.5 - (outside / _Antialiasing)) : outside <= 0;
            }

            // 열 안의 파형을 꺾은선으로 복원해 굵기를 가진 선으로 그립니다. 샘플 간격이 픽셀보다 넓은 확대 상태를 위한 방식입니다.
            float LineAlpha(float2 pixel, int width, int channel, float v, float center, float halfLane)
            {
                // 픽셀에서 굵기와 안티에일리어싱 폭의 절반 안에 들어오는 열만 선에 닿을 수 있습니다.
                float reach = (_Thickness + _Antialiasing) / 2;
                int firstColumn = max((int)floor(pixel.x - reach), 0);
                int lastColumn = min((int)floor(pixel.x + reach), width - 1);

                float nearest = 1e9;

                // 픽셀이 속한 열을 칠할 때, 그 열의 세로 범위 안쪽으로 들어온 거리(바깥은 음수)입니다.
                int pixelColumn = (int)floor(pixel.x);
                float fillDepth = -1e9;

                [loop]
                for (int column = firstColumn; column <= lastColumn; column++)
                {
                    float2 minMax = FetchColumnMinMax(channel, column, width);
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

                    /*
                     * 극값이 경계 값 범위 밖에 있으면 열 안에 가로 위치를 모르는 극값이 있다는 뜻입니다.
                     * 꺾은선은 지어낸 가로 위치를 지나가므로, 안티에일리어싱하면 면 안에 그 위치가 무늬로 드러납니다.
                     * 그런 열은 세로 범위를 열 폭(한 픽셀) 전체에 칠하고, 위아래 끝만 세로 거리로 안티에일리어싱합니다.
                     */
                    if (column == pixelColumn && (minMax.x < min(left, right) || minMax.y > max(left, right)))
                    {
                        float low = center + (minMax.x * halfLane);
                        float high = center + (minMax.y * halfLane);
                        fillDepth = min(pixel.y - low, high - pixel.y);
                    }
                }

                return Coverage(min(nearest, -fillDepth) - (_Thickness / 2));
            }

            // 하위 열 하나의 세로 범위가 픽셀을 덮는 정도입니다. 거의 평평한 범위도 보이도록 높이가 굵기보다 작으면 가운데를 기준으로 굵기만큼 벌립니다.
            float SpanCoverage(float2 minMax, float pixelY, float center, float halfLane)
            {
                if (minMax.x > minMax.y)
                    return 0;

                float low = center + (minMax.x * halfLane);
                float high = center + (minMax.y * halfLane);
                float middle = (low + high) / 2;
                float halfHeight = max((high - low) / 2, _Thickness / 2);

                return Coverage(abs(pixelY - middle) - halfHeight);
            }

            /*
             * 열 하나에 샘플이 많아 파형이 선이 아닌 면이 되는 축소 상태를 위한 방식입니다.
             * 픽셀을 나눈 하위 열들이 픽셀을 덮는 정도를 평균 내므로, 순간 피크 하나만 있는 높이는 흐리게, 소리가 몰린 높이는 진하게 보입니다.
             * 하위 열이 픽셀 안의 가로 변화를 그대로 담으므로 열 사이를 보간하지 않아도 가로 경계가 부드럽게 나옵니다.
             */
            float DensityAlpha(float2 pixel, int width, int channel, float center, float halfLane)
            {
                int column = (int)floor(pixel.x);
                int subColumns = (int)_SubColumns;

                float sum = 0;

                [loop]
                for (int subColumn = 0; subColumn < subColumns; subColumn++)
                    sum += SpanCoverage(FetchMinMax(channel, column, subColumn, width), pixel.y, center, halfLane);

                return sum / subColumns;
            }

            // 열 중앙에 둔 (최소, 최대) 꼭짓점의 픽셀 y와, 샘플이 있는 열인지(z)를 돌려줍니다.
            float3 EnvelopeAt(int channel, int column, int width, float center, float halfLane)
            {
                float2 minMax = FetchColumnMinMax(channel, column, width);
                if (minMax.x > minMax.y)
                    return 0;

                float low = center + (minMax.x * halfLane);
                float high = center + (minMax.y * halfLane);

                // 거의 평평한 열도 보이도록 높이가 굵기보다 작으면 가운데를 기준으로 굵기만큼 벌립니다.
                float middle = (low + high) / 2;
                float halfHeight = max((high - low) / 2, _Thickness / 2);
                return float3(middle - halfHeight, middle + halfHeight, 1);
            }

            /*
             * 열마다 중앙에 (최소, 최대) 꼭짓점을 두고 이웃 열의 꼭짓점끼리 이은 위아래 외곽선 사이를 채웁니다.
             * 한 열짜리 피크가 옆 열로 번지지 않고 꼭짓점에서 뾰족하게 끝납니다.
             * 픽셀 중심은 자기 열의 중앙에 있으므로 안쪽인지는 자기 열의 꼭짓점으로 판정하고, 가장자리는 가까운 외곽선 선분까지의 거리로 안티에일리어싱합니다.
             */
            float PeakAlpha(float2 pixel, int width, int channel, float center, float halfLane)
            {
                int pixelColumn = (int)floor(pixel.x);
                float3 own = EnvelopeAt(channel, pixelColumn, width, center, halfLane);
                if (own.z == 0)
                    return 0;

                float lowDistance = abs(pixel.y - own.x);
                float highDistance = abs(pixel.y - own.y);

                // 이웃 꼭짓점은 한 픽셀 간격이므로 안티에일리어싱 폭의 절반 안에 드는 선분만 봅니다.
                int reach = max((int)ceil(_Antialiasing / 2), 1);
                int firstColumn = max(pixelColumn - reach, 0);
                int lastColumn = min(pixelColumn + reach, width - 1);

                float3 previous = EnvelopeAt(channel, firstColumn, width, center, halfLane);

                [loop]
                for (int column = firstColumn + 1; column <= lastColumn; column++)
                {
                    float3 current = EnvelopeAt(channel, column, width, center, halfLane);
                    if (previous.z > 0 && current.z > 0)
                    {
                        lowDistance = min(lowDistance, SegmentDistance(pixel, float2(column - 0.5, previous.x), float2(column + 0.5, current.x)));
                        highDistance = min(highDistance, SegmentDistance(pixel, float2(column - 0.5, previous.y), float2(column + 0.5, current.y)));
                    }

                    previous = current;
                }

                float outside = pixel.y > own.y ? highDistance : pixel.y < own.x ? lowDistance : -min(lowDistance, highDistance);
                return Coverage(outside);
            }

            float4 frag(v2f_img i) : SV_Target
            {
                // 경계 값 텍스처는 하위 열과 상관없이 (열 개수 + 1) × 채널 수입니다.
                int width = (int)_BoundaryTex_TexelSize.z - 1;
                int channelCount = (int)_BoundaryTex_TexelSize.w;
                int laneHeight = (int)_TargetHeight / channelCount;
                int y = (int)floor(i.uv.y * _TargetHeight);

                int lane = laneHeight > 0 ? y / laneHeight : channelCount;
                if (lane >= channelCount)
                    return 0;

                int channel = channelCount - lane - 1;
                float v = (channel + 0.5) * _BoundaryTex_TexelSize.y;

                float halfLane = laneHeight / 2.0;
                float center = (lane * laneHeight) + halfLane;
                float2 pixel = float2(i.uv.x * width, y + 0.5);

                /*
                 * 픽셀당 샘플 1개 이하는 선으로, _ZoomedOutBlendEnd개 이상은 축소 상태 방식(밀도 또는 피크)으로 그리고, 그 사이는 줌할 때 모양이 튀지 않도록 섞습니다.
                 * 픽셀당 샘플이 1개면 그 픽셀의 소리는 그 샘플뿐이라 밀도가 100%이므로, 밀도 방식은 이 지점에서 선과 자연스럽게 이어집니다.
                 */
                // 끝 지점이 1 이하면 0으로 나누지 않도록 폭을 아주 작게 두어 1에서 바로 바뀌게 합니다.
                float zoomedOutWeight = saturate((_SamplesPerPixel - 1) / max(_ZoomedOutBlendEnd - 1, 1e-6));

                float alpha = 0;
                if (zoomedOutWeight < 1)
                    alpha = LineAlpha(pixel, width, channel, v, center, halfLane);

                if (zoomedOutWeight > 0)
                {
                    float zoomedOut = _Mode > 0.5 ? PeakAlpha(pixel, width, channel, center, halfLane) : DensityAlpha(pixel, width, channel, center, halfLane);
                    alpha = lerp(alpha, zoomedOut, zoomedOutWeight);
                }

                return float4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}
