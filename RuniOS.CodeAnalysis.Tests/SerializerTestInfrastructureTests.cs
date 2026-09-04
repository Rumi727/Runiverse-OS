#nullable enable

using Microsoft.CodeAnalysis;
using System.Reflection.Metadata;
using Xunit.Sdk;

namespace RuniOS.CodeAnalysis.Tests;

[Trait("Category", "Infrastructure")]
public sealed class SerializerTestInfrastructureTests
{
    [Theory]
    [MemberData(nameof(SerializationCases.Types), MemberType = typeof(SerializationCases))]
    public void Input_and_expected_types_are_valid_and_semantically_equivalent_without_the_serializer(string input, string expected) =>
        AssertPair(TestCompilation.BindFields(input, expected, SerializationCases.Declarations));

    [Theory]
    [MemberData(nameof(SerializationCases.TypeOfTypes), MemberType = typeof(SerializationCases))]
    public void Typeof_fixtures_bind_and_emit_without_the_serializer(string input, string expected) =>
        AssertPair(TestCompilation.BindTypesOf(input, expected, SerializationCases.Declarations));

    [Theory]
    [MemberData(nameof(TypeSyntaxSerializerFunctionPointerTests.Types), MemberType = typeof(TypeSyntaxSerializerFunctionPointerTests))]
    public void Function_pointer_fixtures_and_expected_syntax_are_supported_by_the_pinned_compiler(string input, string expected) =>
        AssertPair(TestCompilation.BindFields(input, expected));

    [Theory]
    [MemberData(nameof(TypeSyntaxSerializerPointerTests.Types), MemberType = typeof(TypeSyntaxSerializerPointerTests))]
    public void Pointer_fixtures_bind_and_emit(string input) => AssertPair(TestCompilation.BindFields(input, input));

    [Theory]
    [MemberData(nameof(SerializationCases.TypeParameters), MemberType = typeof(SerializationCases))]
    public void Type_parameter_fixtures_bind_and_emit(string input, string constraints) =>
        AssertPair(TestCompilation.BindFields(input, input, holder: "public unsafe class SerializerTestHost<T> " + constraints));

    [Theory]
    [MemberData(nameof(TypeSyntaxSerializerRoundTripTests.GeneratedTypes), MemberType = typeof(TypeSyntaxSerializerRoundTripTests))]
    public void Generated_combinations_are_valid_CSharp(string input)
    {
        BoundType bound = TestCompilation.BindField(input, SerializationCases.Declarations);
        TestCompilation.AssertNoErrors(bound.Compilation);
        TestCompilation.AssertEmits(bound.Compilation);
    }

    [Theory]
    [InlineData("dynamic", "object", "TypeKind")]
    [InlineData("string?", "string", "NullableAnnotation")]
    [InlineData("List<string?>", "List<string>", "typeArgument[0]")]
    [InlineData("Fixture.Outer<dynamic>.Inner<int>", "Fixture.Outer<object>.Inner<int>", "containingType")]
    [InlineData("Fixture.Outer<string?>.Leaf", "Fixture.Outer<string>.Leaf", "containingType")]
    [InlineData("(int first, int second)", "(int x, int y)", "tupleElement[0].Name")]
    [InlineData("(int first, dynamic second)", "(int first, object second)", "TypeKind")]
    [InlineData("int[,][]", "int[][,]", "Rank")]
    [InlineData("int*", "long*", "pointedAt")]
    [InlineData("delegate*<ref int, void>", "delegate*<out int, void>", "RefKind")]
    [InlineData("delegate*<ref int>", "delegate*<ref readonly int>", "RefKind")]
    [InlineData("delegate* unmanaged[Cdecl]<void>", "delegate* unmanaged[Stdcall]<void>", "CallingConvention")]
    public void Structural_comparator_detects_deliberate_semantic_mutations(string original, string changed, string expectedPath)
    {
        BoundTypePair pair = TestCompilation.BindFields(original, changed, SerializationCases.Declarations);
        TestCompilation.AssertNoErrors(pair.Compilation);
        XunitException error = Assert.Throws<XunitException>(() => TypeSymbolAssertions.Equivalent(pair.Original, pair.RoundTrip));
        Assert.Contains(expectedPath, error.Message);
    }

    [Fact]
    public void Nullable_setting_changes_the_actual_binding_context()
    {
        BoundType enabled = TestCompilation.BindField("string", nullable: NullableContextOptions.Enable);
        BoundType disabled = TestCompilation.BindField("string", nullable: NullableContextOptions.Disable);
        TestCompilation.AssertNoErrors(enabled.Compilation);
        TestCompilation.AssertNoErrors(disabled.Compilation);
        Assert.Equal(NullableAnnotation.NotAnnotated, enabled.Type.NullableAnnotation);
        Assert.Equal(NullableAnnotation.None, disabled.Type.NullableAnnotation);
    }

