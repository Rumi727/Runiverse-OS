#nullable enable
using RuniOS.Localizations;
using RuniOS.Resource;
using System.Globalization;
using System.Text;

namespace RuniOS.Utility
{
    /// <summary>
    /// 시간 및 날짜 관련 유틸리티 메서드를 제공합니다.
    /// <br/>이 클래스는 <see cref="TimeSpan"/>과 <see cref="DateTime"/>을 포함한 다양한 데이터 타입을 다룹니다.
    /// </summary>
    public static class TimeUtility
    {
        public const double dayPerYear = ((365 * 3) + 366) / 4d;
        public const double dayPerMonth = dayPerYear / 12d;
        public const int dayPerWeek = 7;

        public const double secondPerYear = dayPerYear * secondPerDay;
        public const double secondPerMonth = dayPerMonth * secondPerDay;
        public const int secondPerDay = 24 * secondPerHour;
        public const int secondPerHour = 60 * secondPerMinute;
        public const int secondPerMinute = 60;

        public const long ticksPerYear = (long)(TimeSpan.TicksPerDay * dayPerYear);
        public const long ticksPerMonth = (long)(TimeSpan.TicksPerDay * dayPerMonth);
        public const long ticksPerWeek = TimeSpan.TicksPerDay * dayPerWeek;

        /// <summary>
        /// 주어진 <see cref="TimeSpan"/>에서 전체 년도를 반환합니다.
        /// </summary>
        /// <param name="timeSpan">계산할 시간 간격입니다.</param>
        /// <returns>정수 형태의 전체 년도 수입니다.</returns>
        public static int GetYears(this TimeSpan timeSpan) => (int)(timeSpan.Ticks / ticksPerYear);

        /// <summary>
        /// 주어진 <see cref="TimeSpan"/>에서 전체 년도를 소수점 형태로 반환합니다.
        /// <br/>이 값은 평균 365.25일 기준으로 계산됩니다.
        /// </summary>
        /// <param name="timeSpan">계산할 시간 간격입니다.</param>
        /// <returns>소수점 형태의 전체 년도 수입니다.</returns>
        public static double GetTotalYears(this TimeSpan timeSpan) => timeSpan.Ticks / (double)ticksPerYear;

        /// <summary>
        /// 주어진 <see cref="TimeSpan"/>에서 전체 월을 반환합니다.
        /// </summary>
        /// <param name="timeSpan">계산할 시간 간격입니다.</param>
        /// <returns>정수 형태의 전체 월 수입니다.</returns>
        public static int GetMonths(this TimeSpan timeSpan) => (int)(timeSpan.Ticks / ticksPerMonth);

        /// <summary>
        /// 주어진 <see cref="TimeSpan"/>에서 전체 월을 소수점 형태로 반환합니다.
        /// <br/>이 값은 평균 30.4375일 기준으로 계산됩니다.
        /// </summary>
        /// <param name="timeSpan">계산할 시간 간격입니다.</param>
        /// <returns>소수점 형태의 전체 월 수입니다.</returns>
        public static double GetTotalMonths(this TimeSpan timeSpan) => timeSpan.Ticks / (double)ticksPerMonth;

        /// <summary>
        /// 주어진 <see cref="TimeSpan"/>에서 전체 주를 반환합니다.
        /// </summary>
        /// <param name="timeSpan">계산할 시간 간격입니다.</param>
        /// <returns>정수 형태의 전체 주 수입니다.</returns>
        public static int GetWeeks(this TimeSpan timeSpan) => (int)(timeSpan.Ticks / ticksPerWeek);

        /// <summary>
        /// 주어진 <see cref="TimeSpan"/>에서 전체 주를 소수점 형태로 반환합니다.
        /// </summary>
        /// <param name="timeSpan">계산할 시간 간격입니다.</param>
        /// <returns>소수점 형태의 전체 주 수입니다.</returns>
        public static double GetTotalWeeks(this TimeSpan timeSpan) => timeSpan.Ticks / (double)ticksPerWeek;



