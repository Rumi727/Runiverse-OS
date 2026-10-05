using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

namespace RuniOS.APIBridge
{
    public partial class BridgeGeneratorBuilder
    {
        [StructLayout(LayoutKind.Auto)]
        readonly partial struct ClassBuilder(BridgeGeneratorBuilder builder, INamedTypeSymbol targetSymbol)
        {
            public static void Build(BridgeGeneratorBuilder builder, INamedTypeSymbol targetSymbol) => new ClassBuilder(builder, targetSymbol).Build();

            readonly string bridgeNamespace = builder.bridgeNamespace;
            readonly string bridgeName = targetSymbol.GetBridgeTypeName();
            readonly string bridgeTypeName = targetSymbol.GetBridgeTypeName() + targetSymbol.GetTypeParametersText();
            readonly string targetTypeName = targetSymbol.GetFullTypeName(builder.bridgeNamespace);
            readonly bool targetIsAbstract = targetSymbol.IsAbstract;
            readonly bool targetIsStatic = targetSymbol.IsStatic || builder.forceStatic;
            readonly bool targetIsNonPublic = targetSymbol.IsNonPublicMember();
            readonly INamedTypeSymbol? targetPublicBaseType = targetSymbol.GetPublicBaseType();
            
            void Append(string text = "") => builder.Append(text);
            void AppendLine(string text = "") => builder.AppendLine(text);
            void StartBlock() => builder.StartBlock();
            void EndBlock() => builder.EndBlock();

            public void Build()
            {
                string bridgeNamespace = this.bridgeNamespace;
                
                // __targetType 필드
                AppendLine("/// <summary>");
                AppendLine("/// 이 브릿지의 타겟 타입입니다.");
                AppendLine("/// </summary>");
                AppendLine($"public static global::System.Type __targetType {{ get; }} = typeof({targetTypeName});"); // typeof는 항상 원래 타입을 사용
                AppendLine();

                // 모든 생성자에 대한 __CreateInstance 오버로드 생성 (정적 클래스일 경우 제외)
                if (!targetIsStatic)
                {
                    AppendLine("#nullable disable");
                    
                    if (!targetIsAbstract && !builder.skipConstructors) // 정적/추상 클래스는 인스턴스 생성 불가
                    {
                        HashSet<string> processedConstructors = [];
                        bool anyNotDefaultCtor = targetSymbol.Constructors.Any(static c => !c.IsImplicitlyDeclared);
                        int index = 0;
                        foreach (var ctor in targetSymbol.Constructors)
                        {
                            if (anyNotDefaultCtor && ctor.IsImplicitlyDeclared)
                                continue;

                            {
                                var nonPublicPars = ctor.Parameters
                                    .Select(static x => x.Type.GetNamedTypeSymbol())
                                    .OfType<INamedTypeSymbol>()
                                    .Where(static x => x.IsNonPublicMember());

                                // ReSharper disable once LocalVariableHidesPrimaryConstructorParameter
                                INamedTypeSymbol targetSymbol = builder.targetSymbol;
                                if (builder.onlyByMyself && nonPublicPars.Any(x => !SymbolEqualityComparer.Default.Equals(targetSymbol, x)))
                                    continue;
                                
                                ImmutableArray<string> targetAssemblies = builder.targetAssemblies;
                                bool onlyByMyself = builder.onlyByMyself;
                                builder.nonPublicTypeSymbols.AddRange(nonPublicPars
                                    .Select(x => new BridgeGenerationData(bridgeNamespace, targetAssemblies, x.OriginalDefinition, [string.Empty], ImmutableArray<string>.Empty, false, false, ImmutableHashSet<int>.Empty, false, onlyByMyself)));
                            }
                            
                            string parameters = string.Join(", ", ctor.Parameters.GetParameterText(bridgeNamespace));
                            string callParameters = string.Join(", ", ctor.Parameters.GetCallParameterText(bridgeNamespace));
                            bool isNonPublic = builder.includePublicMember || ctor.IsNonPublicMember();

                            if (!processedConstructors.Add(parameters))
                                continue;
                            
                            if (builder.excludeConstructors.Contains(index))
                            {
                                AppendLine($"// (Index {index}) {parameters}");
                                if (isNonPublic || targetIsNonPublic)
                                    AppendLine();
                                
                                continue;
                            }

                            if (isNonPublic || targetIsNonPublic)
                            {
                                AppendLine("/// <summary>");
                                AppendLine("/// 타겟 타입을 만들고 브릿지로 생성합니다.");
                                AppendLine("/// </summary>");
                            }

                            if (isNonPublic)
                                AppendLine($"public static {bridgeTypeName} __CreateInstanceNonPublic({parameters}) => new {bridgeTypeName}(new {targetTypeName}({callParameters}));");
                            else if (targetIsNonPublic)
                                AppendLine($"public static {bridgeTypeName} __CreateInstance({parameters}) => new {bridgeTypeName}(new {targetTypeName}({callParameters}));");

                            if (isNonPublic || targetIsNonPublic)
                                AppendLine();

                            index++;
                        }
                    }
                    
                    AppendLine("#nullable restore");

                    // __cached 및 __GetInstanceFrom
                    // __cached의 키 타입은 항상 원래 타입
                    AppendLine($"private static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<{targetTypeName}, {bridgeTypeName}> __cached = new();");
                    AppendLine();

                    // __GetInstanceFrom의 매개변수 타입 설정
                    string getInstanceFromParamType = targetPublicBaseType == null ? "object" : targetPublicBaseType.GetFullTypeName(bridgeNamespace);

                    AppendLine("/// <summary>");
                    AppendLine("/// 타겟 타입의 인스턴스로 브릿지를 생성합니다.");
                    AppendLine("/// </summary>");
                    AppendLine("/// <exception cref=\"global::System.ArgumentException\">인스턴스의 타입이 유효하지 않을 경우 발생합니다.</exception>");
                    AppendLine("[return: global::System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(instance))]");
                    AppendLine($"public static {bridgeTypeName}? __GetInstanceFrom({getInstanceFromParamType}? instance)");
                    StartBlock();
                    {
                        AppendLine("if (instance == null) return null;");
                        AppendLine("if (!__targetType.IsInstanceOfType(instance)) throw new global::System.ArgumentException(\"Invalid instance type\");");
                        AppendLine();
                        AppendLine($"{targetTypeName} castedInstance = ({targetTypeName})instance;");
                        AppendLine($"if (!__cached.TryGetValue(castedInstance, out {bridgeTypeName}? bridgeInstance))");
                        StartBlock();
                        {
                            AppendLine($"bridgeInstance = new {bridgeTypeName}(castedInstance);");
                            AppendLine("__cached.Add(castedInstance, bridgeInstance);");
                        }
                        EndBlock();
                        AppendLine();
                        AppendLine("return bridgeInstance;");
                    }
                    EndBlock();
                    AppendLine();

                    // 생성자 및 __instance 필드
                    AppendLine($"private {bridgeName}({targetTypeName} instance) => __instance = instance;"); // 생성자 매개변수는 원래 타입
                    AppendLine();
                    AppendLine("/// <summary>");
                    AppendLine("/// 타겟 타입의 인스턴스입니다.");
                    AppendLine("/// </summary>");
                    AppendLine($"public {getInstanceFromParamType} __instance {{ get; }}"); // __instance 필드는 원래 타입
                }

                MemberBuilder.Build(builder, targetSymbol);
            }
        }
    }
}