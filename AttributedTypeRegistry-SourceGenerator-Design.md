# 공통 TypeRegistry 소스 제너레이터 설계

## 1. 목적

이 문서는 `TypeRegistry` 계열 구현마다 반복되는 다음 작업을 공통 소스 제너레이터 기반으로 통합하는 설계를 정의한다.

- 레지스트리 선언 탐색과 계약 검증
- 현재 컴파일 및 참조 어셈블리의 레지스트리 발견
- partial property 구현과 backing field 생성
- 어셈블리 로드/언로드 시점의 등록 및 해제 코드 생성
- 후보 타입과 레지스트리의 매칭 및 등록 코드 출력
- 공통 진단과 안정적인 생성 파일 이름 관리

공통 추상 클래스 `TypeRegistrySourceGenerator`가 위 골격을 담당하고, 실제 레지스트리별 소스 제너레이터는 후보 탐색과 매칭, 필요한 등록 최적화만 구현한다.

첫 구현체인 `AttributedTypeRegistrySourceGenerator`는 `AttributedTypeRegistry<TAttribute>`를 지원한다. 등록 시에는 리플렉션을 사용하는 `Register(Type)` 대신 생성된 어트리뷰트 인스턴스를 `DirectRegisterRange`로 전달한다.

이 설계는 런타임 코드 변경안을 설명하지만, 이 문서 자체의 산출 범위는 설계뿐이다.

## 2. 설계 원칙

1. 레지스트리는 실제 사용할 구체 타입으로 선언한다.
2. 사용자는 `[GenerateTypeRegistry]`가 붙은 partial 정적 프로퍼티만 작성한다.
3. 공통 생성기는 레지스트리 발견, 프로퍼티 구현, manifest, 생명주기 코드를 재사용한다.
4. 파생 생성기는 자신이 지원하는 레지스트리와 등록 후보만 이해한다.
5. 한 owner에 여러 레지스트리가 필요하면 프로퍼티를 여러 개 선언한다. 별도의 다중 등록 규칙은 만들지 않는다.
6. 생성기가 없는 환경에서도 기존 수동 `Register(Type)`/`Unregister(Type)` 경로는 유지한다.
7. 등록 순서는 현재 `OrderByTypes(targetType, priority)` 의미를 유지한다. 동일 대상 타입과 우선순위에 별도의 2차 정렬 키를 추가하지 않으며, 기존 등록 순서를 따른다.

## 3. 사용자 선언 계약

사용자는 다음처럼 실제 레지스트리 타입이 명시된 partial 정적 프로퍼티를 선언한다.

```csharp
#nullable enable

public abstract partial class CollectionHandlerBase
{
    [GenerateTypeRegistry]
    public static partial AttributedTypeRegistry<CustomCollectionHandlerAttribute> registry { get; }
}
```

공통 생성기는 같은 containing type에 명시적 backing field와 partial property 구현부를 생성한다.

```csharp
#nullable enable

public abstract partial class CollectionHandlerBase
{
    private static readonly AttributedTypeRegistry<CustomCollectionHandlerAttribute> __registry =
        new(typeof(CollectionHandlerBase));

    public static partial AttributedTypeRegistry<CustomCollectionHandlerAttribute> registry => __registry;
}
```

C# 13 partial property 규칙상 구현 선언은 자동 프로퍼티일 수 없으므로 expression-bodied getter 또는 명시적 `get` 접근자를 생성한다. 자세한 규칙은 [C# partial properties 명세](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-13.0/partial-properties)를 따른다.

### 3.1 필수 조건

생성 대상 프로퍼티는 다음 조건을 모두 만족해야 한다.

- `public static partial`이다.
- get-only이며 인덱서가 아니다.
- 프로퍼티 타입은 추상이 아닌 구체적인 `TypeRegistry` 파생 타입이다.
- 프로퍼티를 포함하는 모든 타입 선언은 `partial`이다.
- 프로퍼티와 containing type 전체가 외부 어셈블리에서 접근 가능하다.
- 생성기가 사용하는 초기화식에 필요한 레지스트리 생성자에 접근할 수 있다.
- 사용자가 같은 partial property의 구현 선언을 직접 제공하지 않았다.

외부 어셈블리의 생성 코드가 이 프로퍼티를 참조하므로 `internal` owner 또는 `internal` 프로퍼티는 허용하지 않는다. 중첩 타입이면 바깥쪽 containing type까지 모두 실질적으로 `public`이어야 한다.

### 3.2 여러 레지스트리

같은 owner에 여러 레지스트리를 선언할 수 있다.

