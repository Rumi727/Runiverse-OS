# RuniOS.CodeAnalysis

RuniOS.CodeAnalysis는 RuniOS의 TypeRegistry 등록 코드를 생성하고 Unity 관련 진단을 조정하는 Roslyn 기반 source generator 및 analyzer 프로젝트입니다.

> 주의: 이 프로젝트는 100% AI가 작성했으며, 정상적인 빌드·테스트 및 Unity 통합 검증을 거치지 않았습니다.

이 문서는 RuniOS.CodeAnalysis의 현재 소스 기준으로, Roslyn을 처음 접하는 사람도 RuniOS.CodeAnalysis가 어떤 입력을 받아 어떤 심볼을 만들고 어떤 C# 코드를 생성하는지 따라갈 수 있도록 정리한 문서입니다.

기준일: 2026-08-28

문서에서 말하는 “현재 코드”는 이 저장소의 현재 워킹 트리와 연결된 Runiverse OS 런타임 소스를 뜻합니다. 설계 문서의 의도와 실제 구현이 다른 부분은 별도로 표시합니다.

## 0. 먼저 결론

RuniOS.CodeAnalysis는 런타임 라이브러리가 아닙니다. Unity가 C#을 컴파일할 때 Roslyn이 로드하는 analyzer 어셈블리이며, 그 안에 세 가지 종류의 컴파일 확장이 들어 있습니다.

1. IIncrementalGenerator 기반 source generator
   - GenerateTypeRegistry가 붙은 레지스트리 property를 찾습니다.
   - TypeRegistrationAttribute 계열 특성을 가진 구현 클래스를 찾습니다.
   - 레지스트리 property 구현, assembly manifest, assembly load/unload 시 등록 코드를 C# 소스로 추가합니다.

2. DiagnosticAnalyzer 기반 analyzer
   - TypeRegistrationAttribute 계열 특성이 기본 타입 계층의 generated registry에 연결되는지 검사합니다.
   - 연결할 registry가 없으면 ROS0019 warning을 보고합니다.
   - generic target 등록이 AttributedTypeRegistry.TryResolve에서 매칭·구성될 수 없거나 target과 implementation의 제약 조건이 일치하지 않는 경우 ROS0020~ROS0022 warning을 보고하고, non-generic 구현에는 ROS0023 suggestion을 보고합니다.

3. DiagnosticSuppressor 기반 analyzer suppressor
   - Unity analyzer가 AssetRef<TAsset> 필드에 보고하는 UAC1001을 의도적으로 억제합니다.

핵심 흐름은 다음과 같습니다.

~~~text
사용자 C# 소스
    |
    |  Roslyn parse
    v
SyntaxTree + SyntaxNode
    |
    |  semantic binding
    v
Compilation + Symbol + SemanticModel
    |
    |  RuniOS.CodeAnalysis generator pipeline
    v
RegistryDefinition + RegistrationCandidate
    |
    |  candidate binding
    v
BoundRegistration
    |
    |  source emission
    v
추가된 .g.cs 소스
    |
    |  일반 C# 컴파일에 다시 포함
    v
최종 어셈블리
    |
    |  Unity lifecycle callback
    v
AttributedTypeRegistry에 구현 타입 등록
~~~

가장 중요한 경계는 이것입니다.

- generator는 실행 파일을 직접 고치는 것이 아니라 현재 컴파일에 소스 트리를 추가합니다.
- generated source는 원본 .cs 파일로 저장되지 않아도 최종 어셈블리의 일부가 됩니다.
- generator가 만드는 typeof(SomeImplementation)와 new SomeAttribute(...)는 런타임 리플렉션 전체 검색을 대체합니다.
- 다만 이 저장소의 현재 코드는 후보 타입을 “현재 컴파일”에서만 찾고, 참조 어셈블리에서는 generator가 만든 manifest만 읽습니다.

---

## 1. Roslyn부터 이해하기

### 1.1 C# 컴파일러가 하는 일

C# 컴파일러는 대략 다음 순서로 움직입니다.

~~~text
.cs 파일들
  -> 구문 분석(parse)
  -> SyntaxTree 생성
  -> 심볼 바인딩(binding)
  -> Compilation / SemanticModel 생성
  -> analyzer 실행
  -> source generator 실행
  -> generator가 추가한 소스까지 포함해 진단
  -> IL/metadata가 들어 있는 어셈블리 출력
~~~

실제 Roslyn 내부 실행 순서는 세부적으로 더 복잡하지만, 이 프로젝트를 읽을 때는 위 모델로 충분합니다.

### 1.2 SyntaxTree와 SyntaxNode

SyntaxTree는 소스 파일 하나를 C# 문법 구조로 표현한 트리입니다.

예를 들어 다음 코드가 있다고 합시다.

~~~csharp
[GenerateTypeRegistry]
public static partial MyRegistry registry { get; }
~~~

트리 안에는 대략 다음 노드가 있습니다.

~~~text
AttributeListSyntax
  AttributeSyntax("GenerateTypeRegistry")
PropertyDeclarationSyntax
  AttributeListSyntax
  PublicKeyword
  StaticKeyword
  PartialKeyword
  TypeSyntax("MyRegistry")
  Identifier("registry")
  AccessorListSyntax
~~~

SyntaxNode는 “소스가 문법상 어떤 모양인가?”를 알려줍니다. 하지만 MyRegistry가 어떤 타입인지, registry가 실제로 어떤 property symbol인지, GenerateTypeRegistry가 어느 attribute 타입인지까지는 구문 노드만으로 확정할 수 없습니다.

그래서 generator는 syntax와 semantic 정보를 함께 사용합니다.

### 1.3 Symbol

ISymbol 계열은 컴파일러가 바인딩한 선언의 의미적 정체성입니다.

주요 symbol 타입은 다음과 같습니다.

| Roslyn 타입 | 뜻 | 이 프로젝트에서 쓰이는 예 |
|---|---|---|
| IAssemblySymbol | 어셈블리 | 참조 어셈블리의 assembly-level manifest 읽기 |
| INamedTypeSymbol | class, struct, interface, enum 등 명명된 타입 | AttributedTypeRegistry<,>, 후보 구현 클래스 |
| IPropertySymbol | property 선언 | GenerateTypeRegistry 대상 |
| IFieldSymbol | field 선언 | attribute named argument 검증 |
| IMethodSymbol | method 또는 constructor | parameterless constructor, attribute constructor |
| ITypeParameterSymbol | generic type parameter | generated partial type constraint 복원 |
| AttributeData | 적용된 attribute와 인자 값 | 등록 attribute를 generated new Attribute(...)로 재생성 |

이 프로젝트는 문자열로 타입 이름을 비교하는 대신 다음처럼 symbol equality를 사용합니다.

~~~csharp
SymbolEqualityComparer.Default.Equals(left, right)
~~~

이 점이 중요합니다. List<int>라는 문자열과 List<int>라는 문자열이 같다는 판단이 아니라, Roslyn compilation 안에서 두 symbol이 같은 선언을 가리키는지 판단합니다.

### 1.4 Compilation

Compilation은 현재 컴파일 전체를 나타냅니다.

여기에는 다음 정보가 포함됩니다.

- 현재 소스 파일의 syntax tree
- 현재 어셈블리의 declared symbol
- 참조된 어셈블리 symbol
- GetTypeByMetadataName(...)으로 찾을 수 있는 타입
- SourceProductionContext가 진단과 generated source를 연결할 기반

이 프로젝트는 런타임 RuniOS DLL을 직접 참조하지 않습니다. 대신 다음과 같은 metadata name 문자열로 계약을 찾습니다.

~~~csharp
"RuniOS.Reflection.TypeRegistry"
"RuniOS.Reflection.AttributedTypeRegistry`2"
"RuniOS.Reflection.TypeRegistrationAttribute"
~~~

여기서 metadata 이름 끝의 backtick-2 표기는 CLR metadata에서 generic parameter가 2개라는 뜻입니다.

~~~text
AttributedTypeRegistry<TBase, TAttribute>
                        \____ arity 2
~~~

이 방식의 효과는 다음과 같습니다.

- analyzer 어셈블리가 Unity 런타임 어셈블리를 실행 시점 의존성으로 끌고 오지 않습니다.
- 현재 compilation에 실제로 런타임 타입이 있는지 Roslyn symbol로 확인합니다.
- 같은 generator DLL을 여러 런타임 어셈블리에 적용할 수 있습니다.

### 1.5 SemanticModel

SemanticModel은 특정 syntax tree의 노드를 symbol과 연결해 줍니다.

예를 들어 suppressor는 다음과 같이 field 선언 syntax에서 field symbol을 얻습니다.

~~~csharp
semanticModel.GetDeclaredSymbol(variable, cancellationToken)
~~~

이 결과가 IFieldSymbol이므로 실제 field 타입을 검사할 수 있습니다.

~~~csharp
if (field.Type is not INamedTypeSymbol fieldType)
    continue;
~~~

### 1.6 Diagnostic

Diagnostic은 compiler/analyzer가 사용자에게 보고하는 문제 또는 경고입니다.

DiagnosticDescriptor가 진단의 설명서라면 Diagnostic은 특정 소스 위치와 인자를 채운 실제 보고 항목입니다.

~~~csharp
public static readonly DiagnosticDescriptor invalidRegistryType = new
(
    "ROS0005",
    "Invalid registry type",
    "Registry property '{0}' must use a concrete TypeRegistry-derived type",
    category,
    DiagnosticSeverity.Error,
    isEnabledByDefault: true
);
~~~

위 descriptor 자체는 템플릿입니다. 실제 사용은 다음과 같습니다.

~~~csharp
diagnostic = TypeRegistryDiagnostics.Create
(
    TypeRegistryDiagnostics.invalidRegistryType,
    location,
    property.Name
);
~~~

SourceProductionContext.ReportDiagnostic으로 보고된 진단은 일반 C# 오류/경고처럼 IDE와 Unity Console에 표시됩니다.

### 1.7 Analyzer와 source generator의 차이

#### Analyzer

analyzer는 컴파일 중 코드를 읽고 진단을 보고합니다.

~~~text
입력 코드 읽기
  -> 규칙 검사
  -> Diagnostic 보고
~~~

analyzer가 보통 하는 일:

- 특정 API 사용 금지
- nullable 또는 naming 규칙 검사
- 코드 구조에 대한 경고
- 특정 외부 analyzer 진단 억제

#### Source generator

source generator는 컴파일 중 코드를 읽고 추가 C# 소스를 제공합니다.

~~~text
입력 코드 읽기
  -> 필요한 선언/심볼 수집
  -> C# 텍스트 생성
  -> Compilation에 AddSource
~~~

source generator는 기존 .cs 파일의 내용을 수정하지 않습니다. 다음 두 문장은 다릅니다.

~~~text
원본 파일 편집: 하지 않음
컴파일 입력에 새 소스 추가: 함
~~~

이 프로젝트의 TypeRegistryEmitter.RenderPropertyImplementation(...)이 반환한 문자열은 파일 편집 결과가 아니라 Roslyn이 현재 compilation에 추가할 소스입니다.

#### Diagnostic suppressor

suppressor는 이미 다른 analyzer가 보고한 진단을 조건부로 숨깁니다.

~~~text
Unity analyzer가 UAC1001 보고
  -> AssetRefSerializationSuppressor가 진단 위치/field 타입 검사
  -> 조건이 맞으면 ROS0001 suppression 보고
  -> 사용자에게 UAC1001이 보이지 않음
~~~

suppressor는 오류를 고치거나 코드를 생성하지 않습니다. “이 특정 상황에서 기존 진단을 표시하지 않는다”는 역할만 합니다.

### 1.8 IIncrementalGenerator

이 프로젝트의 generator들은 전부 IIncrementalGenerator를 사용합니다.

전통적인 generator가 매번 전체 compilation을 다시 순회하는 방식이라면, incremental generator는 입력과 변환 단계를 그래프로 등록합니다.

~~~text
입력 provider
  -> 변환 provider
  -> 필터 provider
  -> 결합 provider
  -> output callback
~~~

Roslyn은 provider 단계의 결과를 캐시하고 입력이 바뀐 부분을 중심으로 다시 계산할 수 있습니다. 따라서 Initialize에서 실제 검사를 수행하지 않고 “어떤 검사를 어떤 순서로 연결할지”만 등록합니다.

이 프로젝트에서 실제 계산은 RegisterSourceOutput(input, Execute)에 등록한 Execute가 호출될 때 일어납니다.

### 1.9 Incremental provider 용어

#### IncrementalValuesProvider<T>

0개 이상의 값을 흐르게 하는 provider입니다.

~~~csharp
IncrementalValuesProvider<RegistrationCandidate> candidates
~~~

후보 클래스가 10개면 10개의 candidate 값이 흐릅니다.

#### IncrementalValueProvider<T>

하나의 값을 흐르게 하는 provider입니다.

~~~csharp
IncrementalValueProvider<...> input
~~~

CompilationProvider.Combine(...).Combine(...)의 결과처럼 하나의 tuple 안에 여러 컬렉션을 담아 넘길 때 사용됩니다.

#### Collect()

여러 값을 하나의 ImmutableArray<T>로 모읍니다.

~~~csharp
currentRegistries.Collect()
candidates.Collect()
~~~

Execute가 registry와 candidate 전체를 서로 비교해야 하므로 마지막에 배열로 수집합니다.

#### Combine()

두 provider의 결과를 tuple로 묶습니다.

~~~csharp
context.CompilationProvider
    .Combine(currentRegistries.Collect())
    .Combine(candidates.Collect());
~~~

결과 타입은 소스에 실제로 다음처럼 표현됩니다.

~~~csharp
((Compilation, ImmutableArray<RegistryDiscoveryItem>),
 ImmutableArray<RegistrationCandidate>)
~~~

그래서 Execute에서 다음처럼 꺼냅니다.

~~~csharp
Compilation compilation = input.Item1.Item1;
ImmutableArray<RegistryDiscoveryItem> currentItems = input.Item1.Item2;
ImmutableArray<RegistrationCandidate> candidates = input.Item2;
~~~

---

## 2. 프로젝트 구성

### 2.1 프로젝트 파일

RuniOS.CodeAnalysis.csproj의 핵심은 다음과 같습니다.

~~~xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp"
                      Version="4.3.0"
                      PrivateAssets="all" />
  </ItemGroup>
</Project>
~~~

각 설정의 의미:

- netstandard2.0: Unity/IDE가 analyzer DLL을 로드할 수 있는 호환성을 넓히기 위한 대상 프레임워크입니다.
- LangVersion latest: generator 구현 자체는 최신 C# 문법을 사용할 수 있습니다.
- Nullable enable: analyzer 프로젝트 소스의 nullable 분석을 켭니다.
- Microsoft.CodeAnalysis.CSharp 4.3.0: C# syntax API와 incremental generator API를 제공합니다.
- PrivateAssets=all: 이 Roslyn package 참조가 analyzer DLL을 소비하는 프로젝트의 일반 의존성으로 전파되지 않게 합니다.

SDK-style .csproj의 기본 규칙 때문에 별도 Compile Include를 쓰지 않아도 프로젝트 아래의 .cs 파일이 컴파일 대상이 됩니다.

### 2.2 빌드 후 Unity 패키지로 복사

csproj 아래의 CopyAnalyzerToUnityPackage target은 build 뒤 다음 디렉터리로 출력물을 복사합니다.

~~~text
../Runiverse OS/Packages/com.rumi.runios.core/Plugins/RuniOS.Analyzers/
~~~

복사 대상은 다음 두 개입니다.

~~~xml
<Copy SourceFiles="$(TargetPath)" ... />
<Copy SourceFiles="$(TargetDir)$(AssemblyName).pdb" ... />
~~~

현재 csproj에 AssemblyName이 따로 없으므로 기본 assembly 이름은 프로젝트 이름인 RuniOS.CodeAnalysis입니다. 따라서 기본 출력 이름은 다음과 같습니다.

~~~text
RuniOS.CodeAnalysis.dll
RuniOS.CodeAnalysis.pdb
~~~

### 2.3 Unity 쪽 import 상태

현재 Runiverse OS 쪽에는 다음 metadata가 있습니다.

~~~yaml
labels:
- RoslynAnalyzer
~~~

파일:

~~~text
Packages/com.rumi.runios.core/Plugins/RuniOS.Analyzers/RuniOS.Analyzers.dll.meta
~~~

이 label은 Unity가 DLL을 Roslyn analyzer/source generator 용도로 취급하도록 하는 Unity import 계약입니다.

현재 워킹 트리 점검에서 확인한 상태는 다음과 같습니다.

- 기존 RuniOS.Analyzers.dll과 .pdb가 있습니다.
- 새 프로젝트 이름으로 복사된 RuniOS.CodeAnalysis.dll과 .pdb도 있습니다.
- RuniOS.Analyzers.dll.meta에는 RoslynAnalyzer label이 있습니다.
- 현재 확인 범위에는 RuniOS.CodeAnalysis.dll.meta가 없습니다.

