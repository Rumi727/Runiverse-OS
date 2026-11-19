# 11. 다른 패키지의 레지스트리

> 기준 커밋: `7e5d710f`. [← 목차](README.md) · 이전: [10. 언어·로컬라이제이션·JSON](10-languages-and-json.md) · 다음: [12. 차이와 기존 문서](12-differences.md)

core는 레지스트리 틀과 언어 레지스트리만 가지고, 나머지 에셋 종류는 각 패키지가 레지스트리를 더합니다. 모두 `[OnCodeLoaded]`에서 [`AssetRegistryManager.Register<T>()`](05-registries.md#54-등록과-해제-assetregistrymanager)로 등록하고 `[OnCodeUnloading]`에서 해제합니다.

| 레지스트리 (패키지) | id | 기반 | 폴더 / 파일 | 에셋 타입 | 핸들 | 특징 |
| --- | --- | --- | --- | --- | --- | --- |
| `TextureAssetRegistry` (texture) | `runios:textures` | `SimpleAssetRegistry` | `textures/` 그림 파일 + 사이드카 | `Texture2D` | `TextureAssetHandle` | [6.4절](06-simple-asset-registry.md#64-예제-textureassetregistry)의 예제 |
| `SpriteAssetRegistry` (texture) | `runios:sprites` | `SimpleAssetRegistry` | **`textures/`** (텍스처와 같은 폴더) | `Sprite` | `SpriteAssetHandle` | 로드할 때 같은 식별자의 `Texture2D` 스코프를 [자동 조회](05-registries.md#53-조회-세-가지)로 빌려 스프라이트를 만들고, 언로드 때 반환 |
| `WaveAudioAssetRegistry` (fmod) | `runios:waves` | `SimpleAssetRegistry` | `sounds/` 오디오 파일 + 사이드카 | `WaveAudioClip` | `WaveAudioAssetHandle` | 사이드카 `loadMode`로 일반·압축 유지·스트리밍. `IsSameTarget` 재정의([9.4절](09-sidecars.md#94-누가-쓰나)) |
| `SoundAssetRegistry` (sound) | `runios:sounds` | `AssetRegistry` 직접 | 네임스페이스마다 `sounds.json` 한 파일 | `SoundClipRef` | `InstanceAssetHandle<SoundClipRef>` | JSON의 `"이름": ResourceKey`로 다른 레지스트리의 에셋(대개 `runios:waves`)을 가리키는 별칭 |
| `NBSAssetRegistry` (nbs) | `runios:nbs` | `SimpleAssetRegistry` | `nbses/` `.nbs` 파일 | `NoteBlockClip` | `NBSAssetHandle` | 레지스트리 폴더 이름을 `registryName`으로 바꿈 |
| `VectorImageAssetRegistry` (uielements) | `runios:vectors` | `SimpleAssetRegistry` | `vectors/` `.svg` | `VectorImage` | `VectorImageAssetHandle` | |
| `LanguageAssetRegistry` (core) | `runios:lang` | `AssetRegistry` 직접 | `lang/**/*.json` | `LocalizationData` | `InstanceAssetHandle<LocalizationData>` | [10장](10-languages-and-json.md) |

두 갈래가 있습니다.

- **파일 하나 = 에셋 하나**인 종류(텍스처, 오디오, NBS, SVG)는 `SimpleAssetRegistry`를 상속하고 매처와 `CreateHandle`만 정합니다. 에셋은 [스코프를 요청할 때](07-handles-and-scopes.md#72-getscope-지연-로드) 불러옵니다.
- **여러 파일을 합치거나 다른 에셋을 가리키는** 종류(언어, 사운드 별칭)는 `AssetRegistry`를 직접 상속해 리로드 때 값을 만들고, 이미 만든 객체를 `InstanceAssetHandle`로 기록합니다.

각 핸들의 자세한 동작은 해당 패키지 문서를 보세요: [Sound 7. 리소스](../../../io.github.rumi727.runios.sound/Documentation~/07-resources.md), [FMOD 9. 리소스와 에디터](../../../io.github.rumi727.runios.fmod/Documentation~/09-resources-and-editor.md), [NBS 문서](../../../io.github.rumi727.runios.nbs/Documentation~/README.md).

> **관찰된 동작:** `SoundAssetRegistry`의 리로드 루프는 어떤 네임스페이스에 `sounds.json`이 없으면 `continue`가 아니라 `return`합니다([`SoundAssetRegistry.cs#L53`](../../../io.github.rumi727.runios.sound/Runtime/Resource/Sounds/SoundAssetRegistry.cs#L53)). 그 시점까지 만든 읽기 작업은 아직 실행되지 않았으므로, `finally`의 `EndTracking`이 그때까지 기록된(대개 빈) 인덱스로 교체합니다.
