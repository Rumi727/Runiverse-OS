#nullable enable
using FMOD;
using FMODUnity;
using RuniOS.Sounds.Processing;

namespace RuniOS.Sounds
{
    public sealed partial class SoundChannel
    {
        /// <summary>
        /// Gets or sets the position and velocity used for 3D spatial audio processing.<br/>
        /// 3D 공간 오디오 처리에 사용할 위치와 속도를 가져오거나 설정합니다.
        /// </summary>
        public AudioSpatialState spatialState
        {
            get
            {
                native.get3DAttributes(out VECTOR position, out VECTOR velocity).ThrowIfNotOk();
                return new AudioSpatialState(position.ToUnityVector(), velocity.ToUnityVector());
            }
            set
            {
                VECTOR position = value.position.ToFMODVector();
                VECTOR velocity = value.velocity.ToFMODVector();
                native.set3DAttributes(ref position, ref velocity).ThrowIfNotOk();
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
                native.get3DMinMaxDistance(out float minimum, out _).ThrowIfNotOk();
                return minimum;
            }
            set
            {
                native.get3DMinMaxDistance(out _, out float maximum).ThrowIfNotOk();
                native.set3DMinMaxDistance(value, maximum.Clamp(value)).ThrowIfNotOk();
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
                native.get3DMinMaxDistance(out _, out float maximum).ThrowIfNotOk();
                return maximum;
            }
            set
            {
                native.get3DMinMaxDistance(out float minimum, out _).ThrowIfNotOk();
                native.set3DMinMaxDistance(minimum, value.Clamp(minimum)).ThrowIfNotOk();
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
                native.getMode(out MODE mode).ThrowIfNotOk();
                return GetRolloffMode(mode);
            }
            set
            {
                native.getMode(out MODE mode).ThrowIfNotOk();
                mode &= ~(MODE._3D_INVERSEROLLOFF | MODE._3D_LINEARROLLOFF | MODE._3D_LINEARSQUAREROLLOFF | MODE._3D_INVERSETAPEREDROLLOFF | MODE._3D_CUSTOMROLLOFF);
                mode |= GetFMODRolloffMode(value);
                native.setMode(mode).ThrowIfNotOk();
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
                native.get3DLevel(out float level).ThrowIfNotOk();
                return level;
            }
            set => native.set3DLevel(value.Clamp01()).ThrowIfNotOk();
        }

        /// <summary>
        /// Gets or sets the Doppler effect level.<br/>
        /// 도플러 효과 수준을 가져오거나 설정합니다.
        /// </summary>
        public float dopplerLevel
        {
            get
            {
                native.get3DDopplerLevel(out float level).ThrowIfNotOk();
                return level;
            }
            set => native.set3DDopplerLevel(value.Clamp(0, 5)).ThrowIfNotOk();
        }

        /// <summary>
        /// Gets or sets the stereo spread angle in degrees.<br/>
        /// 스테레오 확산 각도를 도 단위로 가져오거나 설정합니다.
        /// </summary>
        public float spread
        {
            get
            {
                native.get3DSpread(out float spread).ThrowIfNotOk();
                return spread;
            }
            set => native.set3DSpread(value.Clamp(0, 360)).ThrowIfNotOk();
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
            native.get3DConeSettings(out float insideAngle, out float outsideAngle, out float outsideVolume).ThrowIfNotOk();
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
            native.set3DConeSettings(insideAngle, outsideAngle, outsideVolume).ThrowIfNotOk();

        /// <summary>
        /// Gets or sets the 3D cone orientation.<br/>
        /// 3D 콘 방향을 가져오거나 설정합니다.
        /// </summary>
        public Vector3 coneOrientation
        {
            get
            {
                native.get3DConeOrientation(out VECTOR orientation).ThrowIfNotOk();
                return orientation.ToUnityVector();
            }
            set
            {
                VECTOR orientation = value.ToFMODVector();
                native.set3DConeOrientation(ref orientation).ThrowIfNotOk();
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
            native.get3DOcclusion(out float direct, out float reverb).ThrowIfNotOk();
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
        public void SetOcclusion(float direct, float reverb) => native.set3DOcclusion(direct.Clamp01(), reverb.Clamp01());
    }
}
