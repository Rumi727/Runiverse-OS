# Setup 패키지 정의

이 폴더는 Setup에서 사용하는 운영용 PackageAsset 정의입니다. 이전 LegacyPackages의 GUID를 유지하므로 SetupConfig의 저장된 root 참조는 유지됩니다. 선택 후보는 `../Screens/RootPackages.asset`에서 명시적으로 작성합니다. 설치 상태, package.json, packages-lock.json 또는 PackageCache에서 후보를 자동 생성하지 않습니다.

## 선택 가능한 RuniOS 패키지

| Root | 기능 | 본체 직접 종속성 |
| --- | --- | --- |
| Core | 공통 유틸리티, IO, 비동기 작업, 리소스와 지역화 | 없음 |
| Sound | 공통 오디오 API, 오디오 소스와 사운드 리소스 | Core |
| UI | 예정된 런타임 UI/UX 시스템, 현재 리소스 바인딩 | Core, Sound, Effects, FMOD, NBS, Texture |
| Effects | UI 둥근 모서리와 메시 외곽선 | Core |
| FMOD | FMOD Core 오디오 시스템과 재생 | Core, Sound |
| NBS | Note Block Studio 파일 로드와 FMOD 재생 | Core, Sound, FMOD |
| Texture | FreeImage·Burst 기반 런타임 이미지 로드 | Core |

Installer 자체와 레거시 Installer는 본체 선택 목록에 넣지 않습니다. Linux·Windows 정의는 이번 목록에서 제외했습니다. 설명은 공유 Installer 언어 에셋의 `installer.install_setting.{package}.label/one_line/description`을 사용합니다.

## 종속성과 설치 출처

PackageAsset 참조가 직접 종속성의 기준입니다. 명시된 설계와 소스·asmdef·패키지 선언을 확인해 작성했으며, 설치 시 native manifest의 종속성을 추가 수집하지 않습니다. UI는 예정된 설계에 따라 모든 본체 패키지와 SoftMask를 직접 참조합니다. 아직 사용 코드가 없다는 이유로 이 종속성을 제거하지 않습니다. FMOD는 실제 참조하는 Sound를 연결했고, Core에는 R3와 UnityWebRequest, Texture에는 Burst·Mathematics를 연결했습니다. uGUI·R3·Unity 모듈의 직접 종속성도 공유 정의를 참조합니다.

Unity 6000.7의 uGUI 패키지가 TextMesh Pro를 포함하므로 실제 `com.unity.ugui` 2.7.0 정의를 사용합니다. 이전의 비어 있는 `com.unity.ui` 및 uGUI로 위임하는 `com.unity.textmeshpro` 정의는 제거했습니다. TMP 리소스 가져오기는 별도 Setup 화면의 역할입니다. SoftMask는 UI의 직접 종속성이며, [3.6.5 registry metadata](https://package.openupm.com/com.coffee.softmask-for-ugui/3.6.5)에 따라 uGUI를 참조합니다.

기존 Git acquisition 방식과 commit pin은 유지합니다.

- RuniOS: `https://github.com/Rumi727/Runiverse-OS.git`, commit `84627157f3c503b7d8a91faf700ab8929b84eb9a`, 각 `Packages/com.rumi.runios.*` 경로.
- R3: `https://github.com/Cysharp/R3.git`, commit `f6eed2dd4208dc4ae171c601e799e85f82aca25e`, `src/R3.Unity/Assets/R3.Unity`.

같은 native 이름의 embedded 패키지가 있으면 기존 UpmExecutor 계약에 따라 충족으로 처리합니다. Git pin은 정의 identity와 미설치 시 acquisition reference이며, 현재 checkout의 변경사항을 원격에 게시하거나 복제하지 않습니다. FMOD for Unity는 프로젝트의 `Assets/Plugins/FMOD`에 별도로 제공해야 합니다. Setup은 해당 외부 통합을 설치하지 않습니다.

| Registry 정의 | 고정 버전 |
| --- | --- |
| UniTask | 2.5.11 |
| LinkMerge | 1.0.0 |
| SoftMask for uGUI | 3.6.5 |
| Unity UI / TextMesh Pro | 2.7.0 |
| Newtonsoft Json | 3.2.2 |
| Burst | 2.0.0 |
| Unity Mathematics | 1.4.0 |
| Unity 모듈 | 1.0.0 |

OpenUPM 정의는 각 패키지의 scope만 선언합니다. dependency-only RegistryPackage는 registry 준비만 수행하고 실제 package 설치는 UPM에 위임합니다. Preview의 Delegated는 package 설치 완료를 증명하지 않습니다. 이 버전 목록은 작성된 고정값이며 latest를 자동 추적하지 않습니다.

## Setup 언어 데이터

새 Setup은 `Editor/Languages`의 en_us·ko_kr·ja_jp 에셋을 사용합니다. 현재 화면과 패키지 metadata에서 사용하지 않는 키는 이 에셋들에서 제거했습니다. 레거시 Installer의 코드와 언어 에셋은 유지합니다.
