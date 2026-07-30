#nullable enable
using FMOD;

namespace RuniOS.Sounds
{
    public sealed partial class SoundChannel
    {
        /// <summary>
        /// Gets or sets the playback rate in hertz.<br/>
        /// 재생 속도를 헤르츠 단위로 가져오거나 설정합니다.
        /// </summary>
        public float frequency
        {
            get
            {
                native.getFrequency(out float frequency).ThrowIfNotOk();
                return frequency;
            }
            set => native.setFrequency(value).ThrowIfNotOk();
        }

        /// <summary>
        /// Gets or sets the virtual-voice priority, where lower values are more important.<br/>
        /// 낮은 값일수록 우선순위가 높은 가상 보이스 우선순위를 가져오거나 설정합니다.
        /// </summary>
        public int priority
        {
            get
            {
                native.getPriority(out int priority).ThrowIfNotOk();
                return priority;
            }
            set => native.setPriority(value.Clamp(0, 256)).ThrowIfNotOk();
        }

        /// <summary>
        /// Gets whether FMOD is currently emulating this channel as a virtual voice.<br/>
        /// FMOD가 현재 이 채널을 가상 보이스로 에뮬레이션하는지 여부를 가져옵니다.
        /// </summary>
        public bool isVirtual
        {
            get
            {
                native.isVirtual(out bool isVirtual).ThrowIfNotOk();
                return isVirtual;
            }
        }

        /// <summary>
        /// Gets this channel's index in the FMOD channel pool.<br/>
        /// FMOD 채널 풀에서 이 채널의 인덱스를 가져옵니다.
        /// </summary>
        public int index
        {
            get
            {
                native.getIndex(out int index).ThrowIfNotOk();
                return index;
            }
        }

        /// <summary>
        /// Gets whether FMOD still reports this channel as playing.<br/>
        /// FMOD가 이 채널을 아직 재생 중으로 보고하는지 여부를 가져옵니다.
        /// </summary>
        public bool isPlaying
        {
            get
            {
                RESULT result = native.isPlaying(out bool isPlaying);
                if (result == RESULT.ERR_INVALID_HANDLE)
                    return false;

                result.ThrowIfNotOk();
                return isPlaying;
            }
        }

        /// <summary>
        /// Gets or sets whether playback is paused.<br/>
        /// 재생이 일시 정지되었는지 여부를 가져오거나 설정합니다.
        /// </summary>
        public bool isPaused
        {
            get
            {
                RESULT result = native.getPaused(out bool isPaused);
                if (result == RESULT.ERR_INVALID_HANDLE)
                    return false;

                result.ThrowIfNotOk();
                return isPaused;
            }
            set => native.setPaused(value).ThrowIfNotOk();
        }

        /// <summary>
        /// Gets or sets the current playback position in PCM samples.<br/>
        /// 현재 재생 위치를 PCM 샘플 단위로 가져오거나 설정합니다.
        /// </summary>
        public uint timeSample
        {
            get
            {
                native.getPosition(out uint sample, TIMEUNIT.PCM).ThrowIfNotOk();
                return sample;
            }
            set => native.setPosition(value, TIMEUNIT.PCM).ThrowIfNotOk();
        }

        /// <summary>
        /// Gets or sets the current playback position in seconds.<br/>
        /// 현재 재생 위치를 초 단위로 가져오거나 설정합니다.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the assigned value is not finite, is negative, or exceeds FMOD's millisecond range.<br/>
        /// 설정한 값이 유한하지 않거나 음수이거나 FMOD의 밀리초 범위를 초과한 경우 발생합니다.
        /// </exception>
        public double time
        {
            get
            {
                native.getPosition(out uint pcm, TIMEUNIT.PCM).ThrowIfNotOk();

                float frequency;
                if (clip != null)
                    frequency = clip.frequency;
                else
                    native.getFrequency(out frequency).ThrowIfNotOk();

                return pcm / frequency.Abs();
            }
            set
            {
                if (clip == null)
                {
                    timeSample = (value * frequency).RoundToUInt();
                    return;
                }

                if (clip.samples <= 0)
                    return;

                timeSample = (value * clip.frequency).RoundToUInt().Clamp(0, clip.samples - 1);
            }
        }

