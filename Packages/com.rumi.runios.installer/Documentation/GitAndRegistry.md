# Git / Scoped Registry 사용

Editor bootstrap entry point는 `InstallerPackageManagement.CreateServices()`입니다. Assembly boundary는 기존 세 asmdef를 유지합니다. 아래 흐름의 Resolve/Read/Plan은 project mutation을 하지 않습니다. 실제 변경은 마지막 ExecuteAsync 호출에서 시작합니다.

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RuniOS.Installer.Editor;
using RuniOS.PackageManagement;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Planning;
using RuniOS.PackageManagement.Resolution;
using RuniOS.PackageManagement.Unity.Editor;
using RuniOS.PackageManagement.Unity.Editor.Git;
using RuniOS.PackageManagement.Unity.Editor.Registry;
using RuniOS.PackageManagement.Unity.Editor.Metadata;
using RuniOS.PackageManagement.Unity.Editor.Manifest;
using RuniOS.PackageManagement.Unity.Editor.Execution;

var registration = InstallerPackageManagement.CreateServices();
if (!registration.succeeded || registration.value is not { } services)
    return; // registration.diagnostics를 표시

var id = new PackageId("com.cysharp.unitask");
var registry = new ScopedRegistryDefinition(
    "OpenUPM", "https://package.openupm.com", new[] { "com.cysharp" });
var root = new PackageRequirement(id,
    new RegistryPackageSource(id, registry.url, registry),
    new[] { new UpmVersionConstraint("2.5.11") });

var resolution = await services.CreateResolver().ResolveAsync(
    new ResolutionRequest(new[] { root }), CancellationToken.None);
if (!resolution.succeeded || resolution.value is not { } graph)
    return; // resolution.diagnostics를 표시

var target = new UnityProjectTarget("/absolute/path/to/open/unity-project");
var observation = await services.ReadInstalledStateAsync(target, CancellationToken.None);
if (!observation.succeeded || observation.value is not { } current)
    return; // observation.diagnostics를 표시

var bindings = graph.packages.Keys.Select(package =>
    new KeyValuePair<PackageId, PackageInstallation>(package, new UnityManifestInstallation()));
var preview = services.CreatePlanner().Plan(new PlanningRequest(
    graph, current, bindings, "example-installer"));
if (!preview.succeeded || preview.value is not { } plan)
    return; // preview.diagnostics를 표시

// plan.changes와 UnityManifestOperation.manifestJson을 사용자에게 표시
// 기존 외부 package 전환은 PlanningRequest의 allowAdoption을 명시해야 함
// 원할 때만 실행:
var execution = await new UnityInstallPlanRunner(services)
    .ExecuteAsync(plan, CancellationToken.None);
// execution.succeeded와 execution.diagnostics를 확인
// 실패하더라도 execution.value.receipts에 부분 적용 journal이 있을 수 있음
```

Git root는 다음처럼 선언합니다. repository URI/path/reference는 실제 배포 경로로 대체합니다.

```csharp
var gitRoot = new PackageRequirement(
    new PackageId("com.example.feature"),
    new GitPackageSource("https://example.org/packages.git", "Packages/com.example.feature"),
    new[] { new GitReferenceConstraint("refs/tags/v1.0.0") });
```

선택 결과의 revision은 symbolic tag 대신 확정 commit입니다. 이미 알고 있는 complete lowercase commit을 원하면 Core의 `ExactRevisionConstraint(commit)`을 사용할 수 있습니다. Registry에서도 exact published version에 대한 `ExactRevisionConstraint`를 지원하지만, 일반 UPM version 요구에는 `UpmVersionConstraint`가 더 명확합니다.

## Sidecar 선언

패키지 디렉터리에 package.json과 함께 `runios.package.json`을 둡니다. [예제](Examples/runios.package.json)는 형식 설명용이며 repository의 다른 package를 변경하지 않습니다.

```json
{
  "schemaVersion": 1,
  "dependencies": {
    "com.rumi.runios.core": {
      "source": "git",
      "path": "Packages/com.rumi.runios.core"
    },
    "com.cysharp.unitask": {
      "source": "registry",
      "version": "2.5.11",
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.cysharp"]
    }
  }
}
```

Git declaration의 repository/reference를 생략하면 부모 Git artifact의 repository/확정 commit을 사용합니다. 다른 저장소에서는 repository를 지정하며, reference를 생략하면 HEAD를 확정합니다. 상대 path는 repository root 기준이며 traversal과 symbolic-link metadata는 거부합니다. Sidecar의 dependency가 package.json의 같은 ID 선언을 대체하므로 개발 중 Git 간 version mismatch는 해당 명시적 dependency에서만 영향을 받지 않습니다. 일반 registry dependency는 계속 exact version을 요구합니다.

새 scoped registry가 아직 project에 없어도 source descriptor에서 조회하고 preview에서 registry 설정을 제안할 수 있습니다. Registry package의 ordinary dependencies는 해당 artifact의 scopes도 상속합니다. Publisher는 tarball 안의 `package/runios.package.json`을 포함해야 합니다. Sidecar가 없으면 package.json만 해석합니다.

## 수동 구성 / private registry

```csharp
using System.Net.Http;

// 클라이언트는 호출자가 소유. Authorization은 registry origin에만 적용하는
// DelegatingHandler로 설정하고, tarball의 다른 host에 전달하지 않도록 구성.
HttpClient client = authenticatedClient;
var sources = new UpmSourceConfiguration(new[] { registry });
var builder = new PackageManagementBuilder();
builder.AddExtension(new UnityPackageManagementExtension(sources, client));
var registration = builder.Build();
```

자동 bootstrap은 concrete public-parameterless `IPackageManagementExtension`과 `IUpmDependencyDeclarationReader`를 검색합니다. 수동 구성에서 reader collection을 전달하면 전체 reader 목록으로 취급합니다. 기본 Git/registry reader도 포함해야 합니다. provider key 중복과 reader key 중복은 허용하지 않습니다.

새 source는 catalog/필요 evaluator/sidecar reader를 등록합니다. 기존 Unity manifest 설치를 재사용하려면 immutable artifact가 `IUnityManifestArtifact`를 구현하여 manifest value, UPM request identifier, required registry와 native installed identity를 제공합니다. 선택된 source identity와 native 설치 identity가 달라도 검증된 receipt로 대응을 유지합니다. Core를 수정하지 않습니다. Embedded 같은 새 installation은 별도의 planner/executor 및 관측을 제공해야 합니다.

## 실패 / 복구

preview 뒤 manifest/lock/ownership 파일이 바뀌면 실행 전에 실패합니다. Registry scope가 기존 retained package를 다른 endpoint로 보내는 계획도 거부합니다. 기본 adoption/removal 정책은 Core 기본값인 false입니다.

UPM batch 실패·mutation 이후 취소·postcondition 불일치 시 자동으로 manifest를 되돌리지 않습니다. 다른 작업자의 변경을 덮어쓸 수 있기 때문입니다. `ManifestExecutionReceipt`의 `manifestApplied`, `verified`, `journalPath`를 확인합니다. Journal의 before 데이터는 resource별 원본 bytes의 Base64이며 자동 복구 명령이 아닙니다. 실패 상태를 확인하고 새 관측/preview를 생성합니다.

이 clone에서는 Unity compile/runtime 검증을 수행하지 않았습니다. 설치 코드를 메인 project에서 자동 호출하는 Editor window/InitializeOnLoad hook은 추가하지 않았습니다.
