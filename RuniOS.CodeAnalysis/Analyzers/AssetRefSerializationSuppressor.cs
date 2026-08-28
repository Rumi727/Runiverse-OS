using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RuniOS.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Analyzers;

/// <summary>
/// Suppresses the Unity analyzer diagnostic for supported <c>AssetRef&lt;TAsset&gt;</c> fields.<br/>
/// 지원되는 <c>AssetRef&lt;TAsset&gt;</c> 필드에 대한 Unity 분석기 진단을 억제합니다.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AssetRefSerializationSuppressor : DiagnosticSuppressor
{
    // OriginalDefinition 매칭을 위해 런타임 AssetRef<TAsset>의 메타데이터 이름과 generic arity를 유지해야 합니다.
    const string assetRefMetadataName = "RuniOS.Resource.AssetRef`1";

    /// <summary>
    /// Gets the suppression descriptors provided by this suppressor.<br/>
    /// 이 억제기가 제공하는 억제 설명자 컬렉션을 가져옵니다.
    /// </summary>
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(SuppressorDiagnostics.descriptor);

    /// <summary>
    /// Reports suppressions for <c>UAC1001</c> diagnostics on fields whose original type is <c>AssetRef&lt;TAsset&gt;</c>.<br/>
    /// 원본 타입이 <c>AssetRef&lt;TAsset&gt;</c>인 필드의 <c>UAC1001</c> 진단에 대한 억제를 보고합니다.
    /// </summary>
    /// <param name="context">
    /// The suppression analysis context containing the compilation and reported diagnostics.<br/>
    /// 컴파일 및 보고된 진단을 포함하는 억제 분석 컨텍스트입니다.
    /// </param>
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        INamedTypeSymbol? assetRefType = context.Compilation.GetTypeByMetadataName(assetRefMetadataName);
        if (assetRefType == null)
            return;

        foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
        {
            // Unity analyzer 계약: 이 suppressor는 의도적으로 UAC1001만 대상으로 합니다.
            if (diagnostic.Id != "UAC1001" || !diagnostic.Location.IsInSource)
                continue;

            SyntaxTree? tree = diagnostic.Location.SourceTree;
            if (tree == null)
                continue;

            SyntaxNode root = tree.GetRoot(context.CancellationToken);
            SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            FieldDeclarationSyntax? declaration = node.FirstAncestorOrSelf<FieldDeclarationSyntax>();
            if (declaration == null)
                continue;

            SemanticModel semanticModel = context.GetSemanticModel(tree);
            foreach (VariableDeclaratorSyntax variable in declaration.Declaration.Variables)
            {
                if (semanticModel.GetDeclaredSymbol(variable, context.CancellationToken) is not IFieldSymbol field)
                    continue;

                if (field.Type is not INamedTypeSymbol fieldType)
                    continue;

                if (!SymbolEqualityComparer.Default.Equals(fieldType.OriginalDefinition, assetRefType))
                    continue;

                context.ReportSuppression(Suppression.Create(SuppressorDiagnostics.descriptor, diagnostic));
                break;
            }
        }
    }
}