        /// <summary>
        /// Gets the clip length in seconds, or zero for DSP playback.<br/>
        /// 클립 길이를 초 단위로 가져오며 DSP 재생에서는 0입니다.
        /// </summary>
        public double length => clip?.length ?? 0;

        /// <summary>
        /// Gets the clip length in PCM samples, or zero for DSP playback.<br/>
        /// 클립 길이를 PCM 샘플 단위로 가져오며 DSP 재생에서는 0입니다.
        /// </summary>
        public uint samples => clip?.samples ?? 0;

        /// <summary>
        /// Gets or sets whether playback repeats indefinitely.<br/>
        /// 재생을 무한 반복할지 여부를 가져오거나 설정합니다.
        /// </summary>
        /// <remarks>
        /// Setting this property replaces an FMOD loop mode with normal looping or no looping and sets <see cref="loopCount"/> to <c>-1</c> or zero.<br/>
        /// Set this property to <see langword="true"/> before assigning <see cref="loopCount"/> for finite looping. Use <see cref="native"/> for bidirectional looping.
        /// <br/><br/>
        /// 이 속성을 설정하면 FMOD 반복 모드를 일반 반복 또는 반복 없음으로 바꾸고 <see cref="loopCount"/>를 <c>-1</c> 또는 0으로 설정합니다.<br/>
        /// 유한 반복은 이 속성을 <see langword="true"/>로 설정한 뒤 <see cref="loopCount"/>를 지정합니다. 양방향 반복은 <see cref="native"/>를 사용해야 합니다.
        /// </remarks>
        public bool loop
        {
            get
            {
                native.getMode(out MODE mode).ThrowIfNotOk();
                return mode.HasFlag(MODE.LOOP_NORMAL) || mode.HasFlag(MODE.LOOP_BIDI);
            }
            set
            {
                native.getMode(out MODE mode).ThrowIfNotOk();
                mode &= ~(MODE.LOOP_OFF | MODE.LOOP_NORMAL | MODE.LOOP_BIDI);
                mode |= value ? MODE.LOOP_NORMAL : MODE.LOOP_OFF;

                native.setMode(mode).ThrowIfNotOk();
                native.setLoopCount(value ? -1 : 0).ThrowIfNotOk();
            }
        }

        /// <summary>
        /// Gets or sets the number of completed loops when <see cref="loop"/> is enabled.<br/>
        /// <see cref="loop"/>가 활성화된 경우 완료할 반복 횟수를 가져오거나 설정합니다.
        /// </summary>
        /// <remarks>
        /// A value of <c>-1</c> repeats indefinitely.<br/>
        /// <c>-1</c>은 무한 반복을 의미합니다.
        /// </remarks>
        public int loopCount
        {
            get
            {
                native.getLoopCount(out int loopCount).ThrowIfNotOk();
                return loopCount;
            }
            set => native.setLoopCount(value);
        }

        /// <summary>
        /// Gets or sets the loop start position in PCM samples.<br/>
        /// 반복 시작 위치를 PCM 샘플 단위로 가져오거나 설정합니다.
        /// </summary>
        public uint loopStartSample
        {
            get
            {
                native.getLoopPoints(out uint start, TIMEUNIT.PCM, out _, TIMEUNIT.PCM).ThrowIfNotOk();
                return start;
            }
            set
            {
                native.getLoopPoints(out _, TIMEUNIT.PCM, out uint end, TIMEUNIT.PCM).ThrowIfNotOk();
                native.setLoopPoints(value, TIMEUNIT.PCM, end, TIMEUNIT.PCM).ThrowIfNotOk();
            }
        }

        /// <summary>
        /// Gets or sets the loop end position in PCM samples.<br/>
        /// 반복 종료 위치를 PCM 샘플 단위로 가져오거나 설정합니다.
        /// </summary>
        public uint loopEndSample
        {
            get
            {
                native.getLoopPoints(out _, TIMEUNIT.PCM, out uint end, TIMEUNIT.PCM).ThrowIfNotOk();
                return end;
            }
            set
            {
                native.getLoopPoints(out uint start, TIMEUNIT.PCM, out _, TIMEUNIT.PCM).ThrowIfNotOk();
                native.setLoopPoints(start, TIMEUNIT.PCM, value, TIMEUNIT.PCM).ThrowIfNotOk();
            }
        }

