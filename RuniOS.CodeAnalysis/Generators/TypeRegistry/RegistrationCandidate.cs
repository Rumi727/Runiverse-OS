using Microsoft.CodeAnalysis;

namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

/// <summary>
/// Represents a symbol considered for registration in a type registry.<br/>
/// 타입 레지스트리 등록 대상으로 검토되는 심볼을 나타냅니다.
/// </summary>
/// <param name="symbol">
/// The symbol represented by this candidate.<br/>
/// 이 후보가 나타내는 심볼입니다.
/// </param>
public abstract class RegistrationCandidate(ISymbol symbol)
{
    /// <summary>
    /// Gets the symbol represented by this candidate.<br/>
    /// 이 후보가 나타내는 심볼을 가져옵니다.
    /// </summary>
    public ISymbol symbol { get; } = symbol;
}
