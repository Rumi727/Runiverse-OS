using Microsoft.CodeAnalysis;
using System;
using System.Reflection.Metadata;
using System.Text;

// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace RuniOS.CodeAnalysis.Generators;

public static partial class TypeSyntaxSerializer
{
    static SerializeErrorResults RenderFunctionPointer(StringBuilder builder, IFunctionPointerTypeSymbol functionPointerTypeSymbol)
    {
        IMethodSymbol signature = functionPointerTypeSymbol.Signature;
        builder.Append("delegate*");

        SerializeErrorResults result = RenderFunctionPointerCallingConvention(builder, signature);
        builder.Append('<');

        foreach (IParameterSymbol parameter in signature.Parameters)
        {
            switch (parameter.RefKind)
            {
                case RefKind.None:
                    break;
                case RefKind.Ref:
                {
                    builder.Append("ref ");
                    break;
                }
                case RefKind.Out:
                {
                    builder.Append("out ");
                    break;
                }
                case RefKind.In:
                {
                    builder.Append("in ");
                    break;
                }
                case (RefKind)4://RefKind.RefReadOnlyParameter:
                {
                    builder.Append("ref readonly ");
                    break;
                }
                default:
                    return new SerializeErrorResults(SerializeError.unsupportedFunctionPointer, parameter);
            }

            result |= RenderType(builder, parameter.Type.WithNullableAnnotation(parameter.NullableAnnotation));
            builder.Append(", ");
        }

        if (signature.ReturnsByRefReadonly)
            builder.Append("ref readonly ");
        else if (signature.ReturnsByRef)
            builder.Append("ref ");

        result |= RenderType(builder, signature.ReturnType.WithNullableAnnotation(signature.ReturnNullableAnnotation));
        builder.Append('>');

        return result;
    }

    static SerializeErrorResults RenderFunctionPointerCallingConvention(StringBuilder builder, IMethodSymbol signature)
    {
        SerializeErrorResults result = default;
        switch (signature.CallingConvention)
        {
            case SignatureCallingConvention.Default:
            {
                // delegate*<...>
                // == delegate* managed<...>
                return default;
            }
            case SignatureCallingConvention.CDecl:
            {
                builder.Append(" unmanaged[Cdecl]");
                return default;
            }
            case SignatureCallingConvention.StdCall:
            {
                builder.Append(" unmanaged[Stdcall]");
                return default;
            }
            case SignatureCallingConvention.ThisCall:
            {
                builder.Append(" unmanaged[Thiscall]");
                return default;
            }
            case SignatureCallingConvention.FastCall:
            {
                builder.Append(" unmanaged[Fastcall]");
                return default;
            }
            case SignatureCallingConvention.Unmanaged:
                break;
            case SignatureCallingConvention.VarArgs:
            default:
            {
                // 특히 VarArgs는 C# function pointer syntax로 표현 불가능.
                result |= new SerializeErrorResults(SerializeError.unsupportedFunctionPointer, signature);
                break;
            }
        }

        builder.Append(" unmanaged");

        var callingConventionTypes = signature.UnmanagedCallingConventionTypes;
        if (callingConventionTypes.IsEmpty)
            return result;

        /*
         * 중요:
         *
         * unmanaged[Cdecl]
         *
         * 은 C#에서 SignatureCallingConvention.CDecl로 인코딩된다.
         *
         * 따라서 metadata에
         *
         * CallingConvention == Unmanaged
         * UnmanagedCallingConventionTypes == [CallConvCdecl]
         *
         * 이 들어있는 이상한 타입을 unmanaged[Cdecl]로 출력하면
         * 원래 타입과 다른 타입이 된다.
         */
        if (callingConventionTypes.Length == 1 && IsLegacyCallingConvention(callingConventionTypes[0]))
            result |= new SerializeErrorResults(SerializeError.unsupportedFunctionPointer, signature);

        builder.Append('[');

        for (int i = 0; i < callingConventionTypes.Length; i++)
        {
            if (i != 0)
                builder.Append(", ");

            INamedTypeSymbol conventionType = callingConventionTypes[i];
            const string prefix = "CallConv";

            if (!conventionType.Name.StartsWith(prefix, StringComparison.Ordinal))
                return new SerializeErrorResults(SerializeError.unsupportedFunctionPointer, conventionType);

            string name = conventionType.Name.Substring(prefix.Length);
            result |= RenderIdentifier(builder, name);
        }

        builder.Append(']');
        return result;
    }

    static bool IsLegacyCallingConvention(INamedTypeSymbol type) => type.Name is "CallConvCdecl" or "CallConvStdcall" or "CallConvThiscall" or "CallConvFastcall";
}