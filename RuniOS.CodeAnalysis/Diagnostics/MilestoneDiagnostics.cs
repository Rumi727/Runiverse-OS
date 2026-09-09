using Microsoft.CodeAnalysis;
using System.Linq;
using System.Resources;

namespace RuniOS.CodeAnalysis.Diagnostics;

static class MilestoneDiagnostics
{
    const string category = "RuniOS.Milestones";
    static readonly ResourceManager resourceManager = new
    (
        "RuniOS.CodeAnalysis.Diagnostics.MilestoneDiagnostics",
        typeof(MilestoneDiagnostics).Assembly
    );

    internal static readonly DiagnosticDescriptor methodMustBeStatic = new
    (
        "ROS0029",
        Text("ROS0029_Title"),
        Text("ROS0029_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor methodMustBeParameterless = new
    (
        "ROS0030",
        Text("ROS0030_Title"),
        Text("ROS0030_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor methodMustNotBeGeneric = new
    (
        "ROS0031",
        Text("ROS0031_Title"),
        Text("ROS0031_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor containingTypeMustBePartial = new
    (
        "ROS0032",
        Text("ROS0032_Title"),
        Text("ROS0032_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor containingTypeMustNotBeGeneric = new
    (
        "ROS0033",
        Text("ROS0033_Title"),
        Text("ROS0033_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static readonly DiagnosticDescriptor invalidReturnType = new
    (
        "ROS0034",
        Text("ROS0034_Title"),
        Text("ROS0034_Message"),
        category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    internal static Diagnostic Create(DiagnosticDescriptor descriptor, Location location)
    {
        LocalizableResourceString detailedMessage = Text($"{descriptor.Id}_Message");
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

    static LocalizableResourceString Text(string resourceName) => new
    (
        resourceName,
        resourceManager,
        typeof(MilestoneDiagnostics)
    );
}
