# 8. 차이와 기존 문서

> 기준 커밋: `571e2110`. [← 목차](README.md) · 이전: [7. 함수 모음](07-function-collections.md)

유틸리티 영역에는 기존 README가 없습니다. 이 장은 주석·설정이 현재 코드와 다른 곳, 이 문서에 "관찰된 동작"으로 적은 항목, 생성기·분석기를 읽다가 발견한 불일치, 사용처가 없는 공개 멤버를 모읍니다. 코드와 주석은 고치지 않았습니다.

- [8.1 주석·설정과 코드가 다른 곳](#81-주석설정과-코드가-다른-곳)
- [8.2 관찰된 동작 목록](#82-관찰된-동작-목록)
- [8.3 생성기·분석기의 불일치](#83-생성기분석기의-불일치)
- [8.4 사용처가 없는 공개 멤버](#84-사용처가-없는-공개-멤버)

## 8.1 주석·설정과 코드가 다른 곳

| 위치 | 주석·설정 | 코드 |
| --- | --- | --- |
| [`RuniPlayerLoop`](../../Runtime/LowLevel/RuniPlayerLoop.cs#L9) 클래스 XML (9행) | 등록된 이벤트는 Play 모드가 끝나면 자동으로 해제 | 해제하는 코드가 없습니다. 구독자가 직접 뺍니다([4.2절](04-execution-helpers.md#42-runiplayerloop)) |
| [`AttributedTypeRegistry.DirectRegisterRange`](../../Runtime/Reflection/AttributedTypeRegistry.cs#L134) XML (134행) | 등록 잠금을 해제한 뒤 `onChanged` 발생 | 잠금이 없습니다. 등록 부분은 이름 없는 중괄호 블록으로만 감싸져 있습니다(142–153행) |
| [`TypeRegistry.Register`](../../Runtime/Reflection/TypeRegistry.cs#L26) 위 주석 (26행) | 생성 코드가 어셈블리 로드 때 `Register(typeof(구현 타입))`을 부름 | 생성 코드는 `DirectRegisterRange`·`RegisterRangeUnchecked`를 부릅니다([2.4절](02-registry-generators.md#24-생성되는-등록-코드)). 40행의 `Unregister` 주석은 맞습니다 |
| [`OrderByTypes`](../../Runtime/Reflection/ReflectionUtility.cs#L116) 안의 주석 (116–119행) | 1차 정렬 키는 인터페이스가 아닌지 여부 | 실제 첫 키는 `void`·`object`·`ValueType`이 아닌지이고, 인터페이스 여부는 넷째입니다(144–148행). 메서드 XML(98행)은 코드와 맞습니다 |
| [`linkmerge.xml`](../../linkmerge.xml) | `RuniOS.Booting.AwakenAttribute`, `RuniOS.Booting.StartenAttribute`, `RuniOS.MathUtility`를 보존 | 셋 다 그 이름의 타입이 없습니다. `MathUtility`의 실제 이름은 `RuniOS.Utility.MathUtility`입니다 |
| `linkmerge.xml`, [`AssemblyInfo.cs`](../../Runtime/AssemblyInfo.cs#L5) | 어셈블리 `RuniOS.Patches`(보존, `InternalsVisibleTo`) | 그 이름의 asmdef가 없습니다. core.editor의 패치 어셈블리 이름은 `RuniOS.Editor.Patches`입니다 |

## 8.2 관찰된 동작 목록

| 항목 | 절 |
| --- | --- |
| `AttributedTypeRegistry.RegisterRange(ReadOnlySpan<Type>)`가 잘못된 타입에서 예외 없이 `return`하고 `onChanged`를 부르지 않음 | [1.4](01-type-registry.md#14-attributedtyperegistry의-저장-구조와-등록) |
| `AsyncTask.Dispose`가 정상 완료에도 `cancelEvent`를 부르고 토큰을 취소함 | [4.3](04-execution-helpers.md#43-asynctask와-progresstask) |
| `Debug`가 사용자 중첩 타입의 로그에 바깥 타입 이름을 붙임 | [5.1](05-diagnostics.md#51-debug) |
| `SerializableNullable<T>.Equals(object)`가 다른 `SerializableNullable<T>`에 `false` | [6.1](06-serializable-structs.md#61-serializablenullablet) |
| `SerializableNullableConverter.WriteJson`이 쓸 때마다 예외를 낼 것으로 읽힘(실행 확인 안 함) | [6.1](06-serializable-structs.md#61-serializablenullablet) |
| `VersionRange < Version`의 본문이 `>`와 같음 | [6.3](06-serializable-structs.md#63-version과-versionrange) |
| `MathUtility.Sign(0)`이 `1` | [7.1](07-function-collections.md#71-수학) |
| 32비트에서 `nuintMaxSize`가 2³¹ | [7.1](07-function-collections.md#71-수학) |
| `GuidUtility.ToByte` 등이 `MaxValue`로 나눈 나머지라 `MaxValue`가 나오지 않음 | [7.1](07-function-collections.md#71-수학) |

## 8.3 생성기·분석기의 불일치

**코드를 읽은 결과이고, 실행으로 확인하지 않았습니다.**

| 항목 | 위치 | 내용 |
| --- | --- | --- |
| 생성자 검사와 호출이 다름 | [`AssignableTypeRegistrySourceGenerator.cs` 66–91행](../../../../RuniOS.CodeAnalysis/Generators/TypeRegistry/AssignableTypeRegistrySourceGenerator.cs#L66) | 생성자에 접근 가능한지는 매개변수가 `params ImmutableArray<Type>` 하나인 생성자로 검사하지만, 초기화 식은 `(bool, ImmutableArray<Type>)` 생성자를 부릅니다. 지금은 둘 다 `public`이라 결과가 같습니다 |
| ROS0024가 다른 어셈블리의 레지스트리에는 나지 않을 것으로 읽힘 | [`AssignableTypeRegistryAnalyzer.cs` 93–97행](../../../../RuniOS.CodeAnalysis/Analyzers/AssignableTypeRegistryAnalyzer.cs#L93) | 참조 어셈블리의 매니페스트를 이 컴파일의 매니페스트 특성과 **심볼 동일성**으로 비교합니다. 매니페스트 특성은 어셈블리마다 따로 넣은 사본이라 같은 심볼이 아닙니다. 또 직접 참조만 봅니다. 생성기와 다른 분석기는 메타데이터 이름으로, 전이적으로 찾습니다 |
| ROS0019 오탐 가능성 | [`TypeRegistrationAttributeAnalyzer.cs` 540–549행](../../../../RuniOS.CodeAnalysis/Analyzers/TypeRegistrationAttributeAnalyzer.cs#L540) | 같은 컴파일의 레지스트리는 구현 타입의 기본 타입 사슬에 선언된 속성만 찾습니다. 생성기는 컴파일의 모든 레지스트리와 결합하므로, 다른 타입에 `baseType`을 지정해 선언한 레지스트리에 실제로 등록되는 구현도 ROS0019 경고를 받을 수 있습니다 |
| 제네릭 소유 타입의 매니페스트 | [`TypeRegistryEmitter.cs` 175–179행](../../../../RuniOS.CodeAnalysis/Generators/TypeRegistry/TypeRegistryEmitter.cs#L175) | 소유 타입은 `typeof(Owner<>)`로 쓰지만 기본 타입은 `GetTypeOfName`으로 써서, 기본 타입을 생략한 제네릭 소유 타입이면 어셈블리 특성 안에 `typeof(Owner<T>)`가 생깁니다. 그 자리에서 `T`는 범위 밖이라 컴파일되지 않을 것으로 읽힙니다. ROS0012는 등록 코드만 막고 매니페스트는 막지 않습니다 |
| 이름 없는 음수 열거형 값 | [`AttributeLiteralEmitter.cs` 475–478행](../../../../RuniOS.CodeAnalysis/Generators/TypeRegistry/AttributeLiteralEmitter.cs#L475) | `(global::E)-1`을 만듭니다. C#은 괄호 안이 키워드 타입이 아니면 이것을 캐스트가 아니라 뺄셈으로 읽습니다 |
| 쓰이지 않는 기본 구현 | [`TypeRegistrySourceGenerator.cs` 282–292행](../../../../RuniOS.CodeAnalysis/Generators/TypeRegistry/TypeRegistrySourceGenerator.cs#L282) | 기본 `EmitRegisterStatements`는 `Register(typeof(…))`를 쓰지만 두 생성기가 모두 재정의하므로 불리지 않습니다 |
| 같은 등록 이름 | [`TypeRegistrySourceGenerator.cs` 832·843행](../../../../RuniOS.CodeAnalysis/Generators/TypeRegistry/TypeRegistrySourceGenerator.cs#L832) | `OnAssemblyLoaded`와 `OnAssemblyUnloading` 등록에 같은 이름 문자열 `"….Initialize"`를 넘깁니다. 이 이름을 Unity가 어디에 쓰는지는 확인하지 않았습니다 |

## 8.4 사용처가 없는 공개 멤버

저장소 전체를 텍스트로 검색한 결과입니다.

| 멤버 | 비고 |
| --- | --- |
| `AttributedTypeRegistry.Resolve`·`TryResolve`의 `predicate` 매개변수 | 넘기는 곳 없음 |
| `AttributedTypeRegistry.registeredEntries` | 클래스 밖에서 읽는 곳 없음 |
| `TypeRegistry.Register`·`RegisterRange`(직접 호출) | 등록은 모두 생성 코드가 함 |
| `RuniPlayerLoop.UnregisterAll` | |
| `AsyncTask.asyncTasks`, `asyncTaskAdd`, `asyncTaskRemove` | 구독·열거하는 곳 없음 |
| `VersionRange.Contains`, `Version`·`VersionRange` 비교 연산자 | |
| `Overridable<T>`, `ConditionalUniObjectTable<TKey, TValue>`, `TypeHandlerAttribute` | 타입 자체를 쓰는 곳 없음 |
