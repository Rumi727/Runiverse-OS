#nullable enable

namespace RuniOS.CodeAnalysis.Tests;

[Trait("Category", "Contract")]
public sealed class TypeSyntaxSerializerPointerTests
{
    public static TheoryData<string> Types => new()
    {
        "int*", "int**", "int***", "void*", "void**", "byte*", "char*", "double*",
        "int*[]", "int**[]", "int*[,]", "int*[]?", "int*[][,]",
        "(int x, long y)*", "delegate*<void>*"
    };

    [Theory]
    [MemberData(nameof(Types))]
    public void Pointer_shape_round_trips_and_emits(string input) => TestCompilation.Check(input, input);
}
