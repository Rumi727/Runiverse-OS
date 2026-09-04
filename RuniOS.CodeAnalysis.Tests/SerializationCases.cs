#nullable enable

namespace RuniOS.CodeAnalysis.Tests;

public static class SerializationCases
{
    public const string Declarations = """
        namespace Fixture
        {
            public class Leaf { }
            public struct Point { public int X; public int Y; }
            public interface I { }
            public enum E : sbyte { Zero }
            public delegate int D();
            public record R;
            public record struct RS;
            public class Box<T> { }
            public class Pair<T, U> { }
            public class Ten<A, B, C, D, E, F, G, H, I, J> { }
            public class Outer<T>
            {
                public class Inner<U> { public class Deep<V> { } }
                public class Leaf { }
            }
            public class Plain { public class Nested<T> { } }
        }
        namespace A { public class Same { } }
        namespace B { public class Same { } }
        namespace Keywords
        {
            public class @int { }
            public class String { }
            public class Object { }
            public class record { }
            public class required { }
            public class 한글 { }
        }
        namespace @namespace.@class
        {
            public class @int<T> { public class @record<U> { } }
        }
        """;

    public static TheoryData<string, string> Types => new()
    {
        { "bool", "bool" }, { "sbyte", "sbyte" }, { "byte", "byte" },
        { "short", "short" }, { "ushort", "ushort" }, { "int", "int" },
        { "uint", "uint" }, { "long", "long" }, { "ulong", "ulong" },
        { "char", "char" }, { "float", "float" }, { "double", "double" },
        { "decimal", "decimal" }, { "string", "string" }, { "object", "object" },
        { "System.Int32", "int" }, { "System.Boolean", "bool" },
        { "System.String", "string" }, { "System.Object", "object" },
        { "Guid", "global::System.Guid" }, { "DateTime", "global::System.DateTime" },
        { "IntPtr", "nint" }, { "UIntPtr", "nuint" },
        { "nint", "nint" }, { "nuint", "nuint" }, { "nint?", "nint?" },
        { "nuint[]", "nuint[]" }, { "dynamic", "dynamic" }, { "dynamic?", "dynamic?" },
        { "dynamic[]", "dynamic[]" }, { "dynamic?[]?", "dynamic?[]?" },
        { "List<dynamic>", "global::System.Collections.Generic.List<dynamic>" },
        { "Dictionary<string, dynamic>", "global::System.Collections.Generic.Dictionary<string, dynamic>" },
        { "(dynamic value, object other)", "(dynamic value, object other)" },
        { "string?", "string?" }, { "object?", "object?" },
        { "string[]", "string[]" }, { "string?[]", "string?[]" },
        { "string[]?", "string[]?" }, { "string?[]?", "string?[]?" },
        { "int?", "int?" }, { "Nullable<int>", "int?" },
        { "Guid?", "global::System.Guid?" }, { "Nullable<Guid>", "global::System.Guid?" },
        { "Fixture.E?", "global::Fixture.E?" }, { "Fixture.Point?", "global::Fixture.Point?" },
        { "List<string>", "global::System.Collections.Generic.List<string>" },
        { "List<string?>", "global::System.Collections.Generic.List<string?>" },
        { "List<string>?", "global::System.Collections.Generic.List<string>?" },
        { "List<string?>?", "global::System.Collections.Generic.List<string?>?" },
        { "List<int?>", "global::System.Collections.Generic.List<int?>" },
        { "Dictionary<string?, List<string?[]>?>?", "global::System.Collections.Generic.Dictionary<string?, global::System.Collections.Generic.List<string?[]>?>?" },
        { "Dictionary<string?, List<Dictionary<string, string?[]?>?>?>[]?", "global::System.Collections.Generic.Dictionary<string?, global::System.Collections.Generic.List<global::System.Collections.Generic.Dictionary<string, string?[]?>?>?>[]?" },
        { "Dictionary<string, List<int>>", "global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<int>>" },
        { "Fixture.Pair<int, string?>", "global::Fixture.Pair<int, string?>" },
        { "Fixture.Box<Fixture.Pair<int, string?>?>", "global::Fixture.Box<global::Fixture.Pair<int, string?>?>" },
        { "Fixture.Outer<int>.Inner<string>.Deep<double>", "global::Fixture.Outer<int>.Inner<string>.Deep<double>" },
        { "Fixture.Outer<List<string?>>.Inner<Dictionary<int, string?>?>", "global::Fixture.Outer<global::System.Collections.Generic.List<string?>>.Inner<global::System.Collections.Generic.Dictionary<int, string?>?>" },
        { "Fixture.Outer<string?>.Leaf", "global::Fixture.Outer<string?>.Leaf" },
        { "Fixture.Plain.Nested<int>", "global::Fixture.Plain.Nested<int>" },
        { "Fixture.Outer<dynamic>.Inner<object?>", "global::Fixture.Outer<dynamic>.Inner<object?>" },
        { "Fixture.Ten<int, string?, List<int?>, int[,], (int x, string? y), object?, dynamic, Guid, nint, Fixture.Leaf>", "global::Fixture.Ten<int, string?, global::System.Collections.Generic.List<int?>, int[,], (int x, string? y), object?, dynamic, global::System.Guid, nint, global::Fixture.Leaf>" },
        { "int[]", "int[]" }, { "int[][]", "int[][]" }, { "int[][][]", "int[][][]" },
        { "int[,]", "int[,]" }, { "int[,,]", "int[,,]" }, { "int[,,,]", "int[,,,]" },
        { "int[][,]", "int[][,]" }, { "int[,][]", "int[,][]" },
        { "int[][,][][,,]", "int[][,][][,,]" },
        { "string[]?[]", "string[]?[]" }, { "string[][]?", "string[][]?" },
        { "string?[]?[]?", "string?[]?[]?" }, { "string?[,]?[]", "string?[,]?[]" },
        { "string?[][,]?[][,,]", "string?[][,]?[][,,]" },
        { "string?[,][]?[,,][]?", "string?[,][]?[,,][]?" },
        { "int[]?[,][][,,]?", "int[]?[,][][,,]?" },
        { "List<string?>[,]", "global::System.Collections.Generic.List<string?>[,]" },
        { "Dictionary<string, int[]>[,,][]?", "global::System.Collections.Generic.Dictionary<string, int[]>[,,][]?" },
        { "(int, string)", "(int, string)" }, { "(int x, string y)", "(int x, string y)" },
        { "(int x, string? y)", "(int x, string? y)" },
        { "(int x, int)", "(int x, int)" },
        { "(int, (string, double))", "(int, (string, double))" },
        { "((int a, int b) point, string name)", "((int a, int b) point, string name)" },
        { "(int x, int y)?", "(int x, int y)?" },
        { "(int x, string? y)[]?", "(int x, string? y)[]?" },
        { "(int @class, string @namespace)", "(int @class, string @namespace)" },
        { "(int 한글, string Δ)", "(int 한글, string Δ)" },
        { "(int a, int b, int c, int d, int e, int f, int g)", "(int a, int b, int c, int d, int e, int f, int g)" },
        { "(int a, int b, int c, int d, int e, int f, int g, string? h)", "(int a, int b, int c, int d, int e, int f, int g, string? h)" },
        { "(string? name, Dictionary<string, List<int?[]>> values, (int x, int y)? point)", "(string? name, global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<int?[]>> values, (int x, int y)? point)" },
        { "Fixture.Leaf", "global::Fixture.Leaf" }, { "Fixture.Point", "global::Fixture.Point" },
        { "Fixture.I", "global::Fixture.I" }, { "Fixture.E", "global::Fixture.E" },
        { "Fixture.D", "global::Fixture.D" }, { "Fixture.R", "global::Fixture.R" },
        { "Fixture.RS", "global::Fixture.RS" },
        { "A.Same", "global::A.Same" }, { "B.Same", "global::B.Same" },
        { "Keywords.@int", "global::Keywords.@int" },
        { "Keywords.String", "global::Keywords.String" }, { "Keywords.Object", "global::Keywords.Object" },
        { "Keywords.record", "global::Keywords.record" }, { "Keywords.required", "global::Keywords.required" },
        { "Keywords.한글", "global::Keywords.한글" },
        { "@namespace.@class.@int<string>.@record<int>", "global::@namespace.@class.@int<string>.record<int>" }
    };

    public static TheoryData<string, string> TypeOfTypes => new()
    {
        { "void", "void" },
        { "List<>", "global::System.Collections.Generic.List<>" },
        { "Dictionary<,>", "global::System.Collections.Generic.Dictionary<,>" },
        { "Nullable<>", "global::System.Nullable<>" },
        { "Fixture.Outer<>.Inner<>", "global::Fixture.Outer<>.Inner<>" },
        { "Fixture.Outer<>.Leaf", "global::Fixture.Outer<>.Leaf" },
        { "Fixture.Ten<,,,,,,,,,>", "global::Fixture.Ten<,,,,,,,,,>" }
    };

    public static TheoryData<string, string> TypeParameters => new()
    {
        { "T", "" }, { "T?", "" }, { "T", "where T : class" },
        { "T?", "where T : class" }, { "T?", "where T : class?" },
        { "T[]?", "where T : class" }, { "T?[]?", "where T : class" },
        { "T", "where T : struct" }, { "T?", "where T : struct" },
        { "T*", "where T : unmanaged" }, { "T", "where T : notnull" },
        { "T", "where T : new()" }, { "T", "where T : IComparable<T>" }
    };
}
