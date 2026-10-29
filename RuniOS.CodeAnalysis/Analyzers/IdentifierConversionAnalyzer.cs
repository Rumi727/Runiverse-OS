using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RuniOS.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Analyzers;

/// <summary>
/// Validates constant strings converted to <c>RuniOS.Resource.Identifier</c> with the same rules as the runtime parser.<br/>
/// <c>RuniOS.Resource.Identifier</c>로 변환되는 상수 문자열을 런타임 파서와 같은 규칙으로 검사합니다.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IdentifierConversionAnalyzer : DiagnosticAnalyzer
{
    const string identifierMetadataName = "RuniOS.Resource.Identifier";
    const char separator = ':';

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create
        (
            IdentifierDiagnostics.tooManySeparators,
            IdentifierDiagnostics.invalidNamespace,
            IdentifierDiagnostics.invalidPath,
            IdentifierDiagnostics.defaultNamespace
        );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static context =>
        {
            if (context.Compilation.GetTypeByMetadataName(identifierMetadataName) is not { } identifierType)
                return;

            context.RegisterOperationAction
            (
                operationContext => AnalyzeConversion(operationContext, identifierType),
                OperationKind.Conversion
            );
        });
    }

    static void AnalyzeConversion(OperationAnalysisContext context, INamedTypeSymbol identifierType)
    {
        IConversionOperation conversion = (IConversionOperation)context.Operation;
        if (!SymbolEqualityComparer.Default.Equals(conversion.OperatorMethod?.ContainingType, identifierType))
            return;

        IOperation operand = conversion.Operand;
        if (operand.Type?.SpecialType != SpecialType.System_String || operand.ConstantValue is not { HasValue: true, Value: string identifier })
            return;

        Location location = operand.Syntax.GetLocation();
        int separatorIndex = identifier.IndexOf(separator);
        string nameSpace = separatorIndex < 0 ? string.Empty : identifier.Substring(0, separatorIndex);
        string path = identifier.Substring(separatorIndex + 1);

        if (separatorIndex >= 0 && identifier.IndexOf(separator, separatorIndex + 1) >= 0)
            context.ReportDiagnostic(IdentifierDiagnostics.Create(IdentifierDiagnostics.tooManySeparators, location, identifier));
        else if (!IsNamespaceValid(nameSpace))
            context.ReportDiagnostic(IdentifierDiagnostics.Create(IdentifierDiagnostics.invalidNamespace, location, identifier));
        else if (!IsPathValid(path))
            context.ReportDiagnostic(IdentifierDiagnostics.Create(IdentifierDiagnostics.invalidPath, location, identifier));
        else if (nameSpace.Length == 0 && path.Length != 0)
            context.ReportDiagnostic(IdentifierDiagnostics.Create(IdentifierDiagnostics.defaultNamespace, location, identifier, path));
    }

    static bool IsNamespaceValid(string nameSpace)
    {
        foreach (char item in nameSpace)
        {
            if (!((item >= 'a' && item <= 'z') || (item >= '0' && item <= '9') || item == '.' || item == '-' || item == '_'))
                return false;
        }

        return true;
    }

    static bool IsPathValid(string path)
    {
        foreach (char item in path)
        {
            if (!((item >= 'a' && item <= 'z') || (item >= '0' && item <= '9') || item == '.' || item == '/' || item == '-' || item == '_'))
                return false;
        }

        return true;
    }
}
