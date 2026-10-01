# Package management foundation

하나의 bootstrap UPM package 내부에 세 assembly를 둡니다. 기존 `com.rumi.runios` Installer는 변경하지 않습니다. 새 package는 RuniOS Core, TMP, NuGet, JSON library 등의 package dependency를 요구하지 않습니다.

| Assembly | 현재 구현 | 의존성 |
|---|---|---|
| `RuniOS.PackageManagement` | Immutable 선언/graph/plan, provider 등록, 후보 탐색, passive planning | BCL |
| `RuniOS.PackageManagement.Unity.Editor` | 변경 없는 프로젝트 target descriptor | Core, Unity Editor 환경 |
| `RuniOS.Installer.Editor` | Extension 진입점 검색 및 서비스 구성 | Core, Unity Editor adapter |

기존 UI의 RuniOS.Editor 언어 호환과 TMP 설치 설정 참조는 의도된 기능입니다. 새 bootstrap에는 해당 의존성을 옮기지 않았습니다. 후속 UI 연결에서 이 기능의 dependency boundary를 유지해야 합니다.

모든 assembly는 기존 Installer의 `csc.rsp`를 그대로 사용합니다. C# 14, nullable enable, nullable warnings as errors입니다. Records, `IsExternalInit`, `System.Collections.Immutable` 등의 추가 선행 의존성은 사용하지 않습니다. Core는 Editor에서 compile되지만 Unity namespace, Unity 참조, `#if UNITY_EDITOR`가 없습니다.

## 입력과 결과

`ResolutionRequest`는 유지할 모든 root requirements와 명시적 source overrides를 포함합니다. `PackageRequirement.SourceHint`는 기본 출처입니다. 같은 ID의 출처 힌트가 충돌하면 실패하며, request의 명시적 source binding으로 재지정할 수 있습니다. ID와 source key는 ordinal 비교하며 canonicalization은 adapter 책임입니다.

`PackageSource.Key`는 canonical package location입니다. Revision 선택은 constraint에 둡니다. Catalog가 반환하는 `PackageIdentity.SourceKind/SourceKey`는 query와 일치해야 합니다. `Revision`은 이미 확정된 opaque identity이며 symbolic Git ref를 그대로 사용해서는 안 됩니다. `Version`은 선택 제약에 쓸 수 있는 선택적 metadata이며 content identity equality에는 포함되지 않습니다.

`PackageResolver`는 한 graph에서 ID당 하나의 선택을 유지합니다. 후보는 catalog 선호 순서로 탐색합니다. 새 dependency의 제약이 앞선 선택을 무효화하면 후보 조합을 되돌립니다. Candidate metadata는 resolution 호출 안에서 exact identity별로 cache합니다. 불완전한 candidate 검색, 예산 초과, 출처 충돌, 중복 exact identity, metadata 불일치, 제약 충돌, dependency cycle은 diagnostics입니다. 실패한 branch의 diagnostics는 성공 branch에 섞지 않습니다.

현재 기본 evaluator는 `ExactRevisionConstraint`뿐입니다. SemVer 범위, Git ref 해석, catalog/source implementations는 아직 없습니다. 검색 계약은 전체 후보 조합 탐색을 지원하므로 새 evaluator를 추가하기 위해 resolver 분기를 수정할 필요가 없습니다. Catalog와 metadata는 같은 exact revision의 일관된 snapshot을 제공해야 합니다.

`ResolvedPackageGraph`는 Core resolver 내부에서만 생성합니다. 모든 node와 edge가 해석되고 제약이 만족되며 cycle이 없어야 합니다. Root requirements, 선언 위치, dependency edges 및 dependency-first order를 보존합니다. UI와 adapter는 graph를 읽기 전용으로 소비합니다.

모델의 collection은 방어적으로 복사합니다. `PackageSource`, constraint, artifact, installation, environment fact, operation의 extension payload 역시 불변이어야 합니다. Mutable JSON DOM, Unity object, provider instance, 실행 callback을 graph나 plan에 넣지 않습니다.

## Provider boundary

