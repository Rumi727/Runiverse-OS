# 리소스 시스템 개요

Language available: \[[**한국어 (대한민국)**](README.md)\] \[[English (US)](README-EN.md)\]  

## 개요

이 프로젝트의 리소스 시스템은 Minecraft의 리소스 팩 구조에서 아이디어를 가져왔습니다.\
파일은 리소스 팩에 들어 있고, 에셋 레지스트리는 그 파일들을 게임에서 사용할 수 있는 `Identifier`와 에셋 핸들로 등록합니다.

```text
Resource Pack files
-> AssetRegistry
-> AssetHandle
-> AssetScope
-> loaded asset object
```

중요한 점은 레지스트리가 실제 에셋 객체를 항상 즉시 로드하지 않는다는 것입니다.\
리로드 시점에는 주로 "어떤 에셋이 어디에 있고, 어떤 핸들로 접근해야 하는가"를 다시 계산합니다.\
실제 에셋 객체는 보통 `AssetHandle<T>.GetScope()` 또는 키 모드의 `AssetRef<T>.LoadScopeAsync()`가 호출될 때 로드됩니다.

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

레지스트리는 보통 `[Awaken]` 메소드에서 등록됩니다.\
에디터에서도 보이게 하려면 기존 구현처럼 `[UnityEditor.InitializeOnLoadMethod]`를 같이 사용할 수 있습니다.

레지스트리는 다음 기준으로 조회됩니다.

```text
registryId       -> AssetRegistryManager.Get(registryId)
registry type    -> AssetRegistryManager.Get<TRegistry>()
asset type       -> AssetRegistryManager.GetAllForAsset(assetType)
default registry -> AssetRegistryManager.GetDefaultForAsset<TAsset>()
```

`isDefault`가 `true`인 레지스트리는 같은 에셋 타입의 기본 레지스트리가 됩니다.\
키 모드의 `AssetRef<T>` 인스펙터 필드는 이 정보를 사용해 호환되는 레지스트리와 에셋을 고를 수 있습니다. 직접 모드에서는 레지스트리 조회 없이 참조에 저장된 에셋 인스턴스를 사용합니다.

## 빠른 리로드 구조

레지스트리는 리로드 때 전체 인덱스를 다시 만듭니다.\
하지만 이 작업은 대부분 파일 조회와 핸들 기록입니다. 실제 에셋 객체 전체를 다시 로드하는 구조가 아닙니다.

`AssetRegistry<THandle>`는 리로드 중 임시 추적 테이블을 만듭니다.

```text
BeginTracking
-> RecordAssetHandle
-> EndTracking
```

`RecordAssetHandle`은 같은 ID의 기존 핸들이 있고, 새 핸들과 같은 대상을 가리킨다면 기존 핸들을 재사용합니다.

```text
same identifier + same target -> keep old handle
same identifier + changed target -> replace with new handle
missing from reload pass -> remove from registry
```

그래서 레지스트리 자체는 전체 리로드처럼 보이지만, 에셋 핸들은 변경된 것만 교체됩니다.\
파일이 바뀌지 않은 에셋은 기존 핸들이 유지되고, 이미 로드된 에셋 객체도 그대로 이어질 수 있습니다.

파일이 바뀐 에셋은 레지스트리에서 새 핸들로 교체됩니다.\
기존 핸들을 들고 있던 렌더러나 시스템은 리로드 완료 이벤트에서 다시 레지스트리를 조회해 새 핸들을 가져오면 됩니다.

이 구조 덕분에 리로드는 Minecraft식 전체 재적재보다 훨씬 가볍습니다.\
레지스트리 갱신은 빠른 파일 인덱싱에 가깝고, 실제 에셋 로드는 필요한 시점에 핸들과 스코프가 처리합니다.

## AssetHandle과 AssetScope

`AssetHandle<TAsset>`는 단일 에셋의 로드와 언로드를 담당합니다.\
실제 에셋은 `GetScope()`가 호출될 때 필요하면 로드됩니다.

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

스코프가 모두 반환되면 핸들은 `unloadDelayFrame` 뒤에 언로드를 시도합니다.\
따라서 짧은 시간 안에 같은 에셋이 다시 요청되는 경우 불필요한 언로드와 재로드를 줄일 수 있습니다.

