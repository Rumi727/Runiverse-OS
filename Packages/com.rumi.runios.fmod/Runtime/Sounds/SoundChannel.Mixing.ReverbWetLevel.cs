#nullable enable
namespace RuniOS.Sounds
{
    public sealed partial class SoundChannel
    {
        /// <summary>
        /// Provides indexed access to this channel's reverb wet/send levels.<br/>
        /// 이 채널의 리버브 웻/센드 레벨에 인덱스로 접근할 수 있게 합니다.
        /// </summary>
        public sealed class ReverbWetLevel
        {
            readonly SoundChannel channel;

            internal ReverbWetLevel(SoundChannel channel) => this.channel = channel;

            /// <summary>
            /// Gets or sets the wet/send level for the specified reverb <paramref name="instance"/>.<br/>
            /// 지정된 리버브 <paramref name="instance"/>의 웻/센드 레벨을 가져오거나 설정합니다.
            /// </summary>
            /// <param name="instance">
            /// The FMOD reverb instance index.<br/>
            /// FMOD 리버브 인스턴스 인덱스입니다.
            /// </param>
            public float this[int instance]
            {
                get
                {
                    channel.native.getReverbProperties(instance, out float wet);
                    return wet;
                }
                set => channel.native.setReverbProperties(instance, value);
            }
        }
    }
}