| Capability | 책임 |
|---|---|
| `IPackageCatalogProvider` | Source별 후보 identity와 exact metadata 조회 |
| `IPackageConstraintEvaluator` | Source와 독립적인 선택 제약 평가 |
| `IPackageContentProvider` | 실행 단계에서 artifact를 필요한 representation으로 획득 |
| `IInstalledStateProvider` | Target의 실제 사용 패키지 및 환경 snapshot 관측 |
| `IInstallationPlanningProvider` | 설치 표현별 passive operations와 package boundaries 생성 |
| `IPlanFragmentComposer` | 공유 환경 변경 병합과 operation references/boundaries 갱신 |
| `IInstallOperationExecutor` | 주입된 환경 서비스로 operation payload 실행 |

`PackageManagementBuilder`는 capability key 중복을 diagnostics로 거부합니다. 등록 순서로 중복 provider를 임의 선택하지 않습니다. Composer 실행 순서만 등록 순서로 명시됩니다. Build 후 builder를 수정해도 생성한 registry에는 영향을 주지 않습니다. Provider 자체의 cache와 thread-safety는 구현체 책임입니다.

`PackageQuery.Session`은 같은 해석 호출에서 공유하는 수명 토큰입니다. Git provider 등은 이를 약한 cache key로 사용하여 같은 repository/ref를 일관된 snapshot으로 조회할 수 있습니다. Core에 provider scratch state나 service locator는 넣지 않습니다. Core는 await 이후 호출자의 synchronization context 유지도 보장하지 않으므로 Unity API provider는 필요한 thread 전환을 직접 처리해야 합니다.

Core registry와 resolver는 discovery framework를 참조하지 않습니다. Frontend의 `InstallerPackageManagement.CreateServices()`가 `TypeCache`로 concrete `IPackageManagementExtension` entry points를 찾아 등록합니다. 자동 검색 진입점에는 public parameterless constructor가 필요합니다. 수동 DI나 CLI에서는 extension instance를 builder에 직접 등록할 수 있습니다. 새로운 declaration codec은 후속 adapter parser registry의 확장 지점으로 구현해야 하며 Core에 source별 parser switch를 추가하면 안 됩니다.

## Source와 installation

Source와 installation은 서로 다른 passive descriptors입니다. 같은 graph를 다른 installation bindings로 계획할 수 있습니다. Git UPM dependency는 확정 repository location/path/revision을 제공하는 artifact가 필요합니다. Embedded 설치는 `package-directory` representation을 획득할 수 있는 content provider를 사용하면 Git, registry, local source와 재사용 가능합니다. Acquisition과 materialization은 planning 중 실행하지 않습니다.

RuniOS Git catalog의 후속 기본 정책은 동일 repository 의존성에 owner의 확정 commit을 적용하는 것입니다. Core에는 이 정책을 넣지 않습니다. 다른 배포처로 전환할 때 논리 ID를 유지하고 source binding과 해당 선언을 바꿀 수 있습니다.

Generated NuGet UPM package는 일반 UPM package로 관측하고 설치합니다. NuGet 원본 provenance, restore, framework filtering, DLL materialization은 개발/CI tooling 영역입니다.

## Installed state와 ownership

`InstalledPackageGraph.Create()`에는 환경 우선순위를 적용한 effective observations를 전달합니다. Manifest entry와 Embedded package를 각각 다른 effective package로 넣으면 안 됩니다. 같은 effective ID의 중복은 diagnostic입니다. 알 수 없는 revision은 `Identity == null`, 불완전한 관측은 `IsComplete == false`로 표현합니다.

Planner는 불완전한 관측과 desired package의 알 수 없는 installed identity를 차단합니다. 외부 package가 이미 desired identity/installation과 일치하면 그대로 사용할 수 있습니다. 변경하려면 `AllowAdoption`을 명시해야 합니다. 이 옵션은 변경 허용이며, 일치하는 외부 package를 자동으로 소유하는 기능은 아닙니다.

`RemoveUnusedOwnedPackages`의 기본값은 false입니다. 켜더라도 현재 `ManagementOwner`와 일치하는 항목만 제거합니다. Desired graph는 유지할 모든 roots의 closure여야 합니다. 외부 또는 유지하는 package가 제거 후보를 여전히 요구하면 실패합니다. 기존 legacy Installer의 manifest 항목은 receipt 없이 소유권을 자동 추정하지 않습니다.

