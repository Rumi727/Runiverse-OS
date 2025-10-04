#nullable enable
using Newtonsoft.Json;
using RuniOS.IO;
using RuniOS.Json.Converters.Resource;
using System;
using UnityEngine;

namespace RuniOS.Resource
{
    /// <summary>
    /// 팩 식별자를 나타내는 구조체입니다.
    /// <br/>내부 ID (<see cref="identifier"/>) 또는 로컬 경로 (<see cref="path"/>) 중 정확히 하나만 가질 수 있습니다.
    /// <br/>두 필드가 모두 null이거나 모두 null이 아니면 유효하지 않은 상태로 간주됩니다.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(PackIdentifierConverter))]
    public struct PackIdentifier : IEquatable<PackIdentifier>, ISerializationCallbackReceiver
    {
        public static readonly PackIdentifier empty = new PackIdentifier();
        
        /// <summary>
        /// 팩의 내부 식별자입니다. 로컬 경로가 없을 때 사용됩니다.
        /// </summary>
        public Identifier? identifier
        {
            readonly get => _identifier;
            set
            {
                if (value != null)
                {
                    _identifier = value;
                    _path = null;
                }
                else
                {
                    _identifier = null;
                    if (_path == null)
                        _path = FilePath.empty;
                }
            }
        }
        [SerializeField, JsonIgnore] SerializableNullable<Identifier> _identifier;

        /// <summary>
        /// 팩의 로컬 파일 시스템 경로입니다. 내부 ID가 없을 때 사용됩니다.
        /// </summary>
        public FilePath? path
        {
            readonly get => _path;
            set
            {
                if (value != null)
                {
                    _identifier = null;
                    _path = value;
                }
                else
                {
                    if (_identifier == null)
                        _identifier = Identifier.empty;
                    _path = null;
                }
            }
        }
        [SerializeField, JsonIgnore] SerializableNullable<FilePath> _path;

        /// <summary>
        /// 이 식별자가 유효한 상태인지 여부를 나타냅니다.
        /// <br/> <see cref="identifier"/>와 <see cref="path"/> 중 정확히 하나만 값을 가질 때 유효합니다.
        /// </summary>
        public readonly bool isValid => (identifier != null && path == null) || (identifier == null && path != null);

        
        
        /// <summary>
        /// <see cref="PackIdentifier"/>의 새 인스턴스를 초기화합니다.
        /// 이 생성자는 내부에서 사용되며, <see cref="CreateByID"/> 또는 <see cref="CreateByPath"/> 메서드를 사용하여 인스턴스를 생성하는 것을 권장합니다.
        /// </summary>
        /// <param name="identifier">팩의 내부 식별자입니다.</param>
        /// <param name="path">팩의 로컬 경로입니다.</param>
        PackIdentifier(Identifier? identifier, FilePath? path)
        {
            _identifier = identifier;
            _path = path;
        }

        /// <summary>
        /// 내부 식별자를 사용하여 <see cref="PackIdentifier"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="identifier">팩의 내부 식별자입니다.</param>
        /// <returns>생성된 <see cref="PackIdentifier"/> 인스턴스입니다.</returns>
        public static PackIdentifier CreateByID(Identifier identifier) => new PackIdentifier(identifier, null);

        /// <summary>
        /// 로컬 경로를 사용하여 <see cref="PackIdentifier"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="path">팩의 로컬 경로입니다.</param>
        /// <returns>생성된 <see cref="PackIdentifier"/> 인스턴스입니다.</returns>
        public static PackIdentifier CreateByPath(FilePath path) => new PackIdentifier(null, path);



