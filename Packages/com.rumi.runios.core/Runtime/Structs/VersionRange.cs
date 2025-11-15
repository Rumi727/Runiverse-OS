#nullable enable
using Newtonsoft.Json;
using RuniOS.Json.Converters;

namespace RuniOS;

/// <summary>
/// 두 <see cref="Version"/> 값(최소 및 최대)을 사용하여 버전 범위를 나타내는 직렬화 가능한 구조체입니다.
/// <br/>
/// 특정 버전이 이 범위에 포함되는지 여부를 확인할 수 있는 메서드를 제공하며,
/// 문자열 파싱, 다른 타입과의 변환, 비교 연산자 등을 지원합니다.
/// </summary>
[Serializable]
[JsonConverter(typeof(VersionRangeConverter))]
public struct VersionRange : IEquatable<Version>, IEquatable<VersionRange>
{
    /// <summary>
    /// 버전 범위의 최소 및 최대 버전을 구분하는 데 사용되는 문자입니다 (예: '~' in "1.0.0~2.0.0").
    /// </summary>
    public const char separator = '~';

    /// <summary>
    /// 지정된 문자열을 파싱하여 <see cref="VersionRange"/> 구조체의 새 인스턴스를 초기화합니다.
    /// <br/>
    /// 문자열은 <c>"minVersion~maxVersion"</c> 형식일 수 있습니다.
    /// <br/>
    /// 문자열이 비어 있거나 파싱할 수 없는 경우, <see cref="min"/>과 <see cref="max"/>는 <see cref="Version.all"/>로 설정됩니다.
    /// 문자열이 하나의 버전만 포함하면 <see cref="min"/>과 <see cref="max"/>는 해당 버전으로 설정됩니다.
    /// </summary>
    /// <param name="value">파싱할 버전 범위 문자열입니다. <see langword="null"/>일 수 있습니다.</param>
    public VersionRange(string? value)
    {
        if (value == null)
        {
            min = max = Version.all;
            return;
        }

        string[]? versions = value.RemoveAllWhitespace().Split(separator);
        if (versions == null || versions.Length <= 0)
            min = max = Version.all;
        else
        {
            min = new Version(versions[0]);
            max = new Version(versions[^1]);
        }
    }

    /// <summary>
    /// 단일 <see cref="Version"/>을 사용하여 <see cref="VersionRange"/> 구조체의 새 인스턴스를 초기화합니다.
    /// <br/>
    /// 이 경우 <see cref="min"/>과 <see cref="max"/>는 모두 지정된 <paramref name="version"/>으로 설정됩니다.
    /// </summary>
    /// <param name="version">범위의 최소 및 최대가 될 <see cref="Version"/>입니다.</param>
    public VersionRange(Version version) => min = max = version;

    /// <summary>
    /// 지정된 최소 및 최대 <see cref="Version"/> 값을 사용하여 <see cref="VersionRange"/> 구조체의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="min">버전 범위의 최소 <see cref="Version"/>입니다.</param>
    /// <param name="max">버전 범위의 최대 <see cref="Version"/>입니다.</param>
    public VersionRange(Version min, Version max)
    {
        this.min = min;
        this.max = max;
    }

    /// <summary>
    /// 이 버전 범위의 최소 <see cref="Version"/>을 가져오거나 설정합니다.
    /// </summary>
    [FieldName("gui.min")] public Version min;
    /// <summary>
    /// 이 버전 범위의 최대 <see cref="Version"/>을 가져오거나 설정합니다.
    /// </summary>
    [FieldName("gui.max")] public Version max;


    /// <summary>
    /// <see cref="VersionRange"/> 인스턴스를 문자열로 암시적으로 변환합니다.
    /// <br/>
    /// <see cref="ToString()"/> 메서드를 사용합니다.
    /// </summary>
    /// <param name="value">변환할 <see cref="VersionRange"/> 인스턴스입니다.</param>
    /// <returns>변환된 버전 범위 문자열입니다.</returns>
    public static implicit operator string(VersionRange value) => value.ToString();

    /// <summary>
    /// 문자열을 <see cref="VersionRange"/> 인스턴스로 암시적으로 변환합니다.
    /// <br/>
    /// <see cref="VersionRange(string?)"/> 생성자를 사용합니다.
    /// </summary>
    /// <param name="value">변환할 버전 범위 문자열입니다.</param>
    /// <returns>변환된 <see cref="VersionRange"/> 인스턴스입니다.</returns>
    public static implicit operator VersionRange(string value) => new VersionRange(value);

