using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

/// <summary>
/// Provides symbol and syntax checks used by type registry discovery and binding.<br/>
/// 타입 레지스트리 발견 및 바인딩에 사용하는 심볼·구문 검사를 제공합니다.
/// </summary>
static class TypeRegistrySymbolHelpers
{
    /// <summary>
    /// Determines whether a candidate is the specified type, derives from it, or implements it.<br/>
    /// 후보가 지정된 타입과 같거나 이를 상속 또는 구현하는지 확인합니다.
    /// </summary>
    /// <param name="candidate">
    /// The candidate type to inspect; <see langword="null"/> is rejected.<br/>
    /// 검사할 후보 타입이며, <see langword="null"/>이면 거부합니다.
    /// </param>
    /// <param name="baseType">
    /// The type that the candidate must match or derive from.<br/>
    /// 후보가 일치하거나 상속 또는 구현해야 하는 타입입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the candidate matches the type or its assignable hierarchy; otherwise, <see langword="false"/>.<br/>
    /// 후보가 타입 또는 할당 가능한 계층과 일치하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    public static bool IsSameOrDerived(ITypeSymbol? candidate, ITypeSymbol baseType)
    {
        if (candidate == null)
            return false;
        if (IsAssignableMatch(candidate, baseType))
            return true;

        if (candidate is not INamedTypeSymbol namedCandidate)
            return false;

        if (namedCandidate.AllInterfaces.Any(x => IsAssignableMatch(x, baseType)))
            return true;

        for (INamedTypeSymbol? current = namedCandidate.BaseType; current != null; current = current.BaseType)
        {
            if (IsAssignableMatch(current, baseType))
                return true;
            if (current.AllInterfaces.Any(x => IsAssignableMatch(x, baseType)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether two types are identical or represent the same unbound generic definition.<br/>
    /// 두 타입이 동일하거나 같은 바인딩되지 않은 제네릭 정의를 나타내는지 확인합니다.
    /// </summary>
    /// <param name="candidate">
    /// The candidate type to compare.<br/>
    /// 비교할 후보 타입입니다.
    /// </param>
    /// <param name="baseType">
    /// The type to compare against.<br/>
    /// 비교 기준 타입입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the types match under the helper's assignability rules; otherwise, <see langword="false"/>.<br/>
    /// 도우미의 할당 가능성 규칙에 따라 두 타입이 일치하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    static bool IsAssignableMatch(ITypeSymbol candidate, ITypeSymbol baseType)
    {
        if (SymbolEqualityComparer.Default.Equals(candidate, baseType))
            return true;

        return candidate is INamedTypeSymbol namedCandidate && baseType is INamedTypeSymbol namedBase &&
            namedBase.IsUnboundGenericType && SymbolEqualityComparer.Default.Equals(namedCandidate.OriginalDefinition, namedBase.OriginalDefinition);
    }

    /// <summary>
    /// Determines whether a symbol and its containing declarations are accessible to generated code.<br/>
    /// 심볼과 이를 포함하는 선언을 생성된 코드에서 접근할 수 있는지 확인합니다.
    /// </summary>
    /// <param name="symbol">
    /// The symbol whose accessibility is checked.<br/>
    /// 접근성을 확인할 심볼입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation used to determine whether internal access is within the same assembly.<br/>
    /// internal 접근이 같은 어셈블리인지 판단하는 데 사용할 컴파일입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when generated code can access every relevant declaration; otherwise, <see langword="false"/>.<br/>
    /// 생성된 코드가 관련 선언을 모두 접근할 수 있으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    public static bool IsAccessibleFromGeneratedCode(ISymbol symbol, Compilation compilation)
    {
        if (symbol is IPointerTypeSymbol pointerType)
            return IsAccessibleFromGeneratedCode(pointerType.PointedAtType, compilation);

        bool sameAssembly = SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, compilation.Assembly);
        for (ISymbol? current = symbol; current != null; current = GetContainingSymbol(current))
        {
            if (current is INamespaceSymbol or IModuleSymbol or IAssemblySymbol)
                continue;

            switch (current.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    continue;
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                {
                    if (sameAssembly)
                        continue;
                    return false;
                }
                default:
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether a type has an accessible instance constructor without parameters.<br/>
    /// 타입에 접근 가능한 매개 변수 없는 인스턴스 생성자가 있는지 확인합니다.
    /// </summary>
    /// <param name="type">
    /// The type whose constructors are inspected.<br/>
    /// 생성자를 검사할 타입입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation used for accessibility checks.<br/>
    /// 접근성 검사에 사용할 컴파일입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when an accessible parameterless constructor exists; otherwise, <see langword="false"/>.<br/>
    /// 접근 가능한 매개 변수 없는 생성자가 있으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    public static bool HasAccessibleParameterlessConstructor(INamedTypeSymbol type, Compilation compilation)
    {
        foreach (IMethodSymbol constructor in type.InstanceConstructors)
        {
            if (constructor.Parameters.Length == 0 && IsAccessibleFromGeneratedCode(constructor, compilation))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether every source declaration in a type's containing hierarchy is <c>partial</c>.<br/>
    /// 타입을 포함하는 계층의 모든 소스 선언이 <c>partial</c>인지 확인합니다.
    /// </summary>
    /// <param name="type">
    /// The type whose containing hierarchy is checked.<br/>
    /// 포함 타입 계층을 확인할 타입입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when every declaration is a source <c>partial</c> type declaration; otherwise, <see langword="false"/>.<br/>
    /// 모든 선언이 소스의 <c>partial</c> 타입 선언이면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    public static bool IsPartialTypeHierarchy(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current != null; current = current.ContainingType)
        {
            if (current.DeclaringSyntaxReferences.Length == 0)
                return false;

            foreach (SyntaxReference reference in current.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is not TypeDeclarationSyntax declaration || declaration.Modifiers.All(x => x.Text != "partial"))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether a property is a <c>partial</c>, <c>get</c>-only definition without an implementation body.<br/>
    /// 속성이 구현 본문이 없는 <c>partial</c> 읽기 전용 정의인지 확인합니다.
    /// </summary>
    /// <param name="declaration">
    /// The property declaration to inspect.<br/>
    /// 검사할 속성 선언입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the declaration has one body-less <c>get</c> accessor and no expression body; otherwise, <see langword="false"/>.<br/>
    /// 선언에 본문 없는 <c>get</c> 접근자 하나만 있고 식 본문이 없으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    public static bool IsPartialPropertyDefinition(PropertyDeclarationSyntax declaration)
    {
        if (declaration.Modifiers.All(x => x.Text != "partial") || declaration.AccessorList == null)
            return false;
        if (declaration.ExpressionBody != null || declaration.AccessorList.Accessors.Count != 1)
            return false;

        AccessorDeclarationSyntax accessor = declaration.AccessorList.Accessors[0];
        return accessor.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.GetAccessorDeclaration) && accessor.Body == null && accessor.ExpressionBody == null;
    }

    /// <summary>
    /// Collects registration attributes declared on a type and on base types when their usage permits inheritance.<br/>
    /// 타입과 기본 타입에 선언된 등록 특성 중 상속이 허용된 특성을 수집합니다.
    /// </summary>
    /// <param name="type">
    /// The type whose attribute hierarchy is inspected.<br/>
    /// 특성 계층을 검사할 타입입니다.
    /// </param>
    /// <param name="registrationAttributeBase">
    /// The base type that qualifying registration attributes must match or derive from.<br/>
    /// 등록 특성이 일치하거나 상속해야 하는 기본 타입입니다.
    /// </param>
    /// <returns>
    /// The matching attributes in traversal order from the specified type toward its base types.<br/>
    /// 지정된 타입에서 기본 타입 방향으로 순회한 순서의 일치하는 특성입니다.
    /// </returns>
    public static ImmutableArray<AttributeData> GetInheritedAttributes(INamedTypeSymbol type, INamedTypeSymbol registrationAttributeBase)
    {
        ImmutableArray<AttributeData>.Builder attributes = ImmutableArray.CreateBuilder<AttributeData>();
        for (INamedTypeSymbol? current = type; current != null; current = current.BaseType)
        {
            bool isDirect = SymbolEqualityComparer.Default.Equals(current, type);
            foreach (AttributeData attribute in current.GetAttributes())
            {
                if (!IsSameOrDerived(attribute.AttributeClass, registrationAttributeBase))
                    continue;
                if (isDirect || IsInheritedAttribute(attribute.AttributeClass))
                    attributes.Add(attribute);
            }
        }

        return attributes.ToImmutable();
    }

    /// <summary>
    /// Determines whether an attribute type is inheritable according to its <see cref="System.AttributeUsageAttribute"/> metadata.<br/>
    /// 특성 타입의 <see cref="System.AttributeUsageAttribute"/> 메타데이터에 따라 상속 가능한지 확인합니다.
    /// </summary>
    /// <param name="attributeType">
    /// The attribute type to inspect; <see langword="null"/> uses the default inheritable result.<br/>
    /// 검사할 특성 타입이며, <see langword="null"/>이면 기본 상속 가능 결과를 사용합니다.
    /// </param>
    /// <returns>
    /// The declared <c>Inherited</c> value, or <see langword="true"/> when no overriding value is found.<br/>
    /// 선언된 <c>Inherited</c> 값이며, 재정의 값을 찾지 못하면 <see langword="true"/>입니다.
    /// </returns>
    public static bool IsInheritedAttribute(INamedTypeSymbol? attributeType)
    {
        for (INamedTypeSymbol? current = attributeType; current != null; current = current.BaseType)
        {
            foreach (AttributeData usage in current.GetAttributes())
            {
                if (usage.AttributeClass?.ToDisplayString() != "System.AttributeUsageAttribute")
                    continue;

                foreach (KeyValuePair<string, TypedConstant> namedArgument in usage.NamedArguments)
                {
                    if (namedArgument.Key == "Inherited" && namedArgument.Value.Value is bool inherited)
                        return inherited;
                }

                return true;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the containing symbol while treating an assembly as the root with no containing symbol.<br/>
    /// 어셈블리를 포함 심볼이 없는 루트로 처리하면서 포함 심볼을 가져옵니다.
    /// </summary>
    /// <param name="symbol">
    /// The symbol whose containing symbol is requested.<br/>
    /// 포함 심볼을 가져올 심볼입니다.
    /// </param>
    /// <returns>
    /// The containing assembly for a module, no symbol for an assembly, or the regular containing symbol otherwise.<br/>
    /// 모듈이면 포함 어셈블리를, 어셈블리면 심볼이 없음을, 그 외에는 일반 포함 심볼을 반환합니다.
    /// </returns>
    public static ISymbol? GetContainingSymbol(ISymbol symbol)
    {
        return symbol switch
        {
            IAssemblySymbol => null,
            IModuleSymbol module => module.ContainingAssembly,
            _ => symbol.ContainingSymbol
        };
    }

    /// <summary>
    /// Gets the first source location of a symbol, or <see cref="Location.None"/> when it has no source location.<br/>
    /// 심볼의 첫 번째 소스 위치를 가져오며, 소스 위치가 없으면 <see cref="Location.None"/>을 반환합니다.
    /// </summary>
    /// <param name="symbol">
    /// The symbol whose location is requested.<br/>
    /// 위치를 가져올 심볼입니다.
    /// </param>
    /// <returns>
    /// The first in-source location, or <see cref="Location.None"/>.<br/>
    /// 첫 번째 소스 위치이며, 없으면 <see cref="Location.None"/>입니다.
    /// </returns>
    public static Location GetLocation(ISymbol symbol) => symbol.Locations.FirstOrDefault(x => x.IsInSource) ?? Location.None;

    /// <summary>
    /// Gets the source location where an attribute was applied, or <see cref="Location.None"/> when unavailable.<br/>
    /// 특성이 적용된 소스 위치를 가져오며, 사용할 수 없으면 <see cref="Location.None"/>을 반환합니다.
    /// </summary>
    /// <param name="attribute">
    /// The attribute whose application location is requested.<br/>
    /// 적용 위치를 가져올 특성입니다.
    /// </param>
    /// <returns>
    /// The attribute application location, or <see cref="Location.None"/>.<br/>
    /// 특성 적용 위치이며, 없으면 <see cref="Location.None"/>입니다.
    /// </returns>
    public static Location GetLocation(AttributeData attribute) => attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
}
