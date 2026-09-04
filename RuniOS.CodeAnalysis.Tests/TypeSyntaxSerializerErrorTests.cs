#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RuniOS.CodeAnalysis.Generators;
using System.Reflection.Metadata;

namespace RuniOS.CodeAnalysis.Tests;

[Trait("Category", "Contract")]
public sealed class TypeSyntaxSerializerErrorTests
{
    [Fact]
    public void Declaration_errors_do_not_automatically_mean_serialization_errors()
    {
        BoundType bound = TestCompilation.BindField("Missing");
        Assert.Contains(bound.Compilation.GetDiagnostics(), d => d.Id == "CS0246");
        Assert.IsAssignableFrom<IErrorTypeSymbol>(bound.Type);
        var errors = bound.Type.TrySerialize(out string output);
        Assert.True(errors.isSuccess, TestCompilation.FormatErrors(errors));
        Assert.False(SyntaxFactory.ParseTypeName(output).ContainsDiagnostics, output);
        BoundType rebound = TestCompilation.BindField(output);
        Assert.IsAssignableFrom<IErrorTypeSymbol>(rebound.Type);
        Assert.Equal(bound.Type.Name, rebound.Type.Name);
    }

    [Fact]
    public void Anonymous_type_reports_a_representational_error()
    {
        BoundType bound = TestCompilation.BindExpression("new { Number = 1, Name = \"anonymous\" }");
        TestCompilation.AssertNoErrors(bound.Compilation);
        Assert.True(bound.Type.IsAnonymousType);
        var errors = bound.Type.TrySerialize(out _);
        Assert.False(errors.isSuccess);
        Assert.Contains(errors, error =>
            (error.error == TypeSyntaxSerializer.SerializeError.unrepresentableType && ReferenceEquals(error.problematicObject, bound.Type)) ||
            (error.error == TypeSyntaxSerializer.SerializeError.invalidIdentifier && Equals(error.problematicObject, bound.Type.Name)));
    }

