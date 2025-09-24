#nullable enable
using Newtonsoft.Json;
using RuniOS.Json.Converters;
using System;
using UnityEngine;

namespace RuniOS
{
    /// <summary>
    /// 메이저, 마이너, 패치 버전 구성 요소를 가질 수 있는 유니티 직렬화 가능한 버전 구조체입니다.
    /// <br/>
    /// 각 구성 요소는 <see langword="null"/>을 가질 수 있어 유연한 버전 관리가 가능합니다.
    /// <br/>
    /// <see cref="IEquatable{T}"/>, <see cref="IComparable"/> 인터페이스를 구현하여 비교 및 정렬 기능을 제공하며,
    /// <see cref="ISerializationCallbackReceiver"/>를 통해 유니티 직렬화 시 <see cref="Nullable{T}"/> 값을 처리합니다.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(VersionConverter))]
    public struct Version : IEquatable<Version>, IEquatable<VersionRange>, IComparable, IComparable<Version>
    {
        /// <summary>
        /// 버전 구성 요소를 구분하는 데 사용되는 문자입니다 (예: '.' in "1.2.3").
        /// </summary>
        public const char separator = '.';
        /// <summary>
        /// 버전 구성 요소가 <see langword="null"/>이거나 지정되지 않았음을 나타내는 데 사용되는 문자입니다.
        /// </summary>
        public const char noneSeparator = '*';

        /// <summary>
        /// 모든 버전 구성 요소가 <see langword="null"/>인 <see cref="Version"/> 인스턴스를 가져옵니다.
        /// <br/>
        /// 이는 모든 버전을 나타내는 데 사용될 수 있습니다.
        /// </summary>
        [JsonIgnore] public static Version all => new Version();
        /// <summary>
        /// 모든 버전 구성 요소가 0인 <see cref="Version"/> 인스턴스를 가져옵니다 (0.0.0).
        /// </summary>
        [JsonIgnore] public static Version zero => new Version(0, 0, 0);
        /// <summary>
        /// 메이저 버전만 1이고 나머지 구성 요소가 0인 <see cref="Version"/> 인스턴스를 가져옵니다 (1.0.0).
        /// </summary>
        [JsonIgnore] public static Version one => new Version(1, 0, 0);

        /// <summary>
        /// 이 버전의 메이저 구성 요소를 가져오거나 설정합니다. <see langword="null"/>일 수 있습니다.
        /// </summary>
        public int? major
        {
            readonly get => _major;
            set => _major = value;
        }
        
        /// <summary>
        /// 이 버전의 마이너 구성 요소를 가져오거나 설정합니다. <see langword="null"/>일 수 있습니다.
        /// </summary>
        public int? minor
        {
            readonly get => _minor;
            set => _minor = value;
        }
        
        /// <summary>
        /// 이 버전의 패치 구성 요소를 가져오거나 설정합니다. <see langword="null"/>일 수 있습니다.
        /// </summary>
        public int? patch
        {
            readonly get => _patch;
            set => _patch = value;
        }

        /// <summary>
        /// 유니티 직렬화를 위한 메이저 버전 구성 요소의 내부 필드입니다.
        /// </summary>
        [SerializeField, FieldName("gui.major"), NullableField("*")] SerializableNullable<int> _major;
        /// <summary>
        /// 유니티 직렬화를 위한 마이너 버전 구성 요소의 내부 필드입니다.
        /// </summary>
        [SerializeField, FieldName("gui.minor"), NullableField("*")] SerializableNullable<int> _minor;
        /// <summary>
        /// 유니티 직렬화를 위한 패치 버전 구성 요소의 내부 필드입니다.
        /// </summary>
        [SerializeField, FieldName("gui.patch"), NullableField("*")] SerializableNullable<int> _patch;

