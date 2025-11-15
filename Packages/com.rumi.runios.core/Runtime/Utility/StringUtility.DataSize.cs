#nullable enable
namespace RuniOS.Utility
{
    public static partial class StringUtility
    {
        static readonly string[] _iecSpaces = { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB", "RiB", "QiB" };
        static readonly string[] _siSpaces = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "RB", "QB" };

        /// <summary>
        /// 바이트 크기를 사람이 읽기 쉬운 문자열 형식으로 변환합니다.
        /// <br/>계산 기준은 1000(SI) 또는 1024(IEC)를 선택할 수 있습니다.
        /// <exception cref="ArgumentOutOfRangeException">소수점 자리수가 음수일 경우 발생합니다.</exception>
        /// </summary>
        /// <param name="byteSize">변환할 바이트 크기입니다.</param>
        /// <param name="digits">반환되는 문자열에 포함할 소수점 자리수입니다.</param>
        /// <param name="isBase1000">
        /// <see langword="true"/>일 경우 1000을 기준으로 계산합니다 (예: KB, MB).
        /// <br/><see langword="false"/>일 경우 1024를 기준으로 계산합니다 (예: KiB, MiB).
        /// </param>
        /// <returns>변환된 크기를 나타내는 읽기 쉬운 문자열입니다 (예: "1.23 GiB").</returns>
        public static string DataSizeToString(this long byteSize, int digits = 2, bool isBase1000 = false)
        {
            if (digits < 0)
                throw new ArgumentOutOfRangeException(nameof(digits), "소수점 자리수는 음수가 될 수 없습니다.");

            double size = GetFormattedSize(byteSize, out string space, isBase1000);
            return $"{Math.Round(size, digits, MidpointRounding.AwayFromZero)} {space}";
        }

        /// <summary>
        /// 바이트 크기를 적절한 단위로 자동 변환하고, 그 단위 문자열을 반환합니다.
        /// <br/>계산 기준은 1000(SI) 또는 1024(IEC)를 선택할 수 있습니다.
        /// </summary>
        /// <param name="byteSize">변환할 바이트 크기입니다.</param>
        /// <param name="space">변환된 단위 문자열(예: "KiB", "MB")입니다.</param>
        /// <param name="isBase1000">
        /// <see langword="true"/>일 경우 1000을 기준으로 계산합니다.
        /// <br/><see langword="false"/>일 경우 1024를 기준으로 계산합니다.
        /// </param>
        /// <returns>변환된 크기를 나타내는 <see cref="double"/> 값입니다.</returns>
        public static double GetFormattedSize(this long byteSize, out string space, bool isBase1000 = false)
        {
            if (byteSize == 0)
            {
                space = "B";
                return 0;
            }

            double unit = isBase1000 ? 1000.0 : 1024.0;
            string[] spaces = isBase1000 ? _siSpaces : _iecSpaces;

            int loopCount = (int)Math.Floor(Math.Log(byteSize, unit));
            loopCount = Math.Min(loopCount, spaces.Length - 1);

            space = spaces[loopCount];
            return byteSize / Math.Pow(unit, loopCount);
        }

        /// <summary>
        /// 바이트 크기를 지정된 <see cref="DataSizeType"/> 단위로 변환합니다.
        /// </summary>
        /// <param name="byteSize">변환할 바이트 크기입니다.</param>
        /// <param name="dataSizeType">변환하고자 하는 단위입니다.</param>
        /// <param name="space">변환된 단위 문자열입니다.</param>
        /// <returns>지정된 단위로 변환된 크기입니다.</returns>
        public static double GetSizeInUnit(this long byteSize, DataSizeType dataSizeType, out string space)
        {
            int loopCount = (int)dataSizeType;
            space = dataSizeType.ToString().ToUpper();

            return byteSize / Math.Pow(1000, loopCount);
        }

        /// <summary>
        /// 데이터 크기 단위를 정의합니다.
        /// </summary>
        public enum DataSizeType
        {
            /// <summary>
            /// 바이트 (Byte)
            /// </summary>
            b,
            /// <summary>
            /// 킬로바이트 (Kilobyte)
            /// </summary>
            kb,
            /// <summary>
            /// 메가바이트 (Megabyte)
            /// </summary>
            mb,
            /// <summary>
            /// 기가바이트 (Gigabyte)
            /// </summary>
            gb,
            /// <summary>
            /// 테라바이트 (Terabyte)
            /// </summary>
            tb,
            /// <summary>
            /// 페타바이트 (Petabyte)
            /// </summary>
            pb,
            /// <summary>
            /// 엑사바이트 (Exabyte)
            /// </summary>
            eb,
            /// <summary>
            /// 제타바이트 (Zettabyte)
            /// </summary>
            zb,
            /// <summary>
            /// 요타바이트 (Yottabyte)
            /// </summary>
            yb
        }
    }
}