`AssetHandle<TAsset>.IsSameTarget()`은 리로드에서 핸들을 재사용해도 되는지 판단합니다.\
기본 구현은 핸들 타입, I/O 대상, 파일 메타데이터, 임포트 데이터 파일의 대상과 메타데이터가 같은지 확인합니다.

## 에셋 임포트 데이터

`AssetImportData`는 `FileMetaData`와 별개인 확장 가능한 임포트 데이터 컨테이너입니다.\
`FileMetaData`가 파일 크기나 수정 시간 같은 파일 시스템 정보를 나타낸다면, `AssetImportData`는 개발자가 에셋별 추가 정보를 저장하는 sidecar JSON 파일을 나타냅니다.

`SimpleAssetRegistry<THandle>`는 에셋 파일과 같은 경로에서 마지막 확장자를 `.json`으로 바꾼 파일을 임포트 데이터 파일로 연결합니다.

```text
assets/runios/sounds/ui/click.ogg
assets/runios/sounds/ui/click.json
```

임포트 데이터 JSON의 최상위 키는 `Identifier`이고, 각 값은 `JObject`입니다.\
키는 데이터를 해석하는 레지스트리나 기능의 식별자로 사용합니다. 파일 에셋의 식별자와 반드시 같을 필요는 없습니다.

```json
{
  "runios:waves": {
    "loadMode": "stream"
  },
  "my_game:music": {
    "bpm": 128,
    "artist": "Example Artist"
  }
}
```

패키지나 특정 에셋 구현이 알지 못하는 식별자와 필드도 `JObject` 형태로 함께 보존됩니다.\
따라서 새로운 에셋별 데이터를 추가하기 위해 기존 패키지 코드를 수정할 필요가 없습니다.\
단, 저장된 값을 실제 동작에 사용하려면 해당 식별자의 데이터를 읽는 소비자 코드가 필요합니다.

핸들은 실제 에셋을 로드하기 직전에 임포트 데이터 JSON을 읽습니다.\
레지스트리 리로드 시에는 sidecar 파일의 존재 여부와 파일 메타데이터만 확인하고, 실제 JSON 역직렬화는 `AssetHandle<TAsset>.GetScope()`의 로드 경로에서 수행합니다.\
프레임마다 읽는 구조는 아니며, 에셋이 언로드된 뒤 다시 로드되면 다시 읽습니다.

```csharp
using Newtonsoft.Json.Linq;

Identifier key = new Identifier("my_game", "music");
JObject? rawData = handle.importData[key];
MusicImportData? typedData = handle.importData.GetValue<MusicImportData>(key);

if (handle.importData.TryGetValue<MusicImportData>(key, out MusicImportData? data))
{
    // MusicImportData는 애플리케이션이 정의한 타입입니다.
    // data 사용
}
```

키가 없으면 `GetValue<T>()`는 해당 타입의 기본값을 반환합니다.\
필수 데이터 여부를 구분해야 하면 `TryGetValue<T>()`를 사용하세요.\
JSON을 읽지 못하면 임포트 데이터는 비워지고 오류가 기록됩니다. 원본 sidecar 파일 자체가 삭제되거나 덮어써지는 것은 아닙니다.

`AssetRegistry<THandle>`를 직접 구현하는 경우에는 sidecar 파일을 레지스트리에서 직접 찾아 `AssetImportData`를 생성한 뒤 핸들에 전달해야 합니다.\
`InstanceAssetHandle<TAsset>`처럼 별도 파일이 없는 인스턴스 핸들은 공유된 빈 `AssetImportData`를 사용할 수 있습니다.

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

일반적인 "폴더 안 파일을 전부 에셋으로 등록"하는 경우에는 `SimpleAssetRegistry<THandle>`를 쓰는 편이 좋습니다.

`SimpleAssetRegistry`는 활성 리소스 팩마다 다음 폴더를 순회합니다.

```text
assets/{namespace}/{registryName}
```

여기서 `{namespace}`는 레지스트리가 탐색 중인 리소스 팩 안의 네임스페이스입니다.\
레지스트리 ID의 네임스페이스가 아닙니다.

