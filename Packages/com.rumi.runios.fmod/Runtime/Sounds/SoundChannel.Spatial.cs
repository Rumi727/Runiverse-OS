#nullable enable
using FMOD;
using FMODUnity;
using RuniOS.Sounds.Processing;
using System.Threading;

namespace RuniOS.Sounds
{
    public sealed partial class SoundChannel
    {
        readonly ReaderWriterLockSlim spatialLock = new();

        /// <summary>
        /// Gets or sets the position and velocity used for 3D spatial audio processing.<br/>
        /// 3D 공간 오디오 처리에 사용할 위치와 속도를 가져오거나 설정합니다.
        /// </summary>
        public AudioSpatialState spatialState
        {
            get
            {
                native.get3DAttributes(out VECTOR position, out VECTOR velocity).ThrowIfNotOkOfChannel();
                return new AudioSpatialState(position.ToUnityVector(), velocity.ToUnityVector());
            }
            set
            {
                VECTOR position = value.position.ToFMODVector();
                VECTOR velocity = value.velocity.ToFMODVector();

                ValidateVector(nameof(value.position), position);
                ValidateVector(nameof(value.velocity), velocity);

                native.set3DAttributes(ref position, ref velocity).ThrowIfNotOkOfChannel();
            }
        }

        static void ValidateVector(string name, VECTOR value)
        {
            if (IsValid(value.x) && IsValid(value.y) && IsValid(value.z))
                return;

            throw new ArgumentException
            (
                $"Channel {name} contains an invalid float: ({value.x}, {value.y}, {value.z}).",
                nameof(value)
            );
        }

        static bool IsValid(float value) => value == 0 || float.IsNormal(value);

