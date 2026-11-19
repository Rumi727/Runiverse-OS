# 6. 직렬화 구조체와 JSON 변환기

> 기준 커밋: `571e2110`. [← 목차](README.md) · 이전: [5. 진단](05-diagnostics.md) · 다음: [7. 함수 모음](07-function-collections.md)
>
> 함께 보기: [Resource 10.3절 JSON 컨버터](../Resource/10-languages-and-json.md#103-json-컨버터)(`Identifier`·`PackIdentifier`)

Unity 직렬화가 다루지 못하는 값(`Nullable<T>`, `Type`)을 담는 구조체와, 문자열 하나로 저장되는 값 구조체(`Version`, `HexColor`)를 다룹니다. 대부분 `[Serializable]`이면서 Newtonsoft.Json `[JsonConverter]`도 붙어 있어, **Unity 에셋과 JSON 파일에 같은 값을 쓸 수 있습니다.**

- [6.1 `SerializableNullable<T>`](#61-serializablenullablet)
- [6.2 `SerializableType`](#62-serializabletype)
- [6.3 `Version`과 `VersionRange`](#63-version과-versionrange)
- [6.4 `HexColor`](#64-hexcolor)
- [6.5 그 밖의 구조체](#65-그-밖의-구조체)
- [6.6 JSON 변환기 목록](#66-json-변환기-목록)

## 6.1 `SerializableNullable<T>`

Unity는 `int?` 같은 `Nullable<T>` 필드를 직렬화하지 않습니다. [`SerializableNullable<T>`](../../Runtime/Structs/SerializableNullable%601.cs#L20)는 .NET `Nullable<T>`의 소스를 옮겨 와, 값과 "값이 있는가"를 **Unity가 직렬화할 수 있는 필드 두 개**로 둡니다.

```csharp
// Runtime/Structs/SerializableNullable`1.cs
20  public struct SerializableNullable<T> : ISerializableNullable<T>, IEquatable<T>, IEquatable<T?>, IEquatable<SerializableNullable<T>>, ISerializationCallbackReceiver where T : struct
    {
        // 필드랑 프로퍼티 이름 바꾸지 마세요.
        // 직렬화에 사용합니다.
25      [SerializeField, JsonIgnore] T value;
26      [SerializeField, JsonIgnore] bool hasValue;
…
97      void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
99          if (!hasValue)
100             value = default;                                   // ① 값이 없으면 저장되는 값도 default
        }
```

- `HasValue`·`Value`·`GetValueOrDefault`는 `Nullable<T>`와 같은 계약입니다. 값이 없을 때 `Value`는 `InvalidOperationException`을 던집니다.
- 변환: `T`·`T?`에서 암시적으로, `T?`로 암시적으로, `T`로는 명시적으로(`Value`) 바뀝니다.
- 필드 이름 `value`·`hasValue`는 문자열 상수로도 공개되어 있습니다([`SerializableNullable.nameOfInternalValue`](../../Runtime/Structs/SerializableNullable.cs#L26) 등). core.editor의 프로퍼티 드로어와 uielements의 Nullable 필드가 이 이름을 씁니다. JSON 변환기는 이 이름을 속성으로 찾습니다(아래 관찰된 동작).
- ① 값이 없으면 저장 직전과 역직렬화 직후에 `value`를 `default`로 지웁니다(97–112행). 그래서 "값 없음"은 늘 `value = default`로 저장됩니다.
- [`SerializableNullable.GetUnderlyingType`](../../Runtime/Structs/SerializableNullable.cs#L41)은 `ISerializableNullable<>`를 구현한 닫힌 타입에서 `T`를 돌려줍니다. [`NullableType`](03-reflection-helpers.md#32-nullabletype-확장-가능한-nullable-판정)에 기본으로 들어 있어, 리플렉션 쪽에서는 `Nullable<T>`와 같은 nullable로 취급됩니다.

같음 비교는 "둘 다 값이 없으면 같음, 한쪽만 없으면 다름, 둘 다 있으면 값 비교"입니다(`==`, `Equals(SerializableNullable<T>)`). 값이 없는 인스턴스는 `Equals(null)`이 `true`입니다.

> **관찰된 동작:** `Equals(object)`(119–127행)는 값이 있을 때 `value.Equals(other)`를 부릅니다. 그래서 다른 `SerializableNullable<T>`를 `object`로 넘기면 값이 같아도 `false`입니다. 같은 타입끼리 비교할 때는 `==`나 `Equals(SerializableNullable<T>)`가 불립니다.

**JSON.** [`SerializableNullableConverter`](../../Runtime/Json/Converters/SerializableNullableConverter.cs#L12)가 맡습니다. 읽기는 `null` 토큰이면 값 없는 인스턴스, 아니면 안의 `T`를 읽어 생성자에 넘깁니다(77–94행).

> **관찰된 동작 (실행으로 확인하지 않음):** `WriteJson`은 값의 타입에서 `IsAssignableToGenericDefinition(typeof(ISerializableNullable<>))`로 찾은 타입에 `GetProperty("hasValue")`를 부릅니다(35·42행). 이 검사는 인터페이스를 먼저 보므로 찾은 타입은 인터페이스 `ISerializableNullable<T>`이고, 거기에는 `hasValue` 속성이 없습니다(구조체의 `hasValue`는 필드). 코드대로라면 `SerializableNullable<T>`를 JSON으로 쓸 때마다 `JsonSerializationException`이 납니다.

## 6.2 `SerializableType`

[`SerializableType`](../../Runtime/Structs/SerializableType.cs#L16)은 `Type`을 `"전체 이름, 어셈블리 이름"` 문자열로 저장합니다.

```csharp
// Runtime/Structs/SerializableType.cs
31   [JsonIgnore] public Type? value { get; set; }
36   [SerializeField, Delayed, JsonProperty(nameof(value))] string? _value;
96   void ISerializationCallbackReceiver.OnBeforeSerialize() => _value = value?.SerializeToString();
103  void ISerializationCallbackReceiver.OnAfterDeserialize()
```

- 런타임 값은 `Type`이고, 문자열은 Unity 직렬화 직전에 만들고 직후에 `Type.GetType`으로 되돌립니다. 어셈블리 버전을 넣지 않으므로 버전이 바뀌어도 찾을 수 있습니다.
- 타입을 찾지 못하면 `value`가 `null`이 됩니다. 예외는 없고, 원래 문자열도 다음 저장 때 사라집니다.
- JSON은 [`SerializableTypeConverter`](../../Runtime/Json/Converters/SerializableTypeConverter.cs#L12)가 같은 문자열 하나로 씁니다. 빈 문자열과 `null`은 `null` 타입입니다.
- 비교는 `value`의 `Type` 비교입니다.

## 6.3 `Version`과 `VersionRange`

[`Version`](../../Runtime/Structs/Version.cs#L18)은 `major.minor.patch` 세 칸을 `int?`로 가지고, **비어 있는 칸(`*`)은 "어떤 값과도 맞음"**을 뜻합니다. 지금은 리소스 팩 메타데이터([`PackMetaData`](../../Runtime/Resource/PackMetaData.cs#L4))의 `version`, `targetVersion`, `targetRuniOSVersion`에 쓰입니다.

| 입력 | 결과 |
| --- | --- |
| `"1.2.3"` | `1.2.3` |
| `"1.2"` | `1.2.*` |
| `"1.x.3"` | `1.*.3`(숫자로 읽을 수 없는 칸은 비움) |
| `"1.2.3.4"` | `1.2.3`(넷째 칸부터 무시) |
| `null`, `""` | `*.*.*` = `Version.all` |

`ToString`은 항상 세 칸을 쓰고 빈 칸은 `*`입니다. 공백은 파싱 전에 모두 지웁니다(99행).

**비교 연산자.** `<`·`>`·`<=`·`>=`는 칸마다 "한쪽이 비어 있으면 그 칸은 맞음"으로 보고 major → minor → patch 순으로 판단합니다.

```csharp
// Runtime/Structs/Version.cs
216  public static bool operator <(Version lhs, Version rhs)
     {
218      if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor == rhs.minor) && (lhs.patch == null || rhs.patch == null || lhs.patch < rhs.patch))
219          return true;
220      else if ((lhs.major == null || rhs.major == null || lhs.major == rhs.major) && (lhs.minor == null || rhs.minor == null || lhs.minor < rhs.minor))
221          return true;
222      else if (lhs.major == null || rhs.major == null || lhs.major < rhs.major)
223          return true;
225      return false;
     }
253  public static bool operator ==(Version lhs, Version rhs) => lhs.major == rhs.major && lhs.minor == rhs.minor && lhs.patch == rhs.patch;
```

- 칸이 모두 채워진 버전끼리는 보통의 사전식 비교입니다.
- 와일드카드가 끼면 의미가 달라집니다. 예를 들어 `1.*.*`와 `1.2.3`은 `<`, `>`, `<=`, `>=`가 **모두** `true`입니다. 와일드카드 칸이 "맞음"이 되어 앞의 조건이 바로 성립하기 때문입니다. 이 와일드카드 규칙은 의도입니다.
- 반면 `==`는 와일드카드를 보지 않고 칸 값을 그대로 비교하므로 `1.*.* == 1.2.3`은 `false`입니다. `CompareTo`(495행)는 `<`를 먼저 보므로 이 경우 `-1`입니다.
- 비교 연산자와 `==`가 하는 일이 달라 헷갈릴 수 있어서, 비교 연산자를 없애고 일반 메서드로 바꾸는 것을 고려하고 있습니다.
- 산술 연산자도 있습니다. `Version + int`는 patch에만 더하고, 두 칸 중 하나라도 비어 있으면 결과 칸도 빕니다(`int?` 연산).

[`VersionRange`](../../Runtime/Structs/VersionRange.cs#L15)는 `min~max`입니다. `"1.0.0~2.0.0"`처럼 쓰고, `~`가 없으면 그 버전 하나가 `min`이자 `max`입니다. `~`가 여럿이면 첫 칸과 마지막 칸을 씁니다(44–45행). [`Contains(Version)`](../../Runtime/Structs/VersionRange.cs#L176)는 `version >= min && version <= max`이므로 위 와일드카드 규칙을 그대로 따릅니다. 저장소에서 `Contains`나 비교 연산자를 부르는 곳은 아직 없습니다.

> **관찰된 동작:** `VersionRange < Version` 연산자(156행)의 본문은 `>` 연산자(166행)와 같습니다(`lhs.min > rhs && lhs.max > rhs`). 줄 끝 주석도 이 점을 적고 있습니다.

## 6.4 `HexColor`

[`HexColor`](../../Runtime/Structs/HexColor.cs#L18)는 색을 **16진 문자열로 저장**하고, RGBA 바이트를 함께 들고 있습니다.

- Unity 직렬화 대상은 문자열 `_value` 하나입니다(192행). 바이트 필드는 직렬화되지 않고, 역직렬화 뒤 `OnAfterDeserialize`의 `value = value`(370행)가 문자열을 다시 읽어 채웁니다. 그래서 인스펙터에서 문자열을 고치면 색이 따라옵니다.
- [`TryParse`](../../Runtime/Structs/HexColor.cs#L396)는 `#`가 있든 없든 `RRGGBBAA`, `RRGGBB`, `RGBA`, `RGB`를 받습니다. 한 자리 형식은 자리를 두 번 반복한 값(`F` → `FF`)입니다(434–441행). 알파가 없으면 255입니다.
- 읽을 수 없는 문자열을 넣으면 예외 없이 투명(`#00000000`)이 됩니다.
- `value`에 넣은 문자열은 그대로 보존되지만(`"#fff"`), `r`·`g`·`b`·`a`·`color`를 바꾸면 `ToHex`가 다시 만든 대문자 문자열(`#FFFFFF`, 알파가 255면 6자리)로 바뀝니다. `ToString`과 JSON 출력은 항상 이 형식입니다.
- 같음 비교는 바이트 넷입니다(316행). `"#fff"`와 `"#FFFFFF"`는 같습니다.

## 6.5 그 밖의 구조체

| 구조체 | 요약 |
| --- | --- |
| [`RectOffset`](../../Runtime/Structs/RectOffset.cs#L8) | `float` 네 변(`left`, `right`, `top`, `bottom`). `Add(Rect)`·`Remove(Rect)`로 사각형을 넓히거나 줄임. `UnityEngine.RectOffset`(정수)과 암시적 변환 |
| [`CornerRadius`](../../Runtime/Structs/CornerRadius.cs#L10) | 네 모서리 반지름. `float` 하나에서 암시적 변환, `float`과 `==` 비교(네 값이 모두 같을 때) |
| [`RectVertices`](../../Runtime/Structs/RectVertices.cs#L12) | 사각형의 네 꼭짓점. `Rect`와 양방향 암시적 변환 |
| [`UnlimitedDateTime`](../../Runtime/Structs/UnlimitedDateTime.cs#L11) | 연·월·일·시·분·초·밀리초를 `int` 필드로 따로 저장. `DateTime`의 범위 밖(예: 음수 연도)도 담음. 범위 검사 없음. `DateTime`으로는 명시적 변환(범위 밖이면 `DateTime` 생성자가 던짐) |

`RectOffset`은 이름이 `UnityEngine.RectOffset`과 같으므로, `using UnityEngine;`이 있는 파일에서는 어느 쪽인지 적어 주어야 합니다.

## 6.6 JSON 변환기 목록

모두 [`Runtime/Json/Converters/`](../../Runtime/Json/Converters)에 있고, 대상 타입에 `[JsonConverter(typeof(…))]`로 붙어 있어 따로 등록할 필요가 없습니다.

| 대상 | 변환기 | JSON 형태 | `null` 토큰 |
| --- | --- | --- | --- |
| `Version` | [`VersionConverter`](../../Runtime/Json/Converters/VersionConverter.cs#L12) | `"1.2.*"` | `Version.all` |
| `VersionRange` | [`VersionRangeConverter`](../../Runtime/Json/Converters/VersionRangeConverter.cs#L12) | `"1.0.0~2.0.0"` | `Version.all`을 변환한 범위 |
| `HexColor` | [`HexColorConverter`](../../Runtime/Json/Converters/HexColorConverter.cs#L12) | `"#RRGGBB"` 또는 `"#RRGGBBAA"` | `HexColor.clear` |
| `SerializableType` | [`SerializableTypeConverter`](../../Runtime/Json/Converters/SerializableTypeConverter.cs#L12) | `"전체 이름, 어셈블리"` | `null` 타입 |
| `SerializableNullable<T>` | [`SerializableNullableConverter`](../../Runtime/Json/Converters/SerializableNullableConverter.cs#L12) | 안의 값, 또는 `null` | 값 없음 |
| `RuniPath`, `PhysicalPath`, `FileExtension` | [`Converters/IO/`](../../Runtime/Json/Converters/IO) | 경로 문자열 | 받지 않음(예외) |
| `Identifier`, `PackIdentifier` | [`Converters/Resource/`](../../Runtime/Json/Converters/Resource) | [Resource 10.3절](../Resource/10-languages-and-json.md#103-json-컨버터) | |

문자열 형태 변환기는 표에 적은 토큰이 아니면 `JsonReaderException`을 던집니다.
