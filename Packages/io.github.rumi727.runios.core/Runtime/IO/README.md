# I/O 시스템 개요

Language available: \[[**한국어 (대한민국)**](README.md)\] \[[English (US)](README-EN.md)\]  

## 개요

이 프로젝트의 I/O 시스템은 파일이 어디에 있는지를 사용하는 쪽에 직접 노출하지 않습니다.\
대신 `IIOProvider`라는 하나의 비동기 API로 파일과 디렉터리를 읽습니다.

```text
Physical files, StreamingAssets, Android assets, virtual files, archives, remote storage
-> IIOProvider
-> IONode
-> read or enumerate
```

중요한 점은 `IIOProvider`를 사용하는 코드는 실제 저장 위치를 몰라도 된다는 것입니다.\
리소스 팩이나 에셋 레지스트리 입장에서는 대상이 로컬 폴더인지, Unity `StreamingAssets`인지, Android APK 내부 asset인지, 네트워크나 압축 파일에서 열리는 스트림인지 신경 쓸 필요가 없습니다.

필요한 것은 "이것은 I/O provider이고, `RuniPath`로 접근할 수 있다"는 사실뿐입니다.

```csharp
IIOProvider provider = StreamingIOProvider.instance;

IONode file = provider.rootNode.CreateChild("assets/runios/lang/ko_kr.json");
string json = await file.file.ReadAllText();
```

특히 `StreamingAssets`처럼 플랫폼마다 접근 방법이 다른 대상도 provider로 감싸면 사용하는 쪽 코드는 그대로 유지됩니다.\
Android에서 `Application.streamingAssetsPath`를 일반 파일 경로처럼 다루기 어려운 문제도 `AndroidStreamingIOProvider`가 처리하고, 밖에서는 같은 `IIOProvider` API만 사용합니다.

## 기본 흐름

`IIOProvider`는 추상 파일 시스템의 루트입니다.\
루트에서 `IONode`를 만들고, 노드의 `file` 또는 `dir` API로 작업합니다.

```text
IIOProvider
-> rootNode
-> IONode.CreateChild
-> node.file / node.dir
-> provider operation
```

읽기 API는 모두 비동기입니다.

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

쓰기까지 필요한 provider는 `IWritableIOProvider`를 구현합니다.

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

`RuniPath`는 프로젝트 내부에서 사용하는 플랫폼 독립 경로입니다.\
디렉터리 구분자는 항상 `/`이며, 시작과 끝의 `/`는 제거되고 반복된 `/`는 하나로 합쳐집니다.

```text
/assets//runios/lang/
-> assets/runios/lang
```

`RuniPath`는 Unix 경로나 Windows 경로를 그대로 따라가지 않습니다.\
`/`만 경로 구분자로 취급하고, 그 밖의 문자는 이름의 일부로 둡니다.

그래서 `.`과 `..`도 `RuniPath` 자체에서는 특별한 디렉터리 이동 문법이 아닙니다.

```text
assets/../lang
```

위 값에서 `..`는 그냥 `..`라는 이름의 세그먼트입니다.\
`RuniPath.NormalizePath`는 dot segment를 해석하지 않습니다.

다만 `PhysicalPath`와 결합되어 실제 파일 시스템 경로가 되는 순간에는 `Path.GetFullPath`와 OS 경로 규칙이 적용됩니다.\
즉 `RuniPath` 자체는 논리 경로이지만, 물리 파일 시스템으로 내려갈 때는 실제 경로 의미가 다시 생길 수 있습니다.

## PhysicalPath

`PhysicalPath`는 실제 파일 시스템 경로를 나타냅니다.\
문자열만 쓰면 이 값이 프로젝트 내부 논리 경로인지, OS 파일 경로인지 헷갈릴 수 있으므로 타입을 분리합니다.

```csharp
PhysicalPath physicalRoot = (PhysicalPath)"./UserData";
RuniPath logicalPath = (RuniPath)"config/settings.json";
```

`PhysicalPath`는 `Path.GetFullPath`로 전체 경로를 만들고, 플랫폼에 맞게 정규화합니다.\
반대로 `RuniPath`는 provider 안쪽에서 쓰는 상대 논리 경로입니다.

`PhysicalIOProvider`는 이 둘을 결합합니다.

```text
PhysicalIOProvider.targetPath
+ RuniPath
-> real file-system path
```

## IONode

`IONode`는 provider와 `RuniPath`를 같이 들고 있는 읽기용 노드입니다.\
문자열 경로를 계속 조합하지 않고 노드에서 자식 노드를 만들며 내려갑니다.

