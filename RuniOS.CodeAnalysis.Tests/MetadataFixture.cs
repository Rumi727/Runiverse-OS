#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace RuniOS.CodeAnalysis.Tests;

internal static class MetadataFixture
{
    static readonly PortableExecutableReference reference = BuildReference();

    public static CSharpCompilation Compilation() => TestCompilation.CreateCompilation(
        "public class MetadataConsumer { }", additionalReferences: new MetadataReference[] { reference });

    public static INamedTypeSymbol Type(CSharpCompilation compilation, string name) =>
        Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName(name));

    public static ITypeSymbol Field(CSharpCompilation compilation, string name)
    {
        IFieldSymbol field = Assert.IsAssignableFrom<IFieldSymbol>(Type(compilation, "Fixture.Cases").GetMembers(name).Single());
        return field.Type.WithNullableAnnotation(field.NullableAnnotation);
    }

    static PortableExecutableReference BuildReference()
    {
        MetadataBuilder metadata = new();
        metadata.AddModule(0, metadata.GetOrAddString("SerializerMetadata.dll"),
            metadata.GetOrAddGuid(new Guid("dfb1bb53-3955-498f-ab25-40c514afcb18")), default, default);
        metadata.AddAssembly(metadata.GetOrAddString("SerializerMetadata"), new Version(1, 0, 0, 0),
            default, default, (AssemblyFlags)0, AssemblyHashAlgorithm.None);

        AssemblyName core = typeof(object).Assembly.GetName();
        AssemblyReferenceHandle coreReference = metadata.AddAssemblyReference(metadata.GetOrAddString(core.Name!),
            core.Version!, default, metadata.GetOrAddBlob(core.GetPublicKeyToken()!), (AssemblyFlags)0, default);
        TypeReferenceHandle objectType = metadata.AddTypeReference(coreReference,
            metadata.GetOrAddString("System"), metadata.GetOrAddString("Object"));

        metadata.AddTypeDefinition(TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"),
            default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        TypeDefinitionHandle badName = AddType("Fixture", "Bad-Name");
        AddType("Fixture", "Bad-Container");
        AddType("Bad-Namespace", "GoodName");
        AddType("Fixture", "Cases");

        // ECMA-335 II.23.2: FIELD (06), SZARRAY (1d), ARRAY (14), I4 (08).
        AddField("Vector", 0x06, 0x1d, 0x08);
        AddField("NonSz", 0x06, 0x14, 0x08, 0x01, 0x00, 0x00);
        AddField("Matrix", 0x06, 0x14, 0x08, 0x02, 0x00, 0x00);

        // ARRAY(CLASS TypeDefOrRef), rank 1, no explicit sizes/lower bounds.
        BlobBuilder badArray = new();
        badArray.WriteBytes(new byte[] { 0x06, 0x14, 0x12 });
        badArray.WriteCompressedInteger(MetadataTokens.GetRowNumber(badName) << 2);
        badArray.WriteBytes(new byte[] { 0x01, 0x00, 0x00 });
        metadata.AddFieldDefinition(FieldAttributes.Public | FieldAttributes.Static,
            metadata.GetOrAddString("NonSzBadElement"), metadata.GetOrAddBlob(badArray));

        // FNPTR (1b), VARARG (05), one parameter, return I4, parameter I4.
        AddField("VarArg", 0x06, 0x1b, 0x05, 0x01, 0x08, 0x08);

        foreach (string convention in new[] { "Cdecl", "Stdcall", "Thiscall", "Fastcall" })
        {
            TypeReferenceHandle marker = metadata.AddTypeReference(coreReference,
                metadata.GetOrAddString("System.Runtime.CompilerServices"), metadata.GetOrAddString("CallConv" + convention));
            BlobBuilder signature = new();
            // UNMANAGED (09), zero parameters, CMOD_OPT (20) on the void return type.
            // A single legacy modopt is deliberately different from legacy header encoding.
            signature.WriteBytes(new byte[] { 0x06, 0x1b, 0x09, 0x00, 0x20 });
            signature.WriteCompressedInteger((MetadataTokens.GetRowNumber(marker) << 2) | 1);
            signature.WriteByte(0x01);
            metadata.AddFieldDefinition(FieldAttributes.Public | FieldAttributes.Static,
                metadata.GetOrAddString("Unmanaged" + convention), metadata.GetOrAddBlob(signature));
        }

        ManagedPEBuilder pe = new(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(metadata), new BlobBuilder());
        BlobBuilder image = new();
        pe.Serialize(image);
        return MetadataReference.CreateFromImage(image.ToImmutableArray());

        TypeDefinitionHandle AddType(string ns, string name) => metadata.AddTypeDefinition(
            TypeAttributes.Public | TypeAttributes.BeforeFieldInit, metadata.GetOrAddString(ns), metadata.GetOrAddString(name),
            objectType, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));

        void AddField(string name, params byte[] signature) => metadata.AddFieldDefinition(
            FieldAttributes.Public | FieldAttributes.Static, metadata.GetOrAddString(name), metadata.GetOrAddBlob(signature));
    }
}
