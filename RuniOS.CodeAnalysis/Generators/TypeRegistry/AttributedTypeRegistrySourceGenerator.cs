using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RuniOS.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace RuniOS.CodeAnalysis.Generators.TypeRegistry;

/// <summary>
/// Generates registrations for concrete types carrying attributes derived from a registry's attribute base type.<br/>
/// 레지스트리의 특성 기본 타입에서 파생된 특성을 가진 구체적인 타입의 등록을 생성합니다.
/// </summary>
[Generator]
public sealed class AttributedTypeRegistrySourceGenerator : TypeRegistrySourceGenerator
{
    // 런타임 제네릭 정의의 정확한 메타데이터 이름과 TAttribute 인자 위치를 유지해야 합니다.
    const string attributedTypeRegistryMetadataName = "RuniOS.Reflection.AttributedTypeRegistry`1";
    // 후보 발견과 타입 매칭을 위해 런타임 등록 특성의 정확한 메타데이터 이름을 유지해야 합니다.
    const string typeRegistrationAttributeMetadataName = "RuniOS.Reflection.TypeRegistrationAttribute";

    /// <summary>
    /// Gets the generator name embedded in generated registration source identifiers.<br/>
    /// 생성된 등록 소스 식별자에 포함할 생성기 이름을 가져옵니다.
    /// </summary>
    protected override string generatorName => "AttributedTypeRegistry";

    /// <summary>
    /// Creates the initializer that supplies the registry base type.<br/>
    /// 레지스트리 기본 타입을 전달하는 초기화 식을 만듭니다.
    /// </summary>
    /// <param name="registry">
    /// The registry definition being initialized.<br/>
    /// 초기화할 레지스트리 정의입니다.
    /// </param>
    /// <returns>
    /// A C# expression that creates the registry with its base type.<br/>
    /// 기본 타입으로 레지스트리를 생성하는 C# 식입니다.
    /// </returns>
    protected override string CreateRegistryInitializer(RegistryDefinition registry) =>
        $"new {GeneratorUtils.GetTypeName(registry.registryType)}(typeof({GeneratorUtils.GetTypeOfName(registry.baseType)}))";

