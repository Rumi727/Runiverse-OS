# Runiverse OS
Game Engine Library for Unity Engine

It's currently under development!!!!\
The basic system hasn't even been implemented!!!!!!

When this library is completed, it will replace the existing [SC KRM 1.0](https://github.com/Rumi727/SC-KRM-1.0/) library.

한창 개발 중입니다!!!!\
기초 시스템 구현 조차 안됐어요!!!!!!

## 참고 사항

* 추후 System.Text.Json으로의 전환을 고려해야합니다.
  * 항상 퍼블릭 프로퍼티를 사용해야하며, 정적 멤버는 허용되지 않습니다.
* 추후 유니티가 CoreCLR로 전환될 가능성도 고려해야합니다.
  * 현재 이 프로젝트가 사용중인 C# 언어 버전은 10 입니다. (global using 사랑해요)
  * 아 맞다 파일 스코프 네임스페이스 절대엄금
    * 미친 유니티가 올바른 MonoBehaviour나 ScriptableObject, Editor등으로 인식 못함 (= 직렬화가 안됨)
* 유니티 에디터 자채를 패치하여 Roslyn 버전을 올려 C# 버전을 올릴 수 있지만, 이 프로젝트는 모두가 사용할 수 있게끔 의도하였기에 (실제로 쓰는 사람은 없겠지만...) 가능한 유니티 순정과의 호환성을 염두에 두고 제작 중입니다.

## TODO

* 세이브 데이터
* FMOD 통합
  * 아 슈발 설계 어떻게 해야할지 생각 하나도 안나네
* 리듬 시스템
* 텍스쳐 및 스프라이트
  * apng 같은 애니메이션 텍스쳐
* 글로벌 폰트
* 메쉬 리소스팩 지원
  * 런타임 fbx 로더가 안보여서 어쩔수 없이 에셋 번들 같은거로 가게 될듯...
  * 이거 안하면 리겜 커스텀 맵 지원 못한다.
* 닷넷 확장 InternalTo어쩌구저쩌구로 internal을 대비한 다른 runios 어셈블리에도 표시
  * 이거 자꾸 까먹네 쉬운건데
* 기초 UI 작업
  * 설정
  * 폴더/파일 입력창
  * 드래그 앤 드랍
  * 알림창
  * 테마 시스템
  * 인스펙터
    * 애니메이션 커브 지원
  * 리듬 시스템을 위한 타임라인
  * 막막하다... 언제 다하고 있냐... 안그래도 어려운게 UI인데ㅋㅋ
* 모딩 API 작업
* IOHandle를 통한 안드로이드 스트리밍 에셋 지원
* 입력 시스템
* DefaultDrawer로 스트리밍 에셋의 인스펙터에 리소스팩 관련 에딧 지원
지원
* 기존 Runi Engine의 카메라, 캔버스 컴포넌트 가져와서 스크린 크롭 등 기능 추가
* 이외에도 여러 기능들...
