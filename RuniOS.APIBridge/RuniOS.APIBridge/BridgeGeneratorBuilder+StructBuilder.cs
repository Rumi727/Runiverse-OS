using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace RuniOS.APIBridge
{
    public partial class BridgeGeneratorBuilder
    {
        [StructLayout(LayoutKind.Auto)]
        readonly partial struct StructBuilder(BridgeGeneratorBuilder builder, INamedTypeSymbol targetSymbol)
        {
            public static void Build(BridgeGeneratorBuilder builder, INamedTypeSymbol targetSymbol) => new StructBuilder(builder, targetSymbol).Build();
            
            readonly string bridgeNamespace = builder.bridgeNamespace;
            readonly string bridgeName = targetSymbol.GetBridgeTypeName();
            readonly string bridgeTypeName = targetSymbol.GetBridgeTypeName() + targetSymbol.GetTypeParametersText();
            readonly string targetTypeName = targetSymbol.GetFullTypeName(builder.bridgeNamespace);
            readonly bool targetIsStatic = builder.forceStatic;
            readonly bool targetIsNonPublic = targetSymbol.IsNonPublicMember();
            
            void Append(string text = "") => builder.Append(text);
            void AppendLine(string text = "") => builder.AppendLine(text);
            void StartBlock() => builder.StartBlock();
            void EndBlock() => builder.EndBlock();

            public void Build()
            {
                // __targetType 필드
                AppendLine("/// <summary>");
                AppendLine("/// 이 브릿지의 타겟 타입입니다.");
                AppendLine("/// </summary>");
                AppendLine($"public static global::System.Type __targetType {{ get; }} = typeof({targetTypeName});"); // typeof는 항상 원래 타입을 사용
                AppendLine();

                // 모든 생성자에 대한 __CreateInstance 오버로드 생성  (정적으로 표시됐을 경우 경우 제외)
                if (!targetIsStatic)
                {
                    if (!builder.skipConstructors)
                    {
                        HashSet<string> processedConstructors = [];
                        bool anyNotDefaultCtor = targetSymbol.Constructors.Any(static c => !c.IsImplicitlyDeclared);
                        int index = 0;
                        foreach (var ctor in targetSymbol.Constructors)
                        {
                            if (anyNotDefaultCtor && ctor.IsImplicitlyDeclared)
                                continue;
                        
                            var parameters = string.Join(", ", ctor.Parameters.GetParameterText(bridgeNamespace));
                            var callParameters = string.Join(", ", ctor.Parameters.GetCallParameterText(bridgeNamespace));
                            bool isNonPublic = ctor.IsNonPublicMember();
                            
                            if (!processedConstructors.Add(parameters))
                                continue;
                            
                            if (builder.excludeConstructors.Contains(index))
                            {
                                AppendLine($"// (Index {index}) {parameters}");
                                if (isNonPublic || targetIsNonPublic)
                                    AppendLine();

                                index++;
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
                    
                    // __GetInstanceFrom의 매개변수 타입 설정
                    var getInstanceFromParamType = targetIsNonPublic ? "object" : targetTypeName;

                    AppendLine("/// <summary>");
                    AppendLine("/// 타겟 타입의 인스턴스로 브릿지를 생성합니다.");
                    AppendLine("/// </summary>");
                    AppendLine("/// <param name=\"instance\">타겟 타입의 인스턴스입니다.</param>");
                    AppendLine("/// <exception cref=\"global::System.ArgumentNullException\">인스턴스가 null일 경우 발생합니다.</exception>");
                    AppendLine("/// <exception cref=\"global::System.ArgumentException\">인스턴스의 타입이 유효하지 않을 경우 발생합니다.</exception>");
                    AppendLine($"public static {bridgeTypeName} __GetInstanceFrom({getInstanceFromParamType} instance)");
                    StartBlock();
                    {
                        if (getInstanceFromParamType == "object" || targetSymbol.IsNullable())
                            AppendLine("if (instance == null) throw new global::System.ArgumentNullException(nameof(instance));");
                        AppendLine("if (!__targetType.IsInstanceOfType(instance)) throw new global::System.ArgumentException(\"Invalid instance type\");");
                        if (getInstanceFromParamType == "object" || targetSymbol.IsNullable())
                            AppendLine();
                        AppendLine($"return new {bridgeTypeName}(({targetTypeName})instance);");
                    }
                    EndBlock();
                    AppendLine();

                    // 생성자 및 __instance 필드
                    AppendLine($"private {bridgeName}({targetTypeName} instance) => __instance = instance;"); // 생성자 매개변수는 원래 타입
                    AppendLine();
                    AppendLine("/// <summary>");
                    AppendLine("/// 타겟 타입의 인스턴스입니다.");
                    AppendLine("/// </summary>");
                    AppendLine($"public {(targetIsNonPublic ? "object" : targetTypeName)} __instance {{ get; private set; }}"); // __instance 필드는 원래 타입
                }

                ClassBuilder.MemberBuilder.Build(builder, targetSymbol);
            }
        }
    }
}