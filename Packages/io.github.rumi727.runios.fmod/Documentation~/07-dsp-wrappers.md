# 7. DSP 래퍼

> 기준 커밋: `3a2b24b9`. [← 목차](README.md) · 이전: [6. DSP 클록과 믹스 스탬프](06-dsp-clock-and-mix-stamp.md) · 다음: [8. WaveAudioSource](08-wave-audio-source.md)

`Runtime/Sounds/Processing`은 FMOD 내장 DSP(에코, 피치 시프트, FFT, 리버브 …)를 타입이 있는 C# 속성으로 다루게 하는 래퍼 모음입니다. 래퍼 하나는 FMOD `DSP_TYPE` 하나에 대응하고, FMOD의 정수 인덱스 파라미터를 이름 있는 속성으로 바꿉니다.
이 장은 공통 기반 클래스와 생성 경로를 설명하고, 개별 래퍼는 표로 정리합니다. 시스템이 내부적으로 쓰는 믹스 스탬프 DSP는 래퍼가 아닙니다([6장](06-dsp-clock-and-mix-stamp.md#63-믹스-스탬프-무엇을-왜)).

- [7.1 생성: `CreateDSP<T>`](#71-생성-createdspt)
- [7.2 기반 클래스 `DSP`](#72-기반-클래스-dsp)
- [7.3 파라미터 접근자](#73-파라미터-접근자)
- [7.4 붙이기와 떼기](#74-붙이기와-떼기)
- [7.5 내장 DSP 래퍼 목록](#75-내장-dsp-래퍼-목록)
- [7.6 `CustomDSP`](#76-customdsp)

---

## 7.1 생성: `CreateDSP<T>`

래퍼의 생성자는 모두 `private`입니다. DSP는 시스템이 만듭니다.

```csharp
// Runtime/Sounds/SoundSystem.CreateDSP.cs
22  public T CreateDSP<T>() where T : DSP => (T)CreateDSP(typeof(T));

48  public DSP CreateDSP(Type type)
    {
50      nativeLock.EnterReadLock();
52      try
        {
54          ThrowIfUnavailableUnsafe();
56          ArgumentNullException.ThrowIfNull(type);
58          if (!typeof(DSP).IsAssignableFrom(type) || type.IsAbstract)
59              throw new ArgumentException($"The type must be a non-abstract {nameof(DSP)} type.", nameof(type));
61          if (Activator.CreateInstance(type, nonPublic: true) is not DSP dsp)        // ①
62              throw new ArgumentException($"Unable to create DSP of type {type.Name}.", nameof(type));
64          FMOD.DSP nativeDSP = CreateNativeDSP(dsp);                                // ②
65          dsp.Initialize(this, nativeDSP);                                          // ③
67          return dsp;
        }
        …
    }
```

1. ① 비공개 생성자를 리플렉션으로 부릅니다. 래퍼가 시스템 밖에서 만들어지지 않게 하면서도 타입만으로 생성할 수 있게 합니다.
2. ② 내장 래퍼면 [`createDSPByType(dsp.type)`](../Runtime/Sounds/SoundSystem.CreateDSP.cs#L80)을 부릅니다. 각 래퍼는 `internal override DSP_TYPE type`으로 자기 FMOD 타입을 알려 줍니다. `CustomDSP`면 콜백이 든 `DSP_DESCRIPTION`을 만들어 `createDSP`를 부릅니다([§7.6](#76-customdsp)).
3. ③ [`Initialize`](../Runtime/Sounds/Processing/DSP.cs#L37)가 시스템과 핸들을 넣고, 시스템에 등록하고, 핸들 → 래퍼 표(`dspLists`)에 넣습니다. 한 래퍼를 두 번 초기화하면 예외입니다.

## 7.2 기반 클래스 `DSP`

[`Processing.DSP`](../Runtime/Sounds/Processing/DSP.cs#L18)는 추상 클래스이고 [2장](02-resource-ownership.md#26-리소스-종류별-구현)의 리소스 계약을 따릅니다.

| 멤버 | 동작 |
| --- | --- |
| `system` | 만든 시스템. 초기화 전이면 예외 |
| `active` | `DSP::setActive`. 그래프에 연결돼 있어도 비활성이면 처리하지 않음 |
| `bypass` | `DSP::setBypass`. 처리 없이 입력을 통과 |
| `signalMix` | `(preWet, postWet, dry)` = `setWetDryMix` |
| `static GetManaged(handle)` | 네이티브 핸들로 래퍼 찾기. [5장 §5.9](05-channel.md#59-채널의-dsp-체인)의 채널 DSP 목록이 사용 |
| `Dispose()` | 시스템의 `Dispose(resource)`로. 해제 시 `dspLists`에서 빼고 `release()` 뒤 `OnNativeReleaseAccepted()` 호출 |
| `UseNative` | 래핑되지 않은 `FMOD.DSP` 기능 |

시스템이 한꺼번에 해제할 때 DSP는 채널·그룹보다 **뒤**, 클립보다 **앞**입니다([2장 §2.5](02-resource-ownership.md#25-시스템이-리소스를-한꺼번에-해제하는-경로)). 채널에서 먼저 떨어진 뒤 해제되게 하려는 순서입니다.

## 7.3 파라미터 접근자

FMOD DSP 파라미터는 인덱스와 타입(float, int, bool, data)으로 접근합니다. 기반 클래스가 타입별 인덱서 네 개를 `protected`로 제공하고, 각 래퍼는 FMOD 열거형 값을 인덱스로 넘기는 한 줄 속성만 가집니다.

```csharp
// Runtime/Sounds/Processing/DSP.Parameter.FloatParameters.cs
13  public float this[int index]
    {
15      get => dsp.UseNative((dsp, index) =>
        {
17          dsp.getParameterFloat(index, out float value).ThrowIfNotOk();
18          return value;
19      }, index);
20      set => dsp.UseNative((FMOD.DSP dsp, (int index, float value) state) => dsp.setParameterFloat(state.index, state.value).ThrowIfNotOk(), (index, value));
    }

// Runtime/Sounds/Processing/EchoDSP.cs
15  public float delay { get => floatParameters[(int)DSP_ECHO.DELAY]; set => floatParameters[(int)DSP_ECHO.DELAY] = value; }
```

파라미터 하나는 `UseNative` 한 번입니다. 상태를 튜플 인자로 넘겨 람다가 캡처하지 않게 합니다.

구조체 데이터 파라미터는 [`DSP.Parameter.Utility.cs`](../Runtime/Sounds/Processing/DSP.Parameter.Utility.cs#L8)의 도우미가 처리합니다.

| 도우미 | 동작 |
| --- | --- |
| `GetDataParameter<T>` | `getParameterData`의 포인터를 `Marshal.PtrToStructure<T>`로 복사. 길이가 모자라면 예외 |
| `SetDataParameter<T>` | 구조체를 비관리 메모리에 마샬한 뒤 바이트로 복사해 `setParameterData` |
| `GetOverallGainDataParameter`, `GetDynamicResponseDataParameter`, `GetFFTSpectrumDataParameter` | FMOD가 정의한 읽기 전용 구조체(전체 이득, 동적 응답 RMS, FFT 스펙트럼)를 관리 배열로 복사 |
| `GetBooleanDataParameter` / `SetBooleanDataParameter` | 4바이트 정수로 된 bool 데이터 |

> **관찰된 동작:** `dataParameters[index]`의 getter([`DSP.Parameter.DataParameters.cs#L13-L27`](../Runtime/Sounds/Processing/DSP.Parameter.DataParameters.cs#L13-L27))는 DSP 잠금을 푼 뒤에도 FMOD가 가진 메모리를 가리키는 `Span<byte>`를 돌려줍니다. 위 표의 도우미들은 이 인덱서를 쓰지 않고, `UseNative` 안에서 값을 복사해 돌려줍니다.

## 7.4 붙이기와 떼기

| 대상 | 붙이기 | 떼기 |
| --- | --- | --- |
| 채널 | `channel.dsps.Add(dsp, DSPIndex)` | `channel.dsps.Remove(dsp)` |
| 그룹 | `group.AddDSP(dsp, DSPIndex)` | `group.RemoveDSP(dsp)` |

`DSPIndex`는 `head`(출력 쪽 끝), `fader`(FMOD 내장 볼륨 페이더 위치), `tail`(입력 쪽 끝)입니다. 다른 시스템의 DSP를 넘기면 `ArgumentException`입니다. 떼어도 DSP는 살아 있으므로 다시 붙이거나 `Dispose()`합니다.

```csharp
SoundChannel channel = system.PlaySound(clip, paused: true);
EchoDSP echo = system.CreateDSP<EchoDSP>();
echo.delay = 250;
echo.feedback = 40;
channel.dsps.Add(echo);
channel.UnPause();
// … 재생이 끝난 뒤
echo.Dispose();
```

[8장](08-wave-audio-source.md#85-피치-dsp-단계)의 `WaveAudioSource`는 `PitchShiftDSP`를 이 방식으로 채널에 붙입니다.

## 7.5 내장 DSP 래퍼 목록

| 분류 | 래퍼 (`DSP_TYPE`) | 비고 |
| --- | --- | --- |
| 필터·EQ | `LowPassDSP`, `LowPassSimpleDSP`, `ITLowPassDSP`, `HighPassDSP`, `HighPassSimpleDSP`, `ParamEQDSP`, `ThreeEQDSP`, `MultibandEQDSP` | `MultibandEQDSP`는 `EqualizerBand`(A~E)별 `Get/SetFilter`·`Frequency`·`Q`·`Gain` 메서드 |
| 다이내믹스 | `CompressorDSP`, `LimiterDSP`, `NormalizeDSP`, `MultibandDynamicsDSP` | `MultibandDynamicsDSP`는 밴드(A~C)별 모드와 응답 |
| 시간·변조 | `EchoDSP`, `ITEchoDSP`, `DelayDSP`, `ChorusDSP`, `FlangeDSP`, `TremoloDSP`, `PitchShiftDSP` | `PitchShiftDSP`: `pitch`, `fftSize`, `maxChannels` |
| 리버브 | `SFXReverbDSP`, `ConvolutionReverbDSP` | `ConvolutionReverbDSP.SetImpulseResponse(channelCount, samples)` |
| 왜곡·생성 | `DistortionDSP`, `OscillatorDSP` | `OscillatorWaveform` 6종 |
| 라우팅·믹스 | `SendDSP`, `ReturnDSP`, `TransceiverDSP`, `ChannelMixDSP`, `MixerDSP`, `FaderDSP` | `ChannelMixDSP`는 입력 채널별 이득과 출력 채널 |
| 공간 | `PanDSP`, `ObjectPanDSP` | 3D 속성은 `DSP3DAttributesMulti`(리스너 1~8개) |
| 분석 | `FFTDSP`, `LoudnessMeterDSP` | `FFTDSP.spectrum`은 `float[][]`(채널 × 빈), `LoudnessMeterDSP.info`는 LUFS 값들 |

FMOD 열거형 값을 C#식으로 다시 정의한 옵션 타입(`EchoDelayChangeMode`, `FFTWindow`, `PanMode`, `LoudnessMeterState` …)과 값 구조체(`DSP3DAttributes`, `AttenuationRange`, `LoudnessMeterInfo` …)는 [`BuiltInDSPOptions.cs`](../Runtime/Sounds/Processing/BuiltInDSPOptions.cs#L8)에 모여 있습니다. 각 파라미터의 의미와 단위는 FMOD 문서의 해당 `FMOD_DSP_…` 항목을 따릅니다.

## 7.6 `CustomDSP`

`Processing/Custom`의 [`CustomDSP`](../Runtime/Sounds/Processing/Custom/CustomDSP.cs#L17)와 파생 `CustomGeneratorDSP`·`CustomReadDSP`·`CustomProcessDSP`, 그리고 `DSPConnection`·`CustomDSPParameterBuilder`·`DSP.AddInput`은 C#으로 오디오 처리를 직접 쓰는 사용자 DSP 기능입니다. 모두 `[Obsolete("CustomDSP has not been tested and is quite complex!")]`로 표시되어 있고 **다시 설계될 예정**이라 이 문서에서는 구조를 다루지 않습니다. `CreateDSP`가 이 타입들을 만나면 내장 DSP 대신 [`CreateNativeDSP`](../Runtime/Sounds/SoundSystem.CreateDSP.cs#L76)의 사용자 DSP 경로를 탄다는 것만 알아 두면 됩니다.
