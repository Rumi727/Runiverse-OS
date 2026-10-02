#nullable enable
using System;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Identifies a logical package using ordinal text equality.<br/>
    /// 문자열의 ordinal 동등성으로 논리적 패키지를 식별합니다.
    /// </summary>
    public readonly struct PackageId : IEquatable<PackageId>
    {
        readonly string? _value;
        /// <summary>
        /// Gets the identifier text; an uninitialized value is empty.<br/>
        /// 식별자 문자열을 가져오며 초기화하지 않은 값은 빈 문자열입니다.
        /// </summary>
        public string value => _value ?? string.Empty;
        /// <summary>
        /// Creates a non-empty logical identifier.<br/>
        /// 비어 있지 않은 논리적 식별자를 생성합니다.
        /// </summary>
        /// <param name="value">
        /// The identifier text.<br/>
        /// 식별자 문자열입니다.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when the text is null, empty, or whitespace.<br/>
        /// 문자열이 <see langword="null"/>, 빈 문자열 또는 공백이면 발생합니다.
        /// </exception>
        public PackageId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A package ID is required.", nameof(value));
            _value = value;
        }
        /// <inheritdoc/>
        public bool Equals(PackageId other) => StringComparer.Ordinal.Equals(value, other.value);
        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is PackageId other && Equals(other);
        /// <inheritdoc/>
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(value);
        /// <inheritdoc/>
        public override string ToString() => value;
        /// <summary>
        /// Compares identifiers for equality.<br/>
        /// 식별자의 동등성을 비교합니다.
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
        public static bool operator ==(PackageId left, PackageId right) => left.Equals(right);
        /// <summary>
        /// Compares identifiers for inequality.<br/>
        /// 식별자의 비동등성을 비교합니다.
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
        public static bool operator !=(PackageId left, PackageId right) => !left.Equals(right);
    }
}
