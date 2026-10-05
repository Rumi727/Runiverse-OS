using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace RuniOS.APIBridge
{
    public static class SymbolExtensions
    {
        /// <summary>
        /// 주어진 심볼이 OriginalAttributesAttribute를 통해 원래 비공개였는지 확인합니다.
        /// </summary>
        /// <param name="symbol">확인할 심볼입니다.</param>
        /// <returns>원래 비공개였으면 <see langword="true"/>, 아니면 <see langword="false"/>를 반환합니다.</returns>
        public static bool IsNonPublicMember(this ISymbol symbol)
        {
            switch (symbol)
            {
                case IArrayTypeSymbol:
                case IPointerTypeSymbol:
                    return false;
                case IPropertySymbol propertySymbol:
                    return (propertySymbol.GetMethod?.IsNonPublicMember() ?? false) || (propertySymbol.SetMethod?.IsNonPublicMember() ?? false);
                case IEventSymbol eventSymbol:
                    return (eventSymbol.AddMethod?.IsNonPublicMember() ?? false) || (eventSymbol.RemoveMethod?.IsNonPublicMember() ?? false);
            }

            if (symbol.DeclaredAccessibility != Accessibility.Public)
                return true;
        
            var originalAttributesAttribute = symbol.GetAttributes()
                .Where(static ad => ad.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::BepInEx.AssemblyPublicizer.OriginalAttributesAttribute")
                .FirstOrDefault();

            if (originalAttributesAttribute != null && originalAttributesAttribute.ConstructorArguments.Length > 0)
            {
                var arg = originalAttributesAttribute.ConstructorArguments[0];
                if (arg.Kind == TypedConstantKind.Enum)
                {
                    if (arg.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Reflection.MethodAttributes")
                    {
                        if (arg.Value is int enumValue && (enumValue & (int)MethodAttributes.MemberAccessMask) != (int)MethodAttributes.Public)
                            return true;
                    }
                    else if (arg.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Reflection.FieldAttributes")
                    {
                        if (arg.Value is int enumValue && (enumValue & (int)FieldAttributes.FieldAccessMask) != (int)FieldAttributes.Public)
                            return true;
                    }
                    else if (arg.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Reflection.TypeAttributes")
                    {
                        if (arg.Value is int enumValue && (enumValue & (int)TypeAttributes.VisibilityMask) != (int)TypeAttributes.Public)
                            return true;
                    }
                }
            }
            
            return symbol.ContainingType?.IsNonPublicMember() ?? false; // 어트리뷰트가 없거나 비공개를 나타내지 않음
        }
        
        /// <summary>
        /// 주어진 심볼이 MethodImpl(MethodImplOptions.InternalCall)인지 확인합니다.
        /// </summary>
        /// <param name="symbol">확인할 심볼입니다.</param>
        /// <returns>심볼이 InternalCall이면 <see langword="true"/>, 아니면 <see langword="false"/>를 반환합니다.</returns>
        public static bool IsInternalCall(this ISymbol symbol)
        {
            if (symbol is IMethodSymbol methodSymbol)
                return methodSymbol.MethodImplementationFlags == MethodImplAttributes.InternalCall;

            return false;
        }

        /// <summary>
        /// 주어진 심볼이 unsafe인지 확인합니다.
        /// </summary>
        /// <param name="symbol">확인할 심볼입니다.</param>
        /// <returns>심볼이 unsafe이면 <see langword="true"/>, 아니면 <see langword="false"/>를 반환합니다.</returns>
        public static bool IsUnsafe(this ISymbol symbol)
        {
            return false;
            /*return symbol switch
            {
                IPointerTypeSymbol => true,
                IArrayTypeSymbol arrayTypeSymbol => arrayTypeSymbol.ElementType.IsUnsafe(),
                INamedTypeSymbol namedTypeSymbol => namedTypeSymbol.GetMembers().Any(static x => x.IsUnsafe()),
                IFieldSymbol fieldSymbol => fieldSymbol.Type.IsUnsafe(),
                IPropertySymbol propertySymbol => propertySymbol.Type.IsUnsafe() || propertySymbol.Parameters.Any(static x => x.IsUnsafe()),
                IMethodSymbol methodSymbol => methodSymbol.ReturnType.IsUnsafe() || methodSymbol.Parameters.Any(static x => x.IsUnsafe()) || methodSymbol.TypeParameters.Any(static x => x.IsUnsafe()),
                IParameterSymbol parameterSymbol => parameterSymbol.Type.IsUnsafe(),
                IEventSymbol eventSymbol => eventSymbol.Type.IsUnsafe(),
                IAliasSymbol aliasSymbol => aliasSymbol.Target.IsUnsafe(),
                _ => false
            };*/
        }

        /// <summary>
        /// 지정된 멤버 심볼이 명시적 인터페이스 구현인지 확인합니다.
        /// </summary>
        /// <param name="symbol">확인할 멤버 심볼입니다.</param>
        /// <returns>
        /// 멤버가 명시적 인터페이스 구현이면 <see langword="true"/>를, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool IsExplicitInterfaceImplementations(this ISymbol symbol)
        {
            return symbol switch
            {
                IMethodSymbol methodSymbol => !methodSymbol.ExplicitInterfaceImplementations.IsEmpty,
                IPropertySymbol propertySymbol => !propertySymbol.ExplicitInterfaceImplementations.IsEmpty,
                IEventSymbol eventSymbol => !eventSymbol.ExplicitInterfaceImplementations.IsEmpty,
                _ => false
            };
        }

        public static bool IsNullable(this ITypeSymbol symbol)
        {
            if (symbol.TypeKind == TypeKind.Struct)
                return symbol.SpecialType == SpecialType.System_Nullable_T;
            else if (symbol.IsReferenceType)
                return true;

            return false;
        }

        public static bool IsEnumerable(this ITypeSymbol symbol) => symbol.IsEnumerable(out _);
        public static bool IsEnumerable(this ITypeSymbol symbol, [MaybeNullWhen(false)] out ITypeSymbol elementType)
        {
            switch (symbol)
            {
                case IArrayTypeSymbol arrayTypeSymbol:
                {
                    elementType = arrayTypeSymbol.ElementType;
                    return true;
                }
                case INamedTypeSymbol namedTypeSymbol when namedTypeSymbol.IsGenericType && namedTypeSymbol.TypeParameters.Length == 1 && namedTypeSymbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Collections.Generic.IEnumerable<T>":
                {
                    if (namedTypeSymbol.TypeArguments.Any())
                        elementType = namedTypeSymbol.TypeArguments.First();
                    else
                        elementType = namedTypeSymbol.TypeParameters.First();
                    
                    return true;
                }
            }

            foreach (var interfaceSymbol in symbol.Interfaces)
            {
                if (interfaceSymbol.IsEnumerable(out elementType))
                    return true;
            }
            
            if (symbol.BaseType?.IsEnumerable(out elementType) ?? false)
                return true;

            elementType = null;
            return false;
        }

        public static bool IsKeyValuePairEnumerable(this ITypeSymbol symbol) => symbol.IsKeyValuePairEnumerable(out _, out _);

        public static bool IsKeyValuePairEnumerable(this ITypeSymbol symbol, [MaybeNullWhen(false)] out ITypeSymbol keyType, [MaybeNullWhen(false)] out ITypeSymbol valueTypeSymbol)
        {
            if (symbol.IsEnumerable(out ITypeSymbol? elementTypeSymbol) && elementTypeSymbol is INamedTypeSymbol elementNamedTypeSymbol && elementNamedTypeSymbol.OriginalDefinition.TypeParameters.Length == 2 && elementNamedTypeSymbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Collections.Generic.KeyValuePair<TKey, TValue>")
            {
                keyType = elementNamedTypeSymbol.TypeArguments[0];
                valueTypeSymbol = elementNamedTypeSymbol.TypeArguments[1];
                
                return true;
            }

            keyType = null;
            valueTypeSymbol = null;

            return false;
        }

        public static bool IsCompilerGenerated(this ISymbol symbol)
        {
            if (symbol is IFieldSymbol fieldSymbol && fieldSymbol.AssociatedSymbol != null)
                return true;

            return symbol.GetAttributes().Any(static x => x.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Runtime.CompilerServices.CompilerGeneratedAttribute");
        }

        public static string EnumerableToList(this ITypeSymbol symbol, string bridgeNamespace, string enumerableText, bool bridgeTypeArguments = true)
        {
            if (symbol is IArrayTypeSymbol)
                return $"global::System.Linq.Enumerable.ToArray({enumerableText})";
            else
            {
                switch (symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                {
                    case "global::System.Collections.Generic.List<T>":
                        return $"global::System.Linq.Enumerable.ToList({enumerableText})";
                    case "global::System.Collections.Generic.HashSet<T>":
                    case "global::System.Collections.Generic.Stack<T>":
                    case "global::System.Collections.Generic.Queue<T>":
                        return $"new {(bridgeTypeArguments ? symbol.GetTypeNameOrBridgeName(bridgeNamespace) : symbol.GetFullTypeName(bridgeNamespace, bridgeTypeArguments))}({enumerableText})";
                    case "global::System.Collections.Generic.IEnumerable<T>":
                        return enumerableText;
                }
            }

            // TODO
            // * 오류 메시지 추가
            
            return enumerableText;
        }

        public static string KeyValuePairEnumerableToDictionary(this ITypeSymbol symbol, string keyValuePairEnumerableText)
        {
            if (symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Collections.Generic.Dictionary<TKey, TValue>")
                return $"global::System.Linq.Enumerable.ToDictionary({keyValuePairEnumerableText}, static x => x.Key, static x => x.Value)";

            return keyValuePairEnumerableText;
        }

        /// <summary>
        /// 대상 심볼의 네임스페이스를 기반으로 브릿지 네임스페이스를 생성합니다.
        /// </summary>
        /// <param name="symbol">대상 심볼입니다.</param>
        /// <param name="bridgeNameSpace">네임스페이스를 결정합니다.</param>
        /// <returns>생성된 브릿지 네임스페이스 문자열입니다.</returns>
        public static string GetBridgeNamespace(this INamedTypeSymbol symbol, string bridgeNameSpace)
        {
            INamespaceSymbol? namespaceSymbol = symbol.ContainingNamespace;
            string result = string.Empty;
            while (namespaceSymbol != null && !string.IsNullOrEmpty(namespaceSymbol.Name))
            {
                result = $".{namespaceSymbol.Name}{result}";
                namespaceSymbol = namespaceSymbol.ContainingNamespace;
            }
            
            return bridgeNameSpace + result;
        }

        /// <summary>
        /// 대상 심볼의 이름을 기반으로 브릿지 타입 이름을 생성합니다.
        /// 중첩 타입의 경우 'OuterBridge.InnerBridge' 형태로 생성합니다.
        /// </summary>
        /// <param name="symbol">대상 심볼입니다.</param>
        /// <param name="bridgeNamespace"></param>
        /// <returns>생성된 브릿지 타입 이름 문자열입니다.</returns>
        public static string GetBridgeTypeNameIncludeContaining(this INamedTypeSymbol symbol, string bridgeNamespace)
        {
            string result = string.Empty;
            if (symbol.ContainingType != null)
                result += symbol.ContainingType.GetBridgeTypeNameIncludeContaining(bridgeNamespace) + '.';

            return result + symbol.GetBridgeTypeName() + symbol.GetBridgeTypeArgumentsText(bridgeNamespace);
        }

        public static string GetBridgeTypeFullName(this INamedTypeSymbol symbol, string bridgeNamespace) => $"global::{symbol.GetBridgeNamespace(bridgeNamespace)}.{symbol.GetBridgeTypeNameIncludeContaining(bridgeNamespace)}"; 
        
        public static string GetBridgeTypeName(this INamedTypeSymbol symbol) => symbol.Name + "Bridge";

        /// <summary>
        /// 주어진 타입 심볼에 대한 브릿지 타입의 완전한 이름을 반환합니다.<br/>
        /// 비공개 타입이 아닌경우, 주어진 타입의 전체 이름을 반환합니다.
        /// </summary>
        /// <param name="symbol">변환할 타입 심볼입니다.</param>
        /// <param name="bridgeNamespace">브릿지 네임스페이스</param>
        /// <returns>브릿지 타입의 완전한 이름 또는 원본 타입의 이름입니다.</returns>
        public static string GetTypeNameOrBridgeName(this ITypeSymbol symbol, string bridgeNamespace)
        {
            if (symbol.IsNonPublicMember() && symbol.TypeKind != TypeKind.Delegate && symbol is INamedTypeSymbol namedTypeSymbol)
                return namedTypeSymbol.GetBridgeTypeFullName(bridgeNamespace);

            return symbol switch
            {
                IArrayTypeSymbol arrayTypeSymbol => $"{arrayTypeSymbol.ElementType.GetTypeNameOrBridgeName(bridgeNamespace)}[]", // 배열 타입 처리 (예: MyClass[] -> MyClassBridge[])
                IPointerTypeSymbol pointerTypeSymbol => $"{pointerTypeSymbol.PointedAtType.GetTypeNameOrBridgeName(bridgeNamespace)}*", // 포인터 타입 처리 (예: MyClass* -> MyClassBridge*)
                _ => symbol.GetFullTypeName(bridgeNamespace) // 그 외 기본 타입이나 다른 어셈블리 타입은 그대로 사용
            };
        }

        public static ITypeSymbol GetElementType(this ITypeSymbol symbol)
        {
            while (symbol is IArrayTypeSymbol arrayTypeSymbol)
                symbol = arrayTypeSymbol.ElementType;
            
            return symbol;
        }
        
        public static ITypeSymbol GetPointedAtType(this ITypeSymbol symbol)
        {
            while (symbol is IPointerTypeSymbol pointerTypeSymbol)
                symbol = pointerTypeSymbol.PointedAtType;
            
            return symbol;
        }

        public static INamedTypeSymbol? GetNamedTypeSymbol(this ITypeSymbol symbol)
        {
            symbol = symbol.GetElementType();
            symbol = symbol.GetPointedAtType();
            
            if (symbol is INamedTypeSymbol namedTypeSymbol)
                return namedTypeSymbol;

            return null;
        }

        public static IEnumerable<INamedTypeSymbol> GetContainingTypes(this INamedTypeSymbol? symbol)
        {
            while (symbol != null)
            {
                yield return symbol;
                symbol = symbol.ContainingType;
            }
        }
        
        public static string GetFullTypeName(this ITypeSymbol symbol, string bridgeNamespace, bool bridgeTypeArguments = true)
        {
            if (symbol is ITypeParameterSymbol)
                return symbol.Name;
            
            string? keyword = symbol.SpecialType switch
            {
                SpecialType.System_Boolean => "bool",
                SpecialType.System_SByte => "sbyte",
                SpecialType.System_Byte => "byte",
                SpecialType.System_Int16 => "short",
                SpecialType.System_UInt16 => "ushort",
                SpecialType.System_Int32 => "int",
                SpecialType.System_UInt32 => "uint",
                SpecialType.System_Int64 => "long",
                SpecialType.System_UInt64 => "ulong",
                SpecialType.System_Single => "float",
                SpecialType.System_Double => "double",
                SpecialType.System_Decimal => "decimal",
                SpecialType.System_IntPtr => "nint",
                SpecialType.System_UIntPtr => "nuint",
                SpecialType.System_Char => "char",
                SpecialType.System_String => "string",
                SpecialType.System_Object => "object",
                _ => null
            };

            if (keyword != null)
                return keyword;

            string nameSpace = symbol.ContainingNamespace != null ? $"{symbol.ContainingNamespace.ToDisplayString()}." : string.Empty;
            string post = string.Empty;
            
            while (symbol is IPointerTypeSymbol pointerTypeSymbol)
            {
                post += "*";
                symbol = pointerTypeSymbol.PointedAtType;
            }

            while (symbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                post += "[]";
                symbol = arrayTypeSymbol.ElementType;
            }
            
            return $"global::{nameSpace}{InternalGetFullTypeName(symbol, bridgeTypeArguments, bridgeNamespace)}{post}";

            static string InternalGetFullTypeName(ITypeSymbol symbol, bool bridgeTypeArguments, string bridgeNamespace)
            {
                string result = $"{symbol.Name}{(bridgeTypeArguments ? symbol.GetBridgeTypeArgumentsText(bridgeNamespace) : symbol.GetTypeArgumentsText(bridgeNamespace))}";
                if (symbol.ContainingType != null)
                    result = $"{InternalGetFullTypeName(symbol.ContainingType, bridgeTypeArguments, bridgeNamespace)}.{result}";
                
                return result;
            }
        }

        public static string GetFullTypeNameIncludeNullable(this ITypeSymbol symbol, string bridgeNamespace)
        { 
            string result = symbol.GetFullTypeName(bridgeNamespace);
            if (symbol.NullableAnnotation == NullableAnnotation.Annotated || symbol.SpecialType == SpecialType.System_Nullable_T)
                result += "?";
            
            return result;
        }
        
        /// <summary>
        /// 지정된 심볼이 Obsolete 특성을 가지고 있는지 확인하고, 메시지를 반환합니다.
        /// </summary>
        /// <param name="symbol">확인할 <see cref="ISymbol"/>입니다.</param>
        /// <param name="message">
        /// 심볼이 obsolete인 경우 메시지가 포함되고, 그렇지 않으면 <see langword="null"/>이 됩니다.
        /// </param>
        /// <returns>심볼이 obsolete이면 <see langword="true"/>를, 그렇지 않으면 <see langword="false"/>를 반환합니다.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="symbol"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public static bool IsObsolete(this ISymbol symbol, out string message)
        {
            // 심볼에 적용된 모든 특성을 가져옵니다.
            foreach (AttributeData attributeData in symbol.GetAttributes().Where(static x => x.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute"))
            {
                // Obsolete 특성의 생성자 인수를 확인합니다.
                // 첫 번째 인수는 보통 경고 메시지입니다.
                if (attributeData.ConstructorArguments.Length > 0 && attributeData.ConstructorArguments[0].Value is string obsoleteMessage)
                {
                    message = obsoleteMessage;
                    return true;
                }
            }

            message = string.Empty;
            return false;
        }

        public static string GetTypeParametersText(this ITypeSymbol symbol)
        {
            if (symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeParameters.Any())
                return "<" + string.Join(", ", namedTypeSymbol.TypeParameters.Select(static x => x.Name)) + ">";

            return string.Empty;
        }
        
        public static string GetBridgeTypeArgumentsText(this ITypeSymbol symbol, string bridgeNamespace)
        {
            if (symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeArguments.Any())
                return "<" + string.Join(", ", namedTypeSymbol.TypeArguments.Select(x => x.GetTypeNameOrBridgeName(bridgeNamespace))) + ">";
            
            return symbol.GetTypeParametersText();
        }
        
        public static string GetTypeArgumentsText(this ITypeSymbol symbol, string bridgeNamespace)
        {
            if (symbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T || symbol.OriginalDefinition.IsTupleType)
                return string.Empty;
            
            if (symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.TypeArguments.Any())
                return "<" + string.Join(", ", namedTypeSymbol.TypeArguments.Select(x => x.GetFullTypeName(bridgeNamespace))) + ">";
            
            return symbol.GetTypeParametersText();
        }
        
        public static string GetTypeParametersText(this IMethodSymbol symbol)
        {
            if (symbol.TypeParameters.Any())
                return "<" + string.Join(", ", symbol.TypeParameters.Select(static x => x.Name)) + ">";
            else
                return string.Empty;
        }
        
        public static string GetBridgeTypeArgumentsText(this IMethodSymbol symbol, string bridgeNamespace)
        {
            if (symbol.TypeArguments.Any())
                return "<" + string.Join(", ", symbol.TypeArguments.Select(x => x.GetTypeNameOrBridgeName(bridgeNamespace))) + ">";
            else
                return string.Empty;
        }

        public static string GetTypeDeclarationKindName(this INamedTypeSymbol symbol) => symbol.TypeKind switch
        {
            TypeKind.Class => "class",
            TypeKind.Interface => "interface",
            TypeKind.Delegate => "delegate",
            TypeKind.Enum => "enum",
            TypeKind.Struct => "struct",
            _ => string.Empty
        };

        public static string GetTypeDeclarationText(this INamedTypeSymbol symbol, string bridgeNamespace, bool forceStatic, bool partial)
        {
            string result = "public ";
            switch (symbol.TypeKind)
            {
                case TypeKind.Class:
                {
                    if (partial)
                        break;
                    
                    if (symbol.IsStatic || forceStatic)
                        result += "static ";
                    if (symbol.IsSealed && !forceStatic)
                        result += "sealed ";
                    break;
                }
                case TypeKind.Struct:
                {
                    if (partial)
                        break;
                    
                    if (symbol.IsRefLikeType)
                        result += "ref ";
                    break;
                }
            }

            if (!partial && symbol.IsUnsafe())
                result += "unsafe ";
            
            if (symbol.TypeKind != TypeKind.Enum)
                result += "partial ";
            
            result += $"{symbol.GetTypeDeclarationKindName()} {symbol.GetBridgeTypeName()}{symbol.GetTypeParametersText()}";
            if (!partial && symbol.EnumUnderlyingType != null)
            {
                string enumUnderlyingTypeName = symbol.EnumUnderlyingType.GetFullTypeName(bridgeNamespace);
                if (enumUnderlyingTypeName != "int")
                    result += $" : {symbol.EnumUnderlyingType.GetFullTypeName(bridgeNamespace)}";
            }
            
            return $"{result} {symbol.GetConstraintsText(bridgeNamespace)}";
        }

        /// <summary>
        /// 메소드의 반환 타입을 브릿지 타입으로 변환합니다.
        /// </summary>
        /// <param name="methodSymbol">변환할 메소드 심볼입니다.</param>
        /// <param name="bridgeNamespace">브릿지 네임스페이스</param>
        /// <returns>브릿지 타입의 완전한 이름 또는 원본 타입의 이름입니다.</returns>
        public static string GetMethodReturnTypeName(this IMethodSymbol methodSymbol, string bridgeNamespace)
        {
            if (methodSymbol.ReturnsVoid)
                return "void";
        
            return methodSymbol.ReturnType.GetTypeNameOrBridgeName(bridgeNamespace);
        }

        public static INamedTypeSymbol? GetPublicBaseType(this INamedTypeSymbol? typeSymbol)
        {
            while (typeSymbol != null)
            {
                if (!typeSymbol.IsNonPublicMember())
                    return typeSymbol;
                
                typeSymbol = typeSymbol.BaseType;
            }
            
            return null;
        }

        public static string GetConstraintsText(this ITypeSymbol symbol, string bridgeNamespace)
        {
            if (symbol is INamedTypeSymbol namedTypeSymbol)
                return string.Join(", ", namedTypeSymbol.TypeParameters.Select(x => x.GetConstraintsText(bridgeNamespace)).Where(static x => !string.IsNullOrEmpty(x)));
            
            return string.Empty;
        }
        
        public static string GetConstraintsText(this IMethodSymbol symbol, string bridgeNamespace) => string.Join(", ", symbol.TypeParameters.Select(x => x.GetConstraintsText(bridgeNamespace)).Where(static x => !string.IsNullOrEmpty(x)));

        /// <summary>
        /// 지정된 제네릭 타입 매개변수의 모든 제약 조건을 문자열로 반환합니다.
        /// </summary>
        /// <param name="symbol">확인할 <see cref="ITypeParameterSymbol"/>입니다.</param>
        /// <param name="bridgeNamespace">브릿지 네임스페이스</param>
        /// <returns>
        /// 모든 제약 조건을 포함하는 문자열을 반환합니다. 제약 조건이 없으면 빈 문자열을 반환합니다.
        /// </returns>
        public static string GetConstraintsText(this ITypeParameterSymbol symbol, string bridgeNamespace)
        {
            string result = string.Empty;
            bool any = false;
            if (symbol.HasUnmanagedTypeConstraint)
            {
                result += "unmanaged";
                if (any)
                    result += ", ";
                any = true;
            }
            else if (symbol.HasReferenceTypeConstraint)
            {
                if (any)
                    result += ", ";
                result += "class";
                any = true;
            }
            else if (symbol.HasValueTypeConstraint)
            {
                if (any)
                    result += ", ";
                result += "struct";
                any = true;
            }
            if (symbol.ConstraintTypes.Any())
            {
                if (any)
                    result += ", ";
                result += string.Join(", ", symbol.ConstraintTypes.Select(x => x.GetTypeNameOrBridgeName(bridgeNamespace)));
                any = true;
            }
            if (symbol.HasNotNullConstraint)
            {
                if (any)
                    result += ", ";
                result += "notnull";
                any = true;
            }
            if (symbol.HasConstructorConstraint)
            {
                if (any)
                    result += ", ";
                result += "new()";
                any = true;
            }

            if (!any)
                return string.Empty;

            return $"where {symbol.Name} : {result}";
        }

        /// <summary>
        /// 원본 타입의 값 엑세스 텍스트를 가능하면 브릿지 타입의 엑세스로 변환합니다.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="bridgeNamespace"></param>
        /// <param name="valueAccessText"></param>
        /// <returns></returns>
        public static string ValueAccessToBridgeAccess(this ITypeSymbol symbol, string bridgeNamespace, string valueAccessText)
        {
            if (symbol.IsKeyValuePairEnumerable(out ITypeSymbol? keyType, out ITypeSymbol? valueType) && (keyType.IsNonPublicMember() || valueType.IsNonPublicMember()))
                return symbol.KeyValuePairEnumerableToDictionary($"global::System.Linq.Enumerable.Select({valueAccessText}, static x => new global::System.Collections.Generic.KeyValuePair<{keyType.GetTypeNameOrBridgeName(bridgeNamespace)}, {valueType.GetTypeNameOrBridgeName(bridgeNamespace)}>({keyType.ValueAccessToBridgeAccess(bridgeNamespace, "x.Key")}, {valueType.ValueAccessToBridgeAccess(bridgeNamespace, "x.Value")}))");
            if (symbol.IsEnumerable(out ITypeSymbol? elementType) && elementType.IsNonPublicMember())
                return symbol.EnumerableToList(bridgeNamespace, $"global::System.Linq.Enumerable.Select({valueAccessText}, static x => {elementType.ValueAccessToBridgeAccess(bridgeNamespace, "x")})");

            if (symbol is not INamedTypeSymbol namedTypeSymbol || !symbol.IsNonPublicMember() || namedTypeSymbol.TypeKind == TypeKind.Delegate)
                return valueAccessText;
            
            string bridgeTypeName = namedTypeSymbol.GetBridgeTypeFullName(bridgeNamespace);
            if (symbol.TypeKind == TypeKind.Enum)
                return $"({bridgeTypeName})(int){valueAccessText}";
            
            return $"{bridgeTypeName}.__GetInstanceFrom({valueAccessText})";
        }

        /// <summary>
        /// 브릿지 타입의 엑세스 텍스트를 가능하면 원본 타입의 값 엑세스 테스트로 변환합니다.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="bridgeNamespace"></param>
        /// <param name="bridgeValueAccessText"></param>
        /// <returns></returns>
        public static string BridgeAccessToValueAccess(this ITypeSymbol symbol, string bridgeNamespace, string bridgeValueAccessText)
        {
            if (symbol.IsKeyValuePairEnumerable(out ITypeSymbol? keyType, out ITypeSymbol? valueType) && (keyType.IsNonPublicMember() || valueType.IsNonPublicMember()))
                return symbol.KeyValuePairEnumerableToDictionary($"global::System.Linq.Enumerable.Select({bridgeValueAccessText}, static x => new global::System.Collections.Generic.KeyValuePair<{keyType.GetFullTypeName(bridgeNamespace)}, {valueType.GetFullTypeName(bridgeNamespace)}>({keyType.BridgeAccessToValueAccess(bridgeNamespace, "x.Key")}, {valueType.BridgeAccessToValueAccess(bridgeNamespace, "x.Value")}))");
            if (symbol.IsEnumerable(out ITypeSymbol? elementType) && elementType.IsNonPublicMember())
                return symbol.EnumerableToList(bridgeNamespace, $"global::System.Linq.Enumerable.Select({bridgeValueAccessText}, static x => {elementType.BridgeAccessToValueAccess(bridgeNamespace, "x")})", false);

            if (symbol is not INamedTypeSymbol namedTypeSymbol || !symbol.IsNonPublicMember() || namedTypeSymbol.TypeKind == TypeKind.Delegate)
                return bridgeValueAccessText;
            
            string typeName = namedTypeSymbol.GetFullTypeName(bridgeNamespace, false);
            if (symbol.TypeKind == TypeKind.Enum)
                return $"({typeName})(int){bridgeValueAccessText}";
            
            return $"({symbol.GetFullTypeName(bridgeNamespace, false)}){bridgeValueAccessText}.__instance";
        }
        
        public static string GetParameterText(this IEnumerable<IParameterSymbol> parameterSymbols, string bridgeNamespace) => string.Join(", ", parameterSymbols.Select(x => x.GetParameterText(bridgeNamespace)));

        /// <summary>
        /// 파라미터를 코드 텍스트로 변환합니다.
        /// </summary>
        /// <param name="parameterSymbol">변환할 파라미터 심볼입니다.</param>
        /// <param name="bridgeNamespace">브릿지 네임스페이스</param>
        /// <returns>this, params, ref, out, in 키워드 및 선택형을 적절하게 붙여줍니다</returns>
        public static string GetParameterText(this IParameterSymbol parameterSymbol, string bridgeNamespace)
        {
            string keyword = string.Empty;
            if (parameterSymbol.IsOptional && !parameterSymbol.HasExplicitDefaultValue)
                keyword += "[global::System.Runtime.InteropServices.OptionalAttribute] ";
            if (parameterSymbol.IsThis)
                keyword += "this ";
            if (parameterSymbol.IsParams)
                keyword += "params ";
            switch (parameterSymbol.RefKind)
            {
                case RefKind.Ref:
                {
                    keyword += "ref ";
                    break;
                }
                case RefKind.Out:
                {
                    keyword += "out ";
                    break;
                }
                case RefKind.In:
                {
                    keyword += "in ";
                    break;
                }
                case RefKind.RefReadOnlyParameter:
                {
                    keyword += "ref readonly ";
                    break;
                }
            }

            keyword += $"{parameterSymbol.Type.GetTypeNameOrBridgeName(bridgeNamespace)} {parameterSymbol.GetEscapeName()}";
            if (parameterSymbol.HasExplicitDefaultValue)
            {
                string value = parameterSymbol.ExplicitDefaultValue switch
                {
                    bool boolValue => boolValue ? "true" : "false",
                    uint uintValue => $"{uintValue}u",
                    long longValue => $"{longValue}L",
                    ulong ulongValue => $"{ulongValue}ul",
                    float floatValue when float.IsNaN(floatValue) => "float.NaN",
                    float floatValue when float.IsNegativeInfinity(floatValue) => "float.NegativeInfinity",
                    float floatValue when float.IsPositiveInfinity(floatValue) => "float.PositiveInfinity",
                    float floatValue when floatValue <= float.Epsilon => "float.Epsilon",
                    float floatValue => $"{floatValue}f",
                    double doubleValue when double.IsNaN(doubleValue) => "double.NaN",
                    double doubleValue when double.IsNegativeInfinity(doubleValue) => "double.NegativeInfinity",
                    double doubleValue when double.IsPositiveInfinity(doubleValue) => "double.PositiveInfinity",
                    double doubleValue when doubleValue <= double.Epsilon => "double.Epsilon",
                    double doubleValue => $"{doubleValue}d",
                    decimal decimalValue => $"{decimalValue}m",
                    char charValue => $"'{charValue}'",
                    string stringValue => $"\"{stringValue}\"",
                    _ => parameterSymbol.ExplicitDefaultValue?.ToString() ?? (parameterSymbol.Type is ITypeParameterSymbol ? "default" : "null")
                };

                if (parameterSymbol.Type.TypeKind == TypeKind.Enum)
                    value = $"({parameterSymbol.Type.GetTypeNameOrBridgeName(bridgeNamespace)}){value}";

                keyword += $" = {value}";
            }

            return keyword;
        }
        
        public static string GetCallParameterText(this IEnumerable<IParameterSymbol> parameterSymbols, string bridgeNamespace) => string.Join(", ", parameterSymbols.Select(x => x.GetCallParameterText(bridgeNamespace)));

        /// <summary>
        /// 콜 파라미터를 코드 텍스트로 변환합니다.
        /// </summary>
        /// <param name="parameterSymbol">변환할 파라미터 심볼입니다.</param>
        /// <param name="bridgeNamespace">브릿지 네임스페이스</param>
        /// <returns>비공개 타입에 ref, out, in 키워드가 붙을 경우, 파라미터 이름은 __로 시작하고, 키워드가 없을 경우엔 .__instance가 접미사로 붙습니다.</returns>
        public static string GetCallParameterText(this IParameterSymbol parameterSymbol, string bridgeNamespace)
        {
            bool isNonPublic = parameterSymbol.Type.IsNonPublicMember();
            string result = string.Empty;
            
            switch (parameterSymbol.RefKind)
            {
                case RefKind.None:
                    return parameterSymbol.Type.BridgeAccessToValueAccess(bridgeNamespace, parameterSymbol.GetEscapeName());
                case RefKind.Ref:
                {
                    result += "ref ";
                    break;
                }
                case RefKind.Out:
                {
                    result += "out ";
                    break;
                }
                case RefKind.In:
                {
                    result += "in ";
                    break;
                }
                case RefKind.RefReadOnlyParameter:
                {
                    result += "ref readonly ";
                    break;
                }
            }
            
            if (parameterSymbol.Type.TypeKind != TypeKind.TypeParameter && isNonPublic)
                return result + "__" + parameterSymbol.Name;

            return result + parameterSymbol.GetEscapeName();
        }

        /// <summary>
        /// ISymbol의 이름을 가져옵니다.<br/>
        /// 이름이 C# 예약 키워드일 경우, <c>@</c> 접두사를 붙여 이스케이프합니다.
        /// </summary>
        /// <param name="symbol">이름을 가져올 <see cref="ISymbol"/>입니다.</param>
        /// <returns>이스케이프된 심볼 이름입니다.</returns>
        public static string GetEscapeName(this ISymbol symbol)
        {
            string name = symbol.Name;
            if (string.IsNullOrEmpty(name))
                return name;

            return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
        }
    }
}