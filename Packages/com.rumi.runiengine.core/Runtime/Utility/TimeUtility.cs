#nullable enable
using System;
using System.Globalization;

namespace RuniEngine
{
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

        public const long timeSpanTicksPerYear = (long)(TimeSpan.TicksPerDay * dayPerYear);
        public const long timeSpanTicksPerMonth = (long)(TimeSpan.TicksPerDay * dayPerMonth);
        public const long timeSpanTicksPerWeek = TimeSpan.TicksPerDay * dayPerWeek;

        public static int GetYears(this TimeSpan timeSpan) => (int)(timeSpan.Ticks / timeSpanTicksPerYear);
        public static double GetTotalYears(this TimeSpan timeSpan) => timeSpan.Ticks / (double)timeSpanTicksPerYear;

        public static int GetMonths(this TimeSpan timeSpan) => (int)(timeSpan.Ticks / timeSpanTicksPerMonth);
        public static double GetTotalMonths(this TimeSpan timeSpan) => timeSpan.Ticks / (double)timeSpanTicksPerMonth;

        public static int GetWeeks(this TimeSpan timeSpan) => (int)(timeSpan.Ticks / timeSpanTicksPerWeek);
        public static double GetTotalWeeks(this TimeSpan timeSpan) => timeSpan.Ticks / (double)timeSpanTicksPerWeek;



        #region To Time
        /// <summary>
        /// (second = 70) = "1:10"
        /// </summary>
        public static string ToTimeString(this int second, AlwayShowTimeUnit alwayShowTimeUnit = AlwayShowTimeUnit.minute, int decimalPlaces = 2) => ToTimeString((float)second, alwayShowTimeUnit, decimalPlaces);

        /// <summary>
        /// (second = 70.1f) = "1:10.1"
        /// </summary>
        public static string ToTimeString(this float second, AlwayShowTimeUnit alwayShowTimeUnit = AlwayShowTimeUnit.minute, int decimalPlaces = 2)
        {
            if (!float.IsNormal(second))
                return "--:--";

            return ToTimeString(second, alwayShowTimeUnit, new string('0', decimalPlaces));
        }
        #endregion

        /// <summary>
        /// (second = 70.1) = "1:10.1"
        /// </summary>
        public static string ToTimeString(this double second, AlwayShowTimeUnit alwayShowTimeUnit = AlwayShowTimeUnit.minute, string decimalFormat = "00")
        {
            if (!double.IsFinite(second))
                return "--:--";

            double secondAbs = second.Abs();

            bool first = true;
            string result = second < 0 ? "-" : string.Empty;

            if (secondAbs >= 86400)
                alwayShowTimeUnit = AlwayShowTimeUnit.day;
            else if (secondAbs >= 3600)
                alwayShowTimeUnit = AlwayShowTimeUnit.hour;
            else if (secondAbs >= 60)
                alwayShowTimeUnit = AlwayShowTimeUnit.minute;

            if (HasFlag(0b100))
                Calculate(secondPerDay, 0);
            if (HasFlag(0b010))
                Calculate(secondPerHour, HasFlag(0b100) ? 24 : 0);
            if (HasFlag(0b001))
                Calculate(secondPerMinute, HasFlag(0b010) ? 60 : 0);

            {
                double value = secondAbs;
                if (HasFlag(0b001))
                    value %= 60;

                string format;
                if (!first && value < 10)
                    format = "00";
                else
                    format = "0";

                if (!string.IsNullOrEmpty(decimalFormat))
                    format += "." + decimalFormat;

                result += value.ToString(format);
            }

            return result;

            void Calculate(int secondPerUnit, int repeat)
            {
                int value = (int)(secondAbs / secondPerUnit);
                if (repeat > 0)
                    value %= repeat;

                if (!first && value < 10)
                    result += 0.ToString();

                result += value;

                if (secondPerUnit > 1)
                    result += ':';

                first = false;
            }

            bool HasFlag(int flag)
            {
                int enumInt = Convert.ToInt32(alwayShowTimeUnit);
                return enumInt == (enumInt | flag);
            }
        }