따라서 “새 csproj output이 Unity에서 실제로 로드되는가?”는 DLL 내용만으로 결정되지 않고, DLL 이름/.meta/Unity import 설정까지 확인해야 합니다. 이 문서 작성 중에는 .NET build나 Unity 재임포트 검증을 수행하지 않았습니다.

### 2.4 파일 지도

| 경로 | 역할 |
|---|---|
| Generators/EmbeddedAttributeSourceGenerator.cs | 생성 구현 타입을 참조 어셈블리 조회에서 숨길 Microsoft.CodeAnalysis.EmbeddedAttribute 선언을 compilation에 주입 |
| Generators/TypeRegistry/TypeRegistrySourceGenerator.cs | 모든 TypeRegistry generator가 공유하는 핵심 pipeline, registry 검증, manifest 탐색, output 생성 |
| Generators/TypeRegistry/AttributedTypeRegistrySourceGenerator.cs | AttributedTypeRegistry<TBase,TAttribute> 전용 후보 탐색/매칭/attribute 직접 등록 코드 |
| Generators/TypeRegistry/GenerateTypeRegistryAttributeSourceGenerator.cs | GenerateTypeRegistry 선언을 compilation에 주입 |
| Generators/TypeRegistry/TypeRegistryManifestAttributeSourceGenerator.cs | assembly-level manifest attribute 선언을 compilation에 주입 |
| Generators/TypeRegistry/TypeRegistryEmitter.cs | property/backing field, manifest, entry type 이름 및 소스 생성 |
| Generators/TypeRegistry/TypeRegistrySymbolHelpers.cs | assignability, accessibility, partial 선언, attribute inheritance 검사 |
| Generators/TypeRegistry/AttributeLiteralEmitter.cs | AttributeData를 new Attribute(...) C# 식으로 변환 |
| Generators/TypeRegistry/RegistryDefinition.cs | 레지스트리 property의 정규화 모델 |
| Generators/TypeRegistry/RegistryDiscoveryItem.cs | registry 발견 결과와 진단을 묶는 모델 |
| Generators/TypeRegistry/RegistrationCandidate.cs | 등록 검토 대상의 공통 모델 |
| Generators/TypeRegistry/AttributedRegistrationCandidate.cs | attribute 목록을 가진 구현 후보 |
| Generators/TypeRegistry/BoundRegistration.cs | 후보와 registry를 연결한 결과 |
| Generators/TypeRegistry/AttributedRegistrationPayload.cs | generated attribute 식 목록 |
| Generators/TypeRegistry/RegistryRegistrationGroup.cs | 같은 registry로 보낼 등록 목록 |
| Generators/TypeRegistry/RegistryOrigin.cs | current compilation인지 referenced manifest인지 구분 |
| Generators/SourceWriter.cs | indentation을 관리하는 간단한 소스 writer |
| Generators/GeneratorUtils.cs | fully-qualified 이름, escape, hash, literal, partial type header 생성 |
| Diagnostics/TypeRegistryDiagnostics0002~0019.cs | TypeRegistry generator/analyzer 진단 descriptor |
| Diagnostics/SuppressorDiagnostics0001.cs | suppressor descriptor ROS0001 |
| Analyzers/AssetRefSerializationSuppressor.cs | Unity UAC1001 억제 구현 |
| Analyzers/TypeRegistryManifestAttributeAnalyzer.cs | 직접 사용된 TypeRegistryManifestAttribute에 대한 ROS0018 경고 |
| Analyzers/TypeRegistrationAttributeAnalyzer.cs | generated registry 부재 및 TryResolve에서 확인할 수 없는 generic TypeRegistrationAttribute에 대한 ROS0019~ROS0022 진단과 non-generic 구현 제안 ROS0023 |
| RuniOS.CodeAnalysis.csproj | Roslyn package, target framework, Unity 복사 target |

현재 RuniOS.CodeAnalysis 저장소에는 이 generator 전용 테스트 프로젝트나 테스트 소스가 보이지 않습니다. 따라서 아래 설명은 소스 독해 기준이며, 모든 generated source/lifecycle 동작이 Unity에서 실행됐다는 뜻은 아닙니다.

---

## 3. generator가 기대하는 런타임 계약

generator만 보면 TypeRegistry가 무엇인지 알기 어렵습니다. generator가 읽는 런타임 타입은 Runiverse OS 쪽에 있습니다.

### 3.1 TypeRegistry

경로:

~~~text
Packages/com.rumi.runios.core/Runtime/Reflection/TypeRegistry.cs
~~~

핵심 API:

~~~csharp
public abstract class TypeRegistry
{
    public abstract event Action? onChanged;
    public abstract void Register(Type type);
    public abstract void Unregister(Type type);
}
~~~

generator는 어떤 구체 registry도 최소한 이 기본 타입에서 파생돼 있어야 한다고 봅니다.

~~~text
registryType.IsAbstract == false
TypeRegistry에서 파생
~~~

TypeRegistry는 source generator 전용이 아닙니다. generator가 없는 외부 어셈블리도 Register(Type)와 Unregister(Type)를 직접 호출할 수 있도록 남겨 둔 공통 계약입니다.

### 3.2 TypeRegistrationAttribute

경로:

~~~text
Packages/com.rumi.runios.core/Runtime/Reflection/TypeRegistrationAttribute.cs
~~~

현재 정의:

~~~csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public abstract class TypeRegistrationAttribute(Type targetType) : Attribute
{
    public Type targetType { get; } = targetType;
    public int priority { get; init; }
    public bool useForChildren { get; init; }
}
~~~

하나의 구현 클래스를 대상 타입에 연결하는 attribute 기본 클래스입니다.

예:

~~~csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class MyRegistrationAttribute(Type targetType)
    : TypeRegistrationAttribute(targetType);
~~~

~~~csharp
[MyRegistration(typeof(string), priority = 10)]
public sealed class StringHandler : HandlerBase
{
}
~~~

여기서 의미는 다음과 같습니다.

- StringHandler가 string을 대상으로 한다.
- priority가 높을수록 registry의 정렬 정책에서 먼저 고려됩니다.
- useForChildren=true면 target type에 할당 가능한 하위 타입에도 적용할 수 있습니다.
- AllowMultiple=true이면 하나의 구현 타입이 여러 대상 타입/여러 priority로 등록될 수 있습니다.

### 3.3 AttributedTypeRegistry<TBase,TAttribute>

경로:

~~~text
Packages/com.rumi.runios.core/Runtime/Reflection/AttributedTypeRegistry.cs
~~~

선언:

~~~csharp
public sealed class AttributedTypeRegistry<TBase, TAttribute> : TypeRegistry
    where TAttribute : TypeRegistrationAttribute
~~~

내부 등록 단위:

~~~csharp
public readonly record struct RegistrationEntry<TAttribute>
(
    Type implementationType,
    TAttribute attribute
);
~~~

registry가 실제로 저장하는 것은 다음과 같은 관계입니다.

~~~text
implementation Type
    + registration attribute instance
~~~

예를 들면:

~~~text
typeof(StringHandler)
    + new MyRegistrationAttribute(typeof(string)) { priority = 10 }
~~~

### 3.4 수동 Register(Type) 경로

AttributedTypeRegistry.Register(Type implementationType)은 다음 순서입니다.

~~~csharp
if (TBase가 abstract이고 TBase 자체를 등록하려는 경우)
    return;

if (TBase가 implementationType의 base/interface가 아니면)
    return;

implementationType.GetCustomAttributes<TAttribute>()
    -> dictionary에 추가
    -> registrationSnapshot 무효화
    -> resolutionCache 교체
    -> onChanged 호출
~~~

구체적인 코드 흐름:

1. typeof(TBase).IsAssignableFrom(implementationType)으로 구현 타입 계약을 검사합니다.
2. GetCustomAttributes<TAttribute>()로 런타임 리플렉션을 수행합니다.
3. registrationsByImplementationType에 attribute들을 넣습니다.
4. registrationSnapshot = default로 정렬 snapshot을 무효화합니다.
5. resolutionCache = []로 resolve cache를 통째로 교체합니다.
6. registration lock 밖에서 onChanged를 한 번 호출합니다.

이 경로가 generator가 없는 외부 어셈블리의 fallback입니다.

### 3.5 DirectRegister와 DirectRegisterRange

DirectRegister(Type, TAttribute)은 source generator가 이미 만든 attribute를 직접 넣습니다.

~~~csharp
public void DirectRegister(Type implementationType, TAttribute attribute)
~~~

이 API에는 runtime validation이 없습니다. 호출자가 잘못된 implementationType을 넘겨도 TBase assignability나 attribute 유효성을 다시 검사하지 않습니다.

DirectRegisterRange(ReadOnlySpan<RegistrationEntry<TAttribute>>)은 같은 일을 batch로 수행합니다.

~~~csharp
public void DirectRegisterRange(ReadOnlySpan<RegistrationEntry<TAttribute>> entries)
{
    lock (registrationLock)
    {
        foreach (RegistrationEntry<TAttribute> entry in entries)
        {
            // dictionary에 entry.attribute 추가
        }

        registrationSnapshot = default;
        resolutionCache = [];
    }

    lock (onChangedLock)
        _onChanged?.Invoke();
}
~~~

중요한 최적화/계약:

- lock은 batch 전체에 대해 한 번입니다.
- snapshot/cache 무효화도 batch당 한 번입니다.
- onChanged도 batch당 한 번입니다.
- 입력 validation은 하지 않습니다.
- 생성된 코드가 compile-time에 유효한 entry만 넣는다는 전제가 있습니다.

AttributedTypeRegistrySourceGenerator.EmitRegisterStatements가 이 API를 사용하는 이유입니다.

### 3.6 snapshot과 resolve

registrationEntries property를 처음 읽을 때만 dictionary를 flat array로 만들고 정렬합니다.

~~~csharp
registrationSnapshot =
[
    ..registrationsByImplementationType
        .SelectMany(pair => pair.Value.Select(attribute =>
            new RegistrationEntry<TAttribute>(pair.Key, attribute)))
        .OrderByTypes(
            x => x.attribute.targetType,
            x => x.attribute.priority)
];
~~~

정렬 정책은 현재 다음 두 key입니다.

~~~text
targetType의 타입 계층 우선순위
priority
~~~

동일 target type과 priority에 대해 generator가 assembly-qualified name 같은 제3의 정렬 key를 추가하지 않습니다. 같은 조건 안의 상대 순서는 기존 enumerable/등록 순서에 맡깁니다.

TryResolve(targetType, predicate)는 다음 순서입니다.

1. predicate가 없으면 resolutionCache에서 먼저 찾습니다.
2. registrationEntries를 정렬된 순서로 순회합니다.
3. targetType == attribute.targetType이면 exact match입니다.
4. 아니면 useForChildren가 true이고 targetType.IsAssignableToAny(...)가 성공하는지 봅니다.
5. match가 되면 matchedTargetType과 implementation type을 반환합니다.
6. implementation type이 generic definition이면 matched target type의 generic arguments로 닫습니다.
7. predicate가 없을 때만 성공/실패를 cache합니다.

generator는 이 runtime resolve 알고리즘을 생성하지 않습니다. generator는 registry가 load될 때 entry를 빠르게 넣는 코드를 생성합니다.

---

## 4. 새 generator와 기존 global reflection resolver의 차이

Runiverse OS에는 이름이 비슷하지만 다른 두 시스템이 현재 공존합니다.

### 4.1 기존 경로: AttributeTypeResolver

기존 경로는 다음 구조입니다.

~~~text
AttributeTypeResolver<TBase,TAttribute>
    -> ReflectionUtility.types
    -> 로드된 모든 assembly/type 탐색
    -> attribute reflection
    -> drawerTypes 정렬
    -> FindDrawerType
~~~

AttributeTypeResolver와 ReflectionUtility는 현재 소스에서 global type discovery deprecated라는 Obsolete 계약을 가지고 있습니다.

기존 handler attribute의 예:

~~~csharp
public abstract class TypeHandlerAttribute : Attribute
{
    public Type targetType { get; }
    public int priority { get; set; }
    public abstract bool isSubtypeCompatible { get; }
}
~~~

CustomCollectionHandlerAttribute도 현재는 다음 계열입니다.

~~~text
RuniOS.TypeHandlerAttribute
~~~

### 4.2 새 경로: AttributedTypeRegistry

새 generator 경로는 다음 구조입니다.

~~~text
[GenerateTypeRegistry]
    -> compile-time registry discovery
    -> current candidate discovery
    -> generated typeof + generated attribute instance
    -> assembly lifecycle registration
    -> AttributedTypeRegistry<TBase,TAttribute>
~~~

새 generator가 요구하는 attribute 계열은 다음입니다.

~~~text
RuniOS.Reflection.TypeRegistrationAttribute
~~~

기존 TypeHandlerAttribute와 새 TypeRegistrationAttribute는 같은 이름의 다른 계약입니다.

| 항목 | 기존 resolver | 새 generator 대상 |
|---|---|---|
| 기본 attribute | RuniOS.TypeHandlerAttribute | RuniOS.Reflection.TypeRegistrationAttribute |
| child 적용 flag | isSubtypeCompatible | useForChildren |
| 탐색 | ReflectionUtility.types global scan | 현재 compilation candidate + referenced manifest |
| 등록 | Register(Type)가 runtime attribute reflection | generated DirectRegisterRange |
| resolver | AttributeTypeResolver<TBase,TAttribute> | AttributedTypeRegistry<TBase,TAttribute> |

현재 Runiverse OS 패키지 소스에서 GenerateTypeRegistry 실제 사용처는 확인되지 않았습니다. 따라서 이 generator는 현재 코드에 이미 적용된 모든 AttributeTypeResolver를 자동으로 대체하는 상태가 아닙니다. 이름이 비슷하다고 기존 CollectionHandlerBase와 새 generator의 registry가 이미 연결됐다고 보면 안 됩니다.

---

## 5. generator가 compilation에 추가하는 marker attribute

두 marker attribute는 Runtime RuniOS API 파일에 직접 들어 있지 않습니다. generator가 각 compilation에 추가합니다.

`EmbeddedAttributeSourceGenerator`도 post-initialization output으로 다음 컴파일러 인식 특성을 compilation마다 한 번 추가합니다.

~~~csharp
namespace Microsoft.CodeAnalysis
{
    internal sealed partial class EmbeddedAttribute
        : global::System.Attribute;
}
~~~

generator가 소유하는 marker, manifest, lifecycle compatibility, registration 타입에는 이 특성이 붙습니다. 따라서 `InternalsVisibleTo`로 소비 어셈블리에서 `internal` 타입을 볼 수 있더라도, 참조 어셈블리의 일반 타입 조회 후보로 유입되지 않습니다.

### 5.1 GenerateTypeRegistryAttributeSourceGenerator

파일:

~~~text
Generators/TypeRegistry/GenerateTypeRegistryAttributeSourceGenerator.cs
~~~

클래스:

~~~csharp
[Generator]
public sealed class GenerateTypeRegistryAttributeSourceGenerator
    : IIncrementalGenerator
~~~

source 문자열에 다음 선언이 들어 있습니다.

~~~csharp
namespace RuniOS.Reflection
{
    [global::Microsoft.CodeAnalysis.Embedded]
    [global::System.AttributeUsage(
        global::System.AttributeTargets.Property,
        AllowMultiple = false,
        Inherited = false)]
    sealed class GenerateTypeRegistryAttribute
        : global::System.Attribute;
}
~~~

Initialize는 계산 provider를 만들지 않고 post-initialization output만 등록합니다.

~~~csharp
public void Initialize(IncrementalGeneratorInitializationContext context) =>
    context.RegisterPostInitializationOutput
    (
        static context => context.AddSource
        (
            "RuniOS.Reflection.GenerateTypeRegistryAttribute.g.cs",
            source
        )
    );
~~~

RegisterPostInitializationOutput은 사용자 소스에 GenerateTypeRegistry를 쓰기 전에 attribute 선언 자체를 compilation에 공급하는 단계입니다.

이 attribute가 generator 전용인 이유:

- 사용자는 일반 C# attribute처럼 쓸 수 있습니다.
- 런타임 패키지가 generator 관리용 marker 타입을 public API로 노출할 필요가 없습니다.
- generator가 꺼진 compilation에서는 marker 타입도 존재하지 않습니다.

### 5.2 TypeRegistryManifestAttributeSourceGenerator

파일:

~~~text
Generators/TypeRegistry/TypeRegistryManifestAttributeSourceGenerator.cs
~~~

주입하는 선언은 다음 구조입니다.