        /// <summary>
        /// 두 <see cref="PackIdentifier"/> 인스턴스가 같은지 여부를 나타냅니다.
        /// <br/>
        /// <br/>**동등성 규칙:**
        /// <list type="bullet">
        /// <item><description>두 인스턴스 모두 유효하지 않으면 (즉, <see cref="isValid"/>가 false이면) 서로 동등하다고 간주합니다 (예: "잘못된 객체끼리는 서로 동일함").</description></item>
        /// <item><description>한쪽만 유효하고 다른 쪽은 유효하지 않으면 항상 동등하지 않다고 간주합니다.</description></item>
        /// <item><description>두 인스턴스 모두 유효하면, <see cref="_identifier"/> 또는 <see cref="_path"/> 중 유효한 필드의 값이 같을 때 동등하다고 간주합니다.</description></item>
        /// </list>
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="PackIdentifier"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="PackIdentifier"/> 인스턴스입니다.</param>
        /// <returns>두 인스턴스가 동등하면 true, 그렇지 않으면 false입니다.</returns>
        public static bool operator ==(PackIdentifier lhs, PackIdentifier rhs)
        {
            // 이 식별자나 다른 식별자 중 하나라도 유효하지 않은 경우 특수 규칙 적용
            if (!lhs.isValid || !rhs.isValid)
            {
                // 둘 다 유효하지 않은 경우에만 true 반환 (잘못된 객체끼리는 동등)
                // 그렇지 않으면 (한쪽만 유효하지 않은 경우) false 반환 (유효한 객체와는 동등하지 않음)
                return !lhs.isValid && !rhs.isValid;
            }

            // 두 식별자가 모두 유효한 경우, 실제 값 비교
            // isValid 속성 덕분에 internalID와 localPath 둘 중 하나만 null이 아님을 보장합니다.
            if (lhs._identifier != null && rhs._identifier != null)
                return lhs._identifier == rhs._identifier;
            else if (lhs._path != null && rhs._path != null)
                return lhs._path == rhs._path;

            return false;
        }

        /// <summary>
        /// 두 <see cref="PackIdentifier"/> 인스턴스가 다른지 여부를 나타냅니다.
        /// <br/>
        /// <br/>**동등성 규칙:**
        /// <list type="bullet">
        /// <item><description>두 인스턴스 모두 유효하지 않으면 (즉, <see cref="isValid"/>가 false이면) 서로 동등하다고 간주합니다 (예: "잘못된 객체끼리는 서로 동일함").</description></item>
        /// <item><description>한쪽만 유효하고 다른 쪽은 유효하지 않으면 항상 동등하지 않다고 간주합니다.</description></item>
        /// <item><description>두 인스턴스 모두 유효하면, <see cref="_identifier"/> 또는 <see cref="_path"/> 중 유효한 필드의 값이 같을 때 동등하다고 간주합니다.</description></item>
        /// </list>
        /// </summary>
        /// <param name="lhs">왼쪽 <see cref="PackIdentifier"/> 인스턴스입니다.</param>
        /// <param name="rhs">오른쪽 <see cref="PackIdentifier"/> 인스턴스입니다.</param>
        /// <returns>두 인스턴스가 동등하면 true, 그렇지 않으면 false입니다.</returns>
        public static bool operator !=(PackIdentifier lhs, PackIdentifier rhs) => !(lhs == rhs);
        
        

        /// <summary>
        /// 지정된 개체가 현재 <see cref="PackIdentifier"/> 인스턴스와 같은지 여부를 나타냅니다.
        /// </summary>
        /// <param name="obj">현재 개체와 비교할 개체입니다.</param>
        /// <returns>지정된 개체가 현재 개체와 같으면 true, 그렇지 않으면 false입니다.</returns>
        public override readonly bool Equals(object? obj) => obj is PackIdentifier identifier && Equals(identifier);

