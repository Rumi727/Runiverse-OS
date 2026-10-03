# Package management

`com.rumi.runios.installer`는 exact Package definitions의 dependency closure를 계산하고, caller가 구성한 executor로 필요한 Installation을 실행합니다. 기존 `com.rumi.runios` Installer UI는 변경하지 않습니다.

| Assembly | 책임 | 참조 |
| --- | --- | --- |
| `RuniOS.PackageManagement` | Package/Installation 계약, flattening, graph diagnostics, dispatch, optional preview와 execution | BCL |
| `RuniOS.PackageManagement.Unity` | SO authoring, UPM batch 실행, 기존 embedded 존재 확인 | Core, Unity Editor, Newtonsoft Json |
| `RuniOS.Installer` | 정의·roots 입력과 결과를 표시하는 독립 UI | Core, Unity 구현 |

Core에는 Unity, JSON, Git, registry, semantic version 해석이 없습니다. 모든 assembly는 Editor 전용이며 `csc.rsp`의 C# 14, nullable 설정을 사용합니다. 별도 NuGet restore, RuniOS Core, TMP를 요구하지 않습니다. Newtonsoft Json 3.2.2는 scopedRegistries 편집에만 사용하며 UPM이 bootstrap dependency를 복원합니다.

## Definitions와 closure

```csharp
public interface IPackage
{
    PackageId id { get; }
    string exactIdentity { get; }
    IReadOnlyList<IPackage?> dependencies { get; }
    IInstallation CreateInstallation(bool isRoot);
}
```

`exactIdentity`는 작성자가 결정하는 opaque deterministic string입니다. `(id, exactIdentity)`가 하나의 exact definition을 식별합니다. Core는 version/revision을 탐색하거나 이 문자열을 해석하지 않습니다. 이것은 설치 파일의 content identity 증명이 아닙니다.

`dependencies`는 직접 `IPackage` 참조입니다. caller는 선택한 roots만 제공합니다. Flatten은 그 참조를 따라 closure를 계산합니다. 전체 catalog, discovery, provider 등록, global package registry가 필요하지 않습니다. 정의는 flatten·결과 사용 중 변경하지 않습니다.

```csharp
PackageFlattenResult closure = PackageGraph.Flatten(selectedRoots);
if (!closure.succeeded)
{
    // closure.diagnostics를 표시하고 실행하지 않습니다.
    return;
}
// Optional authoring inventory comparison.
IEnumerable<IPackage> unused = closure.GetUnused(definitions);
```

같은 instance 반복은 허용합니다. 도달한 closure에서 같은 exact key의 서로 다른 instance는 duplicate-definition 오류입니다. 같은 ID의 서로 다른 identities를 요구하면 conflict입니다. optional inventory 밖의 dependency도 직접 참조로 탐색합니다.

`closure.packages`는 `FlattenedPackage(package, isRoot, requiredBy)` 목록입니다. `requiredBy`는 이 package를 직접 요구한 정의들을 instance 기준으로 중복 제거한 snapshot입니다. 동일 dependency slot 반복은 한 번만 표시하며 여러 owner는 모두 보존합니다. root이면서 dependency인 package는 `isRoot == true`와 비어 있지 않은 `requiredBy`를 함께 갖습니다.

Flatten은 누락 참조, cycle, exact identity conflict를 검출합니다. 반복 탐색으로 unique closure를 만들며 깊은 graph를 CLR 재귀로 탐색하지 않습니다. Package 결과 순서는 정의되지 않습니다(The order is unspecified). Dependency graph는 Installation 실행 순서를 정의하지 않습니다. 실패한 결과의 partial closure는 진단용이고 실행하면 안 됩니다. Graph diagnostic에는 대상·관련 정의, owner·reference slot과 root에서의 경로가 있습니다. Identity conflict에는 양쪽 정의의 root 경로를 보존합니다. source location 표시는 caller가 asset 등으로 대응시킵니다.

`GetUnused`는 성공한 closure 밖의 exact keys를 찾는 순수 정의 query입니다. 설치된 UPM graph와의 비교나 package 제거를 뜻하지 않습니다. 자동 pruning, ownership, 과거 적용 상태는 없습니다.

## Descriptor와 execution

Installation은 실행 요구사항을 담는 descriptor입니다. executor 참조, service locator, static executor singleton을 갖지 않습니다. code-derived descriptor와 직접 구성한 descriptor 모두 같은 `IInstallation` 계약을 사용합니다.

