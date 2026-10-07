#nullable enable
using RuniOS.Sounds.Processing;
using System.Threading;

namespace RuniOS.Sounds
{
    public sealed partial class SoundChannel
    {
        public DSP dsps { get; }

        public sealed class DSP
        {
            readonly SoundChannel channel;
            readonly ReaderWriterLockSlim dspLock = new();

            internal DSP(SoundChannel channel) => this.channel = channel;

            public Processing.DSP this[int index]
            {
                get
                {
                    dspLock.EnterReadLock();

                    try
                    {
                        channel.native.getNumDSPs(out int numdsps).ThrowIfNotOkOfChannel();
                        if (index < 0 || index >= numdsps)
                            throw new ArgumentOutOfRangeException(nameof(index), index, null);

                        channel.native.getDSP(index, out FMOD.DSP dsp).ThrowIfNotOkOfChannel();

                        Processing.DSP? managedDSP = Processing.DSP.GetManaged(dsp.handle);
                        if (managedDSP == null)
                            throw new InvalidOperationException("Failed to get Managed DSP from FMOD DSP");

                        return managedDSP;
                    }
                    finally
                    {
                        dspLock.ExitReadLock();
                    }
                }
            }

            public int count
            {
                get
                {
                    dspLock.EnterReadLock();

                    try
                    {
                        channel.native.getNumDSPs(out int numdsps).ThrowIfNotOkOfChannel();
                        return numdsps;
                    }
                    finally
                    {
                        dspLock.ExitReadLock();
                    }
                }
            }

            /// <summary>
            /// Attaches <paramref name="dsp"/> to this channel's DSP chain.<br/>
            /// <paramref name="dsp"/>를 이 채널의 DSP 체인에 부착합니다.
            /// </summary>
            /// <param name="dsp">
            /// DSP created by the same sound system.<br/>
            /// 같은 사운드 시스템에서 생성한 DSP입니다.
            /// </param>
            /// <param name="index">
            /// Position where FMOD inserts DSP in this channel's chain.<br/>
            /// FMOD가 이 채널 체인에서 DSP를 삽입할 위치입니다.
            /// </param>
            /// <remarks>
            /// FMOD moves a DSP that is already attached to another channel control.<br/>
            /// 이미 다른 channel control에 부착된 DSP는 FMOD가 이동합니다.
            /// </remarks>
            public void Add(Processing.DSP dsp, DSPIndex index = DSPIndex.tail)
            {
                ExceptionUtility.ThrowIfArgumentNull(dsp, nameof(dsp));
                if (dsp.system != channel.system)
                    throw new ArgumentException("The FMOD DSP belongs to a different sound system.", nameof(dsp));

                dspLock.EnterWriteLock();

                try
                {
                    dsp.UseNative((nativeDSP, native) => native.addDSP((int)index, nativeDSP).ThrowIfNotOkOfChannel(), channel.native);
                }
                finally
                {
                    dspLock.ExitWriteLock();
                }
            }

            /// <summary>
            /// Detaches <paramref name="dsp"/> from this channel's DSP chain.<br/>
            /// <paramref name="dsp"/>를 이 채널의 DSP 체인에서 분리합니다.
            /// </summary>
            /// <param name="dsp">
            /// DSP created by the same sound system.<br/>
            /// 같은 사운드 시스템에서 생성한 DSP입니다.
            /// </param>
            public void Remove(Processing.DSP dsp)
            {
                ExceptionUtility.ThrowIfArgumentNull(dsp, nameof(dsp));
                if (dsp.system != channel.system)
                    throw new ArgumentException("The FMOD DSP belongs to a different sound system.", nameof(dsp));

                dspLock.EnterWriteLock();

                try
                {
                    dsp.UseNative((nativeDSP, native) => native.removeDSP(nativeDSP).ThrowIfNotOkOfChannel(), channel.native);
                }
                finally
                {
                    dspLock.ExitWriteLock();
                }
            }

            public int IndexOf(Processing.DSP dsp)
            {
                ExceptionUtility.ThrowIfArgumentNull(dsp, nameof(dsp));
                if (dsp.system != channel.system)
                    throw new ArgumentException("The FMOD DSP belongs to a different sound system.", nameof(dsp));

                dspLock.EnterReadLock();

                try
                {
                    return dsp.UseNative((nativeDSP, native) =>
                    {
                        native.getDSPIndex(nativeDSP, out int index);
                        return index;
                    }, channel.native);
                }
                finally
                {
                    dspLock.ExitReadLock();
                }
            }
        }
    }
}
