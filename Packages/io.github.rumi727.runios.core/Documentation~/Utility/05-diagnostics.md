# 5. 진단

> 기준 커밋: `571e2110`. [← 목차](README.md) · 이전: [4. 실행 도우미](04-execution-helpers.md) · 다음: [6. 직렬화 구조체와 JSON 변환기](06-serializable-structs.md)

로그, 무한 루프 감지, Harmony 패치 로그, 에디터의 driven property 기록처럼 개발 중에 상태를 보려고 쓰는 도구입니다. 대부분 `[Conditional]`이라 **릴리스 빌드에서는 호출 자체가 사라집니다.** 어떤 기호에서 남는지를 먼저 표로 정리합니다.

| 멤버 | 남는 조건 |
| --- | --- |
| `Debug.Log`, `LogWarning`, `LogError`, `Assert` | `UNITY_EDITOR` 또는 `UNITY_ENABLE_CHECKS` |
| `Debug.RuntimeLog*`, `RuntimeAssert`, `LogException`, `NameOfCallingClass` | 항상 |
| `InfiniteLoopDetector.Run` | `UNITY_EDITOR` 또는 `UNITY_ENABLE_CHECKS` |
| `DrivenPropertyManager.RegisterProperty`, `UnregisterProperty` | `UNITY_EDITOR` |

`[Conditional]` 메서드는 호출 식 전체가 지워지므로, 인수를 만드는 코드(문자열 보간 등)도 함께 사라집니다.

