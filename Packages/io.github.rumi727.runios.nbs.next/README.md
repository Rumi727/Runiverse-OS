# Runiverse OS NBS (Next)

Note Block Studio 곡(`.nbs`)을 Unity 에셋과 리소스팩으로 불러오고, FMOD DSP 클록에 맞춰 재생하는 패키지입니다.

> 재설계 중인 임시 패키지입니다. 기존 `io.github.rumi727.runios.nbs`를 대체할 예정이며, 교체할 때 패키지 id, asmdef 이름, 레지스트리 id(`runios:nbs_next`)만 바뀝니다.\
> 바닐라 악기 음원(`runios:block.note_block.*`)은 교체 전까지 기존 패키지의 StreamingAssets를 사용합니다.

## 불러오기

| 경로 | 방법 |
| --- | --- |
| Unity 에셋 | `.nbs` 파일을 프로젝트에 넣으면 `NoteBlockClipImporter`가 `NoteBlockClip`으로 가져옵니다. |
| 리소스팩 | `assets/{namespace}/nbses/` 아래 파일을 `NBSAssetRegistry`가 등록하고, `NBSAssetHandle`이 `NoteBlockClip.LoadAsync(IONode)`로 로드합니다. |
| 코드 | `NoteBlockClip.Create(Stream)`, `NoteBlockClip.Create(header, notes, layers, instruments)`, `NoteBlockClip.LoadAsync(IONode)` |

`NBSReader`는 Stream을 섹션 단위로 순차적으로 읽는 public 리더입니다. 헤더만 읽고 멈출 수도 있습니다.

```csharp
using NBSReader reader = new NBSReader(stream, leaveOpen: true);
NBSHeader header = reader.ReadHeader();
```

## 편집 모델과 맵

`NoteBlockClip`(ScriptableObject)은 직렬화되는 곡 데이터를 그대로 가집니다. 노트와 레이어는 서로 독립이며, 노트는 `tick`과 `layer` 좌표를 가집니다.

```csharp
clip.notes[i] = clip.notes[i] with { pitch = 100 };
clip.layers[0] = clip.layers[0] with { volume = 50 };
clip.Apply(); // Texture2D.Apply()처럼 맵을 다시 만듭니다.
```

`clip.map`(`NBSMap`)은 마지막 `Apply()` 또는 로드 시점의 불변 맵입니다. 어느 스레드에서든 잠금 없이 읽을 수 있습니다.

- `notes`: tick, 레이어 순으로 정렬되고 곡 시간이 붙은 노트. `notes[i]`, `notes[tick, layer]`, `IndexOf`, `TryGet`
- `columns`: 노트가 있는 tick 열(`tick`, `time`, `noteStart`, `noteCount`)
- `GetTickNotes(tick)`, `GetNotes(startTick, endTick, startLayer, endLayer)`, `GetLayerNotes(layer)`: 할당 없는 조회
- `tempo`(`NBSTempoMap`): tick ↔ 시간 변환, TPS·BPM, 템포 변경 목록
- `visualEffects`(`NBSVisualEffectMap`): Rainbow, 배경 강조, 색, Save Popup
- `stoppers`: Sound Stopper 목록
- Raw로 돌아가기: `NBSMapNote.sourceIndex`, `TryGetMapIndex`, `TryGetCustomInstrumentIndex`, `TryGetLayer`

맵은 직렬화하지 않고 `OnAfterDeserialize`와 `Apply()`에서 만듭니다. 정렬은 이미 정렬된 입력에서 O(n)인 삽입 정렬이고, 많이 흐트러진 입력은 introsort로 전환합니다.

## OpenNBS 충실도

기준은 OpenNBS `development` 브랜치의 로더와 재생 코드입니다.

- **로더 보정:** key는 0~87, layer volume은 0~100, 박자표는 2~8로 제한하고, 작업 통계는 음수가 되지 않게 합니다. pitch, velocity, panning은 제한하지 않습니다.
- **커스텀 악기 인덱스:** 기본 악기 20개 배치로 옮깁니다.
- **레이어 lock:** 1은 무음, 2는 솔로입니다. 솔로 레이어가 하나라도 있으면 솔로만 재생합니다.
- **기능성 악기:** 이름은 정확히 일치해야 합니다(Change Color만 대소문자 무시 포함 검사). 레이어 lock과 관계없이 적용합니다.
  - Tempo Changer: `max(0.25, floor(|pitch|) / 15)` TPS
  - Sound Stopper: 1부터 시작하는 레이어 `[floor(pitch), ((pan + 156) % 256) + ((velocity + 156) % 256) · 256]`. 시작이 0이면 전체 레이어입니다.
