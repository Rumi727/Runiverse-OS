using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace RuniOS.CodeAnalysis.Analyzers;

/// <summary>
/// Adds the explicit <c>runios:</c> namespace to identifier string literals that resolve to the default namespace.<br/>
/// 기본 네임스페이스로 해석되는 식별자 문자열 리터럴에 명시적인 <c>runios:</c> 네임스페이스를 추가합니다.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(IdentifierDefaultNamespaceCodeFixProvider)), Shared]
public sealed class IdentifierDefaultNamespaceCodeFixProvider : CodeFixProvider
{
    const string prefix = "runios:";

    /// <summary>
    /// Gets the diagnostic IDs that this provider can fix.<br/>
    /// 이 제공자가 수정할 수 있는 진단 ID 컬렉션을 가져옵니다.
    /// </summary>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("ROS0039");

    /// <summary>
    /// Gets the provider that applies this fix to a whole document, project, or solution.<br/>
    /// 이 수정을 문서, 프로젝트, 솔루션 전체에 적용하는 제공자를 가져옵니다.
    /// </summary>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <summary>
    /// Registers the code fix for the reported diagnostics.<br/>
    /// 보고된 진단에 대한 코드 수정을 등록합니다.
    /// </summary>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literal)
            return;

        // 여는 따옴표 직후에 내용이 시작해야 하는 여러 줄 raw 문자열은 접두사를 삽입할 수 없습니다.
        if (literal.Token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken))
            return;

        context.RegisterCodeFix
        (
            CodeAction.Create
            (
                $"Add '{prefix}' namespace",
                cancellationToken => AddNamespaceAsync(context.Document, root, literal, cancellationToken),
                nameof(IdentifierDefaultNamespaceCodeFixProvider)
            ),
            context.Diagnostics
        );
    }

    static Task<Document> AddNamespaceAsync(Document document, SyntaxNode root, LiteralExpressionSyntax literal, CancellationToken cancellationToken)
    {
        SyntaxToken token = literal.Token;
        string text = token.Text;

        int insertIndex = text.IndexOf('"');
        if (token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken))
        {
            while (text[insertIndex] == '"')
                insertIndex++;
        }
        else
            insertIndex++;

        SyntaxToken newToken = SyntaxFactory.Token
        (
            token.LeadingTrivia,
            token.Kind(),
            text.Insert(insertIndex, prefix),
            prefix + token.ValueText,
            token.TrailingTrivia
        );

        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceToken(token, newToken)));
    }
}