        /// <summary>
        /// Gets or sets the loop start position in seconds.<br/>
        /// 반복 시작 위치를 초 단위로 가져오거나 설정합니다.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the assigned value is not finite, is negative, or exceeds FMOD's millisecond range.<br/>
        /// 설정한 값이 유한하지 않거나 음수이거나 FMOD의 밀리초 범위를 초과한 경우 발생합니다.
        /// </exception>
        public double loopStart
        {
            get
            {
                native.getLoopPoints(out uint start, TIMEUNIT.PCM, out _, TIMEUNIT.PCM).ThrowIfNotOk();
                return start / frequency;
            }
            set
            {
                if (clip == null || clip.samples <= 0)
                    return;

                native.getLoopPoints(out _, TIMEUNIT.PCM, out uint end, TIMEUNIT.PCM).ThrowIfNotOk();
                native.setLoopPoints((value * frequency).RoundToUInt().Clamp(0, clip.samples - 1), TIMEUNIT.PCM, end, TIMEUNIT.PCM).ThrowIfNotOk();
            }
        }

        /// <summary>
        /// Gets or sets the loop end position in seconds.<br/>
        /// 반복 종료 위치를 초 단위로 가져오거나 설정합니다.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the assigned value is not finite, is negative, or exceeds FMOD's millisecond range.<br/>
        /// 설정한 값이 유한하지 않거나 음수이거나 FMOD의 밀리초 범위를 초과한 경우 발생합니다.
        /// </exception>
        public double loopEnd
        {
            get
            {
                native.getLoopPoints(out _, TIMEUNIT.PCM, out uint end, TIMEUNIT.PCM).ThrowIfNotOk();
                return end / frequency;
            }
            set
            {
                if (clip == null || clip.samples <= 0)
                    return;

                native.getLoopPoints(out uint start, TIMEUNIT.PCM, out _, TIMEUNIT.PCM).ThrowIfNotOk();
                native.setLoopPoints(start, TIMEUNIT.PCM, (value * frequency).RoundToUInt().Clamp(0, clip.samples - 1), TIMEUNIT.PCM).ThrowIfNotOk();
            }
        }

        /// <summary>
        /// Pauses playback.<br/>
        /// 재생을 일시 정지합니다.
        /// </summary>
        public void Pause() => isPaused = true;

        /// <summary>
        /// Resumes playback from its current position.<br/>
        /// 현재 위치에서 재생을 다시 시작합니다.
        /// </summary>
        public void UnPause() => isPaused = false;

        /// <summary>
        /// Stops and disposes this channel.<br/>
        /// 이 채널을 중지하고 해제합니다.
        /// </summary>
        public void Stop()
        {
            RESULT result = native.stop();
            if (result != RESULT.ERR_INVALID_HANDLE)
                result.ThrowIfNotOk();
        }

        /// <summary>
        /// Gets the channel and parent channel-group DSP clocks in samples.<br/>
        /// 채널 및 부모 채널 그룹 DSP 클록을 샘플 단위로 가져옵니다.
        /// </summary>
        /// <returns>
        /// The channel DSP clock and parent channel-group DSP clock.<br/>
        /// 채널 DSP 클록 및 부모 채널 그룹 DSP 클록을 반환합니다.
        /// </returns>
        public (ulong dspClock, ulong parentClock) GetDSPClock()
        {
            native.getDSPClock(out ulong dspClock, out ulong parentClock).ThrowIfNotOk();
            return (dspClock, parentClock);
        }

        /// <summary>
        /// Schedules sample-accurate playback start and stop clocks relative to the parent channel group.<br/>
        /// 부모 채널 그룹 기준으로 샘플 단위의 정확한 재생 시작 및 종료 클록을 예약합니다.
        /// </summary>
        /// <param name="startDspClock">
        /// The parent DSP clock at which playback starts, or zero to leave it unscheduled.<br/>
        /// 재생을 시작할 부모 DSP 클록이며, 0이면 시작 예약을 해제합니다.
        /// </param>
        /// <param name="endDspClock">
        /// The parent DSP clock at which playback ends, or zero to leave it unscheduled.<br/>
        /// 재생을 종료할 부모 DSP 클록이며, 0이면 종료 예약을 해제합니다.
        /// </param>
        /// <param name="stopChannel">
        /// <see langword="true"/> to stop at the end clock; <see langword="false"/> to pause instead.<br/>
        /// 종료 클록에서 중지하려면 <see langword="true"/>, 일시 정지하려면 <see langword="false"/>입니다.
        /// </param>
        public void SetDelay(ulong startDspClock = 0, ulong endDspClock = 0, bool stopChannel = true) =>
            native.setDelay(startDspClock, endDspClock, stopChannel).ThrowIfNotOk();

