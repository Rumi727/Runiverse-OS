# 4. 실행 도우미

> 기준 커밋: `571e2110`. [← 목차](README.md) · 이전: [3. 리플렉션 도우미](03-reflection-helpers.md) · 다음: [5. 진단](05-diagnostics.md)
>
> 함께 보기: [부팅 1장 수명 주기 훅](../Boot/01-lifecycle-hooks.md), [부팅 4장 `Kernel`](../Boot/04-kernel.md)

스레드 확인, Unity PlayerLoop 구독, 진행 중인 작업 목록, 예외를 삼키는 이벤트 호출처럼 "코드가 언제 어디서 돌아가는가"에 관한 작은 도우미를 모았습니다. 리소스 리로드를 직렬화하는 `AsyncReloadGate`도 `Tasks` 폴더에 있지만, [Resource 3장](../Resource/03-reload-gate.md)에서 다룹니다.

- [4.1 `UnityThread`](#41-unitythread)
- [4.2 `RuniPlayerLoop`](#42-runiplayerloop)
- [4.3 `AsyncTask`와 `ProgressTask`](#43-asynctask와-progresstask)
- [4.4 `SafeInvoke`와 `SafeReport`](#44-safeinvoke와-safereport)

## 4.1 `UnityThread`

```csharp
// Runtime/Threading/UnityThread.cs
 9  public static int mainThreadId { get; private set; }
10  public static bool isMainThread => Thread.CurrentThread.ManagedThreadId == mainThreadId;
12  [OnAssemblyLoaded]
13  static void OnAssemblyLoaded() => mainThreadId = Thread.CurrentThread.ManagedThreadId;
15  public static void ThrowIfNotMainThread()
```

[`UnityThread`](../../Runtime/Threading/UnityThread.cs#L7)는 `[OnAssemblyLoaded]`가 불린 스레드를 메인 스레드로 기억합니다. Unity가 이 훅을 메인 스레드에서 부르는 것을 전제로 합니다.

- 그 전에는 `mainThreadId`가 `0`이고, 관리 스레드 id는 0이 아니므로 `isMainThread`는 어느 스레드에서나 `false`입니다.
- 사용처는 `DisposeQueue`(메인 스레드가 아니면 큐에 넣음)와 core.editor의 `EditorThreadDispatcher`입니다.
- 빌드에서 `OnAssemblyLoaded`가 언제 불리는지는 확인하지 않았습니다([부팅 1.1절](../Boot/01-lifecycle-hooks.md#11-훅의-뜻)).

## 4.2 `RuniPlayerLoop`

[`RuniPlayerLoop`](../../Runtime/LowLevel/RuniPlayerLoop.cs#L11)는 Unity PlayerLoop의 특정 단계에 델리게이트를 더하고 빼는 도우미입니다. `MonoBehaviour` 없이 매 프레임 코드를 돌리고 싶을 때 씁니다.

단계마다 이벤트가 하나씩 있고, `add`·`remove`가 `Register<T>`·`Unregister<T>`로 이어집니다.

| 이벤트 | PlayerLoop 단계 타입 | core 사용처 |
| --- | --- | --- |
| `onInit` | `Initialization` | `InfiniteLoopDetector`(빌드에서만, [5.2절](05-diagnostics.md#52-infiniteloopdetector)) |
| `onEarlyUpdate` | `EarlyUpdate` | |
| `onFixedUpdate` | `FixedUpdate` | |
| `onPreUpdate` | `PreUpdate` | |
| `onUpdate` | `Update` | |
| `onPreLateUpdate` | `PreLateUpdate` | |
| `onPostLateUpdate` | `PostLateUpdate` | `DisposeQueue` |
| `onTimeUpdate` | `TimeUpdate` | `Kernel`([부팅 4.3절](../Boot/04-kernel.md#43-시간-갱신의-구독)) |

```csharp
// Runtime/LowLevel/RuniPlayerLoop.cs
100  public static bool Register(Type? targetType, PlayerLoopSystem.UpdateFunction? updateDelegate)
     {
102      if (!Kernel.isPlaying)
103          throw new InvalidOperationException();                     // ①
105      if (targetType == null || updateDelegate == null)
106          return false;
108      PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();       // ②
109      bool result = InternalRegister(ref loop, targetType, updateDelegate);
110      PlayerLoop.SetPlayerLoop(loop);
112      return result;
     }
```

① **Play 모드가 아닐 때 등록·해제하면 예외입니다.** 에디터의 편집 모드에서는 `Kernel.isPlaying`이 `false`입니다. core의 구독자는 `[OnEnteringPlayMode]`에서 구독하는데, 그 시점에 `Application.isPlaying`이 이미 `true`인지는 확인하지 않았습니다([부팅 4.3절](../Boot/04-kernel.md#43-시간-갱신의-구독)). 빌드에서 `Kernel.isPlaying`은 상수 `true`입니다.
② 매번 현재 PlayerLoop 전체를 받아 고친 뒤 다시 설정합니다. `InternalRegister`(125행)는 하위 시스템 트리를 깊이 우선으로 훑어 `type`이 같은 첫 시스템의 `updateDelegate`에 더합니다. 구조체 배열이라 고친 항목을 배열에 다시 써 넣습니다(136·143행). 맞는 단계가 없으면 `false`입니다.

해제는 세 가지입니다.

- `Unregister<T>`·`Unregister(Type, …)`: 그 타입의 시스템에서 뺍니다. 넘긴 델리게이트가 멀티캐스트면 호출 목록의 항목을 하나씩 뺍니다(214–216행).
- [`UnregisterAll`](../../Runtime/LowLevel/RuniPlayerLoop.cs#L184): 모든 단계에서 뺍니다.
- 등록과 달리 결과를 돌려주지 않습니다.

**구독은 자동으로 해제되지 않습니다.** 클래스 XML(9행)은 Play 모드가 끝나면 자동으로 해제된다고 적지만, 해제하는 코드는 없습니다. 그래서 core의 구독자는 모두 직접 뺍니다. `Kernel`은 `[OnExitingPlayMode]`에서 `onTimeUpdate -= TimeUpdate`([67행](../../Runtime/Kernel.cs#L67))를 하고, `DisposeQueue`도 같은 훅에서 `onPostLateUpdate`를 뺍니다.

## 4.3 `AsyncTask`와 `ProgressTask`

[`ProgressTask`](../../Runtime/Tasks/ProgressTask.cs#L7)는 이름·설명(`Text`), 진행률(`ReactiveProperty<float>`), 취소 가능 여부, 취소 이벤트, 실행 시간을 가진 추상 작업입니다. `IProgress<float>`이기도 해서 `Report`가 `progress.Value`에 씁니다(25행). `isRunning`은 `!isDisposed`입니다.

[`AsyncTask`](../../Runtime/Tasks/AsyncTask.cs#L9)는 그 구현이고, **만들면 전역 목록에 들어가고 `Dispose`하면 빠집니다.** 목록과 추가·제거 이벤트가 공개되어 있어 진행 중인 작업을 밖에서 열거할 수 있습니다.

```csharp
// Runtime/Tasks/AsyncTask.cs
11  public static event Action? asyncTaskAdd = null;
12  public static event Action? asyncTaskRemove = null;
16  static readonly List<AsyncTask> _asyncTasks = [];
17  public static IReadOnlyList<AsyncTask> asyncTasks { get; } = _asyncTasks.AsReadOnly();
…
25  public AsyncTask(Text name, Text description, bool cancellable = false)
    {
        …
31      _asyncTasks.Add(this);
32      asyncTaskAdd.SafeInvoke();
34      runningTimeWatch.Start();
    }
…
60  public override void Dispose()
    {
62      ObjectDisposedException.ThrowIf(isDisposed, this);       // ① 두 번째 Dispose는 예외
64      cancelEvent.SafeInvoke();                                // ②
65      cancelEvent = null;
67      _asyncTasks.Remove(this);
68      asyncTaskRemove.SafeInvoke();
70      cancelTokenSource.Cancel();                              // ②
72      isDisposed = true;
73      runningTimeWatch.Stop();
75      progress.Dispose();
    }
```

- 생성자는 어느 스레드에서 불려도 같은 `List`에 넣습니다. 잠금은 없습니다.
- `cancelToken`은 `Dispose` 때 취소되는 토큰입니다(56행). 따로 `Cancel` 메서드는 없습니다.
- 지금 core에서 `AsyncTask`를 만드는 곳은 `ResourceManager`의 리로드 하나입니다(100행에서 만들고 162행에서 `Dispose`).

> **관찰된 동작:** ② `Dispose`는 작업이 정상적으로 끝났을 때도 `cancelEvent`를 부르고 `cancelTokenSource.Cancel()`을 호출합니다. 그래서 `ResourceManager`의 리로드가 성공해도 그 작업의 `cancelToken`은 취소 상태가 됩니다. 완료와 취소를 구분하는 상태는 없습니다.

## 4.4 `SafeInvoke`와 `SafeReport`

[`EventUtility.SafeInvoke`](../../Runtime/Utility/EventUtility.cs#L7)는 델리게이트의 호출 목록을 하나씩 부르고, 하나가 던져도 나머지를 계속 부릅니다. 예외는 `Debug.LogException`으로 남깁니다.

```csharp
// Runtime/Utility/EventUtility.cs
 7  public static void SafeInvoke(this Delegate? e)
    {
        …
12      Delegate[] delegates = e.GetInvocationList();
13      for (int i = 0; i < delegates.Length; i++)
        {
            try
            {
17              delegates[i].DynamicInvoke();
            }
            catch (Exception ex)
            {
21              Debug.LogException(ex);
            }
        }
    }
27  public static void SafeInvoke(this Delegate? e, params object?[] args)
```

- `Delegate`를 받으므로 어떤 이벤트 타입에도 쓸 수 있지만, 그 대가로 `DynamicInvoke`(리플렉션 호출)를 씁니다. **구독자가 던진 예외는 `TargetInvocationException`에 감싸여 기록됩니다.** 원래 예외는 `InnerException`에 있습니다.
- 인수가 있는 오버로드는 `params object?[]`라 호출마다 배열을 만들고 값 타입 인수를 boxing합니다. 매 프레임 부르는 곳에는 맞지 않습니다.
- 마일스톤의 동기 구독자도 같은 방식으로 불립니다([부팅 3.3절](../Boot/03-milestones.md#33-milestonedispatcher-호출)).

[`ProgressUtility.SafeReport`](../../Runtime/Utility/ProgressUtility.cs#L7)는 `IProgress<T>`가 `null`이면 아무것도 하지 않고, `Report`가 던진 예외를 기록하고 삼킵니다. 이쪽은 직접 호출이라 감싸지 않습니다.
