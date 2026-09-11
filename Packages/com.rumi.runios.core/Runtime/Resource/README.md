# 리소스 시스템 개요

Language available: \[[**한국어 (대한민국)**](README.md)\] \[[English (US)](README-EN.md)\]  

## 개요

이 프로젝트의 리소스 시스템은 Minecraft의 리소스 팩 구조에서 아이디어를 가져왔습니다.\
파일은 리소스 팩에 들어 있고, 에셋 레지스트리는 `Identifier`를 입력받아 자신이 약속한 타입의 에셋을 제공할 수 있는 핸들을 등록합니다.

```text
Identifier
-> AssetRegistry
-> AssetHandle
-> AssetScope
-> asset object
```

레지스트리의 핵심 계약은 **하나의 식별자가 하나의 에셋을 가리킨다**는 것입니다.\
그 에셋을 만들기 위해 어떤 파일을 읽는지, 여러 파일을 병합하는지, 다른 네임스페이스나 레지스트리를 참조하는지, 실제 객체를 리로드 단계에서 미리 만드는지 여부는 각 레지스트리 구현이 결정합니다.\
같은 물리 파일도 서로 다른 레지스트리가 독립적으로 해석할 수 있습니다. 예를 들어 같은 오디오 파일을 FMOD 레지스트리는 `FMOD.Sound`로, 다른 레지스트리는 Unity `AudioClip`로 제공할 수 있습니다.

일반적인 파일 기반 핸들은 `GetScope()`가 호출될 때 실제 객체를 지연 로드하지만, 이는 레지스트리 전체의 강제 계약이 아닙니다.\
`InstanceAssetHandle<TAsset>`처럼 이미 만들어진 객체를 감싸거나, `LanguageAssetRegistry`처럼 리로드 단계에서 데이터를 파싱하고 실제 객체까지 만들어 등록하는 구현도 가능합니다.

## 로드 흐름

초기 로드는 `BootLoader`에서 시작됩니다.

```text
BootLoader
-> ResourceManager.Reload
-> ResourcePack.ReloadAll
-> AssetRegistryManager.GetAll
-> AssetRegistry.Reload
```

런타임 조회는 레지스트리와 키를 통해 이루어집니다.

```text
ResourceKey
-> ResourceManager.GetHandle
-> AssetRegistryManager.Get
-> AssetRegistry[assetId]
-> AssetHandle.GetScope
```

`ResourceManager.Reload()`는 이미 리로드 중일 때 중복 실행하지 않습니다.\
대신 리로드 요청을 표시하고, 현재 리로드가 끝난 뒤 필요한 경우 한 번 더 리로드합니다.

모든 리소스 팩이 먼저 `ResourcePack.ReloadAll()`로 갱신됩니다.\
그 다음 현재 활성화된 리소스 팩 스냅샷을 각 에셋 레지스트리에 넘겨서 레지스트리들을 병렬로 리로드합니다.

리로드가 끝나면 `preReloadCompletionEvent`, `reloadCompletionEvent`가 호출됩니다.\
렌더러나 UI 같은 시스템은 이 시점에 자신이 들고 있던 핸들이 아직 레지스트리의 최신 핸들인지 확인하고, 필요하면 다시 가져올 수 있습니다.

## 리소스 팩 구조

리소스 팩의 루트에는 `pack.json`이 있습니다.\
에셋은 `assets` 폴더 아래에 네임스페이스별로 들어갑니다.

```text
pack.json
assets/
  runios/
    lang/
      ko_kr.json
      en_us.json
    sounds.json
    sounds/
      ui/click.ogg
```

기본 식별자 형식은 `namespace:path`입니다.

```text
runios:lang
runios:ui/click
```

네임스페이스가 없는 축약 형식은 사용하는 API에 따라 기본 네임스페이스를 정합니다.

