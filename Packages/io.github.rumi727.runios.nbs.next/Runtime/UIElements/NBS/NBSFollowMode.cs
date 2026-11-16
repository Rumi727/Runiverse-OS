#nullable enable
namespace RuniOS.UIElements.NBS
{
    /// <summary>
    /// How <see cref="NBSNoteGridView"/> scrolls to keep the playhead in view while playing, like the "Follow marker when playing" option of Note Block Studio.<br/>
    /// 재생 중 <see cref="NBSNoteGridView"/>가 플레이헤드를 화면에 두기 위해 스크롤하는 방식입니다. Note Block Studio의 "재생 시 마커 따라가기" 옵션과 같습니다.
    /// </summary>
    public enum NBSFollowMode
    {
        /// <summary>
        /// Does not scroll.<br/>
        /// 스크롤하지 않습니다.
        /// </summary>
        none,

        /// <summary>
        /// Keeps the playhead near the center, scrolling one whole tick at a time.<br/>
        /// 플레이헤드를 가운데 근처에 두고 한 틱 단위로 스크롤합니다.
        /// </summary>
        tickByTick,

        /// <summary>
        /// Turns to the next page when the playhead reaches the right edge, starting the page at the playhead.<br/>
        /// 플레이헤드가 오른쪽 끝에 닿으면 플레이헤드에서 시작하는 다음 페이지로 넘깁니다.
        /// </summary>
        pageByPage,

        /// <summary>
        /// Keeps the playhead exactly at the center, scrolling smoothly every frame.<br/>
        /// 플레이헤드를 정확히 가운데에 두고 매 프레임 부드럽게 스크롤합니다.
        /// </summary>
        frameByFrame
    }
}
