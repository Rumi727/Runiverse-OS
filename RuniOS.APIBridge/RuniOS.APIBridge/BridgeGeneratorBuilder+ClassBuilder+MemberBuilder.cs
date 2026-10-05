using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;

namespace RuniOS.APIBridge
{
    public partial class BridgeGeneratorBuilder
    {
        partial struct ClassBuilder
        {
            public readonly struct MemberBuilder(BridgeGeneratorBuilder builder, INamedTypeSymbol targetSymbol, MemberBuilder.Target target = MemberBuilder.Target.All)
            {
                public enum Target
                {
                    All = 0,
                    InstanceMember = 1,
                    StaticMember = 2
                }
                
                public static void Build(BridgeGeneratorBuilder builder, INamedTypeSymbol targetSymbol, Target target = Target.All) => new MemberBuilder(builder, targetSymbol, target).Build();

                readonly BridgeGeneratorBuilder builder = builder;
                readonly string bridgeNamespace = builder.bridgeNamespace;
                
                readonly string targetTypeName = targetSymbol.GetFullTypeName(builder.bridgeNamespace);
                readonly bool targetIsNonPublic = targetSymbol.IsNonPublicMember();

                readonly Target target = target;

                void Append(string text = "") => builder.Append(text);

                void AppendLine(string text = "") => builder.AppendLine(text);

                void StartBlock() => builder.StartBlock();

                void EndBlock() => builder.EndBlock();

                void StartComment() => builder.StartComment();

                void EndComment() => builder.EndComment();

                void AppendLineDocumentation(ISymbol symbol) => builder.AppendLineDocumentation(symbol);

                public void Build()
                {
                    BridgeGeneratorBuilder? builder = this.builder;
                    string bridgeNamespace = this.bridgeNamespace;
                    bool targetIsNonPublic = this.targetIsNonPublic;
                    bool forceStatic = builder.forceStatic || target == Target.StaticMember;
                    bool forceInstance = target == Target.InstanceMember;
                    var members = targetSymbol.GetMembers()
                        .Where(static x => x is IFieldSymbol or IPropertySymbol or IEventSymbol or IMethodSymbol)
                        .Where(static x => !x.IsImplicitlyDeclared)
                        .Where(x => builder.includePublicMember || x.IsNonPublicMember())
                        .Where(static x => !x.IsInternalCall())
                        .Where(static x => !x.IsExplicitInterfaceImplementations())
                        .Where(static x => !x.IsCompilerGenerated())
                        .Where(x => !forceStatic || (x.IsStatic && forceStatic))
                        .Where(x => !forceInstance || (!x.IsStatic && forceInstance))
                        .Where(x => !builder.includeMembers.Any() || builder.includeMembers.Contains(x.Name))
                        .Where(x => !builder.excludeMembers.Contains(x.Name));
                    if (members.Any())
                    {
                        AppendLine();
                        AppendLine();
                        AppendLine();
                        
                        AppendLine("#nullable disable");
                    }
                    foreach (var member in members)
                    {
                        AppendLine();
                        
                        bool isStaticMember = member.IsStatic;
                        string memberName = member.GetEscapeName();

                        string? instanceAccessPrefix;
                        if (isStaticMember)
                            instanceAccessPrefix = targetTypeName; // 정적 멤버는 직접 접근
                        else // __instance가 object 타입일 경우 캐스팅을 추가합니다.
                            instanceAccessPrefix = targetIsNonPublic ? $"(({targetTypeName})__instance)" : "__instance";
                        
                        if (member.IsObsolete(out string message))
                            AppendLine($"[global::System.ObsoleteAttribute(\"{message}\")]");

                        switch (member)
                        {
                            case IFieldSymbol field:
                            {
                                string fieldTypeName = field.Type.GetTypeNameOrBridgeName(bridgeNamespace);
                                bool fieldTypeIsNonPublic = false;
                                bool fieldTypeIsDelegate = false;
                                INamedTypeSymbol? namedTypeSymbol = field.Type.GetNamedTypeSymbol();
                                if (namedTypeSymbol != null)
                                {
                                    if (namedTypeSymbol.TypeKind == TypeKind.Delegate)
                                        fieldTypeIsDelegate = true;

                                    if (namedTypeSymbol.IsNonPublicMember())
                                    {
                                        fieldTypeIsNonPublic = true;
                                        if (builder.onlyByMyself && !SymbolEqualityComparer.Default.Equals(targetSymbol, namedTypeSymbol))
                                            break;
                                        
                                        builder.nonPublicTypeSymbols.Add(new BridgeGenerationData(bridgeNamespace, builder.targetAssemblies, namedTypeSymbol.OriginalDefinition, ImmutableArray<string>.Empty, ImmutableArray<string>.Empty, false, false, ImmutableHashSet<int>.Empty, false, builder.onlyByMyself));
                                    }
                                }

                                if (fieldTypeIsDelegate && fieldTypeIsNonPublic)
                                    StartComment();

                                Append("public ");
                                if (field.IsConst)
                                {
                                    if (field.Type.TypeKind == TypeKind.Enum)
                                        AppendLine($"const {fieldTypeName} {memberName} = ({fieldTypeName})({namedTypeSymbol?.EnumUnderlyingType?.GetFullTypeName(bridgeNamespace)}){targetTypeName}.{memberName};");
                                    else
                                        AppendLine($"const {fieldTypeName} {memberName} = {targetTypeName}.{memberName};");
                                }
                                else
                                {
                                    if (isStaticMember)
                                        Append("static ");
                                    if (field.IsUnsafe())
                                        Append("unsafe ");
                                    Append($"{fieldTypeName} {memberName}");

                                    if (field.IsReadOnly)
                                        AppendLine($" => {field.Type.ValueAccessToBridgeAccess(bridgeNamespace, $"{instanceAccessPrefix}.{memberName}")};");
                                    else
                                    {
                                        AppendLine();
                                        StartBlock();
                                        {
                                            AppendLine($"get => {field.Type.ValueAccessToBridgeAccess(bridgeNamespace, $"{instanceAccessPrefix}.{memberName}")};");
                                            if (targetSymbol.IsValueType)
                                            {
                                                AppendLine("set");
                                                StartBlock();
                                                {
                                                    AppendLine($"{targetTypeName} instance = {instanceAccessPrefix};");
                                                    AppendLine($"instance.{memberName} = {field.Type.BridgeAccessToValueAccess(bridgeNamespace, "value")};");
                                                    AppendLine("__instance = instance;");
                                                }
                                                EndBlock();
                                            }
                                            else
                                                AppendLine($"set => {instanceAccessPrefix}.{memberName} = {field.Type.BridgeAccessToValueAccess(bridgeNamespace, "value")};");
                                        }
                                        EndBlock();
                                    }
                                }
                                
                                if (fieldTypeIsDelegate && fieldTypeIsNonPublic)
                                    EndComment();

                                break;
                            }
                            case IPropertySymbol property:
                            {
                                // 인덱서는 아직 구현되지 않음.
                                if (property.IsIndexer)
                                    break;
                                
                                string propertyTypeName = property.Type.GetTypeNameOrBridgeName(bridgeNamespace);
                                bool propertyTypeIsNonPublic = false;
                                bool propertyTypeIsDelegate = false;
                                INamedTypeSymbol? namedTypeSymbol = property.Type.GetNamedTypeSymbol();
                                if (namedTypeSymbol != null)
                                {
                                    if (namedTypeSymbol.TypeKind == TypeKind.Delegate)
                                        propertyTypeIsDelegate = true;

                                    if (namedTypeSymbol.IsNonPublicMember())
                                    {
                                        propertyTypeIsNonPublic = true;
                                        if (builder.onlyByMyself && !SymbolEqualityComparer.Default.Equals(targetSymbol, namedTypeSymbol))
                                            break;
                                        
                                        builder.nonPublicTypeSymbols.Add(new BridgeGenerationData(bridgeNamespace, builder.targetAssemblies, namedTypeSymbol.OriginalDefinition, ImmutableArray<string>.Empty, ImmutableArray<string>.Empty, false, false, ImmutableHashSet<int>.Empty, false, builder.onlyByMyself));
                                    }
                                }

                                if (propertyTypeIsDelegate && propertyTypeIsNonPublic)
                                    StartComment();

                                Append("public ");
                                if (isStaticMember)
                                    Append("static ");
                                if (property.IsUnsafe())
                                    Append("unsafe ");
                                
                                Append($"{propertyTypeName} {memberName}");

                                if (property.GetMethod != null && property.SetMethod == null)
                                    AppendLine($" => {property.Type.ValueAccessToBridgeAccess(bridgeNamespace, $"{instanceAccessPrefix}.{memberName}")};");
                                else
                                {
                                    AppendLine();
                                    StartBlock();

                                    if (property.GetMethod != null)
                                        AppendLine($"get => {property.Type.ValueAccessToBridgeAccess(bridgeNamespace, $"{instanceAccessPrefix}.{memberName}")};");
                                    if (property.SetMethod != null)
                                    {
                                        if (targetSymbol.IsValueType)
                                        {
                                            AppendLine("set");
                                            StartBlock();
                                            {
                                                AppendLine($"{targetTypeName} instance = {instanceAccessPrefix};");
                                                AppendLine($"instance.{memberName} = {property.Type.BridgeAccessToValueAccess(bridgeNamespace, "value")};");
                                                AppendLine("__instance = instance;");
                                            }
                                            EndBlock();
                                        }
                                        else
                                            AppendLine($"set => {instanceAccessPrefix}.{memberName} = {property.Type.BridgeAccessToValueAccess(bridgeNamespace, "value")};");
                                    }

                                    EndBlock();
                                }

                                if (propertyTypeIsDelegate && propertyTypeIsNonPublic)
                                    EndComment();

                                break;
                            }
                            case IEventSymbol eventSymbol:
                            {
                                string eventTypeName = eventSymbol.Type.GetTypeNameOrBridgeName(bridgeNamespace);
                                bool eventTypeIsNonPublic = eventSymbol.Type.IsNonPublicMember();

                                // 딜리게이트가 Public이 아닐때 어떻게 브릿지를 지을지 생각하지 못했습니다.
                                if (eventTypeIsNonPublic)
                                    StartComment();

                                Append("public ");
                                if (isStaticMember)
                                    Append("static ");
                                AppendLine($"event {eventTypeName} {memberName}");

                                StartBlock();
                                {
                                    if (eventSymbol.AddMethod != null)
                                        AppendLine($"add => {instanceAccessPrefix}.{memberName} += {eventSymbol.Type.BridgeAccessToValueAccess(bridgeNamespace, "value")};");
                                    if (eventSymbol.RemoveMethod != null)
                                        AppendLine($"remove => {instanceAccessPrefix}.{memberName} -= {eventSymbol.Type.BridgeAccessToValueAccess(bridgeNamespace, "value")};");
                                }
                                EndBlock();

                                if (eventTypeIsNonPublic)
                                    EndComment();

                                break;
                            }
                            case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
                            {
                                string returnType = method.GetMethodReturnTypeName(bridgeNamespace);
                                string parameters = string.Join(", ", method.Parameters.GetParameterText(bridgeNamespace));
                                string callParameters = string.Join(", ", method.Parameters.GetCallParameterText(bridgeNamespace));

                                bool returnTypeIsNonPublic = false;
                                bool returnTypeIsDelegate = false;
                                if (!method.ReturnsVoid)
                                {
                                    INamedTypeSymbol? namedReturnType = method.ReturnType.GetNamedTypeSymbol();
                                    if (namedReturnType != null)
                                    {
                                        if (namedReturnType.TypeKind == TypeKind.Delegate)
                                            returnTypeIsDelegate = true;

                                        if (namedReturnType.IsNonPublicMember())
                                        {
                                            if (builder.onlyByMyself && !SymbolEqualityComparer.Default.Equals(targetSymbol, namedReturnType))
                                                break;
                                            
                                            builder.nonPublicTypeSymbols.Add(new BridgeGenerationData(bridgeNamespace, builder.targetAssemblies, namedReturnType.OriginalDefinition, ImmutableArray<string>.Empty, ImmutableArray<string>.Empty, false, false, ImmutableHashSet<int>.Empty, false, builder.onlyByMyself));
                                            returnTypeIsNonPublic = true;
                                        }
                                    }
                                }
                                

                                {
                                    var nonPublicPars = method.Parameters
                                        .Select(static x => x.Type.GetNamedTypeSymbol())
                                        .OfType<INamedTypeSymbol>()
                                        .Where(static x => x.IsNonPublicMember());

                                    // ReSharper disable once LocalVariableHidesPrimaryConstructorParameter
                                    INamedTypeSymbol targetSymbol = builder.targetSymbol;
                                    if (builder.onlyByMyself && nonPublicPars.Any(x => !SymbolEqualityComparer.Default.Equals(targetSymbol, x)))
                                        break;
                                    
                                    builder.nonPublicTypeSymbols.AddRange(nonPublicPars
                                        .Select(x => new BridgeGenerationData(bridgeNamespace, builder.targetAssemblies, x.OriginalDefinition, ImmutableArray<string>.Empty, ImmutableArray<string>.Empty, false, false, ImmutableHashSet<int>.Empty, false, builder.onlyByMyself)));
                                }

                                if (returnTypeIsDelegate && returnTypeIsNonPublic)
                                    StartComment();

                                Append("public ");
                                if (isStaticMember)
                                    Append("static ");
                                if (method.IsUnsafe())
                                    Append("unsafe ");

                                if (!isStaticMember)
                                {
                                    switch (memberName)
                                    {
                                        case "ToString" when method.Parameters.Length == 0:
                                        case "Equals" when method.Parameters.Length == 1 && method.Parameters[0].Type.GetFullTypeName(bridgeNamespace) == "object":
                                        case "GetHashCode" when method.Parameters.Length == 0:
                                            Append("override ");
                                            break;
                                        case "GetType" when method.Parameters.Length == 0:
                                        case "MemberwiseClone" when method.Parameters.Length == 0:
                                            Append("new ");
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (memberName)
                                    {
                                        case "Equals" when method.Parameters.Length == 2 && method.Parameters[0].Type.GetFullTypeName(bridgeNamespace) == "object" && method.Parameters[1].Type.GetFullTypeName(bridgeNamespace) == "object":
                                        case "ReferenceEquals" when method.Parameters.Length == 2 && method.Parameters[0].Type.GetFullTypeName(bridgeNamespace) == "object" && method.Parameters[1].Type.GetFullTypeName(bridgeNamespace) == "object":
                                            Append("new ");
                                            break;
                                    }
                                }
                                
                                Append($"{returnType} {memberName}{method.GetTypeParametersText()}({parameters})");
                                {
                                    string constraintsText = method.GetConstraintsText(bridgeNamespace);
                                    if (!string.IsNullOrEmpty(constraintsText))
                                        Append($" {constraintsText}");
                                }
                                
                                if (!method.Parameters.Any(static x => x.Type.TypeKind != TypeKind.TypeParameter && x.Type.IsNonPublicMember() && x.RefKind != RefKind.None))
                                    AppendLine($" => {GetMethodCallText()};");
                                else
                                {
                                    AppendLine();
                                    StartBlock();

                                    foreach (IParameterSymbol parameterSymbol in method.Parameters.Where(static x => x.Type.TypeKind != TypeKind.TypeParameter && x.Type.IsNonPublicMember()))
                                    {
                                        string parameterTypeName = parameterSymbol.Type.GetFullTypeName(bridgeNamespace);
                                        switch (parameterSymbol.RefKind)
                                        {
                                            case RefKind.Ref:
                                            case RefKind.In:
                                            case RefKind.RefReadOnlyParameter:
                                            {
                                                Append($"{parameterTypeName} __{parameterSymbol.Name} = ");
                                                AppendLine($"({parameterTypeName}){parameterSymbol.GetEscapeName()}.__instance;");
                                                break;
                                            }
                                            case RefKind.Out:
                                            {
                                                AppendLine($"{parameterTypeName} __{parameterSymbol.Name};");
                                                break;
                                            }
                                        }
                                    }

                                    if (!method.ReturnsVoid)
                                        Append($"{returnType} __result = ");
                                    AppendLine($"{GetMethodCallText()};");

                                    foreach (IParameterSymbol parameterSymbol in method.Parameters.Where(static x => x.Type.IsNonPublicMember()))
                                    {
                                        if (parameterSymbol.RefKind is RefKind.Ref or RefKind.Out or RefKind.In or RefKind.RefReadOnlyParameter)
                                            AppendLine($"{parameterSymbol.GetEscapeName()} = {parameterSymbol.Type.ValueAccessToBridgeAccess(bridgeNamespace, $"__{parameterSymbol.Name}")};");
                                    }

                                    if (!method.ReturnsVoid)
                                        AppendLine($"return {method.ReturnType.ValueAccessToBridgeAccess(bridgeNamespace, "__result")};");

                                    EndBlock();
                                }
                                
                                if (returnTypeIsDelegate && returnTypeIsNonPublic)
                                    EndComment();
                                
                                break;

                                string GetMethodCallText() => method.ReturnType.ValueAccessToBridgeAccess(bridgeNamespace, $"{instanceAccessPrefix}.{memberName}{method.GetBridgeTypeArgumentsText(bridgeNamespace)}({callParameters})");
                            }
                        }
                    }
                    
                    if (members.Any())
                        AppendLine("#nullable restore");
                }
            }
        }
    }
}