~~~csharp
namespace RuniOS.Reflection
{
    [global::Microsoft.CodeAnalysis.Embedded]
    [global::System.AttributeUsage(
        global::System.AttributeTargets.Assembly,
        AllowMultiple = true)]
    [global::System.ComponentModel.EditorBrowsable(
        global::System.ComponentModel.EditorBrowsableState.Never)]
    sealed class TypeRegistryManifestAttribute
        : global::System.Attribute
    {
        public global::System.Type ownerType { get; }
        public string propertyName { get; }
    }
}
~~~

이 attribute는 소스 생성기가 생성한 assembly manifest를 위해서만 사용합니다. 정의는 `internal`이고 `Embedded`로 참조 어셈블리 타입 조회에서 제외되며, `EditorBrowsable(Never)`로 IDE 노출도 숨깁니다. 사용자가 직접 적용하면 `TypeRegistryManifestAttributeAnalyzer`가 ROS0018 warning을 보고합니다. 생성된 source는 `GeneratedCodeAnalysisFlags.None` 설정으로 이 warning 대상에서 제외됩니다.

이 attribute의 목적은 “다른 어셈블리에 생성된 registry property가 어디 있는지”를 assembly metadata에 남기는 것입니다.

~~~text
contract assembly
    -> [assembly: TypeRegistryManifest(typeof(Owner), "registry")]
    -> implementation assembly generator가 IAssemblySymbol.GetAttributes()로 읽음
~~~

주의할 점: 현재 TypeRegistryManifestAttributeSourceGenerator.Initialize의 AddSource hint name은 다음과 같습니다.

~~~csharp
"RuniOS.Reflection.GenerateTypeRegistryAttribute.g.cs"
~~~

GenerateTypeRegistryAttributeSourceGenerator도 같은 hint name을 사용합니다. 두 generator가 동시에 로드되는 실제 Roslyn driver에서 hint name 중복이 허용되는지, 또는 이름을 분리해야 하는지는 통합 테스트로 확인할 필요가 있는 코드상 검토 지점입니다. 이 문서에서는 수정하지 않았습니다.

---

## 6. 공통 pipeline: TypeRegistrySourceGenerator

파일:

~~~text
Generators/TypeRegistry/TypeRegistrySourceGenerator.cs
~~~

이 클래스는 template method 패턴의 추상 base generator입니다.

~~~csharp
public abstract class TypeRegistrySourceGenerator : IIncrementalGenerator
~~~

공통 base가 고정하는 일:

- registry property 발견
- property 계약 검증
- current registry와 referenced manifest registry 통합
- property implementation과 manifest 생성
- candidate와 registry 연결
- registration grouping
- lifecycle bootstrap 생성
- generated hint name 중복 방지
- 공통 diagnostics

파생 generator가 제공하는 일:

- 어떤 registry type을 지원하는가
- candidate를 어떻게 발견하는가
- candidate가 어떤 registry와 match하는가
- 등록 문을 어떤 방식으로 출력하는가

현재 concrete 구현은 AttributedTypeRegistrySourceGenerator 하나입니다.

### 6.1 metadata name 상수

TypeRegistrySourceGenerator는 다음 metadata name을 사용합니다.

~~~csharp
protected const string generateTypeRegistryAttributeMetadataName =
    "RuniOS.Reflection.GenerateTypeRegistryAttribute";

protected const string typeRegistryManifestAttributeMetadataName =
    "RuniOS.Reflection.TypeRegistryManifestAttribute";

protected const string typeRegistryMetadataName =
    "RuniOS.Reflection.TypeRegistry";

protected const string onAssemblyLoadedAttributeMetadataName =
    "Unity.Scripting.LifecycleManagement.OnAssemblyLoadedAttribute";

protected const string onAssemblyUnloadingAttributeMetadataName =
    "Unity.Scripting.LifecycleManagement.OnAssemblyUnloadingAttribute";
~~~

마지막 두 이름은 “generated lifecycle method를 실제로 장식할 attribute가 compilation에 존재하는가?”를 검사하는 데 사용됩니다.

### 6.2 generatorName

~~~csharp
protected abstract string generatorName { get; }
~~~

generated registration class와 hint name에 들어가는 짧은 이름입니다.

현재 파생 클래스는 다음 값을 반환합니다.

~~~csharp
protected override string generatorName => "AttributedTypeRegistry";
~~~

결과적으로 대략 다음 이름이 생깁니다.

~~~text
RuniOS.AttributedTypeRegistry.Registration.XXXXXXXX.g.cs
RuniOS.Generated.__AttributedTypeRegistryRegistration_XXXXXXXX (partial)
~~~

`OnAssemblyLoaded`와 `OnAssemblyUnloading` method declaration은 generator의 post-initialization source에 있습니다. Unity lifecycle generator가 이 declaration을 입력 compilation에서 볼 수 있도록 하고, source generator는 같은 partial type의 registration implementation만 추가합니다. Unity.Scripting API가 없는 어셈블리에는 별도의 compile-only no-op compatibility declaration을 추가해 generator가 전역으로 주입되어도 해당 어셈블리가 깨지지 않도록 합니다. 실제 lifecycle API가 있는 어셈블리에서는 Unity의 원래 타입을 그대로 사용하고, 없는 어셈블리에서만 compatibility declaration이 사용됩니다. 생성 registration 타입과 compatibility attribute에는 `Microsoft.CodeAnalysis.EmbeddedAttribute`가 붙으므로 `InternalsVisibleTo`가 있어도 참조 어셈블리의 Unity 타입과 충돌하지 않습니다.
등록 타입의 `XXXXXXXX` 부분은 compilation마다 새로 선택되어 서로 다른 어셈블리에 같은 internal generated type 이름이 생기지 않도록 합니다.

### 6.3 Initialize: pipeline 선언

핵심 코드는 다음입니다.

~~~csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    IncrementalValuesProvider<RegistryDiscoveryItem> currentRegistries =
        context.SyntaxProvider.ForAttributeWithMetadataName
        (
            generateTypeRegistryAttributeMetadataName,
            static (_, _) => true,
            CreateCurrentRegistryDiscovery
        );

    IncrementalValuesProvider<RegistrationCandidate> candidates =
        CreateCandidateProvider(context);

    IncrementalValueProvider<...> input =
        context.CompilationProvider
            .Combine(currentRegistries.Collect())
            .Combine(candidates.Collect());

    context.RegisterSourceOutput(input, Execute);
}
~~~

각 줄의 의미:

1. ForAttributeWithMetadataName(...)
   - 현재 compilation의 syntax 중 정확히 GenerateTypeRegistryAttribute가 붙은 대상을 찾습니다.
   - attribute short name이 아니라 full metadata name으로 비교합니다.
2. static (_, _) => true
   - 이 generator 자체의 추가 syntax filter는 없습니다.
   - metadata name filter가 1차로 대상을 줄입니다.
3. CreateCurrentRegistryDiscovery
   - 찾은 대상 syntax와 semantic symbol을 registry definition으로 정규화합니다.
4. CreateCandidateProvider(context)
   - 파생 generator가 후보 탐색 방식을 결정합니다.
5. Collect()
   - registry/candidate 전체를 배열로 모읍니다.
6. CompilationProvider.Combine(...)
   - 현재 compilation과 수집된 registry/candidate를 하나의 Execute 입력으로 묶습니다.
7. RegisterSourceOutput(input, Execute)
   - provider 입력이 계산된 뒤 최종적으로 Execute를 호출합니다.

Initialize에서 파일 생성이나 reflection을 하는 것이 아닙니다. 이 시점은 pipeline 그래프를 등록하는 시점입니다.

### 6.4 파생 hook

#### IsSupportedRegistryType

~~~csharp
protected abstract bool IsSupportedRegistryType
(
    INamedTypeSymbol registryType,
    Compilation compilation
);
~~~

registry type이 이 concrete generator의 대상인지 결정합니다.

#### CreateRegistryInitializer

~~~csharp
protected virtual string CreateRegistryInitializer
(
    RegistryDefinition registry
) => $"new {GeneratorUtils.GetTypeName(registry.registryType)}()";
~~~

기본 generated backing field 초기화식은 단순한 parameterless constructor 호출입니다.

~~~csharp
static readonly global::Some.Registry __field =
    new global::Some.Registry();
~~~

#### TryValidateSupportedRegistryType

~~~csharp
protected virtual bool TryValidateSupportedRegistryType
(
    INamedTypeSymbol registryType,
    Compilation compilation,
    out Diagnostic? diagnostic
)
~~~

특정 registry type의 generic argument 같은 concrete 규칙을 파생 generator가 추가로 검사할 hook입니다.

#### CreateCandidateProvider

현재 compilation에서 등록 후보를 어떤 syntax로 찾을지 결정합니다.

#### TryBindCandidate

candidate 하나와 registry 하나를 실제 등록 관계로 연결합니다.

~~~text
registry + candidate
    -> match 안 됨: false, diagnostic 없음
    -> 오류로 거부: false, diagnostic 있음
    -> 등록 가능: true, BoundRegistration 있음
~~~

#### EmitRegisterStatements

기본 동작은 candidate마다 runtime Register(Type)를 호출하는 소스를 생성합니다.

~~~csharp
registry.Register(typeof(ImplementationType));
~~~

AttributedTypeRegistrySourceGenerator는 이 hook을 override해서 DirectRegisterRange를 생성합니다.

#### EmitUnregisterStatements

기본 동작은 구현 타입을 중복 제거한 뒤 Unregister(Type)를 한 번씩 생성합니다.

~~~csharp
HashSet<INamedTypeSymbol> implementationTypes =
    new(SymbolEqualityComparer.Default);
~~~

하나의 구현 클래스가 여러 registration attribute를 가지고 있어도 unload 시 구현 타입별 Unregister는 한 번만 나갑니다.

### 6.5 GetRegistryAccess

~~~csharp
protected static string GetRegistryAccess(RegistryDefinition registry)
{
    return $"{GeneratorUtils.GetTypeName(registry.ownerType)}."
        + $"{GeneratorUtils.EscapeIdentifier(registry.property.Name)}";
}
~~~

예:

~~~text
global::MyNamespace.HandlerBase.registry
~~~

fully-qualified owner type을 사용하고 property 이름은 C# keyword일 가능성까지 고려해 escape합니다.

### 6.6 현재 registry 발견: CreateCurrentRegistryDiscovery

ForAttributeWithMetadataName의 transform callback입니다.

~~~csharp
RegistryDiscoveryItem CreateCurrentRegistryDiscovery
(
    GeneratorAttributeSyntaxContext context,
    CancellationToken cancellationToken
)
~~~

실행 순서:

1. attribute가 붙은 대상 syntax 위치를 얻습니다.

   ~~~csharp
   Location location = context.TargetNode.GetLocation();
   ~~~

2. 대상이 property syntax이고 symbol도 IPropertySymbol인지 확인합니다.

   ~~~csharp
   if (context.TargetNode is not PropertyDeclarationSyntax declaration
       || context.TargetSymbol is not IPropertySymbol property)
   ~~~

3. 아니면 ROS0002를 담은 invalid discovery 결과를 만듭니다.

4. property를 TryCreateRegistryDefinition(...)으로 검증합니다.

5. 성공하면 RegistryOrigin.currentCompilation을 가진 RegistryDefinition을 반환합니다.

6. 실패하면 definition은 null이고 진단 하나를 가진 RegistryDiscoveryItem을 반환합니다.

여기서 발견 단계가 곧바로 소스를 생성하지 않는 점이 중요합니다. 발견 결과가 중간 모델로 저장되고, 모든 registry/candidate가 모인 뒤 Execute에서 output을 결정합니다.

---

## 7. registry property 검증: TryCreateRegistryDefinition

메서드:

~~~csharp
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
~~~

이 메서드는 “이 property를 generator가 안전하게 사용할 수 있는가?”를 검사한 뒤 RegistryDefinition을 만듭니다.

### 7.1 수동 property 구현 충돌 검사

현재 compilation의 generated 대상이면 먼저 다음을 검사합니다.

~~~csharp
if (requirePartial && declaration != null
    && HasManualPropertyImplementation(property))
~~~

다음과 같은 구현이 이미 있으면 거부합니다.

~~~csharp
public static partial Registry registry => new();
~~~

또는:

~~~csharp
public static partial Registry registry
{
    get { return existing; }
}
~~~

generator는 같은 partial property의 구현부를 다시 만들기 때문입니다. 이 경우 ROS0008을 보고합니다.

### 7.2 property contract

IsValidPropertyContract가 검사하는 조건은 다음과 같습니다.

~~~csharp
if (
    property.IsIndexer
    || !property.IsStatic
    || property.DeclaredAccessibility != Accessibility.Public
    || property.GetMethod == null
    || property.SetMethod != null
    || property.GetMethod.DeclaredAccessibility != Accessibility.Public
)
    return false;
~~~

즉 다음 형태여야 합니다.

~~~csharp
[GenerateTypeRegistry]
public static partial SomeConcreteRegistry registry { get; }
~~~

필수 조건:

- indexer가 아님
- static
- property 자체가 public
- getter 존재
- setter 없음
- getter도 public
- current compilation이면 partial syntax를 만족

requirePartial=false인 referenced manifest 복원에서는 source partial 문법을 다시 검사할 수 없으므로 위의 public/static/get-only 계약만 검사합니다.

### 7.3 partial property syntax

TypeRegistrySymbolHelpers.IsPartialPropertyDefinition은 syntax를 직접 확인합니다.

~~~csharp
if (
    declaration.Modifiers.All(x => x.Text != "partial")
    || declaration.AccessorList == null
)
    return false;

if (
    declaration.ExpressionBody != null
    || declaration.AccessorList.Accessors.Count != 1
)
    return false;

AccessorDeclarationSyntax accessor =
    declaration.AccessorList.Accessors[0];

return accessor.IsKind(GetAccessorDeclaration)
    && accessor.Body == null
    && accessor.ExpressionBody == null;
~~~

허용:

~~~csharp
public static partial Registry registry { get; }
~~~

거부:

~~~csharp
public static Registry registry { get; }
~~~

~~~csharp
public static partial Registry registry { get; set; }
~~~

~~~csharp
public static partial Registry registry => new();
~~~

### 7.4 owner type hierarchy

property를 선언한 타입과 모든 containing type을 검사합니다.

~~~csharp
if (
    !IsPublicTypeHierarchy(ownerType)
    || (requirePartial
        && !TypeRegistrySymbolHelpers.IsPartialTypeHierarchy(ownerType))
    || (ownerType.TypeKind != TypeKind.Class
        && ownerType.TypeKind != TypeKind.Struct)
)
~~~

따라서 다음 중 하나라도 있으면 ROS0004입니다.

- owner가 public이 아님
- 중첩 owner 중 하나가 public이 아님
- current compilation에서 containing type 중 하나가 partial이 아님
- owner가 class/struct가 아님

예를 들어 다음은 바깥 타입이 internal이므로 생성 코드가 외부 assembly에서 접근할 수 없습니다.

~~~csharp
internal static partial class InternalOwner
{
    public static partial Registry registry { get; }
}
~~~

중첩 타입도 모든 계층을 확인합니다.

~~~csharp
public partial class Outer
{
    public partial class Inner
    {
        [GenerateTypeRegistry]
        public static partial Registry registry { get; }
    }
}
~~~

Outer, Inner 모두 public/partial이어야 합니다.

### 7.5 registry type 확인

property type은 INamedTypeSymbol이어야 합니다.

~~~csharp
if (property.Type is not INamedTypeSymbol registryType)
~~~

그다음 현재 compilation에서 실제 RuniOS.Reflection.TypeRegistry를 찾습니다.

~~~csharp
INamedTypeSymbol? typeRegistry =
    compilation.GetTypeByMetadataName(typeRegistryMetadataName);
~~~

이후 조건:

~~~csharp
typeRegistry != null
registryType.IsAbstract == false
TypeRegistrySymbolHelpers.IsSameOrDerived(registryType, typeRegistry)
~~~

실패하면 ROS0005입니다.

즉 다음은 대상이 아닙니다.

~~~csharp
public static partial string registry { get; }
~~~

~~~csharp
public static partial AbstractRegistry registry { get; }
~~~

### 7.6 concrete generator 지원 여부

TypeRegistry 파생이라는 조건을 통과해도 현재 concrete generator가 지원하지 않을 수 있습니다.

~~~csharp
if (!IsSupportedRegistryType(registryType, compilation))
~~~

지원하는 generator가 없으면 ROS0006입니다.

현재 유일한 concrete generator는 AttributedTypeRegistry<,> 원본 정의만 지원합니다.

### 7.7 파생 generator 추가 검증

~~~csharp
if (!TryValidateSupportedRegistryType
    (registryType, compilation, out diagnostic))
~~~

현재 AttributedTypeRegistrySourceGenerator는 여기서 TAttribute가 TypeRegistrationAttribute에서 파생됐는지 검사합니다. 이 조건은 다음 장에서 자세히 설명합니다.

### 7.8 parameterless constructor

기본 initializer가 다음 식을 만들기 때문에 필요합니다.