- `Identifier.Parse("path")`처럼 `defaultNamespace`를 생략하거나, `"path"` -> `Identifier` 변환을 사용하는 경우 호출 어셈블리의 `[assembly: DefaultIdentifierNamespace("...")]` 값이 사용됩니다.
- 호출 어셈블리에 `DefaultIdentifierNamespaceAttribute`가 없으면 시스템 폴백인 `runios`가 사용됩니다.
- `Identifier.Parse`와 `"path"` -> `Identifier` 변환은 비권장 편의 API입니다. 텍스트 파싱과 호출 어셈블리 확인이 필요하므로 UI나 저작 도구처럼 편의성이 중요한 곳에만 사용하세요.
- 생성자는 호출 어셈블리를 보지 않습니다. 네임스페이스와 경로를 이미 알고 있으면 `new Identifier(nameSpace, path)`가 권장되는 빠른 경로입니다.
- JSON 로더처럼 의미 있는 호출자가 없는 코드 경로도 명시적으로 `Identifier.defaultNamespace`를 전달하므로 `runios`를 폴백으로 사용합니다.
- JSON 로더가 소유 네임스페이스를 알고 있으면 `IdentifierJsonContext`를 `JsonSerializerSettings.Context`로 전달해 축약형 ID의 기본 네임스페이스를 지정할 수 있습니다.
- 리소스 팩 파일은 `assets/{namespace}/...` 폴더 이름에서 네임스페이스를 가져오므로 이 기본값 규칙에 의존하지 않습니다.

이 규칙은 프레임워크가 `runios` 네임스페이스를 쓰더라도, 게임이나 모드의 축약 ID가 프레임워크 네임스페이스로 고정되지 않도록 하기 위한 것입니다.

`ResourcePack.defaultPack`은 `vanilla` 팩이며 `StreamingIOProvider.instance`를 사용합니다.\
`RequiredPackSort.BeforeVanilla`, `RequiredPackSort.AfterVanilla`를 통해 필수 팩의 위치를 `vanilla` 앞뒤로 둘 수 있습니다.

## ResourceKey와 Identifier

`Identifier`는 네임스페이스와 경로로 이루어진 ID입니다.

```csharp
Identifier id = new Identifier("runios", "ui/click");
```

게임이나 패키지 어셈블리에는 기본 식별자 네임스페이스를 지정할 수 있습니다.

```csharp
#nullable enable
using RuniOS.Resource;

[assembly: DefaultIdentifierNamespace("my_game")]
```

그 어셈블리 안에서 `Identifier.Parse`의 기본 네임스페이스를 생략하거나 문자열 암시 변환을 사용하면 `my_game`이 사용됩니다.

```csharp
Identifier id = Identifier.Parse("ui/click");
// my_game:ui/click

Identifier sameId = "ui/click";
// my_game:ui/click
```

런타임 코드, 저장 데이터, 대량 로드 경로에서는 네임스페이스를 명시하는 생성자를 사용하세요.

```csharp
Identifier id = new Identifier("my_game", "ui/click");
```

공유 코드, 저장 데이터, 문서 예제처럼 호출 위치가 의미를 바꾸면 안 되는 곳에서는 네임스페이스를 명시하는 편이 안전합니다.

JSON 역직렬화는 호출 어셈블리 대신 serializer 컨텍스트를 봅니다. 소유 네임스페이스가 있는 JSON을 읽을 때는 `IdentifierJsonContext`를 넘길 수 있습니다.

```csharp
#nullable enable
using Newtonsoft.Json;
using RuniOS.Resource;
using System.Runtime.Serialization;

JsonSerializerSettings settings = new JsonSerializerSettings
{
    Context = new StreamingContext
    (
        StreamingContextStates.Other,
        new IdentifierJsonContext("my_game")
    )
};

MyData? data = JsonConvert.DeserializeObject<MyData>(json, settings);
```

`ResourceKey`는 레지스트리 ID와 에셋 ID를 같이 저장합니다.

