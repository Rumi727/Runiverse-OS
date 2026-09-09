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
            MilestoneDiagnostics.invalidReturnType
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
                symbolContext => AnalyzeMethod(symbolContext, resolvedMilestoneAttributes),
                SymbolKind.Method
            );
        });
    }

    static void AnalyzeMethod(SymbolAnalysisContext context, ImmutableArray<INamedTypeSymbol> milestoneAttributes)
    {
        if (context.Symbol is not IMethodSymbol method || !TryGetMilestoneAttribute(method, milestoneAttributes, out AttributeData milestoneAttribute))
            return;

        CancellationToken cancellationToken = context.CancellationToken;
        MethodDeclarationSyntax? declaration = GetMethodDeclaration(method, cancellationToken);
        Location fallbackLocation = GetFallbackLocation(method, milestoneAttribute, declaration, cancellationToken);

        if (!method.IsStatic)
            Report(context, MilestoneDiagnostics.methodMustBeStatic, GetMethodIdentifierLocation(declaration, fallbackLocation));

        if (method.Parameters.Length != 0)
            Report(context, MilestoneDiagnostics.methodMustBeParameterless, GetParameterListLocation(declaration, fallbackLocation));

        if (method.TypeParameters.Length != 0)
            Report(context, MilestoneDiagnostics.methodMustNotBeGeneric, GetTypeParameterListLocation(declaration, fallbackLocation));

        if (!method.ReturnsVoid && !method.ReturnType.IsNonGenericUniTask)
            Report(context, MilestoneDiagnostics.invalidReturnType, GetReturnTypeLocation(declaration, fallbackLocation));

        for (INamedTypeSymbol? containingType = method.ContainingType; containingType != null; containingType = containingType.ContainingType)
        {
            TypeDeclarationSyntax? nonPartialDeclaration = FindNonPartialDeclaration(containingType, cancellationToken);
            if (nonPartialDeclaration != null || !HasSourceDeclaration(containingType, cancellationToken))
            {
                Location location = nonPartialDeclaration?.Identifier.GetLocation() ?? GetContainingTypeLocation(containingType, fallbackLocation, cancellationToken);
                Report(context, MilestoneDiagnostics.containingTypeMustBePartial, location);
            }

            if (containingType.TypeParameters.Length != 0)
            {
                TypeDeclarationSyntax? typeDeclaration = GetTypeDeclaration(containingType, cancellationToken);
                Location location = typeDeclaration?.TypeParameterList?.GetLocation()
                    ?? typeDeclaration?.Identifier.GetLocation()
                    ?? GetContainingTypeLocation(containingType, fallbackLocation, cancellationToken);
                Report(context, MilestoneDiagnostics.containingTypeMustNotBeGeneric, location);
            }
        }
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

    static MethodDeclarationSyntax? GetMethodDeclaration(IMethodSymbol method, CancellationToken cancellationToken)
    {
        foreach (SyntaxReference reference in method.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax(cancellationToken) is MethodDeclarationSyntax declaration)
                return declaration;
        }

        return null;
    }

    static TypeDeclarationSyntax? GetTypeDeclaration(INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        foreach (SyntaxReference reference in type.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax(cancellationToken) is TypeDeclarationSyntax declaration)
                return declaration;
        }

        return null;
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

    static Location GetFallbackLocation
    (
        IMethodSymbol method,
        AttributeData milestoneAttribute,
        MethodDeclarationSyntax? declaration,
        CancellationToken cancellationToken
    )
    {
        if (milestoneAttribute.ApplicationSyntaxReference is { } attributeReference)
            return attributeReference.GetSyntax(cancellationToken).GetLocation();

        if (declaration != null)
            return declaration.Identifier.GetLocation();

        foreach (Location location in method.Locations)
        {
            if (location.IsInSource)
                return location;
        }

        return Location.None;
    }

    static Location GetMethodIdentifierLocation(MethodDeclarationSyntax? declaration, Location fallbackLocation) =>
        declaration?.Identifier.GetLocation() ?? fallbackLocation;

    static Location GetParameterListLocation(MethodDeclarationSyntax? declaration, Location fallbackLocation) =>
        declaration?.ParameterList.GetLocation() ?? fallbackLocation;

    static Location GetTypeParameterListLocation(MethodDeclarationSyntax? declaration, Location fallbackLocation) =>
        declaration?.TypeParameterList?.GetLocation() ?? fallbackLocation;

    static Location GetReturnTypeLocation(MethodDeclarationSyntax? declaration, Location fallbackLocation) =>
        declaration?.ReturnType.GetLocation() ?? fallbackLocation;

    static Location GetContainingTypeLocation
    (
        INamedTypeSymbol type,
        Location fallbackLocation,
        CancellationToken cancellationToken
    ) => GetTypeDeclaration(type, cancellationToken)?.Identifier.GetLocation() ?? fallbackLocation;

    static void Report(SymbolAnalysisContext context, DiagnosticDescriptor descriptor, Location location) =>
        context.ReportDiagnostic(MilestoneDiagnostics.Create(descriptor, location));
}
