using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuniOS.CodeAnalysis.Collections.Immutable;
using RuniOS.CodeAnalysis.Diagnostics;
using RuniOS.CodeAnalysis.Linq;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace RuniOS.CodeAnalysis.Generators.Milestones;

[Generator]
public sealed class MilestoneGenerator : IIncrementalGenerator
{
    public static ImmutableArray<string> targetAttributes { get; } = ImmutableArray.Create("RuniOS.Milestones.OnResourcesReadyAttribute");

    readonly record struct MilestoneMethodInfo(string hintName, string containingTypeSyntax, PartialTypeDeclarations partialTypeDeclarations, string attributeName, string attributeSyntax, ImmutableEquatableArray<LocationData> attributeApplicationLocations, string methodName, SerializeErrorResults errors);

    void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<MilestoneMethodInfo> milestoneMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(targetAttributes[0], IsCandidateMethod, CreateMethodInfo)
            .WhereNotNull();

        context.RegisterSourceOutput(milestoneMethods, GenerateRegistrationMethod);

        IncrementalValueProvider<ImmutableArray<MilestoneMethodInfo>> allMilestoneMethods = milestoneMethods.Collect();
        for (int i = 1; i < targetAttributes.Length; i++)
        {
            IncrementalValuesProvider<MilestoneMethodInfo> attributeMethods = context.SyntaxProvider
                .ForAttributeWithMetadataName(targetAttributes[i], IsCandidateMethod, CreateMethodInfo)
                .WhereNotNull();

            context.RegisterSourceOutput(attributeMethods, GenerateRegistrationMethod);

            allMilestoneMethods = allMilestoneMethods
                .Combine(attributeMethods.Collect())
                .Select(static (x, _) => x.Left.AddRange(x.Right));
        }

        IncrementalValueProvider<bool> isUnityAssembly = context.CompilationProvider
            .Select(static (compilation, _) =>
                compilation.GetTypeByMetadataName("Unity.Scripting.RequiredByAssemblyAttribute") != null);

        context.RegisterSourceOutput(allMilestoneMethods.Combine(isUnityAssembly), GenerateModuleInitialization);
    }

    static bool IsCandidateMethod(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is MethodDeclarationSyntax methodNode &&
            methodNode.Modifiers.Any(SyntaxKind.StaticKeyword) &&
            methodNode.ParameterList.Parameters.Count == 0 &&
            methodNode.TypeParameterList == null &&
            methodNode.Ancestors()
                .OfType<TypeDeclarationSyntax>()
                .All(x => x.Modifiers.Any(SyntaxKind.PartialKeyword) && x.TypeParameterList == null);
    }

    static MilestoneMethodInfo? CreateMethodInfo(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        IMethodSymbol methodSymbol = (IMethodSymbol)context.TargetSymbol;
        if (!methodSymbol.ReturnsVoid && !methodSymbol.ReturnType.IsNonGenericUniTask)
            return null;

        INamedTypeSymbol containingType = methodSymbol.ContainingType;

        PartialTypeDeclarations partialTypeDeclarations = containingType.ToPartialTypeDeclarations();
        ImmutableArray<LocationData> attributeApplicationLocations = context.Attributes
                .Select(x => x.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation())
                .WhereNotNull()
                .Select(x => x.ToLocationData())
                .ToImmutableArray();

        SerializeErrorResults errors = containingType.TrySyntaxSerialize(out string containingTypeSyntax);

        INamedTypeSymbol attributeSymbol = context.Attributes[0].AttributeClass!;
        string attributeName = attributeSymbol.Name;

        errors |= attributeSymbol.TrySyntaxSerialize(out string attributeSyntax);

        return new MilestoneMethodInfo
        (
            containingType.GetHintName($"{methodSymbol.Name}.{attributeName}.RegisterMilestoneMethod"),
            containingTypeSyntax,
            partialTypeDeclarations,
            attributeName,
            attributeSyntax,
            attributeApplicationLocations,
            methodSymbol.Name,
            errors
        );
    }

    static void GenerateRegistrationMethod(SourceProductionContext context, MilestoneMethodInfo method)
    {
        SourceWriter writer = new SourceWriter();
        using (PartialTypeDeclarationsSerializer.Serialize(writer, method.partialTypeDeclarations, out SerializeErrorResults errors))
        {
            errors |= method.errors;

            if (!errors.isSuccess)
            {
                for (int i = 0; i < errors.count; i++)
                {
                    for (int j = 0; j < method.attributeApplicationLocations.length; j++)
                        context.ReportDiagnostic(SerializerDiagnostics.Create(errors[i], method.attributeApplicationLocations[j].ToLocation()));
                }

                return;
            }

            writer.AppendLineCompilerGenerated();
            writer.AppendLineEditorBrowsable(EditorBrowsableState.Never);
            writer.AppendLine($"internal static void __{method.methodName}_{method.attributeName}_RegisterMilestoneMethod");
            using (writer.Block())
                writer.AppendLine($"global::RuniOS.Milestones.MilestoneDispatcher.Register<{method.attributeSyntax}>({method.methodName})");
        }

        context.AddSource(method.hintName, writer.ToString());
    }

    static void GenerateModuleInitialization(SourceProductionContext context, (ImmutableArray<MilestoneMethodInfo> methods, bool hasRequiredByAssemblyAttribute) source)
    {
        (ImmutableArray<MilestoneMethodInfo> methods, bool hasRequiredByAssemblyAttribute) = source;
        if (!hasRequiredByAssemblyAttribute || methods.IsDefaultOrEmpty)
            return;

        SourceWriter writer = new SourceWriter();
        using (writer.Namespace("RuniOS.Milestones.Generated"))
        {
            writer.AppendLineCompilerGenerated();
            writer.AppendLine("[global::Unity.Scripting.RequiredByAssembly]");
            writer.AppendLine("internal static class __RuniModuleInitialization");
            using (writer.Block())
            {
                writer.AppendLineCompilerGenerated();
                writer.AppendLine("internal static void Initialize()");
                using (writer.Block())
                {
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MilestoneMethodInfo milestoneMethodInfo = methods[i];
                        writer.AppendLine($"{milestoneMethodInfo.containingTypeSyntax}.__{milestoneMethodInfo.methodName}_{milestoneMethodInfo.attributeName}_RegisterMilestoneMethod();");
                    }
                }
            }
        }
        context.AddSource("__RuniModuleInitialization.g.cs", writer.ToString());
    }
}
