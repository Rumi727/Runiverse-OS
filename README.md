# Runiverse OS

Unity 안에 Minecraft의 리소스팩 시스템과 운영체제에 가까운 사용 경험을 넣기 위해 시작한 게임 엔진 라이브러리입니다.

Runiverse OS는 특정 게임 하나만을 위한 도구 모음이 아닙니다.\
리소스가 어디에서 오고, 어떻게 등록되고, 언제 로드되며, 어떤 API로 사용되는지를 하나의 일관된 기반으로 묶어 다양한 게임과 확장 코드가 같은 규칙을 사용하도록 만드는 프로젝트입니다.\
현재 구현은 유저맵 기반 리듬게임에서 사용할 수 있도록 오디오 시간축과 저지연 재생을 중요하게 다루지만, 리소스팩·모딩·에디터 도구·일반적인 게임 런타임까지 여러 사용처를 열어 두는 것을 전제로 합니다.

> 아직 개발 중인 프로젝트입니다. 패키지마다 구현 상태와 검증 수준이 다르며, 완성된 상용 엔진이나 안정 릴리스로 취급하면 안 됩니다.
> 이 라이브러리가 완성되면 기존 [SC KRM 1.0](https://github.com/Rumi727/SC-KRM-1.0/)을 대체할 예정입니다.

## 참고 사항

* 추후 System.Text.Json으로의 전환을 고려해야합니다.
  * 항상 퍼블릭 프로퍼티를 사용해야하며, 정적 멤버는 허용되지 않습니다.
* 추후 유니티가 CoreCLR로 전환될 가능성도 고려해야합니다.
  * 현재 이 프로젝트가 사용중인 C# 언어 버전은 14 입니다. (global using 사랑해요 field 사랑해요)
  * 파일 스코프 네임스페이스 절대엄금
    * 미친 유니티가 올바른 MonoBehaviour나 ScriptableObject, Editor등으로 인식 못함 (= 직렬화가 안됨)
* ~~유니티 에디터 자채를 패치하여 Roslyn 버전을 올려 C# 버전을 올릴 수 있지만, 이 프로젝트는 모두가 사용할 수 있게끔 의도하였기에 (실제로 쓰는 사람은 없겠지만...) 가능한 유니티 순정과의 호환성을 염두에 두고 제작 중입니다.~~
  * 점점 사용 가능 C# 버전 오르더니 이젠 공식 최신까지...ㅋㅋㅋㅋㅋㅋㅋㅋㅋㅋㅋㅋㅋㅋㅋ

## 프로젝트가 시작된 이유

Unity 프로젝트를 만들 때마다 리소스를 Unity의 기본 에셋 흐름에만 맞추는 대신, Minecraft처럼 리소스팩을 엔진의 기본 단위로 사용하고 싶었습니다.\
게임의 기본 UI도 단순한 화면 묶음이 아니라 파일·폴더·설정·테마·인스펙터를 가진 작은 운영체제에 가까운 형태를 목표로 합니다.

동시에 이 프로젝트는 설정상 Rumi의 자캐들이 살아가는 세계관이기도 합니다.\
따라서 Runiverse OS라는 이름은 기술적인 계층과 세계관 속 플랫폼이라는 두 의미를 함께 가집니다.\
관련 설정은 [Rumi727/Rumi727](https://github.com/Rumi727/Rumi727)에서 볼 수 있습니다.

## 핵심 목표

### 리소스팩을 기본 시스템으로

리소스팩은 나중에 추가하는 모드 기능이 아니라 파일·에셋·언어·사운드·텍스처를 연결하는 기본 경계입니다.\
현재 Core는 `pack.json`, `assets/{namespace}/...`, `Identifier`, `ResourceKey`, `AssetRegistry`, `AssetHandle`, `AssetScope`를 중심으로 이 구조를 구현합니다.

### API를 하나로 통합하기

이 프로젝트가 말하는 확장성은 단순히 플러그인 포인트를 많이 제공하는 것이 아닙니다.

> 내부에서 사용하는 API와 외부 확장을 위한 API를 나누지 않습니다.
> 내부 구현에서 사용되는 API도 외부 API처럼 취급하여, 내부에서는 구현할 수 있지만 외부에서는 구현할 수 없는 기능이 생기지 않게 하는 것이 목표입니다.

예를 들어 파일 위치를 아는 코드와 모르는 코드가 서로 다른 파일 접근 API를 쓰지 않도록 `IIOProvider` 하나로 추상화합니다. 에셋도 키 기반 참조와 직접 인스턴스 참조를 `AssetRef<T>` 하나로 다룹니다.

### 확장성을 유지하면서 최적화하기

확장 가능한 구조를 만들기 위해 성능을 포기하지 않습니다.\
비동기 I/O, 지연 에셋 로드, 핸들·스코프 수명 관리, 리로드 중 핸들 재사용, 캐시, `NativeArray`, Burst Job, 필요할 때의 unsafe·네이티브 경로까지 사용합니다.

최적화는 특정 기능을 빠르게 만드는 별도 작업이 아니라 API와 소유권을 설계할 때부터 지켜야 하는 기준입니다.\
일반적인 추상화로 해결되지 않는 병목은 더 낮은 수준까지 내려가 해결할 수 있어야 합니다.\
다만 현재 모든 경로가 최적화되었다는 뜻은 아니며, 이 원칙을 향해 개발 중이라는 뜻입니다.

## 현재 코드의 구조

핵심 흐름은 다음과 같습니다.

```text
IIOProvider
  -> ResourcePack
  -> AssetRegistry
  -> AssetHandle
  -> AssetScope
  -> runtime asset
```

`IIOProvider`가 저장 위치를 숨기고, `ResourcePack`이 리소스팩의 경계를 제공하며, `AssetRegistry`가 파일을 빠르게 인덱싱합니다. `AssetHandle`은 실제 로드와 언로드를 담당하고, `AssetScope`는 사용 중인 에셋의 수명을 나타냅니다. 리로드 때는 전체 인덱스를 다시 계산하되 같은 대상을 가리키는 핸들을 재사용하여 불필요한 에셋 재로드를 줄입니다.

### 위치를 숨기는 I/O

`IIOProvider`는 로컬 파일, Unity `StreamingAssets`, Android APK asset, 여러 provider를 겹친 트리, 메모리 기반 가상 파일을 같은 비동기 API로 노출합니다. 리소스팩과 레지스트리는 데이터가 실제로 어느 저장소에 있는지 알 필요가 없습니다.

```csharp
IIOProvider provider = StreamingIOProvider.instance;
IONode file = provider.rootNode.CreateChild("assets/runios/lang/ko_kr.json");
string json = await file.file.ReadAllText();
```

자세한 계약은 [I/O 시스템 문서](Packages/com.rumi.runios.core/Runtime/IO/README.md)에서 설명합니다.

### 하나의 에셋 참조

키 기반 에셋은 레지스트리에서 찾고, 직접 에셋은 레지스트리 등록 없이 `InstanceAssetHandle<T>`로 감쌉니다. 두 경로 모두 사용하는 쪽에서는 `LoadScopeAsync()`를 호출합니다.

```csharp
AssetRef<Texture2D> byKey = new
(
    new ResourceKey
    (
        new Identifier("my_game", "textures"),
        new Identifier("my_game", "ui/button")
    )
);

AssetRef<Texture2D> direct = new(existingTexture);
```

키와 직접 모드의 세부 동작은 [리소스 시스템 문서](Packages/com.rumi.runios.core/Runtime/Resource/README.md)를 참고하세요.

## 패키지 구성

| 패키지 | 역할 |
| --- | --- |
| `com.rumi.runios.core` | 부트스트랩, I/O provider, 리소스팩·에셋 레지스트리, 텍스트·로컬라이징, 비동기 작업, PlayerLoop, 컬렉션·직렬화·유틸리티, 런타임 검사 모델과 Unity 에디터 도구의 기반 |
| `com.rumi.runios.sound` | 오디오 플레이어 공통 계약과 `RuniAudioSource` 기반. 재생·일시 정지·정지·seek·loop·tempo·pitch·공간 오디오를 하나의 플레이어 표면으로 묶음 |
| `com.rumi.runios.fmod` | FMOD Core `System`, `Sound`, `Channel`, `ChannelGroup` 래퍼. PCM·메모리·`Stream`·`IONode` 입력, DSP, DSP clock 예약, 네이티브 escape hatch 제공. [문서](Packages/com.rumi.runios.fmod/README.md) |
| `com.rumi.runios.nbs` | Note Block Studio 파일 파서와 리소스 레지스트리. NBS 0~6, 커스텀 악기, 템포 변경, Sound Stopper, loop, 순·역방향 timeline, FMOD DSP 기반 예약 재생 지원. [문서](Packages/com.rumi.runios.nbs/README.md) |
| `com.rumi.runios.texture` | FreeImage 기반 런타임 이미지 로더. 메모리·`Stream`·`IONode` 입력, 백그라운드 decode, Burst 밉맵 생성, Unity 메인 스레드 `Texture2D` 업로드. [문서](Packages/com.rumi.runios.texture/README.md) |
| `com.rumi.runios.effects` | UI 둥근 모서리, 단순 메쉬 외곽선, 여러 오브젝트를 합친 외곽선과 관련 셰이더 |
| `com.rumi.runios.ui` | UI 런타임 패키지 경계. 구체적인 기본 UI는 아직 개발 중이며, Core 에디터 쪽에는 IMGUI·UIElements 필드와 인스펙터 기반이 먼저 존재 |
| `com.rumi.runios` | Unity Editor 설치 창, 패키지·scoped registry 설정, TMP 설정, 다국어 설치 화면 |

Core 내부에는 Unity 내부 API를 감싸는 `APIBridge`, Harmony 기반 `Modding`·`Patches`, .NET 호환 보조 계층, unsafe 유틸리티도 별도 어셈블리로 나뉘어 있습니다.\
이런 계층도 내부 전용 구현으로 고립시키기보다 필요한 기능을 같은 확장 표면으로 끌어올리기 위한 기반입니다.

## 리듬게임을 위한 기반

리듬게임에서 시간축을 일관되게 제공하는 것은 중요하지만, 이 프로젝트가 가장 먼저 보는 기준은 레이턴시입니다.\
판정음과 히트 사운드처럼 입력과 거의 동시에 출력되어야 하는 소리는 CPU 사용량만이 아니라 호출 경로, GC 할당, FMOD 믹서 버퍼, 장치 출력까지 이어지는 지연과 jitter를 함께 고려해야 합니다.

이 프로젝트는 FMOD를 다른 오디오 엔진으로 감싸 숨기는 방향을 택하지 않습니다. FMOD Core를 그대로 사용하면서 관리형 래퍼를 편의 계층으로 제공하고, 필요한 개발자에게 `SoundChannel`과 FMOD 네이티브 API를 과감하게 노출합니다. 따라서 모든 소리를 고수준 오디오 소스 계층으로 재생하도록 강제하지 않습니다.

### 레이턴시를 위한 재생 경로 선택

- 계속 재생되는 곡처럼 보간된 `time`, sample 위치, tempo·pitch 변경, seek·loop 및 공간 오디오가 필요한 경우에는 `RuniAudioSource`와 `WaveAudioSource`를 사용할 수 있습니다. 이 계층은 리듬게임에서 읽을 수 있는 시간축과 일반적인 재생 제어를 제공합니다.
- 히트 사운드처럼 즉발 재생 후 끝나는 일회성 SFX는 `SoundSystem.PlaySound(...)`가 반환하는 `SoundChannel`을 직접 사용하는 경로를 선택할 수 있습니다. 오디오 소스 컴포넌트의 생명주기와 상태 동기화를 거치지 않아야 하는 짧은 효과음에 맞는 경로입니다.
- GC 할당과 호출 오버헤드까지 민감한 경로에서는 `SoundChannel.native`로 `FMOD.Channel`을 직접 다루거나, `SoundSystem.UseNative`와 `WaveAudioClip.UseNative`를 통해 FMOD `System`·`Sound`의 네이티브 API를 직접 호출할 수 있습니다. 이 경로로 관리형 channel 래퍼를 생략하고 개발자가 재생·DSP clock·delay·채널 속성을 직접 제어할 수 있습니다.

네이티브 경로는 자동으로 안전해지는 기능이 아니라 의도적인 escape hatch입니다.\
`UseNative`에 직접 접근하면 래퍼가 제공하는 동기화를 우회하고, 복사한 네이티브 handle이 더 이상 유효하지 않을 수 있습니다. `UseNative` callback은 동기적으로 끝내야 하며, callback 밖으로 raw handle을 보관하거나 `await`로 수명을 넘겨서는 안 됩니다.\
즉 일반적인 코드는 `SoundChannel`의 수명 관리와 이벤트를 사용하고, 레이턴시와 할당을 직접 책임질 수 있는 코드만 네이티브 경로를 선택하는 구조입니다.

장기적으로는 일회성 SFX에서도 관리형 수명 계약을 유지하면서 할당을 줄일 수 있도록 `SoundChannel` 재활용·풀링 경로를 보완할 예정입니다. 그 전까지도 성능이 중요한 호출자가 필요하면 더 낮은 FMOD 계층까지 내려갈 수 있어야 한다는 원칙은 유지합니다.

### 시간축과 DSP clock

레이턴시를 최우선으로 둔다고 해서 시간축을 포기하는 것은 아닙니다. `RuniAudioSource`는 보간된 재생 시간을 공통 플레이어 계약으로 제공하고, FMOD channel의 PCM 위치는 살아 있는 channel의 상태를 보정하는 기준으로 사용합니다. 실제 출력 시점을 맞춰야 하는 예약 재생에는 FMOD DSP clock과 channel delay를 사용합니다.

`NoteBlockSource`는 NBS 파일을 단순히 한 번에 읽어 재생하지 않습니다. 파일을 불변 파싱 데이터, tempo map, note·event map, clip 독립 playback map으로 나눈 뒤 현재 악기와 재생 설정으로 schedule을 만들고, 공유 background worker가 lookahead 범위의 voice를 FMOD DSP clock에 맞춰 예약합니다. 중간 재생, seek, 늦은 리소스 로드, 리로드, loop 경계를 고려하는 구조도 레이턴시와 시간축을 함께 보존하기 위해 들어가 있습니다.

이 구조는 NBS 전용 기능으로 끝나지 않습니다. 오디오 클록·리소스 로드·에셋 참조·시각 이벤트를 분리해 두었기 때문에, 다른 리듬게임 포맷이나 일반적인 음악·효과음 시스템으로 확장할 수 있는 여지를 남깁니다.

단, NBS 패키지 README에도 적혀 있듯 해당 패키지는 아직 전체 코드 검토와 테스트가 충분하지 않습니다. 실제 사용 전에는 [NBS 문서](Packages/com.rumi.runios.nbs/README.md)의 주의 사항과 현재 코드를 함께 확인해야 합니다.

## 오래된 프로젝트에서 이어진 것

Runiverse OS는 짧은 기간에 새로 만든 프로젝트가 아닙니다. 아주 오래전부터 개발과 갈아엎기를 반복하며 다음 계보를 거쳤습니다.

```text
[SDJK]의 첫 버전 "System"
  -> 비공개 처리된 여러 SC KRM
  -> [SC-KRM-1.0]
  -> [SC-KRM]
  -> [RuniEngine]
  -> Runiverse OS
```

- [SDJK](https://github.com/Rumi727/SDJK)
- [SC-KRM-1.0](https://github.com/Rumi727/SC-KRM-1.0)
- [SC-KRM](https://github.com/Rumi727/SC-KRM)
- [RuniEngine](https://github.com/Rumi727/RuniEngine)

이번 재설계가 이 계보의 마지막 갈아엎기가 되도록 하는 것이 목표입니다.

## 현재 상태와 시작점

현재 저장소는 Unity 프로젝트 자체이며 패키지는 `Packages/` 아래에 포함되어 있습니다.

에디터에서 설치·패키지 설정 창을 열 때는 `RuniOS/Show Installer` 메뉴를 사용할 수 있습니다.\
개별 API를 사용하려면 위 패키지 문서와 소스의 현재 계약을 우선 확인하세요.\
버전 `0.0.0`의 개발 패키지들이므로 아직 고정된 릴리스 설치 절차나 하위 호환성 보장은 없습니다.

## 문서

- [I/O 시스템](Packages/com.rumi.runios.core/Runtime/IO/README.md)
- [리소스 시스템](Packages/com.rumi.runios.core/Runtime/Resource/README.md)
- [텍스트 시스템](Packages/com.rumi.runios.core/Runtime/Texts/README.md)
- [FMOD SoundSystem](Packages/com.rumi.runios.fmod/README.md)
- [Runiverse OS NBS](Packages/com.rumi.runios.nbs/README.md)
- [Runiverse OS Texture](Packages/com.rumi.runios.texture/README.md)
- [기존 README 원문과 개발 TODO](README-LEGACY.md)

## 라이선스

프로젝트 본체는 [Mozilla Public License 2.0](LICENSE)을 따릅니다. FMOD, FreeImage, UnityAsyncImageLoader 등 포함된 외부 구성 요소는 각각의 라이선스와 고지 문서를 따르므로 배포 전 [Third-party notices](THIRD-PARTY-NOTICES.md)와 각 구성 요소의 문서를 확인해야 합니다.