```csharp
IONode root = provider.rootNode;
IONode langFile = root
    .CreateChild("assets")
    .CreateChild("runios")
    .CreateChild("lang/ko_kr.json");

string json = await langFile.file.ReadAllText();
```

디렉터리 작업은 `dir`, 파일 작업은 `file`에서 합니다.

```csharp
await foreach (IOEntry entry in root.CreateChild("assets").dir.GetAllFiles("*.json"))
{
    IONode file = root.Bind(entry);
    string text = await file.file.ReadAllText();
}
```

`IOEntry`는 provider가 발견한 파일 또는 디렉터리의 스냅샷입니다.\
경로, 메타데이터, 디렉터리 여부를 담고, 다시 `IONode.Bind`로 실제 노드에 바인딩할 수 있습니다.

## IOWriteNode

`IOWriteNode`는 `IWritableIOProvider`에서 얻는 쓰기 가능 노드입니다.\
읽기 API도 같이 사용할 수 있고, 필요하면 `IONode`로 암시 변환됩니다.

```csharp
IWritableIOProvider provider = new PhysicalIOProvider((PhysicalPath)"./UserData");

IOWriteNode file = provider.rootNode.CreateChild("config/settings.json");
await file.GetParent().dir.Create();
await file.file.WriteAllText("{}");
```

쓰기 가능 여부는 provider가 결정합니다.\
예를 들어 `PhysicalIOProvider`와 `VirtualIOProvider`는 `IWritableIOProvider`이고, `GroupIOProvider`와 `AndroidStreamingIOProvider`는 읽기 전용 `IIOProvider`입니다.

## PhysicalIOProvider

`PhysicalIOProvider`는 실제 로컬 파일 시스템 디렉터리를 provider로 노출합니다.\
모든 `RuniPath`는 `targetPath` 아래의 실제 경로로 해석됩니다.

```csharp
var provider = new PhysicalIOProvider((PhysicalPath)"./Packs/MyPack");
IONode packInfo = provider.rootNode.CreateChild("pack.json");
```

기본값으로 `SandboxPolicy.Enabled`가 적용됩니다.\
provider 루트 밖으로 벗어나는 경로나 재분석 지점 접근을 막아, 물리 경로 사용 시 생길 수 있는 경계 문제를 줄입니다.

특수한 경우에는 `SandboxPolicy.Disabled`를 사용할 수 있습니다.\
예를 들어 `StreamingIOProvider`는 Unity가 제공한 `StreamingAssets` 경로를 그대로 다루기 위해 비활성화된 물리 provider를 사용합니다.

## StreamingIOProvider

`StreamingIOProvider.instance`는 현재 플랫폼에 맞는 StreamingAssets provider입니다.

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

그래서 StreamingAssets를 읽는 쪽은 플랫폼 분기를 하지 않아도 됩니다.

```csharp
IIOProvider provider = StreamingIOProvider.instance;
byte[] bytes = await provider.rootNode.CreateChild("data/bootstrap.json").file.ReadAllBytes();
```

Editor에서는 프로젝트 StreamingAssets와 패키지 StreamingAssets가 `GroupIOProvider`로 묶입니다.\
Android에서는 `AssetManager`를 통해 APK 내부 asset을 열고 열거합니다.

## GroupIOProvider

`GroupIOProvider`는 여러 `IIOProvider`를 하나로 묶어 하나의 provider처럼 보이게 합니다.\
C#의 `partial`처럼 여러 I/O 트리를 한 트리처럼 겹쳐 쓰는 용도입니다.

```csharp
IIOProvider provider = new GroupIOProvider(userPack, defaultPack);
```

provider 순서가 우선순위입니다.\
같은 파일이나 디렉터리 경로가 여러 provider에 있으면 가장 앞 provider의 항목이 사용되고, 뒤 provider의 같은 경로는 무시됩니다.

```text
GroupIOProvider
 |- userPack      assets/runios/lang/ko_kr.json
 `- defaultPack   assets/runios/lang/ko_kr.json

