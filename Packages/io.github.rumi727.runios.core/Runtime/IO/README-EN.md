# I/O System Overview

Language available: \[[한국어 (대한민국)](README.md)\] \[[**English (US)**](README-EN.md)\]  

## Overview

This project's I/O system does not expose where files physically live to the code that uses them.\
Instead, files and directories are read through one asynchronous API: `IIOProvider`.

```text
Physical files, StreamingAssets, Android assets, virtual files, archives, remote storage
-> IIOProvider
-> IONode
-> read or enumerate
```

The important part is that code using an `IIOProvider` does not need to know the real backing storage.\
From the perspective of a resource pack or asset registry, it does not matter whether the target is a local folder, Unity `StreamingAssets`, an Android APK asset, a network source, or a stream from an archive.

The code only needs to know: "this is an I/O provider, and it can be accessed with `RuniPath`."

```csharp
IIOProvider provider = StreamingIOProvider.instance;

IONode file = provider.rootNode.CreateChild("assets/runios/lang/ko_kr.json");
string json = await file.file.ReadAllText();
```

This is especially useful for targets such as `StreamingAssets`, where access rules differ by platform.\
On Android, `AndroidStreamingIOProvider` handles APK asset access internally, while callers keep using the same `IIOProvider` API.

## Basic Flow

`IIOProvider` is the root of an abstract file system.\
You create `IONode` values from the root, then use the node's `file` or `dir` API.

```text
IIOProvider
-> rootNode
-> IONode.CreateChild
-> node.file / node.dir
-> provider operation
```

Read APIs are asynchronous.

```text
DirectoryExists
FileExists
GetEntry
EnumerateEntries
OpenRead
ReadAllBytes
ReadAllText
ReadLines
```

Providers that support writes implement `IWritableIOProvider`.

```text
OpenWrite
CreateDirectory
CreateFile
WriteAllBytes
WriteAllText
WriteLines
DeleteDirectory
DeleteFile
```

## RuniPath

`RuniPath` is the platform-independent logical path used inside the project.\
The directory separator is always `/`. Leading and trailing `/` characters are removed, and repeated `/` separators are collapsed.

```text
/assets//runios/lang/
-> assets/runios/lang
```

`RuniPath` does not follow Unix or Windows path rules directly.\
Only `/` is treated as a path separator. Every other character remains part of a name.

This means `.` and `..` are not special traversal syntax in `RuniPath` itself.

```text
assets/../lang
```

In this value, `..` is just a segment named `..`.\
`RuniPath.NormalizePath` does not interpret dot segments.

However, once a `RuniPath` is combined with `PhysicalPath` and becomes a real file-system path, `Path.GetFullPath` and OS path rules apply.\
So `RuniPath` itself is a logical path, but real file-system meaning can appear when it is resolved by a physical provider.

## PhysicalPath

`PhysicalPath` represents a real file-system path.\
It exists separately from `RuniPath` so a string path is not ambiguous between "logical project path" and "real OS path."

```csharp
PhysicalPath physicalRoot = (PhysicalPath)"./UserData";
RuniPath logicalPath = (RuniPath)"config/settings.json";
```

`PhysicalPath` is normalized through `Path.GetFullPath` and platform path rules.\
`RuniPath` is the provider-relative logical path used inside the I/O abstraction.

`PhysicalIOProvider` combines the two.

```text
PhysicalIOProvider.targetPath
+ RuniPath
-> real file-system path
```

## IONode

`IONode` is a read-only node that stores both a provider and a `RuniPath`.\
Instead of repeatedly combining path strings, code navigates by creating child nodes.

```csharp
IONode root = provider.rootNode;
IONode langFile = root
    .CreateChild("assets")
    .CreateChild("runios")
    .CreateChild("lang/ko_kr.json");

string json = await langFile.file.ReadAllText();
```

Directory operations live under `dir`, and file operations live under `file`.

```csharp
await foreach (IOEntry entry in root.CreateChild("assets").dir.GetAllFiles("*.json"))
{
    IONode file = root.Bind(entry);
    string text = await file.file.ReadAllText();
}
```

`IOEntry` is a snapshot of a file or directory discovered by a provider.\
It contains the path, metadata, and whether the entry is a directory. It can be bound back to a node with `IONode.Bind`.

## IOWriteNode

`IOWriteNode` is the writable node returned by `IWritableIOProvider`.\
It also supports read APIs, and it can be implicitly converted to `IONode`.

```csharp
IWritableIOProvider provider = new PhysicalIOProvider((PhysicalPath)"./UserData");

IOWriteNode file = provider.rootNode.CreateChild("config/settings.json");
await file.GetParent().dir.Create();
await file.file.WriteAllText("{}");
```

Whether writing is supported depends on the provider.\
For example, `PhysicalIOProvider` and `VirtualIOProvider` implement `IWritableIOProvider`, while `GroupIOProvider` and `AndroidStreamingIOProvider` are read-only `IIOProvider` implementations.

## PhysicalIOProvider

`PhysicalIOProvider` exposes a real local file-system directory as a provider.\
Every `RuniPath` is resolved under `targetPath`.

```csharp
var provider = new PhysicalIOProvider((PhysicalPath)"./Packs/MyPack");
IONode packInfo = provider.rootNode.CreateChild("pack.json");
```

By default, `SandboxPolicy.Enabled` is applied.\
It reduces boundary issues by blocking paths that escape the provider root and by blocking reparse-point access.

Special cases can use `SandboxPolicy.Disabled`.\
For example, `StreamingIOProvider` uses disabled sandbox validation when wrapping Unity-provided `StreamingAssets` paths.

## StreamingIOProvider

