# Resource System Overview

Language available: \[[한국어 (대한민국)](README.md)\] \[[**English (US)**](README-EN.md)\]  

## Overview

This project's resource system is inspired by Minecraft resource packs.\
Files live inside resource packs, and an asset registry accepts an `Identifier` and registers handles capable of providing the asset type promised by that registry.

```text
Identifier
-> AssetRegistry
-> AssetHandle
-> AssetScope
-> asset object
```

The core registry contract is that **one identifier refers to one asset**.\
How that asset is produced is up to the registry: it may read one file or many files, merge data, inspect other namespaces, depend on another registry, or even construct the final object during registry reload.\
The same physical source file may also be interpreted independently by multiple registries. For example, one audio source may be exposed as `FMOD.Sound` by an FMOD registry and as Unity `AudioClip` by another registry.

Ordinary file-backed handles usually defer real object creation until `GetScope()` is called, but laziness is not a global registry requirement.\
Implementations may wrap an already-created object with `InstanceAssetHandle<TAsset>`, and registries such as `LanguageAssetRegistry` may parse data and construct the final asset object during reload.

## Loading Flow

Initial loading starts from `BootLoader`.

```text
BootLoader
-> ResourceManager.Reload
-> ResourcePack.ReloadAll
-> AssetRegistryManager.GetAll
-> AssetRegistry.Reload
```

Runtime lookup goes through registries and keys.

```text
ResourceKey
-> ResourceManager.GetHandle
-> AssetRegistryManager.Get
-> AssetRegistry[assetId]
-> AssetHandle.GetScope
```

`ResourceManager.Reload()` does not run duplicate reloads concurrently.\
If another reload is requested while one is already running, the request is remembered and another pass runs after the current pass finishes.

All resource packs are refreshed first by `ResourcePack.ReloadAll()`.\
Then a snapshot of enabled resource packs is passed to every asset registry, and registries are reloaded in parallel.

After reload completes, `preReloadCompletionEvent` and `reloadCompletionEvent` are invoked.\
Systems such as renderers or UI can use this point to check whether their cached handle is still the latest handle in the registry, and reacquire it if needed.

## Resource Pack Layout

A resource pack has `pack.json` at its root.\
Assets are stored under the `assets` folder by namespace.

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

The default identifier format is `namespace:path`.

```text
runios:lang
runios:ui/click
```

When shorthand form has no namespace, the default namespace depends on the API being used.

- When `defaultNamespace` is omitted in `Identifier.Parse("path")`, or when the `"path"` -> `Identifier` conversion is used, the value from `[assembly: DefaultIdentifierNamespace("...")]` on the calling assembly is used.
- If the calling assembly has no `DefaultIdentifierNamespaceAttribute`, the system fallback `runios` is used.
- `Identifier.Parse` and the `"path"` -> `Identifier` conversion are discouraged convenience APIs. They parse text and may inspect the calling assembly, so use them only where convenience matters, such as UI or authoring tools.
- Constructors do not inspect the calling assembly. When the namespace and path are already known, `new Identifier(nameSpace, path)` is the preferred fast path.
- Code paths without a meaningful caller, such as JSON loaders, also pass `Identifier.defaultNamespace` explicitly and therefore use `runios` as the fallback.
- If a JSON loader knows the owner namespace, it can pass `IdentifierJsonContext` through `JsonSerializerSettings.Context` to set the default namespace for shorthand IDs.
- Resource pack files get their namespace from the `assets/{namespace}/...` folder name, so they do not depend on this default namespace rule.

This rule keeps game and mod shorthand IDs from being locked to the framework namespace even though the framework itself uses `runios`.

`ResourcePack.defaultPack` is the `vanilla` pack and uses `StreamingIOProvider.instance`.\
`RequiredPackSort.BeforeVanilla` and `RequiredPackSort.AfterVanilla` can place required packs before or after `vanilla`.

## ResourceKey and Identifier

`Identifier` is an ID made of a namespace and path.

```csharp
Identifier id = new Identifier("runios", "ui/click");
```

Game and package assemblies can define their default identifier namespace.

```csharp
#nullable enable
using RuniOS.Resource;

[assembly: DefaultIdentifierNamespace("my_game")]
```

