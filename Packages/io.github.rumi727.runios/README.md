# Runiverse OS Installer

`io.github.rumi727.runios.installer`는 패키지 종속성 계산과 설치 실행을 위한 공개 API, Runiverse OS 초기 설정용 Setup UI를 제공하는 Unity Editor 전용 패키지입니다.

Unity 6000.7을 대상으로 하며, Runiverse OS 본체를 설치하기 전부터 사용할 수 있습니다.

| 어셈블리·네임스페이스 | 제공 기능 |
| --- | --- |
| `RuniOS.PackageManagement` | Unity에 의존하지 않는 패키지 정의, 종속성 계산, 설치 실행과 Preview API |
| `RuniOS.PackageManagement.Unity` | Unity 정의 에셋, UPM 설치와 embedded 패키지 확인 |
| `RuniOS.Installer` / `RuniOS.Editor.Installer` | Setup UI와 화면 확장 API |

API를 사용하는 Editor 어셈블리에서 필요한 어셈블리를 참조합니다. PackageManagement API는 Setup UI와 별도로 사용할 수 있습니다.

## Setup 사용

1. `Window > Runiverse OS > Setup`을 엽니다.
2. TextMesh Pro 화면에서 기본 리소스와 필요한 경우 Examples & Extras를 가져옵니다.
3. 패키지 선택 화면에서 필요한 RuniOS 패키지를 선택합니다. 각 패키지의 설명과 종속성을 펼쳐 볼 수 있습니다.
4. Preview 화면에서 설치 상태를 확인하고 Install을 실행합니다.

영어·한국어·일본어를 지원합니다. 언어와 패키지 선택은 `Assets/Runiverse OS/Installer/SetupConfig.asset`에 저장됩니다.

`RegistryPackage`와 revision을 지정한 `GitPackage`는 요청한 버전 또는 Git revision과 설치 상태가 일치할 때 충족된 것으로 처리합니다. 차이가 있으면 Preview에서 승인한 경우 요청한 상태로 교체하며, 미선택 패키지는 제거하지 않습니다.

요청한 패키지와 같은 이름의 `Embedded` 패키지가 이미 있으면 Preview에 경고를 표시하지만 해당 패키지는 충족된 것으로 처리하며 변경하지 않습니다. Install을 누르면 나머지 설치를 계속할지 확인하고, `force`가 켜져 있어도 Embedded 패키지는 덮어쓰지 않습니다.

## 패키지 정의

`Assets > Create > Runiverse OS > Installer`에서 패키지 정의 에셋을 만들고 논리적 ID, 표시 이름과 직접 종속성을 작성합니다. 실제 UPM 패키지 이름이 ID와 다르면 `packageName`을 지정합니다.

- `GitPackage`: Git 저장소 URL, 선택적인 태그·브랜치·전체 커밋 해시와 패키지 경로. revision을 비우면 UPM이 기본 브랜치 최신 커밋을 선택합니다.
- `RegistryPackage`: 레지스트리의 고정 버전과 필요한 scoped registry 설정.
- `LocalPackage`: 프로젝트 상대 경로나 절대 경로의 로컬 패키지.
- `EmbeddedPackage`: 프로젝트에 이미 존재하는 embedded 패키지.

`PackageAsset`은 `ScriptableObject` 기반의 `IPackage` 구현입니다. `PackageCatalog` 에셋은 정의 목록을 보관하며, 설치할 패키지 선택은 별도로 전달합니다. Git·Registry 정의 작성은 [작성 안내](Documentation/GitAndRegistry.md)를 참고하세요.

Setup에서 제공하는 정의는 [패키지 목록](Editor/Packages/README.md)을 참고하세요.

## PackageManagement API

### 패키지 정의와 종속성 계산

`IPackage`를 구현하면 에셋 없이도 패키지를 정의할 수 있습니다.

```csharp
public interface IPackage
{
    PackageId id { get; }
    string exactIdentity { get; }
    IReadOnlyList<IPackage?> dependencies { get; }
    IInstallation CreateInstallation(bool isRoot);
}
```

- `id`: 논리적 패키지 식별자입니다.
- `exactIdentity`: 작성자가 정하는 고정된 정의 식별 문자열입니다. `PackageIdentity`는 `(id, exactIdentity)`를 묶으며 버전 문자열을 해석하거나 설치된 파일과 대조하지 않습니다.
- `dependencies`: 직접 종속하는 정의의 참조입니다. `null` 항목은 누락된 참조를 뜻합니다.
- `CreateInstallation(isRoot)`: 설치 요구사항을 담는 `IInstallation`을 생성합니다. `isRoot`는 호출자가 직접 선택한 패키지인지 나타냅니다.

