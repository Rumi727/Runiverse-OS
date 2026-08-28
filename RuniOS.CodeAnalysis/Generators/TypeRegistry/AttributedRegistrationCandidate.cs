using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

/// <summary>
/// Represents a type candidate together with the registration attributes inherited by that type.<br/>
/// 타입이 상속한 등록 특성과 함께 타입 후보를 나타냅니다.
/// </summary>
/// <param name="implementationType">
/// The candidate implementation type.<br/>
/// 후보 구현 타입입니다.
/// </param>
/// <param name="attributes">
/// The applicable registration attributes collected from the type hierarchy.<br/>
/// 타입 계층에서 수집한 적용 가능한 등록 특성입니다.
/// </param>
sealed class AttributedRegistrationCandidate(INamedTypeSymbol implementationType, ImmutableArray<AttributeData> attributes) : RegistrationCandidate(implementationType)
{
    /// <summary>
    /// Gets the candidate implementation type.<br/>
    /// 후보 구현 타입을 가져옵니다.
    /// </summary>
    public INamedTypeSymbol implementationType { get; } = implementationType;

    /// <summary>
    /// Gets the registration attributes applicable to the candidate.<br/>
    /// 후보에 적용할 등록 특성을 가져옵니다.
    /// </summary>
    public ImmutableArray<AttributeData> attributes { get; } = attributes;
}