    /// <summary>
    /// Determines whether the registry has an accessible constructor accepting <see cref="System.Type"/>.<br/>
    /// 레지스트리에 <see cref="System.Type"/>을 받는 접근 가능한 생성자가 있는지 확인합니다.
    /// </summary>
    /// <param name="registryType">
    /// The registry type whose constructors are inspected.<br/>
    /// 생성자를 검사할 레지스트리 타입입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation used for accessibility checks.<br/>
    /// 접근성 검사에 사용할 컴파일입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when an accessible <see cref="System.Type"/> constructor exists; otherwise, <see langword="false"/>.<br/>
    /// 접근 가능한 <see cref="System.Type"/> 생성자가 있으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    protected override bool HasAccessibleRegistryConstructor(INamedTypeSymbol registryType, Compilation compilation)
    {
        foreach (IMethodSymbol constructor in registryType.InstanceConstructors)
        {
            INamedTypeSymbol? systemType = compilation.GetTypeByMetadataName("System.Type");
            if (systemType == null)
                return false;

            if (constructor.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, systemType) && TypeRegistrySymbolHelpers.IsAccessibleFromGeneratedCode(constructor, compilation))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the registry is an <c>AttributedTypeRegistry&lt;TAttribute&gt;</c> definition.<br/>
    /// 레지스트리가 <c>AttributedTypeRegistry&lt;TAttribute&gt;</c> 정의인지 확인합니다.
    /// </summary>
    /// <param name="registryType">
    /// The registry type to inspect.<br/>
    /// 검사할 레지스트리 타입입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation used to resolve the attributed registry definition.<br/>
    /// 특성 기반 레지스트리 정의를 확인하는 데 사용할 컴파일입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the registry's original definition is <c>AttributedTypeRegistry&lt;TAttribute&gt;</c>; otherwise, <see langword="false"/>.<br/>
    /// 레지스트리의 원본 정의가 <c>AttributedTypeRegistry&lt;TAttribute&gt;</c>이면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    protected override bool IsSupportedRegistryType(INamedTypeSymbol registryType, Compilation compilation)
    {
        INamedTypeSymbol? attributedRegistry = compilation.GetTypeByMetadataName(attributedTypeRegistryMetadataName);
        return attributedRegistry != null && SymbolEqualityComparer.Default.Equals(registryType.OriginalDefinition, attributedRegistry);
    }

    /// <summary>
    /// Validates that the registry's attribute type derives from <c>TypeRegistrationAttribute</c>.<br/>
    /// 레지스트리의 특성 타입이 <c>TypeRegistrationAttribute</c>에서 파생되었는지 검증합니다.
    /// </summary>
    /// <param name="registryType">
    /// The attributed registry type to validate.<br/>
    /// 검증할 특성 기반 레지스트리 타입입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation used to resolve <c>TypeRegistrationAttribute</c>.<br/>
    /// <c>TypeRegistrationAttribute</c>를 확인하는 데 사용할 컴파일입니다.
    /// </param>
    /// <param name="diagnostic">
    /// Receives the invalid attribute-base diagnostic when validation fails; otherwise, <see langword="null"/>.<br/>
    /// 검증에 실패하면 잘못된 특성 기본 타입 진단을 받고, 그렇지 않으면 <see langword="null"/>입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the attribute type is valid; otherwise, <see langword="false"/>.<br/>
    /// 특성 타입이 유효하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    protected override bool TryValidateSupportedRegistryType(INamedTypeSymbol registryType, Compilation compilation, out Diagnostic? diagnostic)
    {
        INamedTypeSymbol? registrationAttribute = compilation.GetTypeByMetadataName(typeRegistrationAttributeMetadataName);

        // ReSharper disable once MergeIntoNegatedPattern
        if (registrationAttribute == null || registryType.TypeArguments.Length != 1 || registryType.TypeArguments[0] is not INamedTypeSymbol attributeType || !TypeRegistrySymbolHelpers.IsSameOrDerived(attributeType, registrationAttribute))
        {
            diagnostic = TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.invalidAttributeBase,
                TypeRegistrySymbolHelpers.GetLocation(registryType),
                compilation,
                registryType.TypeArguments.Length == 1 ? registryType.TypeArguments[0] : "<unknown>"
            );
            return false;
        }

        diagnostic = null;
        return true;
    }

    /// <summary>
    /// Creates a provider that examines class and record declarations for inherited registration attributes.<br/>
    /// 상속된 등록 특성을 찾기 위해 클래스 및 레코드 선언을 검사하는 프로바이더를 만듭니다.
    /// </summary>
    /// <param name="context">
    /// The incremental generator initialization context used to create the syntax provider.<br/>
    /// 구문 프로바이더를 만드는 데 사용할 증분 생성기 초기화 컨텍스트입니다.
    /// </param>
    /// <returns>
    /// A provider that yields attributed registration candidates.<br/>
    /// 특성 기반 등록 후보를 생성하는 프로바이더입니다.
    /// </returns>
    protected override IncrementalValuesProvider<RegistrationCandidate> CreateCandidateProvider(IncrementalGeneratorInitializationContext context) =>
        context.SyntaxProvider.CreateSyntaxProvider
        (
            static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
            static (syntaxContext, cancellationToken) => CreateCandidate(syntaxContext, cancellationToken)
        )
        .Where(static candidate => candidate != null)
        .Select(static RegistrationCandidate (candidate, _) => candidate!);

