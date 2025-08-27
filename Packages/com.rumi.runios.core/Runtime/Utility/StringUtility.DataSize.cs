#nullable enable
using System;

namespace RuniOS
{
    public static partial class StringUtility
    {
        public static string DataSizeToString(this long byteSize) => ByteTo(byteSize, out string space) + space;

        /// <summary>
        /// 데이터 크기를(바이트) 문자열로 바꿔줍니다 (B, KB, MB, GB, TB, PB, EB, ZB, YB)
        /// </summary>
        /// <param name="byteSize">계산할 용량</param>
        /// <param name="digits">계산된 용량에서 표시할 소수점 자리수</param>
        /// <returns>계산된 용량</returns>
        public static string DataSizeToString(this long byteSize, int digits) => ByteTo(byteSize, out string space).Round(digits) + space;

        /// <summary>
        /// 데이터 크기를(바이트) 적절하게 바꿔줍니다 (B, KB, MB, GB, TB, PB, EB, ZB, YB)
        /// </summary>
        /// <param name="byteSize">계산할 용량</param>
        /// <param name="space"></param>
        public static double ByteTo(long byteSize, out string space)
        {
            int loopCount = 0;
            double size = byteSize;

            while (size > 1000.0)
            {
                size /= 1000.0;
                loopCount++;
            }

            space = loopCount switch
            {
                0 => "B",
                1 => "KB",
                2 => "MB",
                3 => "GB",
                4 => "TB",
                5 => "PB",
                6 => "EB",
                7 => "ZB",
                _ => "YB"
            };

            return size;
        }

        public static double ByteTo(long byteSize, DataSizeType dataSizeType, out string space)
        {
            double size = byteSize / Math.Pow(1024, (int)dataSizeType);
            space = dataSizeType.ToString().ToUpper();

            return size;
        }

        public enum DataSizeType
        {
            b,
            kb,
            mb,
            gb,
            tb,
            pb,
            eb,
            zb,
            yb
        }
    }
}
