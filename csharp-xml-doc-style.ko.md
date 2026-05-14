---
name: csharp-xml-doc-style
description: 사용자가 직접 만들거나 유지보수하는 C# 프로젝트의 XML 문서 주석을 작성, 추가, 수정, 정규화할 때 사용합니다. 영어/한국어 병기, 기능 중심 설명, public/protected 우선 문서화, cref, paramref, langword, returns, exception, async 반환값, nullable 참조 설정 확인, 줄바꿈, 최소 변경 규칙을 적용합니다.
metadata:
  short-description: 사용자 C# XML 문서 주석 스타일
---

# C# XML 문서 주석 스타일

사용자가 직접 만들거나 유지보수하는 C# 프로젝트의 XML 문서 주석을 작성, 추가, 수정, 정규화, 리뷰해달라고 요청할 때 이 스킬을 사용합니다.

## 적용 범위

- 이 규칙은 XML 문서 주석에만 적용합니다.
- 문서화를 맞추기 위해 구현 동작을 변경하지 않습니다.
- 문서화 작업 중 실제 구현 문제가 발견되면, 동작을 바꾸기 전에 별도로 알립니다.
- 문서화 대상 API가 Unity 관련 동작에 직접 의존하지 않는 한 Unity를 강조하지 않습니다.
- 코드가 어떤 엔진이나 프레임워크에 속하는지가 아니라, 코드가 무엇을 하는지 설명합니다.

## 최소 변경 규칙

- 기존 XML 주석이 이미 이 스타일에 맞으면 그대로 둡니다.
- 기존 XML 주석이 이 스타일에 맞지 않으면, 스타일에 맞추는 데 필요한 부분만 수정합니다.
- 올바른 주석을 단순히 표현만 조금 바꾸기 위해 다시 쓰지 않습니다.
- 이미 명확한 도메인 설명은 보존합니다.
- 기존 주석을 수정할 때는, 코드상 기존 설명이 틀렸다는 근거가 없는 한 문서화된 계약을 바꾸지 않습니다.

## 문서화 대상

다른 개발자가 실제로 사용할 수 있는 API를 우선적으로 XML 문서화합니다.

- public 타입.
- public 타입 안에 선언된 public 멤버.
- public 타입 안에 선언되어 있고, 어셈블리 외부의 파생 타입이 현실적으로 사용할 수 있는 protected 또는 protected internal 멤버.
- non-public 타입 안에 중첩된 public 멤버는 실제로 외부에서 접근할 수 없으므로 우선순위에 두지 않습니다.
- private, internal, file-local 및 기타 non-public 구현 세부사항에는 보통 XML 문서 주석을 작성하지 않습니다.
- 단, 복잡한 정적 생성자, 리플렉션 등록 파이프라인, 캐시 무효화 루틴, 직관적이지 않은 정렬 알고리즘처럼 일반 인라인 주석만으로는 부족한 경우 private/internal 멤버에도 XML 문서 주석을 작성할 수 있습니다.

## 불명확할 때 질문하기

- 멤버가 무엇을 하는지 약 95% 이상 확신할 수 없다면, XML 문서 주석을 작성하기 전에 사용자에게 해당 API의 의도를 물어봅니다.
- 계약, 보장 사항, 스레딩 동작, 소유권 규칙, 지속성 동작, 예외 조건을 추측해서 만들지 않습니다.
- 코드 동작은 이해되지만 의도가 불명확하다면, 관찰 가능한 동작만 문서화하고 더 강한 표현을 추가하기 전에 의도를 질문합니다.

## 언어와 말투

- XML 문서 주석은 기본적으로 영어와 한국어를 병기합니다. 영어를 먼저 쓰고, 한국어를 뒤에 씁니다.
- 두 언어 모두 간결하고 기술적이며 자연스럽게 작성합니다.
- 읽는 사람이 숙련된 프로그래머라고 가정합니다.
- API 계약, 엣지 케이스, 부작용, 실패 조건을 명확히 설명합니다.
- 한국어 문장은 `반환합니다`, `발생합니다`, `가져옵니다`, `설정합니다`, `확인합니다`, `등록합니다`, `제거합니다` 같은 정확한 어미를 선호합니다.
- 마케팅식 표현, 모호한 칭찬, 불필요한 배경 설명을 피합니다.

