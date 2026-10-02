# Legacy Installer 테스트 정의

기존 `Packages/com.rumi.runios/Editor/GitPackages` 설정의 복사본입니다. Git Package 5개와 이름으로 선언된 dependency 9개를 새 Package asset으로 작성했습니다. 원본 asset과 manifest는 변경하지 않습니다.

`RuniPackage.asset`은 ID와 설치 URL이 없는 목록 묶음이므로 `LegacyPackageCatalog.asset`으로 대응합니다. Catalog에는 14개 정의를 모두 포함합니다. 원본 목록에서 선택되지 않았던 FMOD도 별도 정의로 포함합니다.

## 사용

1. `Window > Runiverse OS > Installer`를 엽니다.
2. `Catalog (optional)`에 `LegacyPackageCatalog.asset`을 넣습니다.
3. `Selected roots`에 `com.rumi.runios.core`, `com.rumi.runios.sound`, `com.rumi.runios.ui` asset을 넣으면 원본의 세 가지 설치 항목을 재현합니다. FMOD까지 포함하려면 `com.rumi.runios.fmod`도 추가합니다.
4. `Flatten dependencies`로 참조 graph를 확인합니다. 실제 설치를 확인하려는 경우 창의 `Ensure required installations`를 명시적으로 실행합니다.

Git Package 참조와 문자열 dependency를 모두 직접 Package asset 참조로 연결했습니다. 공유 dependency는 동일 asset을 참조합니다. native package.json 내부 dependency를 추가 수집하지 않습니다.

## 고정값

레거시 Git URL에는 commit pin이 없어서 새 정의에 정확한 commit을 지정했습니다.

- Runiverse OS: 작성 시 원격 HEAD `84627157f3c503b7d8a91faf700ab8929b84eb9a`.
- R3: 현재 `packages-lock.json`의 설치 commit `f6eed2dd4208dc4ae171c601e799e85f82aca25e`.

레거시 문자열 dependency에는 version이 없으므로 현재 `packages-lock.json`의 실제 version을 사용합니다. 미설치 SoftMask는 [OpenUPM registry](https://package.openupm.com/com.coffee.softmask-for-ugui)의 `latest`를 조회한 값 `3.6.5`로 고정했습니다. 정의가 이후 latest를 자동 추적하지 않습니다.

| Package | Version | Registry |
| --- | --- | --- |
| `com.coffee.softmask-for-ugui` | `3.6.5` | OpenUPM |
| `com.cysharp.unitask` | `2.5.11` | OpenUPM |
| `com.realitystop.linkmerge` | `1.0.0` | OpenUPM |
| `com.unity.modules.androidjni` | `1.0.0` | Unity |
| `com.unity.modules.ui` | `1.0.0` | Unity |
| `com.unity.modules.uielements` | `1.0.0` | Unity |
| `com.unity.nuget.newtonsoft-json` | `3.2.2` | Unity |
| `com.unity.textmeshpro` | `5.0.0` | Unity |
| `com.unity.ui` | `2.0.0` | Unity |

세 OpenUPM 정의에는 레거시 `package.openupm.com.asset`의 registry name, URL, 세 scopes를 함께 복사했습니다. Unity built-in 항목도 기존 `RegistryPackage`/UPM descriptor의 BuiltIn satisfaction 지원을 사용합니다.

## 원본과의 차이

레거시 `com.rumi.runios.ui.asset`의 URL은 `Packages/com.rumi.runios.core`를 가리킵니다. 새 UI 정의에서는 실제 UI package를 요청하도록 `Packages/com.rumi.runios.ui`로 맞췄습니다. 원본 설정은 그대로 둡니다.

현재 프로젝트의 RuniOS 패키지는 embedded이지만, 이 복사본은 레거시 설정처럼 Git 요구사항입니다. 기존 embedded가 있다고 Git 요구사항을 satisfied로 취급하지 않습니다. 정의 생성만 수행했으며 실제 UPM 설치는 실행하지 않았습니다.
