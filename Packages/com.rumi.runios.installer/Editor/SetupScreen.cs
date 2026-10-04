#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.Editor.Installer
{
    /// <summary>
    /// Owns a Setup screen's scrolling content, header metadata, and lifecycle.<br/>
    /// Setup 화면의 스크롤 콘텐츠, header 정보, lifecycle을 소유합니다.
    /// </summary>
    /// <remarks>
    /// Concrete implementations need a <see langword="public"/> parameterless constructor for automatic discovery.<br/>
    /// 구체적인 구현은 자동 발견을 위해 <see langword="public"/> 매개변수 없는 생성자가 필요합니다.
    /// </remarks>
    public abstract class SetupScreen : VisualElement
    {
        protected SetupScreen(string title, int order) : this(title, order, true) { }

        protected SetupScreen(string title, int order, bool usesHeader) : this(title, order, usesHeader, true) { }

        protected SetupScreen(string title, int order, bool usesHeader, bool hasScrollView)
        {
            this.title = title;
            this.usesHeader = usesHeader;
            this.order = order;

            AddToClassList("runios-setup__content");
            if (usesHeader)
                AddToClassList("runios-setup__content--with-header");

            if (hasScrollView)
            {
                scrollView = new ScrollView();
                scrollView.AddToClassList("runios-setup__scroll-view");

                hierarchy.Add(scrollView);
            }
            else
                AddToClassList("runios-setup__content--without-scroll-view");
        }

        /// <summary>
        /// Gets the scrolling container to which child elements are added.<br/>
        /// 자식 요소를 추가할 스크롤 콘텐츠 컨테이너를 가져옵니다.
        /// </summary>
        public sealed override VisualElement contentContainer => scrollView?.contentContainer ?? this;

        public ScrollView? scrollView { get; } = null;

        /// <summary>
        /// Gets the header title.<br/>
        /// header 제목을 가져옵니다.
        /// </summary>
        public string title { get; }

        /// <summary>
        /// Gets the ascending display order of this screen.<br/>
        /// 이 화면의 오름차순 표시 순서를 가져옵니다.
        /// </summary>
        public int order { get; }

        /// <summary>
        /// Gets whether this screen reserves space for the header.<br/>
        /// 이 화면이 header 공간을 확보하는지 가져옵니다.
        /// </summary>
        public bool usesHeader { get; }

        /// <summary>
        /// Gets or sets the element whose bounds, excluding margins, the logo follows; <see langword="null"/> follows Header-Logo.<br/>
        /// 로고가 마진을 제외한 영역을 따라갈 요소를 가져오거나 설정합니다. <see langword="null"/>이면 Header-Logo를 따라갑니다.
        /// </summary>
        public virtual VisualElement? logoTarget => null;

        /// <summary>
        /// Called when this screen becomes the active screen of its owning Setup window.<br/>
        /// 이 화면이 자신을 소유한 Setup 창의 활성 화면이 될 때 호출됩니다.
        /// </summary>
        protected internal virtual void OnActivated() { }

        /// <summary>
        /// Called when this screen stops being the active screen of its owning Setup window.<br/>
        /// 이 화면이 자신을 소유한 Setup 창의 활성 화면이 아니게 될 때 호출됩니다.
        /// </summary>
        protected internal virtual void OnDeactivated() { }

        /// <summary>
        /// Refreshes this screen's text after creation and whenever the shared Installer locale changes.<br/>
        /// 화면 생성 후와 공유 Installer locale이 변경될 때 화면의 텍스트를 갱신합니다.
        /// </summary>
        protected internal virtual void OnLanguageChanged() { }

        /// <summary>
        /// Updates this screen on every editor update while it is owned by an enabled Setup window,
        /// including while it is inactive.<br/>
        /// 활성 상태가 아닌 경우를 포함하여, 활성화된 Setup 창에 소유되어 있는 동안
        /// 매 editor update마다 이 화면을 갱신합니다.
        /// </summary>
        /// <param name="time">
        /// Seconds elapsed since the editor started.<br/>
        /// Editor 시작 이후 경과한 초입니다.
        /// </param>
        /// <param name="deltaTime">
        /// Seconds elapsed since the previous update of the owning Setup window.<br/>
        /// 소유한 Setup 창의 이전 update 이후 경과한 초입니다.
        /// </param>
        protected internal virtual void OnUpdate(double time, float deltaTime) { }
    }
}