        /// <summary>
        /// 지정된 문자열을 파싱하여 <see cref="Version"/> 구조체의 새 인스턴스를 초기화합니다.
        /// <br/>
        /// 문자열은 <c>"X.Y.Z"</c> 또는 <c>"X.Y"</c>, <c>"X"</c> 형식일 수 있습니다.
        /// 파싱할 수 없는 구성 요소는 <see langword="null"/>로 설정됩니다.
        /// </summary>
        /// <param name="value">파싱할 버전 문자열입니다. <see langword="null"/>일 수 있습니다.</param>
        public Version(string? value)
        {
            if (value == null)
            {
                _major = _minor = _patch = null;
                return;
            }

            string[]? versions = value.RemoveAllWhitespace().Split(separator);
            if (versions == null || versions.Length <= 0)
                _major = _minor = _patch = null;
            else switch (versions.Length)
            {
                case 1:
                {
                    if (int.TryParse(versions[0], out int major))
                        _major = major;
                    else
                        _major = null;

                    _minor = null;
                    _patch = null;
                    break;
                }
                case 2:
                {
                    if (int.TryParse(versions[0], out int major))
                        _major = major;
                    else
                        _major = null;

                    if (int.TryParse(versions[1], out int minor))
                        _minor = minor;
                    else
                        _minor = null;

                    _patch = null;
                    break;
                }
                default:
                {
                    {
                        if (int.TryParse(versions[0], out int major))
                            _major = major;
                        else
                            _major = null;
                    }

                    {
                        if (int.TryParse(versions[1], out int minor))
                            _minor = minor;
                        else
                            _minor = null;
                    }

                    {
                        if (int.TryParse(versions[2], out int patch))
                            _patch = patch;
                        else
                            _patch = null;
                    }
                    break;
                }
            }
        }
        
        /// <summary>
        /// 지정된 메이저, 마이너, 패치 구성 요소를 사용하여 <see cref="Version"/> 구조체의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="major">메이저 버전 구성 요소입니다. <see langword="null"/>일 수 있습니다.</param>
        /// <param name="minor">마이너 버전 구성 요소입니다. <see langword="null"/>일 수 있습니다.</param>
        /// <param name="patch">패치 버전 구성 요소입니다. <see langword="null"/>일 수 있습니다.</param>
        public Version(int? major, int? minor, int? patch)
        {
            _major = major;
            _minor = minor;
            _patch = patch;
        }

