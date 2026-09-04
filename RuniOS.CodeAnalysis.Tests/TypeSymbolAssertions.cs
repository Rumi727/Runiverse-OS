#nullable enable

using Microsoft.CodeAnalysis;
using Xunit.Sdk;

namespace RuniOS.CodeAnalysis.Tests;

internal static class TypeSymbolAssertions
{
    public static void Equivalent(ITypeSymbol expected, ITypeSymbol actual, string path = "root")
    {
        Equal(expected.TypeKind, actual.TypeKind, path + ".TypeKind");
        Equal(expected.NullableAnnotation, actual.NullableAnnotation, path + ".NullableAnnotation");

        switch (expected)
        {
            case IDynamicTypeSymbol:
                Require<IDynamicTypeSymbol>(actual, path);
                break;
            case IArrayTypeSymbol array:
            {
                var other = Require<IArrayTypeSymbol>(actual, path);
                Equal(array.Rank, other.Rank, path + ".Rank");
                Equal(array.IsSZArray, other.IsSZArray, path + ".IsSZArray");
                Equivalent(array.ElementType.WithNullableAnnotation(array.ElementNullableAnnotation),
                    other.ElementType.WithNullableAnnotation(other.ElementNullableAnnotation), path + ".element");
                break;
            }
            case IPointerTypeSymbol pointer:
                Equivalent(pointer.PointedAtType, Require<IPointerTypeSymbol>(actual, path).PointedAtType, path + ".pointedAt");
                break;
            case IFunctionPointerTypeSymbol functionPointer:
                FunctionPointer(functionPointer.Signature, Require<IFunctionPointerTypeSymbol>(actual, path).Signature, path);
                break;
            case ITypeParameterSymbol parameter:
            {
                var other = Require<ITypeParameterSymbol>(actual, path);
                Equal(parameter.Name, other.Name, path + ".Name");
                Equal(parameter.Ordinal, other.Ordinal, path + ".Ordinal");
                Equal(parameter.TypeParameterKind, other.TypeParameterKind, path + ".TypeParameterKind");
                Equal(parameter.Variance, other.Variance, path + ".Variance");
                Equal(parameter.HasReferenceTypeConstraint, other.HasReferenceTypeConstraint, path + ".HasReferenceTypeConstraint");
                Equal(parameter.ReferenceTypeConstraintNullableAnnotation, other.ReferenceTypeConstraintNullableAnnotation, path + ".ReferenceTypeConstraintNullableAnnotation");
                Equal(parameter.HasValueTypeConstraint, other.HasValueTypeConstraint, path + ".HasValueTypeConstraint");
                Equal(parameter.HasUnmanagedTypeConstraint, other.HasUnmanagedTypeConstraint, path + ".HasUnmanagedTypeConstraint");
                Equal(parameter.HasNotNullConstraint, other.HasNotNullConstraint, path + ".HasNotNullConstraint");
                Equal(parameter.HasConstructorConstraint, other.HasConstructorConstraint, path + ".HasConstructorConstraint");
                SameSymbol(parameter.ContainingSymbol, other.ContainingSymbol, path + ".owner");
                Equal(parameter.ConstraintTypes.Length, other.ConstraintTypes.Length, path + ".constraints.Count");
                for (int i = 0; i < parameter.ConstraintTypes.Length; i++)
                    SameSymbol(parameter.ConstraintTypes[i], other.ConstraintTypes[i], path + $".constraint[{i}]");
                break;
            }
            case INamedTypeSymbol named:
                Named(named, Require<INamedTypeSymbol>(actual, path), path);
                break;
            default:
                throw new XunitException($"{path}: comparator does not support {expected.TypeKind}.");
        }
    }