Package는 `CreateInstallation(bool isRoot)`에서 root 여부를 concrete 실행 요구사항으로 번역합니다. Executor는 root 여부를 다시 해석하지 않습니다. Graph에 포함되어 있다는 사실과 direct install 요청 대상이라는 사실은 다릅니다. RegistryPackage는 root이면 package ensure와 registry 준비를 요구하고, dependency-only이면 registry 준비만 요구합니다. Git/Local의 기존 package ensure와 Embedded의 존재 확인 의미는 유지합니다. exactIdentity는 root 여부와 무관합니다.

`IInstallationExecutor<TInstallation>`은 typed `EnsureAsync(IEnumerable<TInstallation>, CancellationToken)`을 구현합니다. default interface implementation이 비제네릭 `CanExecute`와 `EnsureAsync`를 연결합니다. 기본 matching은 `installation is TInstallation`이며 concrete executor는 typed 실행 메서드만 구현하면 됩니다. 별도 Type metadata가 없습니다.

```csharp
var runner = new InstallationRunner(new IInstallationExecutor[]
{
    new UpmExecutor(),
    new EmbeddedExecutor(),
    // 외부 executor instances
});

// 각 Package의 root-aware descriptor를 보관합니다.
var installations = new List<IInstallation>();
foreach (FlattenedPackage flattened in closure.packages)
    installations.Add(flattened.package.CreateInstallation(flattened.isRoot));

await foreach (InstallationPreview preview in runner.PreviewAsync(installations, cancellationToken))
{
    // preview.installation, executor, status, diagnostics
}

// Caller confirmation may occur here. Ensure independently observes current state.

await foreach (InstallationResult result in runner.EnsureAsync(installations, cancellationToken))
{
    // result.installation, executor, succeeded, diagnostics
}
```

Runner는 executor collection의 instance별 `CanExecute`를 평가합니다. 0 match는 unsupported failure, 1 match는 해당 executor, N match는 matching executor 전부입니다. 같은 instance를 executor collection에 반복해서 넣으면 반복 실행됩니다. caller가 executor의 lifetime과 조합을 관리합니다.

Dispatch는 입력을 한 번 열거해 executor와 batch를 연결한 읽기 전용 중간 데이터를 만듭니다. Preview와 Ensure는 각각 Dispatch를 수행하고 별도 관측·실행 경로에서 그 결과만 소비합니다. 중간 데이터는 비공개 matching/grouping 결과이며 environment diff나 execution plan이 아닙니다. dispatch는 executor 실행을 시작하지 않습니다.

비어 있지 않은 executor별 batch invocation은 한 번입니다. 각 executor는 input installation 항목별 최종 결과 하나를 비동기로 생산합니다. Runner는 batch 입력을 복사한 remaining 목록에서 기본 동등성으로 결과당 한 occurrence를 제거합니다. 같은 값이나 instance가 반복되면 각각 결과가 필요하며 누락된 각 항목은 failure로 보고합니다. Runner는 실행 주체를 결과에 붙입니다. N match 결과는 하나로 합치지 않습니다. stream을 끝까지 열거해야 전체 실행이 진행됩니다. caller가 열거를 중단하거나 취소하면 남은 실행은 수행되지 않습니다.

Executor 간 순서, side effect 호환성, 중복 작업의 안전성, idempotency, transactionality는 보장하지 않습니다. priority, ambiguity resolver, execution dependency graph는 없습니다. 특정 installation 실패는 executor가 failure 결과로 반환합니다. 정상 열거 종료 후 미보고 항목은 Runner가 missing-result failure로 보고하고 다른 batch를 계속 처리합니다. executor 호출, GetAsyncEnumerator, MoveNextAsync, Current, DisposeAsync의 예외는 호출자에게 전파합니다. cancellation은 취소로 전파합니다. CanExecute는 dispatch 중 호출되므로 실행 side effect 없이 matching을 수행해야 합니다.

## Optional preview

`IInstallationPreviewer<TInstallation>`은 typed `PreviewAsync(IEnumerable<TInstallation>, CancellationToken)`만 구현하며 default interface bridge가 비제네릭 capability를 연결합니다. Runner는 matching executor instance가 `IInstallationPreviewer`인지 확인합니다. 별도 preview matching, Type metadata, registry, locator는 없습니다. Preview 미지원 executor도 Ensure에 정상 참여합니다.

