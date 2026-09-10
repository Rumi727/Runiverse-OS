using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RuniOS.CodeAnalysis.Diagnostics;
using RuniOS.CodeAnalysis.Generators;
using System.Collections.Immutable;
using System.Threading;

namespace RuniOS.CodeAnalysis.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MilestoneMethodAnalyzer : DiagnosticAnalyzer
{
    const string milestoneAttributeMetadataName = "RuniOS.Milestones.OnResourcesReadyAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create
        (
            MilestoneDiagnostics.methodMustBeStatic,
            MilestoneDiagnostics.methodMustBeParameterless,
            MilestoneDiagnostics.methodMustNotBeGeneric,
            MilestoneDiagnostics.containingTypeMustBePartial,
            MilestoneDiagnostics.containingTypeMustNotBeGeneric,
            MilestoneDiagnostics.invalidReturnType,
            MilestoneDiagnostics.containingTypeMustBePublicOrInternal
        );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static context =>
        {
            ImmutableArray<INamedTypeSymbol>.Builder milestoneAttributes = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
            if (context.Compilation.GetTypeByMetadataName(milestoneAttributeMetadataName) is { } milestoneAttribute)
                milestoneAttributes.Add(milestoneAttribute);

            ImmutableArray<INamedTypeSymbol> resolvedMilestoneAttributes = milestoneAttributes.ToImmutable();
            if (resolvedMilestoneAttributes.IsDefaultOrEmpty)
                return;

            context.RegisterSymbolAction
            (
                symbolContext => AnalyzeType(symbolContext, resolvedMilestoneAttributes),
                SymbolKind.NamedType
            );
        });
    }

    static void AnalyzeType(SymbolAnalysisContext context, ImmutableArray<INamedTypeSymbol> milestoneAttributes)
    {
        if (context.Symbol is not INamedTypeSymbol type)
            return;

        bool containsMilestoneMethod = false;
        Location milestoneLocation = Location.None;
        foreach (ISymbol member in type.GetMembers())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (member is IMethodSymbol method && TryGetMilestoneAttribute(method, milestoneAttributes, out AttributeData milestoneAttribute))
            {
                containsMilestoneMethod = true;
                Location methodMilestoneLocation = GetMilestoneAttributeLocation(method, milestoneAttribute, context.CancellationToken);
                milestoneLocation = methodMilestoneLocation;
                AnalyzeMethod(context, method, methodMilestoneLocation);
            }
            else if (member is INamedTypeSymbol nestedType && TryGetMilestoneMethodLocation(nestedType, milestoneAttributes, context.CancellationToken, out Location nestedMilestoneLocation))
            {
                containsMilestoneMethod = true;
                milestoneLocation = nestedMilestoneLocation;
            }
        }

        if (!containsMilestoneMethod)
            return;

        AnalyzeContainingType(context, type, milestoneLocation);
    }

    static void AnalyzeMethod(SymbolAnalysisContext context, IMethodSymbol method, Location milestoneLocation)
    {
        if (!method.IsStatic)
            Report(context, MilestoneDiagnostics.methodMustBeStatic, milestoneLocation);

        if (method.Parameters.Length != 0)
            Report(context, MilestoneDiagnostics.methodMustBeParameterless, milestoneLocation);

        if (method.TypeParameters.Length != 0)
            Report(context, MilestoneDiagnostics.methodMustNotBeGeneric, milestoneLocation);

        if (!method.ReturnsVoid && !method.ReturnType.IsNonGenericUniTask)
            Report(context, MilestoneDiagnostics.invalidReturnType, milestoneLocation);
    }

    static void AnalyzeContainingType(SymbolAnalysisContext context, INamedTypeSymbol type, Location milestoneLocation)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        TypeDeclarationSyntax? nonPartialDeclaration = FindNonPartialDeclaration(type, cancellationToken);
        if (nonPartialDeclaration != null || !HasSourceDeclaration(type, cancellationToken))
            Report(context, MilestoneDiagnostics.containingTypeMustBePartial, milestoneLocation);

        if (type.TypeParameters.Length != 0)
            Report(context, MilestoneDiagnostics.containingTypeMustNotBeGeneric, milestoneLocation);

        if (type.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            Report(context, MilestoneDiagnostics.containingTypeMustBePublicOrInternal, milestoneLocation);
    }

    static bool TryGetMilestoneMethodLocation
    (
        INamedTypeSymbol type,
        ImmutableArray<INamedTypeSymbol> milestoneAttributes,
        CancellationToken cancellationToken,
        out Location milestoneLocation
    )
    {
        foreach (ISymbol member in type.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is IMethodSymbol method && TryGetMilestoneAttribute(method, milestoneAttributes, out AttributeData milestoneAttribute))
            {
                milestoneLocation = GetMilestoneAttributeLocation(method, milestoneAttribute, cancellationToken);
                return true;
            }

            if (member is INamedTypeSymbol nestedType && TryGetMilestoneMethodLocation(nestedType, milestoneAttributes, cancellationToken, out milestoneLocation))
                return true;
        }

        milestoneLocation = Location.None;
        return false;
    }

    static bool TryGetMilestoneAttribute
    (
        IMethodSymbol method,
        ImmutableArray<INamedTypeSymbol> milestoneAttributes,
        out AttributeData milestoneAttribute
    )
    {
        foreach (AttributeData attribute in method.GetAttributes())
        {
            if (attribute.AttributeClass is not { } attributeClass)
                continue;

            for (int i = 0; i < milestoneAttributes.Length; i++)
            {
                if (!SymbolEqualityComparer.Default.Equals(attributeClass, milestoneAttributes[i]))
                    continue;

                milestoneAttribute = attribute;
                return true;
            }
        }

        milestoneAttribute = null!;
        return false;
    }

    static TypeDeclarationSyntax? FindNonPartialDeclaration(INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        foreach (SyntaxReference reference in type.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax declaration)
                continue;

            bool isPartial = false;
            foreach (SyntaxToken modifier in declaration.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.PartialKeyword))
                {
                    isPartial = true;
                    break;
                }
            }

            if (!isPartial)
                return declaration;
        }

        return null;
    }

    static bool HasSourceDeclaration(INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        foreach (SyntaxReference reference in type.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax(cancellationToken) is TypeDeclarationSyntax)
                return true;
        }

        return false;
    }

    static Location GetMilestoneAttributeLocation
    (
        IMethodSymbol method,
        AttributeData milestoneAttribute,
        CancellationToken cancellationToken
    )
    {
        if (milestoneAttribute.ApplicationSyntaxReference is { } attributeReference)
            return attributeReference.GetSyntax(cancellationToken).GetLocation();

        foreach (Location location in method.Locations)
        {
            if (location.IsInSource)
                return location;
        }

        return Location.None;
    }

    static void Report(SymbolAnalysisContext context, DiagnosticDescriptor descriptor, Location location) =>
        context.ReportDiagnostic(MilestoneDiagnostics.Create(descriptor, location));
}
