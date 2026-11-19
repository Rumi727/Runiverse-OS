# 1. 수명 주기 훅

> 기준 커밋: `1f1bb93d`. [← 목차](README.md) · 다음: [2. `BootLoader`](02-bootloader.md)

core에는 `Main` 같은 시작점이 없습니다. 각 클래스가 Unity의 수명 주기 특성을 붙인 정적 메서드를 두고, Unity가 정한 시점에 그 메서드가 불리며 필요한 것을 준비합니다. 이 장은 core의 어느 클래스가 어떤 훅에서 무엇을 하는지 한곳에 모읍니다.

- [1.1 훅의 뜻](#11-훅의-뜻)
- [1.2 core가 쓰는 훅](#12-core가-쓰는-훅)
- [1.3 Play 모드 진입 시 코드 리로드 끔](#13-play-모드-진입-시-코드-리로드-끔)
- [1.4 확인하지 않은 것](#14-확인하지-않은-것)

## 1.1 훅의 뜻

프로젝트는 Unity `7000.0.0a16`을 씁니다(`ProjectSettings/ProjectVersion.txt`). 아래 설명은 이 버전 에디터에 들어 있는 XML 문서(`UnityEngine.xml`, `UnityEditor.CoreModule.xml`)와 Unity 매뉴얼의 [Code reload and the code lifecycle](https://docs.unity.com/en-us/engine/6000.7/manual/programming-environment/code-reload-serialization/code-reload/code-lifecycle)을 요약한 것입니다.

| 훅 (네임스페이스) | 언제 불리나 | 빌드에서 |
| --- | --- | --- |
| `[OnAssemblyLoaded]` / `[OnAssemblyUnloading]` (`Unity.Scripting.LifecycleManagement`) | 그 메서드가 있는 어셈블리를 로드한 뒤 / 언로드하기 전 | 확인하지 않음 |
| `[OnCodeLoaded]` / `[OnCodeUnloading]` (같은 곳) | 모든 어셈블리를 로드하고 초기화한 뒤(첫 로드, 에디터에서는 코드 리로드 뒤에도) / 코드 리로드로 언로드하기 전. 해제 쪽은 등록의 역순으로 불림 | 첫 로드 때 불림 |
| `[OnCodeInitializing]` / `[OnCodeDeinitializing]` (같은 곳) | 코드 리로드 뒤 관리 객체를 복원한 다음, `Awake`·`OnEnable` 전 / 리로드를 위해 관리 객체를 직렬화하기 전. 해제 쪽은 역순 | 확인하지 않음 |
| `[OnEnteringPlayMode]` / `[OnExitingPlayMode]` (`UnityEngine`) | 에디터에서 Play 모드에 들어갈 때·Play 모드 중 코드 리로드 뒤 / Play 모드를 나갈 때·Play 모드 중 코드 리로드 전 | 앱 시작 때 / 앱 종료 때 불림 |
| `[OnEnteringEditMode]` / `[OnExitingEditMode]` (`UnityEditor.Scripting.LifecycleManagement`) | Edit 모드로 돌아올 때·Edit 모드에서 코드 리로드 뒤 / Play 모드에 들어가기 전·코드 리로드 전 | 없음 (에디터 전용) |
| `[RuntimeInitializeOnLoadMethod(…)]` (`UnityEngine`) | 지정한 `RuntimeInitializeLoadType` 시점 (`SubsystemRegistration`, `BeforeSceneLoad` 등) | 불림 |
| `[InitializeOnLoad]` (`UnityEditor`) | 에디터가 코드를 로드할 때 정적 생성자 실행 | 없음 |
| `[ModuleInitializer]` (`System.Runtime.CompilerServices`) | C# 모듈 초기화자. [3장](03-milestones.md)의 마일스톤 생성기가 씀 | — |

"빌드에서" 열은 매뉴얼이 빌드를 언급한 것만 적었습니다. 매뉴얼은 `OnCodeLoaded`(첫 코드 로드 때), `OnEnteringPlayMode`(앱 시작 때), `OnExitingPlayMode`(앱 종료 때)만 빌드에서 불린다고 명시합니다. `OnAssemblyLoaded`와 `OnCodeInitializing`이 빌드에서 언제 불리는지는 매뉴얼에서 찾지 못했습니다. core는 빌드에서도 `OnAssemblyLoaded`로 서비스를 등록하고 `OnCodeInitializing`으로 Missing Texture를 만듭니다.

`[AutoStaticsCleanup]`은 훅이 아니라 표시입니다. Unity XML의 요약은 "코드 리로드가 꺼진 상태로 Play 모드에 들어가거나 나갈 때 정적 변수를 자동으로 초기화한다"입니다.

## 1.2 core가 쓰는 훅

표의 줄 순서는 실행 순서가 아닙니다([1.4절](#14-확인하지-않은-것)).

**런타임 (`Runtime/`)**

| 클래스 | 훅 | 하는 일 | 자세히 |
| --- | --- | --- | --- |
| [`UnityThread`](../../Runtime/Threading/UnityThread.cs#L12) | `OnAssemblyLoaded` | 그 스레드의 id를 `mainThreadId`로 기록 | 유틸리티 (예정) |
| [`InfiniteLoopDetector`](../../Runtime/InfiniteLoopDetector.cs#L37) | `OnAssemblyLoaded` / [`OnAssemblyUnloading`](../../Runtime/InfiniteLoopDetector.cs#L48) | 프레임마다 감지 카운터를 0으로 돌리는 함수를 구독. 에디터와 `UNITY_ENABLE_CHECKS` 빌드에서만 컴파일 | 유틸리티 (예정) |
| [`PlayerStreamingAssetsService`](../../Runtime/IO/StreamingAssets/Player/PlayerStreamingAssetsService.cs#L35) | `OnAssemblyLoaded` / `OnAssemblyUnloading` | 서비스 등록 / 해제 | [IO 7.2](../IO/07-streaming-assets.md#72-환경별-구현) |
| [`AndroidStreamingAssetsService`](../../Runtime/IO/StreamingAssets/Android/AndroidStreamingAssetsService.cs#L32) | `OnAssemblyLoaded` / `OnAssemblyUnloading` | 서비스 등록 / 해제 | [IO 7.2](../IO/07-streaming-assets.md#72-환경별-구현) |
| [`AndroidStreamingIOProvider`](../../Runtime/IO/StreamingAssets/Android/AndroidStreamingIOProvider.cs#L20) | `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` | Java `AssetManager`를 얻어 둠 | [IO 8.1](../IO/08-android-assets.md#81-assetmanager-얻기) |
| [`LanguageAssetRegistry`](../../Runtime/Resource/Languages/LanguageAssetRegistry.cs#L24) | `OnCodeLoaded` / `OnCodeUnloading` | 언어 레지스트리 등록 / 해제 | [Resource 10.1](../Resource/10-languages-and-json.md#101-languageassetregistry) |
| [`ResourceUtility`](../../Runtime/Utility/ResourceUtility.cs#L93) | `OnCodeInitializing` / [`OnCodeDeinitializing`](../../Runtime/Utility/ResourceUtility.cs#L114) | "Missing Texture"와 색 머티리얼 생성 / 파괴 | 유틸리티 (예정) |
| [`Kernel`](../../Runtime/Kernel.cs#L53) | `OnEnteringPlayMode` / [`OnExitingPlayMode`](../../Runtime/Kernel.cs#L64) | 시간 갱신, `Application.quitting`, 일시정지 이벤트 구독 / 해제 | [4장](04-kernel.md) |
| 〃 | [`OnEnteringEditMode`](../../Runtime/Kernel.cs#L76) / [`OnExitingEditMode`](../../Runtime/Kernel.cs#L79) | 에디터 update에 시간 갱신 구독 / 해제 | [4장](04-kernel.md) |
| [`DisposeQueue`](../../Runtime/Resource/DisposeQueue.cs#L22) | `OnEnteringPlayMode` / [`OnExitingPlayMode`](../../Runtime/Resource/DisposeQueue.cs#L25) | `PostLateUpdate`에 정리 루프 구독 / 해제하고 남은 작업 모두 실행 | [Resource 7.4](../Resource/07-handles-and-scopes.md#74-disposequeue) |
| 〃 | [`OnEnteringEditMode`](../../Runtime/Resource/DisposeQueue.cs#L33) / [`OnExitingEditMode`](../../Runtime/Resource/DisposeQueue.cs#L40) | 에디터 update와 에디터 종료에 구독 / 해제하고 남은 작업 모두 실행 | 〃 |
| [`BootLoader`](../../Runtime/Booting/BootLoader.cs#L18) | `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` | 리소스 리로드, 그다음 `[OnResourcesReady]` 호출 | [2장](02-bootloader.md) |
| 생성된 `__RuniModuleInitialization` | `[ModuleInitializer]` | 그 어셈블리의 `[OnResourcesReady]` 메서드를 `MilestoneDispatcher`에 등록 | [3장](03-milestones.md) |

**에디터 (`Editor/`)**

| 클래스 | 훅 | 하는 일 | 자세히 |
| --- | --- | --- | --- |
| [`EditorStreamingAssetsService`](../../Editor/EditorStreamingAssetsService.cs#L112) | `OnAssemblyLoaded` / `OnAssemblyUnloading` | 서비스 등록 / 해제 | [IO 7.3](../IO/07-streaming-assets.md#73-에디터-프로젝트와-패키지-겹치기) |
| [`EditorResourcePack`](../../Editor/EditorResourcePack.cs#L11) | `OnAssemblyLoaded` / `OnAssemblyUnloading` | `runios:editor` 팩 생성 / `Dispose` | [Resource 2.5](../Resource/02-resource-packs.md#25-기본-팩과-에디터-팩) |
| [`ScriptingDefineSymbolSetter`](../../Editor/ScriptingDefineSymbolSetter.cs#L6) | `[InitializeOnLoad]` | 모든 빌드 대상에 `RUNI_ENGINE` 심볼이 없으면 추가 | — |
| [`ResourceReloadPostprocessor`](../../Editor/ResourceReloadPostprocessor.cs#L9) | `AssetPostprocessor.OnPostprocessAllAssets` (특성 아님) | 도메인 리로드 직후와 `StreamingAssets` 변경 때 리소스 리로드 | [Resource 4.5](../Resource/04-reload-pipeline.md#45-누가-리로드를-시작하나) |

표를 종류별로 묶으면 이렇습니다.

- **서비스 등록, 에디터 팩, 메인 스레드 id**는 `OnAssemblyLoaded`에서 합니다.
- **`AssetRegistryManager`에 레지스트리 등록**은 `OnCodeLoaded`에서 합니다. 다른 패키지의 레지스트리도 같습니다([Resource 11장](../Resource/11-package-registries.md)).
- **프레임마다 도는 구독**(시간 갱신, 정리 루프)은 Play 모드와 Edit 모드에 들어가고 나올 때 붙였다 뗍니다. 다음 절의 설정에서는 이 구독이 Play 모드에 들어갈 때마다 새로 걸립니다.

## 1.3 Play 모드 진입 시 코드 리로드 끔

`ProjectSettings/EditorSettings.asset`은 `m_EnterPlayModeOptionsEnabled: 1`, `m_EnterPlayModeOptions: 3`입니다. 값 3은 도메인(코드) 리로드와 씬 리로드를 모두 끈다는 뜻입니다. 그래서 에디터에서 Play 모드에 들어갈 때 다음과 같습니다.

- 어셈블리를 다시 로드하지 않으므로 `OnAssemblyLoaded`·`OnCodeLoaded`·`[ModuleInitializer]`는 다시 불리지 않고, 그 결과(등록된 서비스, 레지스트리, 마일스톤 메서드)가 그대로 남습니다.
- 들어갈 때 `OnExitingEditMode`와 `OnEnteringPlayMode`, 나올 때 `OnExitingPlayMode`와 `OnEnteringEditMode`가 불립니다(Unity 매뉴얼의 설명대로 Edit 모드를 나간 뒤 Play 모드에 들어가고, Play 모드를 나간 뒤 Edit 모드로 돌아옵니다).
- `BeforeSceneLoad`의 [`BootLoader.Boot`](02-bootloader.md)는 Play 모드에 들어갈 때마다 다시 돌고, 남아 있는 마일스톤 메서드를 다시 부릅니다. 코드 리로드가 꺼진 상태에서도 `RuntimeInitializeOnLoadMethod`가 Play 모드 진입마다 불린다는 전제이며, 이 문서에서 직접 확인하지는 않았습니다.
- 정적 필드는 다음 Play 세션까지 값이 남습니다. 저장소에서 `[AutoStaticsCleanup]`이 붙은 곳은 [`DisposeQueue.shutdownStarted`](../../Runtime/Resource/DisposeQueue.cs#L19) 하나뿐입니다. 나머지 정적 상태는 위 훅에서 직접 구독을 풀거나 상태를 돌립니다.

core의 [`csc.rsp`](../../Runtime/csc.rsp)는 `-nowarn:UAL0010,UAL0013`으로 두 경고를 끕니다.

## 1.4 확인하지 않은 것

- **훅 사이의 순서.** 예를 들어 `OnAssemblyLoaded`, `[ModuleInitializer]`, `OnCodeLoaded`, `OnEnteringPlayMode`, `RuntimeInitializeOnLoadMethod`의 각 시점이 서로 어떤 순서인지는 확인하지 않았습니다. Unity 매뉴얼의 [Domain and scene reload execution order reference](https://docs.unity.com/en-us/engine/6000.7/manual/programming-environment/code-reload-serialization/code-reload/code-reloading-editor/configurable-enter-play-mode-details)가 그림으로 순서를 보여 주지만, 이 문서는 그 그림에 기대지 않습니다. 순서에 기대는 곳은 각 장에 "순서는 확인하지 않음"으로 표시했습니다.
- **`[ModuleInitializer]`가 실행되는 시점.** C# 모듈 초기화자는 그 모듈에 처음 접근하기 전에 실행됩니다. Unity가 어셈블리를 로드할 때 바로 실행하는지는 확인하지 않았습니다([3.4절](03-milestones.md#34-언제-등록되나)).
- **`OnEnteringPlayMode` 시점의 `Application.isPlaying`.** [`RuniPlayerLoop.Register`](../../Runtime/LowLevel/RuniPlayerLoop.cs#L102)는 `Kernel.isPlaying`이 `false`면 예외를 던집니다. 에디터에서 `Kernel`과 `DisposeQueue`가 `OnEnteringPlayMode`에서 구독할 때 이 값이 이미 `true`인지는 확인하지 않았습니다. 빌드에서는 `Kernel.isPlaying`이 상수 `true`라 문제가 되지 않습니다.