`InstallationPreview`에는 관측에 대응하는 installation, Runner가 지정한 executor, status, diagnostics만 있습니다. 0 match는 executor가 없는 `Failed`와 `installation:unsupported` diagnostic입니다. N match는 executor별 결과를 노출하며 합치지 않습니다. 특정 installation의 관측 실패는 previewer가 `Failed`로 반환합니다. Preview도 remaining 목록과 기본 동등성으로 항목별 결과를 추적합니다. 정상 열거 종료 후 누락 결과는 `Failed`로 보고하고 다른 batch를 계속 관측합니다. Preview enumeration의 예외와 DisposeAsync 실패는 호출자에게 전파합니다.

| Status | 의미 |
| --- | --- |
| `Satisfied` | 현재 관측한 요구사항 충족 |
| `RequiresEnsure` | 현재 ensure 작업 필요. 성공 보장 없음 |
| `Delegated` | registry 준비 완료. package 획득은 UPM에 위임하며 설치 여부를 증명하지 않음 |
| `NotSupported` | 실행 가능한 executor에 preview capability가 없음 |
| `Failed` | 관측 실패, 해결 불가능한 요구사항 또는 executor 없음 |

Preview는 읽기 전용 advisory observation입니다. Ensure는 현재 환경을 다시 읽습니다. Preview에서 Satisfied/Delegated였어도 Ensure에서 생략하지 않으며 PackageInfo, manifest snapshot, pending 목록, preview 상태를 실행 판단에 재사용하지 않습니다. 동일 정의·descriptor·executor instance와 순수 검증 함수는 공유할 수 있습니다. snapshot locking, transaction, journal, reconciliation, Planner/InstallPlan/operation graph는 추가하지 않습니다.

## Unity authoring

`Assets > Create > Runiverse OS > Installer`에서 Git/Registry/Local/Embedded Package 또는 선택적인 Package Catalog를 생성합니다. `PackageAsset : ScriptableObject, IPackage`는 Unity 구현의 authoring base이고 Core Package는 interface입니다. 외부 SO는 이 base를 상속하지 않고 `IPackage`를 직접 구현해도 됩니다.

Logical ID, 선택적인 native package name override, 표시 이름, 직접 dependency asset 참조를 작성합니다. PackageAsset dependency 배열의 null slot은 보존하여 graph가 누락 오류를 표시합니다.

- `GitPackage`: Git URL, optional package path, full 40-character commit hash. Installation과 exact identity는 해당 pin에서 계산합니다.
- `RegistryPackage`: exact version, optional scoped-registry name/URL/scopes. versions를 탐색하지 않습니다.
- `LocalPackage`: project-root 상대 또는 절대 directory path. path는 local definition identity이며 내용 동일성을 검사하지 않습니다.
- `EmbeddedPackage`: 기존 embedded package의 native name. definition identity는 graph 구분용입니다.

`PackageCatalog`는 정의 inventory만 저장하며 `IPackage`가 아닙니다. roots는 UI가 별도로 저장합니다. Core가 catalog를 등록하거나 읽지 않습니다. `runios.packages.json`, Source hierarchy, discriminator codec은 없습니다.

## UPM

`UpmInstallation`은 native package name, 없는 package에 사용할 완결된 acquisition reference, optional registry configuration, `ensurePackage`를 담습니다. constructor와 Registry factory의 ensurePackage 기본값은 true입니다. executor는 Package/Source 타입을 다시 해석하지 않습니다. custom UPM requirements도 public constructor로 작성할 수 있습니다. satisfaction predicate는 없습니다.

`UpmExecutor`의 package 충족 판단은 등록된 native package name 존재 여부뿐입니다. 같은 이름이면 Embedded/Git/Registry/BuiltIn/Local 등 source, version, commit, path, native 오류와 무관하게 충족으로 취급합니다. 기존 package를 교체하거나 direct dependency로 승격하지 않습니다. exactIdentity는 여전히 Core graph의 definition 구분과 conflict 검출용이며 설치된 package와 대조하지 않습니다.

전체 batch의 registry 요구는 별도로 준비합니다. package가 이미 설치되어 있어도 필요한 registry 설정을 생략하지 않습니다. ensurePackage가 true이고 현재 이름이 없는 항목의 references만 `Client.AddAndRemove` 한 번에 전달합니다. removals는 비어 있습니다. 요청 후 모든 package ensure 대상의 이름과 registry 요구를 다시 확인합니다. 아직 없는 같은 native name에 서로 다른 references를 요청하면 failure입니다. 이미 설치된 이름은 reference 충돌 검사에서도 제외합니다.