~~~csharp
new global::Some.RegistryType()
~~~

HasAccessibleParameterlessConstructor는 모든 instance constructor를 순회하며 다음을 확인합니다.

~~~csharp
constructor.Parameters.Length == 0
&& IsAccessibleFromGeneratedCode(constructor, compilation)
~~~

없으면 ROS0007입니다.

### 7.9 stable ID와 backing field 충돌

모든 검증을 통과하면 stable ID를 만듭니다.

~~~csharp
string stableId = TypeRegistryEmitter.GetStableId
(
    property,
    registryType
);
~~~

stable ID는 다음 조합입니다.

~~~text
owner metadata name
|
property metadata name
|
fully-qualified registry type name
~~~

예:

~~~text
MyNamespace.HandlerBase|registry|global::RuniOS.Reflection.AttributedTypeRegistry<...>
~~~

이 stable ID는 backing field/hint name에 들어갈 hash의 원본입니다.

~~~text
__typeRegistry_{property.Name}_{GetShortHash(stableId)}
~~~

같은 owner type 안에 해당 backing field 이름이 이미 있으면 ROS0008입니다.

### 7.10 RegistryDefinition

최종 모델:

~~~csharp
new RegistryDefinition
(
    property,
    ownerType,
    registryType,
    origin,
    stableId
)
~~~

필드:

~~~csharp
public IPropertySymbol property { get; }
public INamedTypeSymbol ownerType { get; }
public INamedTypeSymbol registryType { get; }
public RegistryOrigin origin { get; }
public string stableId { get; }
~~~

Equals와 GetHashCode는 registry type이나 stable ID가 아니라 property symbol만 사용합니다.

~~~csharp
SymbolEqualityComparer.Default.Equals(property, other.property)
~~~

그러므로 같은 property가 current discovery와 manifest discovery 양쪽에서 들어와도 한 번만 처리됩니다.

---

## 8. 중간 모델들

generator가 syntax를 곧바로 문자열로 바꾸지 않고 중간 모델을 두는 이유는 discovery, validation, binding, emission 단계를 분리하기 위해서입니다.

### 8.1 RegistryOrigin

~~~csharp
public enum RegistryOrigin
{
    currentCompilation,
    referencedAssemblyManifest,
}
~~~

두 가지 의미:

- currentCompilation: 현재 빌드 중인 소스에서 GenerateTypeRegistry를 직접 찾음
- referencedAssemblyManifest: 참조 어셈블리의 assembly attribute에서 복원함

이 구분은 property/manifest source를 어디에 생성할지 결정할 때 사용됩니다. current registry에는 property 구현과 manifest를 생성하지만, referenced registry에는 그것을 다시 생성하지 않습니다.

### 8.2 RegistryDiscoveryItem

~~~csharp
sealed class RegistryDiscoveryItem
(
    RegistryDefinition? definition,
    IPropertySymbol? property,
    Location location,
    bool isCurrent,
    ImmutableArray<Diagnostic> diagnostics
)
~~~

발견 실패도 값으로 전달합니다.

~~~text
definition = null
diagnostics = [ROS0005]
~~~

이 구조 덕분에 한 registry가 잘못돼도 다른 registry의 output을 계속 만들 수 있습니다.

### 8.3 RegistrationCandidate

~~~csharp
public abstract class RegistrationCandidate(ISymbol symbol)
{
    public ISymbol symbol { get; } = symbol;
}
~~~

공통 candidate base는 “이 symbol을 registration 후보로 검토한다”는 최소 정보만 보관합니다.

### 8.4 AttributedRegistrationCandidate

~~~csharp
sealed class AttributedRegistrationCandidate
(
    INamedTypeSymbol implementationType,
    ImmutableArray<AttributeData> attributes
) : RegistrationCandidate(implementationType)
~~~

추가 정보:

- 후보 구현 타입
- 타입과 base type hierarchy에서 수집한 등록 attribute들

### 8.5 BoundRegistration

~~~csharp
public sealed class BoundRegistration
(
    RegistryDefinition registry,
    INamedTypeSymbol implementationType,
    object? payload
)
~~~

뜻:

~~~text
RegistryDefinition + ImplementationType + generator-specific payload
~~~

공통 base pipeline은 payload의 구체 타입을 몰라도 됩니다. AttributedTypeRegistrySourceGenerator만 AttributedRegistrationPayload로 캐스팅합니다.

### 8.6 AttributedRegistrationPayload

~~~csharp
sealed class AttributedRegistrationPayload
(
    ImmutableArray<string> attributeExpressions
)
~~~

예:

~~~text
new global::MyRegistrationAttribute(typeof(string)) { priority = 10 }
new global::MyRegistrationAttribute(typeof(int)) { priority = 20 }
~~~

Roslyn AttributeData를 런타임 attribute object로 저장하지 않고 generated C# 식 문자열로 저장합니다. source generator가 실행되는 시점에는 대상 runtime attribute object를 만들 수 없기 때문입니다.

### 8.7 RegistryRegistrationGroup

~~~csharp
sealed class RegistryRegistrationGroup
(
    RegistryDefinition registry,
    ImmutableArray<BoundRegistration> registrations
)
~~~

같은 registry property로 들어갈 registration을 하나로 묶습니다.

이 그룹은 최종 generated registration class에서 다음 단위를 결정합니다.

~~~text
registry A
  -> A에 넣을 registrations

registry B
  -> B에 넣을 registrations
~~~

---

## 9. AttributedTypeRegistrySourceGenerator 상세

파일:

~~~text
Generators/TypeRegistry/AttributedTypeRegistrySourceGenerator.cs
~~~

선언:

~~~csharp
[Generator]
public sealed class AttributedTypeRegistrySourceGenerator
    : TypeRegistrySourceGenerator
~~~

Generator는 concrete generator에만 붙어 있습니다. abstract base가 Roslyn에 별도 generator로 등록되는 구조가 아닙니다.

### 9.1 지원하는 registry type

metadata name:

~~~csharp
const string attributedTypeRegistryMetadataName =
    "RuniOS.Reflection.AttributedTypeRegistry`2";
~~~

검사:

~~~csharp
INamedTypeSymbol? attributedRegistry =
    compilation.GetTypeByMetadataName
    (
        attributedTypeRegistryMetadataName
    );

return attributedRegistry != null
    && SymbolEqualityComparer.Default.Equals
    (
        registryType.OriginalDefinition,
        attributedRegistry
    );
~~~

OriginalDefinition을 비교하는 이유:

~~~text
AttributedTypeRegistry<HandlerBase, MyAttribute>
    -> OriginalDefinition
       AttributedTypeRegistry<TBase,TAttribute>
~~~

구성된 generic type의 인자 값이 아니라 generic type 자체가 같은지 확인합니다.

### 9.2 TAttribute 검증

metadata name:

~~~csharp
const string typeRegistrationAttributeMetadataName =
    "RuniOS.Reflection.TypeRegistrationAttribute";
~~~

다음 조건을 검사합니다.

~~~csharp
registryType.TypeArguments is [_, INamedTypeSymbol attributeType]
&& TypeRegistrySymbolHelpers.IsSameOrDerived(attributeType, registrationAttribute)
~~~

실패하면 ROS0013입니다.

올바른 예:

~~~csharp
AttributedTypeRegistry<HandlerBase, MyRegistrationAttribute>
~~~

여기서 MyRegistrationAttribute는 TypeRegistrationAttribute의 파생 타입이어야 합니다.

잘못된 예:

~~~csharp
AttributedTypeRegistry<HandlerBase, System.Attribute>
~~~

### 9.3 후보 provider 생성

핵심 코드:

~~~csharp
protected override IncrementalValuesProvider<RegistrationCandidate>
    CreateCandidateProvider
(
    IncrementalGeneratorInitializationContext context
) =>
    context.SyntaxProvider.CreateSyntaxProvider
    (
        static (node, _) =>
            node is ClassDeclarationSyntax
            or RecordDeclarationSyntax,
        static (syntaxContext, cancellationToken) =>
            CreateCandidate(syntaxContext, cancellationToken)
    )
    .Where(static candidate => candidate != null)
    .Select
    (
        static RegistrationCandidate (candidate, _) => candidate!
    );
~~~

단계별 의미:

1. syntax pre-filter
   - class 또는 record declaration만 semantic 변환 대상으로 합니다.
   - method, property, enum, struct는 여기서 제외됩니다.
2. transform
   - CreateCandidate가 declared symbol을 얻습니다.
3. Where(candidate != null)
   - 등록 attribute가 없는 타입을 버립니다.
4. Select
   - nullable candidate를 공통 RegistrationCandidate 타입으로 넘깁니다.

RecordDeclarationSyntax를 받지만 CreateCandidate 안에서 TypeKind.Class를 다시 검사하므로 record class는 가능하고 record struct는 최종 후보가 되지 않습니다.

### 9.4 CreateCandidate

~~~csharp
static AttributedRegistrationCandidate? CreateCandidate
(
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken
)
~~~

첫 검사:

~~~csharp
if (
    context.Node is not TypeDeclarationSyntax
    || context.SemanticModel.GetDeclaredSymbol
       (context.Node, cancellationToken)
       is not INamedTypeSymbol
          { TypeKind: TypeKind.Class } implementationType
)
    return null;
~~~

이 조건은 다음을 의미합니다.

- syntax가 class-like declaration이어야 합니다.
- semantic symbol을 얻어야 합니다.
- 실제 TypeKind가 class여야 합니다.

그다음 현재 compilation에서 등록 attribute base를 찾습니다.

~~~csharp
INamedTypeSymbol? registrationAttribute =
    context.SemanticModel.Compilation
        .GetTypeByMetadataName
        (
            typeRegistrationAttributeMetadataName
        );
~~~

없으면 후보를 만들지 않습니다.

이후:

~~~csharp
ImmutableArray<AttributeData> attributes =
    TypeRegistrySymbolHelpers.GetInheritedAttributes
    (
        implementationType,
        registrationAttribute
    );

return attributes.Length == 0
    ? null
    : new AttributedRegistrationCandidate
      (
          implementationType,
          attributes
      );
~~~

즉 후보 provider는 단순히 직접 attribute가 붙은 클래스만 찾지 않습니다. GetInheritedAttributes를 통해 base class에서 상속 가능한 registration attribute도 모읍니다.

### 9.5 TryBindCandidate

시그니처:

~~~csharp
protected override bool TryBindCandidate
(
    RegistryDefinition registry,
    RegistrationCandidate candidate,
    Compilation compilation,
    out BoundRegistration? registration,
    out Diagnostic? diagnostic
)
~~~

#### 단계 1: candidate와 generic arguments 확보

~~~csharp
registration = null;
diagnostic = null;

if (
    candidate is not AttributedRegistrationCandidate attributedCandidate
    || registry.registryType.TypeArguments.Length != 2
)
    return false;
~~~

이 concrete generator가 모르는 candidate나 잘못된 registry shape는 조용히 match 실패합니다.

~~~csharp
if (
    registry.registryType.TypeArguments[0] is not { } baseType
    || registry.registryType.TypeArguments[1]
       is not INamedTypeSymbol attributeType
)
    return false;
~~~

baseType는 TBase, attributeType은 TAttribute입니다.

#### 단계 2: 이 registry의 attribute만 선택

~~~csharp
ImmutableArray<AttributeData> matchingAttributes =
    attributedCandidate.attributes
        .Where(attribute =>
            TypeRegistrySymbolHelpers.IsSameOrDerived
            (
                attribute.AttributeClass,
                attributeType
            ))
        .ToImmutableArray();
~~~

TAttribute 그 자체만 허용하는 것이 아니라 TAttribute에서 파생된 실제 attribute도 허용합니다.

~~~text
TAttribute = BaseRegistrationAttribute
실제 attribute = SpecializedRegistrationAttribute : BaseRegistrationAttribute
~~~

이 경우 실제 derived attribute type을 generated expression에 유지합니다.

matching attribute가 없으면 이 registry와는 관계없는 candidate이므로 false입니다.

#### 단계 3: implementation type이 TBase 계열인지 확인

~~~csharp
if (!TypeRegistrySymbolHelpers.IsSameOrDerived
    (attributedCandidate.implementationType, baseType))
    return false;
~~~

class inheritance뿐 아니라 interface 구현, generic unbound definition까지 helper 규칙으로 처리합니다.

#### 단계 4: abstract candidate 처리

~~~csharp
if (attributedCandidate.implementationType.IsAbstract)
{
    diagnostic = TypeRegistryDiagnostics.Create
    (
        TypeRegistryDiagnostics.abstractCandidate,
        TypeRegistrySymbolHelpers.GetLocation
        (
            attributedCandidate.implementationType
        ),
        GeneratorUtils.GetTypeName
        (
            attributedCandidate.implementationType
        )
    );
    return false;
}
~~~

추상 타입은 typeof(AbstractType)로 등록할 수는 있어도 일반적인 구현 인스턴스 후보가 아니므로 등록에서 제외합니다. 진단 severity는 warning인 ROS0016입니다.

#### 단계 5: implementation accessibility

~~~csharp
if (!TypeRegistrySymbolHelpers.IsAccessibleFromGeneratedCode
    (attributedCandidate.implementationType, compilation))
~~~

generated registration code가 typeof(ImplementationType)를 써야 하므로 접근 가능해야 합니다.

- 현재 assembly 내부의 internal은 같은 compilation generated source에서 접근 가능합니다.
- referenced assembly의 internal은 접근 불가능합니다.
- private/protected containing type는 외부 generated source에서 접근할 수 없습니다.

실패하면 현재 구현은 ROS0015 descriptor를 사용합니다.

#### 단계 6: 각 attribute를 C# 식으로 변환

~~~csharp
ImmutableArray<string>.Builder attributeExpressions =
    ImmutableArray.CreateBuilder<string>
    (
        matchingAttributes.Length
    );

foreach (AttributeData attribute in matchingAttributes)
{
    if (!AttributeLiteralEmitter.TryRender
        (
            attribute,
            compilation,
            out string expression,
            out diagnostic
        ))
        return false;

    attributeExpressions.Add(expression);
}
~~~

AttributeData는 Roslyn이 해석한 metadata입니다. 이를 나중에 runtime object로 직렬화하지 않고, generated code가 다시 constructor를 호출하도록 문자열을 만듭니다.

#### 단계 7: bound registration 생성

~~~csharp
registration = new BoundRegistration
(
    registry,
    attributedCandidate.implementationType,
    new AttributedRegistrationPayload
    (
        attributeExpressions.ToImmutable()
    )
);
return true;
~~~

이 시점부터는 “이 implementation type의 이 attribute expression들을 이 registry에 넣어라”라는 완성된 중간 모델입니다.

### 9.6 EmitRegisterStatements: batch 직접 등록

override 구현은 다음 모양의 소스를 만들기 시작합니다.

~~~csharp
registry.DirectRegisterRange(
    new global::RuniOS.Reflection.RegistrationEntry<TAttribute>[]
    {
        // entry들
    }
);
~~~

실제 코드:

~~~csharp
string registryAccess = GetRegistryAccess(registry);
string entryType =
    TypeRegistryEmitter.RenderRegistrationEntryType(registry);

writer.AppendLine($"{registryAccess}.DirectRegisterRange(");
writer.Indent();
writer.AppendLine($"new {entryType}[]");
writer.AppendLine("{");
writer.Indent();
~~~

각 bound registration의 payload에서 attribute expression을 꺼냅니다.

~~~csharp
foreach (string attributeExpression in payload.attributeExpressions)
{
    writer.AppendLine($"new {entryType}(");
    writer.Indent();
    writer.AppendLine
    (
        $"typeof({GeneratorUtils.GetTypeOfGenericDefinitionName"
        + "(registration.implementationType)}),"
    );
    writer.AppendLine($"{attributeExpression}),");
    writer.Unindent();
}
~~~

하나의 implementation type에 attribute가 3개면 RegistrationEntry도 3개 생성됩니다.

~~~text
implementation A + attribute 1 -> entry 1
implementation A + attribute 2 -> entry 2
implementation A + attribute 3 -> entry 3
~~~

### 9.7 generated registration에서 generic type을 unbound로 출력하는 이유

~~~csharp
typeof(global::Some.GenericHandler<>)
~~~

GetTypeOfGenericDefinitionName은 generic type symbol이면 ConstructUnboundGenericType()을 사용합니다.

runtime AttributedTypeRegistry.TryResolve가 필요할 때 matched target type의 generic arguments를 이용해 implementation type을 닫기 때문입니다.

---

## 10. Execute: 실제 output 생성 순서

Execute는 공통 pipeline의 실질적인 본체입니다.

~~~csharp
void Execute
(
    SourceProductionContext context,
    ((Compilation, ImmutableArray<RegistryDiscoveryItem>),
        ImmutableArray<RegistrationCandidate>) input
)
~~~

### 10.1 input 분해