        #region To Time
        /// <summary>
        /// 초(second) 값을 "분:초" 형식의 시간 문자열로 변환합니다.
        /// <br/>예시: 70초는 "1:10"으로 변환됩니다.
        /// <br/>가장 큰 시간 단위(일, 시, 분)가 처음으로 표시되는 유효한 값일 경우 두 자릿수로 채워지지 않습니다.
        /// </summary>
        /// <param name="second">변환할 초(second) 값입니다.</param>
        /// <param name="alwayShowTimeUnit">표시할 최소 시간 단위입니다.</param>
        /// <param name="decimalPlaces">초(second)에 대한 소수점 자리수입니다.</param>
        /// <returns>포맷된 시간 문자열입니다.</returns>
        /// <exception cref="ArgumentOutOfRangeException">소수점 자리수가 음수일 경우 발생합니다.</exception>
        public static string ToTimeString(float second, AlwayShowTimeUnit alwayShowTimeUnit = AlwayShowTimeUnit.minute, int decimalPlaces = 2) => ToTimeString((double)second, alwayShowTimeUnit, decimalPlaces);

        /// <summary>
        /// 초(second) 값을 "분:초" 형식의 시간 문자열로 변환합니다.
        /// <br/>예시: 70.1초는 "1:10.1"로 변환됩니다.
        /// <br/>가장 큰 시간 단위(일, 시, 분)가 처음으로 표시되는 유효한 값일 경우 두 자릿수로 채워지지 않습니다.
        /// </summary>
        /// <param name="second">변환할 초(second) 값입니다.</param>
        /// <param name="alwayShowTimeUnit">표시할 최소 시간 단위입니다.</param>
        /// <param name="decimalPlaces">초(second)에 대한 소수점 자리수입니다.</param>
        /// <returns>포맷된 시간 문자열입니다.</returns>
        /// <exception cref="ArgumentOutOfRangeException">소수점 자리수가 음수일 경우 발생합니다.</exception>
        public static string ToTimeString(double second, AlwayShowTimeUnit alwayShowTimeUnit = AlwayShowTimeUnit.minute, int decimalPlaces = 2)
        {
            if (decimalPlaces < 0)
                throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "The number of decimal places cannot be negative.");

            if (!double.IsFinite(second))
                return "--:--";

            double secondAbs = Math.Abs(second);
            long totalSeconds = (long)Math.Floor(secondAbs);

            StringBuilder sb = StringBuilderCache.Acquire();
            if (second < 0)
                sb.Append('-');

            long totalMinutes = totalSeconds / secondPerMinute;
            long totalHours = totalMinutes / secondPerHour;
            long totalDays = totalHours / secondPerDay;

            bool hasAppendedUnit = false;

            // 일(Day)
            if (alwayShowTimeUnit >= AlwayShowTimeUnit.day || totalDays > 0)
            {
                sb.Append(totalDays).Append(':');
                hasAppendedUnit = true;
            }

            // 시(Hour)
            if (alwayShowTimeUnit >= AlwayShowTimeUnit.hour || totalHours % 24 > 0 || hasAppendedUnit)
            {
                string format = hasAppendedUnit ? "00" : "0";
                sb.AppendFormat(CultureInfo.InvariantCulture, $"{{0:{format}}}:", totalHours % 24);

                hasAppendedUnit = true;
            }

            // 분(Minute)
            if (alwayShowTimeUnit >= AlwayShowTimeUnit.minute || totalMinutes % 60 > 0 || hasAppendedUnit)
            {
                string format = hasAppendedUnit ? "00" : "0";
                sb.AppendFormat(CultureInfo.InvariantCulture, $"{{0:{format}}}:", totalMinutes % 60);

                hasAppendedUnit = true;
            }

            // 초(Second)
            string secondsFormat = hasAppendedUnit ? "00" : "0";
            if (decimalPlaces > 0)
                secondsFormat += "." + new string('0', decimalPlaces);

            sb.AppendFormat(CultureInfo.InvariantCulture, $"{{0:{secondsFormat}}}", secondAbs % 60);
            return StringBuilderCache.Release(sb);
        }
        #endregion



