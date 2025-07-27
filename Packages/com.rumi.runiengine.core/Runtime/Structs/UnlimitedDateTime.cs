#nullable enable
using System;

namespace RuniEngine
{
    /// <summary>
    /// <see cref="DateTime"/>의 제한된 범위(예: 0001년 1월 1일)를 넘어 무제한적인 날짜 및 시간 값을 표현하는 직렬화 가능한 구조체입니다.
    /// <br/>
    /// 연, 월, 일, 시, 분, 초, 밀리초 구성 요소를 개별적으로 저장하며,
    /// <see cref="DateTime"/>과의 변환 및 비교 연산자를 지원합니다.
    /// </summary>
    [Serializable]
    public struct UnlimitedDateTime : IEquatable<UnlimitedDateTime>, IComparable, IComparable<UnlimitedDateTime>
    {
        /// <summary>
        /// 지정된 연, 월, 일로 <see cref="UnlimitedDateTime"/> 구조체의 새 인스턴스를 초기화합니다.
        /// 시간 구성 요소(시, 분, 초, 밀리초)는 기본값인 0으로 설정됩니다.
        /// </summary>
        /// <param name="year">연도입니다.</param>
        /// <param name="month">월(1-12)입니다.</param>
        /// <param name="day">일(1-31)입니다.</param>
        public UnlimitedDateTime(int year, int month, int day) : this()
        {
            this.year = year;
            this.month = month;
            this.day = day;
        }

        /// <summary>
        /// 지정된 연, 월, 일, 시, 분, 초로 <see cref="UnlimitedDateTime"/> 구조체의 새 인스턴스를 초기화합니다.
        /// 밀리초 구성 요소는 기본값인 0으로 설정됩니다.
        /// </summary>
        /// <param name="year">연도입니다.</param>
        /// <param name="month">월(1-12)입니다.</param>
        /// <param name="day">일(1-31)입니다.</param>
        /// <param name="hour">시(0-23)입니다.</param>
        /// <param name="minute">분(0-59)입니다.</param>
        /// <param name="second">초(0-59)입니다.</param>
        public UnlimitedDateTime(int year, int month, int day, int hour, int minute, int second) : this(year, month, day)
        {
            this.hour = hour;
            this.minute = minute;
            this.second = second;
        }

        /// <summary>
        /// 지정된 연, 월, 일, 시, 분, 초, 밀리초로 <see cref="UnlimitedDateTime"/> 구조체의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="year">연도입니다.</param>
        /// <param name="month">월(1-12)입니다.</param>
        /// <param name="day">일(1-31)입니다.</param>
        /// <param name="hour">시(0-23)입니다.</param>
        /// <param name="minute">분(0-59)입니다.</param>
        /// <param name="second">초(0-59)입니다.</param>
        /// <param name="millisecond">밀리초(0-999)입니다.</param>
        public UnlimitedDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond) : this(year, month, day, hour, minute, second) => this.millisecond = millisecond;

        /// <summary>
        /// 연도 구성 요소를 가져오거나 설정합니다.
        /// </summary>
        [FieldName("gui.year")] public int year;
        /// <summary>
        /// 월 구성 요소(1-12)를 가져오거나 설정합니다.
        /// </summary>
        [FieldName("gui.month")] public int month;
        /// <summary>
        /// 일 구성 요소(1-31)를 가져오거나 설정합니다.
        /// </summary>
        [FieldName("gui.day")] public int day;
        /// <summary>
        /// 시 구성 요소(0-23)를 가져오거나 설정합니다.
        /// </summary>
        [FieldName("gui.hour")] public int hour;
        /// <summary>
        /// 분 구성 요소(0-59)를 가져오거나 설정합니다.
        /// </summary>
        [FieldName("gui.minute")] public int minute;
        /// <summary>
        /// 초 구성 요소(0-59)를 가져오거나 설정합니다.
        /// </summary>
        [FieldName("gui.second")] public int second;
        /// <summary>
        /// 밀리초 구성 요소(0-999)를 가져오거나 설정합니다.
        /// </summary>
        [FieldName("gui.millisecond")] public int millisecond;

        /// <summary>
        /// <see cref="UnlimitedDateTime"/>을 <see cref="DateTime"/>으로 명시적으로 변환합니다.
        /// <br/>
        /// 변환 시 <see cref="DateTime"/>의 유효한 범위(0001년 1월 1일 ~ 9999년 12월 31일)를 벗어나면
        /// <see cref="ArgumentOutOfRangeException"/>이 발생할 수 있습니다.
        /// </summary>
        /// <param name="dateTime">변환할 <see cref="UnlimitedDateTime"/> 값입니다.</param>
        /// <returns>변환된 <see cref="DateTime"/> 값입니다.</returns>
        /// <exception cref="ArgumentOutOfRangeException">변환된 날짜 및 시간이 <see cref="DateTime"/>의 유효한 범위를 벗어날 경우 발생합니다.</exception>
        public static explicit operator DateTime(UnlimitedDateTime dateTime) => new DateTime(dateTime.year, dateTime.month, dateTime.day, dateTime.hour, dateTime.minute, dateTime.second, dateTime.millisecond);
        