`StreamingIOProvider.instance` is the StreamingAssets provider for the current platform.

```text
Unity Editor
-> GroupIOProvider
   -> Application.streamingAssetsPath
   -> registered package StreamingAssets folders

Android
-> AndroidStreamingIOProvider

Other platforms
-> PhysicalIOProvider(Application.streamingAssetsPath)
```

So code reading StreamingAssets does not need platform branches.

```csharp
IIOProvider provider = StreamingIOProvider.instance;
byte[] bytes = await provider.rootNode.CreateChild("data/bootstrap.json").file.ReadAllBytes();
```

In the Editor, project StreamingAssets and package StreamingAssets are merged with `GroupIOProvider`.\
On Android, assets inside the APK are opened and enumerated through `AssetManager`.

## GroupIOProvider

`GroupIOProvider` exposes several `IIOProvider` instances as one provider.\
It is useful when several I/O trees should be layered like C# `partial` declarations.

```csharp
IIOProvider provider = new GroupIOProvider(userPack, defaultPack);
```

Provider order is priority order.\
If the same file or directory path exists in more than one provider, the first provider wins and later providers with the same path are ignored.

```text
GroupIOProvider
 |- userPack      assets/runios/lang/ko_kr.json
 `- defaultPack   assets/runios/lang/ko_kr.json

result -> userPack entry
```

Read operations also use the first provider that contains the file.\
Enumeration suppresses duplicate `RuniPath` values that were already yielded.

By default, `GroupIOProvider.Dispose()` disposes its child providers too.\
Use `leaveOpen: true` if the child providers must stay alive.

## VirtualDirectory and VirtualIOProvider

`VirtualDirectory` creates a virtual file-system tree in memory.\
`VirtualIOProvider` exposes that tree as an `IWritableIOProvider`.

```text
VirtualDirectory
-> VirtualIOProvider
-> IOWriteNode / IONode
```

The virtual tree can contain files created in memory, or `VirtualFile` instances that point to existing `IONode` values.

```csharp
using RuniOS.IO;
using RuniOS.IO.Virtual;

IIOProvider sourceProvider = new PhysicalIOProvider((PhysicalPath)"./ExternalFiles");
IONode sourceFile = sourceProvider.rootNode.CreateChild("audio/title.ogg");

var root = new VirtualDirectory();
root.CreateDirectory((RuniPath)"assets/example/sounds");
root.Attach((RuniPath)"assets/example/sounds/title.ogg", new VirtualFile(sourceFile));

IIOProvider provider = new VirtualIOProvider(root);
Stream stream = await provider.rootNode
    .CreateChild("assets/example/sounds/title.ogg")
    .file
    .OpenRead();
```

With this setup, the real file still lives in `sourceProvider`, but callers read it from the virtual path `assets/example/sounds/title.ogg`.

`VirtualFile(IONode)` initially reads from the original node.\
When the virtual file is opened for writing, it copies the original contents into an in-memory buffer, breaks the shortcut, and uses its own virtual contents from then on.\
When `Create` is used, it becomes an empty virtual file without the original shortcut.

This makes `VirtualDirectory` useful for exposing existing I/O targets under different `RuniPath` values, or for mixing memory files and external nodes in one tree.

## Relation To Resources

A resource pack uses the provider root node as the pack root.

```text
ResourcePack
-> IIOProvider.rootNode
-> pack.json
-> assets
```

Because of that, a resource pack does not need to know where the provider gets its data.\
A local folder pack, StreamingAssets pack, virtually remapped pack, or layered pack can all use the same `ResourcePack.Create(..., IIOProvider provider, ...)` flow.

Asset registries also receive `IONode` values instead of opening files directly.\
Handles keep those nodes and read from them later through APIs such as `node.file.OpenRead()`.

## Metadata and Checksums

`IOEntry` contains `FileMetaData`.

```text
name
size
creationTime
lastAccessTime
lastWriteTime
attributes
```

Values that the backing provider cannot supply may be `null`.\
For example, a provider such as Android assets may only be able to provide part of normal file-system metadata.

`IONode.GetFileChecksum()` calculates an MD5 checksum for the file.\
If the provider implements `IPrecalculatedIOChecksum`, the precomputed checksum is used. Otherwise, the file stream is read and hashed.

## When To Use Which

Use `IIOProvider` when:

```text
Only reading is needed
The storage location should be hidden
Code must be independent from the storage type, such as resource packs or registries
```

Use `IWritableIOProvider` when:

```text
Files must be created, modified, or deleted
Save data or temporary virtual files are needed
```

Use `PhysicalIOProvider` when:

```text
A real local folder should be read or written
OS file-system metadata is useful
```

Use `StreamingIOProvider` when:

```text
Unity StreamingAssets should be read without platform-specific branches
```

Use `GroupIOProvider` when:

```text
Several providers should be merged into one prioritized read tree
Earlier providers should override later providers for duplicate paths
```

Use `VirtualIOProvider` when:

```text
Files should be exposed through logical paths different from their real locations
Memory files and external IONode values should appear in one tree
A writable virtual file system is needed
```

## Summary

The I/O system separates storage location from using code.\
Callers mainly work with `IIOProvider`, `RuniPath`, and `IONode`.

`RuniPath` is a platform-independent logical path, while `PhysicalPath` is a real file-system path.\
`IONode` makes provider paths behave like navigable nodes instead of loose string combinations.

`PhysicalIOProvider`, `StreamingIOProvider`, `GroupIOProvider`, and `VirtualIOProvider` adapt different storage models to the same API.\
This lets resource packs and asset registries read and enumerate files the same way, regardless of where those files actually come from.
