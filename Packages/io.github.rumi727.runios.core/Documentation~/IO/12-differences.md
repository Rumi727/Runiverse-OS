# 12. 공급자 차이와 기존 문서

> 기준 커밋: `b3e1a343`. [← 목차](README.md) · 이전: [11. 사용처](11-consumers.md)

앞 장들에 흩어진 공급자별 차이를 한 표로 모으고, 기존 [`Runtime/IO/README.md`](../../Runtime/IO/README.md)와 XML 주석이 현재 코드와 다른 곳을 정리합니다. 이 장의 "관찰된 동작"은 의도인지 확인되지 않은 동작입니다. 기존 README와 코드는 고치지 않았습니다.

- [12.1 공급자 차이표](#121-공급자-차이표)
- [12.2 기존 README와 다른 곳](#122-기존-readme와-다른-곳)
- [12.3 XML 주석과 다른 곳](#123-xml-주석과-다른-곳)
- [12.4 사용처가 없는 공개 멤버](#124-사용처가-없는-공개-멤버)

## 12.1 공급자 차이표

| 동작 | Physical | Group | Android | Virtual | 장 |
| --- | --- | --- | --- | --- | --- |
| 쓰기 | O | X | X | O | [2.1](02-providers-and-nodes.md#21-계층-한눈에-보기) |
| `isThreadSafe` | `true` | 자식 모두면 `true` | `false` (실제로는 스레드 풀) | `false` | [2.2](02-providers-and-nodes.md#22-iioprovider-읽기-계약) |
| 읽기·쓰기 실행 | 호출 스레드, 동기 완료 | 고른 자식 | 스레드 풀 | 호출 스레드 | [4.5](04-physical-and-sandbox.md#45-읽기쓰기는-동기로-끝난다) |
| 열거 실행 | 스레드 풀 | 자식 | 스레드 풀 | 호출 스레드 | [3.4](03-enumeration-and-matchers.md#34-공급자별-열거-차이) |
| 열거 순서 | 파일 시스템 | 공급자 순서 | 너비 우선 | 이름순, 후위 | [3.4](03-enumeration-and-matchers.md#34-공급자별-열거-차이) |
| 없는 디렉터리 열거 | 예외 | 해당 자식 건너뜀 | 빈 결과 | 빈 결과 | [3.4](03-enumeration-and-matchers.md#34-공급자별-열거-차이) |
| 열거 취소 | 조용히 끝남 (토큰 미전달) | 예외 | 조용히 끝남 | 예외 | [3.3](03-enumeration-and-matchers.md#33-enumerateonthreadpool-스레드-풀에서-열거하기) |
| 존재 확인, 부모 없음·종류 다름 | `false` | 자식 중 하나라도 `true` | `false` | **예외** | [9.5](09-virtual-fs.md#95-공급자로-노출하기) |
| 존재 확인 vs `GetEntry` | 일치 | **불일치 가능** | 일치 | 일치 | [6.3](06-group-provider.md#63-존재-확인-하나라도-있으면-참) |
| 경로 대소문자 (열거 결과) | Windows: 소문자 / 그 밖: 유지 | 자식 | 유지 | 유지 | [1.5](01-paths.md#15-physicalpath-디스크에-닿을-때) |
| `lastWriteTime` | 실제 값 | 자식 | 없음 | 생성 시각에서 안 바뀜 | [2.5](02-providers-and-nodes.md#25-ioentry와-filemetadata) |
| 링크 | 정책에 따라 예외 | 자식 | — | — | [4.4](04-physical-and-sandbox.md#44-열거와-샌드박스) |
| 없는 파일 삭제 | 조용히 넘어감 | — | — | 예외 | [5.5](05-write-operations.md#55-공급자별-재정의) |
| `Dispose` | 빈 메서드 | `leaveOpen`이 아니면 자식 dispose | 빈 메서드 | 빈 메서드 | [6.6](06-group-provider.md#66-dispose와-leaveopen) |

## 12.2 기존 README와 다른 곳

`Runtime/IO/README.md`와 같은 내용의 `README-EN.md`에 공통입니다.

| README | 현재 코드 |
| --- | --- |
| 11행: 공급자 예로 "압축 파일, 원격 저장소"를 듦 | 그런 공급자는 없습니다. `IIOProvider.isIndependent`의 XML 주석([`#L25`](../../Runtime/IO/IIOProvider.cs#L25))도 압축 파일을 예로 듭니다. |
| 45행: 읽기 API는 모두 비동기 | 시그니처는 비동기지만 Physical은 호출 스레드에서 동기로 끝납니다([4.5절](04-physical-and-sandbox.md#45-읽기쓰기는-동기로-끝난다)). |
| 82~114행 (경로 규칙) | Windows 물리 경로 소문자화, Windows에서 `\`가 구분자가 되는 것, 루트 안에 머무는 `..`이 해석되어 통과하는 것은 적혀 있지 않습니다([1.5절](01-paths.md#15-physicalpath-디스크에-닿을-때)). |
| 183~187행: `BlockLinks`는 링크를 지나는 모든 경로를 막음 | `targetPath` 자신·조상은 검사하지 않고, `GetPhysicalPath`는 검사를 거치지 않으며, 열거 중 링크는 열거 전체를 끝냅니다([4.3](04-physical-and-sandbox.md#43-validatesandboxpolicy), [4.4](04-physical-and-sandbox.md#44-열거와-샌드박스), [4.6](04-physical-and-sandbox.md#46-샌드박스를-거치지-않는-길)). |
| 195, 208행: 환경마다 서비스가 하나, "그 밖의 플랫폼"은 Player | Player 어셈블리는 WebGL도 제외하므로 WebGL에는 서비스가 없습니다([7.2절](07-streaming-assets.md#72-환경별-구현)). 기본 팩 생성 코드는 서비스가 여럿인 경우도 처리합니다. |
| 197~220행 (에디터 StreamingAssets) | `EditorStreamingAssets`·`editorProvider`, 관리형 패키지의 읽기 전용 `PackageIOProvider`, `.meta` 열거, 패키지 사이 순서가 정해지지 않은 것은 적혀 있지 않습니다([7.3](07-streaming-assets.md#73-에디터-프로젝트와-패키지-겹치기), [7.5](07-streaming-assets.md#75-editorstreamingassets)). |
| 232~244행: Group은 첫 공급자의 엔트리를 씀 | `GetEntry`와 읽기는 맞지만, `FileExists`/`DirectoryExists`는 어느 공급자든 있으면 `true`입니다([6.3절](06-group-provider.md#63-존재-확인-하나라도-있으면-참)). |
| 283행: 가상 바로가기 파일에 쓰기를 시작하면 원본을 메모리로 복사 (284행은 `Create`만 예외로 듦) | `OpenWrite`만 복사합니다. `WriteAll*`도 `Create`와 같은 경로라 복사 없이 비웁니다([9.4절](09-virtual-fs.md#94-virtualfile-버퍼와-바로가기)). |
| 302~303행: 핸들은 `node.file.OpenRead()`로 읽음 | 텍스처는 `ReadAllBytes`, FMOD는 `GetPhysicalPath`를 씁니다([11.4절](11-consumers.md#114-에셋-로더-바이트-vs-물리-경로)). |
| 321~322행: `IPrecalculatedIOChecksum`으로 미리 계산한 체크섬 사용 | 구현한 공급자가 없습니다([2.7절](02-providers-and-nodes.md#27-체크섬)). |

## 12.3 XML 주석과 다른 곳

| 위치 | 주석 | 코드 |
| --- | --- | --- |
| [`RuniPath.GetRelativePath`](../../Runtime/IO/RuniPath.cs#L157) (153~156행 주석) | 접두사가 맞지 않으면 이 경로를 돌려줌 | 빈 경로를 돌려줌([`RuniPathUtility.cs#L276`](../../Runtime/Utility/RuniPathUtility.cs#L276)) |
| [`IIOProvider.isThreadSafe`](../../Runtime/IO/IIOProvider.cs#L65) (54~64행 주석) | `false`면 호출한 스레드에서만 쓰임 | Android(`false`)는 스레드 풀에서 실행하고, `GetFileChecksum`은 공급자와 관계없이 스레드 풀에서 읽음 |
| [`PhysicalPath.NormalizePath`](../../Runtime/IO/PhysicalPath.cs#L267) (247~266행 주석) | 소문자화·`/` 변환 언급 없음 | Windows 소문자화, `/` → `\` |
| [`IONode.GetFileChecksum`](../../Runtime/IO/IONode.Checksum.cs#L16) (12~15행 주석) | MD5를 돌려줌 | `IPrecalculatedIOChecksum` 공급자면 그 공급자의 값을 그대로 돌려줌 |
| [`FileExtension`](../../Runtime/IO/FileExtension.cs#L14) 특성 | — | `[JsonConverter(typeof(FileExtension))]`로 타입 자신을 지정. `FileExtensionConverter`는 미참조([1.6절](01-paths.md#16-직렬화)) |

## 12.4 사용처가 없는 공개 멤버

저장소 전체를 텍스트로 검색한 결과입니다. 리플렉션·직렬화로 쓰이는 경우는 놓칠 수 있습니다.

| 멤버 | 비고 |
| --- | --- |
| `IPrecalculatedIOChecksum` | 구현 없음 |
| `IRootAccessService` | 구현 없음 (파일 브라우저가 서비스를 찾기는 함) |
| `IONode.GetFileChecksum`, `IOWriteNode.GetFileChecksum` | 호출 없음 |
| `IIOProvider.isIndependent` | 모든 공급자가 구현하지만 읽는 곳 없음 |
| `RegexMatcher` | 생성하는 곳 없음 |
| `IPatternMatcher`의 `allMatcher`, `textMatcher`, `videoMatcher`, `compressMatcher`, `codeMatcher` | 사용 없음 |
| `IONode`/`IOWriteNode`의 `RemoveExtension`, `SetExtension`, `IOWriteNode.empty` | 사용 없음 |
| `IKnownDirectoryService.TryGetDirectory` | 사용 없음 |
| `MountSpaceInfo` | `StorageSpaceInfo`와 같은 모양, 사용 없음 |
| `FileExtensionConverter` | 참조 없음 |
| `RuniOS.IO.Virtual` 전체 | `Assets/IOBrowserTestWindow.cs`만 사용 |
