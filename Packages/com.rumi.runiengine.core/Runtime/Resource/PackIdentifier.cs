#nullable enable
using RuniEngine.IO;
using System;

namespace RuniEngine.Resource
{
    /// <summary>
    /// 팩 식별자를 나타내는 구조체입니다.
    /// <br/>내부 ID (<see cref="internalID"/>) 또는 로컬 경로 (<see cref="localPath"/>) 중 정확히 하나만 가질 수 있습니다.
    /// <br/>두 필드가 모두 null이거나 모두 null이 아니면 유효하지 않은 상태로 간주됩니다.
    /// </summary>
    public readonly struct PackIdentifier : IEquatable<PackIdentifier>
    {
        /// <summary>
        /// 팩의 내부 식별자입니다. 로컬 경로가 없을 때 사용됩니다.
        /// </summary>
        public readonly string? internalID;

        /// <summary>
        /// 팩의 로컬 파일 시스템 경로입니다. 내부 ID가 없을 때 사용됩니다.
        /// </summary>
        public readonly FilePath? localPath;

        /// <summary>
        /// 이 식별자가 유효한 상태인지 여부를 나타냅니다.
        /// <br/> <see cref="internalID"/>와 <see cref="localPath"/> 중 정확히 하나만 값을 가질 때 유효합니다.
        /// </summary>
        public bool isValid => (internalID != null && localPath == null) || (internalID == null && localPath != null);

        /// <summary>
        /// <see cref="PackIdentifier"/>의 새 인스턴스를 초기화합니다.
        /// 이 생성자는 내부에서 사용되며, <see cref="Create(string)"/> 또는 <see cref="Create(FilePath)"/> 팩토리 메서드를 사용하여 인스턴스를 생성하는 것을 권장합니다.
        /// </summary>
        /// <param name="internalID">팩의 내부 식별자입니다.</param>
        /// <param name="localPath">팩의 로컬 경로입니다.</param>
        PackIdentifier(string? internalID, FilePath? localPath)
        {
            this.internalID = internalID;
            this.localPath = localPath;
        }

        /// <summary>
        /// 내부 식별자를 사용하여 <see cref="PackIdentifier"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="internalID">팩의 내부 식별자입니다.</param>
        /// <returns>생성된 <see cref="PackIdentifier"/> 인스턴스입니다.</returns>
        public static PackIdentifier Create(string internalID) => new PackIdentifier(internalID, null);

        /// <summary>
        /// 로컬 경로를 사용하여 <see cref="PackIdentifier"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="localPath">팩의 로컬 경로입니다.</param>
        /// <returns>생성된 <see cref="PackIdentifier"/> 인스턴스입니다.</returns>
        public static PackIdentifier Create(FilePath localPath) => new PackIdentifier(null, localPath);

        /// <summary>
        /// 다른 <see cref="PackIdentifier"/> 인스턴스와의 동등성을 확인합니다.
        /// </summary>
        /// <param name="other">비교할 <see cref="PackIdentifier"/> 인스턴스입니다.</param>
        /// <returns>두 인스턴스가 동등하면 true, 그렇지 않으면 false입니다.</returns>
        readonly bool IEquatable<PackIdentifier>.Equals(PackIdentifier other) => Equals(other);

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
        /// <item><description>두 인스턴스 모두 유효하면, <see cref="internalID"/> 또는 <see cref="localPath"/> 중 유효한 필드의 값이 같을 때 동등하다고 간주합니다.</description></item>
        /// </list>
        /// </summary>
        /// <param name="other">비교할 <see cref="PackIdentifier"/> 인스턴스입니다.</param>
        /// <returns>두 인스턴스가 동등하면 true, 그렇지 않으면 false입니다.</returns>
        public readonly bool Equals(in PackIdentifier other)
        {
            // 이 식별자나 다른 식별자 중 하나라도 유효하지 않은 경우 특수 규칙 적용
            if (!isValid || !other.isValid)
            {
                // 둘 다 유효하지 않은 경우에만 true 반환 (잘못된 객체끼리는 동등)
                // 그렇지 않으면 (한쪽만 유효하지 않은 경우) false 반환 (유효한 객체와는 동등하지 않음)
                return !isValid && !other.isValid;
            }

            // 두 식별자가 모두 유효한 경우, 실제 값 비교
            // isValid 속성 덕분에 internalID와 localPath 둘 중 하나만 null이 아님을 보장합니다.
            if (internalID != null)
                return internalID == other.internalID;
            else // localPath != null 인 경우
                return localPath == other.localPath;
        }

        /// <summary>
        /// 이 <see cref="PackIdentifier"/> 인스턴스의 해시 코드를 반환합니다.
        /// <br/>
        /// <br/>**해시 코드 규칙:**
        /// <list type="bullet">
        /// <item><description>유효하지 않은 모든 인스턴스 (<see cref="isValid"/>가 false인 경우)는 동일한 고정된 해시 코드 값 (<see cref="int.MinValue"/>)을 반환합니다.</description></item>
        /// <item><description>유효한 인스턴스는 해당 <see cref="internalID"/> 또는 <see cref="localPath"/> 필드의 해시 코드를 반환합니다.</description></item>
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
            if (internalID != null)
                return internalID.GetHashCode();
            else // localPath != null 인 경우
                return localPath.GetHashCode();
        }

        /// <summary>
        /// 두 <see cref="PackIdentifier"/> 인스턴스가 같은지 여부를 확인합니다.
        /// </summary>
        /// <param name="left">왼쪽 <see cref="PackIdentifier"/> 인스턴스입니다.</param>
        /// <param name="right">오른쪽 <see cref="PackIdentifier"/> 인스턴스입니다.</param>
        /// <returns>두 인스턴스가 같으면 true, 그렇지 않으면 false입니다.</returns>
        public static bool operator ==(PackIdentifier left, PackIdentifier right) => left.Equals(right);

        /// <summary>
        /// 두 <see cref="PackIdentifier"/> 인스턴스가 다른지 여부를 확인합니다.
        /// </summary>
        /// <param name="left">왼쪽 <see cref="PackIdentifier"/> 인스턴스입니다.</param>
        /// <param name="right">오른쪽 <see cref="PackIdentifier"/> 인스턴스입니다.</param>
        /// <returns>두 인스턴스가 다르면 true, 그렇지 않으면 false입니다.</returns>
        public static bool operator !=(PackIdentifier left, PackIdentifier right) => !(left == right);
    }
}
