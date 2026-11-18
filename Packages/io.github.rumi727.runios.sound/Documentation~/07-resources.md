# 7. 리소스와 생성

> 기준 커밋: `3a2b24b9`. [← 목차](README.md) · 이전: [6. 파형 렌더링](06-waveform-rendering.md) · 다음: [8. 에디터](08-editor.md)

재생 추상화와 파형 외에 Sound 패키지에 있는 작은 타입들입니다.
리소스 팩에서 "사운드 이름"을 실제 클립으로 잇는 레지스트리, 3D 공간 상태 값, 최소 클립 계약, 플레이어 생성 표가 있습니다.

- [7.1 `SoundClipRef`](#71-soundclipref)
- [7.2 `SoundAssetRegistry`](#72-soundassetregistry)
- [7.3 `AudioSpatialState`](#73-audiospatialstate)
- [7.4 `IAudioClip`](#74-iaudioclip)
- [7.5 `AudioPlayerManager`](#75-audioplayermanager)

---

## 7.1 `SoundClipRef`

```csharp
// Runtime/Sounds/SoundClipRef.cs
6   [Serializable]
7   public record struct SoundClipRef(ResourceKey key)
    {
9       [SerializeField] public ResourceKey key = key;

11      public static implicit operator SoundClipRef(ResourceKey key) => new SoundClipRef(key);
12      public static implicit operator ResourceKey(SoundClipRef sound) => sound.key;
    }
```

core 패키지의 `ResourceKey(registryId, assetId)` 하나를 감싼 직렬화용 값입니다. "어느 레지스트리의 어느 에셋"을 가리킵니다.
`ResourceKey`와 양방향 암시적 변환이 있으므로, 사운드 참조를 받는 자리에 키를 그대로 넘길 수 있습니다.
따로 타입이 있으므로 **"이 키는 사운드 클립을 가리킨다"는 것이 타입에 드러납니다.** 레지스트리가 제공하는 에셋 타입(`providedAssetType`)도 이 타입입니다.

## 7.2 `SoundAssetRegistry`

리소스 팩의 네임스페이스마다 있는 `sounds.json`을 읽어 **사운드 id → `SoundClipRef`** 표를 만드는 레지스트리입니다.
id가 `runios:sounds`이고, core의 `AssetRegistry<InstanceAssetHandle<SoundClipRef>>`를 상속합니다.

```csharp
// Runtime/Resource/Sounds/SoundAssetRegistry.cs
11  public sealed partial class SoundAssetRegistry : AssetRegistry<InstanceAssetHandle<SoundClipRef>>
    {
14      public static readonly Identifier id = new Identifier("runios", "sounds");
15      public const string jsonFileName = "sounds.json";
        …
21      public override bool isLoading => reloadGate.isRunning;
23      readonly AsyncReloadGate reloadGate = new();

25      [OnCodeLoaded]
26      static void OnCodeLoaded() => AssetRegistryManager.Register<SoundAssetRegistry>();      // ① 코드 로드 시 등록

28      [OnCodeUnloading]
29      static void OnCodeUnloading() => AssetRegistryManager.Unregister<SoundAssetRegistry>();

31      public override UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null)
        {
33          ResourcePack[] resourcePackSnapshot = resourcePacks.ToArray();                       // ② 지금의 팩 목록을 고정
34          return reloadGate.Run(passProgress => ReloadCore(resourcePackSnapshot, passProgress), progress);
        }
```

1. ① 수명 주기 특성으로 레지스트리 관리자에 스스로 등록합니다. 그래서 클래스가 `partial`입니다.
2. ② 열거자를 그대로 넘기면 나중에 실행되는 리로드가 바뀐 목록을 볼 수 있습니다. 호출 시점의 배열로 고정한 뒤 `AsyncReloadGate`에 맡깁니다. 게이트는 리로드 실행을 관리하고, 실행 중인지를 `isLoading`으로 알려 줍니다.

```csharp
37  async UniTask ReloadCore(ResourcePack[] resourcePacks, IProgress<float>? progress)
    {
39      BeginTracking();                                                                        // ③ 새 표를 모으기 시작
        try
        {
43          progress.SafeReport(0);

45          List<UniTask> uniTasks = new List<UniTask>();
46          int count = 0;

48          foreach (var resourcePack in resourcePacks)
            {
50              foreach ((string nameSpace, IONode jsonNode) in resourcePack.GetNamespaceNodes()
51                           .Select(x => (x.name, x.CreateChild(jsonFileName))))
                {
53                  if (!await jsonNode.file.Exists())
54                      return;                                                                 // ④

56                  uniTasks.Add(UniTask.Defer(Method));                                        // ⑤ 아직 실행하지 않음

58                  async UniTask Method()
                    {
                        try
                        {
62                          Dictionary<string, ResourceKey>? sounds = JsonConvert.DeserializeObject<Dictionary<string, ResourceKey>>(await jsonNode.file.ReadAllText());
63                          if (sounds == null)
64                              return;

66                          foreach (var sound in sounds)
67                              RecordAssetHandle(new Identifier(nameSpace, sound.Key), new InstanceAssetHandle<SoundClipRef>(sound.Value));   // ⑥
                        }
69                      catch (Exception e)
                        {
71                          Debug.RuntimeLogError($"An exception occurred while loading {jsonNode.path} resources from the resource pack {resourcePack.identifier}. The exception is: {e}");
                        }

75                      progress.SafeReport((float)++count / uniTasks.Count);
                    }
                }
            }

80          await UniTask.WhenAll(uniTasks);                                                    // ⑦ 여기서 모두 실행
        }
        finally
        {
84          progress.SafeReport(1);

86          EndTracking();                                                                      // ⑧ 모은 표로 교체
        }
    }
```

1. ③ ⑧ `BeginTracking`은 빈 표를 새로 만들고, `EndTracking`은 레지스트리의 표를 **그 표로 통째로 교체**합니다(core `AssetRegistry`). 리로드 도중에도 이전 표가 계속 보이다가 끝나는 순간 바뀝니다.
2. ⑤ `UniTask.Defer`는 작업을 감싸기만 하고 실행은 ⑦까지 미룹니다. 그래서 각 작업이 진행률을 보고할 때(`++count / uniTasks.Count`) `uniTasks.Count`는 이미 전체 개수입니다.
3. ⑥ JSON의 각 항목은 `"이름": ResourceKey`이고, 사운드 id는 `<네임스페이스>:<이름>`이 됩니다. 값은 실제 클립을 가진 다른 레지스트리의 키입니다. 이 레지스트리는 **이름에서 키로 가는 간접 참조**만 담습니다.
4. 파일 하나의 실패(잘못된 JSON 등)는 그 파일만 로그를 남기고 건너뜁니다. 다른 파일에는 영향이 없습니다.

> **관찰된 동작:** ④에서 네임스페이스에 `sounds.json`이 없으면 `continue`가 아니라 `return`합니다([`#L53`](../Runtime/Resource/Sounds/SoundAssetRegistry.cs#L53)). 그러면 그때까지 `Defer`로 쌓은 작업은 ⑦의 `WhenAll`에 도달하지 못해 실행되지 않습니다. `finally`의 `EndTracking`은 아무것도 기록되지 않은 빈 표로 교체합니다. 결과적으로 `sounds.json`이 없는 네임스페이스가 하나라도 있으면, 모든 팩의 사운드 등록이 비게 됩니다. core의 `LanguageAssetRegistry`는 같은 자리에서 `continue`합니다.

## 7.3 `AudioSpatialState`

```csharp
// Runtime/Sounds/AudioSpatialState.cs
8   public struct AudioSpatialState(Vector3 position, Vector3 velocity, Quaternion rotation)
    {
10      public AudioSpatialState(Vector3 position) : this(position, Vector3.zero) { }
12      public AudioSpatialState(Vector3 position, Vector3 velocity) : this(position, velocity, Quaternion.identity) { }
14      public AudioSpatialState(Vector3 position, Vector3 velocity, Vector3 forward, Vector3 up) : this(position, velocity, Quaternion.LookRotation(forward, up)) { }
16      public AudioSpatialState(Transform transform) : this(transform.position, Vector3.zero, transform.rotation) { }
18      public AudioSpatialState(Transform transform, Vector3 velocity) : this(transform.position, velocity, transform.rotation) { }

24      public Vector3 position = position;
30      public Vector3 velocity = velocity;
32      public Quaternion rotation = rotation;
    }
```

3D 소리의 위치, 속도(도플러용), 방향을 한 번에 넘기기 위한 값 묶음입니다.

- 생성자 오버로드는 생략한 값을 0 속도, 단위 회전으로 채웁니다.
- `Transform`에서 만들면 위치와 회전만 가져옵니다. **속도는 `Transform`에 없으므로** 따로 넘기지 않으면 0입니다.
- 값 형식이라 스레드 사이에 복사해 넘기기 쉽습니다. `Transform`은 메인 스레드에서만 읽을 수 있으므로, 메인 스레드에서 이 값을 만들어 넘기면 됩니다.

## 7.4 `IAudioClip`

```csharp
// Runtime/Sounds/IAudioClip.cs
8   public interface IAudioClip
    {
14      public double length { get; }
    }
```

클립이 공통으로 제공하는 것은 길이(초) 하나뿐입니다. 샘플 형식이나 로딩 방식은 구현마다 다르므로 계약에 넣지 않습니다.
에디터의 [`RuniAudioClipPackDrawer<T>`](08-editor.md#87-runiaudioclippackdrawer)가 이 계약으로 길이를 표시합니다.

## 7.5 `AudioPlayerManager`

```csharp
// Runtime/Sounds/AudioPlayerManager.cs
6   public static class AudioPlayerManager
    {
8       static readonly Dictionary<Identifier, Func<Identifier, RuniAudioSource>> playerConstructors = [];

10      public static void Register(Identifier registryId, Func<Identifier, RuniAudioSource> constructor) { … }   // 중복이면 InvalidOperationException
16      public static void Unregister(Identifier registryKey) => playerConstructors.Remove(registryKey);
18      public static RuniAudioSource? Create(ResourceKey resourceKey) => playerConstructors.GetValueOrDefault(resourceKey.registryId)?.Invoke(resourceKey.assetId);
    }
```

**레지스트리 id → 플레이어 생성 함수**의 정적 표입니다. `Create(key)`는 키의 레지스트리에 등록된 함수로 `assetId`를 재생할 플레이어를 만들고, 등록된 함수가 없으면 `null`을 돌려줍니다.
7.2의 `SoundClipRef`와 이으면 "사운드 id → 클립 키 → 그 클립 종류의 플레이어"가 됩니다.

현재 저장소에서 이 표에 등록하거나 `Create`를 부르는 코드는 없습니다. 구조가 바뀔 예정이라 여기서는 있는 것만 적습니다.
