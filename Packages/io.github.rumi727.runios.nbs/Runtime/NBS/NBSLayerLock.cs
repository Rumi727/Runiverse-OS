#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Identifies the lock byte stored for an NBS layer.<br/>
    /// NBS 레이어에 저장된 잠금 바이트를 식별합니다.
    /// </summary>
    public enum NBSLayerLock : byte
    {
        /// <summary>
        /// The layer plays normally.<br/>
        /// 레이어가 정상적으로 재생됩니다.
        /// </summary>
        none = 0,

        /// <summary>
        /// The layer is locked and its notes are muted.<br/>
        /// 레이어가 잠겨 있으며 노트가 무음 처리됩니다.
        /// </summary>
        locked = 1,

        /// <summary>
        /// The layer is soloed. When any layer is soloed, only soloed layers play.<br/>
        /// 레이어가 솔로 상태입니다. 솔로 레이어가 하나라도 있으면 솔로 레이어만 재생됩니다.
        /// </summary>
        solo = 2
    }
}
