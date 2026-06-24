# 텍스트 시스템 개요

Language available: \[[**한국어 (대한민국)**](README.md)\] \[[English (US)](README-EN.md)\]  

## 개요

이 프로젝트의 텍스트 시스템은 `string`을 바로 만들지 않습니다.\
먼저 구조화된 `Text` 트리를 만들고, 렌더링 시점에 Unity/TMP용 rich text 문자열로 변환합니다.

```text
Text tree
-> RichTextBuilder
-> TMP rich text string
```

단순 출력만 필요하다면 rich text 문자열을 직접 쓰는 쪽이 보통 훨씬 빠르고 단순합니다.

```csharp
$"<color=yellow>HP: {hp:0.##}</color>"
```

`Text` 시스템의 목적은 순수 문자열 생성 속도가 아닙니다.\
핵심 장점은 텍스트를 구조화된 데이터로 유지하고, API 매개변수로 넘기거나, 조합하거나, 로컬라이징하거나, 스타일을 붙인 뒤 나중에 렌더링할 수 있다는 점입니다.

## Text Tree를 쓰는 이유

`string`은 이미 완성된 결과입니다.\
한번 문자열로 합쳐지면 어느 부분이 값이었는지, 어느 부분에 스타일이 있었는지, 어떤 값에 포맷이 있었는지 알기 어렵습니다.

`Text`는 이 정보를 렌더링 전까지 보존합니다.

```text
GroupText
 |- LiteralText("HP: ")
 `- LiteralText(100, format: "0.##").Yellow()
```

즉 텍스트를 단순 출력 문자가 아니라 의미 있는 데이터 조각으로 다룰 수 있습니다.

## 핵심 타입

`Text`는 모든 텍스트 요소의 기본 타입입니다.\
스타일도 `Text`에 붙습니다.

```csharp
Text.Literal("Warning").Bold().Red();
```

`LiteralText`는 실제 값을 저장합니다.\
문자열뿐 아니라 숫자, 날짜, 포맷, 정렬도 저장할 수 있습니다.

```csharp
Text.Literal(123, 5, "000");
```

`GroupText`는 여러 `Text` 요소를 순서대로 저장합니다.

```csharp
GroupText text = $"HP: {100:0.##}";
```

`LocalizationText`는 로컬라이징 키와 `Text` 인자를 저장합니다.\
`{0}`, `{1}` 같은 포맷 위치에도 스타일이 적용된 텍스트를 넣을 수 있습니다.

## 재사용 Factory 가정

`Text.Literal`, `Text.Local`, `Text.Group` 같은 정적 factory 메소드는 내부 인스턴스를 재사용한다고 가정합니다.

일반 사용 코드는 텍스트 객체를 직접 생성하기보다 factory 메소드를 쓰는 것을 권장합니다.

```csharp
Text text = Text.Literal("HP");
```

이 방식은 같은 종류의 텍스트를 자주 만들 때 객체 할당을 줄입니다.

## API 데이터로서의 구조화된 텍스트

`Text`는 아직 렌더링된 문자열이 아니므로 매개변수로 넘길 수 있습니다.

```csharp
void SetTitle(Text title)
{
    string richText = RichTextBuilder.Build(title);
}
```

호출자는 일반 텍스트, 스타일 텍스트, 로컬라이징 텍스트, 그룹 텍스트를 모두 같은 API로 넘길 수 있습니다.

```csharp
SetTitle(Text.Literal("Loading").Yellow());
SetTitle(Text.Local(new Identifier("example", "ui.loading.title")));
SetTitle(Text.Group($"HP: {hp:0.##}"));
```

받는 쪽은 이 텍스트가 리터럴인지, 로컬라이징인지, 조합된 그룹인지 몰라도 됩니다.\
필요한 시점에 `Text`를 렌더링하면 됩니다.

## 수정 가능한 Text 인스턴스

`Text`는 단순히 한 번 만들고 끝나는 값 객체가 아닙니다.\
값을 수정 가능한 인스턴스입니다.

예를 들어 어떤 시스템에 `Text`를 넘겨주고, 그 시스템은 그 `Text`를 보관한 뒤 렌더링만 한다고 가정할 수 있습니다.

```csharp
LiteralText progressText = new LiteralText(0, "0.##");
Text description = Text.Group($"Progress: {progressText}%");