    [Theory]
    [InlineData("IntPtr", "nint")]
    [InlineData("UIntPtr", "nuint")]
    public void Runtime_integer_types_are_native_integer_symbols_in_the_reference_environment(string input, string alias)
    {
        BoundTypePair pair = TestCompilation.BindFields(input, alias);
        TestCompilation.AssertNoErrors(pair.Compilation);
        Assert.True(Assert.IsAssignableFrom<INamedTypeSymbol>(pair.Original).IsNativeIntegerType);
        Assert.True(Assert.IsAssignableFrom<INamedTypeSymbol>(pair.RoundTrip).IsNativeIntegerType);
        AssertPair(pair);
    }

    [Fact]
    public void Metadata_fixture_contains_the_intended_array_shapes_and_invalid_names()
    {
        var compilation = MetadataFixture.Compilation();
        TestCompilation.AssertNoErrors(compilation);
        foreach (string name in new[] { "Fixture.Bad-Name", "Fixture.Bad-Container", "Bad-Namespace.GoodName" })
            Assert.Equal(SpecialType.System_Object, MetadataFixture.Type(compilation, name).BaseType!.SpecialType);
        var vector = Assert.IsAssignableFrom<IArrayTypeSymbol>(MetadataFixture.Field(compilation, "Vector"));
        var nonSz = Assert.IsAssignableFrom<IArrayTypeSymbol>(MetadataFixture.Field(compilation, "NonSz"));
        var matrix = Assert.IsAssignableFrom<IArrayTypeSymbol>(MetadataFixture.Field(compilation, "Matrix"));
        Assert.True(vector.IsSZArray);
        Assert.False(nonSz.IsSZArray);
        Assert.False(matrix.IsSZArray);
        Assert.Equal(1, vector.Rank);
        Assert.Equal(1, nonSz.Rank);
        Assert.Equal(2, matrix.Rank);
        Assert.Equal(SpecialType.System_Int32, nonSz.ElementType.SpecialType);
        Assert.False(SymbolEqualityComparer.Default.Equals(vector, nonSz));
        XunitException shapeError = Assert.Throws<XunitException>(() => TypeSymbolAssertions.Equivalent(vector, nonSz));
        Assert.Contains("IsSZArray", shapeError.Message);
        var badArray = Assert.IsAssignableFrom<IArrayTypeSymbol>(MetadataFixture.Field(compilation, "NonSzBadElement"));
        Assert.Equal("Bad-Name", badArray.ElementType.Name);
        Assert.False(badArray.IsSZArray);
        Assert.Equal(1, badArray.Rank);
    }

    [Theory]
    [InlineData("Cdecl")]
    [InlineData("Stdcall")]
    [InlineData("Thiscall")]
    [InlineData("Fastcall")]
    public void Metadata_legacy_modopts_are_exposed_as_unmanaged_not_legacy_headers(string convention)
    {
        var pointer = Assert.IsAssignableFrom<IFunctionPointerTypeSymbol>(MetadataFixture.Field(MetadataFixture.Compilation(), "Unmanaged" + convention));
        Assert.Equal(SignatureCallingConvention.Unmanaged, pointer.Signature.CallingConvention);
        Assert.Equal("CallConv" + convention, Assert.Single(pointer.Signature.UnmanagedCallingConventionTypes).Name);
    }

    [Fact]
    public void Metadata_vararg_signature_is_decoded_as_vararg()
    {
        var pointer = Assert.IsAssignableFrom<IFunctionPointerTypeSymbol>(MetadataFixture.Field(MetadataFixture.Compilation(), "VarArg"));
        Assert.Equal(SignatureCallingConvention.VarArgs, pointer.Signature.CallingConvention);
        Assert.Equal(SpecialType.System_Int32, pointer.Signature.ReturnType.SpecialType);
        Assert.Equal(SpecialType.System_Int32, Assert.Single(pointer.Signature.Parameters).Type.SpecialType);
    }

    [Fact]
    public void Comparator_rejects_same_named_type_parameters_with_different_owners()
    {
        var compilation = TestCompilation.CreateCompilation("public class Owners { public void M<T>() { } public void N<T>() { } }");
        TestCompilation.AssertNoErrors(compilation);
        var owner = Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName("Owners"));
        var first = Assert.IsAssignableFrom<IMethodSymbol>(owner.GetMembers("M").Single()).TypeParameters.Single();
        var second = Assert.IsAssignableFrom<IMethodSymbol>(owner.GetMembers("N").Single()).TypeParameters.Single();
        XunitException error = Assert.Throws<XunitException>(() => TypeSymbolAssertions.Equivalent(first, second));
        Assert.Contains("owner", error.Message);
    }

    static void AssertPair(BoundTypePair pair)
    {
        TestCompilation.AssertNoErrors(pair.Compilation);
        TypeSymbolAssertions.Equivalent(pair.Original, pair.RoundTrip);
        Assert.True(SymbolEqualityComparer.IncludeNullability.Equals(pair.Original, pair.RoundTrip));
        TestCompilation.AssertEmits(pair.Compilation);
    }
}
