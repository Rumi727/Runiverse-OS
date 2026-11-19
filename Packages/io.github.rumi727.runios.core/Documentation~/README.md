# Runiverse OS Core 작동 원리

> 기준 커밋: 영역마다 다릅니다. 각 영역 문서의 머리말을 보세요.

`io.github.rumi727.runios.core`는 다른 RuniOS 패키지가 기대는 라이브러리 모음입니다. 서로 독립적인 하위 시스템이 많아서, 처음부터 순서대로 읽는 한 권짜리 문서 대신 **영역마다 따로 읽을 수 있는 문서**로 나눴습니다. 각 영역 폴더의 `README.md`가 그 영역의 목차입니다.

| 영역 | 내용 | 상태 |
| --- | --- | --- |
| [IO](IO/README.md) | `RuniPath`·`PhysicalPath`, `IIOProvider`와 `IONode`, 샌드박스, `GroupIOProvider`, StreamingAssets, Android 에셋, 가상 파일 시스템, Locations | 작성됨 (`b3e1a343`) |
| [Resource](Resource/README.md) | `Identifier`, 리소스 팩, `AsyncReloadGate`, 리로드 파이프라인, 에셋 레지스트리, 핸들과 스코프, `AssetRef`·바인더, 사이드카 | 작성됨 (`7e5d710f`) |
| 부팅·커널 | 수명 주기 훅, `BootLoader`, 마일스톤, `Kernel`, `ServiceRegistry` | 예정 |
| 유틸리티 | `TypeRegistry`, 컬렉션 핸들러, 텍스트, 인스펙터 모델, Undo, 구조체, `MathUtility` 등 | 예정 |

에디터 확장 패키지 `io.github.rumi727.runios.core.editor`는 그 패키지의 `Documentation~`에 따로 둘 예정입니다.
