#nullable enable
using FMOD;
using System.Runtime.InteropServices;
using System.Threading;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        /// <summary><c>FMOD_PLUGIN_SDK_VERSION</c> of <c>fmod_dsp.h</c>, which the C# wrapper does not define.</summary>
        const uint pluginSDKVersion = 110;

        /// <summary>
        /// The stamp the mixer writes for this system. It lives in native memory because the mixer writes it outside the GC,
        /// and it is freed only after the native system is released, so a callback still running can never write to freed memory.
        /// </summary>
        unsafe MixStampState* mixStampState;

        /// <summary>The pass-through DSP at the head of the master channel group that writes <see cref="mixStampState"/>.</summary>
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
        /// <see langword="true"/> if the mixer has processed a block since this system was initialized or reset; otherwise, <see langword="false"/>.<br/>
        /// 이 시스템이 초기화되거나 재설정된 뒤 믹서가 블록을 처리했으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
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
        /// The stamp is written on the mixer thread for every block, so the timestamp is when the mixer reached that clock, not when the clock was read.
        /// The clock is the end of the block, the same point as the master clock and the channel positions after the block is mixed.
        /// The output clock is what FMOD was outputting at the same time, which gives the position being heard.
        /// It maps clocks to time for interpolation; use the master clock to find a clock the mixer has not started yet.<br/>
        /// 스탬프는 믹서 스레드가 블록마다 기록하므로, 타임스탬프는 클록을 읽은 시각이 아니라 믹서가 그 클록에 도달한 시각입니다.
        /// 클록은 블록의 끝이며, 블록이 믹스된 뒤의 마스터 클록 및 채널 위치와 같은 지점입니다.
        /// 출력 클록은 같은 시각에 FMOD가 출력하던 클록이며, 지금 들리는 위치를 구하는 데 씁니다.
        /// 보간을 위해 클록을 시간에 대응시키는 용도이며, 믹서가 아직 시작하지 않은 클록은 마스터 클록으로 구해야 합니다.
        /// </remarks>
        public unsafe bool TryGetMixStamp(out SoundMixStamp stamp)
        {
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfUnavailableUnsafe();

                MixStampState* state = mixStampState;
                while (true)
                {
                    long sequence = Volatile.Read(ref state->sequence);
                    if (sequence == 0)
                    {
                        stamp = default;
                        return false;
                    }

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

        /// <summary>Forgets the stamp of the previous native system. The caller holds the write lock while no mixer is running.</summary>
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
        /// Removes and releases the stamp DSP. A faulted reset can leave the native system initialized without it.
        /// The caller holds the write lock and handles the result.
        /// </summary>
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
