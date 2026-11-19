# 10. 언어·로컬라이제이션·JSON

> 기준 커밋: `7e5d710f`. [← 목차](README.md) · 이전: [9. 사이드카](09-sidecars.md) · 다음: [11. 다른 패키지의 레지스트리](11-package-registries.md)

core 안의 레지스트리 하나(언어)와 번역 조회, 리소스 타입의 JSON 컨버터를 요약합니다.

- [10.1 `LanguageAssetRegistry`](#101-languageassetregistry)
- [10.2 번역 조회: `LocalizationUtility`](#102-번역-조회-localizationutility)
- [10.3 JSON 컨버터](#103-json-컨버터)

## 10.1 `LanguageAssetRegistry`

[`LanguageAssetRegistry`](../../Runtime/Resource/Languages/LanguageAssetRegistry.cs#L14)(`runios:lang`)는 [`SimpleAssetRegistry`](06-simple-asset-registry.md)를 쓰지 않고 [`AssetRegistry`](05-registries.md#52-추적-먼저-기록한-것이-이긴다)를 직접 상속합니다. 파일 하나가 에셋 하나가 아니라, **여러 팩의 같은 언어 파일을 합친 것**이 에셋 하나이기 때문입니다.

```text
팩 A: assets/runios/lang/ko_kr.json   { "ui.close": "닫기", "ui.open": "열기" }
팩 B: assets/runios/lang/ko_kr.json   { "ui.close": "닫음" }
→ runios:ko_kr = { "ui.close": (먼저 합쳐진 쪽), "ui.open": "열기" }
```

| 항목 | 내용 |
| --- | --- |
| 파일 | 각 네임스페이스의 `lang/**/*.json`. 평평한 `{ "키": "문자열" }` |
| 식별자 | `{네임스페이스}:{lang 기준 경로, 확장자 제외}` (예: `runios:ko_kr`) |
| 에셋 타입 | [`LocalizationData`](../../Runtime/Localizations/LocalizationData.cs#L4) (읽기 전용 사전 하나) |
| 핸들 | [`InstanceAssetHandle<LocalizationData>`](07-handles-and-scopes.md#77-instanceassethandle). 리로드 때 이미 만든 객체를 감쌈 |
| 합치기 | 같은 언어의 키는 `TryAdd`로, 먼저 들어온 값이 남음 |

리로드는 활성화된 팩을 훑어 언어 파일마다 읽기 작업을 만들고(57행), 모두 `WhenAll`로 기다린 뒤 언어별로 합쳐 핸들을 기록합니다(98~103행). 합친 결과는 `calculatedAsset` 사전에도 따로 두고(106행), [`GetAsset`](../../Runtime/Resource/Languages/LanguageAssetRegistry.cs#L122)은 이 사전에서 찾습니다.

> **관찰된 동작:** 파일 읽기 작업들은 동시에 진행되고 끝난 순서대로 합쳐집니다([#L57](../../Runtime/Resource/Languages/LanguageAssetRegistry.cs#L57)~77행). 읽기가 동기로 끝나는 공급자(Physical)에서는 팩 우선순위 순서와 같지만, 비동기 공급자(Android)에서는 같은 키가 여러 팩에 있을 때 어느 팩의 값이 남을지 정해져 있지 않습니다.

> **관찰된 동작:** 매번 새 `LocalizationData` 객체(참조 비교)를 감싸므로 리로드마다 핸들이 새로 기록됩니다([5.2절](05-registries.md#52-추적-먼저-기록한-것이-이긴다)의 재사용이 일어나지 않음). 리로드가 중간에 실패하면 `calculatedAsset`은 이전 값, 인덱스는 부분 값이 될 수 있습니다.

## 10.2 번역 조회: `LocalizationUtility`

번역은 핸들을 거치지 않고 [`LocalizationUtility.GetText`](../../Runtime/Utility/LocalizationUtility.cs#L31)로 바로 찾습니다.

```csharp
// Runtime/Utility/LocalizationUtility.cs
31  public static string? GetText(Identifier identifier, string? languageCode = null)
    {
33      languageCode ??= "ko_kr"; /* TODO : 이거 바꿔라 */
35      LanguageAssetRegistry? registry = AssetRegistryManager.Get<LanguageAssetRegistry>();
36      IReadOnlyDictionary<string, string>? asset = registry?.GetAsset(new Identifier(identifier.nameSpace, languageCode));   // ① 키의 네임스페이스 = 언어 파일의 네임스페이스
38      return asset?.GetValueOrDefault(identifier.path.value);                                                             // ② 키의 경로 = 파일 안의 키
    }
```

번역 키도 식별자입니다. `runios:resource.loading.title`은 `runios` 네임스페이스의 언어 파일에서 `resource.loading.title`을 찾습니다. 그래서 모드는 자기 네임스페이스에 언어 파일을 두면 core의 키와 충돌하지 않습니다.

[`GetFormatSegments`](../../Runtime/Utility/LocalizationUtility.cs#L43)는 번역 문자열을 `{0}` 같은 서식 조각으로 파싱한 결과를 캐시하고, 캐시는 [`preReloadCompletionEvent`](04-reload-pipeline.md#44-완료-단계-이벤트-그다음-ireloadable)에서 비웁니다(11행).

> **관찰된 동작:** 언어 코드를 주지 않으면 `"ko_kr"`로 고정됩니다(33·45행, TODO 주석).

## 10.3 JSON 컨버터

| 타입 | 컨버터 | JSON |
| --- | --- | --- |
| `Identifier` | [`IdentifierConverter`](../../Runtime/Json/Converters/Resource/IdentifierConverter.cs#L13) | `"ns:path"`. `null` → `Identifier.empty`. 네임스페이스 없으면 `IdentifierJsonContext` 또는 `runios` ([1.2절](01-identifiers.md#12-문자열-변환과-파싱)) |
| `PackIdentifier` | [`PackIdentifierConverter`](../../Runtime/Json/Converters/Resource/PackIdentifierConverter.cs#L15) | `{ "identifier": "ns:path" }` 또는 `{ "path": "/abs/path" }`. 다른 속성은 건너뜀. `null` → `PackIdentifier.empty` |

> **관찰된 동작 (실행으로 확인하지 않음):** `PackIdentifierConverter.WriteJson`은 `Identifier?`·`PhysicalPath?` 값을 `JsonWriter.WriteValue`에 그대로 넘깁니다(38·43행). `ReadJson`은 첫 `identifier`/`path` 속성을 읽자마자 반환하므로(98·102행) 객체의 끝(`EndObject`)을 소비하지 않습니다.