Inside that assembly, omitted namespaces in `Identifier.Parse` or string conversion resolve to `my_game`.

```csharp
Identifier id = Identifier.Parse("ui/click");
// my_game:ui/click

Identifier sameId = "ui/click";
// my_game:ui/click
```

For runtime code, persisted data, and bulk loading paths, use constructors with an explicit namespace.

```csharp
Identifier id = new Identifier("my_game", "ui/click");
```

Prefer explicit namespaces for shared code, persisted data, and documentation examples where the call site should not change the meaning.

JSON deserialization uses the serializer context instead of the calling assembly. When loading JSON with a known owner namespace, pass `IdentifierJsonContext`.

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

`ResourceKey` stores both the registry ID and the asset ID.

```csharp
ResourceKey key = new ResourceKey
(
    new Identifier("runios", "sounds"),
    new Identifier("runios", "ui/click")
);
```

So `registryId` means "which registry should be searched", and `assetId` means "which asset inside that registry."

## AssetRegistryManager

`AssetRegistryManager` manages all registered registries.

```csharp
AssetRegistryManager.Register<MyAssetRegistry>();
```

Registries are registered and unregistered with the code lifecycle. Current implementations usually register from `[OnCodeLoaded]` and unregister from `[OnCodeUnloading]`.

Registries can be queried by these keys.

```text
registryId       -> AssetRegistryManager.Get(registryId)
registry type    -> AssetRegistryManager.Get<TRegistry>()
asset type       -> AssetRegistryManager.GetAllForAsset(assetType)
first registry   -> AssetRegistryManager.GetFirstForAsset<TAsset>()
```

Among registries for the same asset type, the registry with the highest `priority` is selected.\
`AssetRegistryManager.GetFirstForAsset<TAsset>()` returns the cached highest-priority registry.\
Selection order among registries with equal `priority` is not part of the contract. The current implementation may select the registry registered first, but callers must not depend on that behavior.\
The first registry is cached at registration time, so do not change `priority` after registration.\
The key-mode `AssetRef<T>` inspector field also uses this information to select compatible registries and assets.\
Direct mode uses the asset instance stored in the reference without querying a registry.

## Fast Reload Model

A registry recalculates its `Identifier -> AssetHandle` index during reload.\
`AssetRegistry<THandle>` uses a temporary tracking table and compares the handles recorded by the new pass with the existing handles.

```text
BeginTracking
-> RecordAssetHandle
-> EndTracking
```

When the same ID already exists and the new handle points to the same target according to `IsSameTarget()`, `RecordAssetHandle` reuses the existing handle.

```text
same identifier + same target -> keep old handle
same identifier + changed target -> replace with new handle
missing from reload pass -> remove from registry
```

This lets a registry recompute its complete registration result while preserving unchanged handles and already-loaded objects.\
Systems that still hold an old handle can reacquire the latest handle after reload completion when a target changed.

However, **registry reload is not required to be a cheap file-indexing pass**. `SimpleAssetRegistry` mostly performs file discovery and metadata comparison, while a custom registry may parse JSON, merge multiple files, or even construct the final asset objects and wrap them in `InstanceAssetHandle<TAsset>` during reload. The registry implementation owns the cost and lifecycle consequences of that choice.

## AssetHandle and AssetScope

`IAssetHandle` is the common contract for handles registered by asset registries. Not every handle is required to have a backing file or sidecar.\
`AssetHandle<TAsset>` is the ordinary base implementation for lazy loading, unloading, and scope lifetime. The real asset is loaded through `GetScope()` when needed.

```csharp
IAssetScope<MyAsset>? scope = await handle.GetScope();
if (scope == null)
    return;

using (scope)
{
    MyAsset asset = scope.asset;
}
```

`AssetScope<TAsset>` is a usage token for the asset.\
It must be disposed when the caller is done using the asset.

When all scopes are returned, an ordinary `AssetHandle<TAsset>` tries to unload after `unloadDelayFrame`.\
This reduces unnecessary unload and reload work when the same asset is requested again soon.

`InstanceAssetHandle<TAsset>` directly wraps an already-existing object and does not require file loading or a sidecar.

