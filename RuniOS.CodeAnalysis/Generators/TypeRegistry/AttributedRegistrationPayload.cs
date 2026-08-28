using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

/// <summary>
/// Stores rendered attribute expressions for an attributed registry registration.<br/>
/// 특성 기반 레지스트리 등록에 사용할 변환된 특성 식을 저장합니다.
/// </summary>
/// <param name="attributeExpressions">
/// The C# expressions that recreate the registration attributes.<br/>
/// 등록 특성을 재생성하는 C# 식입니다.
/// </param>
sealed class AttributedRegistrationPayload(ImmutableArray<string> attributeExpressions)
{
    /// <summary>
    /// Gets the rendered attribute expressions in registration order.<br/>
    /// 등록 순서대로 변환된 특성 식을 가져옵니다.
    /// </summary>
    public ImmutableArray<string> attributeExpressions { get; } = attributeExpressions;
}
