using RuniOS.CodeAnalysis.Collections.Immutable;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Generators;

/// <summary>
/// Represents a collection of errors found when serialization cannot fully preserve the original meaning.<br/>
/// 원래 의미를 직렬화 결과에 완전히 보존하지 못할 때 발견된 오류들의 집합을 나타냅니다.
/// </summary>
/// <remarks>
/// The default value represents a successful result with no errors.<br/>
/// Errors found while recursively rendering nested values are accumulated with the <c>|</c> operator.
/// <br/><br/>
/// 기본값은 오류가 없는 성공 상태를 나타냅니다.<br/>
/// 중첩 값을 재귀적으로 렌더링하면서 발견된 오류는 <c>|</c> 연산자로 누적됩니다.
/// </remarks>
public readonly struct SerializeErrorResults : IReadOnlyList<SerializeErrorResult>, IEquatable<SerializeErrorResults>
{
    /// <summary>
    /// Initializes a result containing one serialization error.<br/>
    /// 직렬화 오류 하나를 포함하는 결과를 초기화합니다.
    /// </summary>
    /// <param name="error">
    /// The kind of error that was found.<br/>
    /// 발견된 오류의 종류입니다.
    /// </param>
    /// <param name="problematicObject">
    /// The object directly associated with the error; it may be <see langword="null"/>.<br/>
    /// 오류와 직접 관련된 객체이며, <see langword="null"/>일 수 있습니다.
    /// </param>
    public SerializeErrorResults(SerializeError error, object? problematicObject) => errors = ImmutableArray.Create(new SerializeErrorResult(error, problematicObject));
    SerializeErrorResults(ImmutableArray<SerializeErrorResult> errors) => this.errors = errors;

    readonly ImmutableEquatableArray<SerializeErrorResult> errors;

    /// <summary>
    /// Gets the serialization error at the specified index.<br/>
    /// 지정한 인덱스의 직렬화 오류를 가져옵니다.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the error to retrieve.<br/>
    /// 가져올 오류의 0부터 시작하는 인덱스입니다.
    /// </param>
    /// <returns>
    /// The serialization error at <paramref name="index"/>.<br/>
    /// <paramref name="index"/>에 해당하는 직렬화 오류입니다.
    /// </returns>
    public SerializeErrorResult this[int index] => errors[index];

    /// <summary>
    /// Gets the number of serialization errors in this result.<br/>
    /// 이 결과에 포함된 직렬화 오류의 개수를 가져옵니다.
    /// </summary>
    public int count => errors.length;
    int IReadOnlyCollection<SerializeErrorResult>.Count => count;

    /// <summary>
    /// Gets a value indicating whether this result contains no serialization errors.<br/>
    /// 이 결과에 포함된 직렬화 오류가 없는지 여부를 나타내는 값을 가져옵니다.
    /// </summary>
    public bool isSuccess => count == 0;

    /// <summary>
    /// Returns an enumerator that iterates through the serialization errors.<br/>
    /// 직렬화 오류를 순회하는 열거자를 반환합니다.
    /// </summary>
    /// <returns>
    /// An enumerator for the serialization errors.<br/>
    /// 직렬화 오류를 위한 열거자입니다.
    /// </returns>
    public ImmutableArray<SerializeErrorResult>.Enumerator GetEnumerator() => errors.GetEnumerator();
    IEnumerator<SerializeErrorResult> IEnumerable<SerializeErrorResult>.GetEnumerator() => ((IEnumerable<SerializeErrorResult>)errors).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)errors).GetEnumerator();

    /// <summary>
    /// Determines whether this result has the same errors in the same order as another result.<br/>
    /// 이 결과가 다른 결과와 같은 순서로 동일한 오류를 포함하는지 여부를 확인합니다.
    /// </summary>
    /// <param name="other">
    /// The result to compare with this result.<br/>
    /// 이 결과와 비교할 결과입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when both results contain equal elements in the same order; otherwise, <see langword="false"/>.<br/>
    /// 두 결과가 같은 순서로 동등한 요소를 포함하면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.
    /// </returns>
    public bool Equals(SerializeErrorResults other) => errors.Equals(other.errors);

    public override bool Equals(object? obj) => obj is SerializeErrorResults other && Equals(other);

    public override int GetHashCode() => errors.GetHashCode();

    /// <summary>
    /// Combines the errors from two serialization results.<br/>
    /// 두 직렬화 결과의 오류를 결합합니다.
    /// </summary>
    /// <param name="lhs">
    /// The left-hand result whose errors are placed first.<br/>
    /// 오류가 먼저 배치되는 왼쪽 결과입니다.
    /// </param>
    /// <param name="rhs">
    /// The right-hand result whose errors are appended after <paramref name="lhs"/>.<br/>
    /// <paramref name="lhs"/> 뒤에 오류가 추가되는 오른쪽 결과입니다.
    /// </param>
    /// <returns>
    /// The combined result; when either operand is successful, returns the other operand.<br/>
    /// 결합된 결과이며, 두 피연산자 중 하나가 성공 상태이면 다른 피연산자를 반환합니다.
    /// </returns>
    public static SerializeErrorResults operator |(SerializeErrorResults lhs, SerializeErrorResults rhs)
    {
        if (lhs.isSuccess)
            return rhs;

        if (rhs.isSuccess)
            return lhs;

        ImmutableArray<SerializeErrorResult> combined = lhs.errors;
        return new SerializeErrorResults(combined.AddRange(rhs.errors));
    }

    public static bool operator ==(SerializeErrorResults left, SerializeErrorResults right) => left.Equals(right);
    public static bool operator !=(SerializeErrorResults left, SerializeErrorResults right) => !left.Equals(right);
}