- **레이어 행이 없는 노트:** OpenNBS와 같이 음량 1%, 완전 왼쪽, 미세 피치 없이 재생합니다.
- **문자열:** UTF-8로 읽고, 실패하면 원본처럼 바이트를 그대로 문자로 바꿉니다(Latin-1).
- **Max loop count:** 저장만 하고 재생에는 영향을 주지 않습니다.

## 재생

```text
NoteBlockSource (시간 축, 직렬화)
  -> NBSVoiceScheduler (시간 축 없음, Voice 예약)
       -> NBSTimeline (노트 배치 계산)
       -> NBSInstrumentBank (악기 사운드)
       -> FMOD SoundChannel
```

- **시간 축은 `NoteBlockSource` 하나만 가집니다.**
  - 반복 중에는 `time`이 진행 방향을 기준으로 반복 범위 안으로 되돌아갑니다.
  - `useFileLoopSettings`이면 NBS 헤더의 반복(시작 tick부터 곡 끝까지)을 사용합니다.
- **`NBSVoiceScheduler`는 `SoundChannel`과 같은 위치의 엔진 계층입니다.**
  - 공유 `NBSPlaybackWorker`가 `workerInterval`(기본 0.1초)마다 트랜스포트의 현재 위치를 읽습니다.
  - 그때부터 `schedulingLookahead`(기본 0.2초) 안에 시작하는 소리만 DSP 클록에 예약합니다.
  - Mono 없이 쓰려면 `ISeekable`, `ITempoControl`, `IPitchControl`, `ILoopControl`, `IPausable`을 구현한 트랜스포트를 넘깁니다.
  - seek, 재생, tempo·pitch·반복 변경 같은 불연속은 트랜스포트 주인이 `Resync`, `OnRateChanged`, `OnLoopChanged`, `Pause`, `UnPause`, `Stop`으로 알립니다.
- **배치:** 소리 길이 `L`, 피치 배율 `Q`인 노트는 `D = (|tempo| / |pitch|) · L / Q` 타임라인 초를 차지합니다. tempo나 pitch가 바뀌어도 다시 계산할 것이 없습니다.
- **tempo와 pitch의 부호:** tempo 부호는 타임라인 방향, pitch 부호는 소리 방향입니다.
  - tempo < 0 재생은 pitch 부호가 반대인 tempo > 0 렌더링을 시간축으로 뒤집은 것입니다.
  - `reversePitchAlignment = attackAtNote`(기본): 네 조합 모두 어택이 노트 위치에 옵니다.
  - `startAtNote`: 샘플러처럼 노트 위치에서 소리 내용만 뒤집습니다.
- **Sound Stopper:** 정방향 렌더링 기준으로 노트 길이를 자릅니다(`D' = min(D, 다음 Stopper − S)`). 역방향에서는 잘린 지점부터 재생합니다. 반복 끝을 넘어 울리는 노트는 반복 시작 이후의 Stopper가 멈춥니다.
- **재생 의미:**
  - seek·재생 시작·늦은 로드는 현재 위치에 걸친 소리를 offset부터 복원합니다.
  - tempo를 바꿔도 재생 중인 소리는 유지하고, pitch를 바꾸면 frequency가 즉시 바뀝니다.
  - pitch 0이면 모두 멈추고, tempo 0이면 새 예약만 멈춥니다.
  - Pause는 소리 위치까지 멈춥니다.
  - 반복 경계에서 소리를 자르지 않으며, 이미 끝난 짧은 소리는 소급해서 재생하지 않습니다.
- **스트림 클립:** 스트림으로 로드된 악기는 역재생할 수 없어 pitch가 음수이면 무음입니다.
- **리로드:** 로드한 뒤 클립에 추가된 악기는 다음 `Reload()` 전까지 무음입니다.

## 에디터

- `NoteBlockClip` 인스펙터: 곡 요약과 씬 없는 미리듣기(`NBSPreviewTransport`)를 제공합니다.
- `NoteBlockSource` 인스펙터:
  - 타임라인 아래에 BPM 계단 그래프, Sound Stopper, 파일 반복 구간 레인을 그립니다. 마커에 마우스를 올리면 정보가 보이고, 클릭하면 그 위치로 이동합니다.
  - tick과 열 시간 단위, 리버스 배치 선택, 시각 효과 미리보기를 제공합니다.