    /// <summary>
    /// Binds a candidate when it matches the registry base type and attribute type, rendering its attributes for generated code.<br/>
    /// 후보가 레지스트리 기본 타입 및 특성 타입과 일치하면 생성 코드용 특성을 변환해 바인딩합니다.
    /// </summary>
    /// <param name="registry">
    /// The attributed registry receiving the candidate.<br/>
    /// 후보를 받을 특성 기반 레지스트리입니다.
    /// </param>
    /// <param name="candidate">
    /// The candidate to match and bind.<br/>
    /// 일치 여부를 확인하고 바인딩할 후보입니다.
    /// </param>
    /// <param name="compilation">
    /// The compilation used for type accessibility and attribute rendering checks.<br/>
    /// 타입 접근성 및 특성 변환 검사에 사용할 컴파일입니다.
    /// </param>
    /// <param name="registration">
    /// Receives the bound registration when the candidate is accepted; otherwise, <see langword="null"/>.<br/>
    /// 후보를 수락하면 바인딩된 등록을 받고, 그렇지 않으면 <see langword="null"/>입니다.
    /// </param>
    /// <param name="diagnostic">
    /// Receives a diagnostic when the candidate is rejected for an error condition; otherwise, <see langword="null"/>.<br/>
    /// 오류 조건으로 후보를 거부하면 진단을 받고, 그렇지 않으면 <see langword="null"/>입니다.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the candidate is bound; otherwise, <see langword="false"/>.<br/>
    /// 후보가 바인딩되면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
    /// </returns>
    protected override bool TryBindCandidate
    (
        RegistryDefinition registry,
        RegistrationCandidate candidate,
        Compilation compilation,
        out BoundRegistration? registration,
        out Diagnostic? diagnostic
    )
    {
        registration = null;
        diagnostic = null;
        // 바인딩과 생성 항목 구성에서 TAttribute는 인자 0이며, 기본 타입은 RegistryDefinition에 저장됩니다.
        if (candidate is not AttributedRegistrationCandidate attributedCandidate || registry.registryType.TypeArguments.Length != 1)
            return false;

        if (registry.registryType.TypeArguments[0] is not INamedTypeSymbol attributeType)
            return false;

        ImmutableArray<AttributeData> matchingAttributes = attributedCandidate.attributes
            .Where(attribute => TypeRegistrySymbolHelpers.IsSameOrDerived(attribute.AttributeClass, attributeType))
            .ToImmutableArray();
        if (matchingAttributes.Length == 0)
            return false;

        if (!TypeRegistrySymbolHelpers.IsSameOrDerived(attributedCandidate.implementationType, registry.baseType))
            return false;
        if (attributedCandidate.implementationType.IsAbstract)
        {
            diagnostic = TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.abstractCandidate,
                TypeRegistrySymbolHelpers.GetLocation(attributedCandidate.implementationType),
                compilation,
                attributedCandidate.implementationType
            );
            return false;
        }
        if (!TypeRegistrySymbolHelpers.IsAccessibleFromGeneratedCode(attributedCandidate.implementationType, compilation))
        {
            diagnostic = TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.inaccessibleAttribute,
                TypeRegistrySymbolHelpers.GetLocation(attributedCandidate.implementationType),
                compilation,
                attributedCandidate.implementationType
            );
            return false;
        }

        if
        (
            !HasReferencedLifecycleApi(compilation, onAssemblyLoadedAttributeMetadataName) ||
            !HasReferencedLifecycleApi(compilation, onAssemblyUnloadingAttributeMetadataName) ||
            !HasReferencedLifecycleApi(compilation, lifecycleMethodRegistrationMetadataName)
        )
        {
            diagnostic = TypeRegistryDiagnostics.Create
            (
                TypeRegistryDiagnostics.missingLifecycleApi,
                TypeRegistrySymbolHelpers.GetLocation(matchingAttributes[0]),
                compilation
            );
            return false;
        }

        ImmutableArray<string>.Builder attributeExpressions = ImmutableArray.CreateBuilder<string>(matchingAttributes.Length);
        foreach (AttributeData attribute in matchingAttributes)
        {
            if (!AttributeLiteralEmitter.TryRender(attribute, compilation, out string expression, out diagnostic))
                return false;
            attributeExpressions.Add(expression);
        }

        registration = new BoundRegistration
        (
            registry,
            attributedCandidate.implementationType,
            new AttributedRegistrationPayload(attributeExpressions.ToImmutable())
        );
        return true;
    }

    /// <summary>
    /// Emits a <c>DirectRegisterRange</c> call containing one entry for each rendered registration attribute.<br/>
    /// 변환된 등록 특성마다 하나의 항목을 포함하는 <c>DirectRegisterRange</c> 호출을 생성합니다.
    /// </summary>
    /// <param name="writer">
    /// The writer receiving the generated registration statements.<br/>
    /// 생성된 등록 문을 받을 작성기입니다.
    /// </param>
    /// <param name="registry">
    /// The registry targeted by the entries.<br/>
    /// 항목이 대상으로 하는 레지스트리입니다.
    /// </param>
    /// <param name="registrations">
    /// The attributed registrations whose entries are emitted.<br/>
    /// 항목을 생성할 특성 기반 등록입니다.
    /// </param>
    protected override void EmitRegisterStatements(SourceWriter writer, RegistryDefinition registry, ImmutableArray<BoundRegistration> registrations)
    {
        string registryAccess = GetRegistryAccess(registry);
        string entryType = TypeRegistryEmitter.RenderRegistrationEntryType(registry);
        int remainingEntryCount = registrations.Sum(static registration =>
            registration.payload is AttributedRegistrationPayload payload ? payload.attributeExpressions.Length : 0);

        // 생성 코드가 AttributedTypeRegistry.DirectRegisterRange(params ReadOnlySpan<RegistrationEntry<TAttribute>>)에 직접 바인딩됩니다.
        writer.AppendLine($"{registryAccess}.DirectRegisterRange(");
        writer.Indent();

        foreach (BoundRegistration registration in registrations)
        {
            if (registration.payload is not AttributedRegistrationPayload payload)
                continue;

            foreach (string attributeExpression in payload.attributeExpressions)
            {
                remainingEntryCount--;
                writer.AppendLine($"new {entryType}(");
                writer.Indent();
                writer.AppendLine($"typeof({GeneratorUtils.GetTypeOfGenericDefinitionName(registration.implementationType)}),");
                writer.AppendLine($"{attributeExpression}){(remainingEntryCount > 0 ? "," : string.Empty)}");
                writer.Unindent();
            }
        }

        writer.AppendLine(");");
        writer.Unindent();
    }

    /// <summary>
    /// Creates a candidate from a class-like declaration when inherited registration attributes are present.<br/>
    /// 상속된 등록 특성이 있는 클래스 계열 선언에서 후보를 만듭니다.
    /// </summary>
    /// <param name="context">
    /// The syntax context containing the declaration and semantic model.<br/>
    /// 선언과 의미 모델을 포함하는 구문 컨텍스트입니다.
    /// </param>
    /// <param name="cancellationToken">
    /// The token used to cancel symbol lookup.<br/>
    /// 심볼 조회 취소에 사용할 토큰입니다.
    /// </param>
    /// <returns>
    /// An attributed candidate when matching attributes exist; otherwise, <see langword="null"/>.<br/>
    /// 일치하는 특성이 있으면 특성 기반 후보를, 그렇지 않으면 <see langword="null"/>을 반환합니다.
    /// </returns>
    static AttributedRegistrationCandidate? CreateCandidate(GeneratorSyntaxContext context, System.Threading.CancellationToken cancellationToken)
    {
        if (context.Node is not TypeDeclarationSyntax || context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol { TypeKind: TypeKind.Class } implementationType)
            return null;

        INamedTypeSymbol? registrationAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName(typeRegistrationAttributeMetadataName);
        if (registrationAttribute == null)
            return null;

        ImmutableArray<AttributeData> attributes = TypeRegistrySymbolHelpers.GetInheritedAttributes(implementationType, registrationAttribute);
        return attributes.Length == 0 ? null : new AttributedRegistrationCandidate(implementationType, attributes);
    }
}