`InstalledPackage.Dependencies`는 effective dependency IDs, `DependencyRequirements`는 관측한 선언입니다. 선언 ID는 effective IDs에도 포함됩니다. Desired graph 밖에서 유지하는 package가 변경할 dependency를 사용하면 그 선언의 제약을 다시 평가합니다. 선언을 관측하지 못했거나 평가기를 제공하지 못하면 호환성을 추측하지 않고 diagnostic으로 차단합니다. 변경하지 않는 dependency에는 이 추가 검증을 요구하지 않습니다.

## Passive plan과 순서

Desired graph의 모든 package에는 explicit installation binding이 필요합니다. 현재 상태와 동일한 package는 operation을 생성하지 않습니다. 변경은 Install, Update, Remove, ChangeInstallation으로 분류합니다. 설치 표현 변경에는 기존 표현 제거와 새 표현 설치의 두 phase를 제공합니다.

Provider는 `InstallChanges`, `RemoveChanges`와 전체 immutable planning request를 받습니다. 패키지 diff가 없어도 원하는 설치 종류의 provider를 호출하므로 registry 설정 등 package 외의 공유 환경 상태도 검증하고 계획할 수 있습니다. 각 package 전이에 대해 non-empty `PackageOperationBoundary`를 반환해야 합니다. Start operations는 target 변경의 시작, completion operations는 해당 phase 완료입니다. 각 시작과 완료는 operation prerequisite 경로로 연결되어야 합니다. 원자적 공유 operation 하나가 여러 package boundary에 포함될 수 있습니다.

Core는 변경한 dependency의 install completion 이후 dependent 설치를 배치합니다. 기존 package 제거는 dependent의 제거 또는 dependency를 해제하는 update 이후에 배치합니다. 설치 방식 변경은 기존 표현 제거 후 새 표현 설치로 연결합니다. 환경에서 단일 원자적 변경으로 교체해야 한다면 composer가 두 phase를 같은 operation으로 병합할 수 있습니다.

Composer는 manifest 등 공유 변경을 병합한 뒤 모든 prerequisite references와 package boundaries를 갱신해야 합니다. Core는 operation ID 중복, 누락된 references, 미구현 phase, 불필요한 package phase, 연결되지 않은 boundaries, operation cycle, 순서 없는 read/write 및 write/write resource 충돌을 검사합니다. Operation 순서는 결정적입니다.

Resource key의 도메인 의미와 path/scope 중첩은 Core가 해석하지 않습니다. Adapter가 canonical keys와 겹침을 표현하고 semantic conflicts를 진단해야 합니다. 같은 resource의 순서 있는 작업이 의미적으로 호환되는지도 adapter 책임입니다.

Plan은 observed resource fingerprints를 보존하지만 실행하거나 fingerprint를 재검사하지 않습니다. 실제 executor/runner 구현에서 변경 대상의 preconditions를 충분히 수집하고 실행 직전 확인해야 합니다. Execution receipts는 향후 복구의 연결점이며 자동 rollback 또는 transaction 보장을 제공하지 않습니다.

## 현재 범위와 다음 단계

현재 실제 구현은 Core와 extension bootstrap입니다. Git/registry/local providers, Unity installed-state discovery, metadata serialization, manifest mutation, package acquisition, execution runner, UI 연결, NuGet restore, CLI는 구현하지 않았습니다. 등록되지 않은 source/installation/observation은 structured diagnostics로 실패합니다.

다음 구현은 read-only RuniOS Git catalog와 Unity installed-state provider, 이후 Unity Git planning/manifest composition입니다. Git dependency는 root뿐 아니라 전체 closure를 manifest에 표현해야 합니다. 기존 Installer 제거와 다른 package의 metadata 변경은 별도 단계입니다.

## Verification boundary

이번 변경은 별도 clone/branch에서 작성했습니다. 기존 project나 package 파일은 수정하지 않았습니다. SDK project, `dotnet build/test`, syntax-only compile, Unity reimport는 실행하지 않았습니다. 정적 확인은 asmdef dependency, 동일 csc.rsp, Core 금지 참조, metadata와 GUID, 변경 범위, diff 및 코드의 분기/순서 검토입니다. Compile/runtime 성공을 의미하지 않습니다.

후속 Unity 환경 검증 대상은 exact selection, diamond closure, 상충 제약, cycle path, 후보 backtracking, registry 중복, unknown installed identity, 외부 ownership, 설치/제거 순서, cross-provider replacement, 공유 원자적 operation, resource 충돌, composer remapping 및 cancellation입니다.
