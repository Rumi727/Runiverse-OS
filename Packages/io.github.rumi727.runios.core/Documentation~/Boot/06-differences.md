# 6. 차이와 기존 문서

> 기준 커밋: `1f1bb93d`. [← 목차](README.md) · 이전: [5. `ServiceRegistry`](05-service-registry.md)

부팅·커널 영역에는 기존 README가 없습니다. 이 장은 XML 주석이 현재 코드와 다른 곳, 이 문서에 "관찰된 동작"과 "확인하지 않음"으로 적은 항목, 사용처가 없는 공개 멤버를 모읍니다. 코드와 XML 주석은 고치지 않았습니다.

- [6.1 XML 주석과 다른 곳](#61-xml-주석과-다른-곳)
- [6.2 관찰된 동작 목록](#62-관찰된-동작-목록)
- [6.3 확인하지 않은 것](#63-확인하지-않은-것)
- [6.4 사용처가 없는 공개 멤버](#64-사용처가-없는-공개-멤버)

## 6.1 XML 주석과 다른 곳

| 위치 | 주석 | 코드 |
| --- | --- | --- |
| [`Kernel.quitting`](../../Runtime/Kernel.cs#L49) (45행) | 플레이 모드 해제 시 이벤트가 자동으로 초기화 | 비우는 곳은 `Quitting()` 안뿐이고, `OnExitingPlayMode`는 비우지 않습니다([4.4절](04-kernel.md#44-종료-quitting과-quit)). |

같은 주석의 "커널보다 먼저 실행되는 것을 보장"은 코드와 어긋나지 않습니다. `Quitting()`은 구독자를 먼저 부르고, 그 뒤에 올 커널의 정리 코드(115–119행)는 지금 주석 처리되어 있습니다.

## 6.2 관찰된 동작 목록

| 항목 | 절 |
| --- | --- |
| `smoothDeltaTime`이 평활되지 않아 `deltaTime`과 같음 | [4.2](04-kernel.md#42-프레임-시간) |
| `OnExitingPlayMode`가 `Application.quitting`보다 먼저 오면 `quitting` 구독자가 불리지도 비워지지도 않음 | [4.4](04-kernel.md#44-종료-quitting과-quit) |

## 6.3 확인하지 않은 것

| 항목 | 절 |
| --- | --- |
| 수명 주기 훅 사이의 실행 순서 | [1.4](01-lifecycle-hooks.md#14-확인하지-않은-것) |
| 빌드에서 `OnAssemblyLoaded`·`OnCodeInitializing`이 불리는 시점 | [1.1](01-lifecycle-hooks.md#11-훅의-뜻) |
| 코드 리로드를 끈 상태에서 `BeforeSceneLoad`가 Play 모드 진입마다 불림 | [1.3](01-lifecycle-hooks.md#13-play-모드-진입-시-코드-리로드-끔) |
| `[ModuleInitializer]`가 실행되는 시점과 `BootLoader.Boot`와의 순서 | [3.4](03-milestones.md#34-언제-등록되나) |
| 에디터에서 `OnEnteringPlayMode` 시점의 `Application.isPlaying` | [4.3](04-kernel.md#43-시간-갱신의-구독) |
| 에디터에서 `OnExitingPlayMode`와 `Application.quitting`의 순서 | [4.4](04-kernel.md#44-종료-quitting과-quit) |

빌드에서 `[OnEnteringPlayMode]`(앱 시작 때)와 `[OnCodeLoaded]`(첫 코드 로드 때)가 불린다는 것은 Unity 매뉴얼로 확인했습니다([1.1절](01-lifecycle-hooks.md#11-훅의-뜻)).

## 6.4 사용처가 없는 공개 멤버

저장소 전체를 텍스트로 검색한 결과입니다.

| 멤버 | 비고 |
| --- | --- |
| `Kernel.rosVersion` | `0.0.0` 고정 |
| `Kernel.quitting`, `Kernel.Quit()`, `Kernel.Quit(int)` | 구독자·호출 없음 |
| `Kernel.isEditor`, `fixedDeltaTime`, `deltaTimeDouble`, `unscaledDeltaTimeDouble` | 외부 사용 없음 |
| `ServiceRegistry.SetDefault`, `GetDefault`, `GetServiceTypes`, `GetAll()` | 사용 없음 |
| `MilestoneDispatcher.Register` | 생성 코드만 부름 |
| `[OnResourcesReady]` | 붙은 메서드 없음 |