`IsSameTarget()` decides whether an existing handle can be reused during registry reload. The ordinary `AssetHandle<TAsset>` implementation compares the concrete handle type, I/O target, and file revision metadata. If the handle also implements `IAssetSidecarHandle`, the linked `AssetSidecar` target and revision are included in the comparison.\
`FileMetaData.IsSameRevision()` requires matching `lastWriteTime` values, and additionally compares `size` and `creationTime` when both sides provide those values. If the write time is unavailable, the metadata is not considered the same revision.

## AssetSidecar

`AssetSidecar` represents an extensible JSON sidecar associated with an I/O asset.\
A sidecar is not a private set of "import settings" owned by one handle or registry. It is separate I/O data that any registry, handle, or feature may load and interpret when needed.

The sidecar file name is formed by appending `.json` to the complete original file name.

```text
assets/runios/sounds/ui/click.ogg
assets/runios/sounds/ui/click.ogg.json

assets/runios/textures/character.png
assets/runios/textures/character.png.json
```

The top-level data in an `AssetSidecar` is a `Dictionary<Identifier, JObject>`.\
Each `Identifier` can act as the namespace of the registry or feature that interprets that section, and does not have to match the identifier of the source asset.

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

Unknown identifiers and fields remain preserved as `JObject` values, so new consumers can add their own sections without modifying a central schema.

`Reload()` rereads the current `IONode` and updates the sidecar to reflect the current source state.\
If the file exists but cannot be read or deserialized, the data is treated as empty and an error is logged, while `FileMetaData` still keeps the revision of the current file entry. If the file does not exist, both data and metadata become empty.

```csharp
await sidecar.Reload();

Identifier key = new Identifier("my_game", "music");
JObject? rawData = sidecar[key];
MusicData? typedData = sidecar.GetValue<MusicData>(key);

if (sidecar.TryGetValue<MusicData>(key, out MusicData? data))
{
    // Use data.
}
```

`IAssetHandle` itself does not require a sidecar. Only handles that need a sidecar for target identity or loading implement `IAssetSidecarHandle` and expose `AssetSidecar sidecar`.\
This means handles such as `InstanceAssetHandle<TAsset>` no longer need a dummy empty sidecar.

The consumer also decides **when** to read a sidecar. An ordinary file handle may read it while loading the real asset, while a registry such as `SpriteRegistry` may read it during its own `Reload()` when sidecar contents determine which asset IDs should be registered.

## AssetRef

`AssetRef<TAsset>` is a wrapper for referencing a resource of a specific type by key or by direct asset instance.\
It uses `key` or `directAsset` according to `mode`.

The supported modes are:

- `AssetRefMode.key`: resolves the asset from a registry through a `ResourceKey`.
- `AssetRefMode.direct`: wraps the `directAsset` in an `InstanceAssetHandle<TAsset>` and uses it without a registry entry.

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

When using it, call `LoadScopeAsync()` instead of manually finding the registry and handle.\
Key mode resolves a handle through `ResourceManager`; direct mode creates a scope from the stored instance immediately.

```csharp
IAssetScope<MyAsset>? scope = await assetRef.LoadScopeAsync();
if (scope == null)
    return;

using (scope)
{
    MyAsset asset = scope.asset;
}
```

Use `GetHandle()` when a handle is needed. Use `IsSameTarget()` to determine whether the current reference points to the same target as an active scope.

The manual key-mode flow is:

```text
ResourceKey
-> AssetRegistryManager.Get
-> registry[assetId]
-> handle.GetScope
```

Direct mode uses this flow:

```text
directAsset
-> InstanceAssetHandle
-> InstanceAssetScope
```

`AssetRef<TAsset>` wraps both flows in one inspector-friendly API.

When `AssetRefField` or `AssetRefPropertyDrawer` is used in the editor, the mode can be selected in the field.\
For Unity-object direct assets, the `allowSceneObjects` argument controls whether scene objects are accepted and defaults to `false`.\
The default property drawer allows scene objects only when all target objects are non-persistent. Direct asset types that are not Unity objects are displayed as the currently stored value.

## SimpleAssetRegistry

