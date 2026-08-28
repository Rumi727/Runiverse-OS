using Microsoft.CodeAnalysis;

namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

/// <summary>
/// Associates a registry with an implementation type and generator-specific registration data.<br/>
/// 레지스트리를 구현 타입 및 생성기별 등록 데이터와 연결합니다.
/// </summary>
/// <param name="registry">
/// The registry that receives the implementation type.<br/>
/// 구현 타입을 등록할 레지스트리입니다.
/// </param>
/// <param name="implementationType">
/// The implementation type selected for registration.<br/>
/// 등록 대상으로 선택된 구현 타입입니다.
/// </param>
/// <param name="payload">
/// Optional data used by a specialized generator when emitting registration statements.<br/>
/// 특수화된 생성기가 등록 문을 내보낼 때 사용하는 선택적 데이터입니다.
/// </param>
public sealed class BoundRegistration(RegistryDefinition registry, INamedTypeSymbol implementationType, object? payload)
{
    /// <summary>
    /// Gets the registry associated with this registration.<br/>
    /// 이 등록과 연결된 레지스트리를 가져옵니다.
    /// </summary>
    public RegistryDefinition registry { get; } = registry;

    /// <summary>
    /// Gets the implementation type to register.<br/>
    /// 등록할 구현 타입을 가져옵니다.
    /// </summary>
    public INamedTypeSymbol implementationType { get; } = implementationType;

    /// <summary>
    /// Gets generator-specific registration data, or <see langword="null"/> when no payload is required.<br/>
    /// 생성기별 등록 데이터를 가져오며, 추가 데이터가 필요하지 않으면 <see langword="null"/>입니다.
    /// </summary>
    public object? payload { get; } = payload;
}
