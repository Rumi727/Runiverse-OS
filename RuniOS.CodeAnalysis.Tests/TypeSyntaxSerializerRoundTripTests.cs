#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RuniOS.CodeAnalysis.Tests;

[Trait("Category", "Contract")]
public sealed class TypeSyntaxSerializerRoundTripTests
{
    [Theory]
    [MemberData(nameof(SerializationCases.Types), MemberType = typeof(SerializationCases))]
    public void Meaning_survives_even_when_exact_formatting_is_checked_separately(string input, string expected)
    {
        _ = expected;
        TestCompilation.Check(input, declarations: SerializationCases.Declarations);
    }

    [Theory]
    [InlineData("string")]
    [InlineData("string[][]")]
    [InlineData("List<string>")]
    [InlineData("Dictionary<string, List<object[]>>")]
    public void Oblivious_annotations_round_trip_in_a_disabled_nullable_context(string input) =>
        TestCompilation.Check(input, nullable: NullableContextOptions.Disable);

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(12)]
    [InlineData(20)]
    public void Deep_generic_nesting_keeps_every_argument(int depth)
    {
        string input = "int";
        string expected = "int";
        for (int i = 0; i < depth; i++)
        {
            input = "List<" + input + ">";
            expected = "global::System.Collections.Generic.List<" + expected + ">";
        }
        TestCompilation.Check(input, expected);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(32)]
    public void Deep_array_nesting_keeps_every_rank_and_annotation(int depth)
    {
        string input = "string?" + string.Concat(Enumerable.Repeat("[]?", depth));
        TestCompilation.Check(input, input);
    }

    public static IEnumerable<object[]> GeneratedTypes()
    {
        string[] leaves = { "int", "long", "string?", "dynamic", "Guid", "Fixture.Leaf", "(int x, int y)", "int?" };
        // Enumerate all 8 * 6 * 6 two-layer combinations, with no random seed or duplicates to lose.
        for (int leaf = 0; leaf < leaves.Length; leaf++)
        for (int inner = 0; inner < 6; inner++)
        for (int outer = 0; outer < 6; outer++)
            yield return new object[] { Wrap(Wrap(leaves[leaf], inner), outer) };
    }

    static string Wrap(string type, int choice) => choice switch
    {
        0 => type + "[]",
        1 => type + "[,]",
        2 => "List<" + type + ">",
        3 => "Dictionary<string, " + type + ">",
        4 => "(" + type + " item, string? tag)",
        5 => type + "[]?",
        _ => throw new ArgumentOutOfRangeException(nameof(choice))
    };

    [Theory]
    [MemberData(nameof(GeneratedTypes))]
    public void Deterministic_combinations_round_trip(string input) =>
        TestCompilation.Check(input, declarations: SerializationCases.Declarations);

    [Theory]
    [InlineData("T", "")]
    [InlineData("T?", "where T : class")]
    [InlineData("T?[]", "where T : struct")]
    [InlineData("T*", "where T : unmanaged")]
    public void Method_type_parameters_remain_bound_to_the_same_owner(string input, string constraints)
    {
        CSharpCompilation original = MethodFixture($"{input} original", constraints);
        TestCompilation.AssertNoErrors(original);
        IMethodSymbol method = Method(original);
        Assert.Equal(TypeParameterKind.Method, method.TypeParameters.Single().TypeParameterKind);
        string output = TestCompilation.Serialize(ParameterType(method.Parameters[0]));
        Assert.Equal(input, output);

        CSharpCompilation rebound = MethodFixture($"{input} original, {output} roundTrip", constraints);
        TestCompilation.AssertNoErrors(rebound);
        IMethodSymbol reboundMethod = Method(rebound);
        TypeSymbolAssertions.Equivalent(ParameterType(reboundMethod.Parameters[0]), ParameterType(reboundMethod.Parameters[1]));
        TestCompilation.AssertEmits(rebound);
    }

    [Fact]
    public void Global_type_does_not_rebind_to_a_shadow_type_at_the_destination()
    {
        BoundType original = TestCompilation.BindField("GlobalType", "public class GlobalType { }");
        TestCompilation.AssertNoErrors(original.Compilation);
        string output = TestCompilation.Serialize(original.Type);
        CSharpCompilation destination = TestCompilation.CreateCompilation($$"""
            public class GlobalType { }
            namespace Destination
            {
                public class GlobalType { }
                public class Consumer { public {{output}} Value; }
            }
            """);
        TestCompilation.AssertNoErrors(destination);
        INamedTypeSymbol consumer = Assert.IsAssignableFrom<INamedTypeSymbol>(destination.GetTypeByMetadataName("Destination.Consumer"));
        IFieldSymbol field = Assert.IsAssignableFrom<IFieldSymbol>(consumer.GetMembers("Value").Single());
        Assert.True(SymbolEqualityComparer.Default.Equals(destination.GetTypeByMetadataName("GlobalType"), field.Type),
            $"Output '{output}' rebound to '{field.Type}' in the destination namespace.");
        TestCompilation.AssertEmits(destination);
    }

    [Fact]
    public void Repeated_parallel_calls_do_not_share_rendering_state()
    {
        string[] inputs = { "int", "string?[]?", "(int x, long y)", "delegate*<int, void>", "int***" };
        ITypeSymbol[] symbols = inputs.Select(input =>
        {
            BoundType bound = TestCompilation.BindField(input);
            TestCompilation.AssertNoErrors(bound.Compilation);
            return bound.Type;
        }).ToArray();
        Parallel.For(0, 200, i =>
        {
            int index = i % inputs.Length;
            Assert.Equal(inputs[index], TestCompilation.Serialize(symbols[index]));
        });
    }

    static CSharpCompilation MethodFixture(string parameters, string constraints) => TestCompilation.CreateCompilation($$"""
        public unsafe class MethodHost
        {
            public static void M<T>({{parameters}}) {{constraints}} { }
        }
        """);

    static IMethodSymbol Method(CSharpCompilation compilation) => Assert.IsAssignableFrom<IMethodSymbol>(
        Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName("MethodHost")).GetMembers("M").Single());

    static ITypeSymbol ParameterType(IParameterSymbol parameter) => parameter.Type.WithNullableAnnotation(parameter.NullableAnnotation);
}
