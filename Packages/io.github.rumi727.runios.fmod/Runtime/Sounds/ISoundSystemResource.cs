#nullable enable
namespace RuniOS.Sounds
{
    /// <summary>
    /// Represents an unmanaged resource owned by a <see cref="SoundSystem"/>.<br/>
    /// <see cref="SoundSystem"/>이 소유하는 비관리 리소스를 나타냅니다.
    /// </summary>
    public interface ISoundSystemResource
    {
        /// <summary>
        /// Gets the sound system that owns this resource.<br/>
        /// 이 리소스를 소유하는 사운드 시스템을 가져옵니다.
        /// </summary>
        SoundSystem system { get; }

        /// <summary>
        /// Releases unmanaged resources held by this instance.<br/>
        /// 이 인스턴스가 보유한 비관리 리소스를 해제합니다.
        /// </summary>
        /// <remarks>
        /// Implementations must tolerate repeated calls and perform native cleanup at most once.<br/>
        /// This method may run after the owning <see cref="SoundSystem"/> has started disposal, where failures are logged and do not stop final system release.<br/>
        /// Do not synchronously dispose the owning system or register this resource again while this method executes.
        /// <br/><br/>
        /// 구현은 반복 호출을 허용하고 네이티브 정리를 최대 한 번만 수행해야 합니다.<br/>
        /// 이 메서드는 소유 <see cref="SoundSystem"/>의 해제가 시작된 뒤에 호출될 수 있으며, 그 경우 실패는 기록되지만 최종 시스템 해제를 중단하지 않습니다.<br/>
        /// 이 메서드가 실행되는 동안 소유 시스템을 동기적으로 해제하거나 이 리소스를 다시 등록하면 안 됩니다.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when native cleanup fails.<br/>
        /// 네이티브 정리에 실패한 경우 발생합니다.
        /// </exception>
        protected internal void ReleaseUnmanagedResources();
    }
}
