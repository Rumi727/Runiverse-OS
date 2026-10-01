# Package management foundation

하나의 bootstrap UPM package 내부에 세 assembly를 둡니다. 기존 `com.rumi.runios` Installer는 변경하지 않습니다. 새 package는 RuniOS Core, TMP, NuGet, JSON library 등의 package dependency를 요구하지 않습니다.

| Assembly | 현재 구현 | 의존성 |
| --- | --- | --- |
| `RuniOS.PackageManagement` | Immutable 선언/graph/plan, provider 등록, 후보 탐색, passive planning | BCL |
| `RuniOS.PackageManagement.Unity.Editor` | Git/registry catalog, sidecar codec, 관측, manifest 계획 및 실행 | Core, Unity Editor 환경 |
| `RuniOS.Installer.Editor` | Extension 진입점 검색 및 서비스 구성 | Core, Unity Editor adapter |

기존 UI의 RuniOS.Editor 언어 호환과 TMP 설치 설정 참조는 의도된 기능입니다. 새 bootstrap에는 해당 의존성을 옮기지 않았습니다. 후속 UI 연결에서 이 기능의 dependency boundary를 유지해야 합니다.

모든 assembly는 기존 Installer의 `csc.rsp`를 그대로 사용합니다. C# 14, nullable enable, nullable warnings as errors입니다. Records, `IsExternalInit`, `System.Collections.Immutable` 등의 추가 선행 의존성은 사용하지 않습니다. Core는 Editor에서 compile되지만 Unity namespace, Unity 참조, `#if UNITY_EDITOR`가 없습니다.

## 입력과 결과

`ResolutionRequest`는 유지할 모든 root requirements와 명시적 source overrides를 포함합니다. `PackageRequirement.sourceHint`는 기본 출처입니다. 같은 ID의 출처 힌트가 충돌하면 실패하며, request의 명시적 source binding으로 재지정할 수 있습니다. ID와 source key는 ordinal 비교하며 canonicalization은 adapter 책임입니다.

`PackageSource.key`는 canonical package location입니다. revision 선택은 constraint에 둡니다. Catalog가 반환하는 `PackageIdentity.sourceKind/sourceKey`는 query와 일치해야 합니다. `revision`은 이미 확정된 opaque identity이며 symbolic Git ref를 그대로 사용해서는 안 됩니다. `version`은 선택 제약에 쓸 수 있는 선택적 metadata이며 content identity equality에는 포함되지 않습니다.

`PackageResolver`는 한 graph에서 ID당 하나의 선택을 유지합니다. 후보는 catalog 선호 순서로 탐색합니다. 새 dependency의 제약이 앞선 선택을 무효화하면 후보 조합을 되돌립니다. Candidate metadata는 resolution 호출 안에서 exact identity별로 cache합니다. 불완전한 candidate 검색, 예산 초과, 출처 충돌, 중복 exact identity, metadata 불일치, 제약 충돌, dependency cycle은 diagnostics입니다. 실패한 branch의 diagnostics는 성공 branch에 섞지 않습니다.

Core 기본 evaluator는 `ExactRevisionConstraint`입니다. Unity adapter는 `GitReferenceConstraint`와 `UpmVersionConstraint` 평가기를 추가합니다. 첫 Registry 구현은 정확한 버전만 선택하며 SemVer 범위와 dist-tag 자동 선택은 지원하지 않습니다. 검색 계약은 전체 후보 조합 탐색을 지원하므로 새 evaluator를 추가하기 위해 resolver 분기를 수정할 필요가 없습니다. Catalog와 metadata는 같은 exact revision의 일관된 snapshot을 제공해야 합니다.

`ResolvedPackageGraph`는 Core resolver 내부에서만 생성합니다. 모든 node와 edge가 해석되고 제약이 만족되며 cycle이 없어야 합니다. Root requirements, 선언 위치, dependency edges 및 dependency-first order를 보존합니다. UI와 adapter는 graph를 읽기 전용으로 소비합니다.

모델의 collection은 방어적으로 복사합니다. `PackageSource`, constraint, artifact, installation, environment fact, operation의 extension payload 역시 불변이어야 합니다. Mutable JSON DOM, Unity object, provider instance, 실행 callback을 graph나 plan에 넣지 않습니다.

## Provider boundary