```csharp
ResourceKey key = new ResourceKey
(
    new Identifier("runios", "sounds"),
    new Identifier("runios", "ui/click")
);
```

즉 `registryId`는 "어떤 레지스트리에서 찾을 것인가"이고, `assetId`는 "그 레지스트리 안의 어떤 에셋인가"입니다.

## AssetRegistryManager

`AssetRegistryManager`는 등록된 모든 레지스트리를 관리합니다.

```csharp
AssetRegistryManager.Register<MyAssetRegistry>();
```

레지스트리는 코드 생명주기에 맞춰 등록하고 해제합니다. 현재 구현에서는 보통 `[OnCodeLoaded]`에서 등록하고 `[OnCodeUnloading]`에서 해제합니다.

레지스트리는 다음 기준으로 조회됩니다.

```text
registryId       -> AssetRegistryManager.Get(registryId)
registry type    -> AssetRegistryManager.Get<TRegistry>()
asset type       -> AssetRegistryManager.GetAllForAsset(assetType)
first registry   -> AssetRegistryManager.GetFirstForAsset<TAsset>()
```

같은 에셋 타입의 레지스트리 중 `priority`가 가장 높은 레지스트리가 선택됩니다.\
`AssetRegistryManager.GetFirstForAsset<TAsset>()`는 캐시된 최고 우선순위 레지스트리를 반환합니다.\
같은 `priority`인 레지스트리의 선택 순서는 계약에 포함되지 않습니다. 현재 구현에서는 먼저 등록된 레지스트리가 선택될 수 있지만, 이 동작에 의존하지 마세요.\
우선 레지스트리는 등록 시 캐시되므로, 등록 후 `priority`를 변경하지 마세요.\
키 모드의 `AssetRef<T>` 인스펙터 필드는 이 정보를 사용해 호환되는 레지스트리와 에셋을 고를 수 있습니다. 직접 모드에서는 레지스트리 조회 없이 참조에 저장된 에셋 인스턴스를 사용합니다.

## 빠른 리로드 구조

레지스트리는 리로드 때 자신의 `Identifier -> AssetHandle` 인덱스를 다시 계산합니다.\
`AssetRegistry<THandle>`는 리로드 중 임시 추적 테이블을 만들고, 새 패스에서 기록된 핸들과 기존 핸들을 비교합니다.

```text
BeginTracking
-> RecordAssetHandle
-> EndTracking
```

`RecordAssetHandle`은 같은 ID의 기존 핸들이 있고, 새 핸들이 `IsSameTarget()` 기준으로 같은 대상을 가리킨다면 기존 핸들을 재사용합니다.

```text
same identifier + same target -> keep old handle
same identifier + changed target -> replace with new handle
missing from reload pass -> remove from registry
```

따라서 레지스트리는 매번 전체 등록 결과를 다시 계산할 수 있으면서도, 바뀌지 않은 핸들과 이미 로드된 객체는 그대로 유지할 수 있습니다.\
핸들이 교체된 뒤에도 기존 핸들을 들고 있는 시스템은 리로드 완료 이벤트에서 최신 핸들을 다시 조회할 수 있습니다.

단, **리로드가 반드시 단순한 파일 인덱싱이어야 하는 것은 아닙니다.** `SimpleAssetRegistry`는 대부분 파일 탐색과 메타데이터 비교만 수행하지만, 직접 구현한 레지스트리는 JSON을 파싱하거나 여러 파일을 병합하거나 `InstanceAssetHandle<TAsset>`에 넣을 실제 에셋 객체를 리로드 단계에서 생성해도 됩니다. 이 비용과 생명주기의 책임은 해당 레지스트리 구현에 있습니다.

## AssetHandle과 AssetScope

`IAssetHandle`은 레지스트리가 등록한 에셋 핸들의 공통 계약입니다. 모든 핸들이 파일이나 사이드카를 가져야 하는 것은 아닙니다.\
`AssetHandle<TAsset>`는 일반적인 지연 로드/언로드와 스코프 생명주기를 제공하는 기본 구현이며, 실제 에셋은 필요할 때 `GetScope()`를 통해 로드됩니다.

