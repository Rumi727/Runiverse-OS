# 4. `Kernel`

> 기준 커밋: `1f1bb93d`. [← 목차](README.md) · 이전: [3. 마일스톤](03-milestones.md) · 다음: [5. `ServiceRegistry`](05-service-registry.md)

[`Kernel`](../../Runtime/Kernel.cs#L7)은 두 파일로 나뉜 정적 partial 클래스입니다. `Kernel.cs`는 실행 상태와 종료를, `Kernel.Time.cs`는 프레임 시간을 다룹니다.

- [4.1 실행 상태](#41-실행-상태)
- [4.2 프레임 시간](#42-프레임-시간)
- [4.3 시간 갱신의 구독](#43-시간-갱신의-구독)
- [4.4 종료: `quitting`과 `Quit`](#44-종료-quitting과-quit)

## 4.1 실행 상태

```csharp
// Runtime/Kernel.cs
15  public static Version rosVersion { get; } = new Version(0, 0, 0);
17  #if UNITY_EDITOR
23      public static bool isEditor => Application.isEditor;
30      public static bool isPlaying => Application.isPlaying;
37      public static bool isPlayingAndNotPaused => Application.isPlaying && !UnityEditor.EditorApplication.isPaused;
38  #else
39      public const bool isEditor = false;
40      public const bool isPlaying = true;
41      public const bool isPlayingAndNotPaused = true;
42  #endif
```

에디터에서는 Unity 값을 그대로 읽고, 빌드에서는 **`const`**입니다. 빌드에서 `if (!Kernel.isPlaying)` 같은 분기는 컴파일러가 상수로 접습니다.

`rosVersion`은 현재 `0.0.0` 고정이고, 저장소에서 읽는 곳이 없습니다.

## 4.2 프레임 시간

```csharp
// Runtime/Kernel.Time.cs
9   public static float fps { get; private set; } = 60;
11  public static float deltaTime { get; private set; } = fps60Second;
15  public static float smoothDeltaTime { get; private set; } = fps60Second;
17  public static float unscaledDeltaTime { get; private set; } = fps60Second;
21  public static float unscaledSmoothDeltaTime { get; private set; } = fps60Second;
23  public static float fixedDeltaTime { get => Time.fixedDeltaTime; set => Time.fixedDeltaTime = value; }
29  public const float fps60Second = 1f / 60f;
31  static readonly Stopwatch deltaTimeStopwatch = Stopwatch.StartNew();
33  static void TimeUpdate()
34  {
35      double realDeltaTime = deltaTimeStopwatch.Elapsed.TotalSeconds;
36      deltaTimeStopwatch.Restart();
38      float timeScale = Time.timeScale;
42      deltaTime = (float)realDeltaTime * timeScale;
43      deltaTimeDouble = realDeltaTime * timeScale;
44      unscaledDeltaTime = (float)realDeltaTime;
45      unscaledDeltaTimeDouble = realDeltaTime;
47      fps = 1f / unscaledDeltaTime;
52      unscaledSmoothDeltaTime = (0.2f * unscaledDeltaTime) + ((1 - 0.2f) * unscaledSmoothDeltaTime);
53      smoothDeltaTime = unscaledDeltaTime * timeScale;
55  }
```

| 줄 | 설명 |
| --- | --- |
| 9–21 | 첫 갱신 전에는 60 fps를 가정한 값입니다. `double` 판(`deltaTimeDouble`, `unscaledDeltaTimeDouble`)도 있습니다. |
| 23 | `Time.fixedDeltaTime`을 그대로 읽고 씁니다. |
| 31·35–36 | Unity의 `Time`이 아니라 **`Stopwatch`**로 잰 실제 경과 시간입니다. 직전 `TimeUpdate`(또는 [4.3절](#43-시간-갱신의-구독)의 재시작) 이후의 시간입니다. 40–41행 주석은 Unity 내장 변수보다 이렇게 값을 저장해 두고 읽는 쪽이 빠르다는 측정 결과를 이유로 듭니다. |
| 42–45 | 스케일 적용 값은 `Time.timeScale`을 곱합니다. |
| 47 | 그 프레임 하나로 계산한 순간 fps입니다. 평균하지 않습니다. |
| 52 | 비율 0.2의 지수 이동 평균입니다. 50행 주석은 Unity 내부 구현과 거의 같다고 적습니다. |
| 53 | 아래 관찰된 동작 참고. |

> **관찰된 동작:** `smoothDeltaTime`은 `unscaledDeltaTime * timeScale`이라 평활되지 않고, 늘 `deltaTime`과 같은 값입니다([`Kernel.Time.cs#L53`](../../Runtime/Kernel.Time.cs#L53)). 평활된 값은 `unscaledSmoothDeltaTime` 하나뿐입니다. 에디터 제어판의 [`GenericPanel`](../../../io.github.rumi727.runios.core.editor/Editor/Windows/GenericPanel.cs#L20)이 두 값을 나란히 표시합니다.

이 값을 쓰는 곳은 fmod의 `WaveAudioSource`(속도 계산), nbs의 `NoteBlockSource`, 에디터 `GenericPanel`입니다.

## 4.3 시간 갱신의 구독

```csharp
// Runtime/Kernel.cs
53  [OnEnteringPlayMode]
54  static void OnEnteringPlayMode()
55  {
56      RuniPlayerLoop.onTimeUpdate += TimeUpdate;
57      Application.quitting += Quitting;
59  #if UNITY_EDITOR
60      UnityEditor.EditorApplication.pauseStateChanged += PauseStateChanged;
61  #endif
62  }
64  [OnExitingPlayMode]
65  static void OnExitingPlayMode() { /* 56–60행의 구독을 모두 해제 */ }
76  [UnityEditor.Scripting.LifecycleManagement.OnEnteringEditMode]
77  static void OnEnteringEditMode() => UnityEditor.EditorApplication.update += TimeUpdate;
79  [UnityEditor.Scripting.LifecycleManagement.OnExitingEditMode]
80  static void OnExitingEditMode() => UnityEditor.EditorApplication.update -= TimeUpdate;
84  static void PauseStateChanged(UnityEditor.PauseState pauseState) => deltaTimeStopwatch.Restart();
```

| 상황 | `TimeUpdate`가 도는 곳 |
| --- | --- |
| 빌드, 에디터 Play 모드 | PlayerLoop의 `TimeUpdate` 단계. [`RuniPlayerLoop.onTimeUpdate`](../../Runtime/LowLevel/RuniPlayerLoop.cs#L73)가 Unity PlayerLoop의 해당 하위 시스템에 델리게이트를 붙입니다(유틸리티, 예정). |
| 에디터 Edit 모드 | `EditorApplication.update`. 에디터가 갱신할 때마다 돌므로, 값은 에디터 갱신 간격입니다. |

빌드에서는 앱이 시작할 때 `OnEnteringPlayMode`가 불립니다([1.1절](01-lifecycle-hooks.md#11-훅의-뜻)).

에디터에서 일시정지 상태가 바뀌면 스톱워치를 다시 시작합니다(84행). 그래서 상태가 바뀌기 전까지 흐른 시간은 다음 `deltaTime`에 들어가지 않습니다. 스톱워치를 재시작하는 곳은 `TimeUpdate`와 이 함수 둘뿐입니다. 그래서 Edit 모드에서 Play 모드로 넘어가는 동안의 시간은 Play 모드 첫 갱신 값에 들어갑니다.

> **확인하지 않음:** 56행의 `RuniPlayerLoop.Register`는 `Kernel.isPlaying`이 `false`면 예외를 던집니다. 에디터에서 `OnEnteringPlayMode`가 불릴 때 `Application.isPlaying`이 이미 `true`인지는 확인하지 않았습니다([1.4절](01-lifecycle-hooks.md#14-확인하지-않은-것)).

## 4.4 종료: `quitting`과 `Quit`

```csharp
// Runtime/Kernel.cs
45  /// Application.quitting 이벤트랑 동일하지만 커널보다 먼저 실행되는 것을 보장하며 플레이 모드 해제 시 이벤트가 자동으로 초기화됩니다
49  public static event Action? quitting;
…
108 static void Quitting()
109 {
110     quitting.SafeInvoke();
111     quitting = null;
113     Application.quitting -= Quitting;
120 }
```

`Kernel.quitting`은 `Application.quitting`에 걸린 `Quitting()`이 대신 부르는 이벤트입니다.

- 110행: 구독자마다 따로 부르고, 예외는 로그로 남깁니다(`SafeInvoke`).
- 111행: 부른 뒤 구독자를 모두 비웁니다. 이벤트를 비우는 곳은 여기뿐입니다. 필드에 `[AutoStaticsCleanup]`도 없습니다.
- 113행: 자기 구독을 풉니다. 다음 Play 세션의 `OnEnteringPlayMode`가 다시 겁니다.

`OnExitingPlayMode`는 `Application.quitting` 구독을 풀기만 하고 `quitting`을 비우지 않습니다. 에디터에서 Play 모드를 나갈 때 `OnExitingPlayMode`와 `Application.quitting` 중 어느 것이 먼저 오는지는 확인하지 않았습니다. `OnExitingPlayMode`가 먼저 오면 `Quitting()`이 불리지 않으므로, `quitting` 구독자가 불리지도 비워지지도 않은 채 다음 Play 세션까지 남습니다. XML과 다른 점은 [6장](06-differences.md)에 모았습니다. 현재 저장소에 `Kernel.quitting` 구독자는 없습니다.

```csharp
// Runtime/Kernel.cs
88  public static void Quit()            // 에디터: EditorApplication.isPlaying = false / 빌드: Application.Quit()
98  public static void Quit(int exitCode) // 에디터: 같음 (exitCode 무시) / 빌드: Application.Quit(exitCode)
```

에디터에서 `Quit`은 Play 모드를 끝낼 뿐 에디터를 닫지 않습니다. 저장소에서 `Kernel.Quit`을 부르는 곳은 없습니다.
