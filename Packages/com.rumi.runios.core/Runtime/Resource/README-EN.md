# Resource System Overview

Language available: \[[한국어 (대한민국)](README.md)\] \[[**English (US)**](README-EN.md)\]  

## Overview

This project's resource system is inspired by Minecraft resource packs.\
Files live inside resource packs, and asset registries expose those files to the game as `Identifier` values and asset handles.

```text
Resource Pack files
-> AssetRegistry
-> AssetHandle
-> AssetScope
-> loaded asset object
```

The important part is that a registry usually does not load every asset object immediately.\
During reload, it mainly recalculates "which asset exists where, and which handle should be used to access it."\
The real asset object is usually loaded later, when `AssetHandle<T>.GetScope()` or `AssetRef<T>.LoadAsync()` is called.

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

Registries are usually registered from an `[Awaken]` method.\
To make them visible in the editor too, existing implementations also use `[UnityEditor.InitializeOnLoadMethod]`.

Registries can be queried by these keys.

```text
registryId       -> AssetRegistryManager.Get(registryId)
registry type    -> AssetRegistryManager.Get<TRegistry>()
asset type       -> AssetRegistryManager.GetAllForAsset(assetType)
default registry -> AssetRegistryManager.GetDefaultForAsset<TAsset>()
```

A registry with `isDefault == true` becomes the default registry for its asset type.\
The `AssetRef<T>` inspector field also uses this information to select compatible registries and assets.

## Fast Reload Model

A registry rebuilds its full index on reload.\
However, this is mostly file lookup and handle recording. It is not a full reload of every real asset object.

`AssetRegistry<THandle>` creates a temporary tracking table while reloading.

```text
BeginTracking
-> RecordAssetHandle
-> EndTracking
```

`RecordAssetHandle` reuses the existing handle when the same ID already exists and the new handle points to the same target.

```text
same identifier + same target -> keep old handle
same identifier + changed target -> replace with new handle
missing from reload pass -> remove from registry
```

So the registry itself behaves like a full reload, but asset handles are replaced only when needed.\
If a file did not change, its old handle stays alive, and an already loaded asset object can continue to be used.

If a file changed, the registry maps that ID to a new handle.\
A renderer or another system that still holds the old handle can listen for reload completion and fetch the latest handle from the registry again.

This makes reload much lighter than a Minecraft-style full asset reload.\
Registry refresh is closer to fast file indexing, while actual asset loading is handled lazily by handles and scopes.

## AssetHandle and AssetScope

`AssetHandle<TAsset>` owns loading and unloading for one asset.\
The real asset is loaded when `GetScope()` is called, if needed.

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

When all scopes are returned, the handle tries to unload after `unloadDelayFrame`.\
This reduces unnecessary unload and reload work when the same asset is requested again soon.

`AssetHandle<TAsset>.IsSameTarget()` decides whether a handle can be reused during reload.\
The default implementation checks handle type, I/O target, file metadata, and the import-data sidecar's target and metadata.

## Asset Import Data

`AssetImportData` is an extensible import-data container separate from `FileMetaData`.\
`FileMetaData` describes file-system information such as file size or write time, while `AssetImportData` represents the sidecar JSON file used to store developer-defined per-asset information.

`SimpleAssetRegistry<THandle>` associates the file obtained by appending `.json` to an asset path as its import-data file.

```text
assets/runios/sounds/ui/click.ogg
assets/runios/sounds/ui/click.ogg.json
```

The top-level keys in an import-data JSON file are `Identifier` values, and each value is a `JObject`.\
Use the key as the identifier of the registry or feature that interprets the data. It does not have to match the file asset's identifier.

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

Identifiers and fields unknown to a package or a particular asset implementation remain in memory as `JObject` values.\
This lets developers add new per-asset data without modifying the existing package code.\
Consumer code is still required if the new value should affect runtime behavior.

Handles read and deserialize the import-data JSON immediately before loading the real asset.\
During registry reload, the registry checks only the sidecar's existence and file metadata; actual JSON deserialization occurs in the `AssetHandle<TAsset>.GetScope()` load path.\
This is not read every frame, but it is read again when an unloaded asset is loaded again.

