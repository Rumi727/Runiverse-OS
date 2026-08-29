using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RuniOS.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Analyzers;

/// <summary>
/// Warns when the source-generator-owned <c>TypeRegistryManifestAttribute</c> is used directly.<br/>
/// 소스 생성기가 소유하는 <c>TypeRegistryManifestAttribute</c>를 직접 사용하는 경우 경고합니다.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeRegistryManifestAttributeAnalyzer : DiagnosticAnalyzer
{
    const string typeRegistryManifestAttributeMetadataName = "RuniOS.Reflection.TypeRegistryManifestAttribute";

    /// <summary>
    /// Gets the diagnostics reported by this analyzer.<br/>
    /// 이 분석기가 보고하는 진단 컬렉션을 가져옵니다.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(TypeRegistryDiagnostics.manualManifestAttribute);

    /// <summary>
    /// Registers analysis for direct manifest attribute usage.<br/>
    /// 매니페스트 특성의 직접 사용을 검사하도록 분석을 등록합니다.
    /// </summary>
    /// <param name="context">
    /// The analyzer initialization context.<br/>
    /// 분석기 초기화 컨텍스트입니다.
    /// </param>
    public override void Initialize(AnalysisContext context)
    {
        // Generated manifest source is an implementation detail of the source generator and must not produce ROS0018.
        // 생성된 매니페스트 소스는 소스 생성기 구현 세부 사항이므로 ROS0018을 보고하지 않습니다.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static context =>
        {
            INamedTypeSymbol? manifestAttribute = context.Compilation.GetTypeByMetadataName(typeRegistryManifestAttributeMetadataName);
            if (manifestAttribute == null)
                return;

            context.RegisterSyntaxNodeAction
            (
                nodeContext => AnalyzeAttribute(nodeContext, manifestAttribute),
                SyntaxKind.Attribute
            );
        });
    }

    static void AnalyzeAttribute(SyntaxNodeAnalysisContext context, INamedTypeSymbol manifestAttribute)
    {
        if (context.Node is not AttributeSyntax attribute)
            return;

        if (context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken).Symbol is not IMethodSymbol constructor)
            return;

        if (!SymbolEqualityComparer.Default.Equals(constructor.ContainingType, manifestAttribute))
            return;

        context.ReportDiagnostic
        (
            TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.manualManifestAttribute,
                attribute.Name.GetLocation(),
                context.SemanticModel.Compilation
            )
        );
    }
}
