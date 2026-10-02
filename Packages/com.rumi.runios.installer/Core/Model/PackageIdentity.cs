#nullable enable
using System;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Identifies one exact package definition without interpreting its identity text.<br/>
    /// identity 문자열을 해석하지 않고 하나의 exact 패키지 정의를 식별합니다.
    /// </summary>
    public readonly struct PackageIdentity : IEquatable<PackageIdentity>
    {
        /// <summary>
        /// Gets the logical identifier.<br/>
        /// 논리적 식별자를 가져옵니다.
        /// </summary>
        public PackageId id { get; }
        readonly string? _exactIdentity;
        /// <summary>
        /// Gets the opaque exact identity; an uninitialized value is empty.<br/>
        /// 불투명한 exact identity를 가져오며 초기화하지 않은 값은 빈 문자열입니다.
        /// </summary>
        public string exactIdentity => _exactIdentity ?? string.Empty;
        /// <summary>
        /// Creates an exact definition key.<br/>
        /// exact 정의 키를 생성합니다.
        /// </summary>
        /// <param name="id">
        /// The non-empty logical identifier.<br/>
        /// 비어 있지 않은 논리적 식별자입니다.
        /// </param>
        /// <param name="exactIdentity">
        /// The opaque deterministic identity text.<br/>
        /// 불투명하고 결정적인 identity 문자열입니다.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when either component is empty or whitespace.<br/>
        /// 구성 요소가 비어 있거나 공백이면 발생합니다.
        /// </exception>
        public PackageIdentity(PackageId id, string exactIdentity)
        {
            if (string.IsNullOrWhiteSpace(id.value)) throw new ArgumentException("A package ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(exactIdentity)) throw new ArgumentException("An exact identity is required.", nameof(exactIdentity));
            this.id = id;
            _exactIdentity = exactIdentity;
        }
        /// <inheritdoc/>
        public bool Equals(PackageIdentity other) => id == other.id && StringComparer.Ordinal.Equals(exactIdentity, other.exactIdentity);
        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is PackageIdentity other && Equals(other);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(id, StringComparer.Ordinal.GetHashCode(exactIdentity));
        /// <inheritdoc/>
        public override string ToString() => $"{id.value} [{exactIdentity}]";
        /// <summary>
        /// Compares exact definition keys for equality.<br/>
        /// exact 정의 키의 동등성을 비교합니다.
        /// </summary>
        /// <param name="left">
        /// The first identifier.<br/>
        /// 첫 번째 식별자입니다.
        /// </param>
        /// <param name="right">
        /// The second identifier.<br/>
        /// 두 번째 식별자입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if equal; otherwise <see langword="false"/>.<br/>
        /// 같으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool operator ==(PackageIdentity left, PackageIdentity right) => left.Equals(right);
        /// <summary>
        /// Compares exact definition keys for inequality.<br/>
        /// exact 정의 키의 비동등성을 비교합니다.
        /// </summary>
        /// <param name="left">
        /// The first identifier.<br/>
        /// 첫 번째 식별자입니다.
        /// </param>
        /// <param name="right">
        /// The second identifier.<br/>
        /// 두 번째 식별자입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if different; otherwise <see langword="false"/>.<br/>
        /// 다르면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool operator !=(PackageIdentity left, PackageIdentity right) => !left.Equals(right);
    }
}
