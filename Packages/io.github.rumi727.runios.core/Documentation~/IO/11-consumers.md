# 11. 사용처

> 기준 커밋: `b3e1a343`. [← 목차](README.md) · 이전: [10. Locations](10-locations.md) · 다음: [12. 공급자 차이와 기존 문서](12-differences.md)

IO 시스템을 실제로 어떻게 쓰는지 core와 다른 패키지의 대표 사용처로 정리합니다. 각 사용처의 내부 동작은 해당 시스템 문서가 다루고, 여기서는 **IO API를 어떻게 부르는가**만 봅니다.

- [11.1 사용 패턴 요약](#111-사용-패턴-요약)
- [11.2 리소스 팩](#112-리소스-팩)
- [11.3 에셋 레지스트리와 핸들](#113-에셋-레지스트리와-핸들)
- [11.4 에셋 로더: 바이트 vs 물리 경로](#114-에셋-로더-바이트-vs-물리-경로)
- [11.5 파일 브라우저](#115-파일-브라우저)

## 11.1 사용 패턴 요약

| 하고 싶은 일 | 쓰는 API | 사용처 예 |
| --- | --- | --- |
| 공급자 루트에서 시작 | `provider.rootNode`, `CreateChild` | `ResourcePack` |
| 하위 폴더 목록 | `node.dir.GetDirectories()` | `ResourcePack` 네임스페이스 |
| 확장자로 파일 찾기 | `node.dir.GetAllFiles(matcher)` + `Bind(entry)` | `SimpleAssetRegistry`, `LanguageAssetRegistry` |
| 파일이 바뀌었는지 | `node.file.GetEntry()` + `FileMetaData.IsSameRevision` | `AssetHandle`, `AssetSidecar` |
| 같은 파일인지 | `IONode.IsSameTarget` | `AssetHandle`, `AssetSidecar` |
| 네이티브 API에 경로로 넘기기 | `IPhysicalBackedIOProvider.GetPhysicalPath` | FMOD 사운드 생성 |
| 쓰기 가능 여부 | `node.AsWriteNode()`, `provider is IWritableIOProvider` | 파일 브라우저, 에디터 팩 브라우저 |
| 백그라운드에서 처리해도 되는지 | `provider.isThreadSafe` | 파일 브라우저 전송 |

## 11.2 리소스 팩

> 이 절의 코드 인용과 줄 번호는 공급자 소유권 수정 이후인 `7e5d710f` 기준입니다. 자세한 내용은 [Resource 2장](../Resource/02-resource-packs.md)에 있습니다.

[`ResourcePack`](../../Runtime/Resource/ResourcePack.cs#L38)은 공급자 하나를 받아 팩 구조를 노드로 잡습니다.

```csharp
// Runtime/Resource/ResourcePack.cs
43  rootFolder = provider.rootNode;
44  assetFolder = rootFolder.CreateChild(assetsFolderName);    // "assets"
45  infoFile = rootFolder.CreateChild(infoPath);               // "pack.json"
…
94  if (!await infoFile.file.Exists())
…
104     metaData = JsonConvert.DeserializeObject<PackMetaData>(await infoFile.file.ReadAllText());
…
115 else if (await assetFolder.dir.Exists())
…
120     await assetFolder.dir.GetDirectories()                  // 네임스페이스 = assets/ 아래 폴더
```

팩은 공급자의 종류를 모릅니다. 디스크 폴더(`ResourcePack.Create(PhysicalPath)`는 기본 정책 `BlockLinks`의 `PhysicalIOProvider`를 만듦, [`#L62`](../../Runtime/Resource/ResourcePack.Static.cs#L62)), StreamingAssets 서비스의 공급자, 에디터의 겹친 공급자가 모두 같은 코드로 읽힙니다.

기본 팩(`runios:vanilla`)은 등록된 StreamingAssets 서비스의 공급자로 만듭니다.

```csharp
// Runtime/Resource/ResourcePack.Static.cs
39  static ResourcePack? CreateDefaultPack()
    {
41      IIOProvider[] providers = [..ServiceRegistry.GetAll<IStreamingAssetsService>().Where(x => x.isAvailable).Select(x => x.provider)];
42      if (providers.Length <= 0)
43          return null;                                                   // ① 서비스가 없으면 기본 팩 없음 (WebGL)
45      return Create("runios:vanilla", providers.Length == 1 ? providers[0] : new GroupIOProvider(providers, leaveOpen: true), RequiredPackSort.BeforeVanilla, leaveOpen: true);   // ② 팩이 공급자를 소유하지 않음
    }
```

`ResourcePack.Dispose()`는 `leaveOpen`이 아닐 때만 자기 공급자를 dispose합니다([`ResourcePack.cs#L147`](../../Runtime/Resource/ResourcePack.cs#L147)). ② 서비스가 하나든 여럿이든 서비스가 가진 공급자는 팩이 소유하지 않습니다. 여럿일 때 쓰는 그룹도 `leaveOpen: true`라 안쪽 공급자를 dispose하지 않습니다. 에디터 팩(`runios:editor`)도 같은 방식으로 만듭니다([`EditorResourcePack.cs#L12`](../../Editor/EditorResourcePack.cs#L12)).

## 11.3 에셋 레지스트리와 핸들

"한 파일 = 한 에셋" 레지스트리의 기반인 [`SimpleAssetRegistry`](../../Runtime/Resource/SimpleAssetRegistry.cs#L96)는 매처로 파일을 찾고, 엔트리를 노드로 묶습니다.

```csharp
// Runtime/Resource/SimpleAssetRegistry.cs
96  IPatternMatcher patterns = auxiliaryMatcher != null ? PatternMatcherSet.Create(assetMatcher, auxiliaryMatcher) : assetMatcher;   // ① 에셋 + 보조 파일을 한 번에
…
105 await foreach (IOEntry fileEntry in registryNode.dir.GetAllFiles(patterns))
    {
        …
116     RuniPath path = context.GetRegistryRelativePath(fileEntry.path);
117     Identifier identifier = new Identifier(nameSpace, path.GetPathWithoutExtension());   // ② 경로 → 식별자
        …
121     IONode node = registryNode.Bind(fileEntry);                                         // ③ 엔트리 → 노드
122     loadTargetDict.Add(new AssetLoadTarget(context, identifier, node, fileEntry.metaData));
    }
```

① 열거를 두 번 하지 않으려고 에셋 매처와 보조 파일(사이드카 `.json`) 매처를 [`PatternMatcherSet`](03-enumeration-and-matchers.md#32-매처)으로 합칩니다. ② 확장자를 뗀 경로가 식별자가 되므로, [1.4절](01-paths.md#14-fileextension과-확장자-규칙)의 확장자 규칙과 [1.5절](01-paths.md#15-physicalpath-디스크에-닿을-때)의 Windows 소문자화가 식별자에 그대로 반영됩니다. ③ 열거 엔트리의 경로가 루트 기준이라 `Bind`를 씁니다([2.4절](02-providers-and-nodes.md#24-ionode-공급자--경로)).

에셋 핸들은 노드와 열거 때의 메타데이터를 들고 있다가, 리로드 때 같은 파일인지 비교합니다.

```csharp
// Runtime/Resource/AssetHandle`1.cs
234 if (GetType() != other.GetType() || !node.IsSameTarget(otherHandle.node) || !fileMetaData.IsSameRevision(otherHandle.fileMetaData))
```

같은 공급자 대상·같은 경로·같은 리비전이면 기존 핸들(이미 로드한 에셋)을 재사용합니다. 이 판정이 [`IsSameTarget`](02-providers-and-nodes.md#22-iioprovider-읽기-계약)과 [`IsSameRevision`](02-providers-and-nodes.md#25-ioentry와-filemetadata)이 따로 있는 이유입니다.

## 11.4 에셋 로더: 바이트 vs 물리 경로

| 사용처 | 방법 | 이유 |
| --- | --- | --- |
| `io.github.rumi727.runios.texture`의 [`TextureLoader`](../../../io.github.rumi727.runios.texture/Runtime/Textures/TextureLoader.cs#L105) | `node.file.ReadAllBytes()` | 디코더가 바이트를 받음 |
| `io.github.rumi727.runios.fmod`의 [`SoundSystem.CreateSoundAsync`](../../../io.github.rumi727.runios.fmod/Runtime/Sounds/SoundSystem.CreateSoundAsync.cs#L24) | 공급자가 `IPhysicalBackedIOProvider`면 `GetPhysicalPath`, 아니면 `ReadAllBytes` | FMOD는 파일 경로를 직접 열 수 있어, 큰 오디오를 메모리에 복사하지 않음 |
| 같은 패키지의 [`CreateStreamAsync`](../../../io.github.rumi727.runios.fmod/Runtime/Sounds/SoundSystem.CreateStreamAsync.cs#L52) | 위와 같음 | 스트리밍은 경로가 있어야 파일을 조금씩 읽음 |

FMOD의 지름길은 [4.6절](04-physical-and-sandbox.md#46-샌드박스를-거치지-않는-길)대로 샌드박스를 거치지 않은 경로를 FMOD에 넘깁니다.

## 11.5 파일 브라우저

`io.github.rumi727.runios.uielements`의 파일 브라우저는 IO API를 가장 넓게 씁니다.

- **전송**([`IOTransfer`](../../../io.github.rumi727.runios.uielements/Runtime/UIElements/IO/IOTransfer.cs#L68)): 두 공급자가 모두 `isThreadSafe`이면 백그라운드 스레드에서 복사·이동합니다. 같은 공급자(참조 비교)면 공급자의 `CopyFile`/`MoveFile`을 쓰고, 다르면 한쪽에서 읽어 다른 쪽에 씁니다. 다른 공급자 사이 복사는 임시 파일(`.이름.runios-temp`)에 쓴 뒤 `MoveFile`로 바꿔 끼워, 실패해도 반쯤 쓴 파일이 최종 이름으로 남지 않게 합니다. [5.3절](05-write-operations.md#53-copyfile과-movefile)의 기본 `CopyFile`이 덮어쓰기 실패 때 원래 파일을 잃는 문제를 피하는 방법이기도 합니다.
- **쓰기 가능 판단:** `AsWriteNode()`로 새 폴더·이름 바꾸기·삭제를 켜고 끕니다.
- **운영체제 연동:** 물리 경로 복사, 파일 관리자에서 보기, 휴지통은 `GetPhysicalPath`와 [10장](10-locations.md)의 서비스를 씁니다.
- **위치 열기:** [10.3절](10-locations.md#103-위치에서-공급자로)대로 드라이브 루트 공급자로 바꿔 엽니다.
