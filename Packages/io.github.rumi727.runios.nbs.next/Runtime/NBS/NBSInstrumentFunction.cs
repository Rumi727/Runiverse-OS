#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Identifies the special behavior that Note Block Studio assigns to a custom instrument by its name.<br/>
    /// Note Block Studio가 이름으로 커스텀 악기에 부여하는 특수 동작을 식별합니다.
    /// </summary>
    public enum NBSInstrumentFunction
    {
        /// <summary>
        /// The instrument plays its sound.<br/>
        /// 악기가 소리를 재생합니다.
        /// </summary>
        none,

        /// <summary>
        /// <c>Tempo Changer</c>: sets the ticks per second to <c>floor(|pitch|) / 15</c>.<br/>
        /// <c>Tempo Changer</c>: 초당 tick 수를 <c>floor(|pitch|) / 15</c>로 설정합니다.
        /// </summary>
        tempoChanger,

        /// <summary>
        /// <c>Toggle Rainbow</c>: toggles the rainbow accent effect.<br/>
        /// <c>Toggle Rainbow</c>: 무지개 강조 효과를 전환합니다.
        /// </summary>
        toggleRainbow,

        /// <summary>
        /// <c>Sound Stopper</c>: stops the sounds that are playing on the target layers.<br/>
        /// <c>Sound Stopper</c>: 대상 레이어에서 재생 중인 소리를 멈춥니다.
        /// </summary>
        soundStopper,

        /// <summary>
        /// <c>Show Save Popup</c>: shows the "Song saved" popup.<br/>
        /// <c>Show Save Popup</c>: "Song saved" 팝업을 표시합니다.
        /// </summary>
        showSavePopup,

        /// <summary>
        /// <c>Change Color to #RRGGBB</c>: changes the accent color.<br/>
        /// <c>Change Color to #RRGGBB</c>: 강조 색을 변경합니다.
        /// </summary>
        changeColor,

        /// <summary>
        /// <c>Toggle Background Accent</c>: toggles the background accent effect.<br/>
        /// <c>Toggle Background Accent</c>: 배경 강조 효과를 전환합니다.
        /// </summary>
        toggleBackgroundAccent
    }
}