`PackageGraph.Flatten(selectedRoots)`은 선택한 패키지와 그 종속성을 모두 포함하는 결과를 반환합니다. 정의는 계산과 결과 사용 중 변경하지 않습니다.

| `PackageFlattenResult` 멤버 | 의미 |
| --- | --- |
| `succeeded` | 오류 없이 종속성을 계산했는지 여부 |
| `packages` | `FlattenedPackage` 목록. `package`는 정의, `isRoot`는 직접 선택 여부, `requiredBy`는 이 정의를 직접 요구하는 패키지들 |
| `diagnostics` | 잘못된 정의, 누락 참조, 순환, 중복 정의와 identity 충돌 정보 |
| `GetUnused(candidates)` | 성공한 결과에 포함되지 않은 정의를 exact key 기준으로 조회. 설치된 패키지를 제거하지 않음 |

같은 정의 인스턴스의 반복 참조는 허용합니다. 같은 exact key를 가진 서로 다른 인스턴스는 중복 정의 오류이며, 같은 ID의 서로 다른 identity가 필요하면 충돌입니다. 진단의 `code`, `message`, `package`, `owner`, `referenceIndex`, `path`로 원인을 확인할 수 있습니다. 충돌 상대는 `relatedPackage`, `relatedPath`에 표시됩니다.

실패한 결과는 실행하지 않습니다. `packages`의 순서는 지정되지 않으며 종속성 그래프가 설치 실행 순서를 보장하지 않습니다.

### Preview와 설치 실행

`InstallationRunner`에 사용할 실행기 인스턴스들을 전달합니다. 다음 예제는 호출자가 준비한 `selectedRoots`와 `cancellationToken`을 사용합니다.

```csharp
using System.Collections.Generic;
using RuniOS.PackageManagement;
using RuniOS.PackageManagement.Unity;

PackageFlattenResult closure = PackageGraph.Flatten(selectedRoots);
if (!closure.succeeded)
{
    // closure.diagnostics를 표시합니다.
    return;
}

var installations = new List<IInstallation>();
foreach (FlattenedPackage package in closure.packages)
    installations.Add(package.package.CreateInstallation(package.isRoot));

var runner = new InstallationRunner(new IInstallationExecutor[]
{
    new UpmExecutor(),
    new EmbeddedExecutor()
});

await foreach (InstallationPreview preview in runner.PreviewAsync(installations, cancellationToken))
{
    // preview.installation, executor, status, diagnostics를 표시합니다.
}

await foreach (InstallationResult result in runner.EnsureAsync(installations, force: false, cancellationToken: cancellationToken))
{
    // result.installation, executor, succeeded, diagnostics를 확인합니다.
}
```

`PreviewAsync`는 현재 상태를 읽고 `EnsureAsync`는 요구사항을 실행합니다. Preview는 선택 사항이며 실행 성공을 보장하지 않습니다. Ensure는 현재 환경을 다시 확인합니다. 설치된 버전이 요청한 Registry 버전이나 Git revision과 다르면 Preview는 `RequiresForce`를 반환합니다. `force` 기본값은 `false`이며, 이때 Ensure는 registry 설정이나 UPM 요청 전에 해당 executor batch 전체를 중단합니다. Preview 후 상태가 달라져도 같은 검사를 실행 직전에 적용합니다. 사용자가 Preview에서 교체를 승인한 경우에만 `force: true`를 전달합니다. Git 태그는 설치된 Git revision과 요청한 태그 이름을 비교하며, 태그가 가리키는 commit hash 변화는 따로 확인하지 않습니다.

| `InstallationPreviewStatus` | 의미 |
| --- | --- |
| `Satisfied` | 현재 요구사항 충족 |
| `RequiresEnsure` | 실행할 작업이 있음 |
| `RequiresForce` | 설치된 버전이 요청 정의와 다르며 교체 승인이 필요함 |
| `Delegated` | 준비 완료. 패키지 획득은 UPM에 위임하며 설치 완료를 확인한 상태는 아님 |
| `NotSupported` | 대응하는 실행기가 Preview를 제공하지 않음 |
| `Failed` | 관측 실패, 해결할 수 없는 요구사항 또는 대응하는 실행기 없음 |

Runner는 `CanExecute`가 수락한 모든 실행기에 요구사항을 전달하고 실행기별 결과를 반환합니다. 대응하는 실행기가 없으면 실패 결과를 반환합니다. 실행기 인스턴스를 반복해서 전달하면 반복 실행되며 실행기 순서나 트랜잭션은 보장하지 않습니다. 실행기의 수명은 호출자가 관리합니다.