| Capability | 책임 |
| --- | --- |
| `IPackageCatalogProvider` | Source별 후보 identity와 exact metadata 조회 |
| `IPackageConstraintEvaluator` | Source와 독립적인 선택 제약 평가 |
| `IPackageContentProvider` | 실행 단계에서 artifact를 필요한 representation으로 획득 |
| `IInstalledStateProvider` | Target의 실제 사용 패키지 및 환경 snapshot 관측 |
| `IInstallationPlanningProvider` | 설치 표현별 passive operations와 package boundaries 생성 |
| `IPlanFragmentComposer` | 공유 환경 변경 병합과 operation references/boundaries 갱신 |
| `IInstallOperationExecutor` | 주입된 환경 서비스로 operation payload 실행 |

`PackageManagementBuilder`는 capability key 중복을 diagnostics로 거부합니다. 등록 순서로 중복 provider를 임의 선택하지 않습니다. Composer 실행 순서만 등록 순서로 명시됩니다. Build 후 builder를 수정해도 생성한 registry에는 영향을 주지 않습니다. Provider 자체의 cache와 thread-safety는 구현체 책임입니다.

`PackageQuery.session`은 같은 해석 호출에서 공유하는 수명 토큰입니다. Git provider 등은 이를 약한 cache key로 사용하여 같은 repository/ref를 일관된 snapshot으로 조회할 수 있습니다. Core에 provider scratch state나 service locator는 넣지 않습니다. Core는 await 이후 호출자의 synchronization context 유지도 보장하지 않으므로 Unity API provider는 필요한 thread 전환을 직접 처리해야 합니다.

Core registry와 resolver는 discovery framework를 참조하지 않습니다. Frontend의 `InstallerPackageManagement.CreateServices()`가 `TypeCache`로 concrete `IPackageManagementExtension` entry points를 찾아 등록합니다. 자동 검색 진입점에는 public parameterless constructor가 필요합니다. 수동 DI나 CLI에서는 extension instance를 builder에 직접 등록할 수 있습니다. 새로운 declaration codec은 후속 adapter parser registry의 확장 지점으로 구현해야 하며 Core에 source별 parser switch를 추가하면 안 됩니다.

## Source와 installation

Source와 installation은 서로 다른 passive descriptors입니다. 같은 graph를 다른 installation bindings로 계획할 수 있습니다. Git UPM dependency는 확정 repository location/path/revision을 제공하는 artifact가 필요합니다. Embedded 설치는 `package-directory` representation을 획득할 수 있는 content provider를 사용하면 Git, registry, local source와 재사용 가능합니다. Acquisition과 materialization은 planning 중 실행하지 않습니다.

`runios.package.json`에서 같은 repository의 상대 path로 선언한 Git 의존성에는 owner의 확정 commit을 적용합니다. Core에는 이 정책을 넣지 않습니다. 다른 배포처로 전환할 때 논리 ID를 유지하고 source binding과 해당 선언을 바꿀 수 있습니다.

Generated NuGet UPM package는 일반 UPM package로 관측하고 설치합니다. NuGet 원본 provenance, restore, framework filtering, DLL materialization은 개발/CI tooling 영역입니다.

## Installed state와 ownership

`InstalledPackageGraph.Create()`에는 환경 우선순위를 적용한 effective observations를 전달합니다. Manifest entry와 Embedded package를 각각 다른 effective package로 넣으면 안 됩니다. 같은 effective ID의 중복은 diagnostic입니다. 알 수 없는 revision은 `identity == null`, 불완전한 관측은 `isComplete == false`로 표현합니다.

Planner는 불완전한 관측과 desired package의 알 수 없는 installed identity를 차단합니다. 외부 package가 이미 desired identity/installation과 일치하면 그대로 사용할 수 있습니다. 변경하려면 `allowAdoption`을 명시해야 합니다. 이 옵션은 변경 허용이며, 일치하는 외부 package를 자동으로 소유하는 기능은 아닙니다.

`removeUnusedOwnedPackages`의 기본값은 false입니다. 켜더라도 현재 `managementOwner`와 일치하는 항목만 제거합니다. Desired graph는 유지할 모든 roots의 closure여야 합니다. 외부 또는 유지하는 package가 제거 후보를 여전히 요구하면 실패합니다. 기존 legacy Installer의 manifest 항목은 receipt 없이 소유권을 자동 추정하지 않습니다.

`InstalledPackage.dependencies`는 effective dependency IDs, `dependencyRequirements`는 관측한 선언입니다. 선언 ID는 effective IDs에도 포함됩니다. Desired graph 밖에서 유지하는 package가 변경할 dependency를 사용하면 그 선언의 제약을 다시 평가합니다. 선언을 관측하지 못했거나 평가기를 제공하지 못하면 호환성을 추측하지 않고 diagnostic으로 차단합니다. 변경하지 않는 dependency에는 이 추가 검증을 요구하지 않습니다.

