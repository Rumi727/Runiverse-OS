#nullable enable
using FMOD;

namespace RuniOS.Sounds
{
    public sealed partial class WaveAudioClip
    {
        /// <summary>
        /// Gets the current FMOD sound opening and streaming information.<br/>
        /// 현재 FMOD 사운드의 열기 및 스트리밍 정보를 가져옵니다.
        /// </summary>
        public SoundOpenStates openStates => UseNative(sound =>
        {
            sound.getOpenState(out OPENSTATE state, out uint bufferedPercent, out bool isStarving, out bool isDiskBusy).ThrowIfNotOk();
            return new SoundOpenStates((SoundOpenState)state, bufferedPercent, isStarving, isDiskBusy);
        });

        public int priority
        {
            get => UseNative(sound =>
            {
                sound.getDefaults(out _, out int priority).ThrowIfNotOk();
                return priority;
            });
            set => UseNative(sound => sound.setDefaults(frequency, value).ThrowIfNotOk());
        }

        public float minDistance
        {
            get => Get3DMinMaxDistance().min;
            set => UseNative(sound =>
            {
                sound.get3DMinMaxDistance(out _, out float maxDistance).ThrowIfNotOk();
                sound.set3DMinMaxDistance(value, maxDistance).ThrowIfNotOk();
            });
        }

        public float maxDistance
        {
            get => Get3DMinMaxDistance().max;
            set => UseNative(sound =>
            {
                sound.get3DMinMaxDistance(out float minDistance, out _).ThrowIfNotOk();
                sound.set3DMinMaxDistance(minDistance, value).ThrowIfNotOk();
            });
        }

        public float insideConeAngle
        {
            get => Get3DConeSettings().insideAngle;
            set => UseNative(sound =>
            {
                sound.get3DConeSettings(out _, out float outsideAngle, out float outsideVolume).ThrowIfNotOk();
                sound.set3DConeSettings(value, outsideAngle, outsideVolume).ThrowIfNotOk();
            });
        }

        public float outsideConeAngle
        {
            get => Get3DConeSettings().outsideAngle;
            set => UseNative(sound =>
            {
                sound.get3DConeSettings(out float insideAngle, out _, out float outsideVolume).ThrowIfNotOk();
                sound.set3DConeSettings(insideAngle, value, outsideVolume).ThrowIfNotOk();
            });
        }

        public float outsideConeVolume
        {
            get => Get3DConeSettings().outsideVolume;
            set => UseNative(sound =>
            {
                sound.get3DConeSettings(out float insideAngle, out float outsideAngle, out _).ThrowIfNotOk();
                sound.set3DConeSettings(insideAngle, outsideAngle, value).ThrowIfNotOk();
            });
        }

        public int loopCount
        {
            get => UseNative(sound =>
            {
                sound.getLoopCount(out int loopCount).ThrowIfNotOk();
                return loopCount;
            });
            set => UseNative(sound => sound.setLoopCount(value).ThrowIfNotOk());
        }

        public uint loopStartSample
        {
            get => GetLoopPoints().start;
            set => UseNative(sound =>
            {
                sound.getLoopPoints(out _, TIMEUNIT.PCM, out uint loopEnd, TIMEUNIT.PCM).ThrowIfNotOk();
                sound.setLoopPoints(value, TIMEUNIT.PCM, loopEnd, TIMEUNIT.PCM).ThrowIfNotOk();
            });
        }

        public uint loopEndSample
        {
            get => GetLoopPoints().end;
            set => UseNative(sound =>
            {
                sound.getLoopPoints(out uint loopStart, TIMEUNIT.PCM, out _, TIMEUNIT.PCM).ThrowIfNotOk();
                sound.setLoopPoints(loopStart, TIMEUNIT.PCM, value, TIMEUNIT.PCM).ThrowIfNotOk();
            });
        }

        public IntPtr userData
        {
            get => UseNative(sound =>
            {
                sound.getUserData(out IntPtr userData).ThrowIfNotOk();
                return userData;
            });
            set => UseNative(sound => sound.setUserData(value).ThrowIfNotOk());
        }

        (float min, float max) Get3DMinMaxDistance() => UseNative(sound =>
        {
            sound.get3DMinMaxDistance(out float minDistance, out float maxDistance).ThrowIfNotOk();
            return (minDistance, maxDistance);
        });

        (float insideAngle, float outsideAngle, float outsideVolume) Get3DConeSettings() => UseNative(sound =>
        {
            sound.get3DConeSettings(out float insideAngle, out float outsideAngle, out float outsideVolume).ThrowIfNotOk();
            return (insideAngle, outsideAngle, outsideVolume);
        });

        (uint start, uint end) GetLoopPoints() => UseNative(sound =>
        {
            sound.getLoopPoints(out uint loopStart, TIMEUNIT.PCM, out uint loopEnd, TIMEUNIT.PCM).ThrowIfNotOk();
            return (loopStart, loopEnd);
        });
    }
}
