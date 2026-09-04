; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
; ROS0001 is a SuppressionDescriptor, not an analyzer diagnostic.

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
ROS0002 | RuniOS.TypeRegistry | Error | invalidGenerateTarget
ROS0003 | RuniOS.TypeRegistry | Error | invalidPropertyContract
ROS0004 | RuniOS.TypeRegistry | Error | invalidContainingType
ROS0005 | RuniOS.TypeRegistry | Error | invalidRegistryType
ROS0006 | RuniOS.TypeRegistry | Error | unsupportedRegistryType
ROS0007 | RuniOS.TypeRegistry | Error | missingRegistryConstructor
ROS0008 | RuniOS.TypeRegistry | Error | generatedMemberConflict
ROS0009 | RuniOS.TypeRegistry | Error | missingLifecycleApi
ROS0010 | RuniOS.TypeRegistry | Warning | invalidManifest
ROS0011 | RuniOS.TypeRegistry | Error | hintNameCollision
ROS0012 | RuniOS.TypeRegistry | Error | genericOwnerRegistration
ROS0013 | RuniOS.TypeRegistry | Error | invalidAttributeBase
ROS0014 | RuniOS.TypeRegistry | Error | unemittableAttributeArgument
ROS0015 | RuniOS.TypeRegistry | Error | inaccessibleAttribute
ROS0016 | RuniOS.TypeRegistry | Warning | abstractCandidate
ROS0017 | RuniOS.TypeRegistry | Error | unsupportedLanguageVersion
ROS0018 | RuniOS.TypeRegistry | Warning | manualManifestAttribute
ROS0019 | RuniOS.TypeRegistry | Warning | registrationWithoutRegistry
ROS0020 | RuniOS.TypeRegistry | Warning | registrationRequiresChildren
ROS0021 | RuniOS.TypeRegistry | Warning | genericRegistrationParameterCountMismatch
ROS0022 | RuniOS.TypeRegistry | Warning | genericRegistrationConstraintMismatch
ROS0023 | RuniOS.TypeRegistry | Info | genericRegistrationSuggestion
ROS0024 | RuniOS.TypeRegistry | Warning | assignableRegistrationRequiresDefaultConstructor
ROS0025 | RuniOS.TypeSyntaxSerializer | Error | invalidIdentifier
ROS0026 | RuniOS.TypeSyntaxSerializer | Error | unsupportedArrayType
ROS0027 | RuniOS.TypeSyntaxSerializer | Error | unsupportedFunctionPointer
ROS0028 | RuniOS.TypeSyntaxSerializer | Error | unrepresentableType
