# Runiverse OS NBS

Note Block Studio 파일을 Runiverse OS 리소스 시스템으로 등록하고 FMOD DSP 클록에 맞춰 재생하는 패키지입니다.

> **주의:** 이 패키지의 대부분의 코드는 Codex가 작성했습니다.\
> 아직 전체 코드를 충분히 검토하고 테스트하지 못했으므로, 의도한 대로 올바르게 작동하는지와 장기적으로 유지보수 가능한지를 완벽하게 보장할 수 없습니다.\
> 실제 사용 전 코드 검토와 충분한 테스트를 진행해야 합니다.

## 리소스 구조

NBS 파일은 `assets/{namespace}/nbs`, 커스텀 악기는 같은 네임스페이스의 `sounds` 아래에 둡니다.

```text
pack.json
assets/
  my_game/
    nbs/music.nbs
    sounds/custom/instrument.ogg
```

`NBSAssetRegistry`는 NBS를 `runios:nbs` 레지스트리에 등록합니다. 커스텀 악기의 `soundFile`에서는 `sounds/` 접두사와 확장자를 제거합니다. 절대 경로, `..`, 다른 네임스페이스 지정은 거부됩니다. 실제 NBS와 악기 클립 수명은 `NBSPlayer`가 보유한 scope와 `NBSInstrumentBank`가 관리합니다.

## 사전 계산 데이터

`NBSReader`는 raw header/tick/layer/custom instrument/event를 검증한 뒤 다음 불변 맵을 만듭니다.

- `NBSTempoMap`: tempo change를 반영한 tick과 절대 시간 변환
- `NBSNoteMap`: 모든 note와 special event의 안정적인 ID, 절대 시간, 이진 검색 범위
- `NBSPlaybackMap`: clip 독립적인 instrument reference, pitch ratio, volume, pan, Sound Stopper
- `NBSVisualEffectMap`: Editor preview용 지속 상태와 transient event

`NBSPlaybackMap`에는 `WaveAudioClip`이나 scope가 없습니다. 각 Player는 현재 instrument metadata, tempo, pitch로 `NBSPlaybackSchedule`을 생성합니다. 스케줄은 note의 원본 시작 `S`, 끝 `E`, 발생 anchor, source 방향, interval index를 보유합니다. Worker hot path는 raw tick을 다시 시간으로 변환하지 않습니다.

## TPS와 BPM

NBS 헤더 원시 tempo는 `TPS × 100`입니다. NBS 한 박은 4틱입니다.

```text
BPM = TPS × 15
TPS = BPM / 15
```

`Tempo Changer` 이후 TPS는 `abs(note.pitch) / 15`입니다. 같은 tick에 여러 개가 있으면 가장 높은 layer가 우선합니다.

## 재생 시간 모델

tempo와 pitch 부호는 독립입니다.

- `tempo > 0`: NBS timeline 정방향
- `tempo < 0`: NBS timeline 역방향
- `pitch > 0`: clip PCM 정방향
- `pitch < 0`: clip PCM 역방향

note에 대해 원본 시작 시간을 `S`, clip 길이를 `L`, 정적 pitch ratio를 `Q`, Player 값을 `T`, `P`라고 하면 다음 값을 스케줄 생성 시 한 번 계산합니다.

```text
sourceRate      = Q × abs(P)
wallDuration    = L / sourceRate
timelineDuration = L × abs(T) / (Q × abs(P))
E               = S + timelineDuration

T > 0: anchor = S
T < 0: anchor = E
```

중간 재생, seek, 늦은 resource load, reload는 현재 위치 `X`에 걸쳐 있는 모든 interval을 찾습니다. 이미 진행한 wall-clock 시간만큼 source offset을 옮겨 살아 있는 tail부터 즉시 시작합니다. 이미 끝난 짧은 음은 소급 출력하지 않습니다.

## Worker와 DSP 예약

