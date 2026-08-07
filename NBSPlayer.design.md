# NBSPlayer 사전 계산 맵 기반 재생 설계 명세

## 문서 목적

이 문서는 현재 `NBSPlayer`의 실시간 틱 계산, deadline 판정, late 생략, DSP 예약 커서 처리를 폐기하고, 정적인 NBS 악보를 사전 계산된 맵과 Player별 재생 스케줄로 재생하는 최종 설계를 정의한다.

구현자는 이 문서만으로 공개 API, 데이터 흐름, 시간 계산, 커서 관리, DSP 예약, 중간 재생, 정·역방향 재생, loop, Pause, reload, 속성 변경 및 검증을 결정할 수 있어야 한다.

API 호환성은 고려하지 않는다. 현재 패키지는 개발 중이므로 기존 `NBSPlayer`, `NBSPlaybackSettings` 및 내부 예약 구현은 이 설계에 맞춰 전면 교체할 수 있다.

## 설계 목표

- NBS 파일의 모든 원본 노트와 특수 이벤트 시간을 로딩 시 한 번 계산한다.
- NBS 데이터는 실제 `WaveAudioClip`, scope, FMOD channel을 소유하지 않는다.
- 다른 NBS Player 구현도 동일한 맵과 조회 API를 사용할 수 있게 공개한다.
- Worker는 tick, TPS, tempo segment, note deadline을 매번 계산하지 않는다.
- Worker는 Player별로 준비된 schedule을 순회하고 FMOD에 제출하는 역할만 담당한다.
- `Play`, `Stop`, `Pause`, `UnPause`, seek, 중간 재생, loop, 정·역방향 재생을 하나의 시간 모델로 처리한다.
- 미래 note는 DSP clock으로 예약한다.
- 현재 또는 이미 지난 note는 남아 있어야 할 PCM 위치부터 즉시 재생한다.
- `schedulingLookahead == 0`이어도 현재 occurrence를 정확히 한 번 재생한다.
- 이미 시작한 Voice는 tempo 변경에도 유지한다.
- pitch, volume, pan, 3D 및 거리 속성 변경은 기존 Voice에도 반영한다.
- loop 경계에서 재생 중인 tail을 자르지 않는다.
- reload가 늦게 끝나도 현재 위치에서 살아 있어야 할 tail을 복원한다.

## 비목표

- 전체 NBS를 하나의 PCM 파일로 렌더링하지 않는다.
- 맵이 `WaveAudioClip`이나 FMOD 자원을 소유하지 않는다.
- tempo/pitch 변경 이력을 자동화 곡선으로 저장하지 않는다.
- 과거에 실제로 출력되지 못했고 현재 시점에는 이미 끝난 짧은 음을 소급해 들려주지 않는다.
- `NBSPlayer.length`를 모든 instrument tail까지 포함한 길이로 바꾸지 않는다.
- loop에서 tail을 오디오 파일 경계처럼 강제로 절단하지 않는다.

## 현재 구현에서 제거할 책임

현재 `NBSPlayer`가 Worker 순회마다 담당하는 다음 책임을 제거한다.

- `NBSTick.tick`을 `NBSTempoMap.TickToTime`으로 반복 변환
- `wallDelay`와 late deadline을 note마다 계산
- `lateTolerance`보다 오래된 tick column 생략
- `nextTickIndex`를 tick 배열에 직접 연결
- `completedFileLoops`와 `scheduledFileLoops`를 별도 예약 cursor로 동기화
- 즉시 재생과 미래 DSP 예약을 서로 다른 경로로 처리
- Sound Stopper용 `(tick, layer)` dictionary를 Player마다 재생 로직에서 조회
- scheduling revision마다 raw tick cursor 재검색

다음 기능은 유지하되 새 구조에 맞춰 다시 구현한다.

- 공유 백그라운드 Worker
- FMOD DSP clock 기반 미래 시작/종료
- Player별 Voice 추적과 `onStop` 정리
- spatial snapshot의 main-thread 갱신
- NBS 및 instrument scope reload
- file loop와 수동 loop
- visual effect map과 Editor preview

## 전체 구조

```text
NBSReader
  └─ NBSFile
      ├─ raw header/ticks/layers/custom instruments/events
      ├─ NBSTempoMap
      ├─ NBSVisualEffectMap
      ├─ NBSNoteMap
      └─ NBSPlaybackMap

NBSPlayer가 NBS scope 로드
  └─ NBSInstrumentBank
      ├─ instrument identifier별 AssetScope<WaveAudioClip>
      ├─ clip resolver
      └─ clip metadata provider

NBSPlaybackMap + clip metadata + tempo/pitch
  └─ NBSPlaybackSchedule
      ├─ 계산된 원본 시작/끝/원래 발생 시점/실제 Voice anchor
      ├─ anchor별 NBSPlaybackMoment
      └─ 중간 재생용 interval index

NBSPlayer
  ├─ NBSPlaybackCursor
  ├─ pending submissions
  ├─ active/pending Voices
  └─ shared NBSPlaybackWorker
```

## 핵심 개념 분리

### `NBSNoteMap`

NBS 파일만으로 계산할 수 있는 원본 악보 시간 맵이다. 불변이며 `NBSFile`이 소유한다.

### `NBSPlaybackMap`

원본 note를 실제 재생에 편리한 정적 속성으로 가공한 불변 맵이다. clip 식별 정보는 포함하지만 실제 clip과 clip 길이는 포함하지 않는다.

### `NBSPlaybackSchedule`

Player가 보유한 clip 길이 metadata와 현재 tempo/pitch를 결합해 생성하는 경량 계산 결과다. 실제 clip 참조는 포함하지 않는다. note의 원본 시작/끝, 원래 발생 시점, 실제 Voice 시작 anchor, playback interval 및 중간 재생 offset 계산에 필요한 값을 확정한다.

### `NBSPlaybackCursor`

한 Player가 현재 schedule에서 FMOD에 아직 제출하지 않은 첫 occurrence를 가리키는 mutable 상태다.

### Pending submission

cursor는 이미 미래까지 전진할 수 있으므로, 아직 DSP clock에 도달하지 않은 제출 moment를 별도로 추적한다. tempo, lookahead, Pause 또는 reload로 예약을 취소할 때 cursor를 되감을 근거가 된다.

### Voice

실제로 생성된 `SoundChannel`과 note occurrence를 연결한다. 아직 미래 DSP start를 기다리는 Voice와 이미 시작한 Voice를 모두 포함한다.

### `NBSPlaybackQueryContext`

이번 schedule 조회에 필요한 현재 위치, tempo, pitch, lookahead, loop 상태를 담는 불변 입력이다. 재생 커서를 대신하지 않는다.

## NBS 원본 데이터 모델

기존 raw 모델은 유지한다.

- `NBSHeader`
- `NBSNote`
- `NBSLayer`
- `NBSCustomInstrument`
- `NBSTick`
- `NBSSpecialEvent`
- `NBSFile.ticks`
- `NBSFile.layers`
- `NBSFile.customInstruments`
- `NBSFile.specialEvents`

`NBSFile`에 다음 공개 속성을 추가한다.

```csharp
public NBSNoteMap noteMap { get; }
public NBSPlaybackMap playbackMap { get; }
```

`NBSReader`가 raw 데이터 검증을 끝낸 뒤 `NBSTempoMap`, `NBSNoteMap`, `NBSPlaybackMap`, `NBSVisualEffectMap`을 생성한다.

## `NBSNoteMap`

### 역할

- 모든 raw note의 원본 tick과 절대 초를 연결한다.
- 모든 특수 이벤트의 원본 tick과 절대 초를 연결한다.
- raw tick column이 없어도 시간 기준 이진 검색을 제공한다.
- tempo changer는 `NBSTempoMap`에 먼저 반영된 최종 절대 시간을 사용한다.