전체 작업을 진행하려면 반환된 비동기 스트림을 끝까지 열거해야 합니다. 열거 중단이나 취소는 남은 실행을 중단합니다. 개별 실패는 `diagnostics`에서 확인하며 실행기 호출·결과 열거·열거자 해제의 예외와 취소는 호출자에게 전파됩니다.

### 사용자 정의 설치 방식

1. `IInstallation`을 구현해 설치 요구사항 타입을 정의합니다. 패키지의 `CreateInstallation`에서 반환하거나 직접 생성할 수 있습니다.
2. `IInstallationExecutor<TInstallation>`의 `EnsureAsync(IEnumerable<TInstallation>, bool force, CancellationToken)`을 구현합니다. `force`가 `false`이면 기존 요구사항과 다른 항목을 교체하지 않습니다. 기본 `CanExecute`는 해당 타입인지 검사합니다. 별도 조건이 필요하면 비제네릭 `IInstallationExecutor.CanExecute`를 구현하되 수락하는 타입은 유지합니다. `CanExecute`는 설치 작업 없이 지원 여부만 판단합니다.
3. 실행기 인스턴스를 `InstallationRunner` 생성자에 전달합니다.

실행기는 입력 항목마다 `InstallationResult` 하나를 반환해야 합니다. 반복된 입력도 각각 결과가 필요하며 결과 대응에는 기본 동등성을 사용합니다. 정상 종료 후 보고하지 않은 항목은 Runner가 실패로 표시합니다. `new InstallationResult(installation, succeeded, diagnostics)`로 결과를 만들면 Runner가 `executor`를 지정합니다.

Preview를 제공하려면 같은 실행기에서 `IInstallationPreviewer<TInstallation>`의 `PreviewAsync(IEnumerable<TInstallation>, CancellationToken)`도 구현합니다. 입력 항목마다 `InstallationPreview` 하나를 반환합니다. Preview를 구현하지 않아도 설치 실행에 참여할 수 있습니다.

## Unity 설치 API

`UpmInstallation`은 UPM 패키지 이름과 획득 참조를 담습니다. `Git(packageName, repositoryUrl, revision, packagePath)`에서 revision은 선택적인 태그·브랜치·전체 커밋 해시입니다. 생략하면 UPM이 기본 브랜치 최신 커밋을 선택합니다. `Registry(packageName, version, registry, ensurePackage)`, `Local(packageName, fullPath)` 정적 메서드로 만들거나 다음 생성자를 사용합니다.

```csharp
new UpmInstallation(packageName, packageReference, registry: null, ensurePackage: true);
```

scoped registry가 필요하면 `ScopedRegistryDefinition(name, url, scopes)`를 전달합니다. `UpmExecutor`는 요청한 Registry 버전과 Git revision을 현재 설치 상태와 비교합니다. 버전 차이를 교체하도록 승인하면 업그레이드·다운그레이드 구분 없이 UPM에 요청합니다. 같은 이름의 패키지가 이미 Embedded 상태면 요청 버전과 무관하게 충족된 것으로 처리하고 변경하지 않습니다. package 이름만으로 상태를 판단하는 직접 `UpmInstallation` 생성자와 `Local` 설치는 기존 동작을 유지합니다. 필요한 `manifest.json`의 `scopedRegistries`는 추가·병합하며 JSON 서식은 변경될 수 있습니다.

`ensurePackage: false`는 registry 준비만 요구합니다. 이 경우 성공은 패키지 설치 완료를 뜻하지 않으며 준비된 상태의 Preview는 `Delegated`입니다. `RegistryPackage.CreateInstallation(isRoot)`은 직접 선택한 패키지에는 설치를 요구하고 종속성으로만 포함된 패키지는 UPM의 종속성 설치에 맡깁니다.

`EmbeddedInstallation(packageName)`과 `EmbeddedExecutor`는 같은 이름의 기존 embedded 패키지를 확인합니다. 없거나 다른 출처이면 실패하며 embedded 패키지를 새로 만들지는 않습니다.

외부 실행기에서 Unity API를 호출할 때는 `UnityEditorThread.RunAsync(action, cancellationToken)`을 사용할 수 있습니다. UPM 요청은 `UnityUpmClient.RunAsync(() => Client.List(), cancellationToken)`처럼 전달하면 메인 스레드에서 시작하고 공용 요청 순서에 따라 실행합니다. 이미 시작한 UPM 요청은 완료 후 취소를 보고합니다.

## Setup 화면 확장

별도 Editor 어셈블리에서 `RuniOS.Installer`를 참조하고 `SetupScreen`을 상속하면 Setup 화면을 추가할 수 있습니다. 구현에는 public 매개변수 없는 생성자가 필요하며, 화면은 `order` 오름차순으로 표시됩니다.