SetDescription(description);
```

이후 `Text`를 받은 쪽을 직접 건드리지 않아도, 밖에서 같은 `Text` 인스턴스의 값을 바꿀 수 있습니다.

```csharp
progressText.value = 50;
```

렌더링하는 쪽은 여전히 같은 `description`을 렌더링할 뿐이지만, 결과는 바뀐 값으로 표시됩니다.

```text
Progress: 50%
```

이 구조는 진행률, 현재 처리 중인 파일 경로, 상태 메시지처럼 실시간으로 값만 바뀌는 텍스트에서 특히 유용합니다.\
텍스트 구조는 그대로 유지하고, 내부 값만 갱신할 수 있기 때문입니다.

## 보간 문자열

`GroupTextStringHandler`를 통해 C# 보간 문자열로 `GroupText`를 만들 수 있습니다.

```csharp
Text name = Text.Literal("Rumi").Bold();
GroupText text = $"Player {name}: {100:000}";
```

구조는 대략 다음과 같습니다.

```text
GroupText
 |- LiteralText("Player ")
 |- Text.Literal("Rumi").Bold()
 |- LiteralText(": ")
 `- LiteralText(100, format: "000")
```

보간 값이 이미 `Text`라면 그대로 삽입되고 스타일도 유지됩니다.\
일반 값은 `LiteralText`가 되며, 포맷과 정렬 정보도 보존됩니다.

## 렌더링 흐름

렌더링은 `RichTextBuilder.Build(text)`에서 시작합니다.

```csharp
string richText = RichTextBuilder.Build(text);
```

빌더는 `Text`의 런타임 타입을 보고 렌더러를 찾습니다.

```text
LiteralText      -> LiteralRichTextBuilder
GroupText        -> GroupRichTextBuilder
LocalizationText -> LocalizationRichTextBuilder
```

`GroupText`는 자식들을 순서대로 렌더링합니다.\
`LocalizationText`는 로컬라이징 포맷을 해석하고, 포맷 위치에 `Text` 인자를 렌더링합니다.

## 확장성

텍스트 시스템은 특정 텍스트 타입만 하드코딩해서 처리하지 않습니다.\
새로운 `Text` 타입과 그 타입을 처리하는 빌더를 직접 추가할 수 있습니다.

빌더는 `CustomTextRendererAttribute`로 자신이 처리할 `Text` 타입을 표시합니다.

```csharp
[CustomTextRenderer(typeof(MyText))]
public sealed class MyRichTextBuilder : RichTextBuilder
{
    protected override void AppendCore(StringBuilder builder, Text text, TextStyleState styleState)
    {
        MyText myText = (MyText)text;
        // render myText
    }
}
```

`RichTextBuilder`는 런타임 타입을 기준으로 등록된 빌더를 찾아 사용합니다.\
따라서 텍스트 구조는 필요에 따라 직접 확장할 수 있습니다.

단, TMP rich text 태그 출력은 예외입니다.\
TMP가 지원하는 태그가 정해져 있고, 이 부분은 렌더링 성능을 위해 `RichTextUtility`에 하드코딩되어 있습니다.

## 스타일 스택

스타일은 중첩될 수 있습니다.

```text
Red
 `- Bold
    `- "Hello"
```

렌더링 중에는 `TextStyleState`가 현재 스타일 스택을 추적합니다.

```text
Open Red
  Open Bold
    Append "Hello"
  Close Bold
Close Red
```

결과는 TMP rich text입니다.

```text
<color=red><b>Hello</b></color>
```

자식 렌더링이 끝나면 반드시 부모 스타일 상태로 돌아와야 합니다.\
이 복구가 틀리면 뒤에 오는 텍스트로 스타일이 새어 나갈 수 있습니다.

## GC와 성능

직접 rich text 문자열을 만드는 방식이 보통 더 빠릅니다.

```csharp
$"<color=yellow>HP: {hp:0.##}</color>"
```

`Text` 시스템은 추가 작업을 합니다.

```text
Text 구조 생성 또는 재사용
스타일 상태 추적
rich text 문자열 생성
TMP 레이아웃 갱신
```

재사용 factory 메소드는 `Text` 객체 생성으로 인한 GC를 줄입니다.\
렌더링 쪽도 `StringBuilderCache`, `TextStyleStateCache`, builder cache를 통해 임시 할당을 줄입니다.

하지만 값이 바뀌면 TMP에 넘길 최종 rich text `string`은 여전히 만들어져야 합니다.\
표시 문자열이 바뀌면 TMP 레이아웃 비용도 발생할 수 있습니다.

자주 갱신되는 UI는 값이 바뀔 때만 빌드하는 것이 좋습니다.

```csharp
if (hp != previousHp)
{
    previousHp = hp;
    label.text = RichTextBuilder.Build(Text.Group($"HP: {hp:0.##}"));
}
```

## 요약

빠른 출력만 필요하다면 직접 rich text 문자열을 쓰는 편이 좋습니다.\
`Text` 시스템은 구조화를 위해 존재합니다.

`Text`는 값, 스타일, 로컬라이징, 포맷, 그룹 정보를 렌더링 전까지 보존합니다.\
그래서 텍스트를 매개변수로 넘기고, 조합하고, 재사용하고, 나중에 렌더링하기 쉽습니다.
