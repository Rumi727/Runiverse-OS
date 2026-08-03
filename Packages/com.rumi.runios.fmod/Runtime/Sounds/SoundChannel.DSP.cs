#nullable enable
using RuniOS.Sounds.Processing;

namespace RuniOS.Sounds
{
    public sealed partial class SoundChannel
    {
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
        public void AddDSP(DSP dsp, DSPIndex index = DSPIndex.tail)
        {
            ExceptionUtility.ThrowIfArgumentNull(dsp, nameof(dsp));
            if (dsp.system != system)
                throw new ArgumentException("The FMOD DSP belongs to a different sound system.", nameof(dsp));

            dsp.UseNative((nativeDSP, native) => native.addDSP((int)index, nativeDSP).ThrowIfNotOk(this), native);
        }

        /// <summary>
        /// Detaches <paramref name="dsp"/> from this channel's DSP chain.<br/>
        /// <paramref name="dsp"/>를 이 채널의 DSP 체인에서 분리합니다.
        /// </summary>
        /// <param name="dsp">
        /// DSP created by the same sound system.<br/>
        /// 같은 사운드 시스템에서 생성한 DSP입니다.
        /// </param>
        public void RemoveDSP(DSP dsp)
        {
            ExceptionUtility.ThrowIfArgumentNull(dsp, nameof(dsp));
            if (dsp.system != system)
                throw new ArgumentException("The FMOD DSP belongs to a different sound system.", nameof(dsp));

            dsp.UseNative((nativeDSP, native) => native.removeDSP(nativeDSP).ThrowIfNotOk(this), native);
        }
    }
}
