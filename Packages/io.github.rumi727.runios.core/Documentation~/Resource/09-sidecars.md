# 9. 사이드카

> 기준 커밋: `7e5d710f`. [← 목차](README.md) · 이전: [8. `AssetRef`와 바인더](08-asset-ref-and-binders.md) · 다음: [10. 언어·로컬라이제이션·JSON](10-languages-and-json.md)

**사이드카**는 에셋 파일 이름 뒤에 `.json`을 붙인, 그 파일에 딸린 JSON 데이터입니다. 특정 핸들이나 레지스트리의 전용 설정 파일이 아니라, 필요한 레지스트리·핸들·기능이 원하는 시점에 읽고 해석하는 별도의 I/O 데이터입니다. 현재 core 밖의 핸들들은 여기에 로드 설정(텍스처 필터, 오디오 로드 방식 등)을 둡니다.

```text
assets/runios/textures/ui/close.png
assets/runios/textures/ui/close.png.json     ← 사이드카
```

- [9.1 형식: 키별 구역](#91-형식-키별-구역)
- [9.2 사이드카 만들기](#92-사이드카-만들기)
- [9.3 읽기와 리비전 비교](#93-읽기와-리비전-비교)
- [9.4 누가 쓰나](#94-누가-쓰나)

## 9.1 형식: 키별 구역

사이드카는 **`Identifier`를 키로 한 JSON 객체**입니다. 키는 읽는 쪽이 정하며, 현재 사용처는 자기 레지스트리 식별자를 키로 씁니다.

```json
{
  "runios:textures": { "…": "텍스처 레지스트리의 로드 설정" },
  "runios:sprites":  { "…": "스프라이트 레지스트리의 설정" }
}
```

같은 `png`를 텍스처 레지스트리와 스프라이트 레지스트리가 함께 읽으므로, 사이드카 하나에 키마다 구역이 생기고 각 핸들은 자기 키의 구역만 꺼냅니다.

```csharp
// Runtime/Resource/AssetSidecar.cs
45  Dictionary<Identifier, JObject> value = [];
47  public T? GetValue<T>(Identifier key) where T : struct
    {
49      if (TryGetValue(key, out T value))
50          return value;
52      return null;                                  // 구역이 없거나 변환 실패 → null (호출자가 기본값 사용)
    }
…
65  public bool TryGetValue<T>(Identifier key, [NotNullWhen(true)] out T? value)
    {
        …
74          value = jObject.ToObject<T>();            // 구역을 설정 타입으로 변환
        …
77      catch (JsonException) { return false; }       // 형식이 틀린 구역은 조용히 무시
81      catch (Exception e) { Debug.LogException(e); return false; }
    }
```

## 9.2 사이드카 만들기

사이드카 객체는 [`SimpleAssetRegistry`](06-simple-asset-registry.md)가 핸들을 만들 때 [`CreateSidecar`](../../Runtime/Resource/SimpleAssetRegistry.cs#L62)로 만듭니다. 이때 파일은 읽지 않고, 1단계 인덱싱에서 모아 둔 보조 파일 목록으로 **있는지 없는지**만 정합니다.

```csharp
// Runtime/Resource/SimpleAssetRegistry.cs
62  protected AssetSidecar CreateSidecar(AssetDiscoveryContext context, IONode node)
    {
64      IONode sidecarNode = node.AddExtension(".json");
65      if (context.TryGetAuxiliary(sidecarNode.path, out IOEntry entry))
66          return new AssetSidecar(sidecarNode, entry.metaData);          // ① present: 같은 팩·레지스트리 폴더에 있었음
68      if (auxiliaryMatcher?.IsMatch(sidecarNode.path) ?? false)
69          return new AssetSidecar(sidecarNode, null); // known missing    // ② missing: 매처로 찾았는데 없었음
71      return new AssetSidecar(sidecarNode); // unknown                     // ③ unknown: 이 레지스트리는 사이드카를 찾지 않음
    }
```

[`AssetSidecarPresence`](../../Runtime/Resource/AssetSidecarPresence.cs#L3)의 세 값은 "사이드카가 없다"와 "사이드카를 찾아보지 않았다"를 구분하려는 것입니다. ② 매처로 훑었는데 없었다면 확실히 없는 것이고, ③ 매처가 없는 레지스트리는 열거에서 `.json`을 보지 않았으므로 있는지 모릅니다.

## 9.3 읽기와 리비전 비교

```csharp
// Runtime/Resource/AssetSidecar.cs
88  public async UniTask Reload()
    {
90      if (await node.file.GetEntry() is not { } entry)
        {
92          value = [];
93          metaData = default;
94          presence = AssetSidecarPresence.missing;
96          return;
        }
99      metaData = entry.metaData;
100     presence = AssetSidecarPresence.present;
102     try
        {
104         string text = await node.file.ReadAllText();
105         value = JsonConvert.DeserializeObject<Dictionary<Identifier, JObject>>(text) ?? [];
        }
107     catch (Exception e)
        {
109         value = [];                                                    // 깨진 사이드카 = 설정 없음
110         Debug.LogError(…);
        }
    }
```

사이드카 내용은 핸들이 **에셋을 불러올 때** `Reload()`로 읽습니다([6.4절](06-simple-asset-registry.md#64-예제-textureassetregistry)). 리로드 단계에서는 읽지 않습니다.

리로드 때 옛 핸들을 재사용할지 정하는 비교([7.5절](07-handles-and-scopes.md#75-issametarget-언제-옛-핸들을-재사용하나))에 사이드카도 참여합니다.

```csharp
// Runtime/Resource/AssetSidecar.cs
114 public bool IsSameTarget(AssetSidecar other)
    {
116     if (!node.IsSameTarget(other.node))
117         return false;
119     if (presence == AssetSidecarPresence.unknown || other.presence == AssetSidecarPresence.unknown)
120         return false;                                       // 모르면 다르다고 봄
122     if (presence != other.presence)
123         return false;                                       // 생겼거나 사라짐
125     return presence switch
        {
127         AssetSidecarPresence.missing => true,               // 둘 다 없음
128         AssetSidecarPresence.present => metaData.IsSameRevision(other.metaData),   // 둘 다 있으면 수정 시간 비교
129         _ => false
        };
    }
```

| 옛 \ 새 | missing | present | unknown |
| --- | --- | --- | --- |
| **missing** | 같음 | 다름 | 다름 |
| **present** | 다름 | 리비전 비교 | 다름 |
| **unknown** | 다름 | 다름 | 다름 |

이 비교는 핸들이 [`IAssetSidecarHandle`](../../Runtime/Resource/IAssetSidecarHandle.cs#L11)을 구현할 때만 쓰입니다. 그래서 사이드카만 고쳐도(이미지는 그대로) 리로드 뒤 새 핸들이 만들어지고, 바인더가 새 설정으로 다시 불러온 에셋으로 갈아탑니다.

> **관찰된 동작:** 옛 핸들의 사이드카는 로드할 때 `Reload()`로 `metaData`와 `presence`가 갱신됩니다. 리로드 때 새로 만든 사이드카는 열거 때의 정보를 가지므로, 비교는 "옛 핸들이 마지막으로 읽은 사이드카"와 "지금 폴더의 사이드카" 사이에서 일어납니다. 로드하기 전의 옛 핸들이 `unknown`이면 항상 다른 대상입니다.

> **관찰된 동작:** `Reload`는 파일을 다 읽기 전에 `presence`를 `present`로 바꿉니다(100행). 오류 로그 문구는 "import settings"입니다(110행).

## 9.4 누가 쓰나

| 핸들 (패키지) | 구역 | `IAssetSidecarHandle` |
| --- | --- | --- |
| `TextureAssetHandle` (texture) | `runios:textures` | O |
| `SpriteAssetHandle` (texture) | `runios:sprites` | O |
| `WaveAudioAssetHandle` (fmod) | `runios:waves` | X |

> **관찰된 동작:** `WaveAudioAssetHandle`은 사이드카를 읽지만 `IAssetSidecarHandle`을 구현하지 않습니다. 대신 `IsSameTarget`을 재정의해 에셋이 로드되어 있을 때만 같은 대상으로 봅니다([`WaveAudioAssetHandle.cs#L31`](../../../io.github.rumi727.runios.fmod/Runtime/Resource/Sounds/WaveAudioAssetHandle.cs#L31)). 그래서 로드되어 있는 동안 사이드카만 바뀌면 리로드 뒤에도 옛 핸들(옛 설정)이 재사용되고, 로드 전이면 파일이 그대로여도 항상 새 핸들이 됩니다.

스프라이트와 텍스처 레지스트리는 같은 `textures/` 폴더를 보므로, `png` 하나에 핸들 두 개와 각자의 사이드카 객체(같은 파일을 가리킴)가 생깁니다.
