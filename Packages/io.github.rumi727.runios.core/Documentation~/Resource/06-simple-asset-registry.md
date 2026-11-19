# 6. `SimpleAssetRegistry`: 파일 하나 = 에셋 하나

> 기준 커밋: `7e5d710f`. [← 목차](README.md) · 이전: [5. 레지스트리와 조회](05-registries.md) · 다음: [7. 핸들과 스코프](07-handles-and-scopes.md)
>
> 선행: [IO 3. 열거와 매처](../IO/03-enumeration-and-matchers.md)

대부분의 레지스트리는 "레지스트리 폴더 아래의 파일 하나가 에셋 하나"입니다. [`SimpleAssetRegistry<THandle>`](../../Runtime/Resource/SimpleAssetRegistry.cs#L84)는 그 경우의 리로드를 모두 구현해 두고, 파생 클래스는 **어떤 파일을 고를지**(매처)와 **파일로 어떤 핸들을 만들지**(`CreateHandle`)만 정합니다. 이 장은 기반 클래스의 두 단계 리로드와, 실제 구현 예로 texture 패키지의 `TextureAssetRegistry`를 줄 단위로 봅니다.

- [6.1 파생 클래스가 정할 것](#61-파생-클래스가-정할-것)
- [6.2 리로드 1단계: 인덱싱](#62-리로드-1단계-인덱싱)
- [6.3 리로드 2단계: 핸들 만들기](#63-리로드-2단계-핸들-만들기)
- [6.4 예제: `TextureAssetRegistry`](#64-예제-textureassetregistry)

## 6.1 파생 클래스가 정할 것

| 멤버 | 필수 | 뜻 |
| --- | --- | --- |
| `registryId` | O | 레지스트리 식별자 ([5.1절](05-registries.md#51-계약-iassetregistry)) |
| `providedAssetType` | O | 핸들이 주는 에셋 타입 |
| [`assetMatcher`](../../Runtime/Resource/SimpleAssetRegistry.cs#L28) | O | 에셋 파일을 고르는 [매처](../IO/03-enumeration-and-matchers.md#32-매처) |
| [`auxiliaryMatcher`](../../Runtime/Resource/SimpleAssetRegistry.cs#L30) | | 보조 파일(사이드카 `.json`) 매처. 기본 `null` |
| [`registryName`](../../Runtime/Resource/SimpleAssetRegistry.cs#L18) | | 레지스트리 폴더 이름. 기본은 `registryId.path` |
| [`CreateHandle`](../../Runtime/Resource/SimpleAssetRegistry.cs#L60) | O | 파일 노드 → 핸들 |
| `OnBeginAssetLoop` / `OnAssetLoop` / `OnEndAssetLoop` | | 2단계 앞·각 핸들·뒤 훅. 기본 `OnAssetLoop`는 `RecordAssetHandle` |

`registryName`이 기본값이면 `runios:textures` 레지스트리는 각 네임스페이스의 `textures/` 폴더를 봅니다. 레지스트리 폴더는 네임스페이스마다 [`GetRegistryNodes`](../../Runtime/Resource/SimpleAssetRegistry.cs#L40)가 찾고, 폴더가 없는 네임스페이스는 건너뜁니다.

## 6.2 리로드 1단계: 인덱싱

리로드는 [`AsyncReloadGate`](03-reload-gate.md)를 거쳐 `ReloadCore`에서 [추적](05-registries.md#52-추적-먼저-기록한-것이-이긴다)을 시작한 뒤 두 단계로 진행합니다. 1단계는 모든 팩을 훑어 **무엇을 만들지 목록만** 만듭니다.

```csharp
// Runtime/Resource/SimpleAssetRegistry.cs
84  public sealed override UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null) => reloadGate.Run(progress => ReloadCore(resourcePacks, progress), progress);
86  async UniTask ReloadCore(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress)
    {
88      BeginTracking();
        …
96      IPatternMatcher patterns = auxiliaryMatcher != null ? PatternMatcherSet.Create(assetMatcher, auxiliaryMatcher) : assetMatcher;   // ① 한 번 열거로 둘 다
99      List<AssetLoadTarget> loadTargetDict = [];
100     foreach (var resourcePack in resourcePacks)                                          // ② 우선순위 순서
        {
102         await foreach ((string nameSpace, IONode registryNode) in GetRegistryNodes(resourcePack))
            {
104             AssetDiscoveryContext context = new AssetDiscoveryContext(resourcePack, nameSpace, registryNode);
105             await foreach (IOEntry fileEntry in registryNode.dir.GetAllFiles(patterns))
                {
107                 try
                    {
109                     bool isAuxiliary = auxiliaryMatcher?.IsMatch(fileEntry.path) ?? false;
110                     if (isAuxiliary)
                        {
112                         context.AddAuxiliary(fileEntry);                                 // ③ 보조 파일은 따로 모아 둠
113                         continue;
                        }
116                     RuniPath path = context.GetRegistryRelativePath(fileEntry.path);
117                     Identifier identifier = new Identifier(nameSpace, path.GetPathWithoutExtension());   // ④ 경로 → 식별자
118                     if (IsTracked(identifier))
119                         continue;
121                     IONode node = registryNode.Bind(fileEntry);
122                     loadTargetDict.Add(new AssetLoadTarget(context, identifier, node, fileEntry.metaData));
                    }
124                 catch (Exception e)
                    {
126                     Debug.RuntimeLogError(…);                                          // ⑤ 파일 하나의 실패는 그 파일만
                    }
                }
            }
        }
```

- ① 에셋 파일과 사이드카를 따로 열거하지 않고, 두 매처를 OR로 합쳐 한 번만 훑습니다.
- ③ 보조 파일은 팩·네임스페이스·레지스트리 폴더 단위의 [`AssetDiscoveryContext`](../../Runtime/Resource/SimpleAssetRegistry.cs#L185)에 경로별로 모아 둡니다. 2단계에서 핸들을 만들 때 "이 에셋의 사이드카가 같은 팩에 있었나"를 열거를 다시 하지 않고 확인하는 데 씁니다([9장](09-sidecars.md)).
- ④ 식별자 규칙은 [1.3절](01-identifiers.md#13-파일-경로에서-식별자로)입니다. 대문자 파일 이름처럼 식별자로 쓸 수 없으면 ⑤에서 로그를 남기고 그 파일만 빠집니다.
- 1단계와 2단계를 나눈 덕분에, 2단계는 팩을 다시 열거하지 않고 모아 둔 목록만 돕니다.

> **관찰된 동작:** 118행의 `IsTracked`는 이 단계에서 항상 `false`입니다. 기록(`RecordAssetHandle`)은 2단계에서만 일어나기 때문입니다. 같은 식별자가 여러 팩에 있으면 목록에 모두 들어가고, 2단계의 `IsTracked`(136행)가 뒤의 것을 건너뜁니다. 목록 변수 이름은 `loadTargetDict`지만 `List`입니다.

## 6.3 리로드 2단계: 핸들 만들기

```csharp
// Runtime/Resource/SimpleAssetRegistry.cs
133 for (int i = 0; i < loadTargetDict.Count; i++)
    {
135     AssetLoadTarget target = loadTargetDict[i];
136     if (IsTracked(target.identifier))
137         continue;                                                         // ① 앞 팩에서 이미 기록됨
139     try
        {
141         THandle handle = await CreateHandle(target.context, target.identifier, target.node, target.fileMetaData);
142         await OnAssetLoop(target.context, target.identifier, target.node, handle);   // ② 기본 구현 = RecordAssetHandle
        }
144     catch (Exception e)
        {
146         Debug.RuntimeLogError(…);
        }
150     progress.SafeReport((float)i / loadTargetDict.Count);
    }
153 await OnEndAssetLoop();
…
159 finally
    {
161     progress.SafeReport(1);
163     EndTracking();                                                        // ③ 실패해도 새 인덱스로 교체
    }
```

① 목록은 팩 우선순위 순서로 쌓였으므로, 같은 식별자는 우선순위가 높은 팩의 것이 먼저 기록되고 나머지는 여기서 걸러집니다. ② 기록할 때 옛 인덱스의 핸들과 [같은 대상](05-registries.md#52-추적-먼저-기록한-것이-이긴다)이면 옛 핸들이 재사용됩니다.

`CreateHandle`은 보통 파일을 읽지 않고 핸들만 만듭니다. 에셋은 누군가 스코프를 요청할 때 불러옵니다([7장](07-handles-and-scopes.md)). 그래서 리로드는 파일 개수에 비례하는 열거 비용만 들고, 쓰이지 않는 에셋은 메모리에 올라가지 않습니다.

## 6.4 예제: `TextureAssetRegistry`

texture 패키지(`io.github.rumi727.runios.texture`)의 텍스처 레지스트리는 `SimpleAssetRegistry`를 쓰는 가장 기본적인 예입니다.

```csharp
// io.github.rumi727.runios.texture/Runtime/Resource/Textures/TextureAssetRegistry.cs
8   public sealed partial class TextureAssetRegistry : SimpleAssetRegistry<TextureAssetHandle>   // ① partial: 수명 주기 특성 때문
    {
11      public static readonly Identifier id = new Identifier("runios", "textures");
14      public override Identifier registryId => id;                                  // ② 폴더 = assets/{ns}/textures/
17      public override Type providedAssetType => typeof(Texture2D);                  // ③ 자동 조회에서 Texture2D 요청에 응답
20      public override IPatternMatcher assetMatcher => IPatternMatcher.pictureMatcher;   // ④ .png, .jpg, … 
21      public override IPatternMatcher auxiliaryMatcher => IPatternMatcher.jsonMatcher;  // ⑤ a.png.json 사이드카
23      [OnCodeLoaded]
24      static void OnCodeLoaded() => AssetRegistryManager.Register<TextureAssetRegistry>();
26      [OnCodeUnloading]
27      static void OnCodeUnloading() => AssetRegistryManager.Unregister<TextureAssetRegistry>();
30      protected override UniTask<TextureAssetHandle> CreateHandle(AssetDiscoveryContext context, Identifier identifier, IONode node, FileMetaData fileMetaData)
            => UniTask.FromResult(new TextureAssetHandle(node, fileMetaData, CreateSidecar(context, node)));   // ⑥ 파일은 읽지 않음
    }
```

- ① 프로젝트 규칙상 `[OnCodeLoaded]` 같은 수명 주기 특성을 쓰는 클래스는 `partial`이어야 합니다.
- ④ [`pictureMatcher`](../IO/03-enumeration-and-matchers.md#32-매처)는 확장자 묶음이라 대소문자를 구분합니다. `A.PNG`는 고르지 않습니다.
- ⑤ 사이드카는 에셋 파일 이름 뒤에 `.json`을 붙인 파일입니다(`close.png.json`). 그런데 `.json`은 `pictureMatcher`에 없으므로 에셋으로 잡히지 않고 보조 파일로만 모입니다.
- ⑥ [`CreateSidecar`](09-sidecars.md#92-사이드카-만들기)가 1단계에서 모아 둔 보조 파일로 사이드카 객체를 만듭니다. 이것도 파일을 읽지 않습니다.

핸들은 로드할 때 비로소 사이드카와 이미지를 읽습니다.

```csharp
// io.github.rumi727.runios.texture/Runtime/Resource/Textures/TextureAssetHandle.cs
8   public sealed class TextureAssetHandle(IONode node, FileMetaData fileMetaData, AssetSidecar sidecar) : AssetHandle<Texture2D>(node, fileMetaData), IAssetSidecarHandle
    {
10      public AssetSidecar sidecar { get; } = sidecar;                              // ① IAssetSidecarHandle: 사이드카도 리비전 비교에 참여
12      protected override async UniTask<Texture2D?> Load()
        {
14          await sidecar.Reload();                                                  // ② 사이드카를 이제 읽음
16          TextureLoadSettings settings = sidecar.GetValue<TextureLoadSettings>(TextureAssetRegistry.id) ?? TextureLoadSettings.defaultValue;   // ③ "runios:textures" 구역
17          Texture2D texture = await TextureLoader.LoadAsync(node, settings);      // ④ node.file.ReadAllBytes → 디코딩
19          texture.name = node.path.GetFileNameWithoutExtension();
20          texture.hideFlags = HideFlags.HideAndDontSave;                          // ⑤ 씬과 무관하게 핸들이 수명을 관리
22          return texture;
        }
25      protected override void Unload(Texture2D unloadedAsset) => Object.DestroyImmediate(unloadedAsset);   // ⑥ 마지막 스코프 반환 후
    }
```

- ① [`IAssetSidecarHandle`](../../Runtime/Resource/IAssetSidecarHandle.cs#L11)을 구현하면, 리로드 때 이미지 파일이 그대로여도 사이드카(로드 설정)가 바뀌면 다른 대상으로 판정되어 새 핸들이 만들어집니다([7.5절](07-handles-and-scopes.md#75-issametarget-언제-옛-핸들을-재사용하나)).
- ③ 사이드카는 `Identifier`를 키로 한 JSON이고, 텍스처 핸들은 자기 레지스트리 식별자(`runios:textures`) 키의 구역에서 설정을 꺼냅니다([9장](09-sidecars.md)).
- ⑤⑥ 텍스처는 핸들이 만들고 핸들이 파괴합니다. `HideAndDontSave`라서 씬에 저장되지 않고 `Resources.UnloadUnusedAssets`로도 정리되지 않습니다. 언로드는 [`AssetHandle`](07-handles-and-scopes.md)이 마지막 스코프가 반환된 뒤 `DisposeQueue`를 거쳐 부릅니다.

다른 패키지의 레지스트리들은 [11장](11-package-registries.md)에 표로 정리했습니다.