## 한영 병기 레이아웃

각 XML 문서 블록은 영어를 먼저 쓰고 한국어를 뒤에 씁니다.

영어 한 문장과 한국어 한 문장으로 된 내용은 영어 줄 끝에 `<br/>`를 붙이고, 다음 XML 주석 줄에 한국어를 작성합니다. 단독 `<br/>` 하나만 `///` 줄에 두지 않습니다.

```xml
/// <summary>
/// Gets a value indicating whether the provider owns an independent file-system structure.<br/>
/// 프로바이더가 독립적인 파일 시스템 구조를 가지는지 여부를 나타내는 값을 가져옵니다.
/// </summary>
```

어느 한쪽 언어라도 두 줄 이상이 되는 내용은 영어 블록 전체를 먼저 작성합니다. 같은 영어 블록 안에서 시각적으로 다음 줄로 이어져야 하는 줄 끝에는 `<br/>`를 붙이고, 영어 블록과 한국어 블록을 구분할 때는 `<br/><br/>`를 단독 줄에 둡니다. 한국어 블록 안에서도 같은 줄 끝 `<br/>` 규칙을 적용합니다.

```xml
/// <summary>
/// This abstract class serves as a registry and resolver for types that implement specific attribute-based logic.<br/>
/// It automatically discovers classes derived from <see cref="TBase"/> that are decorated with <see cref="TAttribute"/> and manages them for lookup.
/// <br/><br/>
/// 이 추상 클래스는 특정 특성 기반 로직을 구현하는 타입들을 위한 레지스트리 및 리졸버 역할을 합니다.<br/>
/// <see cref="TAttribute"/>가 지정된 <see cref="TBase"/>의 파생 클래스를 자동으로 발견하고 조회할 수 있도록 관리합니다.
/// </summary>
```

`<param>`, `<typeparam>`, `<returns>`, `<remarks>`, `<exception>`에도 기본적으로 같은 영어 먼저 / 한국어 나중 구조를 적용합니다. 기존의 허용 가능한 주석을 보존하는 경우나 사용자가 명시적으로 다른 언어/스타일을 요청한 경우에만 한영 병기를 생략합니다.

## 일반 스타일

- 프레임워크 중심 설명보다 기능 중심 설명을 선호합니다.
- 관련이 있다면 입력, 출력, 상태 변경, 부작용, 유효 조건, 생명주기 제약을 설명합니다.
- `<summary>`는 짧지만 구체적으로 작성합니다.
- `<summary>`에 넣기에는 긴 유용한 세부사항은 `<remarks>`를 사용합니다.
- 한국어 용어는 `메서드`, `속성`, `필드`, `인스턴스`, `컬렉션`, `식별자`, `핸들`, `리소스`처럼 일관되게 사용합니다.
- 단순히 멤버 이름을 반복하는 주석은 작성하지 않습니다.

## XML 태그

적절한 XML 문서 태그를 사용합니다.

- `<summary>`: 멤버가 무엇을 하는지 설명합니다.
- `<remarks>`: 추가 동작, 알고리즘 설명, 제약, 순서, 생명주기, 예제를 설명합니다.
- `<param name="...">`: 각 매개변수의 의미를 설명합니다.
- `<typeparam name="...">`: 각 제네릭 타입 매개변수의 의미나 제약을 설명합니다.
- `<returns>`: 호출자가 실제로 관찰하는 결과를 설명합니다.
- `<exception cref="...">`: 메서드에서 합리적으로 발생할 수 있는 모든 예외를 설명합니다.
- `<inheritdoc/>`: 상속 문서가 완전히 정확할 때만 사용합니다.
- `<see cref="..."/>`: 타입, 멤버, 상수, 속성, 메서드를 참조합니다.
- `<paramref name="..."/>`: 매개변수를 참조합니다.
- `<typeparamref name="..."/>`: 제네릭 타입 매개변수를 참조합니다.
- `<see langword="..."/>`: C# 언어 키워드를 참조합니다.
- `<c>...</c>`: 인라인 코드 표현식이나 리터럴 예제를 표시합니다.
- `<b>...</b>`: 명확성에 도움이 될 때 XML 문서 안에서 중요한 부분을 강조합니다.
- `<para>...</para>`: remarks나 긴 설명 안에서 문단을 표현합니다.
- `<list type="bullet">`: 여러 케이스, 규칙, 조건을 나열합니다.

