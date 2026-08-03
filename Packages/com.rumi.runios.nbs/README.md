# Runiverse OS NBS

Note Block Studio 파일을 Runiverse OS 리소스 시스템으로 등록하고 FMOD DSP 클록에 맞춰 재생하는 패키지입니다.

## 리소스 구조

NBS 파일은 일반 리소스 팩의 `assets/{namespace}/nbs` 아래에 둡니다.

```text
pack.json
assets/
  my_game/
    nbs/
      music.nbs
    sounds/
      custom/instrument.ogg
```

`NBSAssetRegistry`는 `runios:nbs` 레지스트리에 `my_game:music`을 등록합니다. `NBSPlayer.nbsFileRef`에는 이 레지스트리 ID와 에셋 ID를 지정합니다. 실제 NBS 및 악기 오디오는 `AssetScope`가 있는 동안만 유지됩니다.

커스텀 악기의 NBS `soundFile`은 NBS 파일과 같은 네임스페이스의 `waves` 레지스트리에서 찾습니다. `Sounds/` 접두사와 확장자는 생략되며 절대 경로, `..`, 다른 네임스페이스 지정은 거부됩니다.

## TPS와 BPM

NBS 헤더의 tempo 원시 값은 `TPS × 100`입니다. 예를 들어 원시 값 `2000`은 `20 TPS`입니다.

NBS 한 박은 4틱이므로 BPM 변환식은 다음과 같습니다.

```text
BPM = TPS × 60 / 4 = TPS × 15
TPS = BPM / 15
```

여기서 `15`는 TPS가 아니라 **TPS를 BPM으로 바꾸는 계수**입니다. 따라서 `20 TPS = 300 BPM`, `10 TPS = 150 BPM`입니다. 파일 도중 `Tempo Changer`가 있으면 해당 틱 이후의 TPS가 `abs(note.pitch) / 15`로 바뀌며, 같은 틱에 여러 개가 있으면 가장 높은 레이어가 우선합니다.

## 워커와 DSP 예약

모든 `NBSPlayer`는 메인 스레드 `Update` 대신 하나의 공유 백그라운드 워커를 사용합니다.

워커의 단조 시간 기준은 `Stopwatch`의 정수 timestamp이고 FMOD 예약값도 `ulong` DSP sample clock입니다. 가변 TPS의 틱↔시간 적분과 두 클록 사이 변환에만 `double`을 사용하므로, 장시간 누적을 반복적인 소수 덧셈에 맡기지 않습니다. 실제 FMOD 출력 sample rate는 `SoundSystem.outputSampleRate`에서 읽으므로 48,000Hz를 예약 코드에 고정하지 않습니다.

- `NBSPlaybackSettings.workerInterval`의 기본값은 `0.1`초입니다. 런타임에서 변경할 수 있으며 100ms는 재생 코드에 하드코딩되지 않습니다.
- 워커는 상태 변경 시 즉시 깨어나고, 그 외에는 한 순회가 끝난 뒤 `workerInterval`만큼 기다립니다.
- `NBSPlaybackSettings.schedulingLookahead` 기본값은 `0.2`초입니다. 이 범위 안의 음은 paused FMOD 채널로 만든 뒤 DSP 시작 클록을 예약합니다.
- `NBSPlaybackSettings.lateTolerance` 기본값은 `0.05`초입니다. deadline보다 이 값 이내로 늦은 음은 즉시 재생하며, 더 오래된 음은 생략합니다.
- `schedulingLookahead == 0`이면 DSP 시작 예약을 하지 않습니다. 다음 틱이 지난 것을 워커가 확인한 펄스에서 즉시 재생하므로, 기존 프레임 `Update` 방식과 같되 기준 클록이 공유 워커입니다.
- 예약 구간은 FMOD가 자동으로 보충하지 않습니다. 연속 예약 재생에는 `workerInterval`을 `schedulingLookahead`보다 짧게 설정해야 합니다. 예를 들어 각각 2초와 1초라면 1초를 예약한 뒤 다음 워커 펄스까지 약 1초가 비고, 다음 펄스에서 밀린 구간을 생략하고 현재 시점부터 다시 예약합니다.

