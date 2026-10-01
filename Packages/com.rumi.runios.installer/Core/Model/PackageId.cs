#nullable enable
using System;
using System.Collections.Generic;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Model
{
    /// <summary>
    /// Identifies a logical package independently of its source.<br/>
    /// 출처와 독립적으로 논리적 패키지를 식별합니다.
    /// </summary>
    public readonly struct PackageId : IEquatable<PackageId>
    {
        readonly string? _value;
        /// <summary>
        /// Gets the identifier text; an uninitialized identifier returns an empty string.<br/>
        /// 식별자 문자열을 가져오며 초기화하지 않은 식별자는 빈 문자열을 반환합니다.
        /// </summary>
        public string value => _value ?? string.Empty;

        /// <summary>
        /// Validates non-empty logical package text.<br/>
        /// 비어 있지 않은 논리적 패키지 문자열을 검증합니다.
        /// </summary>
        /// <param name="value">
        /// The immutable value or identifier to retain.<br/>
        /// 보존할 불변 값 또는 식별자입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="value"/> is empty or whitespace.<br/>
        /// <paramref name="value"/>가 비어 있거나 공백이면 발생합니다.
        /// </exception>
        public PackageId(string value) => _value = Snapshots.Text(value, nameof(value));

        /// <inheritdoc/>
        public bool Equals(PackageId other) => StringComparer.Ordinal.Equals(value, other.value);
        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is PackageId other && Equals(other);
        /// <inheritdoc/>
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(value);
        /// <inheritdoc/>
        public override string ToString() => value;
        /// <summary>
        /// Compares identifiers using ordinal text equality.<br/>
        /// 문자열의 ordinal 동등성으로 식별자를 비교합니다.
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
        /// <see langword="true"/> when equal; otherwise, <see langword="false"/>.<br/>
        /// 같으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool operator ==(PackageId left, PackageId right) => left.Equals(right);
        /// <summary>
        /// Checks whether identifiers differ using ordinal text comparison.<br/>
        /// 문자열의 ordinal 비교로 식별자가 다른지 확인합니다.
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
        /// <see langword="true"/> when different; otherwise, <see langword="false"/>.<br/>
        /// 다르면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool operator !=(PackageId left, PackageId right) => !left.Equals(right);
    }
}