## 키워드 포맷

XML 문서 안에서는 C# 언어 키워드를 `<see langword="..."/>`로 감쌉니다.

예시:

```xml
<see langword="true"/>
<see langword="false"/>
<see langword="null"/>
<see langword="default"/>
```

`<c>` 코드 예제 안에 있는 경우가 아니라면, 본문에 raw `true`, `false`, `null`을 그대로 쓰지 않습니다.

## 줄바꿈

XML 문서 주석의 줄바꿈에는 `<br/>`를 사용하며, 줄바꿈되어야 하는 텍스트 줄의 끝에 붙입니다. 단독 `<br/>` 하나만 `///` 줄에 두지 않습니다.

인접한 텍스트 줄 사이의 단일 줄바꿈:

```xml
/// First line.<br/>
/// Second line.
```

영어/한국어 블록, 문단, 섹션 사이의 큰 구분에는 `<br/><br/>`를 단독 `///` 줄에 둡니다.

```xml
/// First paragraph.
/// <br/><br/>
/// Second paragraph.
```

영어와 한국어 블록을 포함하는 여러 줄 내용:

```xml
/// First English line.<br/>
/// Second English line.
/// <br/><br/>
/// 첫 번째 한국어 줄입니다.<br/>
/// 두 번째 한국어 줄입니다.
```

논리적 문단이 여러 개인 매우 긴 내용은 같은 문단 안의 줄 끝에는 `<br/>`를 붙이고, 문단 또는 큰 의미 묶음 사이에는 `<br/><br/>`를 단독 XML 주석 줄에 둡니다. 이 패턴은 언어 경계 전후에도 필요에 따라 적용합니다.

```xml
/// First English paragraph, first line.<br/>
/// First English paragraph, second line.
/// <br/><br/>
/// Second English paragraph, first line.<br/>
/// Second English paragraph, second line.
/// <br/><br/>
/// 첫 번째 한국어 문단의 첫 줄입니다.<br/>
/// 첫 번째 한국어 문단의 두 번째 줄입니다.
/// <br/><br/>
/// 두 번째 한국어 문단의 첫 줄입니다.<br/>
/// 두 번째 한국어 문단의 두 번째 줄입니다.
```

`</br>`처럼 잘못된 줄바꿈 태그는 사용하지 않습니다.

## 한 줄 태그와 여러 줄 태그

짧은 태그는 의도적으로 한영 병기가 아니고, 시각적 줄바꿈이 필요 없으며, 목록이나 여러 문장을 포함하지 않을 때만 한 줄로 유지합니다. 새로 작성하는 한영 병기 파라미터/타입 파라미터 문서는 보통 여러 줄 태그를 사용해야 합니다.

태그 안에 한영 병기 내용, `<br/>`, `<br/><br/>`, 목록, 또는 한 줄로 읽기 어려운 여러 문장이 포함되면 여러 줄 태그로 펼칩니다. 여러 줄 태그 안에서는 줄바꿈되어야 하는 텍스트 줄 끝에 `<br/>`를 둡니다.

```xml
/// <param name="identifier">
/// The identifier to resolve.<br/>
/// 확인할 식별자입니다.
/// </param>
```

한영 병기 또는 여러 줄 파라미터 문서를 인라인으로 작성하지 않습니다. 태그에 시각적 줄바꿈이 필요해지는 순간, 태그 본문을 여러 XML 주석 줄로 펼칩니다.

한영 병기된 `<summary>`, `<remarks>`, `<returns>`, `<exception>` 및 단순하지 않은 `<param>`, `<typeparam>`은 여러 줄 태그를 선호합니다.

## 비동기 반환 규칙

`UniTask<T>`, `Task<T>`, `ValueTask<T>` 또는 비슷한 awaitable 제네릭 타입을 반환하는 메서드는 래퍼 타입이 아니라 await 후의 실제 결과를 문서화합니다.

예시:

```csharp
/// <returns>
/// When the asynchronous operation completes, returns the loaded <see cref="Texture2D"/>.<br/>
/// 비동기 작업이 완료되면 로드된 <see cref="Texture2D"/>를 반환합니다.
/// </returns>
UniTask<Texture2D> LoadTextureAsync();
```

메서드가 awaitable 작업이 아니라 task 객체 자체를 데이터로 반환하는 경우가 아니라면 `UniTask<Texture2D>`를 반환한다고 쓰지 않습니다.

`UniTask`, `Task`, `ValueTask`처럼 제네릭이 아닌 비동기 메서드는 완료를 설명합니다.

```xml
/// <returns>
/// An asynchronous operation that represents completion of the work.<br/>
/// 작업 완료를 나타내는 비동기 작업입니다.
/// </returns>
```

## 예외

명시적 throw, 검증 분기, 호출자에게 의미 있는 영향을 주는 위임 API, 문서화된 계약 위반에서 합리적으로 발생할 수 있는 모든 예외를 문서화합니다.

- 가능한 가장 구체적인 예외 타입을 사용합니다.
- 예외가 가능하지만 구현 세부사항에 의존한다면 `may be thrown` / `발생할 수 있습니다`처럼 표현합니다.
- 가능하면 관련 매개변수, 상태, 조건을 `<paramref/>`, `<see cref/>`, `<see langword/>`로 언급합니다.

예시:

```xml
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="identifier"/> is <see langword="null"/>.<br/>
/// <paramref name="identifier"/>가 <see langword="null"/>인 경우 발생합니다.
/// </exception>
/// <exception cref="InvalidOperationException">
/// Thrown when the method is called before tracking has started.<br/>
/// 트래킹이 시작되지 않은 상태에서 호출한 경우 발생합니다.
/// </exception>
```

## Nullable 참조 타입

nullability에 민감한 API를 문서화하기 전에는, 관련 프로젝트 또는 어셈블리에서 Nullable 참조 타입이 활성화되어 있는지 확인합니다.

- SDK 스타일 C# 프로젝트에서는 `.csproj`, `Directory.Build.props` 또는 관련 MSBuild 파일에서 `<Nullable>enable</Nullable>`, `<Nullable>annotations</Nullable>` 또는 동등한 설정을 확인합니다.
- Unity 프로젝트라고 해서 Nullable이 꺼져 있다고 단정하지 않습니다. 문서화 대상 어셈블리에 적용되는 `csc.rsp` 같은 컴파일러 응답 파일을 확인하고, `-nullable:enable`, `-nullable:annotations`, `-nullable+` 또는 이에 준하는 활성화/annotation 모드 플래그를 컴파일러상 기준으로 봅니다.
- 문서화 대상 어셈블리에서 컴파일러 수준으로 Nullable이 켜져 있다면, Unity 프로젝트여도 Nullable 참조 타입을 존중합니다.
- 코드 수정이 작업 범위에 포함되어 있다면 시그니처와 문서에서 nullable 동작을 정확히 반영합니다.
- 매개변수, 반환값, 필드, 속성이 null일 수 있고 nullable annotation이 활성화되어 있다면 C# 타입에 `?`를 사용해야 합니다.
- XML 문서 안에서는 null 동작을 `<see langword="null"/>`로 언급합니다.
- 시그니처나 구현이 null을 반환할 수 있는데 non-null 결과처럼 암시하지 않습니다.
- nullable 설정을 확인할 수 없고 시그니처 변경이 명시적으로 요청되지 않았다면, 문서화 작업만을 위해 nullability annotation을 바꾸지 않습니다. 관찰 가능한 null 동작을 문서화하고, 필요하다면 불확실성을 언급합니다.

## Summary 패턴

좋은 예:

```xml
/// <summary>
/// Determines whether the specified <paramref name="identifier"/> is currently being tracked.<br/>
/// 지정된 <paramref name="identifier"/>가 현재 트래킹되고 있는지 여부를 확인합니다.
/// </summary>
```

나쁜 예:

```xml
/// <summary>
/// IsTracking method.<br/>
/// IsTracking 메서드입니다.
/// </summary>
```

## Returns 패턴

bool 반환값은 이 형태를 선호합니다.

```xml
/// <returns>
/// <see langword="true"/> if the condition is met; otherwise, <see langword="false"/>.<br/>
/// 조건을 만족하면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
/// </returns>
```

