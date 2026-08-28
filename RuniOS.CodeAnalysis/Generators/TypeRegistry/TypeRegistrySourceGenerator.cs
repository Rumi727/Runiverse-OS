using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using RuniOS.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

/// <summary>
/// Provides the shared incremental generation pipeline for type registry implementations.<br/>
/// 타입 레지스트리 구현을 위한 공통 증분 생성 파이프라인을 제공합니다.
/// </summary>
/// <remarks>
/// Derived generators provide registry-type validation, candidate discovery, candidate binding, and registration emission.<br/>
/// 파생 생성기는 레지스트리 타입 검증, 후보 발견, 후보 바인딩, 등록 소스 생성을 제공합니다.
/// </remarks>
public abstract class TypeRegistrySourceGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Gets the metadata name of the attribute that marks registry properties in the current compilation.<br/>
    /// 현재 컴파일의 레지스트리 속성을 표시하는 특성의 메타데이터 이름을 가져옵니다.
    /// </summary>
    // 런타임 컴파일에 주입되는 GenerateTypeRegistryAttribute의 네임스페이스/타입 이름과 정확히 일치해야 합니다.
    protected const string generateTypeRegistryAttributeMetadataName = "RuniOS.Reflection.GenerateTypeRegistryAttribute";

    // Microsoft.CodeAnalysis.CSharp 4.3.0 reference에는 CSharp13 enum 멤버가 없어 숫자 값을 사용합니다.
    const int minimumPartialPropertyLanguageVersion = 1300;

    /// <summary>
    /// Gets the metadata name of the assembly manifest attribute for referenced registries.<br/>
    /// 참조된 레지스트리의 어셈블리 매니페스트 특성 메타데이터 이름을 가져옵니다.
    /// </summary>
    // 참조 어셈블리는 이 생성된 매니페스트 특성의 정확한 메타데이터 이름으로 레지스트리 속성을 노출합니다.
    protected const string typeRegistryManifestAttributeMetadataName = "RuniOS.Reflection.TypeRegistryManifestAttribute";

    /// <summary>
    /// Gets the metadata name of the base type implemented by supported registries.<br/>
    /// 지원되는 레지스트리가 구현하는 기본 타입의 메타데이터 이름을 가져옵니다.
    /// </summary>
    // 지원 레지스트리 검증이 런타임 TypeRegistry를 직접 조회하므로 이 메타데이터 이름을 유지해야 합니다.
    protected const string typeRegistryMetadataName = "RuniOS.Reflection.TypeRegistry";

    /// <summary>
    /// Gets the metadata name of the assembly-loaded lifecycle attribute.<br/>
    /// 어셈블리 로드 수명 주기 특성의 메타데이터 이름을 가져옵니다.
    /// </summary>
    // 생성 콜백은 아래의 짧은 이름과 함께 이 Unity lifecycle 특성 메타데이터 이름을 정확히 사용합니다.
    protected const string onAssemblyLoadedAttributeMetadataName = "Unity.Scripting.LifecycleManagement.OnAssemblyLoadedAttribute";

    /// <summary>
    /// Gets the metadata name of the assembly-unloading lifecycle attribute.<br/>
    /// 어셈블리 언로드 수명 주기 특성의 메타데이터 이름을 가져옵니다.
    /// </summary>
    // 생성된 unload 콜백도 이 Unity lifecycle 특성 메타데이터 이름을 정확히 사용합니다.
    protected const string onAssemblyUnloadingAttributeMetadataName = "Unity.Scripting.LifecycleManagement.OnAssemblyUnloadingAttribute";

    /// <summary>
    /// Gets the metadata name of the lifecycle callback registration helper.<br/>
    /// 수명 주기 콜백 등록 도우미의 메타데이터 이름을 가져옵니다.
    /// </summary>
    // 실제 Unity.Scripting API가 없는 compilation에는 generator가 호환용 no-op 선언을 제공합니다.
    protected const string lifecycleMethodRegistrationMetadataName = "Unity.Scripting.LifecycleManagement.CodeGen.LifecycleMethodRegistration";

    /// <summary>
    /// Gets the short name used in generated registration type and hint names.<br/>
    /// 생성된 등록 타입 및 힌트 이름에 사용할 짧은 이름을 가져옵니다.
    /// </summary>
    protected abstract string generatorName { get; }

    /// <summary>
    /// Configures incremental providers and registers the source generation pipeline.<br/>
    /// 증분 프로바이더를 구성하고 소스 생성 파이프라인을 등록합니다.
    /// </summary>
    /// <param name="context">
    /// The incremental generator initialization context used to register providers and outputs.<br/>
    /// 프로바이더 및 출력을 등록하는 데 사용할 증분 생성기 초기화 컨텍스트입니다.
    /// </param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // TODO: Unity 버전이 올라 Roslyn의 RegisterPreCompilationSourceOutput을 사용할 수 있게 되면
        // Unity lifecycle fallback 및 post-initialization 선언을 제거하고, Unity 어셈블리가 실제로 참조된
        // compilation에서만 __AttributedTypeRegistryRegistration을 생성하도록 변경한다.
        // The lifecycle declaration is post-initialization output, so its type name must be chosen before Compilation is available.
        string registrationTypeName = $"__{generatorName}Registration";
        // Unity's lifecycle generator must see these declarations before regular generator output is produced.
        string lifecycleDeclarationSource = TypeRegistryEmitter.RenderRegistrationLifecycleDeclaration(registrationTypeName);
        context.RegisterPostInitializationOutput
        (
            postInitializationContext => postInitializationContext.AddSource
            (
                $"RuniOS.{generatorName}.LifecycleRegistration.g.cs",
                lifecycleDeclarationSource
            )
        );
        context.RegisterSourceOutput
        (
            context.CompilationProvider,
            (productionContext, compilation) => EmitLifecycleCompatibility(productionContext, compilation, generatorName)
        );

        IncrementalValuesProvider<RegistryDiscoveryItem> currentRegistries = context.SyntaxProvider.ForAttributeWithMetadataName
        (
            generateTypeRegistryAttributeMetadataName,
            static (_, _) => true,
            CreateCurrentRegistryDiscovery
        );

        IncrementalValuesProvider<RegistrationCandidate> candidates = CreateCandidateProvider(context);
        IncrementalValueProvider<((Compilation, ImmutableArray<RegistryDiscoveryItem>), ImmutableArray<RegistrationCandidate>)> input =
            context.CompilationProvider.Combine(currentRegistries.Collect()).Combine(candidates.Collect());

        context.RegisterSourceOutput
        (
            input,
            (productionContext, value) => Execute(productionContext, value, registrationTypeName)
        );
    }

    /// <summary>
    /// Determines whether the derived generator supports a registry type.<br/>
    /// 파생 생성기가 레지스트리 타입을 지원하는지 확인합니다.
    /// </summary>
    /// <param name="registryType">
    /// The registry type to validate.<br/>
    /// 검증할 레지스트리 타입입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation containing the registry type.<br/>
    /// 레지스트리 타입을 포함하는 컴파일입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when this generator supports <paramref name="registryType"/>; otherwise, <see langword="false"/>.<br/>
    /// 이 생성기가 <paramref name="registryType"/>을 지원하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    protected abstract bool IsSupportedRegistryType(INamedTypeSymbol registryType, Compilation compilation);

    /// <summary>
    /// Creates the default expression used to initialize a generated registry backing field.<br/>
    /// 생성된 레지스트리 백킹 필드를 초기화하는 기본 식을 만듭니다.
    /// </summary>
    /// <param name="registry">
    /// The registry definition being initialized.<br/>
    /// 초기화할 레지스트리 정의입니다.
    /// </param>
    /// <returns>
    /// A C# expression that creates an instance of the registry type.<br/>
    /// 레지스트리 타입의 인스턴스를 생성하는 C# 식입니다.
    /// </returns>
    // 생성된 속성 초기화가 레지스트리의 접근 가능한 매개 변수 없는 생성자를 호출합니다.
    protected virtual string CreateRegistryInitializer(RegistryDefinition registry) => $"new {GeneratorUtils.GetTypeName(registry.registryType)}()";

    /// <summary>
    /// Performs additional validation for a supported registry type.<br/>
    /// 지원되는 레지스트리 타입에 대한 추가 검증을 수행합니다.
    /// </summary>
    /// <param name="registryType">
    /// The supported registry type to validate.<br/>
    /// 추가 검증할 지원 레지스트리 타입입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation containing the registry type.<br/>
    /// 레지스트리 타입을 포함하는 컴파일입니다.
    /// </param>
    /// <param name="diagnostic">
    /// When validation fails, receives the diagnostic explaining the failure; otherwise, <see langword="null"/>.<br/>
    /// 검증에 실패하면 원인을 설명하는 진단을 받고, 성공하면 <see langword="null"/>입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when additional validation succeeds; otherwise, <see langword="false"/>.<br/>
    /// 추가 검증에 성공하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    protected virtual bool TryValidateSupportedRegistryType(INamedTypeSymbol registryType, Compilation compilation, out Diagnostic? diagnostic)
    {
        diagnostic = null;
        return true;
    }

    /// <summary>
    /// Creates the incremental provider that discovers registration candidates.<br/>
    /// 등록 후보를 발견하는 증분 프로바이더를 만듭니다.
    /// </summary>
    /// <param name="context">
    /// The incremental generator initialization context used to create the provider.<br/>
    /// 프로바이더를 만드는 데 사용할 증분 생성기 초기화 컨텍스트입니다.
    /// </param>
    /// <returns>
    /// A provider that yields candidates considered for registry binding.<br/>
    /// 레지스트리 바인딩 대상으로 검토할 후보를 생성하는 프로바이더입니다.
    /// </returns>
    protected abstract IncrementalValuesProvider<RegistrationCandidate> CreateCandidateProvider(IncrementalGeneratorInitializationContext context);

    /// <summary>
    /// Binds a candidate to a registry or reports why it cannot be bound.<br/>
    /// 후보를 레지스트리에 바인딩하거나 바인딩할 수 없는 이유를 보고합니다.
    /// </summary>
    /// <param name="registry">
    /// The registry definition receiving the candidate.<br/>
    /// 후보를 받을 레지스트리 정의입니다.
    /// </param>
    /// <param name="candidate">
    /// The candidate to inspect and bind.<br/>
    /// 검사하고 바인딩할 후보입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation used for symbol and accessibility checks.<br/>
    /// 심볼 및 접근성 검사에 사용할 컴파일입니다.
    /// </param>
    /// <param name="registration">
    /// Receives the bound registration when binding succeeds; otherwise, <see langword="null"/>.<br/>
    /// 바인딩에 성공하면 바인딩된 등록을 받고, 그렇지 않으면 <see langword="null"/>입니다.
    /// </param>
    /// <param name="diagnostic">
    /// Receives a diagnostic when the candidate produces a reportable binding error; otherwise, <see langword="null"/>.<br/>
    /// 보고할 수 있는 바인딩 오류가 있으면 진단을 받고, 그렇지 않으면 <see langword="null"/>입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a registration was bound; otherwise, <see langword="false"/>.<br/>
    /// 등록이 바인딩되면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    protected abstract bool TryBindCandidate
    (
        RegistryDefinition registry,
        RegistrationCandidate candidate,
        Compilation compilation,
        out BoundRegistration? registration,
        out Diagnostic? diagnostic
    );

    /// <summary>
    /// Emits statements that register the bound implementation types.<br/>
    /// 바인딩된 구현 타입을 등록하는 문을 생성합니다.
    /// </summary>
    /// <param name="writer">
    /// The writer receiving the generated statements.<br/>
    /// 생성 문을 받을 작성기입니다.
    /// </param>
    /// <param name="registry">
    /// The registry targeted by the registrations.<br/>
    /// 등록 대상 레지스트리입니다.
    /// </param>
    /// <param name="registrations">
    /// The bound registrations to emit.<br/>
    /// 생성할 바인딩된 등록입니다.
    /// </param>
    protected virtual void EmitRegisterStatements(SourceWriter writer, RegistryDefinition registry, ImmutableArray<BoundRegistration> registrations)
    {
        foreach (BoundRegistration registration in registrations)
        {
            writer.AppendLine
            (
                // 생성 코드가 TypeRegistry.Register(Type)에 직접 바인딩됩니다.
                $"{GetRegistryAccess(registration.registry)}.Register(typeof({GeneratorUtils.GetTypeOfGenericDefinitionName(registration.implementationType)}));"
            );
        }
    }

    /// <summary>
    /// Emits statements that unregister each distinct bound implementation type.<br/>
    /// 서로 다른 바인딩 구현 타입을 해제 등록하는 문을 생성합니다.
    /// </summary>
    /// <param name="writer">
    /// The writer receiving the generated statements.<br/>
    /// 생성 문을 받을 작성기입니다.
    /// </param>
    /// <param name="registry">
    /// The registry targeted by the registrations.<br/>
    /// 등록 대상 레지스트리입니다.
    /// </param>
    /// <param name="registrations">
    /// The bound registrations to emit.<br/>
    /// 생성할 바인딩된 등록입니다.
    /// </param>
    protected virtual void EmitUnregisterStatements(SourceWriter writer, RegistryDefinition registry, ImmutableArray<BoundRegistration> registrations)
    {
        HashSet<INamedTypeSymbol> implementationTypes = new(SymbolEqualityComparer.Default);
        foreach (BoundRegistration registration in registrations)
        {
            if (!implementationTypes.Add(registration.implementationType))
                continue;

            writer.AppendLine
            (
                // 생성 코드가 TypeRegistry.Unregister(Type)에 직접 바인딩됩니다.
                $"{GetRegistryAccess(registration.registry)}.Unregister(typeof({GeneratorUtils.GetTypeOfGenericDefinitionName(registration.implementationType)}));"
            );
        }
    }

    /// <summary>
    /// Creates the source expression that accesses a registry property.<br/>
    /// 레지스트리 속성에 접근하는 소스 식을 만듭니다.
    /// </summary>
    /// <param name="registry">
    /// The registry definition whose property is accessed.<br/>
    /// 속성에 접근할 레지스트리 정의입니다.
    /// </param>
    /// <returns>
    /// A fully qualified owner type and escaped property access expression.<br/>
    /// 완전 수식 소유자 타입과 이스케이프된 속성 접근 식입니다.
    /// </returns>
    protected static string GetRegistryAccess(RegistryDefinition registry) => $"{GeneratorUtils.GetTypeName(registry.ownerType)}.{GeneratorUtils.EscapeIdentifier(registry.property.Name)}";

    /// <summary>
    /// Creates a discovery result for a registry attribute found in the current compilation.<br/>
    /// 현재 컴파일에서 발견한 레지스트리 특성의 발견 결과를 만듭니다.
    /// </summary>
    /// <param name="context">
    /// The syntax context containing the attributed target and semantic model.<br/>
    /// 특성 대상과 의미 모델을 포함하는 구문 컨텍스트입니다.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel semantic inspection.<br/>
    /// 의미 검사 취소에 사용할 토큰입니다.
    /// </param>
    /// <returns>
    /// A discovery result containing a validated definition or a diagnostic.<br/>
    /// 검증된 정의 또는 진단을 포함하는 발견 결과입니다.
    /// </returns>
    RegistryDiscoveryItem CreateCurrentRegistryDiscovery(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        Location location = context.TargetNode.GetLocation();
        if (context.TargetNode is not PropertyDeclarationSyntax declaration || context.TargetSymbol is not IPropertySymbol property)
        {
            return InvalidDiscovery
            (
                null,
                location,
                TypeRegistryDiagnostics.Create(TypeRegistryDiagnostics.invalidGenerateTarget, location, generateTypeRegistryAttributeMetadataName)
            );
        }

        if (context.SemanticModel.Compilation is CSharpCompilation { LanguageVersion: var languageVersion } && (int)languageVersion < minimumPartialPropertyLanguageVersion)
        {
            return InvalidDiscovery
            (
                property,
                location,
                TypeRegistryDiagnostics.Create
                (
                    TypeRegistryDiagnostics.unsupportedLanguageVersion,
                    location,
                    property.Name
                )
            );
        }

        if (!TryCreateRegistryDefinition(property, declaration, context.SemanticModel.Compilation, RegistryOrigin.currentCompilation, requirePartial: true, out RegistryDefinition? definition, out Diagnostic? diagnostic))
            return InvalidDiscovery(property, location, diagnostic!);

        return new RegistryDiscoveryItem(definition, property, location, isCurrent: true, ImmutableArray<Diagnostic>.Empty);
    }

    /// <summary>
    /// Validates a registry property and creates its normalized registry definition.<br/>
    /// 레지스트리 속성을 검증하고 정규화된 레지스트리 정의를 만듭니다.
    /// </summary>
    /// <param name="property">
    /// The property to validate.<br/>
    /// 검증할 속성입니다.
    /// </param>
    /// <param name="declaration">
    /// The source declaration when the property belongs to the current compilation; otherwise, <see langword="null"/>.<br/>
    /// 속성이 현재 컴파일에 속하면 소스 선언이고, 그렇지 않으면 <see langword="null"/>입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation used for type and accessibility checks.<br/>
    /// 타입 및 접근성 검사에 사용할 컴파일입니다.
    /// </param>
    /// <param name="origin">
    /// The origin recorded in the resulting definition.<br/>
    /// 결과 정의에 기록할 출처입니다.
    /// </param>
    /// <param name="requirePartial">
    /// Indicates whether the current-compilation <c>partial</c> property and containing hierarchy requirements apply.<br/>
    /// 현재 컴파일의 <c>partial</c> 속성 및 포함 계층 요구 사항을 적용할지 나타냅니다.
    /// </param>
    /// <param name="definition">
    /// Receives the created definition when validation succeeds; otherwise, <see langword="null"/>.<br/>
    /// 검증에 성공하면 생성된 정의를 받고, 그렇지 않으면 <see langword="null"/>입니다.
    /// </param>
    /// <param name="diagnostic">
    /// Receives the validation diagnostic when validation fails; otherwise, <see langword="null"/>.<br/>
    /// 검증에 실패하면 검증 진단을 받고, 그렇지 않으면 <see langword="null"/>입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the property satisfies all registry requirements; otherwise, <see langword="false"/>.<br/>
    /// 속성이 모든 레지스트리 요구 사항을 만족하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    bool TryCreateRegistryDefinition
    (
        IPropertySymbol property,
        PropertyDeclarationSyntax? declaration,
        Compilation compilation,
        RegistryOrigin origin,
        bool requirePartial,
        out RegistryDefinition? definition,
        out Diagnostic? diagnostic
    )
    {
        definition = null;
        diagnostic = null;
        Location location = declaration?.GetLocation() ?? TypeRegistrySymbolHelpers.GetLocation(property);

        if (requirePartial && declaration != null && HasManualPropertyImplementation(property))
        {
            diagnostic = TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.generatedMemberConflict,
                location,
                property.Name,
                GeneratorUtils.GetTypeName(property.ContainingType)
            );
            return false;
        }

        if (!IsValidPropertyContract(property, declaration, requirePartial))
        {
            diagnostic = TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.invalidPropertyContract,
                location,
                property.Name
            );
            return false;
        }

        INamedTypeSymbol ownerType = property.ContainingType;
        if (!IsPublicTypeHierarchy(ownerType) || (requirePartial && !TypeRegistrySymbolHelpers.IsPartialTypeHierarchy(ownerType)) || (ownerType.TypeKind != TypeKind.Class && ownerType.TypeKind != TypeKind.Struct))
        {
            diagnostic = TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.invalidContainingType,
                location,
                property.Name
            );
            return false;
        }

        if (property.Type is not INamedTypeSymbol registryType)
        {
            diagnostic = TypeRegistryDiagnostics.Create(TypeRegistryDiagnostics.invalidRegistryType, location, property.Name);
            return false;
        }

        INamedTypeSymbol? typeRegistry = compilation.GetTypeByMetadataName(typeRegistryMetadataName);
        if (typeRegistry == null || registryType.IsAbstract || !TypeRegistrySymbolHelpers.IsSameOrDerived(registryType, typeRegistry))
        {
            diagnostic = TypeRegistryDiagnostics.Create(TypeRegistryDiagnostics.invalidRegistryType, location, property.Name);
            return false;
        }

        if (!IsSupportedRegistryType(registryType, compilation))
        {
            diagnostic = TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.unsupportedRegistryType,
                location,
                GeneratorUtils.GetTypeName(registryType)
            );
            return false;
        }

        if (!TryValidateSupportedRegistryType(registryType, compilation, out diagnostic))
        {
            diagnostic ??= TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.unsupportedRegistryType,
                location,
                GeneratorUtils.GetTypeName(registryType)
            );
            return false;
        }

        if (!TypeRegistrySymbolHelpers.HasAccessibleParameterlessConstructor(registryType, compilation))
        {
            diagnostic = TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.missingParameterlessConstructor,
                location,
                GeneratorUtils.GetTypeName(registryType)
            );
            return false;
        }

        string stableId = TypeRegistryEmitter.GetStableId(property, registryType);
        RegistryDefinition candidate = new(property, ownerType, registryType, origin, stableId);
        string backingFieldName = TypeRegistryEmitter.GetBackingFieldName(candidate);
        if (origin == RegistryOrigin.currentCompilation && ownerType.GetMembers(backingFieldName).Length != 0)
        {
            diagnostic = TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.generatedMemberConflict,
                location,
                backingFieldName,
                GeneratorUtils.GetTypeName(ownerType)
            );
            return false;
        }

        definition = candidate;
        return true;
    }

    /// <summary>
    /// Checks the accessibility, <c>static</c>, accessor, indexer, and <c>partial</c> requirements of a registry property.<br/>
    /// 레지스트리 속성의 접근성, <c>static</c> 여부, 접근자, 인덱서, <c>partial</c> 요구 사항을 확인합니다.
    /// </summary>
    /// <param name="property">
    /// The property symbol to inspect.<br/>
    /// 검사할 속성 심볼입니다.
    /// </param>
    /// <param name="declaration">
    /// The source declaration used for <c>partial</c> syntax validation.<br/>
    /// <c>partial</c> 구문 검증에 사용할 소스 선언입니다.
    /// </param>
    /// <param name="requirePartial">
    /// Indicates whether partial declaration requirements are enforced.<br/>
    /// partial 선언 요구 사항을 적용할지 나타냅니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the property satisfies the selected contract; otherwise, <see langword="false"/>.<br/>
    /// 속성이 선택한 계약을 만족하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    // emitter가 public static partial get-only 속성을 완성하므로 검증과 생성 선언을 함께 유지해야 합니다.
    static bool IsValidPropertyContract(IPropertySymbol property, PropertyDeclarationSyntax? declaration, bool requirePartial)
    {
        if (property.IsIndexer || !property.IsStatic || property.DeclaredAccessibility != Accessibility.Public || property.GetMethod == null || property.SetMethod != null || property.GetMethod.DeclaredAccessibility != Accessibility.Public)
            return false;

        if (!requirePartial)
            return true;

        if (declaration == null || !TypeRegistrySymbolHelpers.IsPartialPropertyDefinition(declaration))
            return false;

        return !HasManualPropertyImplementation(property);
    }

    /// <summary>
    /// Determines whether any source declaration of a property contains an implementation body.<br/>
    /// 속성의 소스 선언 중 구현 본문을 포함하는 선언이 있는지 확인합니다.
    /// </summary>
    /// <param name="property">
    /// The property whose declarations are inspected.<br/>
    /// 선언을 검사할 속성입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when an expression body or accessor body is present; otherwise, <see langword="false"/>.<br/>
    /// 식 본문 또는 접근자 본문이 있으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    static bool HasManualPropertyImplementation(IPropertySymbol property)
    {
        foreach (SyntaxReference reference in property.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is PropertyDeclarationSyntax declaration && (declaration.ExpressionBody != null || declaration.AccessorList?.Accessors.Any(x => x.Body != null || x.ExpressionBody != null) == true))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a type and all containing types are public.<br/>
    /// 타입과 모든 포함 타입이 public인지 확인합니다.
    /// </summary>
    /// <param name="type">
    /// The type whose containing hierarchy is inspected.<br/>
    /// 포함 타입 계층을 검사할 타입입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when every type in the hierarchy is public; otherwise, <see langword="false"/>.<br/>
    /// 계층의 모든 타입이 public이면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    static bool IsPublicTypeHierarchy(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current != null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a failed current-compilation discovery result containing one diagnostic.<br/>
    /// 하나의 진단을 포함하는 현재 컴파일 발견 실패 결과를 만듭니다.
    /// </summary>
    /// <param name="property">
    /// The discovered property, or <see langword="null"/> when no property was found.<br/>
    /// 발견한 속성이며, 속성을 찾지 못하면 <see langword="null"/>입니다.
    /// </param>
    /// <param name="location">
    /// The source location associated with the failure.<br/>
    /// 실패와 연결된 소스 위치입니다.
    /// </param>
    /// <param name="diagnostic">
    /// The diagnostic describing the discovery failure.<br/>
    /// 발견 실패를 설명하는 진단입니다.
    /// </param>
    /// <returns>
    /// A current-compilation discovery item with no definition and the supplied diagnostic.<br/>
    /// 정의 없이 지정된 진단을 포함하는 현재 컴파일 발견 항목입니다.
    /// </returns>
    static RegistryDiscoveryItem InvalidDiscovery(IPropertySymbol? property, Location location, Diagnostic diagnostic) => new RegistryDiscoveryItem(null, property, location, isCurrent: true, ImmutableArray.Create(diagnostic));

    /// <summary>
    /// Validates discovered registries, binds candidates, and emits generated source and diagnostics.<br/>
    /// 발견한 레지스트리를 검증하고 후보를 바인딩한 뒤 생성 소스와 진단을 출력합니다.
    /// </summary>
    /// <param name="context">
    /// The source production context used to add source and report diagnostics.<br/>
    /// 소스를 추가하고 진단을 보고하는 데 사용할 소스 생성 컨텍스트입니다.
    /// </param>
    /// <param name="input">
    /// The compilation, discovered registries, and registration candidates supplied by the incremental pipeline.<br/>
    /// 증분 파이프라인이 제공하는 컴파일, 발견된 레지스트리, 등록 후보입니다.
    /// </param>
    /// <param name="registrationTypeName">
    /// The unique registration type name shared by the lifecycle declaration and implementation sources.<br/>
    /// lifecycle 선언 소스와 구현 소스가 공유하는 고유 등록 타입 이름입니다.
    /// </param>
    void Execute
    (
        SourceProductionContext context,
        ((Compilation, ImmutableArray<RegistryDiscoveryItem>), ImmutableArray<RegistrationCandidate>) input,
        string registrationTypeName
    )
    {
        Compilation compilation = input.Item1.Item1;
        ImmutableArray<RegistryDiscoveryItem> currentItems = input.Item1.Item2;
        ImmutableArray<RegistrationCandidate> candidates = input.Item2;
        HashSet<string> reportedDiagnosticKeys = [];

        void Report(Diagnostic diagnostic)
        {
            string key = $"{diagnostic.Id}|{diagnostic.Location.SourceTree?.FilePath}|{diagnostic.Location.SourceSpan.Start}|{diagnostic.GetMessage()}";
            if (reportedDiagnosticKeys.Add(key))
                context.ReportDiagnostic(diagnostic);
        }

        List<RegistryDefinition> definitions = [];
        HashSet<IPropertySymbol> seenProperties = new(SymbolEqualityComparer.Default);
        foreach (RegistryDiscoveryItem item in currentItems)
        {
            foreach (Diagnostic diagnostic in item.diagnostics)
                Report(diagnostic);

            if (item.definition != null && seenProperties.Add(item.definition.property))
                definitions.Add(item.definition);
        }

        foreach (RegistryDiscoveryItem item in DiscoverManifestRegistries(compilation))
        {
            foreach (Diagnostic diagnostic in item.diagnostics)
                Report(diagnostic);

            if (item.definition != null && seenProperties.Add(item.definition.property))
                definitions.Add(item.definition);
        }

        Dictionary<string, string> hintOwners = [];
        foreach (RegistryDefinition registry in definitions)
        {
            if (registry.origin != RegistryOrigin.currentCompilation)
                continue;

            AddGeneratedSource
            (
                context,
                hintOwners,
                TypeRegistryEmitter.GetPropertyHintName(registry),
                registry.stableId,
                TypeRegistryEmitter.RenderPropertyImplementation(registry, CreateRegistryInitializer(registry)),
                TypeRegistrySymbolHelpers.GetLocation(registry.property),
                Report
            );
            AddGeneratedSource
            (
                context,
                hintOwners,
                TypeRegistryEmitter.GetManifestHintName(registry),
                registry.stableId,
                TypeRegistryEmitter.RenderManifest(registry),
                TypeRegistrySymbolHelpers.GetLocation(registry.property),
                Report
            );
        }

        List<RegistrationCandidate> uniqueCandidates = [];
        HashSet<ISymbol> seenCandidates = new(SymbolEqualityComparer.Default);
        foreach (RegistrationCandidate candidate in candidates)
        {
            if (seenCandidates.Add(candidate.symbol))
                uniqueCandidates.Add(candidate);
        }

        List<BoundRegistration> registrations = [];
        foreach (RegistryDefinition registry in definitions)
        {
            foreach (RegistrationCandidate candidate in uniqueCandidates)
            {
                if (TryBindCandidate(registry, candidate, compilation, out BoundRegistration? registration, out Diagnostic? diagnostic))
                {
                    if (registration != null)
                        registrations.Add(registration);
                }

                if (diagnostic != null)
                    Report(diagnostic);
            }
        }

        List<BoundRegistration> emitRegistrations = [];
        HashSet<IPropertySymbol> genericOwnerDiagnostics = new(SymbolEqualityComparer.Default);
        foreach (BoundRegistration registration in registrations)
        {
            if (registration.registry.ownerType.IsGenericType)
            {
                if (genericOwnerDiagnostics.Add(registration.registry.property))
                {
                    Report
                    (
                        TypeRegistryDiagnostics.Create
                        (
                            TypeRegistryDiagnostics.genericOwnerRegistration,
                            TypeRegistrySymbolHelpers.GetLocation(registration.registry.property),
                            registration.registry.property.Name
                        )
                    );
                }

                continue;
            }

            emitRegistrations.Add(registration);
        }

        if (emitRegistrations.Count == 0)
            return;

        List<RegistryRegistrationGroup> groups = [];
        foreach (RegistryDefinition registry in definitions)
        {
            ImmutableArray<BoundRegistration> registryRegistrations = emitRegistrations.Where(x => x.registry.Equals(registry)).ToImmutableArray();
            if (registryRegistrations.Length != 0)
                groups.Add(new RegistryRegistrationGroup(registry, registryRegistrations));
        }

        if (groups.Count == 0)
            return;

        string registrationStableId = string.Join("|", groups.Select(x => x.registry.stableId));
        string registrationHintName = GeneratorUtils.GetRegistrationHintName(generatorName, registrationStableId);
        SourceWriter writer = TypeRegistryEmitter.CreateRegistrationWriter(registrationTypeName);
        writer.AppendLine("#pragma warning disable CS0618");
        writer.AppendLine();
        writer.AppendLine("static partial void RegisterGeneratedTypesCore()");
        writer.AppendLine("{");
        writer.Indent();
        foreach (RegistryRegistrationGroup group in groups)
            EmitRegisterStatements(writer, group.registry, group.registrations);
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
        writer.AppendLine("static partial void UnregisterGeneratedTypesCore()");
        writer.AppendLine("{");
        writer.Indent();
        foreach (RegistryRegistrationGroup group in groups)
            EmitUnregisterStatements(writer, group.registry, group.registrations);
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("#pragma warning restore CS0618");

        string registrationSource = TypeRegistryEmitter.FinishRegistration(writer);
        AddGeneratedSource(context, hintOwners, registrationHintName, registrationStableId, registrationSource, Location.None, Report);
    }

    /// <summary>
    /// Determines whether a public lifecycle API type is supplied by a referenced assembly.<br/>
    /// public 수명 주기 API 타입이 참조 어셈블리에서 제공되는지 확인합니다.
    /// </summary>
    /// <param name="compilation">
    /// The compilation whose referenced API types are inspected.<br/>
    /// 참조된 API 타입을 검사할 컴파일입니다.
    /// </param>
    /// <param name="metadataName">
    /// The metadata name of the API type to inspect.<br/>
    /// 검사할 API 타입의 메타데이터 이름입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a public type with the metadata name is available from a referenced assembly; otherwise, <see langword="false"/>.<br/>
    /// 해당 메타데이터 이름의 public 타입을 참조 어셈블리에서 사용할 수 있으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.
    /// </returns>
    protected static bool HasReferencedLifecycleApi(Compilation compilation, string metadataName)
    {
        return EnumerateReferencedAssemblies(compilation)
            .Any(assembly => assembly.GetTypeByMetadataName(metadataName) is { DeclaredAccessibility: Accessibility.Public });
    }

    /// <summary>
    /// Adds compile-only lifecycle API compatibility declarations when Unity.Scripting is not referenced.<br/>
    /// Unity.Scripting이 참조되지 않은 경우 컴파일 전용 수명 주기 API 호환 선언을 추가합니다.
    /// </summary>
    /// <param name="context">
    /// The source production context receiving the compatibility source.<br/>
    /// 호환 소스를 받을 소스 생성 컨텍스트입니다.
    /// </param>
    /// <param name="compilation">
    /// The current compilation whose lifecycle API references are inspected.<br/>
    /// 수명 주기 API 참조를 검사할 현재 컴파일입니다.
    /// </param>
    /// <param name="generatorName">
    /// The generator name used to keep the compatibility hint name unique.<br/>
    /// 호환 힌트 이름을 고유하게 유지하는 데 사용할 생성기 이름입니다.
    /// </param>
    static void EmitLifecycleCompatibility(SourceProductionContext context, Compilation compilation, string generatorName)
    {
        bool hasLoadedAttribute = HasAvailableLifecycleApi(compilation, onAssemblyLoadedAttributeMetadataName);
        bool hasUnloadingAttribute = HasAvailableLifecycleApi(compilation, onAssemblyUnloadingAttributeMetadataName);
        if (hasLoadedAttribute && hasUnloadingAttribute)
            return;

        context.AddSource
        (
            $"RuniOS.{generatorName}.LifecycleCompatibility.g.cs",
            SourceText.From
            (
                TypeRegistryEmitter.RenderLifecycleCompatibility
                (
                    hasLoadedAttribute,
                    hasUnloadingAttribute
                ),
                Encoding.UTF8
            )
        );
    }

    /// <summary>
    /// Determines whether a public lifecycle API type is available to the current compilation.<br/>
    /// 현재 컴파일에서 public 수명 주기 API 타입을 사용할 수 있는지 확인합니다.
    /// </summary>
    /// <param name="compilation">
    /// The compilation whose lifecycle API types are inspected.<br/>
    /// 수명 주기 API 타입을 검사할 컴파일입니다.
    /// </param>
    /// <param name="metadataName">
    /// The metadata name of the API type to inspect.<br/>
    /// 검사할 API 타입의 메타데이터 이름입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a public type with the metadata name is available; otherwise, <see langword="false"/>.<br/>
    /// 해당 메타데이터 이름의 public 타입을 사용할 수 있으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.
    /// </returns>
    static bool HasAvailableLifecycleApi(Compilation compilation, string metadataName)
    {
        if (compilation.GetTypeByMetadataName(metadataName) is { DeclaredAccessibility: Accessibility.Public })
            return true;

        return HasReferencedLifecycleApi(compilation, metadataName);
    }

    /// <summary>
    /// Discovers registry definitions restored from manifests in referenced assemblies.<br/>
    /// 참조된 어셈블리의 매니페스트에서 복원할 수 있는 레지스트리 정의를 발견합니다.
    /// </summary>
    /// <param name="compilation">
    /// The compilation whose referenced assemblies are inspected.<br/>
    /// 참조 어셈블리를 검사할 컴파일입니다.
    /// </param>
    /// <returns>
    /// Discovery items for valid manifests and diagnostics for invalid manifests.<br/>
    /// 유효한 매니페스트의 발견 항목과 유효하지 않은 매니페스트의 진단입니다.
    /// </returns>
    ImmutableArray<RegistryDiscoveryItem> DiscoverManifestRegistries(Compilation compilation)
    {
        INamedTypeSymbol? manifestAttribute = compilation.GetTypeByMetadataName(typeRegistryManifestAttributeMetadataName);
        if (manifestAttribute == null)
            return ImmutableArray<RegistryDiscoveryItem>.Empty;

        ImmutableArray<RegistryDiscoveryItem>.Builder result = ImmutableArray.CreateBuilder<RegistryDiscoveryItem>();
        foreach (IAssemblySymbol assembly in EnumerateReferencedAssemblies(compilation))
        {
            foreach (AttributeData manifest in assembly.GetAttributes())
            {
                if (manifest.AttributeClass == null || GeneratorUtils.GetMetadataName(manifest.AttributeClass) != typeRegistryManifestAttributeMetadataName)
                    continue;

                string ownerName = "<unknown>";
                string propertyName = "<unknown>";
                if (manifest.ConstructorArguments.Length > 0 && manifest.ConstructorArguments[0].Value is ITypeSymbol ownerSymbol)
                    ownerName = GeneratorUtils.GetTypeName(ownerSymbol);
                if (manifest.ConstructorArguments.Length > 1 && manifest.ConstructorArguments[1].Value is string manifestPropertyName)
                    propertyName = manifestPropertyName;

                // 매니페스트 계약은 positional `(Type ownerType, string propertyName)` 생성자 인자로 소비됩니다.
                INamedTypeSymbol? ownerType = manifest.ConstructorArguments.Length > 0 ? manifest.ConstructorArguments[0].Value as INamedTypeSymbol : null;
                IPropertySymbol? property = ownerType?.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault();
                if (ownerType == null || property == null || !TryCreateRegistryDefinition(property, null, compilation, RegistryOrigin.referencedAssemblyManifest, requirePartial: false, out RegistryDefinition? definition, out _))
                {
                    result.Add
                    (
                        new RegistryDiscoveryItem
                        (
                            null,
                            property,
                            Location.None,
                            isCurrent: false,
                            ImmutableArray.Create
                            (
                                TypeRegistryDiagnostics.Create
                                (
                                    TypeRegistryDiagnostics.invalidManifest,
                                    Location.None,
                                    assembly.Identity.Name,
                                    ownerName,
                                    propertyName
                                )
                            )
                        )
                    );
                    continue;
                }

                result.Add(new RegistryDiscoveryItem(definition, property, Location.None, isCurrent: false, ImmutableArray<Diagnostic>.Empty));
            }
        }

        return result.ToImmutable();
    }

    /// <summary>
    /// Enumerates all reachable referenced assemblies without yielding an assembly more than once.<br/>
    /// 도달 가능한 모든 참조 어셈블리를 중복 없이 열거합니다.
    /// </summary>
    /// <param name="compilation">
    /// The compilation whose direct references seed the traversal.<br/>
    /// 직접 참조를 순회의 시작점으로 사용할 컴파일입니다.
    /// </param>
    /// <returns>
    /// Referenced assemblies in traversal order.<br/>
    /// 순회 순서의 참조 어셈블리 열거입니다.
    /// </returns>
    static IEnumerable<IAssemblySymbol> EnumerateReferencedAssemblies(Compilation compilation)
    {
        HashSet<IAssemblySymbol> seen = new(SymbolEqualityComparer.Default);
        Stack<IAssemblySymbol> pending = new();
        foreach (IAssemblySymbol assembly in compilation.SourceModule.ReferencedAssemblySymbols)
            pending.Push(assembly);

        while (pending.Count != 0)
        {
            IAssemblySymbol assembly = pending.Pop();
            if (!seen.Add(assembly))
                continue;

            yield return assembly;
            foreach (IModuleSymbol module in assembly.Modules)
            {
                foreach (IAssemblySymbol referencedAssembly in module.ReferencedAssemblySymbols)
                    pending.Push(referencedAssembly);
            }
        }
    }

    /// <summary>
    /// Adds generated source once and reports a diagnostic when the hint name maps to another stable identifier.<br/>
    /// 생성 소스를 한 번 추가하고 힌트 이름이 다른 안정 식별자와 매핑되면 진단을 보고합니다.
    /// </summary>
    /// <param name="context">
    /// The source production context receiving the source.<br/>
    /// 소스를 받을 소스 생성 컨텍스트입니다.
    /// </param>
    /// <param name="hintOwners">
    /// The map of hint names to stable identifiers already emitted in this execution.<br/>
    /// 이 실행에서 이미 출력한 힌트 이름과 안정 식별자의 매핑입니다.
    /// </param>
    /// <param name="hintName">
    /// The source hint name to add.<br/>
    /// 추가할 소스 힌트 이름입니다.
    /// </param>
    /// <param name="stableId">
    /// The stable identifier owning <paramref name="hintName"/>.<br/>
    /// <paramref name="hintName"/>을 소유하는 안정 식별자입니다.
    /// </param>
    /// <param name="source">
    /// The generated source text.<br/>
    /// 생성된 소스 텍스트입니다.
    /// </param>
    /// <param name="location">
    /// The source location used when reporting a collision.<br/>
    /// 충돌을 보고할 때 사용할 소스 위치입니다.
    /// </param>
    /// <param name="report">
    /// The callback used to report a generated diagnostic.<br/>
    /// 생성된 진단을 보고하는 콜백입니다.
    /// </param>
    static void AddGeneratedSource
    (
        SourceProductionContext context,
        Dictionary<string, string> hintOwners,
        string hintName,
        string stableId,
        string source,
        Location location,
        Action<Diagnostic> report
    )
    {
        if (hintOwners.TryGetValue(hintName, out string? previousStableId))
        {
            if (previousStableId != stableId)
            {
                report
                (
                    TypeRegistryDiagnostics.Create
                    (
                        TypeRegistryDiagnostics.hintNameCollision,
                        location,
                        hintName,
                        previousStableId,
                        stableId
                    )
                );
            }

            return;
        }

        hintOwners.Add(hintName, stableId);
        context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
    }

}