        /// <summary>
        /// Gets or sets the minimum and maximum distances for 3D attenuation together.<br/>
        /// 3D 감쇠에 사용할 최소 및 최대 거리를 함께 가져오거나 설정합니다.
        /// </summary>
        public (float min, float max) minMaxDistance
        {
            get
            {
                spatialLock.EnterReadLock();

                try
                {
                    native.get3DMinMaxDistance(out float min, out float max).ThrowIfNotOkOfChannel();
                    return (min, max);
                }
                finally
                {
                    spatialLock.ExitReadLock();
                }
            }
            set
            {
                spatialLock.EnterWriteLock();

                try
                {
                    native.set3DMinMaxDistance(value.min, value.max.Clamp(value.min)).ThrowIfNotOkOfChannel();
                }
                finally
                {
                    spatialLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum distance for 3D attenuation.<br/>
        /// 3D 감쇠에 사용할 최소 거리를 가져오거나 설정합니다.
        /// </summary>
        public float minDistance
        {
            get
            {
                spatialLock.EnterReadLock();

                try
                {
                    native.get3DMinMaxDistance(out float minimum, out _).ThrowIfNotOkOfChannel();
                    return minimum;
                }
                finally
                {
                    spatialLock.ExitReadLock();
                }
            }
            set
            {
                spatialLock.EnterWriteLock();

                try
                {
                    native.get3DMinMaxDistance(out _, out float maximum).ThrowIfNotOkOfChannel();
                    native.set3DMinMaxDistance(value, maximum.Clamp(value)).ThrowIfNotOkOfChannel();
                }
                finally
                {
                    spatialLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum distance for 3D attenuation.<br/>
        /// 3D 감쇠에 사용할 최대 거리를 가져오거나 설정합니다.
        /// </summary>
        public float maxDistance
        {
            get
            {
                spatialLock.EnterReadLock();

                try
                {
                    native.get3DMinMaxDistance(out _, out float maximum).ThrowIfNotOkOfChannel();
                    return maximum;
                }
                finally
                {
                    spatialLock.ExitReadLock();
                }
            }
            set
            {
                spatialLock.EnterWriteLock();

                try
                {
                    native.get3DMinMaxDistance(out float minimum, out _).ThrowIfNotOkOfChannel();
                    native.set3DMinMaxDistance(minimum, value.Clamp(minimum)).ThrowIfNotOkOfChannel();
                }
                finally
                {
                    spatialLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets the 3D distance attenuation curve.<br/>
        /// 3D 거리 감쇠 곡선을 가져오거나 설정합니다.
        /// </summary>
        public SoundRolloffMode rolloffMode
        {
            get
            {
                spatialLock.EnterReadLock();

                try
                {
                    modeLock.EnterReadLock();

                    try
                    {
                        native.getMode(out MODE mode).ThrowIfNotOkOfChannel();
                        return GetRolloffMode(mode);
                    }
                    finally
                    {
                        modeLock.ExitReadLock();
                    }
                }
                finally
                {
                    spatialLock.ExitReadLock();
                }
            }
            set
            {
                spatialLock.EnterWriteLock();

                try
                {
                    modeLock.EnterWriteLock();

                    try
                    {
                        native.getMode(out MODE mode).ThrowIfNotOkOfChannel();
                        mode &= ~(MODE._3D_INVERSEROLLOFF | MODE._3D_LINEARROLLOFF | MODE._3D_LINEARSQUAREROLLOFF | MODE._3D_INVERSETAPEREDROLLOFF | MODE._3D_CUSTOMROLLOFF);
                        mode |= GetFMODRolloffMode(value);
                        native.setMode(mode).ThrowIfNotOkOfChannel();
                    }
                    finally
                    {
                        modeLock.ExitWriteLock();
                    }
                }
                finally
                {
                    spatialLock.ExitWriteLock();
                }
            }
        }

        static SoundRolloffMode GetRolloffMode(MODE mode)
        {
            if (mode.HasFlag(MODE._3D_LINEARROLLOFF))
                return SoundRolloffMode.linear;
            if (mode.HasFlag(MODE._3D_LINEARSQUAREROLLOFF))
                return SoundRolloffMode.linearSquared;
            if (mode.HasFlag(MODE._3D_INVERSETAPEREDROLLOFF))
                return SoundRolloffMode.inverseTapered;

            return SoundRolloffMode.inverse;
        }

        static MODE GetFMODRolloffMode(SoundRolloffMode value) => value switch
        {
            SoundRolloffMode.inverse => MODE._3D_INVERSEROLLOFF,
            SoundRolloffMode.linear => MODE._3D_LINEARROLLOFF,
            SoundRolloffMode.linearSquared => MODE._3D_LINEARSQUAREROLLOFF,
            SoundRolloffMode.inverseTapered => MODE._3D_INVERSETAPEREDROLLOFF,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        /// <summary>
        /// Gets or sets the blend between 2D and 3D spatialization.<br/>
        /// 2D와 3D 공간화 사이의 블렌드를 가져오거나 설정합니다.
        /// </summary>
        public float spatialBlend
        {
            get
            {
                native.get3DLevel(out float level).ThrowIfNotOkOfChannel();
                return level;
            }
            set => native.set3DLevel(value.Clamp01()).ThrowIfNotOkOfChannel();
        }

        /// <summary>
        /// Gets or sets the Doppler effect level.<br/>
        /// 도플러 효과 수준을 가져오거나 설정합니다.
        /// </summary>
        public float dopplerLevel
        {
            get
            {
                native.get3DDopplerLevel(out float level).ThrowIfNotOkOfChannel();
                return level;
            }
            set => native.set3DDopplerLevel(value.Clamp(0, 5)).ThrowIfNotOkOfChannel();
        }

        /// <summary>
        /// Gets or sets the stereo spread angle in degrees.<br/>
        /// 스테레오 확산 각도를 도 단위로 가져오거나 설정합니다.
        /// </summary>
        public float spread
        {
            get
            {
                native.get3DSpread(out float spread).ThrowIfNotOkOfChannel();
                return spread;
            }
            set => native.set3DSpread(value.Clamp(0, 360)).ThrowIfNotOkOfChannel();
        }

        /// <summary>
        /// Gets the angles and outside volume of this channel's 3D sound cone.<br/>
        /// 이 채널의 3D 사운드 콘 각도와 외부 볼륨을 가져옵니다.
        /// </summary>
        /// <returns>
        /// The inside angle, outside angle, and volume outside the cone.<br/>
        /// 내부 각도, 외부 각도 및 콘 외부 볼륨을 반환합니다.
        /// </returns>
        public (float insideAngle, float outsideAngle, float outsideVolume) GetConeSettings()
        {
            native.get3DConeSettings(out float insideAngle, out float outsideAngle, out float outsideVolume).ThrowIfNotOkOfChannel();
            return (insideAngle, outsideAngle, outsideVolume);
        }

        /// <summary>
        /// Sets the angles and outside volume of this channel's 3D sound cone.<br/>
        /// 이 채널의 3D 사운드 콘 각도와 외부 볼륨을 설정합니다.
        /// </summary>
        /// <param name="insideAngle">
        /// The unattenuated inner-cone angle in degrees.<br/>
        /// 감쇠되지 않는 내부 콘 각도(도)입니다.
        /// </param>
        /// <param name="outsideAngle">
        /// The outer-cone angle in degrees.<br/>
        /// 외부 콘 각도(도)입니다.
        /// </param>
        /// <param name="outsideVolume">
        /// The volume outside the outer cone.<br/>
        /// 외부 콘 밖에서의 볼륨입니다.
        /// </param>
        public void SetConeSettings(float insideAngle, float outsideAngle, float outsideVolume) =>
            native.set3DConeSettings(insideAngle, outsideAngle, outsideVolume).ThrowIfNotOkOfChannel();

        /// <summary>
        /// Gets or sets the 3D cone orientation.<br/>
        /// 3D 콘 방향을 가져오거나 설정합니다.
        /// </summary>
        public Vector3 coneOrientation
        {
            get
            {
                native.get3DConeOrientation(out VECTOR orientation).ThrowIfNotOkOfChannel();
                return orientation.ToUnityVector();
            }
            set
            {
                VECTOR orientation = value.ToFMODVector();
                native.set3DConeOrientation(ref orientation).ThrowIfNotOkOfChannel();
            }
        }

        /// <summary>
        /// Gets the direct and reverb occlusion factors.<br/>
        /// 직접음 및 리버브 오클루전 계수를 가져옵니다.
        /// </summary>
        /// <returns>
        /// The direct-path and reverb-path occlusion factors.<br/>
        /// 직접 경로 및 리버브 경로 오클루전 계수를 반환합니다.
        /// </returns>
        public (float direct, float reverb) GetOcclusion()
        {
            native.get3DOcclusion(out float direct, out float reverb).ThrowIfNotOkOfChannel();
            return (direct, reverb);
        }

        /// <summary>
        /// Sets the direct and reverb occlusion factors.<br/>
        /// 직접음 및 리버브 오클루전 계수를 설정합니다.
        /// </summary>
        /// <remarks>
        /// <see cref="SoundSystem.main"/> uses <c>INITFLAGS.NORMAL</c>, so its built-in channel occlusion path is not enabled.<br/>
        /// For manual direct-signal occlusion, create <see cref="LowPassDSP"/> through <see cref="SoundSystem.CreateDSP{LowPassDSP}"/> and attach it with <see cref="AddDSP(Processing.DSP, DSPIndex)"/>.
        /// <br/><br/>
        /// <see cref="SoundSystem.main"/>은 <c>INITFLAGS.NORMAL</c>을 사용하므로 내장 채널 오클루전 경로를 활성화하지 않습니다.<br/>
        /// 직접음 오클루전을 수동으로 처리하려면 <see cref="SoundSystem.CreateDSP{LowPassDSP}"/>로 <see cref="LowPassDSP"/>를 생성하고 <see cref="AddDSP(Processing.DSP, DSPIndex)"/>로 부착하세요.
        /// </remarks>
        /// <param name="direct">
        /// The direct-path occlusion factor.<br/>
        /// 직접 경로 오클루전 계수입니다.
        /// </param>
        /// <param name="reverb">
        /// The reverb-path occlusion factor.<br/>
        /// 리버브 경로 오클루전 계수입니다.
        /// </param>
        public void SetOcclusion(float direct, float reverb) => native.set3DOcclusion(direct.Clamp01(), reverb.Clamp01()).ThrowIfNotOkOfChannel();
    }
}
