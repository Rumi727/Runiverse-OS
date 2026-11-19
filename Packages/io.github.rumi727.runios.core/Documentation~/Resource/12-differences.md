# 12. 차이와 기존 문서

> 기준 커밋: `7e5d710f`. [← 목차](README.md) · 이전: [11. 다른 패키지의 레지스트리](11-package-registries.md)

기존 [`Runtime/Resource/README.md`](../../Runtime/Resource/README.md)(와 같은 내용의 `README-EN.md`), XML 주석이 현재 코드와 다른 곳, 그리고 이 문서에 "관찰된 동작"으로 적은 항목의 위치를 모읍니다. 기존 README와 코드는 고치지 않았습니다.

- [12.1 기존 README와 다른 곳](#121-기존-readme와-다른-곳)
- [12.2 XML 주석과 다른 곳](#122-xml-주석과-다른-곳)
- [12.3 관찰된 동작 목록](#123-관찰된-동작-목록)
- [12.4 사용처가 없는 공개 멤버](#124-사용처가-없는-공개-멤버)

## 12.1 기존 README와 다른 곳

| README | 현재 코드 |
| --- | --- |
| 29~35행: 로드 흐름 `BootLoader → Reload → ReloadAll → 레지스트리` | 그 뒤의 완료 이벤트, `IReloadable` 단계, `OnResourcesReady` 호출이 빠져 있습니다. 에디터에서는 `ResourceReloadPostprocessor`가 시작합니다([4.5절](04-reload-pipeline.md#45-누가-리로드를-시작하나)). |
| 53행: 리로드가 끝나면 완료 이벤트 호출 | 이벤트는 레지스트리 단계 뒤, `IReloadable`보다 먼저, 실패한 패스에서도 발생합니다([4.4절](04-reload-pipeline.md#44-완료-단계-이벤트-그다음-ireloadable)). |
| 84·117행: `IdentifierJsonContext`를 넘겨 기본 네임스페이스 지정 | 넘기는 코드가 없어 팩 JSON의 축약 ID는 항상 `runios`입니다([1.2절](01-identifiers.md#12-문자열-변환과-파싱)). |
| 87행: `defaultPack`은 `StreamingIOProvider.instance` 사용 | 그런 타입은 없습니다. 등록된 `IStreamingAssetsService`의 공급자이고, 서비스가 없으면 `null`입니다([2.5절](02-resource-packs.md#25-기본-팩과-에디터-팩)). |
| 88행: `BeforeVanilla`/`AfterVanilla`로 vanilla 앞뒤에 둠 | 둘 다 0번(vanilla보다 위)에 넣습니다([2.4절](02-resource-packs.md#24-활성화-순서와-우선순위)). |
| 186~195행: 핸들 유지·교체·제거, 완료 이벤트에서 최신 핸들 재조회 | 교체·제거된 옛 핸들은 봉인되지 않고 계속 쓸 수 있습니다. 갈아타는 표준 경로는 이벤트가 아니라 `IReloadable`입니다([7.6절](07-handles-and-scopes.md#76-리로드-뒤의-옛-핸들과-seal)). |
| 218행: `unloadDelayFrame` 뒤 언로드 | 기본값 600프레임, 실제 정리는 `DisposeQueue`가 프레임당 10 ms씩 합니다([7.4절](07-handles-and-scopes.md#74-disposequeue)). |
| 282행: `SpriteAssetRegistry`처럼 `Reload()`에서 사이드카를 미리 읽을 수도 있음 | `SpriteAssetRegistry`는 그렇게 하지 않습니다. 사이드카는 `SpriteAssetHandle.Load`에서 읽습니다. |

## 12.2 XML 주석과 다른 곳

| 위치 | 주석 | 코드 |
| --- | --- | --- |
| [`ResourceManager.isPreloaded`](../../Runtime/Resource/ResourceManager.cs#L22) (16~21행) | 에디터에서는 사전 로드 여부, 런타임에서는 첫 리로드 완료 후 `true` | 에디터·런타임 구분이 없고, 실패한 패스에서도 `true`가 됩니다. |
| [`preReloadCompletionEvent`](../../Runtime/Resource/ResourceManager.cs#L46) / [`reloadCompletionEvent`](../../Runtime/Resource/ResourceManager.cs#L52) | 완료 직전 / 완료될 때 | 두 이벤트 모두 `Reload()`의 Task가 끝나기 전, `IReloadable`보다 먼저 발생합니다. |
| [`PackIdentifier.GetHashCode`](../../Runtime/Resource/PackIdentifier.cs#L168) (163행 근처 주석) | 무효 값의 해시는 `int.MinValue` | `0` |
| [`SimpleAssetRegistry`](../../Runtime/Resource/SimpleAssetRegistry.cs#L84) 주석 (53·76·81행 근처) | MD5 사용, 다른 호출을 기다리게 함, 예외를 던짐 | MD5 없음, 게이트로 합침, 예외는 모두 잡아 로그 |
| [`IdentifierConverter`](../../Runtime/Json/Converters/Resource/IdentifierConverter.cs#L13) (18행 근처 주석) | `Version.ToString` 사용 | `Identifier.ToString` |

## 12.3 관찰된 동작 목록

| 항목 | 절 |
| --- | --- |
| 직렬화 콜백의 `nameSpace = null`이 `"runios"`로 저장됨, 곱셈 해시 | [1.1](01-identifiers.md#11-identifier) |
| `IdentifierJsonContext`를 만드는 곳 없음 | [1.2](01-identifiers.md#12-문자열-변환과-파싱) |
| `targetVersion` 검사 없음 | [2.1](02-resource-packs.md#21-팩의-폴더-구조) |
| `pack.json` 삭제 시 `isValid` 유지, 파싱 실패 시 이전 네임스페이스 유지 | [2.3](02-resource-packs.md#23-팩-리로드-packjson과-네임스페이스) |
| `BeforeVanilla`·`AfterVanilla`가 모두 vanilla 위, 활성화 목록 "임시" | [2.4](02-resource-packs.md#24-활성화-순서와-우선순위) |
| 진행률 콜백이 정적 `currentTask`에 씀 | [4.3](04-reload-pipeline.md#43-레지스트리-리로드와-진행률) |
| 에디터 후처리기가 `movedFromAssetPaths`를 보지 않음, 부분 문자열 일치 | [4.5](04-reload-pipeline.md#45-누가-리로드를-시작하나) |
| 실패한 리로드의 부분 인덱스로 교체 | [5.2](05-registries.md#52-추적-먼저-기록한-것이-이긴다) |
| 타입 불일치 시 다음 레지스트리를 보지 않음 | [5.3](05-registries.md#53-조회-세-가지) |
| `Unregister`가 리로드·핸들을 정리하지 않음 | [5.4](05-registries.md#54-등록과-해제-assetregistrymanager) |
| 1단계 `IsTracked`가 항상 `false`, `loadTargetDict`가 `List` | [6.2](06-simple-asset-registry.md#62-리로드-1단계-인덱싱) |
| 비동기 공급자에서 첫 `GetScope` 경합, 어셈블리 리로드 구독 중복 | [7.2](07-handles-and-scopes.md#72-getscope-지연-로드) |
| `Dispose`하지 않은 스코프 | [7.3](07-handles-and-scopes.md#73-returnscope-참조-세기와-지연-언로드) |
| `fileMetaData`가 로드 시점으로 덮임 | [7.5](07-handles-and-scopes.md#75-issametarget-언제-옛-핸들을-재사용하나) |
| `AssetRef` 기본 모드 | [8.1](08-asset-ref-and-binders.md#81-assetref의-세-모드) |
| `direct` 모드 null 검사와 박싱 | [8.2](08-asset-ref-and-binders.md#82-핸들과-스코프-얻기-같은-대상-판정) |
| `OnDisable` 비가상, 꺼진 상태의 setter | [8.3](08-asset-ref-and-binders.md#83-assetscopebinder) |
| 사이드카 비교 시점, `presence` 갱신 순서 | [9.3](09-sidecars.md#93-읽기와-리비전-비교) |
| `WaveAudioAssetHandle`의 사이드카 비교 | [9.4](09-sidecars.md#94-누가-쓰나) |
| 언어 병합 순서, 매 리로드 새 핸들 | [10.1](10-languages-and-json.md#101-languageassetregistry) |
| `ko_kr` 고정 | [10.2](10-languages-and-json.md#102-번역-조회-localizationutility) |
| `PackIdentifierConverter` | [10.3](10-languages-and-json.md#103-json-컨버터) |
| `SoundAssetRegistry`의 `return` | [11](11-package-registries.md) |

## 12.4 사용처가 없는 공개 멤버

저장소 전체를 텍스트로 검색한 결과입니다. 직렬화·리플렉션으로 쓰이는 필드는 제외했습니다.

| 멤버 | 비고 |
| --- | --- |
| `IAssetHandle.Seal()` | 부르는 곳 없음 ([7.6절](07-handles-and-scopes.md#76-리로드-뒤의-옛-핸들과-seal)) |
| `ResourcePack.emptyPack`, `Create(PhysicalPath)` | 사용 없음 |
| `ResourceManager.currentTask`, `reloadables` (읽기) | 외부에서 읽는 곳 없음 |
| `AssetRegistry.isTracking` | 사용 없음 |
| `SimpleAssetRegistry.OnBeginAssetLoop`·`OnAssetLoop`·`OnEndAssetLoop` | 재정의 없음 |
| `AssetSidecar.empty`, `keys`, `count`, `ContainsKey`, `GetValue(Identifier)` | 외부 사용 없음 |
| `IdentifierJsonContext` | 생성하는 곳 없음 |
| `InstanceAssetScope`의 `TAsset` 암시적 변환 | 사용 없음 |