~~~csharp
Compilation compilation = input.Item1.Item1;
ImmutableArray<RegistryDiscoveryItem> currentItems = input.Item1.Item2;
ImmutableArray<RegistrationCandidate> candidates = input.Item2;
~~~

그리고 진단 중복 방지용 local set을 만듭니다.

~~~csharp
HashSet<string> reportedDiagnosticKeys = [];
~~~

Report local function은 다음 조합을 key로 사용합니다.

~~~text
diagnostic.Id
|
source file path
|
source span start
|
formatted message
~~~

같은 진단이 여러 discovery 경로에서 다시 생겨도 한 번만 ReportDiagnostic합니다.

### 10.2 current registry definition 수집

~~~csharp
List<RegistryDefinition> definitions = [];
HashSet<IPropertySymbol> seenProperties =
    new(SymbolEqualityComparer.Default);
~~~

current item마다:

1. item이 가진 diagnostics를 먼저 보고합니다.
2. definition이 있고 property가 처음 보는 symbol이면 definitions에 넣습니다.

### 10.3 referenced manifest registry 수집

~~~csharp
foreach (RegistryDiscoveryItem item
    in DiscoverManifestRegistries(compilation))
~~~

manifest에서 복원한 registry도 같은 definitions 목록에 넣습니다.

같은 property가 current discovery와 manifest discovery에 동시에 존재해도 seenProperties로 한 번만 들어갑니다.

### 10.4 current registry만 property/manifest source 생성

~~~csharp
Dictionary<string, string> hintOwners = [];

foreach (RegistryDefinition registry in definitions)
{
    if (registry.origin != RegistryOrigin.currentCompilation)
        continue;

    AddGeneratedSource(... property ...);
    AddGeneratedSource(... manifest ...);
}
~~~

current compilation의 registry에는 두 source가 생깁니다.

1. partial property implementation
2. assembly manifest

referenced registry에는 이미 이 source가 contract assembly에 들어 있으므로 다시 생성하지 않습니다.

### 10.5 candidate 중복 제거

~~~csharp
List<RegistrationCandidate> uniqueCandidates = [];
HashSet<ISymbol> seenCandidates =
    new(SymbolEqualityComparer.Default);
~~~

후보 provider가 같은 symbol을 여러 syntax 경로에서 내놓더라도 symbol equality로 한 번만 처리합니다.

### 10.6 모든 registry와 모든 candidate를 매칭

~~~csharp
List<BoundRegistration> registrations = [];

foreach (RegistryDefinition registry in definitions)
{
    foreach (RegistrationCandidate candidate in uniqueCandidates)
    {
        if (TryBindCandidate
            (
                registry,
                candidate,
                compilation,
                out BoundRegistration? registration,
                out Diagnostic? diagnostic
            ))
        {
            if (registration != null)
                registrations.Add(registration);
        }

        if (diagnostic != null)
            Report(diagnostic);
    }
}
~~~

알고리즘 관점에서는 현재 다음과 같습니다.

~~~text
for each registry
    for each candidate
        try bind
~~~

따라서 registry property가 N개이고 candidate가 M개면 binding hook 호출은 기본적으로 N x M번입니다. incremental provider가 입력을 캐시하더라도 Execute 안의 이 논리 자체는 이중 순회입니다.

### 10.7 generic owner 거부

~~~csharp
if (registration.registry.ownerType.IsGenericType)
~~~

generic containing type이 소유한 registry는 자동 lifecycle registration에서 제외합니다.

~~~text
generic owner
    -> 어떤 closed generic owner instance에 등록할지 불명확
    -> 공통 static bootstrap으로 안전하게 등록할 수 없음
    -> ROS0012
~~~

property별로 ROS0012를 한 번만 보고하고 해당 registration을 emitRegistrations에 넣지 않습니다.

이 검사는 “generic registry type”이 아니라 “registry property를 선언한 owner type이 generic인가”를 보는 것입니다.

### 10.8 등록할 것이 없으면 종료

~~~csharp
if (emitRegistrations.Count == 0)
    return;
~~~

registry property implementation과 manifest는 앞 단계에서 이미 생성될 수 있습니다. 그러나 candidate가 하나도 없으면 lifecycle registration implementation source는 생성하지 않습니다.

### 10.9 등록 대상 어트리뷰트에서 lifecycle API 확인

`Execute`는 registry property만으로 ROS0009를 발생시키지 않습니다. `AttributedTypeRegistrySourceGenerator.TryBindCandidate`가 실제 등록 대상 어트리뷰트를 찾고, candidate가 해당 registry에 등록 가능한 상태인지 확인한 뒤 lifecycle API를 검사합니다.

필요한 API가 없으면 `AttributeData.ApplicationSyntaxReference`에서 얻은 등록 대상 어트리뷰트 위치에 ROS0009를 보고하고 해당 registration을 생성하지 않습니다. 따라서 registry property만 선언된 경우에는 이 진단이 발생하지 않으며, Unity lifecycle API가 없는 어셈블리에서 자동 등록 대상 어트리뷰트를 사용한 경우에만 해당 어트리뷰트가 진단 위치가 됩니다.

검사는 정확한 metadata name을 가진 public 타입이 도달 가능한 참조 어셈블리에 있는지 확인합니다. 컴파일 전용 호환 선언처럼 다른 어셈블리의 `internal` fallback 타입은 실제 Unity API로 인정하지 않습니다.

현재 generator가 기대하는 이름:

~~~text
Unity.Scripting.LifecycleManagement.OnAssemblyLoadedAttribute
Unity.Scripting.LifecycleManagement.OnAssemblyUnloadingAttribute
~~~

반면 현재 Runiverse OS 소스 검색에서 실제 lifecycle 사용 예는 대부분 다음입니다.

~~~text
Unity.Scripting.LifecycleManagement.OnCodeLoaded
Unity.Scripting.LifecycleManagement.OnCodeUnloading
~~~

이는 단순한 using 별칭 차이가 아니라 metadata name 문자열의 차이입니다. 외부 Unity API에 두 API가 모두 있는지, 또는 generator가 현재 프로젝트 API에 맞는지 통합 시 확인해야 합니다. 현재 소스 기준으로는 명시적인 계약 불일치 지점입니다.

### 10.10 registry별 group 생성

~~~csharp
foreach (RegistryDefinition registry in definitions)
{
    ImmutableArray<BoundRegistration> registryRegistrations =
        emitRegistrations
            .Where(x => x.registry.Equals(registry))
            .ToImmutableArray();

    if (registryRegistrations.Length != 0)
        groups.Add
        (
            new RegistryRegistrationGroup
            (
                registry,
                registryRegistrations
            )
        );
}
~~~

같은 registry property에 해당하는 bound registration만 하나의 group으로 묶습니다.

### 10.11 lifecycle registration source 생성

group들의 stable ID를 합쳐 registration source의 이름을 만듭니다.

~~~csharp
string registrationStableId =
    string.Join
    (
        "|",
        groups.Select(x => x.registry.stableId)
    );

string registrationHintName =
    GeneratorUtils.GetRegistrationHintName
    (
        generatorName,
        registrationStableId
    );
~~~

writer를 열고:

~~~csharp
SourceWriter writer =
    GeneratorUtils.CreateRegistrationWriter
    (
        generatorName
    );
~~~

그 뒤 generated class 안에 다음 두 method를 씁니다.

~~~csharp
#pragma warning disable CS0618

private static partial void RegisterGeneratedTypesCore()
{
    // registry별 EmitRegisterStatements
}

private static partial void UnregisterGeneratedTypesCore()
{
    // registry별 EmitUnregisterStatements
}

#pragma warning restore CS0618
~~~

CS0618를 잠시 끄는 이유는 현재 runtime의 DirectRegister/DirectRegisterRange가 source generator 전용이라는 Obsolete 표시를 가지고 있기 때문입니다.

`OnAssemblyLoaded`와 `OnAssemblyUnloading` wrapper method 및 attribute는 post-initialization source에 있고, 위 generated source는 그 wrapper가 호출하는 partial core method를 구현합니다.

마지막으로:

~~~csharp
string registrationSource =
    GeneratorUtils.FinishRegistration(writer);

AddGeneratedSource(... registrationSource ...);
~~~

이제 compilation에는 registry property, manifest, lifecycle declaration, lifecycle registration implementation이 추가됩니다.

## 11. 이미 빌드된 다른 assembly의 registry를 찾는 방법

현재 compilation 안에서 선언된 registry는 syntax와 semantic model로 찾을 수 있습니다. 그러나 다른 assembly에서 선언된 registry property는 현재 프로젝트의 syntax tree에 없습니다. 그래서 generator는 별도 manifest를 사용합니다.

### 11.1 manifest attribute의 역할

TypeRegistryManifestAttributeSourceGenerator.cs는 다음과 같은 assembly-level attribute를 compilation에 추가하려는 generator입니다.

~~~csharp
[assembly: global::RuniOS.Reflection.TypeRegistryManifestAttribute
(
    typeof(MyRegistryOwner),
    "services"
)]
~~~

이 attribute는 registry instance를 직접 담지 않습니다. 다음 두 정보만 담습니다.

~~~csharp
public Type ownerType { get; }
public string propertyName { get; }
~~~

즉, 의미는 다음과 같습니다.

~~~text
이 assembly 안의 MyRegistryOwner.services라는 static property가
TypeRegistry를 제공하는 registry definition이다.
~~~

instance 자체를 assembly attribute에 넣지 않는 이유는 다음과 같습니다.

- attribute argument는 compile-time constant, typeof(...), enum, 배열 등 제한된 값만 표현할 수 있습니다.
- registry instance는 runtime object입니다.
- 실제 instance 생성은 generated property의 backing field에서 해야 합니다.
- 다른 assembly에서 접근할 때는 owner type과 property를 symbol로 다시 찾아야 합니다.

따라서 manifest는 object reference가 아니라 symbol lookup용 index입니다.

### 11.2 DiscoverManifestRegistries

TypeRegistrySourceGenerator.DiscoverManifestRegistries의 흐름은 다음과 같습니다.

~~~csharp
INamedTypeSymbol? manifestAttribute =
    compilation.GetTypeByMetadataName
    (
        "RuniOS.Reflection.TypeRegistryManifestAttribute"
    );

if (manifestAttribute == null)
    return ImmutableArray<RegistryDiscoveryItem>.Empty;
~~~

먼저 현재 compilation에서 manifest attribute의 symbol을 찾습니다. marker attribute generator가 제대로 작동하지 않거나 manifest type 자체가 존재하지 않으면 아무것도 찾지 않습니다.

그 다음 참조 assembly를 순회합니다.

~~~csharp
foreach (IAssemblySymbol assembly in EnumerateReferencedAssemblies(compilation))
{
    foreach (AttributeData attribute in assembly.GetAttributes())
    {
        if
        (
            !SymbolEqualityComparer.Default.Equals
            (
                attribute.AttributeClass,
                manifestAttribute
            )
        )
            continue;

        // constructor argument에서 ownerType과 propertyName을 decode한다.
        // ownerType.GetMembers(propertyName)에서 property를 찾는다.
        // TryCreateRegistryDefinition(... requirePartial: false)를 호출한다.
    }
}
~~~

EnumerateReferencedAssemblies는 direct reference만 보지 않습니다.

~~~csharp
var visited = new HashSet<IAssemblySymbol>
(
    SymbolEqualityComparer.Default
);

var pending = new Stack<IAssemblySymbol>
(
    compilation.SourceModule.ReferencedAssemblySymbols
);

while (pending.Count != 0)
{
    IAssemblySymbol assembly = pending.Pop();

    if (!visited.Add(assembly))
        continue;

    yield return assembly;

    foreach (IAssemblySymbol referencedAssembly in
             assembly.Modules.SelectMany(x => x.ReferencedAssemblySymbols))
    {
        pending.Push(referencedAssembly);
    }
}
~~~

핵심은 HashSet<IAssemblySymbol>에 SymbolEqualityComparer.Default를 넣는 것입니다. Roslyn symbol은 일반적인 object reference equality만으로 비교하면 안 됩니다. 같은 metadata symbol을 같은 것으로 판단해야 하므로 Roslyn comparer를 사용합니다.

### 11.3 manifest를 definition으로 복원하기

manifest의 constructor argument에서 얻은 ownerType은 INamedTypeSymbol입니다. 여기에 attribute가 가리키는 이름을 적용합니다.

~~~csharp
IPropertySymbol? property =
    ownerType
        .GetMembers(propertyName)
        .OfType<IPropertySymbol>()
        .FirstOrDefault();
~~~

property를 찾으면 current compilation에서 찾을 때와 거의 같은 검증을 수행합니다. 차이는 requirePartial이 false라는 점입니다.

현재 compilation의 property는 generator가 구현해야 하므로 다음 조건이 중요합니다.

~~~text
public static partial TRegistry Property { get; }
~~~

이미 참조 assembly에 들어온 property는 partial method/property의 source 문법을 다시 검사할 수 없습니다. metadata에는 컴파일된 public static get-only property만 보이면 되므로 partial requirement를 다시 강제하지 않습니다.

그 외에는 다음을 다시 검사합니다.

- owner type이 public hierarchy인지
- property type이 구체적인 TypeRegistry인지
- 해당 registry type을 현재 generator가 지원하는지
- parameterless constructor를 호출할 수 있는지
- stable ID를 만들 수 있는지

문제가 있으면 generator가 전체 compilation을 깨뜨리기보다는 ROS0010 warning을 보고 해당 manifest만 버립니다.

### 11.4 왜 manifest가 필요한가

source generator는 참조 assembly의 원본 source를 다시 읽지 않습니다. 현재 compilation에서 사용할 수 있는 것은 metadata symbol뿐입니다. 또한 “어떤 property를 registry로 취급할 것인가”라는 정보는 일반적인 TypeRegistry inheritance만으로 결정되지 않습니다. 따라서 registry를 선언한 assembly가 manifest를 내보내고, consuming compilation이 이를 수집하는 구조입니다.

전체 흐름은 다음과 같습니다.

~~~text
library A
  [GenerateTypeRegistry] public static partial Registry Services { get; }
        │
        ├─ generated property implementation
        └─ assembly-level TypeRegistryManifestAttribute(owner, "Services")
                                │
                                ▼
application B generator
  referenced assembly metadata에서 manifest 발견
  owner.Services property 복원
  B의 implementation candidate를 Services에 bind
  B의 generated registration code에서 A의 public property 접근
~~~

## 12. TypeRegistryEmitter: registry property와 manifest source를 쓰는 코드

TypeRegistryEmitter.cs는 syntax tree를 조작하는 Roslyn rewriter가 아닙니다. 검증된 RegistryDefinition을 받아 C# source text를 직접 출력하는 emitter입니다.

### 12.1 stable ID

~~~csharp
string stableId =
    string.Join
    (
        "|",
        GeneratorUtils.GetMetadataName(property.ContainingType),
        property.MetadataName,
        GeneratorUtils.GetTypeName(registryType)
    );
~~~

예를 들어 개념적으로 다음과 같은 문자열입니다.

~~~text
MyCompany.Core.RegistryHost|Services|global::RuniOS.Reflection.AttributedTypeRegistry<...>
~~~

stable ID는 generated source의 이름과 충돌 감지에 사용합니다. property의 짧은 이름만 쓰지 않는 이유는 다음과 같습니다.

- 서로 다른 owner type이 같은 property name을 가질 수 있습니다.
- 같은 owner type 안에서도 generic registry type이 다를 수 있습니다.
- 여러 generator instance가 같은 compilation에 참여할 수 있습니다.

### 12.2 backing field 이름

~~~csharp
string backingFieldName =
    $"__typeRegistry_{property.Name}_{hash}";
~~~

실제 구현은 property.Name을 identifier로 escape하고 stable ID의 hash를 뒤에 붙입니다. 결과는 대략 다음 모양입니다.

~~~csharp
static readonly global::Some.Namespace.RegistryType
    __typeRegistry_Services_A1B2C3D4 =
    new global::Some.Namespace.RegistryType();
~~~

hash만 쓰지 않고 property name을 앞에 남기는 것은 generated source를 사람이 읽고 디버깅하기 쉽게 하기 위해서입니다.

### 12.3 property implementation

RenderPropertyImplementation은 owner의 namespace와 containing type hierarchy를 복원합니다.

개념적으로 출력되는 source는 다음과 같습니다.

~~~csharp
namespace MyCompany.Core
{
    public static partial class RegistryHost
    {
        static readonly global::RuniOS.Reflection
            .AttributedTypeRegistry
            <
                global::MyCompany.Core.IService,
                global::MyCompany.Core.ServiceRegistrationAttribute
            >
            __typeRegistry_Services_A1B2C3D4 =
            new global::RuniOS.Reflection
                .AttributedTypeRegistry
                <
                    global::MyCompany.Core.IService,
                    global::MyCompany.Core.ServiceRegistrationAttribute
                >();