        /// <summary>
        /// 이 <see cref="PackIdentifier"/> 인스턴스가 다른 <see cref="PackIdentifier"/> 인스턴스와 같은지 여부를 나타냅니다.
        /// <br/>
        /// <br/>**동등성 규칙:**
        /// <list type="bullet">
        /// <item><description>두 인스턴스 모두 유효하지 않으면 (즉, <see cref="isValid"/>가 false이면) 서로 동등하다고 간주합니다 (예: "잘못된 객체끼리는 서로 동일함").</description></item>
        /// <item><description>한쪽만 유효하고 다른 쪽은 유효하지 않으면 항상 동등하지 않다고 간주합니다.</description></item>
        /// <item><description>두 인스턴스 모두 유효하면, <see cref="_identifier"/> 또는 <see cref="_path"/> 중 유효한 필드의 값이 같을 때 동등하다고 간주합니다.</description></item>
        /// </list>
        /// </summary>
        /// <param name="other">비교할 <see cref="PackIdentifier"/> 인스턴스입니다.</param>
        /// <returns>두 인스턴스가 동등하면 true, 그렇지 않으면 false입니다.</returns>
        public readonly bool Equals(PackIdentifier other) => this == other;

        /// <summary>
        /// 이 <see cref="PackIdentifier"/> 인스턴스의 해시 코드를 반환합니다.
        /// <br/>
        /// <br/>**해시 코드 규칙:**
        /// <list type="bullet">
        /// <item><description>유효하지 않은 모든 인스턴스 (<see cref="isValid"/>가 false인 경우)는 동일한 고정된 해시 코드 값 (<see cref="int.MinValue"/>)을 반환합니다.</description></item>
        /// <item><description>유효한 인스턴스는 해당 <see cref="_identifier"/> 또는 <see cref="_path"/> 필드의 해시 코드를 반환합니다.</description></item>
        /// </list>
        /// </summary>
        /// <returns>이 인스턴스의 해시 코드입니다.</returns>
        public override readonly int GetHashCode()
        {
            // 유효하지 않은 객체는 모두 동일한 고정된 해시 값을 반환하여 Equals 계약을 준수합니다.
            // int.MinValue는 다른 일반적인 해시 값과 겹칠 가능성이 매우 낮아 충돌 위험을 줄입니다.
            if (!isValid)
                return int.MinValue;

            // 유효한 객체는 해당 식별자 필드를 기반으로 해시 코드를 반환합니다.
            // isValid 속성 덕분에 internalID와 localPath 둘 중 하나만 null이 아님을 보장합니다.
            if (_identifier != null)
                return _identifier.GetHashCode();
            else // localPath != null 인 경우
                return _path.GetHashCode();
        }
        
        /// <summary>
        /// 현재 <see cref="PackIdentifier"/> 인스턴스를 나타내는 문자열을 반환합니다.
        /// <br/>
        /// <br/><b>반환 값 규칙:</b>
        /// <list type="bullet">
        /// <item><description>인스턴스가 유효하지 않으면 (<see cref="isValid"/>가 <see langword="false"/>이면), <c>"Invalid PackIdentifier"</c> 문자열을 반환합니다.</description></item>
        /// <item><description>내부 식별자 (<see cref="identifier"/>)를 포함하는 유효한 인스턴스이면, 해당 <see cref="Identifier"/>의 <see cref="Identifier.ToString"/> 결과를 반환합니다.</description></item>
        /// <item><description>로컬 경로 (<see cref="path"/>)를 포함하는 유효한 인스턴스이면, 해당 <see cref="FilePath"/>의 <see cref="FilePath.ToString"/> 결과를 반환합니다.</description></item>
        /// </list>
        /// </summary>
        /// <returns>현재 인스턴스를 나타내는 문자열입니다.</returns>
        public override string ToString()
        {
            if (!isValid)
                return $"Invalid {nameof(PackIdentifier)}";

            if (_identifier != null)
                return _identifier.ToString();
            else
                return _path.ToString();
        }



        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (_identifier != null)
            {
                _path = null;
                return;
            }
            else if (_path != null)
            {
                _identifier = null;
                return;
            }

            _identifier = Identifier.empty;
            _path = null;
        }
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (_identifier != null)
            {
                _path = null;
                return;
            }
            else if (_path != null)
            {
                _identifier = null;
                return;
            }

            _identifier = Identifier.empty;
            _path = null;
        }
    }
}
