# AsyncReloadGate 설계 명세

## 목적

`AsyncReloadGate`는 리소스 관련 비동기 `Reload` 메소드가 실행 중 다시 호출됐을 때 현재 작업과 새 작업이 겹치지 않도록 제어한다.

현재 리로드는 취소하지 않는다. 현재 리로드가 끝난 뒤, 실행 중 들어온 요청들을 하나로 합쳐 가장 최근에 요청된 **전체 리로드를 처음부터 한 번 더 실행**한다.

이 문서는 구현자가 그대로 코드에 반영할 수 있는 최종 계약이다.

## 전제

- 모든 리소스 관련 `Reload` 메소드와 `AsyncReloadGate`는 메인 스레드 전용이다.
- 비동기 설계 목적은 메인 스레드를 블로킹하지 않는 것이며, 여러 스레드에서 동시에 호출하기 위한 것이 아니다.
- `lock`, `SemaphoreSlim`, `Interlocked` 등 스레드 동기화 수단을 사용하지 않는다.
- 현재 실행 중인 리로드는 취소하지 않는다.
- 대기 요청을 모두 순서대로 실행하는 큐를 만들지 않는다.
- 리로드 결과값은 없으며 `UniTask`만 지원한다.
- 취소 토큰은 지원하지 않는다.

## 배치와 패스

- **배치(batch)**: 게이트가 유휴 상태에서 첫 요청을 받은 시점부터, 대기 요청까지 모두 처리해 다시 유휴 상태가 될 때까지의 전체 구간.
- **패스(pass)**: 실제 리로드 메소드 전체를 처음부터 끝까지 한 번 실행하는 구간.

한 배치에는 한 개 이상의 패스가 존재할 수 있다.

```text
유휴
→ A 패스 실행
→ A 실행 중 B, C 요청
→ 최신 요청 C 패스 실행
→ 추가 요청 없음
→ 유휴
```

## 핵심 계약

1. 유휴 상태에서 `Run(A)`가 호출되면 A를 즉시 실행한다.
2. A 실행 중 `Run(B)`, `Run(C)`가 호출되면 B 작업은 폐기하고 C 작업만 보존한다.
3. A를 중단하지 않고 끝까지 실행한다.
4. A가 끝나면 C를 전체 처음부터 한 번 실행한다.
5. C 실행 중 D가 요청되면 C가 끝난 뒤 D를 전체 처음부터 한 번 실행한다.
6. 대기 요청이 없을 때만 배치를 완료한다.
7. 같은 배치에 참가한 모든 `Run` 호출자는 배치 전체가 끝날 때까지 대기한다.
8. 실행되는 각 패스는 일반적인 `Reload` 직접 호출과 동일해야 한다.
9. 이벤트, 진행률, 작업 객체 생성과 정리도 각 패스에서 처음부터 다시 실행한다.

## 공개 API

권장 위치:

```text
Packages/com.rumi.runios.core/Runtime/Tasks/AsyncReloadGate.cs
```

권장 네임스페이스와 API:

```csharp
#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.Tasks
{
    public sealed class AsyncReloadGate
    {
        public bool isRunning { get; }

        public UniTask Run
        (
            Func<IProgress<float>?, UniTask> reload,
            IProgress<float>? progress = null
        );

        public UniTask Run(Func<UniTask> reload);
    }
}
```

`Run(Func<UniTask>)`는 진행률이 없는 리로드를 위한 편의 오버로드다.

## 내부 상태

필수 상태:

```csharp
Func<IProgress<float>?, UniTask>? pendingReload;
readonly List<IProgress<float>> pendingProgresses = [];
UniTaskCompletionSource? completionSource;
```

의미:

- `pendingReload`
  - 다음 패스에서 실행할 최신 리로드 작업.
  - 새 요청이 들어올 때마다 덮어쓴다.
  - 큐가 아니다.
- `pendingProgresses`
  - 다음 패스에 참가할 진행률 객체들.
  - 작업이 덮이더라도 진행률 객체는 폐기하지 않는다.
  - 현재 실행 중인 패스의 진행률 객체는 포함하지 않는다.
- `completionSource`
  - 현재 배치에 참가한 모든 호출자가 공유하는 완료 소스.
  - `null`이면 유휴 상태다.
  - `isRunning`은 `completionSource != null`로 계산한다.

별도 `isRunning` 필드나 `rerunRequested` 필드를 두지 않는다. 동일한 상태를 여러 필드로 중복 표현하지 않는다.