        #region Relative Time
        /// <summary>
        /// 지정된 <see cref="TimeSpan"/>을 상대적인 시간 문자열(예: "5 days ago", "2 months later")에 해당하는
        /// <see cref="Localization"/> 객체로 변환합니다.
        /// </summary>
        /// <param name="timeSpan">변환할 <see cref="TimeSpan"/>입니다.</param>
        /// <param name="digits">표시할 소수점 이하 자릿수입니다.</param>
        /// <returns>상대적인 시간 문자열을 포함하는 <see cref="Localization"/> 객체입니다.</returns>
        public static Localization ToRelativeString(this TimeSpan timeSpan, int digits = 2)
        {
            // 1. 시간 방향 결정 및 절대값 계산
            bool isNegative = timeSpan < TimeSpan.Zero;
            if (isNegative)
                timeSpan = -timeSpan;

            string isAgoOrLater = isNegative ? "ago" : "later";

            // 2. 시간 단위와 총합을 한 번에 계산
            (string unitPath, double totalValue) = timeSpan.Ticks switch
            {
                >= ticksPerYear => ("years", timeSpan.GetTotalYears()),
                >= ticksPerMonth => ("months", timeSpan.GetTotalMonths()),
                >= ticksPerWeek => ("weeks", timeSpan.GetTotalWeeks()),
                >= TimeSpan.TicksPerDay => ("days", timeSpan.TotalDays),
                >= TimeSpan.TicksPerHour => ("hours", timeSpan.TotalHours),
                >= TimeSpan.TicksPerMinute => ("minutes", timeSpan.TotalMinutes),
                >= TimeSpan.TicksPerSecond => ("seconds", timeSpan.TotalSeconds),
                >= TimeSpan.TicksPerMillisecond => ("milliseconds", timeSpan.TotalMilliseconds),
                _ => (string.Empty, 0.0) // 0 또는 아주 작은 값
            };
            
            if (string.IsNullOrEmpty(unitPath))
                return "runios:gui.now";

            // 4. Localization Identifier 구성
            Identifier identifier = "runios:gui." + isAgoOrLater + "." + unitPath;

            // 5. ReplacePair 구성
            string formattedValue = totalValue.Floor(digits).ToString("F" + digits, CultureInfo.InvariantCulture);
            PlaceholderReplacePair replace = new PlaceholderReplacePair("{value}", formattedValue);

            return new Localization(identifier, null, replace);
        }
        #endregion

        #region Lunisolar Calendar
        static readonly KoreanLunisolarCalendar _klc = new KoreanLunisolarCalendar();

        /// <summary>
        /// 그레고리력(<see cref="DateTime"/>)을 한국 음력으로 변환합니다.
        /// </summary>
        /// <param name="dateTime">변환할 그레고리력 날짜입니다.</param>
        /// <param name="isLeapMonth">결과가 윤달인지 여부입니다.</param>
        /// <returns>음력 날짜 및 시간 정보를 담은 <see cref="UnlimitedDateTime"/>입니다.</returns>
        /// <exception cref="Exception">음력 날짜 변환 중 오류가 발생할 수 있습니다.</exception>
        public static UnlimitedDateTime ToLunarDate(this DateTime dateTime, out bool isLeapMonth)
        {
            int year = _klc.GetYear(dateTime);
            int month = _klc.GetMonth(dateTime);
            int day = _klc.GetDayOfMonth(dateTime);
            isLeapMonth = _klc.IsLeapMonth(year, month);

            return new UnlimitedDateTime(year, month, day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);
        }

        /// <summary>
        /// 한국 음력 날짜를 그레고리력(<see cref="DateTime"/>)으로 변환합니다.
        /// </summary>
        /// <param name="dateTime">변환할 음력 날짜입니다.</param>
        /// <returns>음력 날짜에 해당하는 그레고리력 <see cref="DateTime"/>입니다.</returns>
        /// <exception cref="Exception">양력 날짜 변환 중 오류가 발생할 수 있습니다.</exception>
        public static DateTime ToSolarDate(this UnlimitedDateTime dateTime) => _klc.ToDateTime(dateTime.year, dateTime.month, dateTime.day, dateTime.hour, dateTime.minute, dateTime.second, dateTime.millisecond);
        #endregion
    }
}