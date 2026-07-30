#nullable enable
namespace RuniOS.Sounds
{
    /// <summary>
    /// Selects the 3D distance attenuation curve for a sound channel.<br/>
    /// 사운드 채널의 3D 거리 감쇠 곡선을 선택합니다.
    /// </summary>
    public enum SoundRolloffMode
    {
        /// <summary>
        /// Uses inverse distance attenuation.<br/>
        /// 역거리 감쇠를 사용합니다.
        /// </summary>
        inverse,

        /// <summary>
        /// Uses linear distance attenuation.<br/>
        /// 선형 거리 감쇠를 사용합니다.
        /// </summary>
        linear,

        /// <summary>
        /// Uses squared linear distance attenuation.<br/>
        /// 제곱 선형 거리 감쇠를 사용합니다.
        /// </summary>
        linearSquared,

        /// <summary>
        /// Uses inverse attenuation near the source and fades to silence at the maximum distance.<br/>
        /// 소스 근처에서는 역거리 감쇠를 사용하고 최대 거리에서 무음으로 감쇠합니다.
        /// </summary>
        inverseTapered
    }
}