## Run 동작

`Run`은 다음 순서를 따른다.

1. `reload`가 `null`이면 `ArgumentNullException`을 던진다.
2. `pendingReload`를 새 `reload`로 교체한다.
3. `progress`가 있다면 `pendingProgresses`에 등록한다.
4. 동일한 진행률 인스턴스가 이미 다음 패스에 등록됐다면 참조 동일성 기준으로 중복 등록하지 않는다.
5. 배치가 실행 중이면 기존 `completionSource.Task`를 반환한다.
6. 유휴 상태라면 새 `UniTaskCompletionSource`를 생성한다.
7. 실행 루프를 시작한다.
8. 실행 루프가 동기적으로 완료될 가능성에 대비해 지역 변수에 보관한 완료 소스의 `Task`를 반환한다.

개념 코드:

```csharp
public UniTask Run
(
    Func<IProgress<float>?, UniTask> reload,
    IProgress<float>? progress = null
)
{
    if (reload == null)
        throw new ArgumentNullException(nameof(reload));

    pendingReload = reload;
    AddPendingProgress(progress);

    if (completionSource != null)
        return completionSource.Task;

    UniTaskCompletionSource source = new();
    completionSource = source;

    RunLoop(source).Forget();
    return source.Task;
}
```

## 실행 루프

각 반복은 정확히 한 패스를 담당한다.

패스 시작 시 다음 작업을 순서대로 수행한다.

1. 현재 `pendingReload`를 지역 변수로 가져온다.
2. `pendingReload`를 `null`로 초기화한다.
3. `pendingProgresses`를 배열로 스냅샷한다.
4. `pendingProgresses`를 즉시 비운다.
5. 스냅샷으로 해당 패스 전용 결합 진행률 객체를 만든다.
6. 지역 변수로 가져온 리로드 작업을 결합 진행률과 함께 실행한다.
7. 실행 중 새 요청이 들어왔다면 `pendingReload`가 다시 채워져 있으므로 다음 패스를 실행한다.
8. 새 요청이 없다면 배치를 종료한다.

개념 코드:

```csharp
async UniTask RunLoop(UniTaskCompletionSource source)
{
    List<Exception>? exceptions = null;

    do
    {
        Func<IProgress<float>?, UniTask> reload = pendingReload!;
        pendingReload = null;

        IProgress<float>[] progresses = pendingProgresses.ToArray();
        pendingProgresses.Clear();

        IProgress<float>? combinedProgress = CreateCombinedProgress(progresses);

        try
        {
            await reload(combinedProgress);
        }
        catch (Exception exception)
        {
            (exceptions ??= []).Add(exception);
        }
    }
    while (pendingReload != null);

    completionSource = null;

    if (exceptions == null)
        source.TrySetResult();
    else if (exceptions.Count == 1)
        source.TrySetException(exceptions[0]);
    else
        source.TrySetException(new AggregateException(exceptions));
}
```

실제 구현은 예상하지 못한 내부 예외가 발생해도 `completionSource`가 영구히 남지 않도록 상태 정리를 보장해야 한다.

## 진행률 계약

진행률 객체는 작업 델리게이트와 다르게 처리한다.

- 최신 작업만 실행한다.
- 실행되지 않는 요청의 진행률 객체도 다음 실제 패스에 참가한다.
- 한 패스에 참가한 진행률 객체는 그 패스가 끝나면 제거한다.
- 완료된 진행률 객체를 후속 패스에 재사용하지 않는다.
- 현재 패스 실행 중 새로 등록된 진행률 객체는 현재 패스의 중간 진행률을 받지 않는다.
- 새 진행률 객체는 다음 패스의 `0`부터 `1`까지 받는다.
- 동일한 인스턴스가 같은 대기 패스에 여러 번 전달되면 한 번만 보고한다.
- 진행률 객체의 동일성 비교는 `Equals`가 아닌 `ReferenceEquals`를 사용한다.

예시:

```text
A(progressA) 실행
B(progressB), C(progressC) 요청

A 패스:
  progressA만 0 → 1 수신

최종 C 패스:
  progressB, progressC만 0 → 1 수신

progressA는 C 패스의 진행률을 받지 않음
```

추가 요청 예시:

```text
A(progressA) 실행
B(progressB), C(progressC) 요청
C 패스 실행 중 D(progressD) 요청

A 패스:
  progressA만 수신

C 패스:
  progressB, progressC만 수신

D 패스:
  progressD만 수신
```

