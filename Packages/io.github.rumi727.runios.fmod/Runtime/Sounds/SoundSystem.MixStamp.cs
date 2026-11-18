#nullable enable
using FMOD;
using System.Runtime.InteropServices;
using System.Threading;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        /// <summary>
        /// <c>FMOD_PLUGIN_SDK_VERSION</c> of <c>fmod_dsp.h</c>, which the C# wrapper does not define.<br/>
        /// C# 래퍼가 정의하지 않는 <c>fmod_dsp.h</c>의 <c>FMOD_PLUGIN_SDK_VERSION</c>입니다.
        /// </summary>
        const uint pluginSDKVersion = 110;

        /// <summary>
        /// How many times <see cref="TryGetMixStamp"/> reads the stamp before it gives up.<br/>
        /// A mixer stalled in the middle of a write would otherwise hold the reader, while the stamp is only a time correction that a later read catches up.
        /// <br/><br/>
        /// <see cref="TryGetMixStamp"/>가 포기하기 전까지 스탬프를 읽는 횟수입니다.<br/>
        /// 쓰기 도중 멈춘 믹서가 읽는 쪽을 붙잡지 않게 하며, 스탬프는 이후 읽기에서 따라잡는 시간 보정일 뿐입니다.
        /// </summary>
        const int mixStampReadAttempts = 100;

        /// <summary>
        /// The stamp that the mixer writes for this system.<br/>
        /// It lives in native memory because the mixer writes it outside the GC, and it is freed only after the native system is released,
        /// so a callback still running never writes to freed memory.
        /// <br/><br/>
        /// 믹서가 이 시스템에 대해 기록하는 스탬프입니다.<br/>
        /// 믹서가 GC 바깥에서 기록하므로 네이티브 메모리에 두며, 네이티브 시스템이 해제된 뒤에만 해제하므로
        /// 아직 실행 중인 콜백이 해제된 메모리에 기록하는 일이 없습니다.
        /// </summary>
        unsafe MixStampState* mixStampState;

        /// <summary>
        /// The pass-through DSP at the head of the master channel group that writes <see cref="mixStampState"/>.<br/>
        /// 마스터 채널 그룹의 head에서 <see cref="mixStampState"/>를 기록하는 패스스루 DSP입니다.
        /// </summary>
        DSP mixStampDSP;

        /// <summary>
        /// Gets the latest master DSP clock that the mixer processed and the timestamp at which it processed it.<br/>
        /// 믹서가 마지막으로 처리한 마스터 DSP 클록과 그 클록을 처리한 시각을 가져옵니다.
        /// </summary>
        /// <param name="stamp">
        /// The latest stamp when successful; otherwise, <see langword="default"/>.<br/>
        /// 성공하면 최신 스탬프이고, 그렇지 않으면 <see langword="default"/>입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the mixer has processed a block since this system was initialized or reset and a consistent stamp was read;
        /// otherwise, <see langword="false"/>, which includes the mixer writing the stamp throughout every attempt.<br/>
        /// 이 시스템이 초기화되거나 재설정된 뒤 믹서가 블록을 처리했고 일관된 스탬프를 읽었으면 <see langword="true"/>를 반환하고,
        /// 그렇지 않으면 <see langword="false"/>를 반환하며, 시도할 때마다 믹서가 스탬프를 쓰고 있던 경우도 포함됩니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this sound system has been disposed.<br/>
        /// 이 사운드 시스템이 해제된 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this sound system is resetting or faulted.<br/>
        /// 이 사운드 시스템이 재설정 중이거나 오류 상태인 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// The stamp is written on the mixer thread for every block, so the timestamp is when the mixer reached that clock, not when the clock was read.<br/>
        /// The clock is the end of the block, the same point as the master clock and the channel positions after the block is mixed.<br/>
        /// The output clock is what FMOD was outputting at the same time, which gives the position being heard.<br/>
        /// It maps clocks to time for interpolation; use the master clock to find a clock the mixer has not started yet.
        /// <br/><br/>
        /// 스탬프는 믹서 스레드가 블록마다 기록하므로, 타임스탬프는 클록을 읽은 시각이 아니라 믹서가 그 클록에 도달한 시각입니다.<br/>
        /// 클록은 블록의 끝이며, 블록이 믹스된 뒤의 마스터 클록 및 채널 위치와 같은 지점입니다.<br/>
        /// 출력 클록은 같은 시각에 FMOD가 출력하던 클록이며, 지금 들리는 위치를 구하는 데 사용합니다.<br/>
        /// 보간을 위해 클록을 시간에 대응시키는 용도이며, 믹서가 아직 시작하지 않은 클록은 마스터 클록으로 구해야 합니다.
        /// </remarks>
        public unsafe bool TryGetMixStamp(out SoundMixStamp stamp)
        {
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfUnavailableUnsafe();

                MixStampState* state = mixStampState;
                for (int attempt = 0; attempt < mixStampReadAttempts; attempt++)
                {
                    long sequence = Volatile.Read(ref state->sequence);
                    if (sequence == 0)
                        break;

                    // The mixer is writing the stamp, which takes only a few stores.
                    if ((sequence & 1) != 0)
                    {
                        System.Threading.Thread.SpinWait(1);
                        continue;
                    }

                    ulong dspClock = Volatile.Read(ref state->dspClock);
                    ulong outputClock = Volatile.Read(ref state->outputClock);
                    long timestamp = Volatile.Read(ref state->timestamp);

                    if (Volatile.Read(ref state->sequence) == sequence)
                    {
                        stamp = new SoundMixStamp(dspClock, outputClock, timestamp);
                        return true;
                    }
                }

                stamp = default;
                return false;
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        // Allocated once per system; see mixStampState.
        unsafe void AllocateMixStampState() => mixStampState = (MixStampState*)NativeMemory.AllocZeroed((nuint)sizeof(MixStampState));

        unsafe void FreeMixStampState()
        {
            NativeMemory.Free(mixStampState);
            mixStampState = null;
        }

        /// <summary>
        /// Forgets the stamp of the previous native system.<br/>
        /// The caller holds the write lock while no mixer is running.
        /// <br/><br/>
        /// 이전 네이티브 시스템의 스탬프를 지웁니다.<br/>
        /// 호출자는 믹서가 실행되지 않는 동안 쓰기 잠금을 보유합니다.
        /// </summary>
        unsafe void ClearMixStampStateUnsafe() => *mixStampState = default;

        unsafe void CreateMixStampDSPUnsafe()
        {
            // FMOD describes its software latency as the DSP buffer length times the buffer count.
            native.getDSPBufferSize(out uint bufferLength, out int bufferCount).ThrowIfNotOk();
            mixStampState->outputLatency = bufferLength * (ulong)bufferCount;

            DSP_DESCRIPTION description = new()
            {
                pluginsdkversion = pluginSDKVersion,
                name = CreateDSPName("RuniOS Mix Stamp"),
                version = 1,
                numinputbuffers = 1,
                numoutputbuffers = 1,
                read = mixStampReadCallback,
                shouldiprocess = mixStampShouldProcessCallback,
                userdata = (IntPtr)mixStampState
            };

            native.createDSP(ref description, out DSP dsp).ThrowIfNotOk();

            try
            {
                // The head is processed after the whole master graph, so the stamp is taken at the same point of every block.
                native.getMasterChannelGroup(out ChannelGroup masterGroup).ThrowIfNotOk();
                masterGroup.addDSP(CHANNELCONTROL_DSP_INDEX.HEAD, dsp).ThrowIfNotOk();
            }
            catch
            {
                dsp.release().LogErrorIfNotOk();
                throw;
            }

            mixStampDSP = dsp;
        }

        /// <summary>
        /// Removes and releases the stamp DSP.<br/>
        /// A faulted reset can leave the native system initialized without it, in which case this does nothing.<br/>
        /// The caller holds the write lock and handles the result.
        /// <br/><br/>
        /// 스탬프 DSP를 제거하고 해제합니다.<br/>
        /// 오류로 끝난 재설정은 이 DSP 없이 네이티브 시스템을 초기화된 상태로 남길 수 있으며, 그 경우 아무것도 하지 않습니다.<br/>
        /// 호출자는 쓰기 잠금을 보유하고 결과를 처리합니다.
        /// </summary>
        /// <returns>
        /// The first failing result of getting the master channel group, removing the DSP, or releasing it; otherwise, <see cref="RESULT.OK"/>.<br/>
        /// 마스터 채널 그룹 가져오기, DSP 제거 또는 해제 중 처음 실패한 결과를 반환하고, 그렇지 않으면 <see cref="RESULT.OK"/>를 반환합니다.
        /// </returns>
        RESULT ReleaseMixStampDSPUnsafe()
        {
            if (!mixStampDSP.hasHandle())
                return RESULT.OK;

            RESULT result = native.getMasterChannelGroup(out ChannelGroup masterGroup);
            if (result == RESULT.OK)
                result = masterGroup.removeDSP(mixStampDSP);
            if (result == RESULT.OK)
                result = mixStampDSP.release();

            mixStampDSP.clearHandle();
            return result;
        }
    }
}