    [Fact]
    public void Accessibility_is_the_callers_responsibility()
    {
        var compilation = TestCompilation.CreateCompilation("namespace Fixture { public class Owner { private class Hidden { } } }");
        TestCompilation.AssertNoErrors(compilation);
        INamedTypeSymbol hidden = Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName("Fixture.Owner+Hidden"));
        Assert.Equal(Accessibility.Private, hidden.DeclaredAccessibility);
        var errors = hidden.TrySerialize(out string output);
        Assert.True(errors.isSuccess, TestCompilation.FormatErrors(errors));
        Assert.Equal("global::Fixture.Owner.Hidden", output);
    }

    [Theory]
    [InlineData("Fixture.Bad-Name", "Bad-Name")]
    [InlineData("Fixture.Bad-Container", "Bad-Container")]
    [InlineData("Bad-Namespace.GoodName", "Bad-Namespace")]
    public void Invalid_metadata_identifiers_report_the_exact_bad_name(string metadataName, string badName)
    {
        var compilation = MetadataFixture.Compilation();
        INamedTypeSymbol type = MetadataFixture.Type(compilation, metadataName);
        var errors = type.TrySerialize(out _);
        var error = Assert.Single(errors);
        Assert.Equal(TypeSyntaxSerializer.SerializeError.invalidIdentifier, error.error);
        Assert.Equal(badName, error.problematicObject);
    }

    [Fact]
    public void Non_vector_rank_one_array_is_not_silently_replaced_by_a_vector()
    {
        var compilation = MetadataFixture.Compilation();
        var array = Assert.IsAssignableFrom<IArrayTypeSymbol>(MetadataFixture.Field(compilation, "NonSz"));
        Assert.Equal(1, array.Rank);
        Assert.False(array.IsSZArray);
        var errors = array.TrySerialize(out string partial);
        var error = Assert.Single(errors);
        Assert.Equal(TypeSyntaxSerializer.SerializeError.unsupportedArrayType, error.error);
        Assert.Same(array, error.problematicObject);
        // The surviving element is renderable; the API permits partial syntax on failure.
        Assert.Contains("int", partial);
    }

    [Theory]
    [InlineData("Vector", "int[]")]
    [InlineData("Matrix", "int[,]")]
    public void Representable_metadata_arrays_round_trip(string field, string expected)
    {
        var compilation = MetadataFixture.Compilation();
        ITypeSymbol original = MetadataFixture.Field(compilation, field);
        Assert.Equal(expected, TestCompilation.Serialize(original));
        BoundType rebound = TestCompilation.BindField(expected, nullable: NullableContextOptions.Disable);
        TestCompilation.AssertNoErrors(rebound.Compilation);
        TypeSymbolAssertions.Equivalent(original, rebound.Type);
        TestCompilation.AssertEmits(rebound.Compilation);
    }

    [Fact]
    public void Vararg_function_pointer_reports_unsupported_signature()
    {
        var compilation = MetadataFixture.Compilation();
        var pointer = Assert.IsAssignableFrom<IFunctionPointerTypeSymbol>(MetadataFixture.Field(compilation, "VarArg"));
        Assert.Equal(SignatureCallingConvention.VarArgs, pointer.Signature.CallingConvention);
        var errors = pointer.TrySerialize(out string partial);
        var error = Assert.Single(errors);
        Assert.Equal(TypeSyntaxSerializer.SerializeError.unsupportedFunctionPointer, error.error);
        Assert.Same(pointer.Signature, error.problematicObject);
        Assert.Contains("delegate*", partial);
    }

    [Theory]
    [InlineData("Cdecl")]
    [InlineData("Stdcall")]
    [InlineData("Thiscall")]
    [InlineData("Fastcall")]
    public void Unmanaged_with_one_legacy_modifier_is_not_equivalent_to_a_legacy_header(string convention)
    {
        var compilation = MetadataFixture.Compilation();
        var pointer = Assert.IsAssignableFrom<IFunctionPointerTypeSymbol>(MetadataFixture.Field(compilation, "Unmanaged" + convention));
        Assert.Equal(SignatureCallingConvention.Unmanaged, pointer.Signature.CallingConvention);
        Assert.Equal("CallConv" + convention, Assert.Single(pointer.Signature.UnmanagedCallingConventionTypes).Name);
        var errors = pointer.TrySerialize(out _);
        var error = Assert.Single(errors);
        Assert.Equal(TypeSyntaxSerializer.SerializeError.unsupportedFunctionPointer, error.error);
        Assert.Same(pointer.Signature, error.problematicObject);
    }

    [Fact]
    public void Errors_in_separate_generic_arguments_accumulate_left_to_right()
    {
        var compilation = MetadataFixture.Compilation();
        INamedTypeSymbol definition = MetadataFixture.Type(compilation, "System.Collections.Generic.Dictionary`2");
        INamedTypeSymbol first = MetadataFixture.Type(compilation, "Fixture.Bad-Name");
        INamedTypeSymbol second = MetadataFixture.Type(compilation, "Fixture.Bad-Container");
        var errors = definition.Construct(first, second).TrySerialize(out string partial);
        Assert.False(errors.isSuccess);
        Assert.Collection(errors,
            error => AssertBadName(error, "Bad-Name"),
            error => AssertBadName(error, "Bad-Container"));
        Assert.Equal(2, errors.count);
        Assert.Contains(", ", partial);
        Assert.EndsWith(">", partial);
    }

    [Fact]
    public void Container_and_element_errors_both_survive_recursive_rendering()
    {
        var compilation = MetadataFixture.Compilation();
        var array = Assert.IsAssignableFrom<IArrayTypeSymbol>(MetadataFixture.Field(compilation, "NonSzBadElement"));
        Assert.False(array.IsSZArray);
        Assert.Equal("Bad-Name", array.ElementType.Name);
        var errors = array.TrySerialize(out _);
        Assert.Collection(errors,
            error =>
            {
                Assert.Equal(TypeSyntaxSerializer.SerializeError.unsupportedArrayType, error.error);
                Assert.Same(array, error.problematicObject);
            },
            error => AssertBadName(error, "Bad-Name"));
    }

    static void AssertBadName(TypeSyntaxSerializer.SerializeErrorResult error, string name)
    {
        Assert.Equal(TypeSyntaxSerializer.SerializeError.invalidIdentifier, error.error);
        Assert.Equal(name, error.problematicObject);
    }
}