`progressA`가 `1`을 받은 뒤에도 A의 `Run` 반환 작업은 배치 전체가 끝날 때까지 완료되지 않을 수 있다. 진행률 수명은 해당 패스까지이며, `Run` 작업의 수명은 전체 배치까지다.

## 결합 진행률

결합 진행률은 패스 시작 시점의 진행률 스냅샷만 보유한다.

```csharp
sealed class CombinedProgress : IProgress<float>
{
    readonly IProgress<float>[] progresses;

    public CombinedProgress(IProgress<float>[] progresses)
    {
        this.progresses = progresses;
    }

    public void Report(float value)
    {
        foreach (IProgress<float> progress in progresses)
            progress.SafeReport(value);
    }
}
```

생성 규칙:

- 진행률 객체가 없으면 `null`.
- 하나만 있으면 해당 객체를 직접 반환해도 된다.
- 둘 이상이면 `CombinedProgress`를 생성한다.
- 한 진행률 객체가 예외를 던져도 다른 객체와 리로드 작업에 영향을 주지 않도록 `SafeReport`를 사용한다.

## 예외 계약

- 한 패스의 예외가 대기 중인 최종 패스를 제거하면 안 된다.
- 패스에서 예외가 발생해도 다음 요청이 있으면 다음 패스를 실행한다.
- 배치 중 발생한 예외는 보관한다.
- 예외가 하나면 해당 예외로 공유 완료 소스를 실패시킨다.
- 예외가 여러 개면 `AggregateException`으로 공유 완료 소스를 실패시킨다.
- 예외가 없으면 공유 완료 소스를 성공시킨다.
- 완료 소스를 신호하기 전에 게이트 상태를 유휴 상태로 초기화한다.
- 완료 연속 실행에서 새 `Run`이 호출되면 새 배치로 정상 시작할 수 있어야 한다.

개별 `ReloadCore`가 기존처럼 자체 오류 로깅 및 복구를 수행한다면 처리된 예외를 다시 던질 필요는 없다.

## 재진입 계약

동일한 게이트가 실행하는 리로드 내부에서 같은 게이트의 `Run`을 호출하고 그 결과를 `await`하면 안 된다.

```csharp
async UniTask ReloadCore(IProgress<float>? progress)
{
    // 금지: 현재 패스가 자신이 끝나기를 기다리는 배치 완료를 대기함
    await reloadGate.Run(ReloadCore, progress);
}
```

이는 자기 자신을 기다리는 교착 상태를 만든다.

다음 형태는 현재 패스를 기다리지 않으므로 새 패스를 요청하는 용도로 사용할 수 있다.

```csharp
reloadGate.Run(ReloadCore, progress).Forget();
```

다만 일반 구현에서는 리로드 내부가 직접 재요청하기보다 외부 이벤트나 상태 변경 진입점이 `Run`을 호출하도록 구성한다.

## 인자 캡처

`progress` 이외의 인자는 최신 `reload` 델리게이트가 캡처한다.

```csharp
public UniTask Reload
(
    IEnumerable<ResourcePack> resourcePacks,
    IProgress<float>? progress = null
)
{
    ResourcePack[] snapshot = resourcePacks.ToArray();
    return reloadGate.Run
    (
        passProgress => ReloadCore(snapshot, passProgress),
        progress
    );
}
```

지연 실행 시 변경될 수 있는 `IEnumerable`, 컬렉션, 임시 상태는 `Run` 호출 전에 필요한 형태로 스냅샷해야 한다. 새 요청이 기존 요청을 덮으면 최신 요청이 캡처한 인자를 사용한다.

## 적용 패턴

### 진행률이 있는 리로드

```csharp
readonly AsyncReloadGate reloadGate = new();

public bool isLoading => reloadGate.isRunning;

public UniTask Reload(IProgress<float>? progress = null)
    => reloadGate.Run(ReloadCore, progress);

async UniTask ReloadCore(IProgress<float>? progress)
{
    progress.SafeReport(0);

    try
    {
        // 전체 리로드 1회
    }
    finally
    {
        progress.SafeReport(1);
    }
}
```

### 진행률이 없는 리로드

```csharp
readonly AsyncReloadGate reloadGate = new();

public UniTask Reload()
    => reloadGate.Run(ReloadCore);

async UniTask ReloadCore()
{
    // 전체 리로드 1회
}
```

## ResourceManager 적용 규칙

현재 `ResourceManager.Reload` 내부의 `reloadRequested`와 `while (reloadRequested)` 구조는 제거한다.