## Passive plan과 순서

Desired graph의 모든 package에는 explicit installation binding이 필요합니다. 현재 상태와 동일한 package는 operation을 생성하지 않습니다. 변경은 Install, Update, Remove, ChangeInstallation으로 분류합니다. 설치 표현 변경에는 기존 표현 제거와 새 표현 설치의 두 phase를 제공합니다.

Provider는 `installChanges`, `removeChanges`와 전체 immutable planning request를 받습니다. 패키지 diff가 없어도 원하는 설치 종류의 provider를 호출하므로 registry 설정 등 package 외의 공유 환경 상태도 검증하고 계획할 수 있습니다. 각 package 전이에 대해 non-empty `PackageOperationBoundary`를 반환해야 합니다. Start operations는 target 변경의 시작, completion operations는 해당 phase 완료입니다. 각 시작과 완료는 operation prerequisite 경로로 연결되어야 합니다. 원자적 공유 operation 하나가 여러 package boundary에 포함될 수 있습니다.

Core는 변경한 dependency의 install completion 이후 dependent 설치를 배치합니다. 기존 package 제거는 dependent의 제거 또는 dependency를 해제하는 update 이후에 배치합니다. 설치 방식 변경은 기존 표현 제거 후 새 표현 설치로 연결합니다. 환경에서 단일 원자적 변경으로 교체해야 한다면 composer가 두 phase를 같은 operation으로 병합할 수 있습니다.

Composer는 manifest 등 공유 변경을 병합한 뒤 모든 prerequisite references와 package boundaries를 갱신해야 합니다. Core는 operation ID 중복, 누락된 references, 미구현 phase, 불필요한 package phase, 연결되지 않은 boundaries, operation cycle, 순서 없는 read/write 및 write/write resource 충돌을 검사합니다. Operation 순서는 결정적입니다.

Resource key의 도메인 의미와 path/scope 중첩은 Core가 해석하지 않습니다. Adapter가 canonical keys와 겹침을 표현하고 semantic conflicts를 진단해야 합니다. 같은 resource의 순서 있는 작업이 의미적으로 호환되는지도 adapter 책임입니다.

Plan은 observed resource fingerprints를 보존하고 실행하지 않습니다. Unity observer는 manifest/UPM lock/Installer 상태의 전체 파일 지문을 수집하며, executor가 실행 직전 재검사합니다. Execution receipts는 향후 복구의 연결점이며 자동 rollback 또는 transaction 보장을 제공하지 않습니다.

## Git / Scoped Registry 구현

사용 흐름과 선언 예제는 [GitAndRegistry.md](Documentation/GitAndRegistry.md)에 있습니다.

- Git: 절대 HTTPS/HTTP/SSH/GIT/FILE URI, repository-relative package path, branch/tag/HEAD 또는 full commit 선택. 해석 호출 안에서 repository/ref snapshot을 공유하고 확정 commit의 regular blob에서 metadata를 읽습니다. UPM에는 complete dependency closure를 commit-pinned direct dependency로 기록합니다. SCP shorthand와 Git expression/refspec은 현재 입력 포맷에서 지원하지 않습니다.
- Registry: npm-compatible metadata endpoint, exact version, SHA-512/384/256/1 integrity 검사, published tarball의 package.json/sidecar 조회. 가장 긴 scope가 우선하며 같은 길이의 다른 endpoint는 거부합니다. 새 scopes가 기존에 유지할 패키지 출처를 바꾸는 경우 계획을 거부합니다.
- Built-in modules: com.unity.modules.*는 HTTP registry가 아닌 현재 Unity Editor의 offline UPM search로 조회합니다.
- Source/installation: `IUnityManifestArtifact`가 manifest value, UPM identifier 및 native installed identity를 제공합니다. Planner는 source 종류를 분기하지 않습니다. 선택된 source identity와 native UPM identity는 별도로 보존하여 새 source도 기존 installation을 재사용할 수 있습니다.
- Metadata: package.json을 기본으로, optional runios.package.json schemaVersion 1이 dependency별 출처를 덧붙입니다. `IUpmDependencyDeclarationReader` 구현으로 source codec을 추가하며 자동 bootstrap은 TypeCache로 reader를 발견합니다. Manual DI에서는 전체 reader collection을 지정할 수 있습니다.
- Observation: 현재 열린 Unity project만 관측/실행합니다. Effective packages와 UPM lock을 비교하고 drift를 진단합니다. Embedded/local package는 외부 관측으로 보존하며 이번 provider가 설치/제거하지 않습니다.
- Planning: provider 하나가 manifest dependency/registry 변경을 하나의 passive transaction으로 합성합니다. Unknown manifest fields를 보존하고 모든 package boundary를 공유 작업에 대응시킵니다. `UnityManifestOperation.manifestJson`을 UI/CLI preview에서 읽을 수 있습니다.
- Execution: `UnityInstallPlanRunner`가 operation kind로 executor를 선택합니다. Unity executor는 지문 검사, journal 저장, atomic manifest-file replacement, completion request가 있는 `Client.AddAndRemove` batch, effective identities 검증 순으로 진행합니다. `Client.Resolve()`의 void 반환을 완료 신호로 추정하지 않습니다.
- Ownership: 성공적인 native identity 검증 후 `Packages/runios.installer-state.json`에 선택된 identity와 native identity를 기록합니다. Matching 외부 package를 자동 소유하지 않으며 indirect dependency를 직접 manifest에 pin하는 전환도 adoption 정책을 따릅니다.
- Recovery: `Library/RuniOS.PackageManagement/transactions/` journal이 변경 전 manifest/lock/ownership 파일과 실행 상태를 보존합니다. 실패·취소 후 이미 적용된 변경은 receipt/diagnostic으로 표시합니다. Automatic rollback, 전체 파일 transaction, 실행 중 외부 UPM 작업과의 global lock은 보장하지 않습니다.

