#nullable enable
namespace RuniOS.Texts.Builders
{
    /// <summary>
    /// Provides a thread-local reusable <see cref="TextStyleState"/> instance.<br/>
    /// 스레드 로컬 재사용 <see cref="TextStyleState"/> 인스턴스를 제공합니다.
    /// </summary>
    public static class TextStyleStateCache
    {
        [ThreadStatic]
        static TextStyleState? cachedInstance;

        /// <summary>
        /// Gets a cleared style state instance for the current thread.<br/>
        /// 현재 스레드에서 사용할 초기화된 스타일 상태 인스턴스를 가져옵니다.
        /// </summary>
        /// <returns>
        /// A style state instance ready for use.<br/>
        /// 사용할 준비가 된 스타일 상태 인스턴스를 반환합니다.
        /// </returns>
        public static TextStyleState Acquire()
        {
            TextStyleState? state = cachedInstance;
            if (state != null)
            {
                cachedInstance = null;
                state.Clear();

                return state;
            }

            return new TextStyleState();
        }

        /// <summary>
        /// Clears and stores a style state instance for reuse on the current thread.<br/>
        /// 스타일 상태 인스턴스를 지우고 현재 스레드에서 재사용하도록 저장합니다.
        /// </summary>
        /// <param name="state">
        /// The style state instance to release.<br/>
        /// 해제할 스타일 상태 인스턴스입니다.
        /// </param>
        public static void Release(TextStyleState state)
        {
            state.Clear();
            cachedInstance = state;
        }
    }
}
