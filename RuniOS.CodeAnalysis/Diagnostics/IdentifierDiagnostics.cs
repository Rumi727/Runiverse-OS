using Microsoft.CodeAnalysis;
using System.Linq;
using System.Resources;

namespace RuniOS.CodeAnalysis.Diagnostics;

/// <summary>
/// Defines diagnostics reported for constant strings converted to <c>RuniOS.Resource.Identifier</c>.<br/>
/// <c>RuniOS.Resource.Identifier</c>로 변환되는 상수 문자열에 대해 보고하는 진단을 정의합니다.
/// </summary>
static class IdentifierDiagnostics
{
    const string category = "RuniOS.Identifier";
    static readonly ResourceManager resourceManager = new
    (
        "RuniOS.CodeAnalysis.Diagnostics.IdentifierDiagnostics",
        typeof(IdentifierDiagnostics).Assembly
    );

    /// <summary>
    /// Describes the diagnostic for an identifier string that contains more than one separator.<br/>
    /// 구분자가 두 개 이상 포함된 식별자 문자열에 대한 진단 설명자입니다.
    /// </summary>
    internal static readonly DiagnosticDescriptor tooManySeparators = new
    (
        "ROS0036",
        Text("ROS0036_Title"),
        Text("ROS0036_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic for an identifier string whose namespace contains invalid characters.<br/>
    /// 네임스페이스에 유효하지 않은 문자가 포함된 식별자 문자열에 대한 진단 설명자입니다.
    /// </summary>
    internal static readonly DiagnosticDescriptor invalidNamespace = new
    (
        "ROS0037",
        Text("ROS0037_Title"),
        Text("ROS0037_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic for an identifier string whose path contains invalid characters.<br/>
    /// 경로에 유효하지 않은 문자가 포함된 식별자 문자열에 대한 진단 설명자입니다.
    /// </summary>
    internal static readonly DiagnosticDescriptor invalidPath = new
    (
        "ROS0038",
        Text("ROS0038_Title"),
        Text("ROS0038_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// Describes the diagnostic for an identifier string without a namespace, which resolves to the <c>runios</c> namespace.<br/>
    /// 네임스페이스가 없어 <c>runios</c> 네임스페이스로 해석되는 식별자 문자열에 대한 진단 설명자입니다.
    /// </summary>
    internal static readonly DiagnosticDescriptor defaultNamespace = new
    (
        "ROS0039",
        Text("ROS0039_Title"),
        Text("ROS0039_Message"),
        category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    internal static Diagnostic Create(DiagnosticDescriptor descriptor, Location location, params string[] arguments)
    {
        LocalizableResourceString detailedMessage = Text($"{descriptor.Id}_Message", arguments);
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

        return Diagnostic.Create(detailedDescriptor, location);
    }

    static LocalizableResourceString Text(string resourceName, params string[] formatArguments) => new
    (
        resourceName,
        resourceManager,
        typeof(IdentifierDiagnostics),
        formatArguments
    );
}