Git/HTTP 조회는 target project를 변경하지 않습니다. Temporary bare Git cache와 HTTP tarball 조회만 수행합니다. Git 인증은 기존 Git credential/SSH 설정을 사용합니다. 기본 HTTP transport는 익명이며 private registry에는 origin별 인증 handler가 설정된 caller-owned HttpClient를 주입해야 합니다. 설치 시 UPM 인증은 별도로 Unity 설정을 사용합니다. 인증 정보를 source descriptor나 plan에 넣지 않습니다.

Registry content identity는 canonical endpoint + immutable published version입니다. Tarball integrity는 discovery metadata 검증에 사용하지만 UPM package cache의 모든 파일을 재해시하지는 않습니다. 같은 버전을 재배포하는 registry 정책은 지원하지 않습니다. Native request는 취소할 수 없으므로 시작된 UPM 작업은 완료까지 drain한 뒤 gate를 해제합니다. 변경 중 assembly reload를 잠그며 검증/실패 기록 후 해제합니다.

다른 RuniOS package의 선언과 기존 Installer UI는 변경하지 않았습니다. 기존 개발 중 version mismatch를 자동으로 완화하지 않습니다. Git transitive dependency를 선택하려면 해당 dependency의 same-commit sidecar 선언 또는 명시적으로 그와 동등한 authored requirement가 필요합니다. Generated NuGet UPM package에도 같은 선언을 생성하면 일반 Git/registry package로 처리됩니다.

UI 재작성, Embedded/local 설치 구현, NuGet tooling, CLI 및 SemVer-range 선택은 이번 구현에 포함하지 않습니다. Assembly와 bootstrap package dependency는 추가하지 않았습니다.

## Verification boundary

별도 clone의 codex/package-management-core에서 구현했습니다. 메인 checkout은 수정하지 않았습니다. SDK project, dotnet build/test, syntax-only compile 및 Unity 실행은 하지 않았습니다.

정적 검토는 type/file/namespace 규칙, camelCase, XML documentation, immutable payload, source/installation 경계, operation boundary, GUID, 기존 assembly/compiler 설정 및 변경 범위를 확인합니다. 별도 temporary fixture에서 Git CLI의 shallow fetch/pinned blob command를 확인했습니다. Unity/OpenUPM의 실제 exact-version metadata와 tarball SHA-1/package.json 일치도 HTTP protocol 수준으로 확인했습니다. 이 확인은 C# provider 실행 또는 Unity 설치 성공을 증명하지 않습니다.

Unity에서 확인할 항목: Git tag/branch snapshot, same-commit diamond closure, conflicting requirements, sidecar 없는 metadata, exact registry dependency closure, 신규/중첩 registry scope, preview 중 외부 manifest edit, UPM failure, cancellation 전후, ownership/adoption, install/update/remove, mixed-source atomic boundaries, unrelated manifest fields, indirect dependency promotion 및 assembly reload 후 journal 확인.

참조: [Unity Git dependencies](https://docs.unity.com/en-us/engine/6000.0/manual/packages-list/upm-git), [Client.AddAndRemove](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PackageManager.Client.AddAndRemove.html), [npm package metadata](https://github.com/npm/registry/blob/main/docs/responses/package-metadata.md).