        /// <summary>
        /// 두 <see cref="Version"/> 인스턴스의 값이 작거나 같은지 여부를 결정합니다.
        /// <br/>
        /// <see langword="null"/>인 구성 요소는 해당 레벨에서 모든 값과 일치한다고 간주됩니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <returns>왼쪽 인스턴스가 오른쪽 인스턴스보다 작거나 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator <=(Version lhs, Version rhs)
        {
            if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor == rhs.minor) && (lhs.patch == null || rhs.patch == null || lhs.patch <= rhs.patch))
                return true;
            else if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor < rhs.minor))
                return true;
            else if (lhs.major == null || rhs.major == null || lhs.major < rhs.major)
                return true;

            return false;
        }
        /// <summary>
        /// 두 <see cref="Version"/> 인스턴스의 값이 크거나 같은지 여부를 결정합니다.
        /// <br/>
        /// <see langword="null"/>인 구성 요소는 해당 레벨에서 모든 값과 일치한다고 간주됩니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <returns>왼쪽 인스턴스가 오른쪽 인스턴스보다 크거나 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator >=(Version lhs, Version rhs)
        {
            if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor == rhs.minor) && (lhs.patch == null || rhs.patch == null || lhs.patch >= rhs.patch))
                return true;
            else if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor > rhs.minor))
                return true;
            else if (lhs.major == null || rhs.major == null || lhs.major > rhs.major)
                return true;

            return false;
        }
        /// <summary>
        /// 두 <see cref="Version"/> 인스턴스의 값이 작은지 여부를 결정합니다.
        /// <br/>
        /// <see langword="null"/>인 구성 요소는 해당 레벨에서 모든 값과 일치한다고 간주됩니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <returns>왼쪽 인스턴스가 오른쪽 인스턴스보다 작으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator <(Version lhs, Version rhs)
        {
            if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor == rhs.minor) && (lhs.patch == null || rhs.patch == null || lhs.patch < rhs.patch))
                return true;
            else if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor < rhs.minor))
                return true;
            else if (lhs.major == null || rhs.major == null || lhs.major < rhs.major)
                return true;

            return false;
        }
        /// <summary>
        /// 두 <see cref="Version"/> 인스턴스의 값이 큰지 여부를 결정합니다.
        /// <br/>
        /// <see langword="null"/>인 구성 요소는 해당 레벨에서 모든 값과 일치한다고 간주됩니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <returns>왼쪽 인스턴스가 오른쪽 인스턴스보다 크면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator >(Version lhs, Version rhs)
        {
            if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor == rhs.minor) && (lhs.patch == null || rhs.patch == null || lhs.patch > rhs.patch))
                return true;
            else if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor > rhs.minor))
                return true;
            else if (lhs.major == null || rhs.major == null || lhs.major > rhs.major)
                return true;

            return false;
        }

        /// <summary>
        /// 두 <see cref="Version"/> 인스턴스의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <returns>두 인스턴스의 모든 구성 요소가 같거나 둘 중 하나가 <see langword="null"/>이면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator ==(Version lhs, Version rhs) => lhs.major == rhs.major && lhs.minor == rhs.minor && lhs.patch == rhs.patch;
        
        /// <summary>
        /// 두 <see cref="Version"/> 인스턴스의 값이 다른지 여부를 결정합니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <returns>두 인스턴스가 다르면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator !=(Version lhs, Version rhs) => !(lhs == rhs);

        /// <summary>
        /// <see cref="Version"/>이 <see cref="VersionRange"/>의 최소 및 최대 버전과 모두 같은지 여부를 결정합니다.
        /// <br/>
        /// 이 연산자는 <paramref name="lhs"/>가 <paramref name="rhs"/>의 <see cref="VersionRange.min"/>과 같고
        /// 동시에 <paramref name="lhs"/>가 <paramref name="rhs"/>의 <see cref="VersionRange.max"/>와 같을 때 <see langword="true"/>를 반환합니다.
        /// (일반적인 버전 범위의 포함 여부와는 다른 로직일 수 있습니다.)
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="VersionRange"/> 인스턴스입니다.</param>
        /// <returns><paramref name="lhs"/>가 <paramref name="rhs"/>의 최소 및 최대 버전과 모두 같으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator ==(Version lhs, VersionRange rhs) => lhs == rhs.min && lhs == rhs.max;
        
        /// <summary>
        /// <see cref="Version"/>이 <see cref="VersionRange"/>의 최소 및 최대 버전 중 하나라도 같지 않은지 여부를 결정합니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="VersionRange"/> 인스턴스입니다.</param>
        /// <returns><paramref name="lhs"/>가 <paramref name="rhs"/>의 최소 및 최대 버전 중 하나라도 같지 않으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator !=(Version lhs, VersionRange rhs) => !(lhs == rhs);

        /// <summary>
        /// <see cref="Version"/>이 <see cref="VersionRange"/>의 최소 및 최대 버전보다 모두 작은지 여부를 결정합니다.
        /// <br/>
        /// 이 연산자는 <paramref name="lhs"/>가 <paramref name="rhs"/>의 <see cref="VersionRange.min"/>보다 작고
        /// 동시에 <paramref name="lhs"/>가 <paramref name="rhs"/>의 <see cref="VersionRange.max"/>보다 작을 때 <see langword="true"/>를 반환합니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="VersionRange"/> 인스턴스입니다.</param>
        /// <returns><paramref name="lhs"/>가 <paramref name="rhs"/>의 최소 및 최대 버전보다 모두 작으면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator <(Version lhs, VersionRange rhs) => lhs < rhs.min && lhs < rhs.max;
        
        /// <summary>
        /// <see cref="Version"/>이 <see cref="VersionRange"/>의 최소 및 최대 버전보다 모두 큰지 여부를 결정합니다.
        /// <br/>
        /// 이 연산자는 <paramref name="lhs"/>가 <paramref name="rhs"/>의 <see cref="VersionRange.min"/>보다 크고
        /// 동시에 <paramref name="lhs"/>가 <paramref name="rhs"/>의 <see cref="VersionRange.max"/>보다 클 때 <see langword="true"/>를 반환합니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="VersionRange"/> 인스턴스입니다.</param>
        /// <returns><paramref name="lhs"/>가 <paramref name="rhs"/>의 최소 및 최대 버전보다 모두 크면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool operator >(Version lhs, VersionRange rhs) => lhs > rhs.min && lhs > rhs.max;

        /// <summary>
        /// 두 <see cref="Version"/> 인스턴스의 각 구성 요소를 더하여 새 <see cref="Version"/> 인스턴스를 반환합니다.
        /// <br/>
        /// 어느 한쪽의 구성 요소가 <see langword="null"/>이면 결과는 <see langword="null"/>이 됩니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <returns>더해진 각 구성 요소를 가진 새 <see cref="Version"/> 인스턴스입니다.</returns>
        public static Version operator +(Version lhs, Version rhs) => new Version(lhs.major + rhs.major, lhs.minor + rhs.minor, lhs.patch + rhs.patch);
        
        /// <summary>
        /// 두 <see cref="Version"/> 인스턴스의 각 구성 요소를 빼서 새 <see cref="Version"/> 인스턴스를 반환합니다.
        /// <br/>
        /// 어느 한쪽의 구성 요소가 <see langword="null"/>이면 결과는 <see langword="null"/>이 됩니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <returns>빼진 각 구성 요소를 가진 새 <see cref="Version"/> 인스턴스입니다.</returns>
        public static Version operator -(Version lhs, Version rhs) => new Version(lhs.major - rhs.major, lhs.minor - rhs.minor, lhs.patch - rhs.patch);

        /// <summary>
        /// <see cref="Version"/> 인스턴스의 패치 구성 요소에 정수 값을 더하여 새 <see cref="Version"/> 인스턴스를 반환합니다.
        /// <br/>
        /// <paramref name="lhs"/>의 패치 구성 요소가 <see langword="null"/>이면 결과 패치 구성 요소도 <see langword="null"/>이 됩니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <param name="rhs">더할 정수 값입니다.</param>
        /// <returns>패치 구성 요소가 업데이트된 새 <see cref="Version"/> 인스턴스입니다.</returns>
        public static Version operator +(Version lhs, int rhs) => new Version(lhs.major, lhs.minor, lhs.patch + rhs);
        
        /// <summary>
        /// <see cref="Version"/> 인스턴스의 패치 구성 요소에서 정수 값을 빼서 새 <see cref="Version"/> 인스턴스를 반환합니다.
        /// <br/>
        /// <paramref name="lhs"/>의 패치 구성 요소가 <see langword="null"/>이면 결과 패치 구성 요소도 <see langword="null"/>이 됩니다.
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <param name="rhs">뺄 정수 값입니다.</param>
        /// <returns>패치 구성 요소가 업데이트된 새 <see cref="Version"/> 인스턴스입니다.</returns>
        public static Version operator -(Version lhs, int rhs) => new Version(lhs.major, lhs.minor, lhs.patch - rhs);

        /// <summary>
        /// 정수 값을 <see cref="Version"/> 인스턴스의 패치 구성 요소에 더하여 새 <see cref="Version"/> 인스턴스를 반환합니다.
        /// <br/>
        /// <paramref name="rhs"/>의 패치 구성 요소가 <see langword="null"/>이면 결과 패치 구성 요소도 <see langword="null"/>이 됩니다.
        /// </summary>
        /// <param name="lhs">더할 정수 값입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <returns>패치 구성 요소가 업데이트된 새 <see cref="Version"/> 인스턴스입니다.</returns>
        public static Version operator +(int lhs, Version rhs) => new Version(rhs.major, rhs.minor, lhs + rhs.patch);
        
        /// <summary>
        /// <see cref="Version"/> 인스턴스의 패치 구성 요소에서 정수 값을 빼서 새 <see cref="Version"/> 인스턴스를 반환합니다.
        /// <br/>
        /// <paramref name="rhs"/>의 패치 구성 요소가 <see langword="null"/>이면 결과 패치 구성 요소도 <see langword="null"/>이 됩니다.
        /// </summary>
        /// <param name="lhs">정수 값입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="Version"/> 인스턴스입니다.</param>
        /// <returns>패치 구성 요소가 업데이트된 새 <see cref="Version"/> 인스턴스입니다.</returns>
        public static Version operator -(int lhs, Version rhs) => new Version(rhs.major, rhs.minor, lhs - rhs.patch);

        /// <summary>
        /// <see cref="Version"/> 인스턴스를 문자열로 암시적으로 변환합니다.
        /// <br/>
        /// <see cref="ToString()"/> 메서드를 사용합니다.
        /// </summary>
        /// <param name="value">변환할 <see cref="Version"/> 인스턴스입니다.</param>
        /// <returns>변환된 버전 문자열입니다.</returns>
        public static implicit operator string(Version value) => value.ToString();
        
        /// <summary>
        /// 문자열을 <see cref="Version"/> 인스턴스로 암시적으로 변환합니다.
        /// <br/>
        /// <see cref="Version"/> 생성자를 사용합니다.
        /// </summary>
        /// <param name="value">변환할 버전 문자열입니다.</param>
        /// <returns>변환된 <see cref="Version"/> 인스턴스입니다.</returns>
        public static implicit operator Version(string value) => new Version(value);

        /// <summary>
        /// <see cref="Version"/> 인스턴스를 <see cref="VersionRange"/>로 암시적으로 변환합니다.
        /// <br/>
        /// 변환된 <see cref="VersionRange"/>는 <see cref="VersionRange.min"/>과 <see cref="VersionRange.max"/>가 모두 이 버전 인스턴스와 동일한 값을 가집니다.
        /// </summary>
        /// <param name="value">변환할 <see cref="Version"/> 인스턴스입니다.</param>
        /// <returns>변환된 <see cref="VersionRange"/> 인스턴스입니다.</returns>
        public static implicit operator VersionRange(Version value) => new VersionRange(value);

        /// <summary>
        /// <see cref="Version"/> 인스턴스를 <see cref="Vector3Int"/>으로 암시적으로 변환합니다.
        /// <br/>
        /// <see cref="major"/>, <see cref="minor"/>, <see cref="patch"/>가 각각 <see cref="Vector3Int.x"/>, <see cref="Vector3Int.y"/>, <see cref="Vector3Int.z"/>로 매핑됩니다.
        /// <see langword="null"/>인 구성 요소는 0으로 변환됩니다.
        /// </summary>
        /// <param name="value">변환할 <see cref="Version"/> 인스턴스입니다.</param>
        /// <returns>변환된 <see cref="Vector3Int"/> 값입니다.</returns>
        public static implicit operator Vector3Int(Version value) => new Vector3Int(value.major ?? 0, value.minor ?? 0, value.patch ?? 0);
        
        /// <summary>
        /// <see cref="Vector3Int"/>를 <see cref="Version"/> 인스턴스로 암시적으로 변환합니다.
        /// <br/>
        /// <see cref="Vector3Int.x"/>, <see cref="Vector3Int.y"/>, <see cref="Vector3Int.z"/>가 각각 <see cref="major"/>, <see cref="minor"/>, <see cref="patch"/>로 매핑됩니다.
        /// </summary>
        /// <param name="value">변환할 <see cref="Vector3Int"/> 값입니다.</param>
        /// <returns>변환된 <see cref="Version"/> 인스턴스입니다.</returns>
        public static implicit operator Version(Vector3Int value) => new Version(value.x, value.y, value.z);

        /// <summary>
        /// <see cref="Version"/> 인스턴스를 <see cref="Vector3"/>으로 암시적으로 변환합니다.
        /// <br/>
        /// <see cref="major"/>, <see cref="minor"/>, <see cref="patch"/>가 각각 <see cref="Vector3.x"/>, <see cref="Vector3.y"/>, <see cref="Vector3.z"/>로 매핑됩니다.
        /// <see langword="null"/>인 구성 요소는 0으로 변환됩니다.
        /// </summary>
        /// <param name="value">변환할 <see cref="Version"/> 인스턴스입니다.</param>
        /// <returns>변환된 <see cref="Vector3"/> 값입니다.</returns>
        public static implicit operator Vector3(Version value) => new Vector3(value.major ?? 0, value.minor ?? 0, value.patch ?? 0);
        
        /// <summary>
        /// <see cref="Vector3"/>를 <see cref="Version"/> 인스턴스로 명시적으로 변환합니다.
        /// <br/>
        /// <see cref="Vector3.x"/>, <see cref="Vector3.y"/>, <see cref="Vector3.z"/>가 각각 <see cref="major"/>, <see cref="minor"/>, <see cref="patch"/>로 매핑됩니다.
        /// 각 구성 요소는 <see cref="MathUtility.FloorToInt(float)"/>를 통해 정수로 변환됩니다.
        /// </summary>
        /// <param name="value">변환할 <see cref="Vector3"/> 값입니다.</param>
        /// <returns>변환된 <see cref="Version"/> 인스턴스입니다.</returns>
        public static explicit operator Version(Vector3 value) => new Version(value.x.FloorToInt(), value.y.FloorToInt(), value.z.FloorToInt());

        /// <summary>
        /// 이 <see cref="Version"/> 인스턴스와 다른 지정된 <see cref="Version"/> 인스턴스의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="other">현재 인스턴스와 비교할 <see cref="Version"/>입니다.</param>
        /// <returns>지정된 <see cref="Version"/>이 현재 인스턴스와 같은 값을 가지면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public readonly bool Equals(Version other) => this == other;
        
        /// <summary>
        /// 이 <see cref="Version"/> 인스턴스와 지정된 <see cref="VersionRange"/> 인스턴스의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="other">현재 인스턴스와 비교할 <see cref="VersionRange"/>입니다.</param>
        /// <returns>지정된 <see cref="VersionRange"/>가 현재 인스턴스와 같은 값을 가지면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public readonly bool Equals(VersionRange other) => this == other;

        /// <summary>
        /// 이 <see cref="Version"/> 인스턴스와 지정된 개체의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="obj">현재 인스턴스와 비교할 개체입니다. <see cref="Version"/> 또는 <see cref="VersionRange"/> 타입일 수 있습니다.</param>
        /// <returns>지정된 개체가 <see cref="Version"/> 또는 <see cref="VersionRange"/>이고 현재 인스턴스와 같은 값을 가지면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public override readonly bool Equals(object? obj)
        {
            return obj switch
            {
                Version range => Equals(range),
                VersionRange version => Equals(version),
                _ => false
            };
        }

        /// <summary>
        /// 이 <see cref="Version"/> 인스턴스의 해시 코드를 반환합니다.
        /// </summary>
        /// <returns>32비트 부호 있는 정수 해시 코드입니다.</returns>
        public override readonly int GetHashCode() => HashCode.Combine(major, minor, patch);

        /// <summary>
        /// 이 <see cref="Version"/> 인스턴스와 지정된 개체를 비교하고 상대 순서를 나타내는 값을 반환합니다.
        /// </summary>
        /// <param name="value">현재 인스턴스와 비교할 개체입니다.</param>
        /// <returns>
        /// 현재 인스턴스가 <paramref name="value"/>보다 작으면 음수,
        /// 현재 인스턴스가 <paramref name="value"/>와 같으면 0,
        /// 현재 인스턴스가 <paramref name="value"/>보다 크면 양수입니다.
        /// </returns>
        /// <exception cref="InvalidCastException"><paramref name="value"/>가 <see langword="null"/>이 아니고 <see cref="Version"/> 타입이 아닌 경우 발생합니다.</exception>
        public readonly int CompareTo(object? value)
        {
            return value switch
            {
                null => 1,
                Version version => CompareTo(version),
                _ => throw new InvalidCastException()
            };
        }

        /// <summary>
        /// 이 <see cref="Version"/> 인스턴스와 다른 <see cref="Version"/> 인스턴스를 비교하고 상대 순서를 나타내는 값을 반환합니다.
        /// </summary>
        /// <param name="value">현재 인스턴스와 비교할 <see cref="Version"/>입니다.</param>
        /// <returns>
        /// 현재 인스턴스가 <paramref name="value"/>보다 작으면 음수,
        /// 현재 인스턴스가 <paramref name="value"/>와 같으면 0,
        /// 현재 인스턴스가 <paramref name="value"/>보다 크면 양수입니다.
        /// </returns>
        public readonly int CompareTo(Version value)
        {
            if (this < value)
                return -1;
            else if (this > value)
                return 1;
            else
                return 0;
        }

        /// <summary>
        /// 이 <see cref="Version"/> 인스턴스의 문자열 표현을 반환합니다.
        /// <br/>
        /// 형식은 "메이저.마이너.패치"이며, <see langword="null"/>인 구성 요소는 <see cref="noneSeparator"/>('*')로 표시됩니다.
        /// <br/>
        /// 예: "1.2.3", "1.*.*", "*.*.3"
        /// </summary>
        /// <returns>이 인스턴스의 문자열 표현입니다.</returns>
        public override readonly string ToString() => $"{major ?? noneSeparator}{separator}{minor ?? noneSeparator}{separator}{patch ?? noneSeparator}";
    }
}