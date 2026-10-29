#nullable enable
namespace RuniOS.Texts
{
    /// <summary>
    /// Serves as the base type for bindings that rebuild a text whenever a change source it depends on changes.<br/>
    /// The change sources are those reported by the latest build, so structural changes are tracked by rebuilding.
    /// <br/><br/>
    /// 텍스트가 의존하는 변경 원인이 바뀔 때마다 텍스트를 다시 빌드하는 바인딩의 기본 타입 역할을 합니다.<br/>
    /// 변경 원인은 마지막 빌드가 보고한 것이므로, 구조 변경은 다시 빌드하면서 추적됩니다.
    /// </summary>
    /// <remarks>
    /// Change sources keep a reference to this binding until <see cref="Dispose"/> is called.<br/>
    /// Rebuilds run on the thread that raised the change.
    /// <br/><br/>
    /// <see cref="Dispose"/>를 호출하기 전까지 변경 원인이 이 바인딩을 참조합니다.<br/>
    /// 다시 빌드는 변경을 발생시킨 스레드에서 실행됩니다.
    /// </remarks>
    public abstract class TextBinding : IDisposable
    {
        /// <summary>
        /// Initializes a binding for the specified text.<br/>
        /// Derived types call <see cref="Rebuild"/> after their own initialization to perform the first build.
        /// <br/><br/>
        /// 지정된 텍스트에 대한 바인딩을 초기화합니다.<br/>
        /// 파생 타입은 자체 초기화를 마친 뒤 <see cref="Rebuild"/>를 호출하여 첫 빌드를 수행합니다.
        /// </summary>
        /// <param name="text">
        /// The text to build.<br/>
        /// 빌드할 텍스트입니다.
        /// </param>
        protected TextBinding(Text text) => this.text = text;

        List<ITextChangeSource> subscribed = [];
        List<ITextChangeSource> collected = [];

        /// <summary>
        /// Gets the text built by this binding.<br/>
        /// 이 바인딩이 빌드하는 텍스트를 가져옵니다.
        /// </summary>
        public Text text { get; }

        /// <summary>
        /// Builds the text through <see cref="Update"/> and updates the subscriptions to match the reported change sources.<br/>
        /// <see cref="Update"/>로 텍스트를 빌드하고, 보고된 변경 원인에 맞게 구독을 갱신합니다.
        /// </summary>
        protected void Rebuild()
        {
            collected.Clear();
            Update(collected);

            for (int i = 0; i < subscribed.Count; i++)
            {
                if (!collected.Contains(subscribed[i]))
                    subscribed[i].onChanged -= Rebuild;
            }

            for (int i = 0; i < collected.Count; i++)
            {
                if (!subscribed.Contains(collected[i]))
                    collected[i].onChanged += Rebuild;
            }

            (subscribed, collected) = (collected, subscribed);
        }

        /// <summary>
        /// Builds <see cref="text"/>, reports the change sources the output depends on to <paramref name="dependencies"/>, and passes the output to its target.<br/>
        /// <see cref="text"/>를 빌드하고, 출력이 의존하는 변경 원인을 <paramref name="dependencies"/>에 보고한 뒤 출력을 대상에 전달합니다.
        /// </summary>
        /// <param name="dependencies">
        /// The collection that receives the change sources the output depends on.<br/>
        /// 출력이 의존하는 변경 원인을 받을 컬렉션입니다.
        /// </param>
        protected abstract void Update(ICollection<ITextChangeSource> dependencies);

        /// <summary>
        /// Unsubscribes from every change source reported by the latest build.<br/>
        /// 마지막 빌드가 보고한 모든 변경 원인의 구독을 해제합니다.
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < subscribed.Count; i++)
                subscribed[i].onChanged -= Rebuild;

            subscribed.Clear();
        }
    }
}