    /// <summary>
    /// <see cref="VersionRange"/>를 튜플 <c>(Version min, Version max)</c>로 암시적으로 변환합니다.
    /// </summary>
    /// <param name="other">변환할 <see cref="VersionRange"/> 인스턴스입니다.</param>
    /// <returns>최소 및 최대 <see cref="Version"/>을 포함하는 튜플입니다.</returns>
    public static implicit operator (Version min, Version max)(VersionRange other) => (other.min, other.max);

    /// <summary>
    /// 튜플 <c>(Version min, Version max)</c>를 <see cref="VersionRange"/>로 암시적으로 변환합니다.
    /// </summary>
    /// <param name="other">변환할 튜플입니다.</param>
    /// <returns>지정된 최소 및 최대 <see cref="Version"/>을 가진 <see cref="VersionRange"/> 인스턴스입니다.</returns>
    public static implicit operator VersionRange((Version min, Version max) other) => new VersionRange(other.min, other.max);

    /// <summary>
    /// 두 <see cref="VersionRange"/> 인스턴스의 값이 같은지 여부를 결정합니다.
    /// <br/>
    /// 두 범위의 <see cref="min"/>과 <see cref="max"/> 버전이 모두 같을 경우 <see langword="true"/>를 반환합니다.
    /// </summary>
    /// <param name="lhs">왼쪽 <see cref="VersionRange"/> 인스턴스입니다.</param>
    /// <param name="rhs">오른쪽 <see cref="VersionRange"/> 인스턴스입니다.</param>
    /// <returns>두 범위가 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public static bool operator ==(VersionRange lhs, VersionRange rhs) => lhs.min == rhs.min && lhs.max == rhs.max;

    /// <summary>
    /// 두 <see cref="VersionRange"/> 인스턴스의 값이 다른지 여부를 결정합니다.
    /// </summary>
    /// <param name="lhs">왼쪽 <see cref="VersionRange"/> 인스턴스입니다.</param>
    /// <param name="rhs">오른쪽 <see cref="VersionRange"/> 인스턴스입니다.</param>
    /// <returns>두 범위가 다르면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public static bool operator !=(VersionRange lhs, VersionRange rhs) => !(lhs == rhs);

    /// <summary>
    /// <see cref="VersionRange"/>가 특정 <see cref="Version"/>과 같은지 여부를 결정합니다.
    /// <br/>
    /// 이 연산자는 범위의 <see cref="min"/>과 <see cref="max"/>가 모두 주어진 <paramref name="rhs"/> <see cref="Version"/>과 같을 때 <see langword="true"/>를 반환합니다.
    /// (일반적인 버전 범위의 포함 여부와는 다른 로직입니다.)
    /// </summary>
    /// <param name="lhs">왼쪽 <see cref="VersionRange"/> 인스턴스입니다.</param>
    /// <param name="rhs">오른쪽 <see cref="Version"/> 인스턴스입니다.</param>
    /// <returns>범위의 최소 및 최대가 지정된 버전과 모두 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public static bool operator ==(VersionRange lhs, Version rhs) => lhs.min == rhs && lhs.max == rhs;

    /// <summary>
    /// <see cref="VersionRange"/>가 특정 <see cref="Version"/>과 다른지 여부를 결정합니다.
    /// </summary>
    /// <param name="lhs">왼쪽 <see cref="VersionRange"/> 인스턴스입니다.</param>
    /// <param name="rhs">오른쪽 <see cref="Version"/> 인스턴스입니다.</param>
    /// <returns>범위가 지정된 버전과 다르면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public static bool operator !=(VersionRange lhs, Version rhs) => !(lhs == rhs);

    /// <summary>
    /// <see cref="VersionRange"/>의 최소 및 최대 버전이 특정 <see cref="Version"/>보다 모두 큰지 여부를 결정합니다.
    /// <br/>
    /// 이 연산자는 <see cref="min"/>과 <see cref="max"/>가 모두 주어진 <paramref name="rhs"/> <see cref="Version"/>보다 클 때 <see langword="true"/>를 반환합니다.
    /// (일반적인 버전 범위 비교와는 다른 로직일 수 있습니다.)
    /// </summary>
    /// <param name="lhs">왼쪽 <see cref="VersionRange"/> 인스턴스입니다.</param>
    /// <param name="rhs">오른쪽 <see cref="Version"/> 인스턴스입니다.</param>
    /// <returns>범위의 최소 및 최대가 지정된 버전보다 모두 크면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public static bool operator <(VersionRange lhs, Version rhs) => lhs.min > rhs && lhs.max > rhs; // NOTE: Operator '<' is implemented as '>' logic. This comment reflects the code's behavior.

