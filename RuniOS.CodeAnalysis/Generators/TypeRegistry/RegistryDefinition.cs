using Microsoft.CodeAnalysis;
using System;

namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

/// <summary>
/// Describes a registry property discovered by a type registry source generator.<br/>
/// 타입 레지스트리 소스 생성기가 발견한 레지스트리 속성을 설명합니다.
/// </summary>
/// <param name="property">
/// The registry property represented by this definition.<br/>
/// 이 정의가 나타내는 레지스트리 속성입니다.
/// </param>
/// <param name="ownerType">
/// The type that declares <paramref name="property"/>.<br/>
/// <paramref name="property"/>를 선언한 타입입니다.
/// </param>
/// <param name="registryType">
/// The concrete registry type used by <paramref name="property"/>.<br/>
/// <paramref name="property"/>가 사용하는 구체적인 레지스트리 타입입니다.
/// </param>
/// <param name="origin">
/// The compilation source from which the definition was discovered.<br/>
/// 이 정의를 발견한 컴파일 소스입니다.
/// </param>
/// <param name="stableId">
/// The stable identifier used to derive generated names for this registry.<br/>
/// 이 레지스트리의 생성 이름을 도출하는 데 사용하는 안정 식별자입니다.
/// </param>
public sealed class RegistryDefinition(IPropertySymbol property, INamedTypeSymbol ownerType, INamedTypeSymbol registryType, RegistryOrigin origin, string stableId) : IEquatable<RegistryDefinition>
{
    /// <summary>
    /// Gets the registry property represented by this definition.<br/>
    /// 이 정의가 나타내는 레지스트리 속성을 가져옵니다.
    /// </summary>
    public IPropertySymbol property { get; } = property;

    /// <summary>
    /// Gets the type that declares <see cref="property"/>.<br/>
    /// <see cref="property"/>를 선언한 타입을 가져옵니다.
    /// </summary>
    public INamedTypeSymbol ownerType { get; } = ownerType;

    /// <summary>
    /// Gets the concrete registry type used by <see cref="property"/>.<br/>
    /// <see cref="property"/>가 사용하는 구체적인 레지스트리 타입을 가져옵니다.
    /// </summary>
    public INamedTypeSymbol registryType { get; } = registryType;

    /// <summary>
    /// Gets the source from which this registry definition was discovered.<br/>
    /// 이 레지스트리 정의를 발견한 소스를 가져옵니다.
    /// </summary>
    public RegistryOrigin origin { get; } = origin;

    /// <summary>
    /// Gets the stable identifier used to derive generated source names.<br/>
    /// 생성된 소스 이름을 도출하는 데 사용하는 안정 식별자를 가져옵니다.
    /// </summary>
    public string stableId { get; } = stableId;

    /// <summary>
    /// Determines whether this definition represents the same registry property as another definition.<br/>
    /// 이 정의가 다른 정의와 동일한 레지스트리 속성을 나타내는지 확인합니다.
    /// </summary>
    /// <param name="other">
    /// The definition to compare with this definition.<br/>
    /// 이 정의와 비교할 정의입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when both definitions refer to the same property; otherwise, <see langword="false"/>.<br/>
    /// 두 정의가 같은 속성을 참조하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    public bool Equals(RegistryDefinition? other) => other != null && SymbolEqualityComparer.Default.Equals(property, other.property);

    /// <summary>
    /// Determines whether this definition equals the specified object.<br/>
    /// 이 정의가 지정된 객체와 같은지 확인합니다.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with this definition.<br/>
    /// 이 정의와 비교할 객체입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="obj"/> is an equivalent <see cref="RegistryDefinition"/>; otherwise, <see langword="false"/>.<br/>
    /// <paramref name="obj"/>가 동일한 <see cref="RegistryDefinition"/>이면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    public override bool Equals(object? obj) => obj is RegistryDefinition other && Equals(other);

    /// <summary>
    /// Returns a hash code derived from the represented registry property.<br/>
    /// 나타내는 레지스트리 속성에서 파생된 해시 코드를 반환합니다.
    /// </summary>
    /// <returns>
    /// The hash code for the represented registry property.<br/>
    /// 나타내는 레지스트리 속성의 해시 코드입니다.
    /// </returns>
    public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(property);
}