`registryId.nameSpace`는 레지스트리끼리 ID가 충돌하지 않게 하는 이름 영역입니다.\
`SimpleAssetRegistry`의 폴더 탐색 범위를 제한하지 않습니다.

`registryName`의 기본값은 `registryId.path`입니다.\
즉 `SimpleAssetRegistry`는 모든 리소스 팩 네임스페이스 아래에서 `registryName` 폴더를 찾습니다.

예를 들어 `registryId`가 `example:textures`라면 기본 `registryName`은 `textures`입니다.\
따라서 리소스 팩에 존재하는 모든 네임스페이스에서 다음 위치를 찾습니다.

```text
assets/runios/textures
assets/example/textures
assets/any_namespace/textures
```

파일 경로는 확장자를 제외한 에셋 ID가 됩니다.

```text
assets/runios/textures/ui/button.png
-> runios:ui/button

assets/any_namespace/textures/ui/button.png
-> any_namespace:ui/button
```

개발자는 대부분 `CreateHandle`만 구현하면 됩니다.

```csharp
#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Booting;
using RuniOS.IO;
using UnityEngine.Scripting;

namespace RuniOS.Resource.Example
{
    public sealed class MyAssetRegistry : SimpleAssetRegistry<MyAssetHandle>
    {
        public override Identifier registryId => new Identifier("example", "my_assets");
        public override bool isDefault => true;
        public override Type assetType => typeof(MyAsset);
        public override WildcardPatterns assetFilter { get; } = "json";

        [Awaken]
        [Preserve]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Awaken() => AssetRegistryManager.Register<MyAssetRegistry>();

        protected override UniTask<MyAssetHandle> CreateHandle(IONode node, FileMetaData fileMetaData, AssetImportData importData)
        {
            return UniTask.FromResult(new MyAssetHandle(node, fileMetaData, importData));
        }
    }
}
```

더 세밀한 처리가 필요하면 `OnBeginAssetLoop`, `OnAssetLoop`, `OnEndAssetLoop`를 오버라이드할 수 있습니다.

현재 구현 기준으로, 같은 리로드 패스에서 같은 ID가 이미 기록되었다면 뒤에 나온 항목은 무시됩니다.\
즉 팩 우선순위는 활성 팩 순서와 `RecordAssetHandle`의 중복 처리 규칙을 따릅니다.

## 직접 AssetRegistry 구현

파일을 단순 순회하는 구조가 아니라면 `AssetRegistry<THandle>`를 직접 상속합니다.

예를 들어 다음 같은 경우입니다.

```text
여러 json 파일의 딕셔너리를 언어별로 병합
assets/{namespace}/sounds.json 하나를 파싱해 여러 사운드 ID 등록
파일 경로가 아니라 내부 데이터 키를 에셋 ID로 사용
```

실제 예시는 `LanguageAssetRegistry`, `SoundAssetRegistry`입니다.

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
CreateHandle만 다르면 됨
```

직접 `AssetRegistry`가 좋은 경우:

```text
여러 파일을 합쳐 하나의 에셋으로 만들어야 함
한 파일에서 여러 에셋 ID가 나와야 함
리소스 팩별 병합 규칙이 필요함
폴더 순회가 아니라 고정 json 파일을 읽어야 함
진행도와 병렬 처리 방식을 직접 제어해야 함
```

## 요약

리소스 시스템은 리소스 팩의 파일 구조와 게임 내부 에셋 접근을 분리합니다.\
레지스트리는 파일을 빠르게 인덱싱하고, 핸들은 실제 에셋 로드와 생명주기를 담당합니다.

일반 파일 에셋은 `SimpleAssetRegistry`를 쓰면 됩니다.\
복잡한 병합이나 커스텀 포맷은 `AssetRegistry`를 직접 구현하면 됩니다.

리로드는 레지스트리 전체를 다시 계산하지만, 에셋 객체 전체를 무조건 버리고 다시 로드하지 않습니다.\
변경된 핸들만 교체하고, 사용 중인 시스템은 리로드 완료 이벤트에서 최신 핸들을 다시 가져오는 방식으로 동작합니다.