```csharp
IAssetScope<MyAsset>? scope = await handle.GetScope();
if (scope == null)
    return;

using (scope)
{
    MyAsset asset = scope.asset;
}
```

`AssetScope<TAsset>`는 에셋 사용권입니다.\
사용이 끝나면 반드시 `Dispose()`해야 합니다.

스코프가 모두 반환되면 일반 `AssetHandle<TAsset>`은 `unloadDelayFrame` 뒤에 언로드를 시도합니다.\
따라서 짧은 시간 안에 같은 에셋이 다시 요청되는 경우 불필요한 언로드와 재로드를 줄일 수 있습니다.

`InstanceAssetHandle<TAsset>`은 이미 존재하는 객체를 직접 감싸며 파일 로드나 사이드카를 요구하지 않습니다.

`IsSameTarget()`은 리로드에서 기존 핸들을 재사용해도 되는지 판단합니다. 일반 `AssetHandle<TAsset>` 구현은 실제 핸들 타입, I/O 대상, 파일 리비전 메타데이터를 비교하며, 핸들이 `IAssetSidecarHandle`도 구현한다면 연결된 `AssetSidecar`의 대상과 리비전도 함께 비교합니다.\
`FileMetaData.IsSameRevision()`은 `lastWriteTime`을 필수 비교 기준으로 사용하고, 양쪽에서 제공되는 경우 `size`와 `creationTime`도 추가로 비교합니다. 비교에 필요한 수정 시간을 알 수 없으면 동일한 리비전으로 취급하지 않습니다.

## AssetSidecar

`AssetSidecar`는 특정 I/O 에셋에 연결된 확장 가능한 JSON 사이드카를 나타냅니다.\
사이드카는 특정 핸들이나 레지스트리의 전용 "임포트 설정"이 아니며, 필요한 레지스트리·핸들·기능이 원하는 시점에 읽고 해석할 수 있는 별도의 I/O 데이터입니다.

사이드카 파일 이름은 원본 파일 이름 전체 뒤에 `.json`을 붙입니다.

```text
assets/runios/sounds/ui/click.ogg
assets/runios/sounds/ui/click.ogg.json

assets/runios/textures/character.png
assets/runios/textures/character.png.json
```

`AssetSidecar`의 최상위 데이터는 `Dictionary<Identifier, JObject>` 형태입니다.\
각 `Identifier`는 해당 섹션을 해석하는 레지스트리나 기능의 이름 영역으로 사용할 수 있으며, 원본 에셋의 식별자와 같을 필요는 없습니다.

```json
{
  "runios:waves": {
    "loadMode": "stream"
  },
  "runios:sprites": {
    "idle": {
      "rect": [0, 0, 32, 32]
    }
  },
  "my_game:music": {
    "bpm": 128,
    "artist": "Example Artist"
  }
}
```

알 수 없는 식별자와 필드도 `JObject` 형태로 보존되므로, 코어 타입을 수정하지 않고 새로운 소비자가 자기 섹션을 추가할 수 있습니다.

`Reload()`는 현재 `IONode`의 내용을 다시 읽어 사이드카의 현재 상태를 반영합니다.\
파일이 존재하지만 JSON을 읽거나 역직렬화하지 못한 경우 데이터는 빈 상태로 취급되고 오류가 기록되지만, 파일 자체의 `FileMetaData`는 현재 엔트리의 리비전을 유지합니다. 파일이 존재하지 않는 경우에는 데이터와 메타데이터가 빈 상태가 됩니다.

```csharp
await sidecar.Reload();

Identifier key = new Identifier("my_game", "music");
JObject? rawData = sidecar[key];
MusicData? typedData = sidecar.GetValue<MusicData>(key);

if (sidecar.TryGetValue<MusicData>(key, out MusicData? data))
{
    // data 사용
}
```

