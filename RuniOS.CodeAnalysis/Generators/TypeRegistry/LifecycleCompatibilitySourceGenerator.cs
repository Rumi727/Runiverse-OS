using Microsoft.CodeAnalysis;

namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

/// <summary>
/// Emits lifecycle API compatibility declarations once for each analyzed compilation.<br/>
/// 분석하는 각 컴파일에 lifecycle API 호환 선언을 한 번씩 생성합니다.
/// </summary>
[Generator]
public sealed class LifecycleCompatibilitySourceGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Registers the lifecycle compatibility output for analyzed compilations.<br/>
    /// 분석하는 컴파일에 lifecycle 호환 출력을 등록합니다.
    /// </summary>
    /// <param name="context">
    /// The incremental generator initialization context receiving the output registration.<br/>
    /// 출력 등록을 받을 증분 생성기 초기화 컨텍스트입니다.
    /// </param>
    public void Initialize(IncrementalGeneratorInitializationContext context) =>
        context.RegisterSourceOutput
        (
            context.CompilationProvider,
            static (productionContext, compilation) => TypeRegistrySourceGenerator.EmitLifecycleCompatibility(productionContext, compilation)
        );
}