Use `SimpleAssetRegistry<THandle>` for the common "one file in a folder = one asset" pattern.

`SimpleAssetRegistry` scans this folder in every enabled resource pack.

```text
assets/{namespace}/{registryName}
```

Here, `{namespace}` is a namespace folder inside the resource pack being scanned, not the namespace of the registry ID.

`registryId.nameSpace` only prevents registry ID conflicts.\
The default `registryName` is `registryId.path`, and the same `registryName` folder is searched under every resource-pack namespace.

For example, if `registryId` is `example:textures`, the default `registryName` is `textures`.

```text
assets/runios/textures
assets/example/textures
assets/any_namespace/textures
```

The file path with its final extension removed becomes the asset ID.

```text
assets/runios/textures/ui/button.png
-> runios:ui/button

assets/any_namespace/textures/ui/button.png
-> any_namespace:ui/button
```

`SimpleAssetRegistry` provides this file-discovery and ID-mapping pattern as a convenience. It does not require every handle to use a sidecar or a particular loading strategy. A handle that needs one may associate the matching `AssetSidecar` during handle creation.

Most implementations only need to implement `CreateHandle`.

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

For more control, override `OnBeginAssetLoop`, `OnAssetLoop`, or `OnEndAssetLoop`.

In the current implementation, if the same ID was already recorded in the same reload pass, later entries are ignored.\
Pack priority therefore follows enabled pack order and the duplicate handling rule in `RecordAssetHandle`.

## Custom AssetRegistry

If the resource shape is not a simple folder scan, inherit from `AssetRegistry<THandle>` directly.

A custom registry is responsible for **resolving an identifier to the asset type it promises**. It is otherwise free to choose which resource-pack files it reads and how they are combined.\
Multiple registries may interpret the same physical source independently, one file may produce multiple asset IDs, and a registry may even construct final objects during reload and register them through `InstanceAssetHandle<TAsset>`.

Examples:

```text
Merge multiple language json dictionaries and construct LocalizationData immediately
Parse one assets/{namespace}/sounds.json file into many sound IDs
Interpret the same source file independently as different asset types in different registries
Use internal data keys or AssetSidecar contents to determine registered asset IDs
```

Real examples are `LanguageAssetRegistry` and `SoundAssetRegistry`. `LanguageAssetRegistry` merges language JSON during reload, constructs `LocalizationData`, and registers it immediately through `InstanceAssetHandle<LocalizationData>`.

When implementing a registry directly, use `AsyncReloadGate` for duplicate reload coordination. Keep progress reporting and tracking lifecycle in the reload body.

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

`AsyncReloadGate` joins requests into the current reload batch and runs the latest pending request as another pass. There is no need to write `WaitWhile`, a separate `isLoading` flag, or duplicate-execution branches manually.\
The registry decides progress calculation, parallel work, merge rules, and exactly when handles are recorded.

## When To Use Which

`SimpleAssetRegistry` is enough when:

```text
One file in a folder = one asset
File path = asset ID
Target files can be selected with an extension filter
Only the standard file-discovery pattern is needed
```

Direct `AssetRegistry` is better when:

```text
Several files must be merged into one asset
One file produces many asset IDs
AssetSidecar or internal file data must be interpreted during registration
The same source must be reinterpreted with registry-specific rules
Resource-pack-specific merge rules are needed
The final asset object should be constructed during registry reload
Progress and parallel work need custom control
```

## Summary

The resource system separates the physical file layout of resource packs from logical in-game asset access.\
A registry is responsible for resolving an `Identifier` to the asset type it promises, while file discovery, merging, sidecar interpretation, and eager/lazy loading strategies are implementation choices.

Use `SimpleAssetRegistry` for the ordinary one-file-per-asset pattern, and implement `AssetRegistry` directly for complex merging, multi-asset registration, or registration-time data interpretation.\
`AssetSidecar` is an extensible I/O sidecar that may be consumed independently by multiple systems, not private import settings owned by one handle.

Reload recomputes the complete registration result of each registry, but existing handles are reused when `IsSameTarget()` says the target is unchanged.\
This preserves unchanged asset lifetimes without restricting how a registry produces its assets.