`IAssetHandle` 자체는 사이드카를 요구하지 않습니다. 사이드카가 자신의 대상 동일성이나 로드 과정에 필요한 핸들만 `IAssetSidecarHandle`을 구현해 `AssetSidecar sidecar`를 제공합니다.\
따라서 `InstanceAssetHandle<TAsset>`처럼 사이드카가 필요 없는 핸들은 빈 더미 객체를 가질 필요가 없습니다.

사이드카를 **언제** 읽을지도 소비자 책임입니다. 일반 파일 핸들은 실제 에셋을 로드할 때 읽을 수 있고, `SpriteRegistry`처럼 사이드카 내용이 어떤 에셋 ID를 등록할지 결정하는 레지스트리는 자신의 `Reload()` 단계에서 미리 읽을 수도 있습니다.

## AssetRef

`AssetRef<TAsset>`는 특정 타입의 리소스를 키 또는 직접 에셋 인스턴스로 참조하는 래퍼입니다.\
`mode`에 따라 `key` 또는 `directAsset`을 사용합니다.

지원 모드는 다음과 같습니다.

- `AssetRefMode.key`: `ResourceKey`로 레지스트리에서 에셋을 찾습니다.
- `AssetRefMode.direct`: 참조에 저장된 `directAsset`을 `InstanceAssetHandle<TAsset>`로 감싸 사용합니다. 레지스트리 등록이 필요하지 않습니다.

```csharp
[SerializeField] AssetRef<MyAsset> assetRef;

AssetRef<MyAsset> byKey = new AssetRef<MyAsset>
(
    new ResourceKey
    (
        new Identifier("my_game", "assets"),
        new Identifier("my_game", "ui/button")
    )
);

AssetRef<MyAsset> direct = new AssetRef<MyAsset>(asset);
```

사용할 때는 모드에 맞는 레지스트리와 핸들을 직접 찾아다니지 않고 `LoadScopeAsync()`를 호출하면 됩니다.\
키 모드는 `ResourceManager`를 통해 핸들을 찾고, 직접 모드는 저장된 인스턴스로 즉시 스코프를 만듭니다.

```csharp
IAssetScope<MyAsset>? scope = await assetRef.LoadScopeAsync();
if (scope == null)
    return;

using (scope)
{
    MyAsset asset = scope.asset;
}
```

핸들이 필요하면 `GetHandle()`을 사용할 수 있습니다. 현재 참조와 사용 중인 스코프가 같은 대상을 가리키는지는 `IsSameTarget()`으로 확인합니다.

키 모드의 수동 흐름은 다음과 같습니다.

```text
ResourceKey
-> AssetRegistryManager.Get
-> registry[assetId]
-> handle.GetScope
```

직접 모드는 다음 흐름을 사용합니다.

```text
directAsset
-> InstanceAssetHandle
-> InstanceAssetScope
```

`AssetRef<TAsset>`는 두 흐름을 하나의 인스펙터 친화적인 API로 감싸 줍니다.

에디터에서 `AssetRefField` 또는 `AssetRefPropertyDrawer`를 사용하면 모드를 필드에서 선택할 수 있습니다.\
직접 모드의 Unity 객체 필드는 `allowSceneObjects` 인자로 씬 객체 허용 여부를 제어하며 기본값은 `false`입니다.\
기본 프로퍼티 드로어는 모든 대상 객체가 영속 에셋이 아닐 때만 씬 객체를 허용합니다. Unity 객체가 아닌 직접 에셋 타입은 현재 저장된 값을 레이블로 표시합니다.

## SimpleAssetRegistry

일반적인 "폴더 안 파일 하나 = 에셋 하나" 패턴에는 `SimpleAssetRegistry<THandle>`를 사용합니다.

`SimpleAssetRegistry`는 활성 리소스 팩마다 다음 폴더를 순회합니다.

```text
assets/{namespace}/{registryName}
```

