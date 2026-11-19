# 7. 함수 모음

> 기준 커밋: `571e2110`. [← 목차](README.md) · 이전: [6. 직렬화 구조체와 JSON 변환기](06-serializable-structs.md) · 다음: [8. 차이와 기존 문서](08-differences.md)

상태 없이 쓰는 정적 확장 메서드 모음입니다. 개별 함수는 XML 주석에 맡기고, 이 장은 **파일마다 무엇이 있는지, 그리고 표준 라이브러리와 다르게 동작하는 곳**만 적습니다.

core의 전역 using에는 `global using static RuniOS.Utility.MathUtility;`가 있습니다([`GenericGlobalUsing.cs`](../../Runtime/GenericGlobalUsing.cs#L12)). 그래서 core 안에서는 `Lerp(…)`, `pi`처럼 클래스 이름 없이 `MathUtility` 멤버를 씁니다.

- [7.1 수학](#71-수학)
- [7.2 컬렉션과 LINQ](#72-컬렉션과-linq)
- [7.3 문자열과 시간](#73-문자열과-시간)
- [7.4 Unity와 IO](#74-unity와-io)
- [7.5 작은 타입](#75-작은-타입)

## 7.1 수학

| 파일 | 내용 |
| --- | --- |
| [`MathUtility.cs`](../../Runtime/Utility/MathUtility.cs#L12) | 공개 정적 멤버가 약 270개인 가장 큰 파일. 상수(`pi`, `deg2Rad` 등), `Abs`·`Clamp`·`Lerp`·`Repeat`·`PingPong`·`MoveTowards`·`Round` 계열을 `INumber<T>` 제네릭, `float`/`double`/`decimal`, `BigDecimal`, Unity 벡터·사원수에 대해 제공. 벡터 연산자도 C# 14 확장 연산자로 더함 |
| [`DecimalMath.cs`](../../Runtime/DecimalMath.cs) | `decimal`용 `Sqrt`, `Exp`, `Log`, `Power`, 삼각·쌍곡 함수 |
| [`EasingFunctions.cs`](../../Runtime/EasingFunctions.cs) | `EaseIn/Out/InOut` × `Quad`~`Bounce`, `Spring`, `Linear`과 각각의 도함수(`…D`). `EasingFunction` 열거형으로 고르는 `EasingCalculate`·`EasingDerivativeCalculate` |
| [`GuidUtility.cs`](../../Runtime/Utility/GuidUtility.cs#L7) | `Guid` ↔ 정수·`BigInteger`·`BigDecimal` 변환 |

`MathUtility`의 부동소수 `Lerp`는 `Mathf.Lerp`처럼 `t`를 0~1로 자릅니다.

표준 라이브러리와 다른 곳:

> **관찰된 동작:** [`Sign<T>`](../../Runtime/Utility/MathUtility.cs#L1387)는 `0`에 `1`을 돌려줍니다(`value < 0 ? -1 : 1`). `Math.Sign(0)`은 `0`입니다. `BigDecimal`용 `Sign`도 같습니다.

> **관찰된 동작:** [`nuintMaxSize`](../../Runtime/Utility/MathUtility.cs#L37)는 64비트에서 `ulong.MaxValue`이지만, 32비트에서는 `1uL << 31`(2³¹)을 돌려줍니다. 32비트 `nuint`의 최댓값은 2³²−1입니다.

> **관찰된 동작:** [`GuidUtility.ToByte`](../../Runtime/Utility/GuidUtility.cs#L11)는 `Guid`를 `BigInteger`로 바꾼 뒤 `Repeat(byte.MaxValue)`, 즉 255로 나눈 나머지를 씁니다. 그래서 결과는 0~254이고 255는 나오지 않습니다. `ToUInt16`·`ToUInt32`·`ToUInt64`도 같은 방식으로 각 타입의 `MaxValue`로 나눕니다.

## 7.2 컬렉션과 LINQ

| 파일 | 내용 |
| --- | --- |
| [`ListUtility.cs`](../../Runtime/Utility/ListUtility.cs) | `IList`·`IList<T>` 확장: `Move`, `Resize`, `Deduplicate`, `SyncWithEnumerable`, 가장 가까운 값·인덱스 찾기(`CloseValue`, 이진 탐색판 포함), 숫자 섞인 문자열 정렬(`OrderByAlphaNumeric`) 등 |
| [`LinqExtras.cs`](../../Runtime/Utility/LinqExtras.cs) | `WhereNotNull`, `IsEmpty`, `TwoOrMore`, `ContainsAll`, `IndexOf`, `GetSequenceHashCode`, `AsReadOnly`, 비제네릭 `SequenceEqual` 등 |
| [`UniTaskLinqExtras.cs`](../../Runtime/Utility/UniTaskLinqExtras.cs#L19) | `EnumerateOnThreadPool`: 동기 열거를 스레드 풀에서 돌리는 `IUniTaskAsyncEnumerable` |
| [`Spans/`](../../Runtime/Spans) | `SplitSpans`·`SplitAnySpans`: `Span`·`ReadOnlySpan`을 할당 없이 나누는 `ref struct` 열거자. 구분자는 원소 하나, 원소열, 원소 집합 중 아무거나의 세 종류 |

Span 분할기는 `StringSplitOptions` 중 `RemoveEmptyEntries`만 봅니다(예: [`ReadOnlySpanSplitter.cs` 68행](../../Runtime/Spans/ReadOnlySpanSplitter.cs#L68)). `TrimEntries`는 무시됩니다. 빈 구분자는 `ArgumentException`입니다.

## 7.3 문자열과 시간

| 파일 | 내용 |
| --- | --- |
| [`StringUtility.cs`](../../Runtime/Utility/StringUtility.cs) | `RemoveAllWhitespace`, `GetLines`, 따옴표를 고려한 분할(`QuotedSplit`, `EnumerateQuotedSplit`), `AddSpacesToSentence`(`ConstEnvironmentVariable`은 주석 처리됨) |
| [`StringUtility.DataSize.cs`](../../Runtime/Utility/StringUtility.DataSize.cs) | 바이트 수 → `"1.5 MB"` 같은 문자열 |
| [`StringUtility.KeyCode.cs`](../../Runtime/Utility/StringUtility.KeyCode.cs) | `KeyCode` → 표시 문자열 |
| [`StringUtility.ToBar.cs`](../../Runtime/Utility/StringUtility.ToBar.cs) | 값을 텍스트 진행 막대로 |
| [`TimeUtility.cs`](../../Runtime/Utility/TimeUtility.cs) | `TimeSpan`의 연·월·주 단위 계산, `ToTimeString`, 상대 시간 문자열 `ToRelativeString`, 양력·음력 변환 |
| [`StringBuilderCache.cs`](../../Runtime/StringBuilderCache.cs#L11) | .NET 내부 `StringBuilderCache`를 옮긴 것. 스레드마다 360자 이하 `StringBuilder` 하나를 재사용(`Acquire` → `GetStringAndRelease`) |

`StringBuilderCache`는 스레드당 하나만 보관합니다. `Acquire`한 빌더를 놓기 전에 다시 `Acquire`하면 새 빌더가 만들어지므로 중첩해서 써도 안전하고, `Release`하면 마지막으로 놓은 빌더 하나만 남습니다.

## 7.4 Unity와 IO

| 파일 | 내용 |
| --- | --- |
| [`ComponentUtility.cs`](../../Runtime/Utility/ComponentUtility.cs#L8) | `GetComponentFieldSave` 계열: 넘긴 필드가 있고 같은 게임 오브젝트의 것이면 그대로, 아니면 `GetComponent…`로 찾아 돌려줌. 찾지 못했을 때 모드에 따라 `AddComponent`하거나(`addIfNull`), 빈 배열을 돌려주거나(`addZeroLengthIfNull`), 호출한 컴포넌트 자신을 `DestroyImmediate`함(`destroyIfNull`) |
| [`ResourceUtility.cs`](../../Runtime/Utility/ResourceUtility.cs#L7) | `Resources/RuniOS/`에서 처음 읽을 때 불러 두는 빈 `Transform`·`RectTransform`, 기본·색 `Material`, 정적 확장 `Texture2D.missingTexture` |
| [`ExceptionUtility.cs`](../../Runtime/Utility/ExceptionUtility.cs#L17) | 정적 확장 `ArgumentNullException.ThrowIfUnityNull`: Unity의 가짜 null도 잡는 `==` 비교. `ToSummaryString` |
| [`DirectoryUtility.cs`](../../Runtime/Utility/DirectoryUtility.cs#L10) | 정적 확장 `Directory.Copy`(하위 폴더 포함 복사) |
| [`StreamUtility.cs`](../../Runtime/Utility/StreamUtility.cs#L10) | `ReadToEnd`, `ReadToEndAsync` |
| [`RuniPathUtility.cs`](../../Runtime/Utility/RuniPathUtility.cs) | `RuniPath` 문자열 도우미. [IO 1장](../IO/01-paths.md)에서 다룸 |
| [`LocalizationUtility.cs`](../../Runtime/Utility/LocalizationUtility.cs) | 번역 조회. [Resource 10.2절](../Resource/10-languages-and-json.md#102-번역-조회-localizationutility)에서 다룸 |
| [`PointerUtility.cs`](../../Runtime/Utility/PointerUtility.cs) | `System.Reflection.Pointer` → `IntPtr`. 실제 `unsafe` 코드는 별도 어셈블리 `RuniOS.Unsafe`([`Unsafe/PointerUtility.cs`](../../Unsafe/PointerUtility.cs))에 있음 |

`ThrowIfUnityNull`·`Directory.Copy`·`Texture2D.missingTexture`는 C# 14의 정적 확장 멤버(`extension(타입) { public static … }`)라 원래 타입의 정적 멤버처럼 부릅니다.

## 7.5 작은 타입

| 타입 | 내용 |
| --- | --- |
| [`Overridable<T>`](../../Runtime/Overridable.cs#L3) | 원래 값 `rawValue`에 `Func<T, T>` 수정자를 `order` 오름차순으로 차례로 적용한 값이 `value`. `Override`가 돌려준 `IDisposable`을 `Dispose`하면 그 수정자가 빠짐. 같은 `order`는 나중에 더한 것이 뒤. 저장소에 사용처 없음 |
| [`ConditionalUniObjectTable<TKey, TValue>`](../../Runtime/ConditionalUniObjectTable.cs#L10) | Unity 오브젝트를 키로 하는 직렬화 가능한 표. 연산마다 파괴된 키를 먼저 지움(`Clean`). 키를 **강하게** 참조하므로 `ConditionalWeakTable`처럼 키를 살려 두지 않는 효과는 없고, 파괴된 키가 다음 연산 때 지워질 뿐. 저장소에 사용처 없음 |
| [`TypeHandlerAttribute`](../../Runtime/TypeHandlerAttribute.cs#L18) | `[Obsolete]`("전역 타입 탐색은 폐기됨. 명시적 등록을 사용"). 사용처 없음. 지금 쓰는 등록 특성은 [1장](01-type-registry.md)의 `TypeRegistrationAttribute` |