- [5.1 `Debug`](#51-debug)
- [5.2 `InfiniteLoopDetector`](#52-infiniteloopdetector)
- [5.3 `HarmonyUtility`](#53-harmonyutility)
- [5.4 `DrivenPropertyManager`](#54-drivenpropertymanager)

## 5.1 `Debug`

[`RuniOS.Debug`](../../Runtime/Debug.cs#L8)는 메시지 앞에 호출한 클래스 이름을 붙여 `UnityEngine.Debug`로 넘깁니다. core와 Modding 어셈블리의 전역 using이 `Debug = RuniOS.Debug`로 별칭을 두므로, 그 어셈블리 안에서 `Debug.Log`라고 쓰면 이 클래스입니다.

| 메서드 | 형식 |
| --- | --- |
| `Log` 계열 | `[클래스] 메시지` |
| `RuntimeLog` 계열 | `<b>[클래스]</b> 메시지` |

`className`을 넘기지 않으면 [`NameOfCallingClass`](../../Runtime/Debug.cs#L78)가 스택을 거슬러 올라가 찾습니다. 호출마다 `StackTrace`를 만들므로 싸지 않습니다. `Log`·`RuntimeLog` 계열과 `NameOfCallingClass`는 `[MethodImpl(NoInlining)]`이라 인라인으로 프레임이 사라지지 않습니다.

```csharp
// Runtime/Debug.cs
 80  StackTrace stackTrace = new StackTrace(1, false);
 82  for (int i = 0; i < stackTrace.FrameCount; i++)
     {
 84      MethodBase? method = stackTrace.GetFrame(i)?.GetMethod();
 85      Type? type = GetLogicalDeclaringType(method);        // ① 컴파일러 생성 타입을 바깥으로
 90      if (type == typeof(Debug))                           // ② Debug 자신의 프레임은 건너뜀
 91          continue;
 93      if (skipFrames > 0)
         {
 95          skipFrames--;
 96          continue;
         }
 99      return type.GetTypeDisplayName();
     }
102  return nameof(Debug);
```

① 람다·`async` 상태 기계 같은 컴파일러 생성 타입은 그 코드를 쓴 타입 안에 중첩되므로, [`GetLogicalDeclaringType`](../../Runtime/Debug.cs#L105)이 `DeclaringType`을 따라 바깥으로 올라갑니다.
② 그래서 `Log` → `NameOfCallingClass` 사이의 프레임은 세지 않고, 그 바깥의 첫 타입이 이름이 됩니다.

> **관찰된 동작:** 바깥으로 올라갈지 정하는 [`IsCompilerGeneratedType`](../../Runtime/Debug.cs#L117)은 `DeclaringType`이 있으면 무조건 `true`를 돌려줍니다(125행). 그래서 컴파일러가 만든 타입뿐 아니라 사용자가 만든 중첩 타입 `Foo.Bar`에서 찍은 로그도 `[Foo]`로 나옵니다.

## 5.2 `InfiniteLoopDetector`

[`InfiniteLoopDetector`](../../Runtime/InfiniteLoopDetector.cs#L8)는 루프 안에 `Run()`을 넣어 두면, 카운터가 다시 0이 되기 전까지의 `Run()` 호출이 100만 번(`detectionThreshold`)을 넘을 때 예외를 던집니다.

```csharp
// Runtime/InfiniteLoopDetector.cs
13  public static void Run([CallerFilePath] string fp = "", [CallerLineNumber] int ln = 0, [CallerMemberName] string mn = "")
    {
15      string currentPoint = $"{fp}:{ln}, {mn}()";
16      if (isLoopDetected)
17          throw new Exception($"Infinite Loop Detected: {currentPoint}");
    }
…
25  if (++detectionCount > detectionThreshold)   // isLoopDetected getter: 읽을 때마다 1 증가
```

- **카운터는 하나뿐이고, 모든 호출 지점과 스레드가 함께 씁니다.** 카운터가 0으로 돌아가는 때는 에디터에서는 `EditorApplication.update`마다(41행), 빌드(`UNITY_ENABLE_CHECKS`)에서는 `RuniPlayerLoop.onInit`마다(43행)입니다. 빌드에서 이 구독은 `[OnAssemblyLoaded]`에서 걸리는데, 빌드에서 그 훅이 언제 불리는지는 확인하지 않았습니다([부팅 1.1절](../Boot/01-lifecycle-hooks.md#11-훅의-뜻)). 루프마다 따로 세지 않는 단순한 감지기이고, 이것은 의도입니다.
- 예외 메시지에는 마지막으로 `Run`을 부른 위치가 들어갑니다. 어느 루프가 100만 번째 호출을 했느냐에 따라 달라집니다.
- `Run`은 호출마다 위치 문자열을 만듭니다(15행). `isLoopDetected`가 `true`일 때만 쓰지만 매번 만듭니다.
- `UNITY_EDITOR`·`UNITY_ENABLE_CHECKS`가 아니면 `isLoopDetected`는 상수 `false`이고 `Run` 호출은 지워집니다.
- core에서 `Run`을 부르는 곳은 없고, core.editor의 `UndoHandler`가 씁니다.

## 5.3 `HarmonyUtility`

Modding 어셈블리(`RuniOS.Modding`, `Modding/Runtime/`)의 [`HarmonyUtility`](../../Modding/Runtime/HarmonyUtility.cs#L12)는 Harmony 패치 적용·해제를 감싸고, 걸린 시간을 로그로 남깁니다.

| 메서드 | 하는 일 |
| --- | --- |
| `Patch(harmony, assembly)` | `harmony.PatchAll(assembly)` |
| `Unpatch(harmony)` | `harmony.UnpatchAll(harmony.Id)`. 그 id의 패치만 풉니다 |
| `Repatch(harmony, assembly)` | 그 id의 패치를 풀고 다시 `PatchAll` |
| `PatchInEditor(harmony, assembly)` | 에디터에서는 `Repatch`, 빌드에서는 `Patch` |

`PatchInEditor`는 에디터에서 먼저 풀기 때문에, 같은 id의 패치가 이미 걸려 있어도 겹쳐 걸리지 않습니다. core.editor의 `Patches`가 `[OnAssemblyLoaded]`에서 `PatchInEditor`, `[OnAssemblyUnloading]`에서 `Unpatch`를 부릅니다.

로그 여부 `logEnable`은 에디터에서 `false`, 빌드에서 `true`입니다(15·18행). 로그는 `Debug.Log`(별칭으로 `RuniOS.Debug`)로 찍으므로, 빌드에서도 `UNITY_ENABLE_CHECKS`가 없으면 호출이 지워져 남지 않습니다.

## 5.4 `DrivenPropertyManager`

[`DrivenPropertyManager`](../../Runtime/DrivenPropertyManager.cs#L7)는 Unity 내부의 `DrivenPropertyManager`(인스펙터에서 "다른 컴포넌트가 이 값을 제어함"으로 회색 처리되는 속성)를 부르고, 등록한 항목을 자기 목록 `drivenProperties`에도 남깁니다. 내부 API는 `RuniOS.APIBridge`가 만든 브리지 DLL의 `DrivenPropertyManagerBridge`로 부릅니다.

- 두 메서드 모두 `[Conditional("UNITY_EDITOR")]`입니다.
- 브리지 호출이 던지면 예외를 기록하고, 목록은 바꾸지 않고 돌아갑니다.
- 목록은 core.editor의 `DevelopmentPanel`이 화면에 보여 줍니다.
- 항목 비교([`DrivenPropertyData`](../../Runtime/DrivenPropertyManager.cs#L46))는 `driver`·`target`·`propertyPath`가 모두 같을 때 같습니다. `Remove`는 같은 항목 하나를 지웁니다.