        public static partial
            global::RuniOS.Reflection
                .AttributedTypeRegistry<...>
            Services =>
            __typeRegistry_Services_A1B2C3D4;
    }
}
~~~

실제 output은 한 줄로 압축되지 않고 SourceWriter의 indentation을 이용해 출력됩니다. 중요한 점은 property 선언이 원래 source의 partial 선언과 짝을 이룬다는 것입니다.

원본:

~~~csharp
public static partial RegistryType Services { get; }
~~~

generated:

~~~csharp
public static partial RegistryType Services => __typeRegistry_Services_A1B2C3D4;
~~~

즉 source generator는 기존 property를 수정하는 것이 아니라 partial declaration을 만족하는 별도의 implementation declaration을 추가합니다.

### 12.4 nested type와 generic owner

owner가 단순한 top-level class가 아닐 수도 있습니다.

~~~csharp
public partial class Outer
{
    public partial class Inner
    {
        [GenerateTypeRegistry]
        public static partial RegistryType Services { get; }
    }
}
~~~

이 경우 emitter는 Outer와 Inner를 바깥쪽부터 다시 열어야 합니다.

~~~csharp
public partial class Outer
{
    public partial class Inner
    {
        // generated property
    }
}
~~~

GeneratorUtils.GetContainingTypes가 이 hierarchy를 outermost-to-innermost 순서로 반환합니다. 각 type header는 RenderTypeDeclarationHeader로 출력하며, generic type parameter와 constraint도 유지합니다.

다만 generic owner의 registration은 별도의 제약이 있습니다. Execute는 registry.ownerType.TypeParameters.Length != 0인 registration을 ROS0012로 보고 건너뜁니다. registry property 자체를 출력하는 것과, assembly lifecycle 시점에 generic owner의 static property에 접근하는 것은 다른 문제이기 때문입니다.

### 12.5 manifest source

RenderManifest는 registry object를 생성하지 않습니다. 다음과 같은 assembly attribute만 출력합니다.

~~~csharp
[assembly: global::RuniOS.Reflection.TypeRegistryManifestAttribute
(
    typeof(global::MyCompany.Core.RegistryHost),
    "Services"
)]
~~~

owner가 generic type이면 GetTypeOfGenericDefinitionName을 사용해 open generic definition 형태의 typeof를 만들려고 합니다. 이 정보는 consuming compilation이 owner type과 property를 symbol로 찾는 데 사용됩니다.

### 12.6 generated source hint name

property source hint는 다음 규칙입니다.

~~~text
RuniOS.TypeRegistry.Property.{hash}.g.cs
~~~

manifest source hint는 다음 규칙입니다.

~~~text
RuniOS.TypeRegistry.Manifest.{hash}.g.cs
~~~

hint name은 실제 filesystem 경로가 아닙니다. context.AddSource(hintName, sourceText)에서 Roslyn이 generated tree를 식별하기 위한 논리적 이름입니다.

## 13. GeneratorUtils와 SourceWriter

작은 utility들이지만 generated C#의 안정성을 담당하므로 중요합니다.

### 13.1 metadata name과 display name은 다르다

GetMetadataName은 namespace, containing type, MetadataName을 재귀적으로 연결합니다.

MetadataName은 generic arity를 보존합니다. 예를 들어 다음 두 type은 C# display name은 비슷해 보여도 metadata identity는 다릅니다.

~~~text
A.Outer<T>.Inner<U>
A.Outer generic arity 1.Inner generic arity 1
~~~

generator가 stable ID나 GetTypeByMetadataName와 관련된 작업을 할 때는 이 차이가 중요합니다.

반대로 generated source에 출력할 type은 GetTypeName을 사용합니다.

~~~csharp
symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
~~~

결과는 보통 다음 모양입니다.

~~~text
global::MyCompany.Namespace.Type
~~~

앞에 global::을 붙이는 이유는 generated source의 namespace context나 사용자가 만든 type alias에 영향받지 않게 하기 위해서입니다.

### 13.2 generic type 이름

GetTypeOfGenericDefinitionName은 generic named type을 unbound generic type으로 바꾼 뒤 typeof에 넣을 수 있는 이름을 만듭니다.

예를 들면 개념적으로 다음과 같습니다.

~~~csharp
typeof(global::MyNamespace.Handler<>)
~~~

이것은 “특정 type argument로 이미 닫힌 Handler”가 아니라 “Handler generic definition”을 표현합니다. runtime AttributedTypeRegistry.TryResolve가 matched target의 generic arguments로 MakeGenericType을 수행할 수 있게 하는 부분과 연결됩니다.

### 13.2.1 GetShortHash

GetShortHash는 stable ID를 짧은 generated name으로 줄입니다.

~~~csharp
uint hash = 2166136261;

foreach (char character in value)
{
    hash ^= character;
    hash *= 16777619;
}

return hash.ToString("X8", CultureInfo.InvariantCulture);
~~~

이는 FNV-1a 계열의 32-bit hash와 같은 형태입니다. 구현은 문자열을 UTF-8 byte sequence로 변환하지 않고 .NET char 단위로 순회한다는 점을 기억해야 합니다. 따라서 같은 stable ID 문자열이면 어느 machine에서도 같은 8자리 hexadecimal 결과를 얻고, owner/property/registry identity가 바뀌면 generated member와 hint name도 바뀝니다.

### 13.3 identifier escape

EscapeIdentifier는 Roslyn SyntaxFacts를 사용해 keyword와 contextual keyword를 검사합니다.

~~~csharp
public static partial RegistryType @event { get; }
~~~

처럼 source에서 합법적인 이름이라도 generated source 문자열을 만들 때 escape를 빠뜨리면 compile error가 됩니다. property name, type parameter name, named attribute member name 같은 identifier가 이 helper를 거칩니다.

### 13.4 string/char literal

StringLiteral과 CharLiteral은 attribute expression을 source로 재출력할 때 사용됩니다. 다음 문자들을 escape합니다.

- null, bell, backspace
- tab, newline, vertical tab, form feed, carriage return
- quote, apostrophe, backslash
- 그 외 control character는 unicode escape

따라서 attribute의 string argument가 단순한 영문이라고 가정하지 않습니다.

~~~csharp
[SomeAttribute("line 1\nline 2")]
~~~

를 generated source 안에서도 동일한 값으로 보존해야 합니다.

### 13.5 type declaration header

RenderTypeDeclarationHeader는 다음 종류를 처리합니다.

- class
- struct
- interface
- record class
- record struct
- static, abstract, sealed, readonly
- generic type parameters
- unmanaged, struct, class, class?, notnull
- type constraint
- new()

generated member가 nested type 안에 들어가야 하는 경우, 이 header가 원래 hierarchy와 compatible해야 합니다. 단순히 public partial class만 출력하면 record, struct, generic constraint를 가진 owner에서 generated declaration이 깨집니다.

### 13.6 registration writer

CreateRegistrationWriter는 다음 공통 머리말을 출력합니다.

~~~csharp
#nullable enable

namespace RuniOS.Generated
{
    internal static partial class
        __AttributedTypeRegistryRegistration_XXXXXXXX
    {
        // caller가 method를 출력한다.
    }
}
~~~

FinishRegistration은 열어 둔 class와 namespace scope를 닫습니다. source writer가 scope stack을 검사해 주는 것은 아니므로 Indent와 Unindent 호출 균형은 caller 책임입니다.

### 13.7 SourceWriter

SourceWriter는 의도적으로 매우 작습니다.

~~~csharp
readonly StringBuilder builder = new();
int indentation;

public void AppendLine(string value = "")
{
    if (value.Length != 0)
        builder.Append(' ', indentation * 4).Append(value);

    builder.AppendLine();
}

public void Indent() => indentation++;
public void Unindent() => indentation--;
public override string ToString() => builder.ToString();
~~~

특징은 다음과 같습니다.

- indentation 단위는 4 spaces
- 빈 줄은 indentation 없이 출력
- syntax validity를 검사하지 않음
- brace balance를 관리하지 않음
- formatting만 담당하고 semantic decision은 하지 않음

## 14. TypeRegistrySymbolHelpers: Roslyn symbol을 안전하게 비교하고 검증하는 코드

이 파일은 generator의 “컴파일 가능한 source를 출력할 수 있는가?”를 판단하는 공통 semantic helper입니다.

### 14.1 IsSameOrDerived

registry type과 candidate type을 비교할 때 단순히 Name을 비교하지 않습니다.

개념적 알고리즘은 다음과 같습니다.

~~~text
IsSameOrDerived(candidate, target)
  1. exact SymbolEqualityComparer.Default 비교
  2. 두 symbol이 named generic이면 OriginalDefinition 비교
  3. candidate의 interface 목록을 재귀적으로 검사
  4. candidate의 BaseType chain을 따라가며 검사
  5. 모두 아니면 false
~~~

이 helper가 필요한 이유는 다음과 같습니다.

~~~csharp
class Concrete : Base, IMarker
~~~

에서 Concrete는 직접 base class만 봐서는 IMarker와의 관계가 보이지 않지만 interface를 통해 assignable합니다. generic type은 constructed type과 original definition을 구분해야 합니다.

### 14.2 accessibility

IsAccessibleFromGeneratedCode는 generated source가 해당 type을 참조할 수 있는지 확인합니다.

기본 원칙은 다음과 같습니다.

- 같은 assembly의 symbol은 internal 접근이 가능한 것으로 취급
- 다른 assembly의 public symbol은 접근 가능
- private는 접근 불가
- protected는 generated code의 위치에 따라 달라질 수 있으므로 일반적으로 허용하지 않음
- namespace는 type 접근성 검사의 대상이 아님
- module은 containing assembly로 올려서 검사

현재 generator가 “같은 compilation에서 생성되는 code”라는 점을 반영해, current assembly의 internal candidate도 허용할 수 있게 되어 있습니다. 반대로 referenced assembly의 internal implementation type을 consuming assembly의 generated source에서 직접 typeof할 수는 없습니다.

이 검사는 특히 attribute 재출력에서 중요합니다. attribute class는 public인데 constructor가 internal이거나, named property가 private이면 generated code가 해당 attribute expression을 만들 수 없습니다.

### 14.3 parameterless constructor

registry property의 implementation을 다음처럼 만들기 때문입니다.

~~~csharp
new global::Some.RegistryType()
~~~

HasAccessibleParameterlessConstructor는 다음을 확인합니다.

- instance constructor인가
- parameter count가 0인가
- generated code에서 접근 가능한가

명시적 constructor가 하나라도 있고 parameterless constructor가 없으면 C#이 암묵적 parameterless constructor를 만들지 않으므로 ROS0007이 발생합니다.

### 14.4 partial hierarchy

partial property를 nested owner 안에 출력하려면 property만 partial이어서는 부족합니다.

~~~csharp
public class Outer
{
    public partial class Inner
    {
        public static partial RegistryType Services { get; }
    }
}
~~~

generated source가 Outer를 다시 선언해야 하는데 Outer가 partial이 아니면 같은 type의 두 declaration을 합칠 수 없습니다. 그래서 모든 containing type declaration이 source에 존재하고 partial modifier를 가지고 있는지 검사합니다.

### 14.5 partial property definition

IsPartialPropertyDefinition이 기대하는 형태는 매우 좁습니다.

~~~csharp
public static partial RegistryType Services { get; }
~~~

검사 항목:

- partial modifier
- accessor list 존재
- expression body 없음
- accessor가 정확히 하나
- 그 하나가 getter
- getter body 없음
- indexer 아님

다음은 허용 대상이 아닙니다.

~~~csharp
public static partial RegistryType Services { get; set; }
public static partial RegistryType Services => Existing;
public static partial RegistryType this[int index] { get; }
~~~

### 14.6 inherited attribute 수집

GetInheritedAttributes는 candidate class와 base class chain에서 registration attribute를 수집합니다.

~~~csharp
[ServiceRegistration(typeof(IService))]
public class BaseService
{
}

public class DerivedService : BaseService
{
}
~~~

registration attribute가 Inherited = true라면 DerivedService에도 해당 registration이 적용됩니다. 반대로 Inherited = false이면 base에 붙은 attribute를 candidate에 복사하지 않습니다.

현재 TypeRegistrationAttribute 자체는 다음 attribute usage를 사용합니다.

~~~csharp
[AttributeUsage
(
    AttributeTargets.Class,
    Inherited = false,
    AllowMultiple = true
)]
~~~

따라서 TypeRegistrationAttribute 계열 registration을 class inheritance로 자동 전파하지 않는 것이 현재 runtime contract입니다. derived class가 같은 registration을 사용하려면 직접 attribute를 붙여야 합니다.

### 14.7 location

diagnostic의 정확한 위치를 만들기 위해 symbol 또는 AttributeData에서 location을 얻습니다.

- property diagnostic: property declaration location
- candidate diagnostic: class declaration location
- attribute diagnostic: attribute syntax location
- manifest metadata diagnostic: source location이 없을 수 있으므로 Location.None

이 차이 때문에 referenced assembly의 잘못된 manifest는 consuming project의 특정 source line이 아니라 warning으로만 표시될 수 있습니다.

## 15. AttributeLiteralEmitter: AttributeData를 C# expression으로 바꾸는 과정

source generator가 AttributeData를 가지고 있다고 해서 attribute object를 그대로 generated source에 넣을 수 있는 것은 아닙니다.

AttributeData는 Roslyn이 semantic analysis 결과로 제공하는 immutable metadata view입니다. 이를 다음과 같은 C# expression으로 다시 써야 합니다.

~~~csharp
new global::MyCompany.ServiceRegistrationAttribute
(
    typeof(global::MyCompany.IService)
)
{
    priority = 10,
    useForChildren = true
}
~~~

### 15.1 entry point

~~~csharp
TryRender
(
    AttributeData attribute,
    Compilation compilation,
    out string expression,
    out Diagnostic? diagnostic
)
~~~

가장 먼저 다음을 확인합니다.

1. attribute class가 있는가
2. constructor symbol을 찾을 수 있는가
3. attribute type이 generated code에서 접근 가능한가
4. constructor가 접근 가능한가
5. 각 constructor argument를 render할 수 있는가
6. 각 named argument의 member를 찾을 수 있는가
7. named member가 writable이고 접근 가능한가

실제 constructor call에는 attribute의 concrete type을 사용합니다. 예를 들어 registry의 TAttribute가 base type이어도, 실제 candidate에 붙은 derived attribute가 SpecialServiceRegistrationAttribute라면 그 concrete type으로 출력합니다.

### 15.2 constructor argument

TryRenderConstant가 처리하는 대표적인 값은 다음과 같습니다.

| Roslyn TypedConstantKind | generated expression |
| --- | --- |
| Null | null |
| Type | typeof(global::...) |
| Enum | enum member 또는 numeric cast |
| Array | new ElementType[] { ... } |
| Primitive | C# literal |

TypedConstantKind.Error이거나 지원하지 않는 kind면 expression을 만들지 않고 ROS0014를 발생시킵니다.

### 15.3 primitive literal

다음 primitive를 invariant culture로 출력합니다.

- bool
- char
- string
- signed integer
- unsigned integer
- float
- double
- decimal

float와 double의 NaN, positive infinity, negative infinity도 별도 처리합니다. 단순히 value.ToString()만 사용하면 현재 culture나 NaN 표현 차이 때문에 generated source가 깨질 수 있습니다.

예를 들어 다음과 같은 값은 literal emitter가 특별히 처리해야 합니다.

~~~text
float.NaN
double.PositiveInfinity
decimal 1.25M
unsigned integer suffix
~~~

### 15.4 enum

enum 값은 먼저 정확히 일치하는 declared field를 찾습니다. 정확한 이름이 없으면 flags 조합을 복원하려고 합니다.

~~~csharp
[Flags]
enum Options
{
    None = 0,
    Fast = 1,
    Safe = 2,
    Verbose = 4
}
~~~

값이 Fast | Safe라면 가능한 경우 다음처럼 출력합니다.

~~~csharp
Options.Fast | Options.Safe
~~~

구현은 set bit 수가 많은 field부터 greedy하게 선택하고, 같은 조건에서는 declaration order를 사용합니다. 표현할 수 없는 값이면 underlying numeric value를 enum cast로 출력합니다.

### 15.5 named argument

named argument의 member는 attribute class에서만 찾지 않습니다. base class chain도 따라갑니다.

~~~csharp
public abstract class BaseRegistrationAttribute : Attribute
{
    public int priority { get; init; }
}

public sealed class ServiceRegistrationAttribute
    : BaseRegistrationAttribute
{
}
~~~

priority가 base에 선언되어 있어도 FindMember가 찾습니다. 단, 다음은 거부합니다.

- setter가 없는 property
- private setter
- readonly field
- const field
- generated code에서 접근 불가능한 member

### 15.6 실패를 diagnostic으로 바꾸는 이유

