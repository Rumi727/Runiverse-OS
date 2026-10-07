#nullable enable
using FMOD;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Specifies the current FMOD sound opening or streaming state.<br/>
    /// 현재 FMOD 사운드의 열기 또는 스트리밍 상태를 지정합니다.
    /// </summary>
    public enum SoundOpenState
    {
        /// <summary>
        /// The sound is ready for use.<br/>
        /// 사운드를 사용할 준비가 되었습니다.
        /// </summary>
        Ready = OPENSTATE.READY,

        /// <summary>
        /// The sound is loading.<br/>
        /// 사운드를 불러오는 중입니다.
        /// </summary>
        Loading = OPENSTATE.LOADING,

        /// <summary>
        /// Opening the sound failed.<br/>
        /// 사운드를 여는 데 실패했습니다.
        /// </summary>
        Error = OPENSTATE.ERROR,

        /// <summary>
        /// The sound is connecting to its source.<br/>
        /// 사운드 소스에 연결하는 중입니다.
        /// </summary>
        Connecting = OPENSTATE.CONNECTING,

        /// <summary>
        /// The stream is buffering.<br/>
        /// 스트림을 버퍼링하는 중입니다.
        /// </summary>
        Buffering = OPENSTATE.BUFFERING,

        /// <summary>
        /// The stream is seeking.<br/>
        /// 스트림 위치를 탐색하는 중입니다.
        /// </summary>
        Seeking = OPENSTATE.SEEKING,

        /// <summary>
        /// The stream is playing.<br/>
        /// 스트림을 재생 중입니다.
        /// </summary>
        Playing = OPENSTATE.PLAYING,

        /// <summary>
        /// The stream is changing its playback position.<br/>
        /// 스트림 재생 위치를 변경하는 중입니다.
        /// </summary>
        SetPosition = OPENSTATE.SETPOSITION
    }
}