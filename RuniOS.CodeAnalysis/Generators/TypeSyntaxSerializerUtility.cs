using Microsoft.CodeAnalysis;
using RuniOS.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Generators;

/// <summary>
/// Converts <see cref="TypeSyntaxSerializer"/> errors into source diagnostics.<br/>
/// <see cref="TypeSyntaxSerializer"/> 오류를 소스 진단으로 변환합니다.
/// </summary>
static class TypeSyntaxSerializerUtility
{
    /// <summary>
    /// Serializes a type and creates one diagnostic for each serialization error.<br/>
    /// 타입을 직렬화하고 직렬화 오류마다 진단 하나를 생성합니다.
    /// </summary>
    /// <param name="typeSymbol">
    /// The type symbol to serialize.<br/>
    /// 직렬화할 타입 심볼입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation used to format diagnostic symbols.<br/>
    /// 진단 심볼을 포맷할 때 사용할 컴파일입니다.
    /// </param>
    /// <param name="location">
    /// The source location to attach to every diagnostic.<br/>
    /// 모든 진단에 연결할 소스 위치입니다.
    /// </param>
    /// <param name="result">
    /// Receives the serialized type syntax, including any partial text produced before an error.<br/>
    /// 오류 전에 생성된 부분 문자열을 포함한 직렬화 결과를 받습니다.
    /// </param>
    /// <param name="diagnostics">
    /// Receives one diagnostic for each serializer error.<br/>
    /// 직렬화 오류마다 하나씩 생성된 진단을 받습니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when serialization succeeds; otherwise, <see langword="false"/>.<br/>
    /// 직렬화에 성공하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.
    /// </returns>
    public static bool TrySerialize
    (
        ITypeSymbol typeSymbol,
        Compilation compilation,
        Location location,
        out string result,
        out ImmutableArray<Diagnostic> diagnostics
    )
    {
        TypeSyntaxSerializer.SerializeErrorResults errors = typeSymbol.TrySerialize(out result);
        diagnostics = CreateDiagnostics(errors, compilation, location);
        return errors.isSuccess;
    }

    /// <summary>
    /// Serializes a type and reports every serialization error through the supplied callback.<br/>
    /// 타입을 직렬화하고 모든 직렬화 오류를 지정한 콜백으로 보고합니다.
    /// </summary>
    /// <param name="typeSymbol">
    /// The type symbol to serialize.<br/>
    /// 직렬화할 타입 심볼입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation used to format diagnostic symbols.<br/>
    /// 진단 심볼을 포맷할 때 사용할 컴파일입니다.
    /// </param>
    /// <param name="location">
    /// The source location to attach to every diagnostic.<br/>
    /// 모든 진단에 연결할 소스 위치입니다.
    /// </param>
    /// <param name="result">
    /// Receives the serialized type syntax, including any partial text produced before an error.<br/>
    /// 오류 전에 생성된 부분 문자열을 포함한 직렬화 결과를 받습니다.
    /// </param>
    /// <param name="reportDiagnostic">
    /// The callback used to report created diagnostics.<br/>
    /// 생성된 진단을 보고할 콜백입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when serialization succeeds; otherwise, <see langword="false"/>.<br/>
    /// 직렬화에 성공하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.
    /// </returns>
    public static bool TrySerialize
    (
        ITypeSymbol typeSymbol,
        Compilation compilation,
        Location location,
        out string result,
        Action<Diagnostic> reportDiagnostic
    )
    {
        TypeSyntaxSerializer.SerializeErrorResults errors = typeSymbol.TrySerialize(out result);
        foreach (Diagnostic diagnostic in CreateDiagnostics(errors, compilation, location))
            reportDiagnostic(diagnostic);

        return errors.isSuccess;
    }

    /// <summary>
    /// Converts serializer errors into diagnostics without serializing again.<br/>
    /// 직렬화를 다시 수행하지 않고 직렬화 오류를 진단으로 변환합니다.
    /// </summary>
    /// <param name="errors">
    /// The serializer errors to convert.<br/>
    /// 변환할 직렬화 오류입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation used to format diagnostic symbols.<br/>
    /// 진단 심볼을 포맷할 때 사용할 컴파일입니다.
    /// </param>
    /// <param name="location">
    /// The source location to attach to every diagnostic.<br/>
    /// 모든 진단에 연결할 소스 위치입니다.
    /// </param>
    /// <returns>
    /// One diagnostic for each serializer error, or an empty array for success.<br/>
    /// 직렬화 오류마다 하나의 진단을 반환하며, 성공 시 빈 배열을 반환합니다.
    /// </returns>
    public static ImmutableArray<Diagnostic> CreateDiagnostics
    (
        TypeSyntaxSerializer.SerializeErrorResults errors,
        Compilation compilation,
        Location location
    )
    {
        if (errors.isSuccess)
            return ImmutableArray<Diagnostic>.Empty;

        ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>(errors.count);
        foreach (TypeSyntaxSerializer.SerializeErrorResult error in errors)
            diagnostics.Add(TypeSyntaxSerializerDiagnostics.Create(error, location, compilation));

        return diagnostics.ToImmutable();
    }
}
