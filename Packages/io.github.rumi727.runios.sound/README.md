# Runiverse OS Sound

`io.github.rumi727.runios.sound`는 특정 오디오 엔진에 묶이지 않는 재생 추상화와 파형 축약·렌더링을 제공합니다.

- **능력 인터페이스:** `IPlayControl`, `ISeekable`, `IPausable`, `ITempoControl` 등. 소비자는 필요한 능력만 요구합니다.
- **`RuniAudioSource`:** 능력 인터페이스를 모두 구현하는 추상 `MonoBehaviour`. 재생 상태, `playingLock`, `time`/`outputTime` 보간을 관리하고, 실제 소리는 파생 클래스가 냅니다.
- **`Waveform`과 `WaveformRenderer`:** 16비트 샘플로 min/max 피라미드를 만들어 임의 구간을 파형 열로 줄이고, 셰이더로 렌더 텍스처에 그립니다.
- **`SoundAssetRegistry`:** 리소스 팩의 `sounds.json`으로 사운드 id를 클립 키에 잇는 레지스트리(`runios:sounds`)
- **에디터:** `PlaybackController`(IMGUI 재생 컨트롤), `RuniAudioSource` 기본 인스펙터

> 내부 구조와 작동 원리는 [Documentation~/README.md](Documentation~/README.md)에 그림과 함께 정리되어 있습니다.
