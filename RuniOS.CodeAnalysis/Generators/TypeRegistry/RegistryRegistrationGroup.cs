using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

/// <summary>
/// Groups bound registrations that target the same registry definition.<br/>
/// 같은 레지스트리 정의를 대상으로 하는 바인딩된 등록을 그룹화합니다.
/// </summary>
/// <param name="registry">
/// The registry targeted by the registrations.<br/>
/// 등록 대상 레지스트리입니다.
/// </param>
/// <param name="registrations">
/// The bound registrations belonging to the registry.<br/>
/// 해당 레지스트리에 속한 바인딩된 등록입니다.
/// </param>
sealed class RegistryRegistrationGroup(RegistryDefinition registry, ImmutableArray<BoundRegistration> registrations)
{
    /// <summary>
    /// Gets the registry targeted by the registrations.<br/>
    /// 등록 대상 레지스트리를 가져옵니다.
    /// </summary>
    public RegistryDefinition registry { get; } = registry;

    /// <summary>
    /// Gets the bound registrations belonging to the registry.<br/>
    /// 해당 레지스트리에 속한 바인딩된 등록을 가져옵니다.
    /// </summary>
    public ImmutableArray<BoundRegistration> registrations { get; } = registrations;
}