attribute를 runtime reflection으로 다시 읽을 수 있다고 가정하면 generated registration source를 만들 수 없습니다. 하지만 source generator의 목적은 runtime reflection을 제거하고 compile-time registration을 만드는 것입니다.

따라서 다음 상황을 조용히 무시하지 않습니다.

- inaccessible attribute: ROS0015
- unsupported constructor/named argument: ROS0014

이렇게 해야 “왜 이 class가 registry에 들어가지 않았는가?”를 compile 단계에서 알 수 있습니다.

## 16. 처음부터 끝까지 보는 실제 예제

다음 예제는 현재 generator가 기대하는 구조를 단순화한 것입니다. lifecycle attribute가 실제 compilation에서 제공된다는 전제까지 포함합니다.

### 16.1 runtime contract

~~~csharp
public interface IService
{
}

public sealed class ServiceRegistrationAttribute(Type targetType)
    : global::RuniOS.Reflection.TypeRegistrationAttribute(targetType)
{
}
~~~

registry owner는 다음처럼 선언합니다.

~~~csharp
public static partial class Registries
{
    [global::RuniOS.Reflection.GenerateTypeRegistry]
    public static partial
        global::RuniOS.Reflection.AttributedTypeRegistry
        <
            IService,
            ServiceRegistrationAttribute
        >
        Services
    {
        get;
    }
}
~~~

registration candidate는 다음과 같습니다.

~~~csharp
[ServiceRegistration(typeof(IService), priority = 10)]
public sealed class LoggingService : IService
{
}
~~~

### 16.2 generator가 이 입력을 읽는 순서

1. GenerateTypeRegistryAttributeSourceGenerator가 marker attribute를 compilation에 주입합니다.
2. 공통 pipeline의 ForAttributeWithMetadataName이 Services property를 발견합니다.
3. property symbol을 semantic model에서 얻습니다.
4. TryCreateRegistryDefinition이 property contract를 검증합니다.
5. AttributedTypeRegistrySourceGenerator의 candidate provider가 LoggingService class를 발견합니다.
6. GetInheritedAttributes가 ServiceRegistrationAttribute를 수집합니다.
7. TryBindCandidate가 LoggingService가 IService에 assignable한지 확인합니다.
8. AttributeLiteralEmitter가 attribute를 generated expression으로 재출력합니다.
9. TypeRegistryEmitter가 Registries.Services의 backing field/property를 생성합니다.
10. TypeRegistryEmitter가 manifest를 생성합니다.
11. Execute가 bound registration을 registry group에 넣습니다.
12. lifecycle API가 있으면 registration/unregistration source를 생성합니다.

### 16.3 property generated source

개념적으로 다음 source가 추가됩니다.

~~~csharp
namespace MyCompany
{
    public static partial class Registries
    {
        static readonly
            global::RuniOS.Reflection.AttributedTypeRegistry
            <
                global::MyCompany.IService,
                global::MyCompany.ServiceRegistrationAttribute
            >
            __typeRegistry_Services_A1B2C3D4 =
            new
                global::RuniOS.Reflection.AttributedTypeRegistry
                <
                    global::MyCompany.IService,
                    global::MyCompany.ServiceRegistrationAttribute
                >();

        public static partial
            global::RuniOS.Reflection.AttributedTypeRegistry<...>
            Services =>
            __typeRegistry_Services_A1B2C3D4;
    }
}
~~~

원래 property는 body가 없는 partial declaration이고, generated declaration이 expression-bodied implementation을 제공합니다.

### 16.4 manifest generated source

~~~csharp
[assembly: global::RuniOS.Reflection.TypeRegistryManifestAttribute
(
    typeof(global::MyCompany.Registries),
    "Services"
)]
~~~

이 source는 runtime에 별도 registry를 등록하지 않습니다. 다른 compilation이 이 assembly를 reference할 때 registry property를 찾을 수 있도록 metadata marker를 추가합니다.

### 16.5 lifecycle registration generated source

Unity lifecycle generator가 먼저 보는 post-initialization source는 다음 구조입니다.

~~~csharp
namespace RuniOS.Generated
{
    [global::Microsoft.CodeAnalysis.Embedded]
    static partial class __AttributedTypeRegistryRegistration_XXXXXXXX
    {
        [global::Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void RegisterGeneratedTypes()
        {
            RegisterGeneratedTypesCore();
        }

        [global::Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void UnregisterGeneratedTypes()
        {
            UnregisterGeneratedTypesCore();
        }

        static partial void RegisterGeneratedTypesCore();
        static partial void UnregisterGeneratedTypesCore();
    }
}
~~~

source generator가 추가하는 implementation source는 다음 구조입니다.

~~~csharp
namespace RuniOS.Generated
{
    internal static partial class
        __AttributedTypeRegistryRegistration_XXXXXXXX
    {
        private static partial void RegisterGeneratedTypesCore()
        {
            global::MyCompany.Registries.Services.DirectRegisterRange
            (
                new global::RuniOS.Reflection.RegistrationEntry
                    <
                        global::MyCompany.ServiceRegistrationAttribute
                    >[]
                {
                    new
                        global::RuniOS.Reflection.RegistrationEntry
                    <
                        global::MyCompany.ServiceRegistrationAttribute
                    >
                    (
                        typeof(global::MyCompany.LoggingService),
                        new global::MyCompany.ServiceRegistrationAttribute
                        (
                            typeof(global::MyCompany.IService)
                        )
                        {
                            priority = 10
                        }
                    )
                }
            );
        }

        private static partial void UnregisterGeneratedTypesCore()
        {
            global::MyCompany.Registries.Services.Unregister
            (
                typeof(global::MyCompany.LoggingService)
            );
        }
    }
}
~~~

실제 emitter는 type name을 길게 여러 줄로 나누기보다 fully qualified type name을 출력하지만, 의미는 같습니다.

### 16.6 runtime에서 일어나는 일

compile-time:

~~~text
attribute/class/property 분석
  -> post-initialization lifecycle declaration과 generated C# implementation 추가
  -> 일반 C# compiler가 generated source까지 함께 compile
  -> assembly에 registry property와 lifecycle method가 포함
~~~

runtime:

~~~text
assembly load
  -> OnAssemblyLoaded method 호출
  -> Registries.Services의 singleton-like backing instance 획득
  -> DirectRegisterRange로 LoggingService registration 삽입

resolve 요청
  -> target type 검색
  -> registration 순서대로 target match
  -> priority/type ordering에 따라 첫 registration 반환

assembly unload
  -> OnAssemblyUnloading method 호출
  -> 각 implementation type을 Unregister
~~~

여기서 source generator가 만든 것은 registry의 resolve algorithm 자체가 아닙니다. generator는 어떤 type을 언제 registry에 넣을지 source로 고정하고, 실제 target matching은 runtime의 AttributedTypeRegistry가 담당합니다.

## 17. Analyzer와 suppressor: AssetRef 경고를 선택적으로 숨기는 코드

이 repository에는 source generator뿐 아니라 diagnostic suppressor도 있습니다. 파일은 Analyzers/AssetRefSerializationSuppressor.cs입니다.

### 17.1 analyzer가 필요한 이유

Unity analyzer가 다음과 같은 경고를 낸다고 가정합니다.

~~~text
UAC1001: 특정 field type은 Unity serialization을 지원하지 않는다
~~~

RuniOS의 AssetRef<TAsset>는 의도적으로 일반 Unity serialization 규칙과 다른 저장/해석 방식을 사용할 수 있습니다. 모든 UAC1001을 끄면 실제 문제까지 숨겨집니다. 따라서 AssetRef<TAsset> field에 해당하는 경우만 suppression합니다.

### 17.2 클래스 선언

~~~csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AssetRefSerializationSuppressor
    : DiagnosticSuppressor
{
}
~~~

일반 DiagnosticAnalyzer와 이름이 비슷하지만 base type이 DiagnosticSuppressor입니다. compiler 또는 다른 analyzer가 보고한 diagnostic을 보고, 특정 diagnostic에 suppression을 추가하는 역할입니다.

### 17.3 SupportedSuppressions

SuppressorDiagnostics0001 파일에서 suppression descriptor를 정의합니다.

~~~csharp
public override ImmutableArray<SuppressionDescriptor>
    SupportedSuppressions =>
    ImmutableArray.Create
    (
        SuppressorDiagnostics.descriptor
    );
~~~

descriptor에는 suppression ID와 suppress할 diagnostic ID가 들어갑니다. 현재 suppressor는 ROS0001 descriptor를 통해 UAC1001을 대상으로 합니다.

### 17.4 ReportSuppressions

핵심 흐름은 다음과 같습니다.

~~~csharp
INamedTypeSymbol? assetRefType =
    context.Compilation.GetTypeByMetadataName
    (
        "RuniOS.Resource.AssetRef`1"
    );

if (assetRefType == null)
    return;

foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
{
    if (diagnostic.Id != "UAC1001")
        continue;

    if (!diagnostic.Location.IsInSource)
        continue;

    // source tree와 diagnostic span으로 syntax node를 찾는다.
    // 가장 가까운 FieldDeclarationSyntax를 찾는다.
    // semantic model에서 field symbol을 얻는다.
    // field.Type.OriginalDefinition과 assetRefType을 비교한다.
    // 같으면 context.ReportSuppression(...)을 호출한다.
}
~~~

실제 소스의 metadata lookup은 AssetRef의 generic metadata identity, 즉 arity 1인 definition을 사용합니다. 핵심은 AssetRef의 constructed type이 아니라 generic original definition을 찾는다는 점입니다.

### 17.5 왜 OriginalDefinition을 비교하는가

AssetRef<TAsset>는 generic type이므로 field마다 실제 type argument가 다를 수 있습니다.

~~~csharp
AssetRef<Texture2D> texture;
AssetRef<AudioClip> audio;
~~~

두 field의 constructed type은 다르지만 OriginalDefinition은 같습니다.

~~~text
AssetRef<Texture2D>.OriginalDefinition == AssetRef<>
AssetRef<AudioClip>.OriginalDefinition == AssetRef<>
~~~

따라서 suppressor는 특정 TAsset 종류가 아니라 AssetRef generic family 전체를 대상으로 삼을 수 있습니다.

### 17.6 declaration-level 검사

diagnostic location은 field type 이름이나 variable declarator에 잡힐 수 있습니다. 그래서 suppressor는 location span을 곧바로 “이게 field symbol이다”라고 가정하지 않습니다.

1. SourceTree를 얻습니다.
2. root에서 diagnostic span의 가장 안쪽 node를 찾습니다.
3. FirstAncestorOrSelf<FieldDeclarationSyntax>()로 field declaration을 찾습니다.
4. 그 declaration 안의 모든 variable declarator를 순회합니다.
5. SemanticModel.GetDeclaredSymbol로 실제 IFieldSymbol을 얻습니다.

한 declaration에 여러 field가 있더라도 diagnostic에 해당하는 field type을 semantic symbol로 확인한 뒤 suppression합니다.

### 17.7 generator와 suppressor의 차이

~~~text
source generator
  입력: compilation/source/metadata
  출력: 새 source tree

diagnostic analyzer
  입력: syntax/semantic/operation/compilation
  출력: 진단

diagnostic suppressor
  입력: 다른 diagnostic
  출력: 해당 diagnostic을 숨길 suppression
~~~

AssetRefSerializationSuppressor는 source를 추가하지 않습니다. 또한 모든 AssetRef 관련 compiler warning을 무조건 무시하지 않고, diagnostic ID가 UAC1001인지와 field의 OriginalDefinition이 정확히 AssetRef generic definition인지 모두 확인합니다.

## 18. Diagnostics: 실패를 표현하는 방식

Diagnostics/TypeRegistryDiagnostics0002~0019.cs는 type registry generator/analyzer의 ROS0002~ROS0023 진단 descriptor를 모아 둔 파일입니다. category는 RuniOS.TypeRegistry입니다.

| ID | severity | 의미 |
| --- | --- | --- |
| ROS0001 | suppression | AssetRef<TAsset>에 대한 UAC1001 suppression |
| ROS0002 | error | GenerateTypeRegistryAttribute가 property가 아닌 대상에 적용됨 |
| ROS0003 | error | property가 public static partial get-only non-indexed 계약을 만족하지 않음 |
| ROS0004 | error | owner containing type hierarchy가 public/partial이 아님 |
| ROS0005 | error | property type이 구체적인 TypeRegistry-derived type이 아님 |
| ROS0006 | error | registry type을 지원하는 generator가 없음 |
| ROS0007 | error | registry type에 접근 가능한 parameterless constructor가 없음 |
| ROS0008 | error | generated member name이 기존 member와 충돌함 |
| ROS0009 | error | 등록 대상 어트리뷰트 바인딩 중 public OnAssemblyLoaded/OnAssemblyUnloading API를 찾을 수 없음 |
| ROS0010 | warning | referenced assembly manifest를 registry definition으로 복원할 수 없음 |
| ROS0011 | error | 서로 다른 stable ID가 같은 generated hint name을 사용함 |
| ROS0012 | error | generic containing type의 registry에 automatic registration을 넣을 수 없음 |
| ROS0013 | error | TAttribute가 TypeRegistrationAttribute 계열이 아님 |
| ROS0014 | error | attribute argument/named argument를 C# source로 재현할 수 없음 |
| ROS0015 | error | attribute, constructor, named member가 generated code에서 접근 불가 |
| ROS0016 | warning | abstract candidate를 실제 registration에서 제외함 |
| ROS0017 | error | C# 13 미만에서 partial property 기반 registry를 사용할 수 없음 |
| ROS0018 | warning | TypeRegistryManifestAttribute는 소스 생성기 전용이므로 직접 사용할 수 없음 |
| ROS0019 | warning | TypeRegistrationAttribute에 일치하는 generated registry가 없음 |
| ROS0020 | warning | 열린 generic target에서 useForChildren=false라 닫힌 target을 확인할 수 없음 |
| ROS0021 | warning | target이 제공하는 generic 인자와 generic implementation의 매개 변수 개수가 다름 |
| ROS0022 | warning | target과 generic implementation의 제네릭 제약 조건이 일치하지 않음 |
| ROS0023 | info | non-generic implementation이 유효하게 생성되지만 별도 generic implementation을 고려할 수 있음 |

### 18.1 error와 warning의 의미

현재 compilation에 선언된 registry property가 잘못되면 generated property가 없는 상태로 계속 진행하는 것이 더 위험할 수 있습니다. 그래서 property contract 및 registry type 문제는 error로 설계되어 있습니다.

ROS0009는 registry property 자체가 아니라 자동 등록 대상 어트리뷰트를 진단 위치로 사용합니다. 등록 대상이 실제로 존재할 때만 생성된 lifecycle registration에 필요한 API 누락을 보고합니다.

반면 참조 assembly의 manifest는 consuming compilation이 소유하지 않은 metadata입니다. 하나의 잘못된 외부 manifest 때문에 전체 compilation을 막기보다 ROS0010 warning을 보고 그 manifest만 건너뜁니다.

abstract candidate도 “generator 자체가 파손됨”이 아니라 “등록할 concrete implementation이 아님”에 가깝기 때문에 ROS0016 warning으로 처리합니다.

### 18.2 Execute의 diagnostic dedup

Execute에는 local Report 함수가 있습니다. 대략적인 의도는 다음과 같습니다.

~~~csharp
string key =
    string.Join
    (
        "|",
        diagnostic.Id,
        diagnostic.Location.SourceTree?.FilePath,
        diagnostic.Location.SourceSpan.Start,
        diagnostic.GetMessage()
    );

if (reported.Add(key))
    context.ReportDiagnostic(diagnostic);
~~~

같은 diagnostic이 여러 discovery path를 통해 반복해서 올라오는 것을 막습니다. 특히 다음 상황에서 필요합니다.

- current registry와 manifest registry가 함께 수집됨
- 같은 candidate가 여러 registry definition에 bind됨
- 여러 attribute가 같은 source location을 공유함
- referenced assembly traversal에서 동일 symbol이 반복될 수 있음

### 18.3 generated hint collision

모든 generated source는 hint name을 가지고 AddSource에 들어갑니다. Execute의 AddGeneratedSource는 hint name과 stable ID를 함께 기록합니다.

~~~text
hint name이 처음이면 추가
같은 hint + 같은 stable ID면 중복 output 생략
같은 hint + 다른 stable ID면 ROS0011
~~~

stable ID가 다르면 source가 다른데도 같은 generated file identity를 공유하는 셈입니다. 이 경우 한 output이 다른 output을 덮거나 compiler가 중복 source를 거부할 수 있으므로 error입니다.

## 19. incremental generator로 만든 이유

두 generator 모두 IIncrementalGenerator를 구현합니다. 핵심은 “매번 전체 source tree를 수동으로 처음부터 scan하는 방식” 대신 입력 단계와 출력 단계를 분리하는 것입니다.

### 19.1 입력 provider

공통 generator는 다음 세 입력을 합칩니다.

~~~text
CompilationProvider
  현재 compilation과 모든 semantic symbol

currentRegistries.Collect()
  GenerateTypeRegistry marker가 붙은 현재 source의 property들

candidates.Collect()
  registry 후보가 될 수 있는 class declaration들
~~~

실제 연결은 다음과 같습니다.

~~~csharp
context.CompilationProvider
    .Combine(currentRegistries.Collect())
    .Combine(candidates.Collect())
    .RegisterSourceOutput(input, Execute);
~~~

### 19.2 syntax predicate와 transform 분리

AttributedTypeRegistrySourceGenerator의 candidate provider는 처음부터 모든 class의 semantic analysis를 수행하지 않습니다.

~~~csharp
context.SyntaxProvider.CreateSyntaxProvider
(
    static (node, _) =>
        node is ClassDeclarationSyntax
        or RecordDeclarationSyntax,

    static (context, _) =>
        CreateCandidate(context)
)
~~~

첫 번째 함수는 syntax 모양만 빠르게 필터링합니다. 두 번째 transform에서 declared symbol을 얻고 registration attribute와 base type을 확인합니다.

따라서 class가 많아도 “class/record가 아닌 syntax”는 semantic binding 단계에 들어오지 않습니다.

### 19.3 candidate provider가 넓고 bind가 좁은 이유

candidate provider가 registry마다 다른 semantic 조건을 전부 알면 common generator와 concrete generator 간 결합이 커집니다. 현재 구조는 다음처럼 나눕니다.

~~~text
candidate discovery
  class/record + 관련 attribute가 있는가?

registry binding
  이 candidate가 TBase에 assignable한가?
  attribute가 TAttribute 계열인가?
  attribute literal을 재출력할 수 있는가?
~~~

같은 candidate가 여러 registry definition에 등록될 수 있는 이유도 이 분리와 관련됩니다.

### 19.4 캐시 관점

Roslyn incremental pipeline은 syntax provider 결과와 compilation 결합 결과를 입력 identity에 따라 재사용할 수 있습니다. 이 code가 직접 cache dictionary를 구현하지는 않습니다. 대신 immutable result와 symbol equality를 사용해 incremental engine이 입력 변화를 판단할 수 있게 합니다.

단, Execute 안에서 사용하는 dictionary와 list는 “이번 실행에서 중복과 충돌을 관리하기 위한 local state”이지, 여러 compilation 사이에 유지되는 incremental cache가 아닙니다.

## 20. 전체 구성요소의 관계

다음 표는 파일과 책임을 한 번에 대응시킨 것입니다.

| 구성요소 | 입력 | 출력/책임 |
| --- | --- | --- |
| EmbeddedAttributeSourceGenerator | generator initialization | 생성 구현 타입을 참조 어셈블리 조회에서 숨길 compiler-recognized attribute 선언 |
| GenerateTypeRegistryAttributeSourceGenerator | generator initialization | property marker attribute 선언 |
| TypeRegistryManifestAttributeSourceGenerator | generator initialization | assembly manifest attribute 선언 |
| TypeRegistrySourceGenerator | property/candidate/compilation | discovery, validation, bind orchestration, lifecycle source |
| AttributedTypeRegistrySourceGenerator | class syntax와 attribute | AttributedTypeRegistry용 candidate/bind/registration statements |
| TypeRegistryEmitter | RegistryDefinition | property implementation와 manifest source |
| GeneratorUtils | Roslyn symbols와 문자열 | metadata name, type/literal rendering, declarations |
| TypeRegistrySymbolHelpers | Roslyn symbols | inheritance/accessibility/partial/attribute 검사 |
| AttributeLiteralEmitter | AttributeData | new Attribute(...) { ... } expression |
| SourceWriter | 문자열 line | indentation이 적용된 source text |
| TypeRegistryDiagnostics | diagnostic metadata | ROS0002~ROS0023 descriptor |
| AssetRefSerializationSuppressor | compiler/analyzer diagnostic | UAC1001에 대한 조건부 suppression |
| TypeRegistryManifestAttributeAnalyzer | C# attribute syntax | 직접 사용된 TypeRegistryManifestAttribute에 대한 ROS0018 |
| TypeRegistrationAttributeAnalyzer | C# type symbols | generated registry 부재 ROS0019, TryResolve generic 실패 ROS0020~ROS0022, non-generic 구현 제안 ROS0023 |

여기서 중요한 ownership은 다음과 같습니다.

- runtime registry가 실제 저장/resolve/unregister behavior를 소유
- generator가 compile-time candidate 발견과 generated call을 소유
- emitter가 source text shape를 소유
- helper가 symbol validity를 소유
- analyzer/suppressor가 compile-time 진단 policy를 소유

## 21. 현재 저장소에서 확인되는 통합 상태와 주의점

이 절은 설계 의도가 아니라 현재 checkout의 실제 파일을 기준으로 한 관찰입니다.

### 21.1 별도 analyzer project의 build target

RuniOS.CodeAnalysis.csproj는 다음을 선언합니다.

~~~xml
<TargetFramework>netstandard2.0</TargetFramework>
<LangVersion>latest</LangVersion>
<Nullable>enable</Nullable>
<PackageReference
    Include="Microsoft.CodeAnalysis.CSharp"
    Version="4.3.0"
    PrivateAssets="all" />
~~~

Roslyn analyzer/generator는 host인 IDE, Unity, compiler process에 로드되므로 netstandard2.0을 target으로 선택한 것은 host compatibility를 위한 설정입니다. Roslyn package는 analyzer assembly 안에 private dependency로 publish하려는 목적이 아니라 compile-time API dependency로 사용됩니다.

### 21.2 Unity package copy target

csproj에는 다음 post-build target이 있습니다.

~~~xml
<Target Name="CopyAnalyzerToUnityPackage" AfterTargets="Build">
    <PropertyGroup>
        <UnityAnalyzerDirectory>
            $(MSBuildThisFileDirectory)../Runiverse OS/
            Packages/com.rumi.runios.core/Plugins/RuniOS.Analyzers/
        </UnityAnalyzerDirectory>
    </PropertyGroup>

