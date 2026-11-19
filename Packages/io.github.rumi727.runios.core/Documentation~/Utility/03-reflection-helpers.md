# 3. 리플렉션 도우미

> 기준 커밋: `571e2110`. [← 목차](README.md) · 이전: [2. 레지스트리 소스 생성기·분석기](02-registry-generators.md) · 다음: [4. 실행 도우미](04-execution-helpers.md)

타입 레지스트리와 인스펙터가 공통으로 쓰는 리플렉션 확장입니다. 가장 중요한 것은 열린 제네릭까지 다루는 할당 검사 `IsAssignableToAny`이고, 그다음은 사용자 정의 nullable 값 타입(`SerializableNullable<T>`)을 `Nullable<T>`와 같은 자리에 놓는 `NullableType`·`Nullability`입니다.

- [3.1 `IsAssignableToAny`](#31-isassignabletoany)
- [3.2 `NullableType`: 확장 가능한 nullable 판정](#32-nullabletype-확장-가능한-nullable-판정)
- [3.3 `Nullability`](#33-nullability)
- [3.4 그 밖의 확장](#34-그-밖의-확장)

## 3.1 `IsAssignableToAny`

`Type.IsAssignableFrom`은 `typeof(IList<>).IsAssignableFrom(typeof(List<int>))`에 `false`를 돌려줍니다. 열린 제네릭 정의는 어떤 닫힌 타입에도 할당되지 않기 때문입니다. 레지스트리는 `typeof(IList<>)` 같은 대상을 써야 하므로, [`IsAssignableToAny`](../../Runtime/Utility/TypeUtility.cs#L335)가 이 경우를 따로 처리합니다. **결과로 "실제로 맞은 닫힌 타입"을 함께 돌려주는 것**이 핵심입니다. 레지스트리는 이 타입의 제네릭 인수로 구현을 닫습니다([1.6절](01-type-registry.md#16-tryresolve-대상-타입에서-구현-타입-찾기)).

```csharp
// Runtime/Utility/TypeUtility.cs
335  public bool IsAssignableToAny(Type targetType, [MaybeNullWhen(false)] out Type resolvedType)
     {
         …
344      if (targetType.IsPointer && type.IsAssignableToPointer(targetType))   // ① 포인터
         {
346          resolvedType = type;
347          return true;
         }
350      if (targetType == typeof(object))                                      // ② object
         {
352          resolvedType = targetType;
353          return true;
         }
356      if (targetType.IsGenericTypeDefinition)                                // ③ 열린 제네릭
357          return type.IsAssignableToGenericDefinition(targetType, out resolvedType);
358      else if (targetType.IsAssignableFrom(type))                            // ④ 보통의 할당
         {
360          resolvedType = targetType;
361          return true;
         }
     …
```

① `void*`는 모든 포인터를 받고, 그 밖의 포인터는 같은 타입만 받습니다([`IsAssignableToPointer`](../../Runtime/Utility/TypeUtility.cs#L255)).
② `object` 대상은 포인터를 포함해 무엇이든 받습니다.
③ [`IsAssignableToGenericDefinition`](../../Runtime/Utility/TypeUtility.cs#L193)은 타입에서 기본 타입 쪽으로 올라가며, 단계마다 **인터페이스를 먼저**, 그다음 그 타입 자신을 제네릭 정의와 비교합니다. 처음 맞은 닫힌 타입을 `resolvedType`으로 돌려줍니다. 예: `List<int>`와 `IList<>` → `IList<int>`.
④ 나머지는 표준 `IsAssignableFrom`이고, `resolvedType`은 대상 타입 자신입니다.

같은 파일의 [`GetHierarchy`](../../Runtime/Utility/TypeUtility.cs#L392)는 타입 자신부터 `BaseType`을 따라 `object`까지 열거합니다. 레지스트리 정렬의 깊이 키가 이 개수입니다([1.5절](01-type-registry.md#15-등록-항목의-정렬)).

## 3.2 `NullableType`: 확장 가능한 nullable 판정

[`NullableType`](../../Runtime/NullableType.cs#L11)은 "이 타입에 null이 들어갈 수 있나", "nullable 래퍼라면 안의 타입은 무엇인가"를 묻는 확장 메서드 둘을 이벤트로 열어 둡니다.

```csharp
// Runtime/NullableType.cs
19  public static event Func<Type, bool> isNullable;
20  public static bool IsNullable(this Type type) => isNullable.GetInvocationList()
        .OfType<Func<Type, bool>>()
        .Any(x => x.Invoke(type));                                       // ① 하나라도 true면 true

33  public static event Func<Type, Type?> getNullableUnderlyingType;
34  public static Type? GetNullableUnderlyingType(this Type type) => getNullableUnderlyingType.GetInvocationList()
        .OfType<Func<Type, Type?>>()
        .Select(x => x.Invoke(type))
        .FirstOrDefault(x => x != null);                                 // ② 처음 나온 null 아닌 값

44  public static bool IsNull([NotNullWhen(false)] this object? value) => value == null || value.Equals(null);   // ③

55  static NullableType()
    {
57      isNullable += x => x.IsClass || x.IsInterface || Nullable.GetUnderlyingType(x) != null || SerializableNullable.GetUnderlyingType(x) != null;
59      getNullableUnderlyingType += Nullable.GetUnderlyingType;
60      getNullableUnderlyingType += SerializableNullable.GetUnderlyingType;
    }
```

① ② 판정 함수가 이벤트라서, 함수를 더하면 다른 nullable 래퍼도 같은 판정에 들어갑니다. 클래스 XML 주석도 이 클래스가 nullable 타입의 "등록과 확인"을 맡는다고 적습니다. 기본으로 참조 타입, `Nullable<T>`, `SerializableNullable<T>`([6.1절](06-serializable-structs.md#61-serializablenullablet))가 들어 있습니다.
③ `value.Equals(null)`을 함께 보는 이유는 Unity의 파괴된 오브젝트("가짜 null")와 값이 없는 `SerializableNullable<T>`도 null로 보기 위해서입니다. 둘 다 `Equals(null)`이 `true`를 돌려줍니다.

## 3.3 `Nullability`

[`Nullability`](../../Runtime/Reflection/Nullability.cs#L30)는 .NET의 `NullabilityInfo`를 불변 레코드 구조체로 옮긴 것입니다. 인스펙터가 필드·속성·매개변수에 null을 넣어도 되는지 판단할 때 씁니다.

```csharp
// Runtime/Reflection/Nullability.cs
30  public readonly record struct Nullability(Type type, NullState readState, NullState writeState, ImmutableArray<Nullability> children)
36  public Nullability? elementType => type.IsArray && !children.IsDefaultOrEmpty ? children[0] : null;
42  public ImmutableArray<Nullability> genericTypeArguments => type.IsArray || children.IsDefault ? [] : children;
…
98  static Nullability From(NullabilityInfo info)
    {
        …
104     if (info.Type.IsValueType && info.Type.GetNullableUnderlyingType() != null)
105         readState = writeState = NullState.Nullable;                       // ①
```

- `children` 하나에 배열이면 요소 타입, 제네릭이면 제네릭 인수를 담습니다. 두 속성이 타입을 보고 나눠 읽습니다.
- ① .NET은 `SerializableNullable<T>`를 그냥 구조체로 보고 `NotNull`이라 보고합니다. `From`은 `NullableType`이 래퍼로 인정한 값 타입을 `Nullable`로 고쳐 적습니다.
- `Create`는 `ParameterInfo`·`PropertyInfo`·`FieldInfo`·`EventInfo`용이 있고, 매번 새 `NullabilityInfoContext`를 만듭니다(58–76행).
- `Equals`·`GetHashCode`를 직접 구현한 이유는 `ImmutableArray`가 참조 비교를 하기 때문입니다. `children`을 요소 단위로 비교합니다(82행).

상태 값은 [`NullState`](../../Runtime/Reflection/NullState.cs#L8)의 `Unknown`·`NotNull`·`Nullable`입니다. .NET의 `NullabilityState`와 같은 의미이고, nullable 문맥이 꺼진 코드는 `Unknown`입니다.

## 3.4 그 밖의 확장

| 멤버 | 파일 | 하는 일 |
| --- | --- | --- |
| `IsInteger`, `IsFractional`, `IsNumeric`, `IsText`, `IsExactlyRepresentableAs*`, `NumericMinValue`/`NumericMaxValue` | [`TypeUtility.cs`](../../Runtime/Utility/TypeUtility.cs#L19) | `TypeCode`로 숫자 타입을 분류. `IsInteger`는 열거형을 뺍니다. `NumericMinValue`는 지원하지 않는 타입에 `ArgumentException` |
| `GetTypeDisplayName` | [`TypeUtility.cs`](../../Runtime/Utility/TypeUtility.cs#L368) | `void`, 배열, 포인터를 처리하고 나머지는 `Unity.Properties.TypeUtility.GetTypeDisplayName`에 맡김 |
| `SerializeToString`, `DeserializeFromString` | [`TypeUtility.cs`](../../Runtime/Utility/TypeUtility.cs#L400) | `"전체 이름, 어셈블리 이름"` 문자열과 `Type.GetType`. 찾지 못하면 `null` |
| `IsAsyncMethod`, `IsCompilerGenerated` | [`ReflectionUtility.cs`](../../Runtime/Reflection/ReflectionUtility.cs#L13) | `AsyncStateMachineAttribute`·`CompilerGeneratedAttribute` 확인 |
| `GetDefaultValue` | [`ReflectionUtility.cs`](../../Runtime/Reflection/ReflectionUtility.cs#L23) | 참조 타입은 `null`, 값 타입은 `Activator.CreateInstance` |
| `GetDefaultValueNotNull` | [`ReflectionUtility.cs`](../../Runtime/Reflection/ReflectionUtility.cs#L31) | null이 아닌 기본값: `string`은 빈 문자열, 배열은 길이 0, `Nullable<T>`는 `default(T)`, 나머지는 기본 생성자 |
| `HasDefaultConstructor` | [`ReflectionUtility.cs`](../../Runtime/Reflection/ReflectionUtility.cs#L51) | 값 타입은 항상 `true` |
| `Cast(object, Type)` | [`ReflectionUtility.cs`](../../Runtime/Reflection/ReflectionUtility.cs#L61) | 식 트리로 `(Type)(실제 타입)value` 변환을 만들어 실행. 사용자 정의 변환 연산자도 적용되지만 호출마다 컴파일합니다 |

`GetDefaultValueNotNull`에서 `Nullable<T>`를 따로 다루는 이유는 코드 주석(38–39행)에 있습니다. `default(T?)`를 boxing하면 `null`이 되므로 값이 있는 `default(T)`를 대신 돌려줍니다.