    static void Named(INamedTypeSymbol expected, INamedTypeSymbol actual, string path)
    {
        Equal(expected.SpecialType, actual.SpecialType, path + ".SpecialType");
        Equal(expected.MetadataName, actual.MetadataName, path + ".MetadataName");
        Equal(expected.ContainingNamespace.ToDisplayString(), actual.ContainingNamespace.ToDisplayString(), path + ".namespace");
        Equal(expected.ContainingAssembly?.Identity, actual.ContainingAssembly?.Identity, path + ".assembly");
        Equal(expected.IsNativeIntegerType, actual.IsNativeIntegerType, path + ".IsNativeIntegerType");
        Equal(expected.IsTupleType, actual.IsTupleType, path + ".IsTupleType");
        Equal(expected.IsUnboundGenericType, actual.IsUnboundGenericType, path + ".IsUnboundGenericType");
        Equal(expected.Arity, actual.Arity, path + ".Arity");
        Equal(expected.ContainingType is null, actual.ContainingType is null, path + ".hasContainingType");
        if (expected.ContainingType is { } containing)
            Equivalent(containing, actual.ContainingType!, path + ".containingType");

        Equal(expected.TypeArguments.Length, actual.TypeArguments.Length, path + ".typeArguments.Count");
        if (!expected.IsUnboundGenericType)
        {
            for (int i = 0; i < expected.TypeArguments.Length; i++)
                Equivalent(expected.TypeArguments[i].WithNullableAnnotation(expected.TypeArgumentNullableAnnotations[i]),
                    actual.TypeArguments[i].WithNullableAnnotation(actual.TypeArgumentNullableAnnotations[i]), path + $".typeArgument[{i}]");
        }

        if (!expected.IsTupleType)
            return;

        Equal(expected.TupleElements.Length, actual.TupleElements.Length, path + ".tupleElements.Count");
        for (int i = 0; i < expected.TupleElements.Length; i++)
        {
            IFieldSymbol left = expected.TupleElements[i];
            IFieldSymbol right = actual.TupleElements[i];
            Equal(left.Name, right.Name, path + $".tupleElement[{i}].Name");
            Equivalent(left.Type.WithNullableAnnotation(left.NullableAnnotation),
                right.Type.WithNullableAnnotation(right.NullableAnnotation), path + $".tupleElement[{i}]");
        }
        SameSymbol(expected.TupleUnderlyingType, actual.TupleUnderlyingType, path + ".TupleUnderlyingType");
    }

    static void FunctionPointer(IMethodSymbol expected, IMethodSymbol actual, string path)
    {
        Equal(expected.CallingConvention, actual.CallingConvention, path + ".CallingConvention");
        Equal(expected.RefKind, actual.RefKind, path + ".return.RefKind");
        Equal(expected.ReturnsByRef, actual.ReturnsByRef, path + ".ReturnsByRef");
        Equal(expected.ReturnsByRefReadonly, actual.ReturnsByRefReadonly, path + ".ReturnsByRefReadonly");
        Equal(expected.UnmanagedCallingConventionTypes.Length, actual.UnmanagedCallingConventionTypes.Length, path + ".conventions.Count");
        for (int i = 0; i < expected.UnmanagedCallingConventionTypes.Length; i++)
            SameSymbol(expected.UnmanagedCallingConventionTypes[i], actual.UnmanagedCallingConventionTypes[i], path + $".convention[{i}]");
        Equal(expected.Parameters.Length, actual.Parameters.Length, path + ".parameters.Count");
        for (int i = 0; i < expected.Parameters.Length; i++)
        {
            IParameterSymbol left = expected.Parameters[i];
            IParameterSymbol right = actual.Parameters[i];
            Equal(left.RefKind, right.RefKind, path + $".parameter[{i}].RefKind");
            Equivalent(left.Type.WithNullableAnnotation(left.NullableAnnotation),
                right.Type.WithNullableAnnotation(right.NullableAnnotation), path + $".parameter[{i}]");
        }
        Equivalent(expected.ReturnType.WithNullableAnnotation(expected.ReturnNullableAnnotation),
            actual.ReturnType.WithNullableAnnotation(actual.ReturnNullableAnnotation), path + ".return");
    }

    static T Require<T>(object value, string path) where T : class => value as T
        ?? throw new XunitException($"{path}: expected {typeof(T).Name}, got {value.GetType().Name}.");

    static void SameSymbol(ISymbol? expected, ISymbol? actual, string path)
    {
        if (!SymbolEqualityComparer.IncludeNullability.Equals(expected, actual))
            throw new XunitException($"{path}: expected symbol {expected}, actual {actual}.");
    }

    static void Equal<T>(T expected, T actual, string path)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new XunitException($"{path}: expected {expected}, actual {actual}.");
    }
}