        /*public static NameSpacePathReplacePair ToRelativeString(this TimeSpan timeSpan, int digits = 2)
        {
            try
            {
                NameSpacePathReplacePair nameSpacePathReplacePair = new NameSpacePathReplacePair();
                ReplaceOldNewPair replaceOldNewPair = new ReplaceOldNewPair("%value%", "");

                nameSpacePathReplacePair.nameSpace = "sc-krm";
                nameSpacePathReplacePair.path = "gui.";

                string isAgo = "later";
                if (timeSpan < TimeSpan.Zero)
                {
                    timeSpan = -timeSpan;
                    isAgo = "ago";
                }

                nameSpacePathReplacePair.path += isAgo + ".";

                switch (timeSpan.Ticks)
                {
                    case >= timeSpanTicksPerYear:
                        nameSpacePathReplacePair.path += "years";
                        replaceOldNewPair.replaceNew = timeSpan.GetTotalYears().Floor(digits).ToString("F" + digits);
                        break;
                    case >= timeSpanTicksPerMonth:
                        nameSpacePathReplacePair.path += "months";
                        replaceOldNewPair.replaceNew = timeSpan.GetTotalMonths().Floor(digits).ToString("F" + digits);
                        break;
                    case >= timeSpanTicksPerWeek:
                        nameSpacePathReplacePair.path += "weeks";
                        replaceOldNewPair.replaceNew = timeSpan.GetTotalWeeks().Floor(digits).ToString("F" + digits);
                        break;
                    case >= TimeSpan.TicksPerDay:
                        nameSpacePathReplacePair.path += "days";
                        replaceOldNewPair.replaceNew = timeSpan.TotalDays.Floor(digits).ToString("F" + digits);
                        break;
                    case >= TimeSpan.TicksPerHour:
                        nameSpacePathReplacePair.path += "hours";
                        replaceOldNewPair.replaceNew = timeSpan.TotalHours.Floor(digits).ToString("F" + digits);
                        break;
                    case >= TimeSpan.TicksPerMinute:
                        nameSpacePathReplacePair.path += "minutes";
                        replaceOldNewPair.replaceNew = timeSpan.TotalMinutes.Floor(digits).ToString("F" + digits);
                        break;
                    case >= TimeSpan.TicksPerSecond:
                        nameSpacePathReplacePair.path += "seconds";
                        replaceOldNewPair.replaceNew = timeSpan.TotalSeconds.Floor(digits).ToString("F" + digits);
                        break;
                    case >= TimeSpan.TicksPerMillisecond:
                        nameSpacePathReplacePair.path += "milliseconds";
                        replaceOldNewPair.replaceNew = timeSpan.TotalMilliseconds.Floor(digits).ToString("F" + digits);
                        break;
                    default:
                        return "";
                }

                nameSpacePathReplacePair.replace = new ReplaceOldNewPair[] { replaceOldNewPair };
                return nameSpacePathReplacePair;
            }
            catch (Exception)
            {
                return "";
            }
        }*/

        static readonly KoreanLunisolarCalendar klc = new KoreanLunisolarCalendar();
        public static UnlimitedDateTime ToLunarDate(this DateTime dateTime, out bool isLeapMonth)
        {
            int year = klc.GetYear(dateTime);
            int month = klc.GetMonth(dateTime);
            int day = klc.GetDayOfMonth(dateTime);
            int hour = klc.GetHour(dateTime);
            int minute = klc.GetMinute(dateTime);
            int second = klc.GetSecond(dateTime);
            int millisecond = (int)klc.GetMilliseconds(dateTime);

            isLeapMonth = false;

            //1년이 12이상이면 윤달이 있음..
            if (klc.GetMonthsInYear(year) > 12)
            {
                //년도의 윤달이 몇월인지?
                int leapMonth = klc.GetLeapMonth(year);

                //달이 윤월보다 같거나 크면 -1을 함 즉 윤8은->9 이기때문
                if (month >= leapMonth)
                {
                    isLeapMonth = true;
                    month--;
                }
            }

            return new UnlimitedDateTime(year, month, day, hour, minute, second, millisecond);
        }

        public static DateTime ToSolarDate(this DateTime dateTime, bool isLeapMonth = false)
        {
            if (klc.GetMonthsInYear(dateTime.Year) > 12)
            {
                int leapMonth = klc.GetLeapMonth(dateTime.Year);

                if (dateTime.Month > leapMonth - 1)
                    dateTime = klc.AddMonths(dateTime, 1);
                else if (dateTime.Month == leapMonth - 1 && isLeapMonth)
                    dateTime = klc.AddMonths(dateTime, 1);
            }

            return klc.ToDateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);
        }
    }
}