예약의 deadline은 "그 음이 실제로 시작해야 하는 절대 시각"입니다. 예약할 틱이 생긴 워커 펄스에서 마스터 DSP 클록과 `Stopwatch`의 대응 기준을 한 번만 얻고, 그 기준으로 틱별 절대 DSP 샘플 클록을 계산합니다. 같은 틱의 모든 음과 Sound Stopper는 동일한 DSP 클록을 공유합니다. 채널 준비 도중 DSP 시작 클록이 지나도 `lateTolerance` 이내면 즉시 시작하며, 허용 시간을 넘기면 시작하지 않습니다.

워커가 늦어지면 템포 맵과 이진 검색으로 `lateTolerance` 경계까지 커서를 바로 옮기므로 오래된 틱을 하나씩 몰아서 재생하지 않습니다. 허용 범위 안의 밀린 틱은 같은 펄스에서 즉시 처리한 뒤 미래 예약 구간까지 계속 채웁니다. 시스템 전체 정지로 FMOD DSP 클록도 멈춘 경우에는, 이미 만들어졌지만 아직 시작하지 않은 Voice 중 wall-clock deadline이 허용 범위를 넘긴 예약도 취소합니다. 이미 시작한 음의 tail은 중지하지 않습니다.

대략적인 누락 틱 열 수는 `TPS × max(0, 정지 시간 - 남은 예약 구간 - lateTolerance)`입니다. 실제 누락 음 수는 해당 구간의 활성 틱과 레이어 밀도에 따라 달라집니다. 사용자가 `time`, `tick`, `index`를 직접 바꾸는 seek도 건너간 구간을 의도적으로 재생하지 않습니다.

`Play()`는 NBS 파일과 악기 뱅크의 준비 여부와 무관하게 요청한 시작 시간에서 트랜스포트를 즉시 시작합니다. 두 리소스가 이미 준비됐다면 요청 시점의 첫 활성 틱 열을 포함해 예약합니다. 아직 준비되지 않았다면 로딩 중에도 시간이 계속 흐르며, 준비 완료 시점의 현재 트랜스포트 위치부터 재생합니다. 로딩 중 지나간 틱 열은 소급 재생하지 않습니다. 트랜스포트 시간은 음수나 곡 길이 밖이어도 자동 보정하지 않습니다.

## 재생 의미

- `Pause`는 트랜스포트와 아직 시작하지 않은 예약만 멈춥니다. 이미 시작한 음의 tail은 일시 정지하지 않습니다.
- `Stop`은 모든 음을 중지하고 시간을 0으로 되돌리지만, 컴포넌트가 활성인 동안 악기 스코프는 유지합니다.
- loop는 공개 `time`을 실제 경계에서 loop start로 이동시키되, 예약 커서는 경계를 미리 넘어 다음 회차의 음을 DSP에 예약합니다. 실제 경계에서는 예약을 취소하거나 커서를 다시 찾지 않습니다. 파일 loop를 쓰려면 `useFileLoopSettings`를 켭니다.
- `tick`은 템포 맵이 적용된 논리 위치이며 범위를 제한하지 않습니다.
- `index`는 개별 음표가 아니라 활성 틱 열의 인덱스입니다. special event만 있거나 velocity가 0인 틱 열도 포함합니다.

## 지원 포맷과 기능성 악기

[공식 NBS 포맷 문서](https://noteblock.studio/nbs)의 버전 0~5 구조와 [3.12 Beta 3의 v6 및 4종 trumpet 안내](https://github.com/OpenNBS/NoteBlockStudio/releases/tag/v3.12.0-beta.3)를 기준으로 버전 0부터 6까지 읽고 버전 7 이상은 명시적으로 거부합니다. 헤더, 노트, 레이어, 커스텀 악기 및 에디터용 메타데이터를 모두 보존합니다. 문자열은 strict UTF-8을 먼저 시도한 뒤 Windows-1252로 폴백합니다.

다음 기능성 커스텀 악기를 대소문자 구분 없이 파싱합니다.

- `Tempo Changer`
- `Sound Stopper`
- `Toggle Rainbow`
- `Show Save Popup`
- `Toggle Background Accent`
- `Change Color to #RRGGBB`

`Tempo Changer`와 `Sound Stopper`는 재생에 적용합니다. 나머지는 `NBSFile.specialEvents`에 보존하며 오디오를 만들지 않습니다.