    <MakeDir Directories="$(UnityAnalyzerDirectory)" />
    <Copy
        SourceFiles="$(TargetPath)"
        DestinationFolder="$(UnityAnalyzerDirectory)"
        SkipUnchangedFiles="true" />
</Target>
~~~

즉 별도 project를 build하면 결과 assembly와 PDB를 Runiverse OS package의 plugin directory로 복사하려는 흐름입니다. 이 project에는 test project나 test source가 보이지 않으므로 현재 repository는 source-level implementation 중심입니다.

### 21.3 artifact 이름과 Unity plugin metadata

현재 별도 project의 AssemblyName을 따로 지정하지 않았으므로 target path의 기본 이름은 project 이름인 RuniOS.CodeAnalysis.dll이 됩니다. 반면 Runiverse OS package directory에는 RuniOS.Analyzers.dll과 RuniOS.CodeAnalysis.dll이 함께 보이며, RuniOS.Analyzers.dll.meta에는 RoslynAnalyzer label이 있습니다.

따라서 다음은 통합 시 확인해야 하는 실제 지점입니다.

- build copy 결과의 실제 filename
- Unity가 RoslynAnalyzer로 인식하는 .meta가 어느 DLL에 붙는지
- RuniOS.Analyzers.dll이 현재 source의 build 산출물인지 이전 artifact인지
- RuniOS.CodeAnalysis.dll을 Unity가 analyzer로 로드하는지

이 문서는 이 부분을 임의로 rename하거나 meta를 수정하지 않습니다. 현재 사용자의 요청은 설명 문서 작성이고, artifact policy 변경은 별도 변경 사항이기 때문입니다.

### 21.4 lifecycle API 이름

현재 generator source의 metadata name은 다음입니다.

~~~text
Unity.Scripting.LifecycleManagement.OnAssemblyLoadedAttribute
Unity.Scripting.LifecycleManagement.OnAssemblyUnloadingAttribute
~~~

그러나 현재 Runiverse OS source 검색에서 확인되는 lifecycle 사용 예는 다음입니다.

~~~text
Unity.Scripting.LifecycleManagement.OnCodeLoaded
Unity.Scripting.LifecycleManagement.OnCodeUnloading
~~~

이것은 단순히 using을 생략한 차이가 아니라 attribute metadata name 자체의 차이입니다. ROS0009는 자동 등록 대상 어트리뷰트를 바인딩할 때 도달 가능한 참조 어셈블리에서 public OnAssemblyLoaded/OnAssemblyUnloading API를 찾지 못하면 해당 어트리뷰트 위치에서 발생하도록 작성되어 있습니다.

따라서 현재 Unity package와 이 generator를 실제로 함께 사용할 때 다음을 확인해야 합니다.

1. 현재 Unity/RuniOS runtime이 두 API를 모두 제공하는가?
2. generator가 오래된 lifecycle contract를 보고 있는가?
3. source의 OnCodeLoaded/OnCodeUnloading 호출부가 다른 시스템의 API인가?
4. generated registration이 실제 Unity assembly lifecycle callback으로 실행되는가?

이는 이 문서가 source를 읽고 도출한 통합 확인 사항이며, 여기서 API 이름을 한쪽으로 임의 변경하지 않습니다.

### 21.5 새 generator와 기존 resolver는 아직 별개다

현재 Runiverse OS에는 기존 RuniOS.TypeHandlerAttribute와 AttributeTypeResolver가 남아 있습니다. 기존 resolver는 ReflectionUtility.types를 스캔하는 global reflection 방식이며, 관련 코드에는 global type discovery가 deprecated라는 안내가 있습니다.

반면 새 generator는 다음 새 contract만 인식합니다.

~~~text
RuniOS.Reflection.TypeRegistrationAttribute
RuniOS.Reflection.AttributedTypeRegistry<,>
RuniOS.Reflection.GenerateTypeRegistry
~~~

현재 Runiverse OS source 검색에서는 GenerateTypeRegistry 사용 예가 발견되지 않았고, 기존 CustomCollectionHandlerAttribute는 old TypeHandlerAttribute 계열입니다. 그러므로 새 generator가 존재한다고 해서 기존 CollectionHandlerBase가 자동으로 compile-time registry로 바뀌는 것은 아닙니다.

### 21.6 generated source는 repository source가 아니다

generator가 AddSource로 만든 output은 일반적으로 원본 repository에 .g.cs 파일로 저장되지 않습니다. compiler/IDE가 compilation 내부에서 다루는 generated syntax tree입니다.

그래서 source directory에서 다음이 안 보인다고 generator가 실행되지 않았다고 단정할 수 없습니다.

~~~text
RuniOS.TypeRegistry.Property....g.cs
RuniOS.TypeRegistry.Manifest....g.cs
RuniOS.AttributedTypeRegistry.Registration....g.cs
~~~

반대로 현재 checkout 검색에서는 별도 repository의 obj/bin과 generated .g.cs가 확인되지 않았습니다. 이 문서 작성은 source inspection을 기준으로 했으며, Unity 안에서 실제 callback까지 실행되는 통합 검증을 의미하지 않습니다.

## 22. 문제를 추적할 때의 권장 읽기 순서

### 22.1 “property가 generated되지 않는다”

다음 순서로 봅니다.

1. owner property에 GenerateTypeRegistry marker가 실제로 붙었는가
2. marker attribute가 compilation에 주입되는가
3. property가 public static partial get-only인가
4. containing type hierarchy가 public partial인가
5. property type이 concrete TypeRegistry-derived type인가
6. parameterless constructor가 접근 가능한가
7. ROS0002~ROS0008 중 어떤 diagnostic이 있는가
8. generated source hint를 AddGeneratedSource가 충돌로 버리지 않았는가

### 22.2 “candidate class가 registry에 안 들어간다”

다음 순서로 봅니다.

1. class 또는 record declaration인가
2. TypeRegistrationAttribute 계열 attribute가 붙었는가
3. TAttribute가 그 attribute base와 compatible한가
4. candidate type이 TBase에 assignable한가
5. candidate가 abstract인가
6. attribute constructor/named property가 generated code에서 accessible한가
7. attribute argument가 supported TypedConstantKind인가
8. ROS0013~ROS0016가 있는가
9. owner가 generic containing type이라 ROS0012로 skip되지 않았는가

### 22.3 “다른 assembly의 candidate가 안 보인다”

candidate class 자체를 manifest에 넣는 구조가 아니라는 점이 중요합니다. consuming compilation에서 candidate는 해당 compilation의 source/metadata를 대상으로 provider가 수집합니다. 다른 assembly의 registry owner를 연결하려면 owner assembly가 manifest를 내보내야 합니다.

확인 순서는 다음입니다.

1. owner assembly에 TypeRegistryManifestAttribute가 실제 metadata로 들어갔는가
2. consuming compilation이 owner assembly를 reference하는가
3. EnumerateReferencedAssemblies가 해당 assembly를 도달하는가
4. manifest constructor argument의 owner type이 복원되는가
5. propertyName으로 public registry property를 찾는가
6. ROS0010 warning이 있는가
7. restored registry property가 public이라 generated code에서 접근 가능한가

### 22.4 “UAC1001이 왜 어떤 field만 사라지는가”

AssetRef suppressor는 다음 조건을 모두 만족해야 합니다.

~~~text
diagnostic ID == UAC1001
diagnostic location이 source 안에 있음
location에서 field declaration을 찾음
field symbol을 얻음
field type의 OriginalDefinition == RuniOS.Resource.AssetRef<>
~~~

하나라도 다르면 suppression하지 않습니다. 특히 AssetRef가 아닌 Unity field의 UAC1001까지 숨기지 않는 것이 의도입니다.

## 23. Roslyn 용어를 이 project의 코드에 대응시키기

| Roslyn 용어 | 이 project에서의 의미 |
| --- | --- |
| syntax node | ClassDeclarationSyntax, PropertyDeclarationSyntax 같은 source 모양 |
| syntax tree | 한 .cs 파일의 parsing 결과 |
| symbol | class/property/type/attribute의 semantic identity |
| compilation | 현재 project 전체의 source + references + options |
| semantic model | syntax node를 symbol/type으로 해석하는 API |
| AttributeData | attribute 적용 결과의 compile-time metadata view |
| incremental provider | 입력을 단계별 값으로 만들고 변경분을 재사용하는 pipeline node |
| source generator | compilation에 새 source tree를 추가하는 compile-time component |
| analyzer | compilation을 검사하고 diagnostic을 보고하는 component |
| suppressor | 기존 diagnostic에 조건부 suppression을 붙이는 component |
| generated source | AddSource로 compilation에 추가된 가상 .cs 파일 |
| hint name | generated source를 식별하는 논리적 이름 |
| manifest | 다른 assembly의 generated registry를 찾기 위한 metadata index |

가장 중요한 구분은 syntax와 symbol입니다.

~~~text
syntax:  “이 파일에 public class Foo라고 적혀 있다”
symbol:  “Foo는 어떤 namespace/assembly의 어떤 type이며, Base를 상속하고 있다”
~~~

registry binding과 accessibility 검사는 syntax만으로는 신뢰할 수 없으므로 symbol이 필요합니다.

## 24. 최종 mental model

이 project를 한 문장으로 줄이면 다음과 같습니다.

~~~text
Roslyn이 compilation을 syntax와 symbol로 분석하고,
RuniOS.CodeAnalysis가 registry 선언과 attributed class를 검증한 뒤,
runtime reflection 대신 호출할 수 있는 C# registration source를 만들어
같은 compilation에 다시 넣는 구조다.
~~~

조금 더 구체적으로 나누면:

1. marker generator가 registry 선언용 attribute type을 제공한다.
2. common generator가 marker property를 발견하고 registry definition으로 정규화한다.
3. concrete generator가 candidate class와 registration attribute를 찾아 bind한다.
4. emitter/helper가 접근성, generic, partial, literal, naming 문제를 해결한다.
5. generated property가 registry instance를 만든다.
6. manifest가 assembly 경계를 넘어 registry owner를 발견할 수 있게 한다.
7. generated lifecycle method가 DirectRegisterRange와 Unregister를 호출한다.
8. runtime registry가 priority/type matching/cache를 사용해 Resolve한다.
9. 별도 suppressor가 AssetRef에 대해 특정 Unity diagnostic만 숨긴다.

따라서 이 repository는 단순한 “attribute를 읽어주는 reflection helper”가 아닙니다. 핵심은 다음 두 compile-time 변환입니다.

~~~text
partial registry property
  -> 실제 registry singleton-like property + manifest

attribute가 붙은 implementation class
  -> DirectRegisterRange에 들어가는 typed RegistrationEntry
~~~

그리고 현재 checkout 기준으로는 이 새 변환 계층과 기존 global reflection resolver, Unity package artifact/lifecycle contract가 완전히 하나로 합쳐졌다고 단정할 수 없습니다. 그 경계가 실제 통합 검증에서 가장 먼저 확인해야 할 부분입니다.
