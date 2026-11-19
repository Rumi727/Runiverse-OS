# 2. 리소스 팩

> 기준 커밋: `7e5d710f`. [← 목차](README.md) · 이전: [1. 식별자](01-identifiers.md) · 다음: [3. `AsyncReloadGate`](03-reload-gate.md)
>
> 선행: [IO 2. 공급자와 노드](../IO/02-providers-and-nodes.md), [IO 7. StreamingAssets](../IO/07-streaming-assets.md)

**리소스 팩**은 에셋 파일을 담은 폴더 하나입니다. 게임 기본 에셋(`runios:vanilla`)도, 에디터 전용 에셋(`runios:editor`)도, 사용자가 추가하는 팩도 같은 구조를 가집니다. 팩은 [`IIOProvider`](../IO/02-providers-and-nodes.md#22-iioprovider-읽기-계약) 하나를 받아 만들므로, 디스크 폴더든 APK 안이든 여러 폴더를 겹친 것이든 같은 코드로 읽힙니다.

- [2.1 팩의 폴더 구조](#21-팩의-폴더-구조)
- [2.2 팩 만들기와 등록](#22-팩-만들기와-등록)
- [2.3 팩 리로드: `pack.json`과 네임스페이스](#23-팩-리로드-packjson과-네임스페이스)
- [2.4 활성화 순서와 우선순위](#24-활성화-순서와-우선순위)
- [2.5 기본 팩과 에디터 팩](#25-기본-팩과-에디터-팩)
- [2.6 `Dispose`와 공급자 소유권](#26-dispose와-공급자-소유권)

## 2.1 팩의 폴더 구조

```text
<팩 루트>/
  pack.json                       PackMetaData (없으면 팩이 아님)
  assets/
    runios/                       네임스페이스
      textures/ui/close.png       → runios:textures 레지스트리의 runios:ui/close
      lang/ko_kr.json             → runios:lang 레지스트리의 runios:ko_kr
    mymod/                        다른 네임스페이스
      textures/…
```

`assets/` 바로 아래 폴더가 **네임스페이스**이고, 그 아래 첫 폴더가 보통 **레지스트리 폴더**입니다. 레지스트리 폴더 이름은 각 레지스트리가 정합니다([6장](06-simple-asset-registry.md)). 이 구조 덕분에 한 팩이 여러 네임스페이스에 에셋을 넣을 수 있고, 다른 팩이 같은 네임스페이스·같은 경로에 파일을 두어 덮어쓸 수 있습니다.

[`PackMetaData`](../../Runtime/Resource/PackMetaData.cs#L4)는 이름, 버전, 대상 버전 범위 둘(`targetVersion`, `targetRuniOSVersion`)을 가집니다.

> **관찰된 동작:** `targetVersion`과 `targetRuniOSVersion`을 검사하는 코드는 없습니다.

## 2.2 팩 만들기와 등록

[`ResourcePack`](../../Runtime/Resource/ResourcePack.cs#L15)의 생성자는 private이고, 정적 `Create`로 만듭니다.

```csharp
// Runtime/Resource/ResourcePack.Static.cs
62  public static ResourcePack Create(PhysicalPath path) => Create(PackIdentifier.CreateByPath(path), new PhysicalIOProvider(path));   // ① 디스크 폴더 팩
74  public static ResourcePack Create(Identifier identifier, IIOProvider provider, RequiredPackSort requiredSort = RequiredPackSort.NotRequired, bool leaveOpen = false) => …;
86  public static ResourcePack Create(PackIdentifier packIdentifier, IIOProvider provider, RequiredPackSort requiredSort = RequiredPackSort.NotRequired, bool leaveOpen = false)
    {
88      ResourcePack resourcePack = new ResourcePack(packIdentifier, provider, requiredSort, leaveOpen);
89      _loadedResourcePacks.Add(packIdentifier, resourcePack);          // ② 같은 식별자면 ArgumentException
91      return resourcePack;
    }
99  public static ResourcePack? GetLoadedPack(PackIdentifier packIdentifier) => _loadedResourcePacks.GetValueOrDefault(packIdentifier);
```

- ① 디스크 폴더 팩은 기본 정책(`BlockLinks`)의 [`PhysicalIOProvider`](../IO/04-physical-and-sandbox.md)로 열립니다. 남이 만든 팩 폴더에서 링크로 루트 밖에 닿지 못합니다.
- ② 만든 팩은 정적 사전 `loadedResourcePacks`에 등록됩니다. 같은 식별자로 두 번 만들면 예외이고, 이미 있는 팩은 `GetLoadedPack`으로 꺼냅니다.
- 생성자([`#L38`](../../Runtime/Resource/ResourcePack.cs#L38))는 `rootFolder`(공급자 루트), `assetFolder`(`assets`), `infoFile`(`pack.json`) 노드만 잡고 아무것도 읽지 않습니다. 읽기는 리로드 때 합니다.

**등록(loaded)과 활성화(enabled)는 다릅니다.** 등록된 팩은 리로드되지만, 레지스트리가 에셋을 찾는 대상은 활성화된 팩뿐입니다([2.4절](#24-활성화-순서와-우선순위)).

## 2.3 팩 리로드: `pack.json`과 네임스페이스

```csharp
// Runtime/Resource/ResourcePack.cs
90  public UniTask Reload() => reloadGate.Run(ReloadCore);              // ① 3장의 게이트로 겹친 요청을 합침
92  async UniTask ReloadCore()
    {
94      if (!await infoFile.file.Exists())
        {
96          metaData = new PackMetaData();
97          namespaces = ImmutableArray<string>.Empty;                  // ② pack.json이 없으면 네임스페이스 없음
99          return;
        }
102     try
        {
104         metaData = JsonConvert.DeserializeObject<PackMetaData>(await infoFile.file.ReadAllText());
105         isValid = true;
        }
107     catch (Exception e)
        {
109         Debug.LogException(e);
110         isValid = false;                                            // ③ 깨진 pack.json = 무효 팩
        }
113     if (!isValid)
114         return;
115     else if (await assetFolder.dir.Exists())
        {
117         namespaces = [ .. await assetFolder.dir.GetDirectories()
121             .Select(x => x.path.GetFileName())
122             .Where(x => { … Identifier.IsNamespaceValid(x) … })     // ④ 네임스페이스로 쓸 수 없는 폴더 이름은 경고 후 제외
132             .ToArrayAsync() ];
        }
    }
```

④ 네임스페이스 폴더 이름도 [식별자 규칙](01-identifiers.md#11-identifier)을 따라야 합니다. `MyMod`처럼 대문자가 있으면 그 폴더 전체가 빠집니다.

> **관찰된 동작:** ② `pack.json`이 없을 때는 `isValid`를 바꾸지 않습니다. 한 번 유효했던 팩에서 `pack.json`을 지우고 리로드하면 `isValid == true`인 채 네임스페이스만 비게 됩니다. ③ 파싱에 실패하면 `namespaces`는 이전 값을 유지합니다.

## 2.4 활성화 순서와 우선순위

활성화된 팩 목록 [`enabledPackIdentifiers`](../../Runtime/Resource/ResourcePack.Static.cs#L56)는 순서가 곧 우선순위이고, **0번이 가장 높습니다**. 레지스트리는 이 순서로 팩을 훑어 같은 식별자가 여러 팩에 있으면 앞의 것을 씁니다([5.2절](05-registries.md#52-추적-먼저-기록한-것이-이긴다)).

```csharp
// Runtime/Resource/ResourcePack.Static.cs
134 public static void EnablePack(PackIdentifier identifier) => EnablePack(identifier, 0);   // ① 기본은 맨 앞
136 public static void EnablePack(PackIdentifier identifier, int index)
    {
138     if (!_enabledPackIdentifiers.Contains(identifier))
139         _enabledPackIdentifiers.Insert(index, identifier);         // ② 이미 있으면 아무것도 안 함
    }
…
144 public static IEnumerable<ResourcePack> GetEnabledPacks() => _enabledPackIdentifiers
145     .Where(x => loadedResourcePacks.ContainsKey(x))
146     .Select(x => loadedResourcePacks[x])
147     .Where(x => x.isValid);                                         // ③ 등록되어 있고 유효한 팩만
```

`ReloadAll`은 리로드 전에 필수 팩을 활성화합니다. [`RequiredPackSort`](../../Runtime/Resource/RequiredPackSort.cs#L3)가 그 규칙입니다.

```csharp
// Runtime/Resource/ResourcePack.Static.cs
104 static UniTask ReloadAllCore()
    {
106     if (defaultPack is { } vanillaPack)
107         EnablePack(vanillaPack.identifier, _enabledPackIdentifiers.Count);   // ① vanilla는 맨 뒤(가장 낮음)
109     return UniTask.WhenAll(loadedResourcePacks.Values.Select(pack =>
        {
112         switch (pack.requiredSort)
            {
114             // 0번 인덱스가 제일 우선순위가 높습니다.
115             case RequiredPackSort.BeforeVanilla:
                {
117                 int vanillaIndex = _enabledPackIdentifiers.LastIndexOf(defaultPack?.identifier ?? PackIdentifier.empty);
118                 if (vanillaIndex < 0)
119                     EnablePack(pack.identifier, 0);
120                 else
121                     EnablePack(pack.identifier, vanillaIndex + 1);
123                 break;
                }
125             case RequiredPackSort.AfterVanilla:
126                 EnablePack(pack.identifier);
127                 break;
            }
130         return UniTask.Defer(pack.Reload);                              // ② 등록된 모든 팩을 동시에 리로드
        }));
    }
```

`NotRequired` 팩은 자동으로 활성화되지 않습니다. 사용자가 `EnablePack`으로 켭니다. ② 리로드는 활성화 여부와 관계없이 **등록된** 팩 모두에 대해 일어납니다.

`BeforeVanilla`는 vanilla 팩의 위치를 찾아 그 바로 **뒤**(낮은 우선순위)에 넣습니다. vanilla가 활성화되어 있지 않으면 0번에 넣습니다. vanilla 팩 자신도 `BeforeVanilla`지만 이미 활성화되어 있어 `EnablePack`이 아무것도 하지 않습니다. `AfterVanilla`는 0번에 넣으며, vanilla는 항상 맨 뒤에 있으므로 vanilla **위**가 됩니다.

> **관찰된 동작:** 같은 정렬 값의 필수 팩이 여럿이면 서로의 순서는 정해져 있지 않고 `loadedResourcePacks` 열거 순서를 따릅니다(의도적으로 미지정).

현재 에디터의 활성화 목록은 `[runios:vanilla, runios:editor]`가 됩니다. 에디터 팩이 vanilla(StreamingAssets)보다 낮으므로, StreamingAssets가 에디터 팩의 에셋을 덮어씁니다.

> **관찰된 동작:** `_enabledPackIdentifiers`에는 "임시"라는 TODO 주석이 있습니다(51~54행). 활성화 목록을 저장하거나 불러오는 코드는 없습니다.

## 2.5 기본 팩과 에디터 팩

| 팩 | 만드는 곳 | 공급자 | 정렬 |
| --- | --- | --- | --- |
| `runios:vanilla` | [`defaultPack`](../../Runtime/Resource/ResourcePack.Static.cs#L36) (처음 쓸 때) | 등록된 [`IStreamingAssetsService`](../IO/07-streaming-assets.md#71-istreamingassetsservice)의 공급자. 여럿이면 `GroupIOProvider(leaveOpen: true)` | `BeforeVanilla` |
| `runios:editor` | [`EditorResourcePack`](../../Editor/EditorResourcePack.cs#L12) (`[OnAssemblyLoaded]`, 에디터만) | [`EditorStreamingAssetsService.editorProvider`](../IO/07-streaming-assets.md#75-editorstreamingassets) | `BeforeVanilla` |

```csharp
// Runtime/Resource/ResourcePack.Static.cs
39  static ResourcePack? CreateDefaultPack()
    {
41      IIOProvider[] providers = [..ServiceRegistry.GetAll<IStreamingAssetsService>().Where(x => x.isAvailable).Select(x => x.provider)];
42      if (providers.Length <= 0)
43          return null;                                     // WebGL처럼 서비스가 없으면 기본 팩도 없음
45      return Create("runios:vanilla", providers.Length == 1 ? providers[0] : new GroupIOProvider(providers, leaveOpen: true), RequiredPackSort.BeforeVanilla, leaveOpen: true);
    }
```

## 2.6 `Dispose`와 공급자 소유권

```csharp
// Runtime/Resource/ResourcePack.cs
142 public void Dispose()
    {
144     if (isDisposed)
145         throw new ObjectDisposedException(identifier.ToString());
147     if (!leaveOpen)
148         rootFolder.provider.Dispose();               // ① 팩이 공급자를 소유할 때만
150     isDisposed = true;
151     isValid = false;
153     DisablePack(identifier);                         // ② 활성화 목록과
154     _loadedResourcePacks.Remove(identifier);         //    등록 목록에서 빠짐
    }
```

`leaveOpen`은 `Create`의 마지막 인자이고 기본값은 `false`(팩이 공급자를 소유)입니다. `Create(PhysicalPath)`처럼 팩을 만들면서 공급자도 새로 만드는 경우는 팩이 소유하는 것이 맞습니다. 서비스가 가진 공급자처럼 다른 곳에서 계속 쓰는 공급자를 넘길 때는 `leaveOpen: true`를 줍니다. vanilla 팩과 editor 팩이 그렇게 만들어집니다.

두 번 dispose하면 `ObjectDisposedException`입니다.