공개 `Reload`는 게이트 진입점만 담당한다.

```csharp
static readonly AsyncReloadGate reloadGate = new();

public static bool isLoading => reloadGate.isRunning;

public static UniTask Reload(IProgress<float>? progress = null)
    => reloadGate.Run(ReloadCore, progress);
```

`ReloadCore`는 기존 공개 `Reload` 전체를 정확히 한 번 수행해야 한다.

다음 항목은 반드시 `ReloadCore` 안에 있어야 하며 각 패스마다 다시 실행돼야 한다.

- 새 `AsyncTask` 생성
- `reloadStartEvent`
- 진행률 `0` 보고
- `ResourcePack.ReloadAll`
- 전체 레지스트리 리로드
- 진행률 `1` 보고
- `AsyncTask` 진행률 완료 및 정리
- `isPreloaded` 갱신
- `preReloadCompletionEvent`
- `reloadCompletionEvent`

따라서 첫 패스 실행 중 재요청이 들어오면 첫 패스의 완료 이벤트까지 모두 끝난 뒤, 새 `AsyncTask`와 시작 이벤트부터 후속 패스를 다시 시작한다.

## 다른 리로드 적용 규칙

다음 리로드 진입점도 동일한 `Reload`/`ReloadCore` 분리 패턴을 사용한다.

- `SimpleAssetRegistry.Reload`
- `SoundAssetRegistry.Reload`
- `LanguageAssetRegistry.Reload`
- `ResourcePack.Reload`
- `ResourcePack.ReloadAll`
- `WaveAudioSource.Reload`

개별 클래스의 수명주기와 정리 책임은 게이트로 옮기지 않는다.

예:

- `ResourcePack`의 `isDisposed` 검사
- `WaveAudioSource` 파괴 전후 검사
- 비동기 로드 후 생성된 스코프의 폐기
- `BeginTracking`과 `EndTracking`
- 각 패스의 진행률 `0` 및 `1`

게이트는 오직 패스 실행, 최신 요청 병합, 패스별 진행률 배정, 배치 완료 대기만 담당한다.

## 금지 사항

- 모든 요청을 `Queue`에 넣어 하나씩 실행하지 않는다.
- 실행 중인 리로드를 취소하지 않는다.
- 현재 요청을 새 요청으로 즉시 교체하지 않는다.
- 이전 패스의 진행률 객체를 후속 패스에 전달하지 않는다.
- 대기 작업이 덮였다는 이유로 해당 요청의 진행률 객체까지 폐기하지 않는다.
- `WaitWhile`, 프레임 폴링으로 완료를 기다리지 않는다.
- 메인 스레드 전용 계약에 불필요한 `lock`을 추가하지 않는다.
- 게이트가 이벤트나 리소스 수명주기를 직접 관리하지 않는다.
- 기존 동작과 다른 변경을 묵시적으로 섞지 않는다.

## 수용 조건

구현 후 최소한 다음 시나리오가 성립해야 한다.

1. 단일 호출은 리로드를 정확히 한 번 실행한다.
2. 실행 중 요청이 없으면 추가 패스를 실행하지 않는다.
3. A 실행 중 B와 C가 요청되면 A 다음에 C만 실행한다.
4. C 실행 중 D가 요청되면 C 다음에 D를 실행한다.
5. B와 C의 진행률 객체는 C 패스를 함께 추적한다.
6. A의 진행률 객체는 C 패스의 보고를 받지 않는다.
7. 동일 진행률 인스턴스를 B와 C에 전달해도 C 패스에서 한 번만 보고한다.
8. 모든 호출자의 반환 작업은 배치가 유휴 상태가 된 뒤 완료된다.
9. 첫 패스가 실패해도 대기 요청이 있으면 최종 패스를 실행한다.
10. 패스별 이벤트와 진행률이 매번 처음부터 다시 실행된다.
11. 동기적으로 완료되는 리로드 델리게이트도 정상 처리한다.
12. 완료 연속 실행에서 다시 호출해도 새 배치가 정상 시작된다.

## 검증 제한

이 Unity/C# 프로젝트에서는 검증 목적으로 다음 명령을 실행하지 않는다.

- `dotnet build`
- `dotnet test`
- 생성된 `.csproj` 또는 `.sln` 빌드
- 명시적으로 요청되지 않은 문법 전용 컴파일

변경 파일 직접 검토, 호출부 검색, API 사용 확인, `git diff --check` 같은 경량 검증을 사용한다.