    /// <summary>
    /// <see cref="VersionRange"/>의 최소 및 최대 버전이 특정 <see cref="Version"/>보다 모두 큰지 여부를 결정합니다.
    /// <br/>
    /// 이 연산자는 <see cref="min"/>과 <see cref="max"/>가 모두 주어진 <paramref name="rhs"/> <see cref="Version"/>보다 클 때 <see langword="true"/>를 반환합니다.
    /// </summary>
    /// <param name="lhs">왼쪽 <see cref="VersionRange"/> 인스턴스입니다.</param>
    /// <param name="rhs">오른쪽 <see cref="Version"/> 인스턴스입니다.</param>
    /// <returns>범위의 최소 및 최대가 지정된 버전보다 모두 크면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public static bool operator >(VersionRange lhs, Version rhs) => lhs.min > rhs && lhs.max > rhs;


    /// <summary>
    /// 지정된 <see cref="Version"/>이 이 버전 범위에 포함되는지 여부를 결정합니다.
    /// <br/>
    /// 버전이 범위의 <see cref="min"/> 이상이고 <see cref="max"/> 이하일 경우 <see langword="true"/>를 반환합니다.
    /// </summary>
    /// <param name="version">확인할 <see cref="Version"/>입니다.</param>
    /// <returns><paramref name="version"/>이 범위 내에 있으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public readonly bool Contains(Version version) => version >= min && version <= max;

    /// <summary>
    /// 지정된 <see cref="VersionRange"/>가 이 버전 범위 내에 완전히 포함되는지 여부를 결정합니다.
    /// <br/>
    /// 주어진 범위의 <see cref="VersionRange.min"/>이 이 범위의 <see cref="min"/> 이상이고,
    /// 주어진 범위의 <see cref="VersionRange.max"/>가 이 범위의 <see cref="max"/> 이하일 경우 <see langword="true"/>를 반환합니다.
    /// </summary>
    /// <param name="range">확인할 <see cref="VersionRange"/>입니다.</param>
    /// <returns><paramref name="range"/>가 이 범위 내에 완전히 포함되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public readonly bool Contains(VersionRange range) => range.min >= min && range.max <= max;

    /// <summary>
    /// 이 <see cref="VersionRange"/> 인스턴스가 단일 <see cref="Version"/>과 같은지 여부를 결정합니다.
    /// <br/>
    /// 범위의 <see cref="min"/>과 <see cref="max"/>가 모두 지정된 <paramref name="other"/> <see cref="Version"/>과 같을 경우 <see langword="true"/>를 반환합니다.
    /// </summary>
    /// <param name="other">현재 인스턴스와 비교할 <see cref="Version"/>입니다.</param>
    /// <returns>범위의 최소 및 최대가 지정된 버전과 모두 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public readonly bool Equals(Version other) => min == other && max == other;

    /// <summary>
    /// 이 <see cref="VersionRange"/> 인스턴스와 다른 지정된 <see cref="VersionRange"/> 인스턴스의 값이 같은지 여부를 결정합니다.
    /// </summary>
    /// <param name="other">현재 인스턴스와 비교할 <see cref="VersionRange"/>입니다.</param>
    /// <returns>지정된 <see cref="VersionRange"/>가 현재 인스턴스와 같은 값을 가지면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public readonly bool Equals(VersionRange other) => this == other;

    /// <summary>
    /// 이 <see cref="VersionRange"/> 인스턴스와 지정된 개체의 값이 같은지 여부를 결정합니다.
    /// </summary>
    /// <param name="obj">현재 인스턴스와 비교할 개체입니다. <see cref="VersionRange"/> 또는 <see cref="Version"/> 타입일 수 있습니다.</param>
    /// <returns>지정된 개체가 <see cref="VersionRange"/> 또는 <see cref="Version"/>이고 현재 인스턴스와 같은 값을 가지면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public override readonly bool Equals(object? obj)
    {
        return obj switch
        {
            VersionRange range => Equals(range),
            Version version => Equals(version),
            _ => false
        };
    }

    /// <summary>
    /// 이 <see cref="VersionRange"/> 인스턴스의 해시 코드를 반환합니다.
    /// </summary>
    /// <returns>32비트 부호 있는 정수 해시 코드입니다.</returns>
    public override readonly int GetHashCode() => HashCode.Combine(min, max);


    /// <summary>
    /// 이 <see cref="VersionRange"/> 인스턴스의 문자열 표현을 반환합니다.
    /// <br/>
    /// 형식은 "minVersion~maxVersion"입니다.
    /// <br/>
    /// 예: "1.0.0~2.5.0", "*.*.*~1.0.0"
    /// </summary>
    /// <returns>이 인스턴스의 문자열 표현입니다.</returns>
    public override readonly string ToString() => $"{min}{separator}{max}";
}