nullable 반환값:

```xml
/// <returns>
/// The matching instance if found; otherwise, <see langword="null"/>.<br/>
/// 값을 찾은 경우 해당 인스턴스를 반환하고, 그렇지 않으면 <see langword="null"/>을 반환합니다.
/// </returns>
```

컬렉션 반환값:

```xml
/// <returns>
/// A collection containing the elements that match the condition.<br/>
/// 조건에 맞는 요소를 포함하는 컬렉션을 반환합니다.
/// </returns>
```

## Remarks 패턴

동작을 `<summary>`보다 더 자세히 설명해야 할 때 `<remarks>`를 사용합니다. 헷갈리기 쉬운 계약, 독립적/외부 변경 가능 상태, 프로바이더 동작, 리플렉션 탐색, 캐싱, 정렬, 생명주기, 소유권 규칙에는 특히 선호합니다.

```xml
/// <remarks>
/// <para>This property depends on the concrete <see cref="IIOProvider"/> implementation.</para>
/// <list type="bullet">
/// <item><description>
///   Returns <see langword="true"/> when the provider refers to a structure that is controlled by the provider or developer, such as an asset bundle, compressed archive, or virtual directory.
/// </description></item>
/// <item><description>
///   Returns <see langword="false"/> when the provider refers to a normal file-system path that can be changed by the OS or other programs.
/// </description></item>
/// </list>
/// <br/><br/>
/// <para>이 속성은 <see cref="IIOProvider"/>의 구체적인 구현에 따라 다르게 동작합니다.</para>
/// <list type="bullet">
/// <item><description>
///   프로바이더 또는 개발자가 제어하는 에셋 번들, 압축 파일, 가상 디렉터리 같은 구조를 참조하는 경우 <see langword="true"/>를 반환합니다.
/// </description></item>
/// <item><description>
///   OS나 다른 프로그램이 변경할 수 있는 일반 파일 시스템 경로를 참조하는 경우 <see langword="false"/>를 반환합니다.
/// </description></item>
/// </list>
/// </remarks>
```

## 복잡한 Non-Public 예시

non-public 구현 멤버는 비정상적으로 복잡하고 올바르게 유지보수하는 것이 중요할 때만 문서화합니다.

```csharp
/// <summary>
/// Initializes the static members of the <see cref="AttributeTypeResolver{TBase,TAttribute}"/> class.<br/>
/// This process subscribes to <see cref="ReflectionUtility.onListUpdate"/> and immediately performs the initial discovery of handler types.
/// <br/><br/>
/// <see cref="AttributeTypeResolver{TBase,TAttribute}"/> 클래스의 정적 멤버를 초기화합니다.<br/>
/// 이 과정은 <see cref="ReflectionUtility.onListUpdate"/>에 구독하고 핸들러 타입의 초기 발견을 즉시 수행합니다.
/// </summary>
static AttributeTypeResolver()
{
}
```

## 전체 예시

```csharp
/// <summary>
/// Asynchronously loads the resource associated with the specified <paramref name="identifier"/>.<br/>
/// 지정된 <paramref name="identifier"/>에 해당하는 리소스를 비동기로 로드합니다.
/// </summary>
/// <param name="identifier">
/// The value that identifies the resource to load.<br/>
/// 로드할 리소스를 식별하는 값입니다.
/// </param>
/// <param name="cancellationToken">
/// The cancellation token used to cancel the load operation.<br/>
/// 로드 작업을 취소하는 데 사용되는 취소 토큰입니다.
/// </param>
/// <returns>
/// When the asynchronous operation completes, returns the loaded <see cref="Texture2D"/>.<br/>
/// 비동기 작업이 완료되면 로드된 <see cref="Texture2D"/>를 반환합니다.
/// </returns>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="identifier"/> is <see langword="null"/>.<br/>
/// <paramref name="identifier"/>가 <see langword="null"/>인 경우 발생합니다.
/// </exception>
/// <exception cref="OperationCanceledException">
/// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.<br/>
/// <paramref name="cancellationToken"/>을 통해 작업이 취소된 경우 발생합니다.
/// </exception>
public UniTask<Texture2D> LoadAsync(Identifier identifier, CancellationToken cancellationToken);
```
