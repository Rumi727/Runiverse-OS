using Microsoft.CodeAnalysis;
using System.Linq;

namespace RuniOS.APIBridge
{
    public partial class BridgeGeneratorBuilder
    {
        readonly struct EnumBuilder(BridgeGeneratorBuilder builder, INamedTypeSymbol targetSymbol)
        {
            public static void Build(BridgeGeneratorBuilder builder, INamedTypeSymbol targetSymbol) => new EnumBuilder(builder, targetSymbol).Build();

            readonly string bridgeNamespace = builder.bridgeNamespace;
            readonly string bridgeName = targetSymbol.GetBridgeTypeName() + targetSymbol.GetTypeParametersText();
            readonly string targetTypeName = targetSymbol.GetFullTypeName(builder.bridgeNamespace) + targetSymbol.GetTypeParametersText();
            
            void Append(string text = "") => builder.Append(text);
            void AppendLine(string text = "") => builder.AppendLine(text);
            void StartBlock() => builder.StartBlock();
            void EndBlock() => builder.EndBlock();

            public void Build()
            {
                /*// __targetType 필드
                AppendLine("/// <summary>");
                AppendLine("/// 이 브릿지의 타겟 타입입니다.");
                AppendLine("/// </summary>");
                AppendLine($"public static Type __targetType {{ get; }} = typeof({targetTypeName});"); // typeof는 항상 원래 타입을 사용
                AppendLine();*/

                foreach (var fieldSymbol in targetSymbol.GetMembers().OfType<IFieldSymbol>().Where(static x => x.IsConst))
                {
                    Append(fieldSymbol.GetEscapeName());
                    if (fieldSymbol.HasConstantValue)
                        Append($" = {fieldSymbol.ConstantValue}");
                    AppendLine(",");
                }
            }
        }
    }
}