result -> userPack entry
```

읽기 작업도 먼저 발견된 provider에서 수행됩니다.\
열거할 때도 이미 나온 `RuniPath`는 다시 내보내지 않습니다.

기본적으로 `GroupIOProvider.Dispose()`는 하위 provider들도 같이 정리합니다.\
하위 provider를 계속 유지해야 하면 생성자에서 `leaveOpen: true`를 사용합니다.

## VirtualDirectory와 VirtualIOProvider

`VirtualDirectory`는 메모리 안에서 가상 파일 시스템 트리를 만듭니다.\
`VirtualIOProvider`는 이 트리를 `IWritableIOProvider`로 노출합니다.

```text
VirtualDirectory
-> VirtualIOProvider
-> IOWriteNode / IONode
```

가상 트리에는 직접 만든 `VirtualFile`을 넣을 수도 있고, 기존 `IONode`를 가리키는 `VirtualFile`을 넣을 수도 있습니다.

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

이렇게 하면 실제 파일은 `sourceProvider` 쪽에 있어도, 사용하는 쪽에서는 `assets/example/sounds/title.ogg`라는 가상 경로로 읽을 수 있습니다.

`VirtualFile(IONode)`는 처음에는 원본 노드를 바로 읽습니다.\
그 파일에 쓰기를 시작하면 원본 내용을 메모리 버퍼로 복사한 뒤 shortcut을 끊고, 이후부터는 가상 파일 자체 내용을 사용합니다.\
`Create`로 새로 만들면 기존 shortcut 없이 빈 가상 파일로 바뀝니다.

즉 `VirtualDirectory`는 기존 I/O 대상을 다른 `RuniPath` 아래에 배치하거나, 메모리 파일과 외부 파일을 한 트리 안에 섞어 보여주는 데 쓸 수 있습니다.

## 리소스 시스템과의 관계

리소스 팩은 provider의 루트 노드를 팩 루트로 사용합니다.

```text
ResourcePack
-> IIOProvider.rootNode
-> pack.json
-> assets
```

그래서 리소스 팩은 provider가 어디서 데이터를 가져오는지 몰라도 됩니다.\
로컬 폴더 팩, StreamingAssets 팩, 가상으로 재배치한 팩, 여러 provider를 합친 팩 모두 같은 `ResourcePack.Create(..., IIOProvider provider, ...)` 흐름으로 들어올 수 있습니다.

에셋 레지스트리도 파일을 직접 열지 않고 `IONode`를 받습니다.\
핸들은 그 노드를 보관하고, 실제 에셋이 필요해질 때 `node.file.OpenRead()` 같은 API로 읽습니다.

## 메타데이터와 체크섬

`IOEntry`에는 `FileMetaData`가 들어 있습니다.

```text
name
size
creationTime
lastAccessTime
lastWriteTime
attributes
```

provider에 따라 제공할 수 없는 값은 `null`일 수 있습니다.\
예를 들어 Android asset처럼 일반 파일 시스템 메타데이터를 그대로 얻기 어려운 provider는 일부 값만 채울 수 있습니다.

`IONode.GetFileChecksum()`은 파일의 MD5 체크섬을 계산합니다.\
provider가 `IPrecalculatedIOChecksum`을 구현하면 미리 계산된 체크섬을 사용할 수 있고, 아니면 파일 스트림을 읽어서 계산합니다.

## 언제 무엇을 쓸까

`IIOProvider`가 맞는 경우:

```text
읽기만 필요함
데이터 위치를 숨기고 싶음
리소스 팩이나 레지스트리처럼 저장소 종류와 무관해야 함
```

`IWritableIOProvider`가 맞는 경우:

```text
파일 생성, 수정, 삭제가 필요함
저장 데이터나 임시 가상 파일을 다뤄야 함
```

`PhysicalIOProvider`가 맞는 경우:

```text
실제 로컬 폴더를 읽고 쓰고 싶음
OS 파일 시스템 메타데이터가 필요함
```

`StreamingIOProvider`가 맞는 경우:

```text
Unity StreamingAssets를 플랫폼 분기 없이 읽고 싶음
```

`GroupIOProvider`가 맞는 경우:

```text
여러 provider를 우선순위 있는 하나의 읽기 트리처럼 합치고 싶음
중복 경로에서 앞 provider가 뒤 provider를 덮어야 함
```

`VirtualIOProvider`가 맞는 경우:

```text
실제 위치와 다른 논리 경로로 파일을 노출하고 싶음
메모리 파일과 외부 IONode를 한 트리 안에 섞고 싶음
쓰기 가능한 가상 파일 시스템이 필요함
```

## 요약

I/O 시스템은 저장 위치와 사용 코드를 분리합니다.\
사용하는 쪽은 `IIOProvider`, `RuniPath`, `IONode`만 보면 됩니다.

`RuniPath`는 플랫폼 독립 논리 경로이고, `PhysicalPath`는 실제 파일 시스템 경로입니다.\
`IONode`는 provider 안의 경로를 노드 구조로 다루게 해 줍니다.

`PhysicalIOProvider`, `StreamingIOProvider`, `GroupIOProvider`, `VirtualIOProvider`는 서로 다른 저장소를 같은 API로 맞춥니다.\
덕분에 리소스 팩과 에셋 레지스트리는 파일이 어디에서 오는지 몰라도 같은 방식으로 읽고 열거할 수 있습니다.