### 권장 공개 모델

```csharp
public sealed class NBSNoteMap
{
    public IReadOnlyList<NBSMappedNote> notes { get; }
    public IReadOnlyList<NBSMappedSpecialEvent> specialEvents { get; }
}

public readonly record struct NBSMappedNote
(
    int id,
    double time,
    NBSNote note
);

public readonly record struct NBSMappedSpecialEvent
(
    int id,
    double time,
    NBSSpecialEvent specialEvent
);
```

### 정렬

- note: `(time, layer, id)` 오름차순
- special event: `(time, layer, id)` 오름차순
- `id`는 해당 맵 안에서 안정적이며 파일 수명 동안 바뀌지 않는다.

### 조회

다음 의미의 allocation-free index/range 조회를 제공한다.

- 첫 `time >= value`
- 첫 `time > value`
- 마지막 `time <= value`
- 마지막 `time < value`
- 두 시간 사이 note range
- 두 시간 사이 special event range

구체적인 반환형은 start/end index를 가진 readonly range struct로 통일한다. Worker hot path에서 LINQ와 iterator allocation을 사용하지 않는다.

## `NBSInstrumentReference`

맵은 `WaveAudioClip` 대신 악기 식별 정보만 저장한다.

```csharp
public readonly record struct NBSInstrumentReference
{
    public bool isFunctional { get; }
    public bool usesSongNamespace { get; }
    public Identifier fixedAssetId { get; }
    public string relativePath { get; }

    public ResourceKey Resolve(Identifier nbsAssetId);
}
```

### Vanilla instrument

- `usesSongNamespace == false`
- `fixedAssetId`에 현재 `runios:block.note_block.*` identifier 저장
- registry는 `runios:waves`

### Custom instrument

- `usesSongNamespace == true`
- `relativePath`에 정규화된 NBS `soundFile` 경로 저장
- 실제 asset namespace는 `NBSPlayer.nbsFileRef.key.assetId.nameSpace`
- registry는 `runios:waves`

### Functional instrument

- tempo changer, Sound Stopper 및 visual functional instrument는 `isFunctional == true`
- clip을 요청하지 않는다.
- audio note command로 생성하지 않는다.

현재 `NBSInstrumentBank.TryNormalizeCustomPath`의 경로 정규화 책임은 NBS 재생 데이터 생성 쪽으로 옮긴다. 실제 scope 로드는 계속 Player 소유 bank가 담당한다.

## `NBSPlaybackMap`

### 역할

- raw note와 layer 정보를 재생 친화적인 정적 entry로 결합한다.
- note별 정적 pitch 배율을 미리 계산한다.
- note/layer volume과 pan을 미리 결합한다.
- 악기 reference를 연결한다.
- Sound Stopper와 기타 재생 이벤트를 원본 순서로 보존한다.
- 실제 clip 길이, Player tempo, Player pitch 없이 계산 가능한 값까지만 저장한다.

### 정적 pitch 배율

현재 NBS pitch 계산을 그대로 사용한다.

```text
semitones = (note.key + instrumentKeyOffset - 45) + note.pitch / 100
staticPitchRatio = 2 ^ (semitones / 12)
```

- vanilla `instrumentKeyOffset`은 0
- custom `instrumentKeyOffset`은 `custom.key - 45`
- `staticPitchRatio`는 항상 양수여야 한다.
- 유한하지 않거나 0 이하가 되는 note는 invalid playback entry로 표시하고 Voice를 만들지 않는다.

### 정적 volume

```text
noteVolume = note.velocity / 100
layerVolume = layer.volume / 100
staticVolume = noteVolume * layerVolume
```

Player volume은 Voice 적용 시 곱한다.

### 정적 pan

```text
if layer.panning == 100:
    combinedNbsPan = note.panning
else:
    combinedNbsPan = (layer.panning + note.panning) * 0.5

staticPan = (combinedNbsPan - 100) / 100
```

Player `panStereo`는 Voice 적용 시 현재 규칙대로 합성한다.

### 권장 공개 모델

```csharp
public sealed class NBSPlaybackMap
{
    public IReadOnlyList<NBSPlaybackEntry> entries { get; }

    public NBSPlaybackSchedule CreateSchedule
    (
        float tempo,
        float pitch,
        INBSClipMetadataProvider clipMetadata
    );
}

public readonly record struct NBSPlaybackEntry
(
    int id,
    double originalTime,
    int layer,
    NBSPlaybackEntryKind kind,
    NBSInstrumentReference instrument,
    double staticPitchRatio,
    float staticVolume,
    float staticPan,
    NBSNote note,
    NBSSpecialEvent specialEvent
);
```

`NBSPlaybackEntryKind`는 최소 다음 값을 갖는다.

- `note`
- `soundStop`

Tempo changer는 이미 `NBSTempoMap`에 반영된다. visual event는 `NBSVisualEffectMap`이 계속 담당하므로 audio schedule entry로 중복 생성하지 않는다.

## Clip metadata와 실제 자원 수명

### Metadata provider

```csharp
public interface INBSClipMetadataProvider
{
    bool TryGetLength
    (
        NBSInstrumentReference instrument,
        out double length
    );
}
```

필요하면 sample count와 frequency를 함께 제공할 수 있지만, schedule의 시간 계산 계약은 clip 원본 길이 초를 기준으로 한다.

### Resolver

실제 Player는 metadata 외에 clip resolver를 보유한다.

```csharp
internal interface INBSClipResolver : INBSClipMetadataProvider
{
    bool TryGetClip
    (
        NBSInstrumentReference instrument,
        out WaveAudioClip clip
    );
}
```

### `NBSInstrumentBank`

- `NBSPlaybackMap`에서 실제 사용되는 unique instrument reference만 로드한다.
- 현재와 같이 `IAssetScope<WaveAudioClip>`을 보유한다.
- scope와 clip 수명은 `NBSPlayer`가 bank를 통해 관리한다.
- `NBSPlaybackMap`과 `NBSPlaybackSchedule`에는 clip reference를 넣지 않는다.
- reload 교체 시 새 bank와 schedule을 먼저 준비한 뒤 lock 안에서 기존 세대와 교체한다.
- 기존 bank는 Voice 정리 후 `DisposeQueue`로 넘긴다.

## Tempo와 pitch 부호의 최종 의미

역재생 모드 enum은 만들지 않는다.

- tempo 부호는 NBS timeline 진행 방향을 결정한다.
- pitch 부호는 clip PCM 진행 방향을 결정한다.
- tempo와 pitch 부호는 독립이다.

```text
tempo > 0 : timeline 정방향
tempo < 0 : timeline 역방향
pitch > 0 : clip 시작에서 끝으로
pitch < 0 : clip 끝에서 시작으로
```

부호 조합:

| tempo | pitch | timeline | PCM |
|---|---|---|---|
| 양수 | 양수 | 정방향 | 정방향 |
| 양수 | 음수 | 정방향 | 역방향 |
| 음수 | 양수 | 역방향 | 정방향 |
| 음수 | 음수 | 역방향 | 역방향 |

`tempo < 0 && pitch < 0`이 전체 시간 기준 완전 역재생이다.

FMOD는 `CREATESAMPLE` sound에 음수 frequency를 지정해 역재생할 수 있다. NBS instrument asset은 현재 `CreateSoundAsync`를 통해 sample sound로 준비하므로 이 조건을 만족한다.

참고:

- [FMOD Channel::setFrequency](https://www.fmod.com/docs/2.03/api/core-api-channel.html)
- [FMOD ChannelControl::setDelay](https://www.fmod.com/docs/2.03/api/core-api-channelcontrol.html)

## 원본 시작점과 끝점

각 note에 다음 기호를 사용한다.

```text
S = NBSTempoMap으로 계산한 원본 note 시작 시간
L = clip 원본 길이
Q = note의 staticPitchRatio
T = player tempo
P = player pitch
```

PCM source 진행 속도:

```text
sourceRate = Q * abs(P)
```

wall-clock 재생 길이:

```text
wallDuration = L / sourceRate
```

해당 wall-clock 동안 transport가 이동하는 NBS timeline 길이:

```text
timelineDuration = wallDuration * abs(T)
                 = L * abs(T) / (Q * abs(P))
```

원본 끝점:

```text
E = S + timelineDuration
```

`[S, E]`는 pitch 방향 보정 전 원본 note가 차지하는 NBS timeline 구간이다.

## 원래 발생 시점과 실제 Voice 시작 시점

`*`는 note가 원래 발생해야 하는 시간이다. tempo 방향은 `*`가 어느 끝인지 결정한다.

```text
T > 0 : originalAnchor(*) = S
T < 0 : originalAnchor(*) = E
```

`^`는 실제 Voice가 시작하는 시간이다. tempo 부호는 숫자 타임라인에서 offset 방향을 바꾸지 않는다. pitch가 음수일 때만 `timelineDuration`만큼 `^`를 숫자 타임라인 기준으로 앞당긴다.

```text
P > 0 : actualAnchor(^) = originalAnchor
P < 0 : actualAnchor(^) = originalAnchor - timelineDuration
```

실제 interval은 tempo 진행 방향에 맞춰 `^`에서 `timelineDuration`만큼 차지한다.

```text
T > 0 : [actualAnchor, actualAnchor + timelineDuration]
T < 0 : [actualAnchor - timelineDuration, actualAnchor]
```

pitch 방향은 clip의 어느 PCM 끝에서 시작하는지를 결정한다.

```text
P > 0 : sourceStart = 0
P < 0 : sourceStart = clip의 마지막 유효 sample
```

tempo가 음수인 경우 `*`는 `E`에 있지만, pitch가 음수이면 `^`는 `E - timelineDuration = S`가 된다. 따라서 tempo 음수는 offset을 반전시키지 않고, pitch 음수만 실제 Voice 시작 시점을 이동시킨다.

## 네 부호 조합 예제

```text
S = 1.0
L = 0.5
Q = 1
abs(T) = 1
abs(P) = 1
E = 1.5
```

| tempo | pitch | `*` 원래 발생 | `^` 실제 시작 | PCM 시작점 | timeline 종료점 |
|---|---|---:|---:|---|---:|
| 양수 | 양수 | 1.0 | 1.0 | clip 시작 | 1.5 |
| 양수 | 음수 | 1.0 | 0.5 | clip 끝 | 0.0 |
| 음수 | 양수 | 1.5 | 1.5 | clip 시작 | 1.0 |
| 음수 | 음수 | 1.5 | 1.0 | clip 끝 | 0.5 |

`T > 0, P < 0`에서는 clip이 0.5에서 clip 끝부터 시작해 1.0(`*`)에서 clip 시작에 도달한다.

`T < 0, P > 0`에서는 1.5(`* = ^`)에서 clip 시작부터 재생하지만 timeline은 뒤로 진행해 1.0에서 clip 끝에 도달한다.

`T < 0, P < 0`에서는 1.0(`^`)에서 clip 끝부터 재생하고 timeline은 뒤로 진행해 0.5에서 clip 시작에 도달한다. 원래 발생 시점 `*`는 1.5다.

## 중간 위치 source offset

현재 transport 위치를 `X`라고 한다.

실제 Voice 시작점 `^`부터 진행한 timeline 거리:

```text
T > 0 : progress = X - actualAnchor
T < 0 : progress = actualAnchor - X
```

경과 wall-clock 시간:

```text
elapsedWall = progress / abs(T)
```

PCM source 이동량:

```text
sourceTravel = elapsedWall * Q * abs(P)
```

최종 source offset:

```text
P > 0 : sourceOffset = sourceTravel
P < 0 : sourceOffset = L - sourceTravel
```

실제 playback interval 활성 조건:

```text
T > 0 : actualAnchor <= X < actualAnchor + timelineDuration
T < 0 : actualAnchor - timelineDuration < X <= actualAnchor
```

실제 PCM 설정 시 다음을 적용한다.

- 정방향 시작은 sample 0
- 역방향 시작은 `clip.samples - 1`
- 계산 offset은 `[0, clip.samples - 1]`로 제한
- 끝점과 정확히 같은 위치에서 남은 sample이 없으면 Voice를 만들지 않아도 된다.
- 부동소수점 경계 오차는 sample 단위 환산 뒤 처리한다.

## `NBSPlaybackSchedule`

### 필요한 이유

`E`와 anchor는 clip 길이, tempo 크기, tempo 부호, pitch 크기에 따라 달라진다. 따라서 모든 최종 발생 시간을 `NBSFile`에 영구 저장할 수 없다.

대신 tempo/pitch 또는 clip metadata가 바뀔 때만 Player별 schedule을 한 번 생성한다. Worker 순회 중에는 note 시간을 다시 계산하지 않는다.

### 생성 조건

schedule은 다음 값의 조합에 종속된다.

- `NBSPlaybackMap` 세대
- instrument metadata 세대
- `abs(tempo)`
- tempo 부호
- `abs(pitch)`
- pitch 부호

다음 변경 시 새 schedule을 생성한다.

- NBS 파일 reload
- instrument bank reload
- tempo 크기 변경
- tempo 부호 변경
- pitch 크기 변경
- pitch 부호 변경
- pitch 0에서 음수 또는 양수로 복구

volume, pan, spatial blend, doppler, spread, 거리, rolloff 변경은 schedule을 다시 만들지 않는다.

### 준비된 note

```csharp
public readonly record struct NBSPreparedNote
(
    int mapEntryId,
    double originalStartTime,
    double originalEndTime,
    double anchorTime,
    double timelineDuration,
    double sourceLength,
    bool reverseSource,
    int layer,
    NBSInstrumentReference instrument,
    double staticPitchRatio,
    float staticVolume,
    float staticPan
);
```

### 준비된 Sound Stopper

Sound Stopper는 길이가 없는 timeline 이벤트다.

- anchor는 원본 event time
- `startLayer`, `endLayer` 보존
- loop occurrence마다 다른 occurrence ID 사용
- snapshot 계산 시 실제 Voice anchor에서 현재 위치까지 진행 방향으로 Stopper를 통과했는지 판정

### Moment

```csharp
public readonly record struct NBSPlaybackMoment
(
    double anchorTime,
    IReadOnlyList<NBSPreparedEntry> entries
);
```

- 같은 계산 anchor를 가진 entry를 하나로 묶는다.
- moment는 anchor 오름차순으로 저장한다.
- 역방향 cursor는 moment 배열을 역순으로 순회한다.
- 같은 moment 내부 entry는 원본 map 순서를 유지한다.
- note와 Sound Stopper가 같은 anchor에서 충돌하면 먼저 처리된 entry만 후속 Stopper의 영향을 받는다.
- 모든 entry는 같은 moment에서 동일한 DSP start clock을 공유한다.

### Snapshot interval index

schedule은 `includePreviousNotes` 조회를 위해 실제 playback interval `[playbackStart, playbackEnd]` 검색 index를 함께 만든다.

요구사항:

- `playbackStart <= X < playbackEnd`인 note 후보를 전체 note 선형 순회 없이 찾는다.
- 정방향과 역방향 모두 같은 interval을 사용한다.
- source offset은 조회 시 위 수식으로 계산한다.
- Sound Stopper 경계와 loop occurrence를 적용한 뒤 최종 결과를 만든다.
- 구현은 interval tree 또는 start/end 정렬 배열을 사용할 수 있지만 공개 동작은 동일해야 한다.

## Schedule generation

schedule을 다시 만들 때마다 generation을 증가시킨다.

```text
occurrence ID =
(scheduleGeneration, loopIteration, momentIndex, entryIndex)
```

generation이 필요한 이유:

- tempo 방향을 바꾼 뒤 같은 raw note를 반대 방향으로 다시 통과할 수 있다.
- pitch/tempo 변경 전 활성 tail과 새 schedule occurrence가 겹칠 수 있다.
- 같은 generation의 Worker 재조회는 중복이어야 하지만, 새 generation의 합법적인 재통과는 새 Voice여야 한다.

## Player별 재생 상태

새 `NBSPlayer`는 최소 다음 상태를 갖는다.

```csharp
NBSPlaybackSchedule? playbackSchedule;
NBSPlaybackCursor playbackCursor;
List<PendingSubmission> pendingSubmissions;
List<Voice> voices;
long scheduleGeneration;
long completedLoops;
```

### `NBSPlaybackCursor`

```csharp
public struct NBSPlaybackCursor
{
    public int momentIndex;
    public long loopIteration;
    public long scheduleGeneration;
    public NBSPlaybackDirection direction;
    public bool initialized;
}
```

cursor 의미:

- 현재 schedule에서 FMOD에 아직 제출하지 않은 첫 moment
- 실제로 audible하게 시작한 마지막 note가 아님
- 미래 DSP 예약을 제출하면 audible 시작 전이어도 다음 moment로 전진
- cursor가 배열 끝을 벗어나면 해당 방향의 다음 loop occurrence 또는 schedule 종료를 뜻함

### `PendingSubmission`

```csharp
readonly record struct PendingSubmission
(
    NBSOccurrenceId occurrence,
    ulong targetDspClock,
    NBSPlaybackCursor cursorBeforeMoment
);
```

note가 하나도 없고 Sound Stopper만 있는 future moment도 pending submission으로 기록한다. 그래야 예약 취소 시 cursor가 Stopper 이전까지 정확히 되돌아간다.

target DSP clock이 현재 parent clock 이하가 되면 pending submission에서 제거한다. 해당 moment는 이미 timeline상 시작됐거나 지난 것으로 본다.

### `Voice`

```csharp
sealed class Voice
{
    public SoundChannel channel;
    public NBSOccurrenceId occurrence;
    public int layer;
    public NBSPreparedNote preparedNote;
    public ulong startDspClock;
    public bool isPendingStart;
}
```

Voice는 동적 속성 재적용을 위해 prepared note의 정적 volume, pan, pitch ratio 및 instrument 정보를 보존한다.

## Cursor 초기화

### 정방향

```text
includeCurrent == true  : anchor >= current position인 첫 moment
includeCurrent == false : anchor > current position인 첫 moment
```

### 역방향

```text
includeCurrent == true  : anchor <= current position인 마지막 moment
includeCurrent == false : anchor < current position인 마지막 moment
```

이진 검색으로 찾는다.

### `includeCurrent == true` 사용 위치

- `Play(startTime)`
- 명시적 `time` 변경
- 명시적 `tick` 변경
- 명시적 `index` 변경
- NBS/instrument load 완료 후 전체 snapshot 복원
- pitch 0에서 복구

### `includeCurrent == false` 사용 위치

- tempo/pitch 변경으로 미래 schedule만 교체
- lookahead 변경
- Pause 후 미래 예약 재구성
- 현재 occurrence를 이미 처리한 상태에서 같은 위치를 재기준화

명시적 seek에서는 모든 Voice를 비우므로 현재 anchor를 다시 포함한다. 속성 변경에서는 기존 활성 Voice를 유지하므로 현재 anchor 중복 생성을 막는다.

## `NBSPlaybackQueryContext`

```csharp
public readonly record struct NBSPlaybackQueryContext
(
    NBSPlaybackPosition currentPosition,
    double schedulingLookahead,
    float tempo,
    float pitch,
    NBSLoopInfo loopInfo
);
```

`NBSPlaybackPosition`은 최소 다음을 포함한다.

```csharp
public readonly record struct NBSPlaybackPosition
(
    double fileTime,
    long loopIteration
);
```

공개 `NBSPlayer.time`은 loop가 적용된 file time을 나타낸다. occurrence 거리 계산은 `loopIteration`을 결합한 unwrapped timeline 위치를 사용한다.

## Schedule 조회 API

```csharp
public void Query
(
    NBSPlaybackCursor cursor,
    in NBSPlaybackQueryContext context,
    bool includePreviousNotes,
    List<NBSPlaybackCommand> output,
    out NBSPlaybackCursor nextCursor
);
```

### 입력 cursor를 직접 변경하지 않는 이유

- Player가 command 처리를 끝낸 뒤 cursor를 commit할 수 있다.
- batch 처리 전 치명적 예외가 발생하면 기존 cursor를 유지할 수 있다.
- 일부 Voice가 이미 생성된 상태에서 재조회돼도 occurrence ID로 중복을 제거할 수 있다.

### 출력 command

```csharp
public readonly record struct NBSPlaybackCommand
(
    NBSOccurrenceId occurrence,
    NBSPlaybackCommandKind kind,
    double wallDelay,
    double sourceOffset,
    NBSPreparedNote note,
    int stopStartLayer,
    int stopEndLayer
);
```

`NBSPlaybackCommandKind`:

- `note`
- `soundStop`

맵과 schedule은 DSP clock을 모른다. Player가 `wallDelay`를 현재 SoundSystem output sample rate와 parent DSP clock으로 변환한다.

## Lookahead 계산

`schedulingLookahead`는 wall-clock 초다. schedule anchor는 NBS timeline 초다.

현재 occurrence anchor까지의 wall delay는 loop 이동 거리를 반영한 뒤 다음 식으로 계산한다.

```text
wallDelay = signedTimelineDistance / abs(tempo)
```

loop가 없는 같은 구간에서는 다음과 동치다.

```text
wallDelay = (anchor - currentTime) / tempo
```

tempo 부호가 음수이면 미래 anchor와 현재 time의 차이도 음수이므로 결과 wall delay는 양수가 된다.

```text
wallDelay < 0  : 이미 지났지만 cursor에 남은 moment
wallDelay == 0 : 현재 moment
wallDelay > 0  : 미래 moment
```

조회 조건:

```text
wallDelay <= schedulingLookahead
```

## `schedulingLookahead == 0`

lookahead가 0이면 조건은 다음과 같다.

```text
wallDelay <= 0
```

따라서:

- 현재 anchor는 처리한다.
- Worker가 늦어 이미 지난 cursor moment도 처리한다.
- 미래 anchor는 처리하지 않는다.
- 처리한 moment 다음으로 cursor가 전진하므로 같은 현재 index를 중복 재생하지 않는다.

예시:

```text
schedule moments = 1.0, 1.5, 2.0
Play(1.0)
tempo = 1
lookahead = 0
```

첫 Worker:

```text
current = 1.0
cursor = 1.0
wallDelay = 0
1.0 moment 처리
cursor = 1.5
```

다음 Worker:

```text
current = 1.1
cursor = 1.5
wallDelay = 0.4
미래이므로 처리하지 않음
```

1.5를 조금 지난 Worker:

```text
current = 1.503
cursor = 1.5
wallDelay = -0.003
3ms 경과 offset으로 즉시 재생
cursor = 2.0
```

## 양수 lookahead

예시:

```text
current = 1.1
tempo = 1
lookahead = 0.5
cursor = 1.5
```

1.5 moment의 wall delay는 0.4이므로 미래 DSP 예약한다. cursor는 즉시 다음 moment로 이동한다. 실제 audible 시작 전이라도 이미 FMOD에 제출했으므로 다음 Worker에서 다시 반환하지 않는다.

제출된 future moment는 `PendingSubmission`에 기록한다.

## Worker가 늦었을 때

cursor가 현재 위치보다 뒤처졌다면 cursor부터 현재까지의 moment를 소비한다.

각 note command:

- 현재 playback interval 안에 있으면 source offset을 계산해 즉시 재생한다.
- Sound Stopper를 이미 통과했다면 생성하지 않는다.
- natural end를 이미 통과했다면 Voice를 만들지 않고 cursor만 전진한다.
- clip을 찾을 수 없거나 velocity가 0이면 Voice를 만들지 않고 cursor만 전진한다.
- future note라면 source start offset을 사용해 DSP 예약한다.

`lateTolerance`에 따라 tick 전체를 버리지 않는다.

현재 시점에 이미 완전히 끝난 음은 소급 재생할 수 없으므로 생성하지 않는다. 이 동작은 late 생략 정책이 아니라 현재 audible contribution이 0인 결과다.

## `includePreviousNotes`

### `false`

- cursor부터 lookahead 경계까지 처리한다.
- cursor에 남은 past moment는 offset 보정한다.
- cursor보다 앞서 이미 소비된 note는 다시 검사하지 않는다.

### `true`

- `false`의 결과를 포함한다.
- interval index에서 현재 위치를 포함하는 과거 note occurrence를 추가 검색한다.
- source offset을 계산한다.
- Sound Stopper를 이미 통과한 note를 제외한다.
- 이전 loop iteration에서 현재 iteration까지 넘어온 tail도 포함한다.

### 계약

`includePreviousNotes == true`는 호출자가 기존 Voice를 모두 제거한 완전 재동기화에서 사용한다. occurrence ID 중복 검사는 안전망으로 유지하지만, 일반 Worker에서 반복 호출하지 않는다.

### 사용 위치

- `Play(startTime)`
- seek
- 늦게 끝난 NBS/instrument load
- reload generation 교체
- pitch 0에서 음수/양수로 복구
- Stop 이후 새 Play

## 단일 Voice 생성 경로

과거, 현재, 미래 note를 서로 다른 메소드로 분리하지 않는다.

모든 note command는 다음 순서를 사용한다.

1. instrument reference로 Player 소유 clip을 조회한다.
2. `PlaySound(clip, paused: true)`로 channel을 만든다.
3. frequency를 적용한다.
4. volume과 pan을 적용한다.
5. spatial blend, doppler, spread, min/max distance, rolloff, spatial state를 적용한다.
6. `channel.time = sourceOffset`을 설정한다.
7. command wall delay를 target DSP clock으로 변환한다.
8. `channel.SetDelay(max(targetClock, currentParentClock), endClock)`를 호출한다.
9. occurrence ID와 함께 Voice를 등록한다.
10. `channel.UnPause()`를 호출한다.

미래 note:

- target DSP clock이 미래
- `SetDelay`가 audible start를 막음
- source offset은 PCM 진행 방향의 시작점

현재 또는 과거 note:

- target clock을 현재 parent clock 이상으로 제한
- delay 없이 시작
- source offset은 이미 진행한 PCM 위치

FMOD의 past `setDelay`는 zero delay로 시작하지만 PCM 위치를 자동 보정하거나 manual pause flag를 해제하지 않는다. 따라서 `channel.time`과 `UnPause()`는 항상 필요하다.

참고:

- [FMOD 과거 DSP clock과 setPosition 답변](https://qa.fmod.com/t/getlength-setposition-vs-getdspclock-values-sample-rate-dependent/20478/4)

## Frequency 계산

```text
frequencyMagnitude = clip.frequency * staticPitchRatio * abs(playerPitch)
frequencySign = sign(playerPitch)
frequency = frequencyMagnitude * frequencySign
```

tempo 부호는 channel frequency 부호에 관여하지 않는다.

pitch가 음수면 source offset을 clip 끝 기준으로 계산하고 frequency도 음수로 지정한다.

## Sound Stopper 처리

### 미래 Stopper

- Stopper command의 target DSP clock을 계산한다.
- 현재 등록된 대상 layer Voice를 찾는다.
- 해당 Voice의 start clock과 Stopper clock으로 `SetDelay(startClock, stopClock)`을 설정한다.
- Stopper가 future-only moment여도 pending submission으로 기록한다.

### 현재 또는 past Stopper

- 대상 layer의 현재 Voice를 즉시 중지한다.
- past snapshot 생성 과정에서는 이미 해당 Stopper를 통과한 note를 애초에 반환하지 않는다.

### 같은 anchor 순서

- prepared moment의 원본 entry 순서를 따른다.
- Stopper 전에 등록된 Voice만 영향받는다.
- Stopper 뒤 entry로 생성되는 같은-anchor Voice는 영향받지 않는다.

### Pause와 Stopper end delay

Pause 또는 timing schedule 재생성 시 활성 Voice에 남은 미래 end delay를 해제한다. parent DSP clock은 channel pause와 독립적으로 진행할 수 있으므로, 이전 Stopper 예약을 그대로 두면 Pause 중에 Voice가 종료될 수 있다.

UnPause 후 새 schedule의 future Stopper를 다시 제출한다.

## Pending submission과 예약 취소

cursor만으로는 이미 미래까지 예약한 범위를 되돌릴 수 없다. `pendingSubmissions`가 필수다.

### 제출 시

- future moment를 처리하면 `PendingSubmission`을 추가한다.
- note가 없어도 moment 자체를 기록한다.
- cursor는 다음 moment로 전진한다.

### 만료 정리

- target clock이 current parent clock 이하가 되면 submission을 제거한다.
- Voice의 `isPendingStart`도 current clock과 start clock 비교로 해제한다.

### 취소 시

1. 아직 future인 pending submission을 찾는다.
2. 가장 이른 occurrence의 `cursorBeforeMoment`를 구한다.
3. 해당 submission 이후의 future Voice를 중지한다.
4. 활성 Voice의 이전 future stop delay를 해제한다.
5. future submission을 제거한다.
6. playback cursor를 가장 이른 `cursorBeforeMoment`까지 되감는다.
7. 새 조건으로 Query한다.

cursor를 앞으로 이동시키지 않는다. 아직 처리하지 않은 현재 moment가 있다면 그대로 보존한다.

### 취소 원인

- Pause
- tempo 변경
- pitch 변경
- scheduling lookahead 변경
- loop 설정 변경
- file loop 사용 여부 변경
- schedule generation 교체
- reload
- explicit seek

seek와 reload는 모든 Voice를 정리하고 새 cursor를 만들므로 부분 되감기 대신 전체 재기준화를 사용한다.

## Play

`Play(startTime)`:

1. 기존 Voice와 pending submission을 정리한다.
2. transport를 정확히 `startTime`으로 설정한다.
3. loop iteration을 0으로 초기화한다.
4. 현재 NBS와 instrument metadata가 준비됐다면 schedule을 준비한다.
5. `includeCurrent: true` cursor를 만든다.
6. `includePreviousNotes: true` Query를 실행한다.
7. 현재 살아 있어야 할 tail을 offset부터 생성한다.
8. 현재 occurrence와 lookahead 범위의 future occurrence를 제출한다.

리소스가 준비되지 않았어도 transport는 요청한 위치에서 계속 흐른다. 리소스 준비 완료 시 그때의 현재 위치로 snapshot을 복원한다.

## Stop

`Stop()`:

- 모든 active/pending Voice 중지
- 모든 pending submission 제거
- schedule은 보존 가능하지만 cursor는 초기화
- loop iteration 0
- time 0
- instrument scope는 component가 활성인 동안 유지

## Pause

`Pause()`:

1. transport time을 고정한다.
2. future pending submissions을 취소한다.
3. future Voice를 제거한다.
4. active Voice의 future end delay를 해제한다.
5. active Voice 전부 `Pause()`한다.
6. active PCM 위치를 유지한다.

Pause 중에는 Worker가 새 occurrence를 제출하지 않는다.

## UnPause

`UnPause()`:

1. active Voice를 현재 PCM 위치에서 `UnPause()`한다.
2. transport timestamp를 재기준화한다.
3. 취소로 되감긴 cursor에서 future schedule 조회를 재개한다.
4. active Voice에 영향을 줄 future Stopper를 다시 예약한다.

Pause 중 time이 변경됐다면 기존 active Voice를 유지하지 않고 seek 전체 재동기화 경로를 사용한다.

## Seek

명시적 `time`, `tick`, `index` 변경:

1. 모든 Voice 중지
2. pending submission 제거
3. target time 적용
4. completed loop와 loop iteration 초기화
5. 현재 tempo/pitch로 schedule 확인 또는 재생성
6. `includeCurrent: true` cursor 생성
7. 재생 중이라면 `includePreviousNotes: true` Query
8. active tail 복원
9. current/future occurrence 제출

재생 중이 아니라면 cursor만 준비하고 Voice를 만들지 않는다. 다음 Play가 snapshot을 만든다.

## 늦은 resource load와 reload

NBS 또는 instrument bank가 Play 이후 준비되면:

- transport를 되감지 않는다.
- 로딩 중 지난 원본 start를 무조건 버리지 않는다.
- 현재 위치에서 살아 있어야 할 note를 schedule snapshot으로 찾는다.
- 각 note를 정확한 source offset부터 생성한다.
- future range를 DSP 예약한다.

reload generation 교체:

1. 새 NBS scope 로드
2. 새 playback map 기반 새 instrument bank 로드
3. 새 schedule 생성 준비
4. Player write lock
5. 기존 Voice와 submissions 정리
6. scope, bank, maps, schedule generation 교체
7. 현재 transport 위치 snapshot 복원
8. lock 해제
9. 기존 scope와 bank를 `DisposeQueue`에 등록

## Tempo 변경

tempo 변경 시:

- 현재 transport time을 먼저 동기화한다.
- 기존 active Voice는 유지한다.
- future Voice와 pending submissions을 취소한다.
- active Voice의 future Stopper end delay를 해제한다.
- 새 tempo 크기와 부호로 schedule을 다시 만든다.
- schedule generation 증가
- 현재 위치를 제외하는 새 cursor 생성
- future occurrence를 새 tempo로 예약

tempo는 active channel frequency를 바꾸지 않는다.

tempo 변경 전에 시작된 Voice의 실제 timeline 끝점은 과거 schedule의 예측과 달라질 수 있다. 기존 Voice를 유지하기로 한 정책상 이를 강제로 seek/reconstruct하지 않는다.

## Pitch 변경

pitch 변경 시:

- future Voice와 pending submissions을 취소한다.
- 새 pitch 크기와 부호로 schedule을 다시 만든다.
- schedule generation 증가
- 기존 active Voice는 현재 PCM 위치를 유지한다.
- 기존 Voice frequency의 크기와 부호를 즉시 변경한다.
- volume, pan 및 spatial 속성은 그대로 유지한다.
- future occurrence를 새 schedule로 예약한다.

pitch 부호가 바뀌면 기존 Voice는 현재 PCM 위치에서 즉시 진행 방향을 바꾼다. 기존 Voice를 원본 반대 끝으로 순간 이동시키지 않는다.

임의 seek snapshot은 pitch 변경 이력을 보존하지 않는다. 현재 tempo/pitch가 해당 note의 전체 interval에 적용됐다고 계산한다.

## Pitch 0

`pitch == 0`:

- sourceRate와 note duration을 정의할 수 없다.
- active/pending Voice를 정지한다.
- future submissions을 취소한다.
- 새 Voice를 만들지 않는다.
- transport 자체는 tempo에 따라 계속 진행할 수 있다.

pitch가 다시 0이 아니게 되면 현재 위치에서 schedule을 생성하고 `includePreviousNotes: true` snapshot을 복원한다.

## Tempo 0

`tempo == 0`:

- transport가 진행하지 않는다.
- direction과 wall delay를 정의할 수 없으므로 새 occurrence를 제출하지 않는다.
- future submissions을 취소한다.
- active Voice는 명시적 Pause가 아니므로 현재 pitch로 계속 재생한다.

tempo가 다시 0이 아니게 되면 현재 위치 기준으로 future cursor와 schedule을 재구성한다. 기존 active Voice는 유지한다.

## Volume 변경

모든 active/pending Voice에 즉시 적용한다.

```text
voiceVolume = preparedNote.staticVolume * player.volume
```

schedule 재생성은 하지 않는다.

## Pan 변경

모든 active/pending Voice에 즉시 적용한다.

```text
combinedPan = Lerp
(
    preparedNote.staticPan,
    player.panStereo,
    abs(player.panStereo)
)
```

schedule 재생성은 하지 않는다.

## 3D 및 거리 속성 변경

다음 속성은 active/pending Voice에 즉시 반영한다.

- `spatialBlend`
- `dopplerLevel`
- `spread`
- `minDistance`
- `maxDistance`
- `rolloffMode`

`Update()`는 현재처럼 main thread에서 Transform 및 Rigidbody 기반 spatial snapshot을 만든다. Worker와 channel 갱신은 snapshot만 읽는다.

## Loop 시간 모델

### 공개 time과 unwrapped 위치

- 공개 `NBSPlayer.time`은 현재 loop 구간 안의 file time을 나타낸다.
- schedule occurrence 계산은 `(fileTime, loopIteration)`을 unwrapped timeline 위치로 변환한다.
- 같은 raw note라도 loop iteration마다 별도 occurrence다.

### 반복 대상

- loop 시작 이전 intro note는 첫 회차에만 존재한다.
- loop 구간 안에서 시작하는 note는 iteration마다 반복한다.
- loop 구간 안의 Sound Stopper도 iteration마다 반복한다.
- 실제 playback interval `[playbackStart, playbackEnd]`가 loop 경계를 넘으면 다음 iteration과 겹칠 수 있다.
- tail은 loop 경계에서 중지하지 않는다.

### Forward loop

- transport가 loop end에 도달하면 file time을 loop start로 이동
- loop iteration 증가
- schedule cursor는 다음 iteration의 forward occurrence로 이동
- 이전 iteration Voice는 자연 종료까지 유지

### Reverse loop

- transport가 loop start에 도달하면 file time을 loop end로 이동
- loop iteration 증가
- schedule cursor는 다음 reverse occurrence로 이동
- 역방향 note의 원래 발생 시점은 `E`이며, pitch가 음수이면 실제 anchor는 `E - timelineDuration`이다.
- 이전 iteration Voice는 자연 종료까지 유지

### 인접 iteration snapshot

중간 재생 시 현재 iteration만 검사하면 경계를 넘어온 tail을 놓칠 수 있다. interval index는 최소 다음을 검사한다.

- 현재 iteration
- 진행 방향 기준 직전 iteration
- 최대 note duration이 loop range보다 길면 필요한 추가 이전 iteration

필요 iteration 수는 schedule의 최대 timeline duration과 loop range로 계산한다.

### File loop

`useFileLoopSettings`가 켜지고 header loop가 활성일 때:

- loop start는 `header.loopStartTick`의 tempo-map time
- loop end는 `NBSFile.duration`
- `maxLoopCount == 0`은 무한
- 그 외 값은 실제 반복 횟수

공식 형식: [Note Block Studio NBS Format](https://noteblock.studio/nbs)

### Manual loop

- `base.loopStart`, `base.loopEnd` 사용
- 유효 범위로 제한
- 무한 반복
- file loop 설정을 사용하지 않을 때 적용

### Loop 없는 종료

확정 정책:

- `isPlaying`을 자동으로 false로 만들지 않는다.
- time은 곡 길이 밖으로 계속 진행한다.
- 새 occurrence가 없으면 아무 Voice도 만들지 않는다.
- 기존 tail은 자연 종료한다.
- `length`는 NBS score duration이며 tail-inclusive length가 아니다.

## Sound Stopper와 방향

Sound Stopper는 진행 방향에서 event anchor를 통과할 때 대상 layer Voice를 멈춘다.

정방향 snapshot:

- note anchor에서 현재 위치까지 증가 방향으로 Stopper를 검사

역방향 snapshot:

- 실제 Voice anchor `^`에서 현재 위치까지 감소 방향으로 Stopper를 검사

같은 tick의 기존 layer ordering 의미를 보존한다. 동적으로 이동된 reverse note anchor와 Stopper anchor가 우연히 같으면 schedule의 안정적 원본 entry 순서로 처리한다.

## Worker 구조

`NBSPlaybackWorker`는 계속 모든 Player가 공유하는 하나의 background thread다.

각 순회:

1. 등록 Player snapshot 생성
2. Player별 `WorkerUpdate`
3. Player write lock 획득
4. active/play/pause/resource/schedule 상태 검증
5. 현재 transport time 및 loop position snapshot
6. current DSP parent clock snapshot
7. 만료 pending submission 정리
8. loop wrap 적용
9. schedule invalidation 처리
10. Query 실행
11. moment 순서대로 command 처리
12. pending submission 등록
13. cursor commit
14. lock 해제

Player 하나의 오류가 Worker 전체를 중단하지 않게 현재처럼 Player별 예외를 기록하고 다음 Player를 계속 처리한다.

## DSP clock 변환

한 Worker batch에서 current parent clock과 output sample rate를 한 번 얻는다.

```text
targetClock = currentParentClock + round(max(0, wallDelay) * outputSampleRate)
```

- 같은 moment의 모든 command는 같은 target clock
- 과거 command는 current parent clock 사용
- overflow는 `ulong.MaxValue`로 제한
- output sample rate를 48,000으로 고정하지 않는다.
- Stopwatch와 DSP clock midpoint 보정이 필요하면 batch anchor 한 번에만 수행한다.
- note마다 별도 current DSP query를 하지 않는다.

## `NBSPlaybackSettings`

유지:

```csharp
public static double workerInterval { get; set; }
public static double schedulingLookahead { get; set; }
```

기본값은 현재 실제 값 기준으로 유지한다.

```text
workerInterval = 0.1초
schedulingLookahead = 0.2초
```

제거:

```csharp
public static double lateTolerance
```

lookahead 변경 시:

- scheduling revision 증가
- Worker signal
- future pending submission 취소
- cursor 되감기
- 새 lookahead로 재조회

`workerInterval`은 Worker wake 간격만 변경한다. schedule 자체를 다시 만들 필요는 없다.

## `NBSPlayer` 공개 동작

### 유지할 주요 공개 속성

- `nbsFileRef`
- `nbsFile`
- `time`
- `tick`
- `index`
- `tickLength`
- `indexLength`
- `ticksPerSecond`
- `beatsPerMinute`
- `length`
- `useFileLoopSettings`
- `tempo`
- `pitch`
- `volume`
- `panStereo`
- spatial/거리 속성
- `rolloffMode`
- `nonRigidbodyVelocity`
- loop 속성

API 서명 호환성은 목표가 아니므로 새 내부 의미에 방해되면 바꿀 수 있다. 단, 외부에서 필요한 transport와 inspector 기능은 유지한다.

### 제거할 내부 상태

- `specialEventMap`
- raw `nextTickIndex`
- `scheduledFileLoops`
- `observedSchedulingRevision` 기반 raw tick reset
- deadline timestamp
- late tolerance 관련 Voice 상태
- 즉시/예약 분리 함수

### 새 내부 상태

- current `NBSPlaybackSchedule`
- `NBSPlaybackCursor`
- schedule generation
- pending submissions
- occurrence-aware Voice 목록
- clip resolver/bank

## Editor 변경

`NBSPlayerEditor`:

- worker interval 필드 유지
- scheduling lookahead 필드 유지
- late tolerance 필드 제거
- 역재생 mode 필드를 추가하지 않음
- tempo 부호가 timeline 방향, pitch 부호가 PCM 방향임을 tooltip/documentation에 명시
- playback map/schedule 준비 상태와 현재 cursor 정보를 디버그 정보로 표시할 수 있음

언어 파일:

- late tolerance label/tooltip 제거
- 필요하면 schedule/cursor 디버그 label 추가
- tempo/pitch 음수 의미 tooltip 추가

## README 변경

기존 `워커와 DSP 예약`, `재생 의미` 설명을 다음 내용으로 교체한다.

- NBSNoteMap/NBSPlaybackMap 사전 계산
- Player별 schedule 생성 조건
- tempo/pitch 부호 의미
- source start/end 수식
- lookahead 0 현재 occurrence 처리
- future DSP 예약과 past offset 복원
- late tolerance 제거
- Pause가 active Voice까지 정지
- seek와 늦은 reload snapshot 복원
- loop tail 유지
- loop 없는 unbounded transport

## 수명과 lock 계약

### `playingLock`

다음 상태를 함께 보호한다.

- current NBS scope
- current instrument bank
- current playback schedule
- cursor
- loop position
- pending submissions
- 재생 상태 변경과 schedule generation 교체

### `voiceLock`

- Voice 목록
- channel callback attach/detach
- occurrence ID 중복 확인

### Lock 순서

고정 순서:

```text
playingLock
  → voiceLock
```

callback은 native handle이 이미 detach된 뒤 들어올 수 있으므로 `playingLock`을 재진입하지 않고 `voiceLock` 안에서 목록 정리만 수행한다.

### Reload disposal

- 새 세대를 완전히 준비한 뒤 교체
- 교체 lock 안에서 기존 Voice 정지
- lock 밖에서 기존 scope/bank를 `DisposeQueue`에 등록
- stale channel callback이 새 generation Voice를 제거하지 않도록 occurrence와 channel reference 둘 다 확인

## 오류 처리

- 없는 instrument: 경고 후 해당 entry 소비, 반복 재시도하지 않음
- velocity 0: Voice 없이 entry 소비
- invalid static pitch ratio: Voice 없이 entry 소비
- clip length 0/비유한: schedule에서 해당 note 제외
- pitch 0: schedule 무음 상태
- tempo 0: 새 제출 중지
- FMOD invalid handle: Voice 목록에서 제거하고 정상 완료로 취급
- 개별 note FMOD 오류: 해당 command만 실패 처리하고 같은 moment의 나머지 command 계속
- schedule batch 치명적 오류: cursor 미commit, 다음 Worker 재조회; occurrence ID로 이미 만든 Voice 중복 방지
- loop range 0 이하/비유한: loop 비활성 처리
- 비유한 transport time: 새 조회 중지

## 성능 계약

- NBS parse 시 raw note 시간 계산 1회
- schedule 생성 시 note별 duration/anchor 계산 1회
- Worker hot path에서 tick-to-time 변환 금지
- Worker hot path에서 전체 note 선형 순회 금지
- range 조회와 cursor 이동은 이진 검색 또는 순차 cursor
- `includePreviousNotes` snapshot은 interval index 사용
- LINQ는 parse/schedule 생성 경로에서는 허용 가능하지만 Worker hot path에서는 사용하지 않음
- 같은 Worker batch에서 DSP clock/sample rate query 1회
- clip scope는 instrument별 공유
- 미래 lookahead 안에서만 FMOD channel 생성
- pending submission 목록은 lookahead 범위로 제한

## 정확성 한계

- Worker나 시스템이 clip 전체 길이보다 오래 멈췄다면 현재 이미 끝난 음을 소급 출력하지 않는다.
- tempo/pitch를 note 재생 도중 바꾸면 기존 Voice는 현재 PCM 위치에서 새 속성을 적용한다. 과거부터 새 값이 적용된 것처럼 재계산하지 않는다.
- 이후 명시적 seek는 속성 변경 이력이 없으므로 현재 tempo/pitch가 note 전체에 적용됐다고 가정한다.
- FMOD command 제출 자체의 mix-block latency는 존재할 수 있다. 미래 occurrence는 DSP 예약으로 sample-accurate하게 맞춘다.
- past occurrence는 source offset으로 timeline 위치를 보정하지만 이미 지나간 attack transient를 복원할 수는 없다.

## 구현 순서

### 1. 정적 맵

- `NBSNoteMap` 추가
- `NBSPlaybackMap` 추가
- instrument reference와 path 정규화 이동
- `NBSFile`에 두 맵 연결
- tempo/special event 원본 순서 검증

### 2. Clip metadata 경계

- `INBSClipMetadataProvider` 추가
- `NBSInstrumentBank`를 map reference 기반으로 변경
- unique instrument load 및 resolver 제공
- map/schedule에 clip reference가 들어가지 않는지 확인

### 3. Playback schedule

- 네 tempo/pitch 부호 조합 수식 구현
- `[S, E]`, 실제 playback interval, anchor, source 방향 계산
- prepared moments 생성
- interval snapshot index 생성
- Sound Stopper projection

### 4. Cursor와 loop occurrence

- `NBSPlaybackCursor`
- occurrence ID
- loop iteration/unwrapped position
- lookahead Query
- `includePreviousNotes`
- pending submissions

### 5. Player playback 교체

- 기존 deadline/late/cursor 코드 제거
- 단일 Voice 생성 경로 구현
- SetDelay + source offset 통합
- Pause/UnPause/seek/reload 상태 전이 구현
- 동적 속성 active Voice 반영

### 6. Editor와 문서

- late tolerance UI 제거
- tempo/pitch 부호 설명 추가
- README 전면 갱신
- 디버그 정보 정리

## 테스트 계획

프로젝트 규칙상 `dotnet build`, `dotnet test`, 생성 `.csproj`/`.sln` 빌드를 사용하지 않는다. 순수 로직은 Unity EditMode Test Runner, FMOD 경로는 Unity Editor 재생으로 검증한다.

### Map 테스트

- 고정 tempo note 절대 시간
- Tempo Changer 전후 note 절대 시간
- 같은 tick/layer 정렬
- custom instrument 상대 경로 정규화
- vanilla/custom instrument reference 해석
- static pitch ratio
- static volume/pan
- functional instrument audio 제외

### Schedule 수식 테스트

기준:

```text
S = 1.0
L = 0.5
Q = 1
abs(T) = 1
abs(P) = 1
```

검증:

- `T>0,P>0`: `* = 1.0`, `^ = 1.0`, source 0, end 1.5
- `T>0,P<0`: `* = 1.0`, `^ = 0.5`, source 0.5, end 0.0
- `T<0,P>0`: `* = 1.5`, `^ = 1.5`, source 0, end 1.0 방향
- `T<0,P<0`: `* = 1.5`, `^ = 1.0`, source 0.5, end 0.5 방향
- tempo 2배 시 timeline duration 2배
- pitch magnitude 2배 시 timeline duration 절반
- static note pitch ratio 2배 시 duration 절반

### Offset 테스트

- 네 부호 조합의 interval 중간점 source offset
- exact S/E 경계
- 마지막 PCM sample 제한
- 이미 끝난 note 제외
- pitch 0 제외
- clip length 0 제외

### Cursor 테스트

- forward include-current lower bound
- forward exclude-current upper bound
- reverse include-current upper bound
- reverse exclude-current lower bound
- 같은 anchor moment 전체 처리
- cursor 한 번 전진 후 중복 반환 없음
- schedule generation 변경 시 occurrence ID 분리

### Lookahead 테스트

- lookahead 0에서 current moment 재생
- lookahead 0에서 미래 moment 미제출
- Worker가 조금 늦은 moment offset 재생
- 양수 lookahead future 제출
- tempo magnitude에 따른 wall delay
- tempo 음수 future wall delay 양수

### Pending submission 테스트

- future moment 제출 시 cursor 전진
- target clock 도달 후 submission 제거
- Pause 취소 시 earliest future moment로 cursor 복원
- Sound Stopper-only moment도 복원
- lookahead 변경 후 future Voice 중복 없음
- batch 재조회 occurrence 중복 방지

### Snapshot 테스트

- 1초 clip을 0.5초 지점에서 시작
- pitch 배율을 반영한 source offset
- 역방향 source offset
- 동시에 살아 있는 복수 tail 반환
- 이미 Sound Stopper를 지난 tail 제외
- 현재 anchor note와 이전 tail 동시 반환
- 늦은 resource load snapshot

### FMOD 통합 테스트

- future SetDelay sample-accurate start
- past/current SetDelay zero delay start
- `channel.time` 적용 후 정확한 offset 시작
- negative frequency와 마지막 sample 역재생
- 같은 moment Voice가 같은 DSP clock 사용
- future Stopper end delay
- invalid handle callback 정리

### Pause 테스트

- active Voice PCM 위치 고정
- future Voice 제거
- future Stopper delay 해제
- UnPause 후 동일 PCM 위치 재개
- future schedule 재제출
- Pause 중 seek 후 전체 snapshot 교체

### 속성 변경 테스트

- tempo 변경 시 active Voice 유지
- tempo 변경 시 future schedule 교체
- pitch magnitude 변경 시 active frequency 즉시 변경
- pitch 부호 변경 시 현재 PCM 위치에서 방향 전환
- volume/pan/3D/거리/rolloff active Voice 반영
- pitch 0에서 Voice 정리 후 복구 snapshot
- tempo 0에서 active Voice 계속 재생, future 제출 중지

### Loop 테스트

- forward loop occurrence 반복
- reverse loop occurrence 반복
- loop boundary tail 유지
- 이전 iteration tail snapshot
- tail이 loop range보다 긴 경우 여러 iteration 검색
- file max loop count 0 무한
- file finite loop count
- manual loop 무한
- loop 없는 unbounded time

### Lifecycle 테스트

- Play 전 resource 미준비
- reload 중 Play
- reload 완료 snapshot
- seek 연속 호출
- Stop 후 stale callback
- OnDisable 후 Voice/submission 없음
- code unload Worker 종료
- old bank/scope가 Voice 정리 후 dispose

## 완료 조건

다음 조건을 모두 만족하면 전면 교체가 완료된 것으로 본다.

- NBS raw 데이터에서 공개 note/playback map이 생성된다.
- 공개 맵과 schedule에 실제 clip 또는 scope reference가 없다.
- Player가 보유한 bank만 clip 수명을 관리한다.
- Worker hot path에서 raw tick-to-time 계산과 late tolerance가 사라진다.
- lookahead 0에서 현재 occurrence가 정확히 한 번 재생된다.
- 미래 occurrence는 DSP 예약된다.
- past/current occurrence는 동일한 SetDelay 경로와 source offset으로 시작한다.
- seek와 늦은 load가 살아 있는 모든 tail을 복원한다.
- tempo 부호와 pitch 부호가 독립적으로 동작한다.
- 네 tempo/pitch 부호 조합에서 원래 발생 시점과 실제 Voice 시작 시점이 분리되어 동작한다.
- Pause가 active Voice를 실제로 정지하고 같은 PCM 위치에서 재개한다.
- tempo/pitch/lookahead 변경 후 future 예약 중복이 없다.
- loop tail이 경계에서 잘리지 않는다.
- Stop/reload/disable 뒤 stale Voice, callback, pending submission, scope가 남지 않는다.
- Editor와 README가 새 의미를 정확히 설명한다.
