# Setup 패키지 정의

이 폴더에는 Setup에서 사용하는 RuniOS 패키지와 종속 패키지의 정의 에셋이 들어 있습니다. Setup 선택 목록은 [RootPackages.asset](../Screens/RootPackages.asset)에 지정합니다.

## 선택 가능한 RuniOS 패키지

| 패키지 | 기능 |
| --- | --- |
| Core | 공통 유틸리티, IO, 비동기 작업, 리소스와 지역화 |
| Sound | 공통 오디오 API, 오디오 소스와 사운드 리소스 |
| UI | 런타임 리소스 바인딩 |
| Effects | UI 둥근 모서리와 메시 외곽선 |
| FMOD | FMOD Core 오디오 시스템과 재생 |
| NBS | Note Block Studio 파일 로드와 FMOD 재생 |
| Texture | FreeImage·Burst 기반 런타임 이미지 로드 |

종속 패키지로 UniTask, R3, LinkMerge, SoftMask for uGUI, Unity UI / TextMesh Pro, Newtonsoft Json, Burst, Unity Mathematics와 Unity 모듈 정의를 포함합니다.

FMOD for Unity는 프로젝트의 `Assets/Plugins/FMOD`에 별도로 제공해야 합니다.
