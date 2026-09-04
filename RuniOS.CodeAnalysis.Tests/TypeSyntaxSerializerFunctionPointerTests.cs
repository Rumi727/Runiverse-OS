#nullable enable

namespace RuniOS.CodeAnalysis.Tests;

[Trait("Category", "Contract")]
public sealed class TypeSyntaxSerializerFunctionPointerTests
{
    public static TheoryData<string, string> Types => new()
    {
        { "delegate*<void>", "delegate*<void>" },
        { "delegate* managed<void>", "delegate*<void>" },
        { "delegate*<int, void>", "delegate*<int, void>" },
        { "delegate*<int, string, void>", "delegate*<int, string, void>" },
        { "delegate*<string?, object?>", "delegate*<string?, object?>" },
        { "delegate*<dynamic, dynamic>", "delegate*<dynamic, dynamic>" },
        { "delegate*<ref int, out string, in double, ref long>", "delegate*<ref int, out string, in double, ref long>" },
        { "delegate*<ref int>", "delegate*<ref int>" },
        { "delegate*<ref readonly int>", "delegate*<ref readonly int>" },
        { "delegate*<ref string?, ref readonly string?>", "delegate*<ref string?, ref readonly string?>" },
        { "delegate*<int*, double*, long*>", "delegate*<int*, double*, long*>" },
        { "delegate* unmanaged<void>", "delegate* unmanaged<void>" },
        { "delegate* unmanaged[Cdecl]<void>", "delegate* unmanaged[Cdecl]<void>" },
        { "delegate* unmanaged[Stdcall]<int, void>", "delegate* unmanaged[Stdcall]<int, void>" },
        { "delegate* unmanaged[Thiscall]<int, void>", "delegate* unmanaged[Thiscall]<int, void>" },
        { "delegate* unmanaged[Fastcall]<int, void>", "delegate* unmanaged[Fastcall]<int, void>" },
        { "delegate* unmanaged[SuppressGCTransition]<void>", "delegate* unmanaged[SuppressGCTransition]<void>" },
        { "delegate* unmanaged[Cdecl, SuppressGCTransition]<int, void>", "delegate* unmanaged[Cdecl, SuppressGCTransition]<int, void>" },
        { "delegate* unmanaged[MemberFunction, SuppressGCTransition]<int, void>", "delegate* unmanaged[MemberFunction, SuppressGCTransition]<int, void>" },
        { "delegate*<delegate*<int, void>, void>", "delegate*<delegate*<int, void>, void>" },
        { "delegate* unmanaged[Cdecl]<int*, delegate*<long, void>, void>", "delegate* unmanaged[Cdecl]<int*, delegate*<long, void>, void>" },
        { "delegate*<delegate*<int, void>, delegate*<delegate*<long, void>, void>>", "delegate*<delegate*<int, void>, delegate*<delegate*<long, void>, void>>" },
        { "delegate*<int, void>[]", "delegate*<int, void>[]" },
        { "delegate*<int, void>[,][]?", "delegate*<int, void>[,][]?" },
        { "delegate*<(int x, int y), (string? text, int code)>", "delegate*<(int x, int y), (string? text, int code)>" }
    };

    [Theory]
    [MemberData(nameof(Types))]
    public void Signature_calling_convention_and_annotations_round_trip(string input, string expected) =>
        TestCompilation.Check(input, expected);
}
