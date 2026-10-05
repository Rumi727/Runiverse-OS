using Microsoft.CodeAnalysis;
using System.Runtime.InteropServices;

namespace RuniOS.APIBridge
{
    public partial class BridgeGeneratorBuilder
    {
        [StructLayout(LayoutKind.Auto)]
        readonly partial struct InterfaceBuilder(BridgeGeneratorBuilder builder, INamedTypeSymbol targetSymbol)
        {
            public static void Build(BridgeGeneratorBuilder builder, INamedTypeSymbol targetSymbol) => new InterfaceBuilder(builder, targetSymbol).Build();

            readonly string bridgeNamespace = builder.bridgeNamespace;
            readonly string bridgeName = targetSymbol.GetBridgeTypeName();
            readonly string bridgeTypeName = targetSymbol.GetBridgeTypeName() + targetSymbol.GetTypeParametersText();
            readonly string targetTypeName = targetSymbol.GetFullTypeName(builder.bridgeNamespace);
            readonly bool targetIsStatic = builder.forceStatic;
            readonly INamedTypeSymbol? targetPublicBaseType = targetSymbol.GetPublicBaseType();
            
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
                
                // __GetInstanceFrom의 매개변수 타입 설정
                string getInstanceFromParamType = targetPublicBaseType == null ? "object" : targetPublicBaseType.GetFullTypeName(bridgeNamespace);

                if (!targetIsStatic)
                {
                    // __cached 및 __GetInstanceFrom
                    // __cached의 키 타입은 항상 원래 타입
                    AppendLine($"private static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<{targetTypeName}, __{bridgeName}> __cached = new();");
                    AppendLine();

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
                        AppendLine($"if (!__cached.TryGetValue(castedInstance, out __{bridgeName}? bridgeInstance))");
                        StartBlock();
                        {
                            AppendLine($"bridgeInstance = new __{bridgeName}(castedInstance);");
                            AppendLine("__cached.Add(castedInstance, bridgeInstance);");
                        }
                        EndBlock();
                        AppendLine();
                        AppendLine("return bridgeInstance;");
                    }
                    EndBlock();
                    AppendLine();

                    // __instance 필드
                    AppendLine("/// <summary>");
                    AppendLine("/// 타겟 타입의 인스턴스입니다.");
                    AppendLine("/// </summary>");
                    AppendLine($"public {getInstanceFromParamType} __instance {{ get; }}"); // __instance 필드는 원래 타입
                    
                    MemberBuilder.Build(builder, targetSymbol);
                }
                
                ClassBuilder.MemberBuilder.Build(builder, targetSymbol, ClassBuilder.MemberBuilder.Target.StaticMember);

                if (!targetIsStatic)
                {
                    AppendLine();
                    AppendLine();
                    AppendLine();
                    
                    // 비공개 구현 클래스
                    AppendLine("/// <summary>");
                    AppendLine("/// 브릿지 인터페이스의 내부 구현 클래스입니다.");
                    AppendLine("/// </summary>");
                    AppendLine($"internal partial class __{bridgeName} : {bridgeTypeName}");
                    StartBlock();
                    {
                        AppendLine($"public __{bridgeName}({targetTypeName} instance) => this.__instance = instance;"); // 생성자 매개변수는 원래 타입
                        AppendLine($"public {getInstanceFromParamType} __instance {{ get; }}"); // __instance 필드는 원래 타입

                        ClassBuilder.MemberBuilder.Build(builder, targetSymbol, ClassBuilder.MemberBuilder.Target.InstanceMember);
                    }
                    EndBlock();
                }
            }
        }
    }
}