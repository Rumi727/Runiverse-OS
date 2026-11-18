#nullable enable
using FMOD;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        // The static fields keep the delegates, and so their native thunks, alive for as long as the assembly is loaded.
        static readonly DSP_SHOULDIPROCESS_CALLBACK mixStampShouldProcessCallback = OnMixStampShouldProcess;
        static readonly DSP_READ_CALLBACK mixStampReadCallback = OnMixStampRead;

        /// <summary>
        /// Writes the stamp on the mixer thread for every block.<br/>
        /// It runs on the hot path, so it does not allocate or lock,
        /// and it calls the FMOD functions through raw pointers because <see cref="DSP_STATE.functions"/> marshals a new structure of delegates.
        /// <br/><br/>
        /// 믹서 스레드에서 블록마다 스탬프를 기록합니다.<br/>
        /// hot path에서 실행되므로 할당하거나 잠그지 않으며,
        /// <see cref="DSP_STATE.functions"/>는 델리게이트 구조체를 새로 마샬링하므로 FMOD 함수를 원시 포인터로 호출합니다.
        /// </summary>
        /// <returns>
        /// <see cref="RESULT.ERR_DSP_DONTPROCESS"/> so that the mixer passes the input through, or the failing result of an FMOD function.<br/>
        /// 믹서가 입력을 그대로 통과시키도록 <see cref="RESULT.ERR_DSP_DONTPROCESS"/>를 반환하거나, 실패한 FMOD 함수의 결과를 반환합니다.
        /// </returns>
        [AOT.MonoPInvokeCallback(typeof(DSP_SHOULDIPROCESS_CALLBACK))]
        static unsafe RESULT OnMixStampShouldProcess(ref DSP_STATE dspState, bool inputsIdle, uint length, CHANNELMASK inMask, int inChannels, SPEAKERMODE speakerMode)
        {
            NativeDSPState* state = (NativeDSPState*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref dspState);

            IntPtr userData;
            RESULT result = state->functions->getuserdata(state, &userData);
            if (result != RESULT.OK)
                return result;

            ulong dspClock;
            result = state->functions->getclock(state, &dspClock, null, null);
            if (result != RESULT.OK)
                return result;

            // The clock is the start of the block, while the master clock and the channel positions are at its end once it is mixed.
            dspClock += length;

            long timestamp = Stopwatch.GetTimestamp();
            MixStampState* stamp = (MixStampState*)userData;

            // The software buffer is full while the mixer keeps up, so the output is that far behind the block just mixed.
            ulong outputLatency = stamp->outputLatency;
            ulong outputClock = dspClock > outputLatency ? dspClock - outputLatency : 0;

            // Seqlock with a single writer: the odd sequence is published with a full fence before the data,
            // and the even sequence is released after it.
            long sequence = Interlocked.Increment(ref stamp->sequence);
            Volatile.Write(ref stamp->dspClock, dspClock);
            Volatile.Write(ref stamp->outputClock, outputClock);
            Volatile.Write(ref stamp->timestamp, timestamp);
            Volatile.Write(ref stamp->sequence, sequence + 1);

            // The DSP never changes the signal, so the mixer passes the input through without calling read.
            return RESULT.ERR_DSP_DONTPROCESS;
        }

        /// <summary>
        /// Copies the input to the output in case the mixer calls read anyway.<br/>
        /// 믹서가 그래도 read를 호출하는 경우를 대비해 입력을 출력으로 복사합니다.
        /// </summary>
        [AOT.MonoPInvokeCallback(typeof(DSP_READ_CALLBACK))]
        static unsafe RESULT OnMixStampRead(ref DSP_STATE dspState, IntPtr inBuffer, IntPtr outBuffer, uint length, int inChannels, ref int outChannels)
        {
            outChannels = inChannels;

            long bytes = length * inChannels * sizeof(float);
            Buffer.MemoryCopy((void*)inBuffer, (void*)outBuffer, bytes, bytes);
            return RESULT.OK;
        }

        struct MixStampState
        {
            public long sequence;
            public ulong dspClock;
            public ulong outputClock;
            public long timestamp;

            /// <summary>
            /// The software buffer in output samples, written before the stamp DSP is added and only read by the mixer.<br/>
            /// 출력 샘플 단위의 소프트웨어 버퍼이며, 스탬프 DSP를 추가하기 전에 기록하고 믹서는 읽기만 합니다.
            /// </summary>
            public ulong outputLatency;
        }

        /// <summary>
        /// The layout of <c>FMOD_DSP_STATE</c>.<br/>
        /// <c>FMOD_DSP_STATE</c>의 레이아웃입니다.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        unsafe struct NativeDSPState
        {
            public IntPtr instance;
            public IntPtr pluginData;
            public uint channelMask;
            public int sourceSpeakerMode;
            public IntPtr sidechainData;
            public int sidechainChannels;
            public NativeDSPStateFunctions* functions;
            public int systemObject;
        }

        /// <summary>
        /// The layout of <c>FMOD_DSP_STATE_FUNCTIONS</c>, where only the used functions are typed.<br/>
        /// <c>FMOD_DSP_STATE_FUNCTIONS</c>의 레이아웃이며, 사용하는 함수만 타입을 지정합니다.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        unsafe struct NativeDSPStateFunctions
        {
            public IntPtr alloc;
            public IntPtr realloc;
            public IntPtr free;
            public IntPtr getSampleRate;
            public IntPtr getBlockSize;
            public IntPtr dft;
            public IntPtr pan;
            public IntPtr getSpeakerMode;
            public delegate* unmanaged<NativeDSPState*, ulong*, uint*, uint*, RESULT> getclock;
            public IntPtr getListenerAttributes;
            public IntPtr log;
            public delegate* unmanaged<NativeDSPState*, IntPtr*, RESULT> getuserdata;
        }
    }
}