```csharp
public abstract partial class CollectionHandlerBase
{
    [GenerateTypeRegistry]
    public static partial AttributedTypeRegistry<CustomCollectionHandlerAttribute> registry { get; }

    [GenerateTypeRegistry]
    public static partial AnotherTypeRegistry<CollectionHandlerBase> fallbackRegistry { get; }
}
```

각 프로퍼티는 독립적인 `RegistryDefinition`으로 취급한다. 후보가 어느 레지스트리에 등록되는지는 각 파생 생성기의 매칭 규칙으로 결정한다.

## 4. 소스 제너레이터 관리 특성

두 특성은 런타임 프로젝트 API가 아니다. 별도 bootstrap generator가 `RegisterPostInitializationOutput`으로 현재 컴파일에 `internal` 선언을 주입한다. 사용자는 generator가 활성화된 어셈블리에서 평소처럼 attribute를 사용할 수 있고, 생성된 manifest는 참조 어셈블리 metadata에 남는다.

### 4.1 `GenerateTypeRegistryAttribute`

```csharp
#nullable enable

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class GenerateTypeRegistryAttribute : Attribute
{
    public readonly Type? baseType;

    public GenerateTypeRegistryAttribute()
    {
        baseType = null;
    }

    public GenerateTypeRegistryAttribute(Type baseType)
    {
        this.baseType = baseType;
    }
}
```

사용자가 구현 생성을 요청한 partial property를 표시한다. 특성 자체는 특정 레지스트리 구현을 알지 못한다.

### 4.2 `TypeRegistryManifestAttribute`

```csharp
#nullable enable

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class TypeRegistryManifestAttribute : Attribute
{
    public TypeRegistryManifestAttribute(Type ownerType, string propertyName)
        : this(ownerType, propertyName, ownerType)
    {
    }

    public TypeRegistryManifestAttribute(Type ownerType, string propertyName, Type baseType)
    {
        this.ownerType = ownerType;
        this.propertyName = propertyName;
        this.baseType = baseType;
    }

    public Type ownerType { get; }
    public string propertyName { get; }
    public Type baseType { get; }
}
```

공통 생성기는 유효한 레지스트리 선언마다 assembly attribute 사용 코드를 생성한다. attribute 선언 자체는 bootstrap generator가 생성한다.

```csharp
[assembly: TypeRegistryManifest(
    typeof(CollectionHandlerBase),
    nameof(CollectionHandlerBase.registry),
    typeof(CollectionHandlerBase))]
```

참조 어셈블리의 생성기는 `IAssemblySymbol.GetAttributes()`를 통해 manifest만 읽고 owner 타입의 프로퍼티 심볼을 복원한다. 따라서 참조 어셈블리의 전체 namespace/type 트리를 재귀 탐색할 필요가 없다.

manifest에는 owner, 프로퍼티 이름, base type을 기록한다. 실제 프로퍼티 타입과 접근성은 복원한 `IPropertySymbol`에서 다시 검증하여 오래되었거나 잘못 작성된 manifest를 안전하게 거부한다.

## 5. 생성기 프로젝트 구성

생성기 구현은 런타임 패키지와 분리된 analyzer 어셈블리에 둔다.

```text
RuniOS.Analyzers
└── SourceGenerators
    └── TypeRegistry
        ├── TypeRegistryAttributeSourceGenerator.cs
        ├── TypeRegistrySourceGenerator.cs
        ├── TypeRegistryModels.cs
        ├── TypeRegistryDiagnostics.cs
        ├── TypeRegistryEmitter.cs
        └── AttributedTypeRegistrySourceGenerator.cs
```

`DirectRegisterRange`만 런타임 어셈블리에 둔다. 두 marker attribute와 bootstrap generator는 analyzer 어셈블리에서 관리한다. 공통 생성기는 런타임 어셈블리를 직접 참조하지 않고 metadata name과 Roslyn symbol로 계약을 판별한다. 이 방식은 analyzer 로딩 컨텍스트에 런타임 의존성을 추가하지 않는다.

