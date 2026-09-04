using Microsoft.CodeAnalysis;
using RuniOS.CodeAnalysis.Generators;
using System;
using System.Globalization;
using System.Linq;
using System.Resources;

namespace RuniOS.CodeAnalysis.Diagnostics;

/// <summary>
/// Defines diagnostics produced from <see cref="TypeSyntaxSerializer"/> errors.<br/>
/// <see cref="TypeSyntaxSerializer"/> 오류를 진단으로 변환할 때 사용하는 진단을 정의합니다.
/// </summary>
static class TypeSyntaxSerializerDiagnostics
{
    const string category = "RuniOS.TypeSyntaxSerializer";
    static readonly ResourceManager resourceManager = new
    (
        "RuniOS.CodeAnalysis.Diagnostics.Diagnostics",
        typeof(TypeSyntaxSerializerDiagnostics).Assembly
    );

    /// <summary>
    /// Describes an identifier that cannot be represented as C# syntax.<br/>
    /// C# 구문으로 표현할 수 없는 식별자를 설명합니다.
    /// </summary>
    public static readonly DiagnosticDescriptor invalidIdentifier = CreateDescriptor("ROS0025");

    /// <summary>
    /// Describes an array shape that cannot be represented by equivalent C# syntax.<br/>
    /// 동일한 의미의 C# 구문으로 표현할 수 없는 배열 형태를 설명합니다.
    /// </summary>
    public static readonly DiagnosticDescriptor unsupportedArrayType = CreateDescriptor("ROS0026");

    /// <summary>
    /// Describes a function pointer that cannot be represented by equivalent C# syntax.<br/>
    /// 동일한 의미의 C# 구문으로 표현할 수 없는 함수 포인터를 설명합니다.
    /// </summary>
    public static readonly DiagnosticDescriptor unsupportedFunctionPointer = CreateDescriptor("ROS0027");

    /// <summary>
    /// Describes a type kind that the serializer cannot represent.<br/>
    /// 직렬화기가 표현할 수 없는 타입 종류를 설명합니다.
    /// </summary>
    public static readonly DiagnosticDescriptor unrepresentableType = CreateDescriptor("ROS0028");

    /// <summary>
    /// Creates a diagnostic for one serializer error.<br/>
    /// 직렬화 오류 하나에 대한 진단을 생성합니다.
    /// </summary>
    /// <param name="error">
    /// The serializer error to report.<br/>
    /// 보고할 직렬화 오류입니다.
    /// </param>
    /// <param name="location">
    /// The source location at which the generated type syntax is used.<br/>
    /// 생성된 타입 구문이 사용되는 소스 위치입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation used to format symbols in the diagnostic context.<br/>
    /// 진단 문맥에서 심볼을 포맷할 때 사용할 컴파일입니다.
    /// </param>
    /// <returns>
    /// A diagnostic describing <paramref name="error"/>.<br/>
    /// <paramref name="error"/>를 설명하는 진단입니다.
    /// </returns>
    public static Diagnostic Create(TypeSyntaxSerializer.SerializeErrorResult error, Location location, Compilation compilation)
    {
        DiagnosticDescriptor descriptor = GetDescriptor(error.error);
        string argument = FormatProblematicObject(error.problematicObject, compilation, location);
        LocalizableResourceString detailedMessage = Text($"{descriptor.Id}_Message", argument);
        DiagnosticDescriptor detailedDescriptor = new
        (
            descriptor.Id,
            detailedMessage,
            detailedMessage,
            descriptor.Category,
            descriptor.DefaultSeverity,
            descriptor.IsEnabledByDefault,
            descriptor.Description,
            descriptor.HelpLinkUri,
            descriptor.CustomTags.ToArray()
        );

        // Rider's error list displays DiagnosticDescriptor.Title, while the editor tooltip displays the formatted message.
        // 오류 목록과 코드 툴팁이 같은 내용을 표시하도록 제목에도 형식화된 메시지를 사용합니다.
        return Diagnostic.Create(detailedDescriptor, location);
    }

    static DiagnosticDescriptor CreateDescriptor(string id) => new
    (
        id,
        Text($"{id}_Title"),
        Text($"{id}_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    static DiagnosticDescriptor GetDescriptor(TypeSyntaxSerializer.SerializeError error) => error switch
    {
        TypeSyntaxSerializer.SerializeError.invalidIdentifier => invalidIdentifier,
        TypeSyntaxSerializer.SerializeError.unsupportedArrayType => unsupportedArrayType,
        TypeSyntaxSerializer.SerializeError.unsupportedFunctionPointer => unsupportedFunctionPointer,
        TypeSyntaxSerializer.SerializeError.unrepresentableType => unrepresentableType,
        _ => throw new ArgumentOutOfRangeException(nameof(error), error, "The serializer error does not have a diagnostic descriptor.")
    };

    static string FormatProblematicObject(object? problematicObject, Compilation compilation, Location location) => problematicObject switch
    {
        ISymbol symbol => symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
        _ => Convert.ToString(problematicObject, CultureInfo.InvariantCulture) ?? "<unknown>"
    };

    static LocalizableResourceString Text(string resourceName, params string[] formatArguments) => new
    (
        resourceName,
        resourceManager,
        typeof(TypeSyntaxSerializerDiagnostics),
        formatArguments
    );
}
