#nullable enable

namespace RuniOS.CodeAnalysis.Tests;

[Trait("Category", "Contract")]
public sealed class TypeSyntaxSerializerExactTests
{
    [Theory]
    [MemberData(nameof(SerializationCases.Types), MemberType = typeof(SerializationCases))]
    public void Canonical_syntax_preserves_the_documented_contract(string input, string expected) =>
        TestCompilation.Check(input, expected, SerializationCases.Declarations);

    [Theory]
    [MemberData(nameof(SerializationCases.TypeOfTypes), MemberType = typeof(SerializationCases))]
    public void Void_and_unbound_types_use_a_legal_typeof_site(string input, string expected) =>
        TestCompilation.Check(input, expected, SerializationCases.Declarations, useTypeOf: true);

    [Theory]
    [MemberData(nameof(SerializationCases.TypeParameters), MemberType = typeof(SerializationCases))]
    public void Type_parameters_preserve_constraints_and_annotations(string input, string constraints) =>
        TestCompilation.Check(input, input, holder: "public unsafe class SerializerTestHost<T> " + constraints);

    [Fact]
    public void Escaped_type_parameter_keeps_its_identifier() =>
        TestCompilation.Check("@class?", "@class?", holder: "public unsafe class SerializerTestHost<@class> where @class : class");

    [Fact]
    public void Alias_is_replaced_by_the_underlying_type_identity() =>
        TestCompilation.Check("Alias", "global::Fixture.Leaf", "using Alias = Fixture.Leaf;\n" + SerializationCases.Declarations);

    [Fact]
    public void Global_namespace_type_is_qualified() =>
        TestCompilation.Check("GlobalType", "global::GlobalType", "public class GlobalType { }");

    [Theory]
    [InlineData(8)]
    [InlineData(15)]
    [InlineData(20)]
    public void Long_tuple_preserves_every_name_including_rest_elements(int count)
    {
        string tuple = "(" + string.Join(", ", Enumerable.Range(0, count).Select(i => $"int item{i}")) + ")";
        TestCompilation.Check(tuple, tuple);
    }
}