        /// <summary>
        /// <see cref="DateTime"/>을 <see cref="UnlimitedDateTime"/>으로 암시적으로 변환합니다.
        /// </summary>
        /// <param name="dateTime">변환할 <see cref="DateTime"/> 값입니다.</param>
        /// <returns>변환된 <see cref="UnlimitedDateTime"/> 값입니다.</returns>
        public static implicit operator UnlimitedDateTime(DateTime dateTime) => new UnlimitedDateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);

        /// <summary>
        /// 두 <see cref="UnlimitedDateTime"/> 인스턴스의 값이 작거나 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="UnlimitedDateTime"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="UnlimitedDateTime"/> 인스턴스입니다.</param>
        /// <returns>왼쪽 인스턴스가 오른쪽 인스턴스보다 작거나 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator <=(UnlimitedDateTime lhs, UnlimitedDateTime rhs)
        {
            if (lhs.year < rhs.year) return true;
            if (lhs.year > rhs.year) return false;

            if (lhs.month < rhs.month) return true;
            if (lhs.month > rhs.month) return false;

            if (lhs.day < rhs.day) return true;
            if (lhs.day > rhs.day) return false;

            if (lhs.hour < rhs.hour) return true;
            if (lhs.hour > rhs.hour) return false;

            if (lhs.minute < rhs.minute) return true;
            if (lhs.minute > rhs.minute) return false;

            if (lhs.second < rhs.second) return true;
            if (lhs.second > rhs.second) return false;

            return lhs.millisecond <= rhs.millisecond;
        }

        /// <summary>
        /// 두 <see cref="UnlimitedDateTime"/> 인스턴스의 값이 크거나 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="UnlimitedDateTime"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="UnlimitedDateTime"/> 인스턴스입니다.</param>
        /// <returns>왼쪽 인스턴스가 오른쪽 인스턴스보다 크거나 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator >=(UnlimitedDateTime lhs, UnlimitedDateTime rhs)
        {
            if (lhs.year > rhs.year) return true;
            if (lhs.year < rhs.year) return false;

            if (lhs.month > rhs.month) return true;
            if (lhs.month < rhs.month) return false;

            if (lhs.day > rhs.day) return true;
            if (lhs.day < rhs.day) return false;

            if (lhs.hour > rhs.hour) return true;
            if (lhs.hour < rhs.hour) return false;

            if (lhs.minute > rhs.minute) return true;
            if (lhs.minute < rhs.minute) return false;

            if (lhs.second > rhs.second) return true;
            if (lhs.second < rhs.second) return false;

            return lhs.millisecond >= rhs.millisecond;
        }

        /// <summary>
        /// 두 <see cref="UnlimitedDateTime"/> 인스턴스의 값이 작은지 여부를 결정합니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="UnlimitedDateTime"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="UnlimitedDateTime"/> 인스턴스입니다.</param>
        /// <returns>왼쪽 인스턴스가 오른쪽 인스턴스보다 작으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator <(UnlimitedDateTime lhs, UnlimitedDateTime rhs)
        {
            if (lhs.year < rhs.year) return true;
            if (lhs.year > rhs.year) return false;

            if (lhs.month < rhs.month) return true;
            if (lhs.month > rhs.month) return false;

            if (lhs.day < rhs.day) return true;
            if (lhs.day > rhs.day) return false;

            if (lhs.hour < rhs.hour) return true;
            if (lhs.hour > rhs.hour) return false;

            if (lhs.minute < rhs.minute) return true;
            if (lhs.minute > rhs.minute) return false;

            if (lhs.second < rhs.second) return true;
            if (lhs.second > rhs.second) return false;

            return lhs.millisecond < rhs.millisecond;
        }

        /// <summary>
        /// 두 <see cref="UnlimitedDateTime"/> 인스턴스의 값이 큰지 여부를 결정합니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="UnlimitedDateTime"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="UnlimitedDateTime"/> 인스턴스입니다.</param>
        /// <returns>왼쪽 인스턴스가 오른쪽 인스턴스보다 크면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator >(UnlimitedDateTime lhs, UnlimitedDateTime rhs)
        {
            if (lhs.year > rhs.year) return true;
            if (lhs.year < rhs.year) return false;

            if (lhs.month > rhs.month) return true;
            if (lhs.month < rhs.month) return false;

            if (lhs.day > rhs.day) return true;
            if (lhs.day < rhs.day) return false;

            if (lhs.hour > rhs.hour) return true;
            if (lhs.hour < rhs.hour) return false;

            if (lhs.minute > rhs.minute) return true;
            if (lhs.minute < rhs.minute) return false;

            if (lhs.second > rhs.second) return true;
            if (lhs.second < rhs.second) return false;

            return lhs.millisecond > rhs.millisecond;
        }

        /// <summary>
        /// 두 <see cref="UnlimitedDateTime"/> 인스턴스의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="UnlimitedDateTime"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="UnlimitedDateTime"/> 인스턴스입니다.</param>
        /// <returns>두 인스턴스가 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator ==(UnlimitedDateTime lhs, UnlimitedDateTime rhs) => lhs.year == rhs.year && lhs.month == rhs.month && lhs.day == rhs.day && lhs.hour == rhs.hour && lhs.minute == rhs.minute && lhs.second == rhs.second && lhs.millisecond == rhs.millisecond;
        
        /// <summary>
        /// 두 <see cref="UnlimitedDateTime"/> 인스턴스의 값이 다른지 여부를 결정합니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="UnlimitedDateTime"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="UnlimitedDateTime"/> 인스턴스입니다.</param>
        /// <returns>두 인스턴스가 다르면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator !=(UnlimitedDateTime lhs, UnlimitedDateTime rhs) => !(lhs == rhs);

        /// <summary>
        /// 이 <see cref="UnlimitedDateTime"/> 인스턴스와 다른 지정된 <see cref="UnlimitedDateTime"/> 인스턴스의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="other">현재 인스턴스와 비교할 <see cref="UnlimitedDateTime"/>입니다.</param>
        /// <returns>지정된 <see cref="UnlimitedDateTime"/>가 현재 인스턴스와 같은 값을 가지면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public readonly bool Equals(UnlimitedDateTime other) => this == other;

        /// <summary>
        /// 이 <see cref="UnlimitedDateTime"/> 인스턴스와 지정된 <see cref="object"/>의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="obj">현재 인스턴스와 비교할 <see cref="object"/>입니다.</param>
        /// <returns>지정된 <see cref="object"/>가 <see cref="UnlimitedDateTime"/>이고 현재 인스턴스와 같은 값을 가지면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public override readonly bool Equals(object? obj)
        {
            if (obj is not UnlimitedDateTime value)
                return false;

            return Equals(value);
        }

        /// <summary>
        /// 이 <see cref="UnlimitedDateTime"/> 인스턴스의 해시 코드를 반환합니다.
        /// </summary>
        /// <returns>32비트 부호 있는 정수 해시 코드입니다.</returns>
        public override readonly int GetHashCode() => HashCode.Combine(year, month, day, hour, minute, second, millisecond);

        /// <summary>
        /// 이 <see cref="UnlimitedDateTime"/> 인스턴스와 지정된 개체를 비교하고 상대 순서를 나타내는 값을 반환합니다.
        /// </summary>
        /// <param name="value">현재 인스턴스와 비교할 개체입니다.</param>
        /// <returns>
        /// 현재 인스턴스가 <paramref name="value"/>보다 작으면 음수,
        /// 현재 인스턴스가 <paramref name="value"/>와 같으면 0,
        /// 현재 인스턴스가 <paramref name="value"/>보다 크면 양수입니다.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="value"/>가 <see langword="null"/>이 아니고 <see cref="UnlimitedDateTime"/> 타입이 아닌 경우 발생합니다.</exception>
        public readonly int CompareTo(object? value)
        {
            if (value == null)
                return 1; // null은 항상 현재 인스턴스보다 작다고 간주합니다.
            else if (value is UnlimitedDateTime version)
                return CompareTo(version);

            throw new ArgumentException($"Object must be of type {nameof(UnlimitedDateTime)}.");
        }

        /// <summary>
        /// 이 <see cref="UnlimitedDateTime"/> 인스턴스와 다른 <see cref="UnlimitedDateTime"/> 인스턴스를 비교하고 상대 순서를 나타내는 값을 반환합니다.
        /// </summary>
        /// <param name="value">현재 인스턴스와 비교할 <see cref="UnlimitedDateTime"/>입니다.</param>
        /// <returns>
        /// 현재 인스턴스가 <paramref name="value"/>보다 작으면 음수,
        /// 현재 인스턴스가 <paramref name="value"/>와 같으면 0,
        /// 현재 인스턴스가 <paramref name="value"/>보다 크면 양수입니다.
        /// </returns>
        public readonly int CompareTo(UnlimitedDateTime value)
        {
            if (this < value)
                return -1;
            else if (this > value)
                return 1;
            else
                return 0;
        }

        /// <summary>
        /// 이 <see cref="UnlimitedDateTime"/> 인스턴스의 문자열 표현을 반환합니다.
        /// </summary>
        /// <returns>"YYYY-MM-DD HH:MM:SS" 형식의 문자열입니다 (밀리초는 포함되지 않음).
        /// <br/>
        /// 예: "2023-07-27 10:30:45"
        /// </returns>
        public override readonly string ToString() => $"{year}-{month:D2}-{day:D2} {hour:D2}:{minute:D2}:{second:D2}";
    }
}