모든 `NBSPlayer`는 하나의 공유 background Worker를 사용합니다.

- `NBSPlaybackSettings.workerInterval`: 기본 `0.1`초
- `NBSPlaybackSettings.schedulingLookahead`: 기본 `0.2`초, wall-clock 기준
- 한 batch에서 master DSP clock과 실제 output sample rate를 한 번 읽음
- 미래 occurrence는 paused channel 생성 후 `SetDelay`로 예약
- 현재/past occurrence는 같은 경로에서 현재 DSP clock과 계산된 source offset 사용
- 같은 moment의 note와 Sound Stopper는 같은 DSP start clock 사용

`schedulingLookahead == 0`이어도 `wallDelay <= 0`인 현재 occurrence를 정확히 한 번 처리합니다. Worker가 늦으면 cursor부터 현재까지 소비하며, 아직 살아 있는 note만 offset부터 재생합니다. `lateTolerance` 기반 tick-column 생략은 없습니다.

미래까지 전진한 cursor와 실제 DSP 시작 사이에는 pending submission이 남습니다. Pause, tempo/pitch/lookahead/loop 변경 시 아직 미래인 Voice를 제거하고 가장 이른 pending moment 전으로 cursor를 되감아 중복 없이 다시 예약합니다.

## 상태 변경

- `Play(startTime)`: 현재 위치 snapshot과 lookahead 범위를 제출합니다. resource가 늦게 준비되면 그 시점의 현재 위치에서 snapshot을 복원합니다.
- `Pause`: active Voice의 PCM 위치까지 정지하고 future Voice 및 Stopper 종료 예약을 취소합니다.
- `UnPause`: active Voice를 같은 PCM 위치에서 재개하고 future schedule을 다시 제출합니다.
- `Stop`: 모든 Voice와 submission을 제거하고 time과 loop iteration을 0으로 되돌립니다.
- seek: 모든 Voice를 비운 뒤 target 위치의 active tail과 current/future occurrence를 다시 만듭니다.
- tempo 변경: 기존 active Voice는 유지하고 timeline schedule만 교체합니다.
- pitch 변경: 기존 active Voice의 현재 PCM 위치를 유지한 채 frequency 크기와 부호를 즉시 바꿉니다.
- pitch 0: 모든 Voice를 비웁니다. 0이 아닌 값으로 복구하면 현재 위치 snapshot을 복원합니다.
- tempo 0: transport와 새 제출만 멈춥니다. 이미 시작한 Voice는 계속 재생합니다.
- volume, pan, spatial blend, doppler, spread, distance, rolloff 변경: active/pending Voice에 즉시 반영합니다.

## Loop

공개 `time`은 현재 loop 구간의 file time이며 내부 occurrence는 loop iteration을 함께 사용합니다. file loop는 header의 start tick, score duration, maximum loop count를 사용합니다. manual loop는 `loopStart`와 `loopEnd`를 무한 반복합니다.

loop 경계에서는 기존 tail을 자르지 않습니다. interval snapshot은 최대 note duration에 따라 이전 iteration도 검색하므로 경계를 넘어온 tail을 복원합니다. loop가 없으면 score duration 이후에도 transport는 계속 진행하고 기존 tail은 자연 종료합니다.

## 지원 포맷과 기능성 악기

[공식 NBS 포맷 문서](https://noteblock.studio/nbs) 기준 버전 0부터 6까지 읽고 버전 7 이상은 거부합니다.

다음 기능성 커스텀 악기를 대소문자 구분 없이 파싱합니다.

- `Tempo Changer`
- `Sound Stopper`
- `Toggle Rainbow`
- `Show Save Popup`
- `Toggle Background Accent`
- `Change Color to #RRGGBB`

`Tempo Changer`는 `NBSTempoMap`, `Sound Stopper`는 audio schedule에 반영합니다. visual event는 `NBSVisualEffectMap`에만 저장하며 clip을 요청하지 않습니다.
