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
        /// Writes the stamp on the mixer thread for every block. It runs on the hot path, so it does not allocate or lock,
        /// and it reads the FMOD functions through raw pointers because <see cref="DSP_STATE.functions"/> marshals a new structure of delegates.
        /// </summary>
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

            // Seqlock with a single writer: the odd sequence is published with a full fence before the data,
            // and the even sequence is released after it.
            long sequence = Interlocked.Increment(ref stamp->sequence);
            Volatile.Write(ref stamp->dspClock, dspClock);
            Volatile.Write(ref stamp->timestamp, timestamp);
            Volatile.Write(ref stamp->sequence, sequence + 1);

            // The DSP never changes the signal, so the mixer passes the input through without calling read.
            return RESULT.ERR_DSP_DONTPROCESS;
        }

        /// <summary>Copies the input to the output in case the mixer calls read anyway.</summary>
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
            public long timestamp;
        }

        /// <summary>The layout of <c>FMOD_DSP_STATE</c>.</summary>
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

        /// <summary>The layout of <c>FMOD_DSP_STATE_FUNCTIONS</c>. Only the used functions are typed.</summary>
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
