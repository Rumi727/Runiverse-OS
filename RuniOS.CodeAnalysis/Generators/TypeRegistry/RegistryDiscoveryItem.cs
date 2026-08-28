using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

/// <summary>
/// Stores the result of discovering a registry property and its diagnostics.<br/>
/// 레지스트리 속성 발견 결과와 해당 진단을 저장합니다.
/// </summary>
/// <param name="definition">
/// The valid registry definition, or <see langword="null"/> when discovery failed.<br/>
/// 유효한 레지스트리 정의이며, 발견에 실패하면 <see langword="null"/>입니다.
/// </param>
/// <param name="property">
/// The discovered property, or <see langword="null"/> when the target was not a property.<br/>
/// 발견한 속성이며, 대상이 속성이 아니면 <see langword="null"/>입니다.
/// </param>
/// <param name="location">
/// The source location associated with the discovery result.<br/>
/// 발견 결과와 연결된 소스 위치입니다.
/// </param>
/// <param name="isCurrent">
/// Indicates whether the item originated in the current compilation.<br/>
/// 현재 컴파일에서 발견한 항목인지 나타냅니다.
/// </param>
/// <param name="diagnostics">
/// The diagnostics produced while discovering the item.<br/>
/// 항목을 발견하는 동안 생성된 진단입니다.
/// </param>
sealed class RegistryDiscoveryItem(RegistryDefinition? definition, IPropertySymbol? property, Location location, bool isCurrent, ImmutableArray<Diagnostic> diagnostics)
{
    /// <summary>
    /// Gets the valid registry definition, or <see langword="null"/> when discovery failed.<br/>
    /// 유효한 레지스트리 정의를 가져오며, 발견에 실패하면 <see langword="null"/>입니다.
    /// </summary>
    public RegistryDefinition? definition { get; } = definition;

    /// <summary>
    /// Gets the discovered property, or <see langword="null"/> when the target was not a property.<br/>
    /// 발견한 속성을 가져오며, 대상이 속성이 아니면 <see langword="null"/>입니다.
    /// </summary>
    public IPropertySymbol? property { get; } = property;

    /// <summary>
    /// Gets the source location associated with the discovery result.<br/>
    /// 발견 결과와 연결된 소스 위치를 가져옵니다.
    /// </summary>
    public Location location { get; } = location;

    /// <summary>
    /// Gets a value indicating whether the item originated in the current compilation.<br/>
    /// 항목이 현재 컴파일에서 발견되었는지 나타내는 값을 가져옵니다.
    /// </summary>
    public bool isCurrent { get; } = isCurrent;

    /// <summary>
    /// Gets the diagnostics produced while discovering the item.<br/>
    /// 항목을 발견하는 동안 생성된 진단을 가져옵니다.
    /// </summary>
    public ImmutableArray<Diagnostic> diagnostics { get; } = diagnostics;
}