Unity에서 source generator로 로드되는 analyzer DLL의 구성과 Roslyn 버전은 [Unity source generator 문서](https://docs.unity3d.com/kr/current/Manual/create-source-generator.html)에 맞춘다.

## 6. 공통 생성기 구조

`TypeRegistrySourceGenerator`는 `IIncrementalGenerator`를 구현하는 template-method 기반 추상 클래스다. `[Generator]`는 concrete generator에만 붙인다.

```csharp
internal abstract class TypeRegistrySourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 공통 incremental pipeline 구성
    }

    protected abstract bool IsSupportedRegistryType(
        INamedTypeSymbol registryType,
        Compilation compilation);

    protected abstract IncrementalValuesProvider<RegistrationCandidate>
        CreateCandidateProvider(IncrementalGeneratorInitializationContext context);

    protected abstract bool TryBindCandidate(
        RegistryDefinition registry,
        RegistrationCandidate candidate,
        Compilation compilation,
        out BoundRegistration registration,
        out Diagnostic? diagnostic);

    protected virtual RegistryInitializer CreateRegistryInitializer(
        RegistryDefinition registry);

    protected virtual void EmitRegisterStatements(
        SourceWriter writer,
        RegistryDefinition registry,
        ImmutableArray<BoundRegistration> registrations);

    protected virtual void EmitUnregisterStatements(
        SourceWriter writer,
        RegistryDefinition registry,
        ImmutableArray<BoundRegistration> registrations);
}
```

위 시그니처는 계층의 역할 경계를 나타낸다. 구현 중에는 Roslyn 4.3에서 사용할 수 있는 타입과 API 범위 안에서 record/class 모델이나 writer 세부 형식을 조정할 수 있으나, 공통 파이프라인과 파생 hook의 책임은 바꾸지 않는다.

### 6.1 공통 모델

```csharp
internal sealed record RegistryDefinition(
    IPropertySymbol Property,
    INamedTypeSymbol OwnerType,
    INamedTypeSymbol RegistryType,
    RegistryOrigin Origin,
    string StableId);

internal abstract record RegistrationCandidate(ISymbol Symbol);

internal sealed record BoundRegistration(
    RegistryDefinition Registry,
    INamedTypeSymbol ImplementationType,
    object? Payload);
```

- `RegistryOrigin`은 현재 컴파일 선언과 참조 어셈블리 manifest를 구분한다.
- `StableId`는 owner metadata name, property metadata name, registry type을 기반으로 생성한다.
- `Payload`는 파생 생성기 전용 데이터다. `AttributedTypeRegistrySourceGenerator`는 생성할 어트리뷰트 표현식들을 담는다.
- 심볼 비교는 `SymbolEqualityComparer.Default`를 사용한다.

### 6.2 공통 incremental pipeline

공통 `Initialize`는 다음 pipeline을 구성한다.

1. `ForAttributeWithMetadataName`으로 현재 컴파일의 `[GenerateTypeRegistry]` 프로퍼티를 수집한다.
2. 선언 계약을 검증하고 유효한 프로퍼티를 `RegistryDefinition`으로 변환한다.
3. `CompilationProvider`에서 현재 컴파일 및 직접/간접 참조 어셈블리의 `TypeRegistryManifestAttribute`를 읽는다.
4. manifest가 가리키는 owner/property를 복원하고 다시 검증한다.
5. 현재 선언과 manifest 결과를 property symbol 기준으로 중복 제거한다.
6. concrete generator의 `IsSupportedRegistryType`으로 자신이 처리할 레지스트리만 남긴다.
7. concrete generator가 제공한 후보 provider와 레지스트리 집합을 결합한다.
8. `TryBindCandidate`로 후보를 각 레지스트리에 매칭하고 `BoundRegistration`을 만든다.
9. 현재 컴파일에서 선언된 레지스트리에 대해서만 backing field, partial property 구현, manifest를 생성한다.
10. 현재 컴파일에서 발견된 후보 등록분을 레지스트리별로 묶어 assembly load/unload 코드를 생성한다.

후보 구문 탐색에는 가능한 한 [ForAttributeWithMetadataName](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxvalueprovider.forattributewithmetadataname)을 사용한다. 생성 단계에 불필요한 `Compilation`, `SemanticModel`, syntax node를 오래 보관하지 않고, equatable한 소형 모델로 투영하여 incremental cache 재사용성을 유지한다.

### 6.3 현재 선언과 manifest의 소유권

프로퍼티 구현과 manifest는 프로퍼티를 선언한 어셈블리에서 한 번만 생성한다. 반면 등록 코드는 후보 구현 타입을 포함하는 어셈블리에서 생성한다.

예를 들어 `Registry.Contracts`에 레지스트리 프로퍼티가 있고 `Registry.Implementations`에 구현 타입이 있으면 다음처럼 나뉜다.

```text
Registry.Contracts
  ├─ backing field + partial property 구현
  └─ TypeRegistryManifestAttribute

Registry.Implementations
  ├─ manifest를 통해 Registry.Contracts.registry 발견
  └─ OnAssemblyLoaded/OnAssemblyUnloading 등록 코드
```

이 구조에서는 각 구현 어셈블리가 자신이 가진 등록분만 관리한다. 참조 어셈블리의 후보를 다시 등록하지 않는다.

## 7. 공통 코드 생성

### 7.1 partial property 구현

생성 코드는 원본 namespace와 containing type 계층을 그대로 재현한다. 타입 매개변수와 constraint도 원본 심볼에서 출력한다.

backing field 이름은 `__typeRegistry_<propertyName>_<shortHash>` 형식으로 만든다. hash는 owner metadata name과 property metadata name으로부터 생성하여 같은 partial 타입의 사용자 멤버 및 다른 생성 프로퍼티와 충돌할 가능성을 낮춘다. 생성 전에 실제 멤버 충돌도 검사한다.

레지스트리 초기화식의 기본값은 다음과 같다.

```csharp
new global::Namespace.ConcreteRegistry<...>()
```

특수 생성 방법이 필요한 파생 generator는 `CreateRegistryInitializer`를 override한다.

### 7.2 manifest

현재 컴파일에서 공통 검증과 concrete generator 지원 판별을 모두 통과한 프로퍼티만 manifest에 기록한다. unsupported registry 타입은 manifest를 만들지 않고 진단한다.

### 7.3 registration class

후보 등록 코드가 있는 각 concrete generator마다 현재 어셈블리에 내부 정적 클래스를 하나 생성한다.

```csharp
#nullable enable

namespace RuniOS.Generated
{
    internal static class __AttributedTypeRegistryRegistration_<hash>
    {
        [global::RuniOS.OnAssemblyLoaded]
        private static void RegisterGeneratedTypes()
        {
            // concrete generator가 생성
        }

        [global::RuniOS.OnAssemblyUnloading]
        private static void UnregisterGeneratedTypes()
        {
            // concrete generator가 생성
        }
    }
}
```

현재 lifecycle attribute 계약에 맞춰 특성은 메서드에만 부착한다.

공통 기본 등록/해제 본문은 다음과 같다.

```csharp
global::RegistryOwner.registry.Register(typeof(global::Implementation));
global::RegistryOwner.registry.Unregister(typeof(global::Implementation));
```

하나의 구현 타입이 같은 레지스트리에 여러 등록 payload를 만들더라도 기본 해제는 구현 타입당 한 번만 출력한다. 파생 generator가 다른 대칭 규칙을 필요로 하면 등록과 해제 hook을 함께 override해야 한다.

### 7.4 생명주기 순서

- 등록은 Unity가 제공하는 의존 어셈블리 로드 순서를 그대로 사용한다.
- 해제는 Unity가 제공하는 역순 어셈블리 언로드를 그대로 사용한다.
- 생성기에서 전체 어셈블리 순서를 별도로 계산하거나 정렬하지 않는다.
- 같은 생성 메서드 안에서는 Roslyn이 수집한 기존 후보 순서를 유지한다. 의미상 필요한 정렬은 레지스트리 런타임의 기존 정렬 정책에 맡긴다.

### 7.5 hint name

hint name에는 concrete generator 이름, owner metadata name, property 이름의 안정적인 hash를 포함한다.

```text
RuniOS.TypeRegistry.Property.<hash>.g.cs
RuniOS.TypeRegistry.Manifest.<hash>.g.cs
RuniOS.AttributedTypeRegistry.Registration.<hash>.g.cs
```

파일 시스템에 부적합한 원본 이름을 직접 연결하지 않는다. hash 충돌 검출을 위해 한 generator 실행 안에서 hint name과 원본 stable ID를 함께 추적하고, 충돌하면 공통 진단을 보고한다.

## 8. `AttributedTypeRegistrySourceGenerator`

`AttributedTypeRegistrySourceGenerator`는 공통 기반을 상속하고 다음 책임만 가진다.

### 8.1 지원 레지스트리 판별

프로퍼티 타입의 `OriginalDefinition`이 다음 metadata type과 같은지 확인한다.

```text
RuniOS.Reflection.AttributedTypeRegistry`1
```

그리고 다음을 검증한다.

- `TAttribute`가 `TypeRegistrationAttribute`와 같거나 그 파생 타입이다.
- registry definition에 저장된 `baseType`가 유효한 타입이다.
- 레지스트리 타입 자체가 `TypeRegistry`를 상속한다.

### 8.2 후보 수집

등록 어트리뷰트가 붙을 수 있는 class 선언을 syntax 단계에서 후보로 잡고 semantic 단계에서 다음을 확인한다.

- 후보는 `INamedTypeSymbol`인 class다.
- 추상 타입은 등록하지 않는다.
- 후보에 적용된 attribute class가 `TypeRegistrationAttribute`와 같거나 파생 타입이다.
- 상속된 어트리뷰트의 반영 여부는 런타임 `GetCustomAttributes<TAttribute>()`와 동일한 계약을 따른다. 즉, attribute의 `AttributeUsageAttribute.Inherited` 의미를 보존한다.

특정 파생 어트리뷰트 metadata name 하나만으로 `ForAttributeWithMetadataName`을 구성할 수 없으므로, class에 attribute list가 있는 구문을 좁게 수집한 뒤 `AttributeData.AttributeClass`의 상속 계층을 검사한다. 후보 타입 단위로 중복 제거하되, 같은 타입에 적용된 복수 어트리뷰트 인스턴스는 모두 보존한다.

### 8.3 레지스트리 매칭

후보 구현 타입과 각 `AttributedTypeRegistry<TAttribute>`에 대해 다음을 모두 만족하면 매칭한다.

1. 구현 타입이 registry definition의 `baseType`에 할당 가능하다.
2. 적용된 attribute type이 `TAttribute`와 같거나 그 파생 타입이다.
3. 구현 타입과 attribute 생성에 필요한 모든 심볼이 등록 코드를 생성하는 어셈블리에서 접근 가능하다.

할당 가능성은 base type/interface 계층과 constructed generic 관계를 symbol 기반으로 판별한다. 단순한 이름 비교나 문자열 비교를 사용하지 않는다.

하나의 구현 타입에 같은 `TAttribute` 계열 어트리뷰트가 여러 개 있으면 모두 하나의 레지스트리 등록 batch에 포함한다.

### 8.4 어트리뷰트 인스턴스 생성

`AttributeData.ConstructorArguments`와 `NamedArguments`를 C# 식으로 변환한다.

지원 범위는 다음과 같다.

- `typeof(ConcreteType)`
- `typeof(OpenGeneric<>)` 및 복수 arity open generic
- enum 값
- 1차원 attribute 배열
- 문자열과 문자
- `bool`
- attribute 인자로 허용되는 정수 및 부동소수점 숫자
- `null`
- named field/property initializer

예시:

```csharp
new global::CustomCollectionHandlerAttribute(typeof(global::MyTarget<>))
{
    priority = 10,
    useForChildren = true,
    modes = new global::Mode[]
    {
        global::Mode.Read,
        global::Mode.Write,
    },
}
```

문자열과 문자는 C# literal escaping을 적용한다. 숫자는 invariant culture와 타입 suffix를 사용하여 원래 `TypedConstant.Type`을 보존한다. enum은 가능하면 `global::EnumType.Member`를 사용하고 이름 없는 조합 값은 명시적 underlying value cast로 출력한다. 배열의 `IsNull`과 빈 배열을 구분한다.

다음 경우에는 추측해 코드를 만들지 않고 진단한다.

- `TypedConstantKind.Error`
- 생성자 또는 named member가 접근 불가능함
- 컴파일 시 C# 식으로 안전하게 재현할 수 없는 값
- 오류 타입 또는 해석되지 않은 타입 인자

### 8.5 배치 등록

기본 `Register(Type)` 생성 대신 레지스트리별로 `DirectRegisterRange` 호출을 출력한다.

개념적인 생성 결과는 다음과 같다.

```csharp
global::CollectionHandlerBase.registry.DirectRegisterRange(
    new global::RuniOS.Reflection.RegistrationEntry<
        global::CustomCollectionHandlerAttribute>[]
    {
        new(typeof(global::CsvCollectionHandler),
            new global::CustomCollectionHandlerAttribute(typeof(global::CsvCollection))
            {
                priority = 10,
            }),
        new(typeof(global::CsvCollectionHandler),
            new global::CustomCollectionHandlerAttribute(typeof(global::TabularCollection))
            {
                useForChildren = true,
            }),
    });
```

정확한 런타임 시그니처는 다음 계약을 만족해야 한다.

```csharp
public void DirectRegisterRange(ReadOnlySpan<RegistrationEntry<TAttribute>> entries)
```

Unity/Roslyn 대상 프레임워크에서 collection expression이나 span 호출 제약이 있으면 배열 또는 `ImmutableArray` 기반 overload를 제공해도 된다. 생성기 전용 API이므로 입력 검증은 수행하지 않는다. 핵심 계약은 한 batch가 다음을 한 번씩만 수행하는 것이다.

1. lock 획득
2. 모든 entry 추가
3. snapshot/cache 무효화
4. lock 해제
5. `onChanged` 호출

호출자는 generator가 생성한 유효한 entry만 전달해야 한다. `onChanged`는 lock 밖에서 한 번 호출한다.

해제는 구현 타입별로 한 번씩 기존 API를 사용한다.

```csharp
global::CollectionHandlerBase.registry.Unregister(typeof(global::CsvCollectionHandler));
```

### 8.6 정렬 정책

`registrationEntries`의 정렬은 기존 정책만 유지한다.

```csharp
.OrderByTypes(
    x => x.attribute.targetType,
    x => x.attribute.priority)
```

동일한 대상 타입과 우선순위에는 assembly-qualified name이나 구현 타입 이름을 이용한 2차 키를 추가하지 않는다. LINQ의 안정 정렬과 기존 등록 순서에 따라 상대 순서를 유지한다.

## 9. 진단 설계

진단 ID는 공통 생성기와 concrete generator를 구분한다. ID와 메시지는 구현 시 analyzer의 기존 명명 규칙에 맞춰 최종 확정하되 다음 범위를 제공한다.

| ID | 심각도 | 조건 |
|---|---|---|
| `RUTRSG001` | Error | `[GenerateTypeRegistry]` 대상이 property가 아님 |
| `RUTRSG002` | Error | property가 `public static partial` get-only 계약을 만족하지 않음 |
| `RUTRSG003` | Error | containing type 계층이 partial 또는 실질적 public이 아님 |
| `RUTRSG004` | Error | property 타입이 구체적인 `TypeRegistry` 파생 타입이 아님 |
| `RUTRSG005` | Error | 어떤 concrete generator도 registry type을 지원하지 않음 |
| `RUTRSG006` | Error | 기본 initializer에 필요한 접근 가능한 매개변수 없는 생성자가 없음 |
| `RUTRSG007` | Error | 생성할 backing field/property 구현과 기존 멤버가 충돌함 |
| `RUTRSG008` | Error | `OnAssemblyLoaded` 또는 `OnAssemblyUnloading` API를 찾을 수 없음 |
| `RUTRSG009` | Warning | manifest가 가리키는 owner/property를 복원하거나 검증할 수 없음 |
| `RUTRSG010` | Error | 생성 hint name hash 충돌 |
| `RUTRSG011` | Error | generic containing type의 registry에는 자동 lifecycle 등록을 생성할 수 없음 |
| `RUATRSG001` | Error | `TAttribute`가 `TypeRegistrationAttribute` 계열이 아님 |
| `RUATRSG002` | Error | 어트리뷰트 인자를 C# 식으로 재현할 수 없음 |
| `RUATRSG003` | Error | 구현 타입 또는 attribute 생성자가 생성 위치에서 접근 불가능함 |
| `RUATRSG004` | Warning | 후보가 추상 타입이어서 등록에서 제외됨 |

오류가 난 레지스트리에는 불완전한 property 구현이나 registration 코드를 생성하지 않는다. 다른 유효한 레지스트리의 출력은 계속 생성한다. 진단 위치는 가능하면 사용자 property 또는 attribute 적용 구문을 가리킨다. 참조 manifest 문제처럼 현재 syntax 위치가 없으면 `Location.None`을 사용하고 assembly/type/property 이름을 메시지에 포함한다.

## 10. 생성 결과 예시

### 10.1 계약 어셈블리

입력:

```csharp
public abstract partial class CollectionHandlerBase
{
    [GenerateTypeRegistry]
    public static partial AttributedTypeRegistry<CustomCollectionHandlerAttribute> registry { get; }
}
```

생성 개요:

```csharp
public abstract partial class CollectionHandlerBase
{
    private static readonly AttributedTypeRegistry<CustomCollectionHandlerAttribute> __typeRegistry_registry_A1B2C3D4 =
        new(typeof(CollectionHandlerBase));

    public static partial AttributedTypeRegistry<CustomCollectionHandlerAttribute> registry
        => __typeRegistry_registry_A1B2C3D4;
}

[assembly: TypeRegistryManifest(
    typeof(CollectionHandlerBase),
    nameof(CollectionHandlerBase.registry),
    typeof(CollectionHandlerBase))]
```

### 10.2 구현 어셈블리

입력:

```csharp
[CustomCollectionHandler(typeof(CsvCollection), priority = 10)]
[CustomCollectionHandler(typeof(TabularCollection), useForChildren = true)]
public sealed class CsvCollectionHandler : CollectionHandlerBase
{
}
```

생성 개요:

```csharp
internal static class __AttributedTypeRegistryRegistration_A1B2C3D4
{
    [OnAssemblyLoaded]
    private static void RegisterGeneratedTypes()
    {
        CollectionHandlerBase.registry.DirectRegisterRange(
            new global::RuniOS.Reflection.RegistrationEntry<
                CustomCollectionHandlerAttribute>[]
            {
                new(
                    typeof(CsvCollectionHandler),
                    new CustomCollectionHandlerAttribute(typeof(CsvCollection))
                    {
                        priority = 10,
                    }),
                new(
                    typeof(CsvCollectionHandler),
                    new CustomCollectionHandlerAttribute(typeof(TabularCollection))
                    {
                        useForChildren = true,
                    }),
            });
    }

    [OnAssemblyUnloading]
    private static void UnregisterGeneratedTypes()
    {
        CollectionHandlerBase.registry.Unregister(typeof(CsvCollectionHandler));
    }
}
```

실제 생성 코드는 namespace 모호성과 using 상태에 영향받지 않도록 사용자 타입 및 런타임 타입을 `global::` 정규화 이름으로 출력한다.

## 11. 수동 등록 호환성

소스 제너레이터를 배포하지 않는 외부 어셈블리도 기존 API를 사용할 수 있다.

```csharp
[OnAssemblyLoaded]
private static void RegisterTypes()
{
    CollectionHandlerBase.registry.Register(typeof(ExternalCollectionHandler));
}

[OnAssemblyUnloading]
private static void UnregisterTypes()
{
    CollectionHandlerBase.registry.Unregister(typeof(ExternalCollectionHandler));
}
```

`Register(Type)`은 런타임 리플렉션 경로로 남긴다. `DirectRegisterRange`는 source generator가 이미 해석한 attribute 인스턴스를 효율적으로 넣는 최적화 경로이지, 수동 등록 API를 대체하지 않는다.

같은 구현 타입을 수동 경로와 생성 경로에서 동시에 등록하는 것은 호출자 오류로 본다. 필요하면 런타임 registry가 기존 중복 등록 정책에 따라 방어하되, 생성기가 전역적으로 수동 호출을 탐지하려 하지 않는다.

## 12. 테스트 설계

생성기 테스트는 `CSharpCompilation`과 `GeneratorDriver`로 생성 소스, 진단, 후속 컴파일 결과를 검사한다. Unity 프로젝트의 생성 `.csproj`를 빌드하는 방식은 사용하지 않는다.

### 12.1 공통 생성기 테스트

테스트용 `FakeTypeRegistry : TypeRegistry`와 `FakeTypeRegistrySourceGenerator : TypeRegistrySourceGenerator`를 만든다.

- 유효한 partial property에서 backing field와 구현 접근자가 생성된다.
- 기본 initializer가 `new FakeTypeRegistry()`를 생성한다.
- 후보마다 `Register(typeof(...))`가 생성된다.
- unload 본문에 동일 구현 타입의 `Unregister(typeof(...))`가 생성된다.
- 등록과 해제의 구현 타입 집합이 대칭이다.
- 같은 owner의 여러 registry property가 각각 독립적으로 생성된다.
- nested/generic partial containing type의 type parameter와 constraint가 보존된다.
- 생성 멤버 이름이 사용자 멤버와 충돌하면 진단하고 해당 출력만 생략한다.
- 후보가 없는 어셈블리에는 빈 lifecycle class를 만들지 않는다.

### 12.2 manifest 통합 테스트

두 단계 컴파일을 사용한다.

1. 계약 어셈블리를 생성하고 generator 출력이 포함된 metadata reference를 만든다.
2. 구현 어셈블리가 그 reference를 참조하도록 컴파일한다.
3. 구현 어셈블리 generator가 전체 타입 재귀 탐색 없이 manifest를 읽어 registry property를 찾는지 검증한다.
4. 구현 어셈블리에는 property/backing field를 중복 생성하지 않고 registration code만 생성하는지 확인한다.
5. 잘못된 owner 또는 property를 가리키는 manifest는 경고하고 무시하는지 확인한다.

### 12.3 `AttributedTypeRegistry` 테스트

- 하나의 구현 타입에 복수 등록 어트리뷰트가 있으면 entry가 모두 생성된다.
- constructor argument와 named argument가 함께 보존된다.
- `typeof(ConcreteType)`과 `typeof(OpenGeneric<>)`가 생성된다.
- enum의 이름 있는 값과 flags 조합 값이 생성된다.
- 빈 배열, 값이 있는 배열, null 배열을 구분한다.
- 문자열 escape, 문자, 숫자 suffix, `null`을 올바르게 생성한다.
- `baseType`에 할당할 수 없는 후보는 해당 registry와 매칭되지 않는다.
- `TAttribute`와 무관한 attribute는 포함하지 않는다.
- 파생 attribute가 `TAttribute` 계약에 맞으면 구체 파생 attribute 생성식을 유지한다.
- 등록은 레지스트리당 `DirectRegisterRange` 한 번으로 묶인다.
- 해제는 구현 타입당 `Unregister(Type)` 한 번만 생성된다.
- 동일 대상 타입과 우선순위에 assembly-qualified name 2차 정렬이 생성되지 않는다.

### 12.4 진단 테스트

다음 입력마다 예상 진단 ID와 source location을 검증한다.

- non-partial property
- non-static 또는 non-public property
- setter가 있는 property
- non-partial containing type
- 외부에서 접근 불가능한 containing type
- `TypeRegistry` 비파생 property 타입
- 추상 registry 타입
- 지원하는 concrete generator가 없는 registry 타입
- 기본 생성자로 초기화할 수 없는 registry 타입
- lifecycle attribute API가 없는 compilation
- 재현할 수 없는 attribute typed constant

### 12.5 생명주기 및 런타임 테스트

- assembly load를 모사해 생성 등록 메서드를 호출하면 모든 entry가 등록된다.
- unload 메서드를 호출하면 같은 구현 타입이 모두 제거된다.
- 여러 어트리뷰트를 가진 구현 타입도 unload 호출은 한 번이다.
- `DirectRegisterRange`가 batch당 lock, snapshot/cache 무효화, `onChanged`를 각각 한 번 수행한다.
- generator가 생성한 entry가 reflection 경로와 같은 registration entry를 만든다.
- 서로 다른 구현 어셈블리의 load/unload 순서가 런타임 lifecycle 순서를 그대로 따른다.
- 동일 대상·우선순위 entry가 기존 등록 순서를 유지한다.

### 12.6 외부 수동 모드 테스트

- generator를 적용하지 않은 외부 어셈블리에서 `registry.Register(typeof(T))`가 정상 동작한다.
- 대응하는 `registry.Unregister(typeof(T))`가 정상 동작한다.
- 수동 경로의 attribute reflection 결과와 생성 경로의 직접 등록 결과가 기능적으로 동일하다.

## 13. 구현 순서

1. bootstrap generator가 `GenerateTypeRegistryAttribute`와 `TypeRegistryManifestAttribute`를 post-initialization source로 주입한다.
2. `AttributedTypeRegistry`에 generator 전용 `DirectRegisterRange`를 추가한다.
3. analyzer에 공통 모델, 진단, symbol helper, literal emitter를 추가한다.
4. `TypeRegistrySourceGenerator` 공통 incremental pipeline을 구현한다.
5. fake registry generator 테스트로 공통 기본 등록/해제 출력을 먼저 고정한다.
6. `AttributedTypeRegistrySourceGenerator`의 후보 탐색과 매칭을 구현한다.
7. attribute literal 및 batch 등록 테스트를 추가한다.
8. 두 컴파일을 사용하는 manifest 통합 테스트를 추가한다.
9. Unity에서 analyzer DLL 배치 후 실제 generated source와 assembly load/unload 동작을 확인한다.

## 14. 완료 조건

다음을 모두 만족하면 구현이 완료된 것으로 본다.

- 사용자는 유효한 partial static property 한 개만 선언해 레지스트리를 소유할 수 있다.
- 공통 생성기가 backing field, property 구현, manifest, lifecycle 골격을 생성한다.
- 새 레지스트리 지원은 `TypeRegistrySourceGenerator` 파생 generator 추가만으로 가능하다.
- 기본 파생 generator는 `Register(Type)`/`Unregister(Type)` 코드를 재사용할 수 있다.
- `AttributedTypeRegistrySourceGenerator`는 `DirectRegisterRange`로 attribute reflection을 제거한다.
- 참조 어셈블리의 레지스트리는 manifest로 발견되며 전체 타입 재귀 탐색이 없다.
- 한 owner의 여러 registry property가 독립적으로 동작한다.
- 등록과 해제가 구현 타입 집합 기준으로 대칭이다.
- 생성기가 없는 외부 어셈블리의 수동 등록 경로가 유지된다.
- 기존 `OrderByTypes(targetType, priority)` 외의 결정적 2차 정렬을 추가하지 않는다.

## 참고 자료

- [C# 13 partial properties 명세](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-13.0/partial-properties)
- [IIncrementalGenerator API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.iincrementalgenerator)
- [ForAttributeWithMetadataName API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxvalueprovider.forattributewithmetadataname)
- [Unity source generator 문서](https://docs.unity3d.com/kr/current/Manual/create-source-generator.html)
