using Microsoft.CodeAnalysis;
using System;
using System.Globalization;
using System.Linq;
using System.Resources;

namespace RuniOS.CodeAnalysis.Diagnostics;

/// <summary>
/// Defines diagnostics reported by the type registry source generators.<br/>
/// 타입 레지스트리 소스 생성기가 보고하는 진단을 정의합니다.
/// </summary>
static class TypeRegistryDiagnostics
{
    const string category = "RuniOS.TypeRegistry";
    static readonly ResourceManager resourceManager = new
    (
        "RuniOS.CodeAnalysis.Diagnostics.TypeRegistryDiagnostics",
        typeof(TypeRegistryDiagnostics).Assembly
    );

    /// <summary>
    /// Describes the diagnostic reported when <c>GenerateTypeRegistryAttribute</c> targets a non-property.<br/>
    /// <c>GenerateTypeRegistryAttribute</c>가 속성이 아닌 대상에 지정된 경우의 진단 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor invalidGenerateTarget = new
    (
        "ROS0002",
        Text("ROS0002_Title"),
        Text("ROS0002_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic for a registry property that does not satisfy the required contract.<br/>
    /// 레지스트리 속성이 요구되는 계약을 만족하지 않는 경우의 진단 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor invalidPropertyContract = new
    (
        "ROS0003",
        Text("ROS0003_Title"),
        Text("ROS0003_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic for an invalid containing type hierarchy.<br/>
    /// 포함 타입 계층이 유효하지 않은 경우의 진단 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor invalidContainingType = new
    (
        "ROS0004",
        Text("ROS0004_Title"),
        Text("ROS0004_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic for a registry property with an invalid registry type.<br/>
    /// 레지스트리 속성의 레지스트리 타입이 유효하지 않은 경우의 진단 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor invalidRegistryType = new
    (
        "ROS0005",
        Text("ROS0005_Title"),
        Text("ROS0005_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic for a registry type unsupported by all available generators.<br/>
    /// 사용 가능한 생성기가 지원하지 않는 레지스트리 타입인 경우의 진단 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor unsupportedRegistryType = new
    (
        "ROS0006",
        Text("ROS0006_Title"),
        Text("ROS0006_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic for a registry type without an accessible parameterless constructor.<br/>
    /// 레지스트리 타입에 접근 가능한 매개 변수 없는 생성자가 없는 경우의 진단 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor missingParameterlessConstructor = new
    (
        "ROS0007",
        Text("ROS0007_Title"),
        Text("ROS0007_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic for a generated member name that conflicts with an existing member.<br/>
    /// 생성되는 멤버 이름이 기존 멤버와 충돌하는 경우의 진단 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor generatedMemberConflict = new
    (
        "ROS0008",
        Text("ROS0008_Title"),
        Text("ROS0008_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic for a registration target whose required assembly lifecycle APIs are missing.<br/>
    /// 필요한 어셈블리 수명 주기 API가 없는 등록 대상의 진단 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor missingLifecycleApi = new
    (
        "ROS0009",
        Text("ROS0009_Title"),
        Text("ROS0009_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the warning for a manifest that cannot restore its registry property.<br/>
    /// 매니페스트에서 레지스트리 속성을 복원할 수 없는 경우의 경고 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor invalidManifest = new
    (
        "ROS0010",
        Text("ROS0010_Title"),
        Text("ROS0010_Message"),
        category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic for generated hint names with different stable identifiers.<br/>
    /// 서로 다른 안정 식별자가 생성된 힌트 이름을 공유하는 경우의 진단 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor hintNameCollision = new
    (
        "ROS0011",
        Text("ROS0011_Title"),
        Text("ROS0011_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic for automatic registration owned by a generic containing type.<br/>
    /// 제네릭 포함 타입이 소유한 자동 등록을 설명하는 진단 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor genericOwnerRegistration = new
    (
        "ROS0012",
        Text("ROS0012_Title"),
        Text("ROS0012_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic for an attributed registry whose attribute type has an invalid base type.<br/>
    /// 특성 기반 레지스트리의 특성 타입 기반이 유효하지 않은 경우의 진단 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor invalidAttributeBase = new
    (
        "ROS0013",
        Text("ROS0013_Title"),
        Text("ROS0013_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic for an attribute argument that cannot be emitted as C# source.<br/>
    /// 특성 인수를 C# 소스로 내보낼 수 없는 경우의 진단 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor unemittableAttributeArgument = new
    (
        "ROS0014",
        Text("ROS0014_Title"),
        Text("ROS0014_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic for an attribute or member inaccessible from generated code.<br/>
    /// 특성 또는 해당 멤버를 생성된 코드에서 접근할 수 없는 경우의 진단 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor inaccessibleAttribute = new
    (
        "ROS0015",
        Text("ROS0015_Title"),
        Text("ROS0015_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the warning reported when an abstract attributed candidate is skipped.<br/>
    /// 추상 특성 후보를 건너뛸 때 보고하는 경고 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor abstractCandidate = new
    (
        "ROS0016",
        Text("ROS0016_Title"),
        Text("ROS0016_Message"),
        category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic reported when a registry property uses a language version below C# 13.<br/>
    /// C# 13 미만 언어 버전에서 레지스트리 속성을 사용하는 경우의 진단 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor unsupportedLanguageVersion = new
    (
        "ROS0017",
        Text("ROS0017_Title"),
        Text("ROS0017_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the warning reported when <c>TypeRegistryManifestAttribute</c> is used directly.<br/>
    /// <c>TypeRegistryManifestAttribute</c>를 직접 사용하는 경우의 경고 설명자입니다.
    /// </summary>
    public static readonly DiagnosticDescriptor manualManifestAttribute = new
    (
        "ROS0018",
        Text("ROS0018_Title"),
        Text("ROS0018_Message"),
        category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the warning reported when a type-registration attribute has no matching generated registry.<br/>
    /// 일치하는 생성 레지스트리가 없는 타입 등록 특성에 대해 보고하는 경고를 설명합니다.
    /// </summary>
    public static readonly DiagnosticDescriptor registrationWithoutRegistry = new
    (
        "ROS0019",
        Text("ROS0019_Title"),
        Text("ROS0019_Message"),
        category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Creates a diagnostic using the specified descriptor, location, and message arguments.<br/>
    /// 지정된 설명자, 위치, 메시지 인수로 진단을 생성합니다.
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor that defines the diagnostic metadata and message format.<br/>
    /// 진단 메타데이터와 메시지 형식을 정의하는 설명자입니다.
    /// </param>
    /// <param name="location">
    /// The source location associated with the diagnostic.<br/>
    /// 진단과 연결할 소스 위치입니다.
    /// </param>
    /// <param name="arguments">
    /// The values used to format the descriptor message.<br/>
    /// 설명자 메시지를 구성하는 데 사용할 값입니다.
    /// </param>
    /// <returns>
    /// A diagnostic created from the supplied values.<br/>
    /// 지정된 값으로 생성한 진단입니다.
    /// </returns>
    public static Diagnostic Create(DiagnosticDescriptor descriptor, Location location, params object[] arguments)
    {
        string[] formatArguments = ConvertArguments(arguments);
        LocalizableResourceString detailedMessage = Text($"{descriptor.Id}_Message", formatArguments);
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
        // 보고된 진단의 제목에도 같은 localizable message를 사용하여 오류 목록과 코드 툴팁을 일치시킵니다.
        return Diagnostic.Create(detailedDescriptor, location);
    }

    static LocalizableResourceString Text(string resourceName, params string[] formatArguments) => new
    (
        resourceName,
        resourceManager,
        typeof(TypeRegistryDiagnostics),
        formatArguments
    );

    static string[] ConvertArguments(object[] arguments)
    {
        string[] formatArguments = new string[arguments.Length];
        for (int index = 0; index < arguments.Length; index++)
            formatArguments[index] = Convert.ToString(arguments[index], CultureInfo.InvariantCulture) ?? string.Empty;

        return formatArguments;
    }
}
