#nullable enable
using System;

namespace RuniEngine
{
    [Serializable]
    public struct UnlimitedDateTime : IEquatable<UnlimitedDateTime>, IComparable, IComparable<UnlimitedDateTime>
    {
        public UnlimitedDateTime(int year, int month, int day) : this()
        {
            this.year = year;
            this.month = month;
            this.day = day;
        }

        public UnlimitedDateTime(int year, int month, int day, int hour, int minute, int second) : this(year, month, day)
        {
            this.hour = hour;
            this.minute = minute;
            this.second = second;
        }

        public UnlimitedDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond) : this(year, month, day, hour, minute, second) => this.millisecond = millisecond;

        [FieldName("gui.year")] public int year;
        [FieldName("gui.month")] public int month;
        [FieldName("gui.day")] public int day;
        [FieldName("gui.hour")] public int hour;
        [FieldName("gui.minute")] public int minute;
        [FieldName("gui.second")] public int second;
        [FieldName("gui.millisecond")] public int millisecond;

        public static explicit operator DateTime(UnlimitedDateTime dateTime) => new DateTime(dateTime.year, dateTime.month, dateTime.day, dateTime.hour, dateTime.minute, dateTime.second, dateTime.minute);
        public static implicit operator UnlimitedDateTime(DateTime dateTime) => new UnlimitedDateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Minute);

        public static bool operator <=(UnlimitedDateTime lhs, UnlimitedDateTime rhs)
        {
            if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour == rhs.hour && lhs.minute == rhs.minute && lhs.second == rhs.second && lhs.millisecond <= rhs.millisecond)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour == rhs.hour && lhs.minute == rhs.minute && lhs.second < rhs.second)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour == rhs.hour && lhs.minute < rhs.minute)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour < rhs.hour)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day < rhs.day)
                return true;
            else if (lhs.year == rhs.year && lhs.month < rhs.month)
                return true;
            else if (lhs.year < rhs.year)
                return true;

            return false;
        }

        public static bool operator >=(UnlimitedDateTime lhs, UnlimitedDateTime rhs)
        {
            if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour == rhs.hour && lhs.minute == rhs.minute && lhs.second == rhs.second && lhs.millisecond >= rhs.millisecond)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour == rhs.hour && lhs.minute == rhs.minute && lhs.second > rhs.second)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour == rhs.hour && lhs.minute > rhs.minute)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour > rhs.hour)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day > rhs.day)
                return true;
            else if (lhs.year == rhs.year && lhs.month > rhs.month)
                return true;
            else if (lhs.year > rhs.year)
                return true;

            return false;
        }

        public static bool operator <(UnlimitedDateTime lhs, UnlimitedDateTime rhs)
        {
            if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour == rhs.hour && lhs.minute == rhs.minute && lhs.second == rhs.second && lhs.millisecond < rhs.millisecond)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour == rhs.hour && lhs.minute == rhs.minute && lhs.second < rhs.second)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour == rhs.hour && lhs.minute < rhs.minute)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour < rhs.hour)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day < rhs.day)
                return true;
            else if (lhs.year == rhs.year && lhs.month < rhs.month)
                return true;
            else if (lhs.year < rhs.year)
                return true;

            return false;
        }

        public static bool operator >(UnlimitedDateTime lhs, UnlimitedDateTime rhs)
        {
            if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour == rhs.hour && lhs.minute == rhs.minute && lhs.second == rhs.second && lhs.millisecond > rhs.millisecond)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour == rhs.hour && lhs.minute == rhs.minute && lhs.second > rhs.second)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour == rhs.hour && lhs.minute > rhs.minute)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour > rhs.hour)
                return true;
            else if (lhs.year == rhs.year && lhs.month == rhs.month && lhs.day > rhs.day)
                return true;
            else if (lhs.year == rhs.year && lhs.month > rhs.month)
                return true;
            else if (lhs.year > rhs.year)
                return true;

            return false;
        }

        public static bool operator ==(UnlimitedDateTime lhs, UnlimitedDateTime rhs) => lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour == rhs.hour && lhs.minute == rhs.minute && lhs.second == rhs.second && lhs.millisecond == rhs.millisecond;
        public static bool operator !=(UnlimitedDateTime lhs, UnlimitedDateTime rhs) => !(lhs == rhs);

        public readonly bool Equals(UnlimitedDateTime other) => this == other;

        public override readonly bool Equals(object obj)
        {
            if (obj is not UnlimitedDateTime)
                return false;

            return Equals((UnlimitedDateTime)obj);
        }

        public override readonly int GetHashCode()
        {
            unchecked
            {
                int hash = 708572895;
                hash *= 95514639 + year.GetHashCode();
                hash *= -541921074 + month.GetHashCode();
                hash *= 533634984 + day.GetHashCode();
                hash *= -402168980 + hour.GetHashCode();
                hash *= -182987143 + minute.GetHashCode();
                hash *= -309051701 + second.GetHashCode();
                hash *= 909854872 + millisecond.GetHashCode();

                return hash;
            }
        }

        public readonly int CompareTo(object? value)
        {
            if (value == null)
                return 1;
            else if (value is UnlimitedDateTime version)
                return CompareTo(version);

            throw new ArgumentException();
        }

        public readonly int CompareTo(UnlimitedDateTime value)
        {
            if (this < value)
                return -1;
            else if (this > value)
                return 1;
            else
                return 0;
        }

        public override readonly string ToString() => $"{year}-{month}-{day} {hour}:{minute}:{second}";
    }
}