```csharp
using Newtonsoft.Json.Linq;

Identifier key = new Identifier("my_game", "music");
JObject? rawData = handle.importData[key];
MusicImportData? typedData = handle.importData.GetValue<MusicImportData>(key);

if (handle.importData.TryGetValue<MusicImportData>(key, out MusicImportData? data))
{
    // MusicImportData is an application-defined type.
    // Use data.
}
```

When a key is missing, `GetValue<T>()` returns the default value for that type.\
Use `TryGetValue<T>()` when missing data must be distinguished from a valid default value.\
If the JSON cannot be read, the import data is cleared and an error is logged. The original sidecar file is not deleted or overwritten.

When implementing `AssetRegistry<THandle>` directly, the registry must locate the sidecar file, create `AssetImportData`, and pass it to the handle itself.\
For instance handles without a sidecar file, `InstanceAssetHandle<TAsset>` can use the shared empty `AssetImportData` instance.

## AssetRef

`AssetRef<TAsset>` is an inspector-friendly wrapper for selecting a resource of a specific type.\
Internally, it stores only a `ResourceKey`.

```csharp
[SerializeField] AssetRef<MyAsset> assetRef;
```

When using it, call `LoadAsync()` instead of manually finding the registry, handle, and scope.

```csharp
IAssetScope<MyAsset>? scope = await assetRef.LoadAsync();
if (scope == null)
    return;

using (scope)
{
    MyAsset asset = scope.asset;
}
```

The manual flow would be:

```text
ResourceKey
-> AssetRegistryManager.Get
-> registry[assetId]
-> handle.GetScope
```

`AssetRef<TAsset>` wraps that flow in an inspector-friendly API.

## SimpleAssetRegistry

For the common "every file in this folder is one asset" case, use `SimpleAssetRegistry<THandle>`.

`SimpleAssetRegistry` scans this folder in every enabled resource pack.

```text
assets/{namespace}/{registryName}
```

Here, `{namespace}` is a namespace folder inside the resource pack being scanned.\
It is not the namespace of the registry ID.

`registryId.nameSpace` is only a namespace for avoiding registry ID conflicts.\
It does not limit which folders `SimpleAssetRegistry` scans.

The default value of `registryName` is `registryId.path`.\
This means `SimpleAssetRegistry` searches for the `registryName` folder under every namespace in every enabled resource pack.

For example, if `registryId` is `example:textures`, the default `registryName` is `textures`.\
So it searches these locations for every namespace that exists in the resource pack.

```text
assets/runios/textures
assets/example/textures
assets/any_namespace/textures
```

The file path without extension becomes the asset ID.

```text
assets/runios/textures/ui/button.png
-> runios:ui/button

assets/any_namespace/textures/ui/button.png
-> any_namespace:ui/button
```

Most implementations only need to implement `CreateHandle`.

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

For more control, override `OnBeginAssetLoop`, `OnAssetLoop`, or `OnEndAssetLoop`.

In the current implementation, if the same ID was already recorded in the same reload pass, later entries are ignored.\
That means pack priority follows enabled pack order and the duplicate handling rule in `RecordAssetHandle`.

## Custom AssetRegistry

If the resource shape is not a simple folder scan, inherit from `AssetRegistry<THandle>` directly.

Examples:

```text
Merge dictionaries from multiple language json files
Parse one assets/{namespace}/sounds.json file into many sound IDs
Use internal data keys as asset IDs instead of file paths
```

Real examples are `LanguageAssetRegistry` and `SoundAssetRegistry`.

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
Only CreateHandle differs
```

Direct `AssetRegistry` is better when:

```text
Several files must be merged into one asset
One file produces many asset IDs
Resource pack merge rules are needed
A fixed json file is read instead of scanning a folder
Progress and parallel work need custom control
```

## Summary

The resource system separates resource pack files from in-game asset access.\
Registries quickly index files, while handles own real asset loading and lifetime.

Use `SimpleAssetRegistry` for normal file assets.\
Implement `AssetRegistry` directly for complex merging or custom formats.

Reload recalculates the full registry, but it does not blindly discard and reload every asset object.\
Only changed handles are replaced, and systems can reacquire the latest handle from the registry after reload completion.