여기서 `{namespace}`는 레지스트리가 탐색 중인 리소스 팩 안의 네임스페이스이며, 레지스트리 ID의 네임스페이스가 아닙니다.

`registryId.nameSpace`는 레지스트리 ID끼리 충돌하지 않게 하는 이름 영역입니다.\
`registryName`의 기본값은 `registryId.path`이며, 모든 리소스 팩 네임스페이스 아래에서 같은 `registryName` 폴더를 찾습니다.

예를 들어 `registryId`가 `example:textures`라면 기본 `registryName`은 `textures`입니다.

```text
assets/runios/textures
assets/example/textures
assets/any_namespace/textures
```

파일 경로에서 마지막 확장자를 제거한 경로가 에셋 ID가 됩니다.

```text
assets/runios/textures/ui/button.png
-> runios:ui/button

assets/any_namespace/textures/ui/button.png
-> any_namespace:ui/button
```

`SimpleAssetRegistry`는 이 파일 탐색/ID 변환 패턴을 편의 기능으로 제공할 뿐이며, 모든 핸들에 사이드카나 특정 로드 방식을 강제하지 않습니다. 필요한 핸들은 자신의 생성 과정에서 대응하는 `AssetSidecar`를 연결할 수 있습니다.

개발자는 보통 `CreateHandle`만 구현하면 됩니다.

```csharp
#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Resource.Example
{
    public partial sealed class MyAssetRegistry : SimpleAssetRegistry<MyAssetHandle>
    {
        public override Identifier registryId => new Identifier("example", "my_assets");
        public override int priority => 100;
        public override Type assetType => typeof(MyAsset);
        public override WildcardPatterns assetFilter { get; } = "json";

        [OnCodeLoaded]
        static void OnCodeLoaded() => AssetRegistryManager.Register<MyAssetRegistry>();

        [OnCodeUnloading]
        static void OnCodeUnloading() => AssetRegistryManager.Unregister<MyAssetRegistry>();

        protected override UniTask<MyAssetHandle> CreateHandle(IONode node, FileMetaData fileMetaData)
        {
            return UniTask.FromResult(new MyAssetHandle(node, fileMetaData));
        }
    }
}
```

더 세밀한 처리가 필요하면 `OnBeginAssetLoop`, `OnAssetLoop`, `OnEndAssetLoop`를 오버라이드할 수 있습니다.

현재 구현 기준으로, 같은 리로드 패스에서 같은 ID가 이미 기록되었다면 뒤에 나온 항목은 무시됩니다.\
즉 팩 우선순위는 활성 팩 순서와 `RecordAssetHandle`의 중복 처리 규칙을 따릅니다.

## 직접 AssetRegistry 구현

파일을 단순 순회하는 구조가 아니라면 `AssetRegistry<THandle>`를 직접 상속합니다.

직접 레지스트리는 **식별자를 자신이 약속한 에셋 타입으로 해결하는 책임**만 지며, 그 과정에서 어떤 리소스팩 파일을 읽고 어떻게 조합할지는 자유롭게 결정합니다.\
한 물리 파일을 여러 레지스트리가 각각 다른 타입으로 해석해도 되고, 한 파일에서 여러 에셋 ID를 만들어도 되며, 필요하다면 리로드 단계에서 실제 객체까지 만들어 `InstanceAssetHandle<TAsset>`로 등록해도 됩니다.

예를 들어 다음 같은 경우입니다.

```text
여러 json 파일의 딕셔너리를 언어별로 병합하고 LocalizationData를 즉시 생성
assets/{namespace}/sounds.json 하나를 파싱해 여러 사운드 ID 등록
하나의 소스 파일을 서로 다른 레지스트리가 각자 다른 에셋 타입으로 해석
파일 경로가 아니라 내부 데이터 키나 AssetSidecar 내용을 에셋 ID 등록에 사용
```