ensurePackage가 false이면 package를 직접 요청하거나 설치 완료를 검증하지 않습니다. 이 항목의 Ensure 성공은 registry 준비 성공만 뜻합니다. Preview는 registry 준비가 필요하면 RequiresEnsure, 준비되었으면 Delegated입니다. default registry를 사용하는 dependency-only 항목도 Delegated입니다. registry 충돌은 Failed입니다. real package.json dependencies, version resolution, transport는 UPM 책임입니다.

Unity에 scoped registry mutation public API가 없어 필요한 `manifest.json`의 `scopedRegistries`만 추가·병합합니다. 다른 semantic fields는 보존합니다. JSON formatting은 다시 작성될 수 있습니다. dependencies와 packages-lock.json은 직접 편집하지 않습니다. SharpZipLib, HTTP/tarball acquisition, 수제 JSON codec은 제거했습니다.

`UnityEditorThread`와 `UnityUpmClient`는 외부 Unity executor도 사용할 수 있는 public 접근면입니다. Core에 main-thread/Awaitable 계약을 넣지 않습니다. 협력하는 UPM 요청은 공용 gate로 순차 실행합니다. 이미 시작한 native 요청에는 취소 API가 없으므로 완료까지 기다린 뒤 취소를 보고합니다. 외부에서 gate를 우회한 native 요청과의 조합은 보장하지 않습니다. Editor reload lifetime은 host가 관리하며 기본 UI는 실행 중 reload를 잠급니다.

UPM batch의 rollback/atomicity는 가정하지 않습니다. 실패 후 다음 실행은 현재 환경을 다시 관측합니다. transaction, journal, recovery framework는 없습니다.

## Existing embedded compatibility

`EmbeddedInstallation(packageName)`은 현재 프로젝트에 이미 존재하는 embedded package만 요구합니다. `EmbeddedExecutor`는 `PackageInfo.GetAllRegisteredPackages()`에서 source가 Embedded이고 name이 같은 항목을 찾습니다.

존재하면 변경 없이 성공하며 Preview는 Satisfied입니다. 없으면 `embedded:missing` failure이며 Preview는 Failed입니다. Preview와 Ensure는 각각 새로 관측합니다. 다른 source의 같은 이름 package는 embedded 존재 조건을 만족하지 않습니다.

Client.Embed, 선설치, direct dependency 승격, 복사, commit/content/digest 검증, provenance/history/ownership 추적을 하지 않습니다. 공통 Unknown/3-state 모델도 없습니다.

## Independent UI

`Window > Runiverse OS > Installer`에서 selected roots를 입력합니다. optional catalog 또는 직접 definitions 목록은 unused 비교에만 사용합니다. inventory 오류는 유효한 roots-only closure를 무효화하지 않습니다.

Selected roots와 Dependencies를 구분하고 requiredBy, graph errors, closure 밖 정의 query를 표시합니다. Preview와 Ensure 버튼은 독립적이며 성공한 closure만 관측·실행할 수 있습니다. 각 호출 직전 현재 정의를 다시 flatten하고 descriptor를 생성합니다. 기본 host는 UpmExecutor와 EmbeddedExecutor를 구성하고 비동기 결과를 표시합니다. Registry-only 결과는 package 설치 완료와 구분합니다. 이 UI는 기존 Installer의 language/TMP 기능을 변경하지 않습니다.

## Migration

Prototype의 PackageCandidate/PackageMetadata/ResolvedPackage, resolver/provider/service builder, planner/operations/environment/ownership API는 제거했습니다. 기존 사용자는 IPackage definitions, PackageGraph/PackageFlattenResult, InstallationRunner와 executor contracts로 이전해야 합니다. Source/revision/version 필드 기반 PackageIdentity는 `(PackageId, exactIdentity)`로 교체했습니다. public types는 `RuniOS.PackageManagement`와 `RuniOS.PackageManagement.Unity` namespace를 사용합니다.

이전 `new PackageGraph(definitions).Flatten(roots)`는 `PackageGraph.Flatten(roots)`로 변경합니다. closure.packages 원소는 IPackage 대신 FlattenedPackage이며 CreateInstallation에는 isRoot를 전달해야 합니다. Preview 결과는 Ensure 입력 계획으로 사용하지 않습니다.

UpmInstallation의 isSatisfied 속성과 constructor predicate 인자는 제거했습니다. constructor는 `(packageName, packageReference, registry = null, ensurePackage = true)`를 사용하며 native package name 존재 여부만으로 package 충족을 판단합니다.