        /// <summary>
        /// Gets the scheduled playback start and end clocks relative to the parent channel group.<br/>
        /// 부모 채널 그룹 기준으로 예약된 재생 시작 및 종료 클록을 가져옵니다.
        /// </summary>
        /// <returns>
        /// The start clock, end clock, and whether the channel stops at the end clock.<br/>
        /// 시작 클록, 종료 클록 및 종료 클록에서 채널을 중지하는지 여부를 반환합니다.
        /// </returns>
        public (ulong startDspClock, ulong endDspClock, bool stopChannel) GetDelay()
        {
            native.getDelay(out ulong startDspClock, out ulong endDspClock, out bool stopChannel).ThrowIfNotOk();
            return (startDspClock, endDspClock, stopChannel);
        }

        /// <summary>
        /// Adds a sample-accurate volume fade point relative to the parent channel group.<br/>
        /// 부모 채널 그룹 기준으로 샘플 단위의 정확한 볼륨 페이드 지점을 추가합니다.
        /// </summary>
        /// <param name="dspClock">
        /// The parent DSP clock at which to apply <paramref name="volume"/>.<br/>
        /// <paramref name="volume"/>을 적용할 부모 DSP 클록입니다.
        /// </param>
        /// <param name="volume">
        /// The volume level at <paramref name="dspClock"/>.<br/>
        /// <paramref name="dspClock"/>에서의 볼륨 레벨입니다.
        /// </param>
        public void AddFadePoint(ulong dspClock, float volume) =>
            native.addFadePoint(dspClock, volume).ThrowIfNotOk();

        /// <summary>
        /// Schedules a volume ramp that ends at the specified parent DSP clock.<br/>
        /// 지정된 부모 DSP 클록에서 끝나는 볼륨 램프를 예약합니다.
        /// </summary>
        /// <param name="dspClock">
        /// The parent DSP clock at which the ramp ends.<br/>
        /// 램프가 끝나는 부모 DSP 클록입니다.
        /// </param>
        /// <param name="volume">
        /// The target volume level.<br/>
        /// 목표 볼륨 레벨입니다.
        /// </param>
        public void SetFadePointRamp(ulong dspClock, float volume) =>
            native.setFadePointRamp(dspClock, volume).ThrowIfNotOk();

        /// <summary>
        /// Removes every fade point in the inclusive parent-DSP-clock range.<br/>
        /// 부모 DSP 클록의 포함 범위 안에 있는 모든 페이드 지점을 제거합니다.
        /// </summary>
        /// <param name="startDspClock">
        /// The first parent DSP clock in the removal range.<br/>
        /// 제거 범위의 첫 번째 부모 DSP 클록입니다.
        /// </param>
        /// <param name="endDspClock">
        /// The last parent DSP clock in the removal range.<br/>
        /// 제거 범위의 마지막 부모 DSP 클록입니다.
        /// </param>
        public void RemoveFadePoints(ulong startDspClock, ulong endDspClock) =>
            native.removeFadePoints(startDspClock, endDspClock).ThrowIfNotOk();

        /// <summary>
        /// Gets the scheduled fade-point clocks and their volume levels.<br/>
        /// 예약된 페이드 지점의 클록과 볼륨 레벨을 가져옵니다.
        /// </summary>
        /// <returns>
        /// Parallel arrays of parent DSP clocks and volume levels.<br/>
        /// 부모 DSP 클록과 볼륨 레벨의 병렬 배열을 반환합니다.
        /// </returns>
        public (ulong[] dspClocks, float[] volumes) GetFadePoints()
        {
            uint pointCount = 0;
            native.getFadePoints(ref pointCount, null!, null!).ThrowIfNotOk();

            if (pointCount == 0)
                return (Array.Empty<ulong>(), Array.Empty<float>());

            int count = checked((int)pointCount);
            ulong[] dspClocks = new ulong[count];
            float[] volumes = new float[count];
            native.getFadePoints(ref pointCount, dspClocks, volumes).ThrowIfNotOk();
            return (dspClocks, volumes);
        }
    }
}