실제 예시는 `LanguageAssetRegistry`, `SoundAssetRegistry`입니다. `LanguageAssetRegistry`는 리로드 단계에서 언어 JSON을 병합하고 `LocalizationData`를 만든 뒤 `InstanceAssetHandle<LocalizationData>`로 바로 등록합니다.

직접 구현할 때는 `AsyncReloadGate`로 중복 리로드를 조정하고, 진행도 보고와 트래킹 시작 및 종료는 리로드 본문에서 직접 처리합니다.

```csharp
readonly AsyncReloadGate reloadGate = new();

public override bool isLoading => reloadGate.isRunning;

public override UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null)
{
    ResourcePack[] resourcePackSnapshot = resourcePacks.ToArray();

    return reloadGate.Run
    (
        reloadProgress => ReloadCore(resourcePackSnapshot, reloadProgress),
        progress
    );
}

async UniTask ReloadCore(ResourcePack[] resourcePacks, IProgress<float>? progress)
{
    BeginTracking();

    try
    {
        progress.SafeReport(0);

        foreach (ResourcePack resourcePack in resourcePacks)
        {
            // Read files, parse data, and call RecordAssetHandle.
            // RecordAssetHandle(assetId, handle);
        }
    }
    catch (Exception e)
    {
        Debug.RuntimeLogError($"An unexpected exception occurred while reloading resources. The exception is: {e}");
    }
    finally
    {
        progress.SafeReport(1);

        EndTracking();
    }
}
```

`AsyncReloadGate`는 실행 중인 리로드 요청을 같은 배치로 합치고, 대기 중인 최신 요청을 다음 패스로 실행합니다. 따라서 `WaitWhile`, 별도 `isLoading` 플래그, 중복 실행 분기를 직접 작성할 필요가 없습니다.\
진행도 계산, 병렬 작업, 병합 규칙, 어떤 시점에 어떤 핸들을 등록할지 모두 레지스트리 구현이 직접 결정합니다.

## 직접 레지스트리가 필요한 경우

`SimpleAssetRegistry`로 충분한 경우:

```text
폴더 안 파일 하나 = 에셋 하나
파일 경로 = 에셋 ID
확장자 필터로 대상 파일을 고를 수 있음
표준 파일 탐색 규칙만 필요함
```

직접 `AssetRegistry`가 좋은 경우:

```text
여러 파일을 합쳐 하나의 에셋으로 만들어야 함
한 파일에서 여러 에셋 ID가 나와야 함
AssetSidecar나 파일 내부 데이터를 등록 단계에서 해석해야 함
같은 소스를 독자적인 규칙으로 재해석해야 함
리소스 팩별 병합 규칙이 필요함
리로드 단계에서 실제 에셋 객체까지 만들고 싶음
진행도와 병렬 처리 방식을 직접 제어해야 함
```

## 요약

리소스 시스템은 리소스팩의 물리 파일 구조와 게임 내부의 논리 에셋 접근을 분리합니다.\
레지스트리는 `Identifier`를 자신이 약속한 에셋 타입으로 해결할 책임을 가지며, 파일 탐색·병합·사이드카 해석·eager/lazy 로드 방식은 구현이 선택합니다.

일반적인 파일 하나당 에셋 하나 구조는 `SimpleAssetRegistry`를 사용하고, 복잡한 병합·다중 에셋 생성·등록 단계 데이터 해석은 `AssetRegistry`를 직접 구현합니다.\
`AssetSidecar`는 특정 핸들의 전용 임포트 설정이 아니라 여러 소비자가 독립적으로 사용할 수 있는 확장 가능한 I/O 사이드카입니다.

리로드는 각 레지스트리의 전체 등록 결과를 다시 계산하지만, `IsSameTarget()`이 같은 대상으로 판단한 기존 핸들은 재사용합니다.\
따라서 레지스트리는 자유로운 구현 방식을 유지하면서도 변경되지 않은 에셋의 핸들과 생명주기를 보존할 수 있습니다.
