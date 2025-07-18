#nullable enable
using UnityEngine.UIElements;

namespace RuniEngine
{
    /// <summary>
    /// UI Toolkit과 관련된 유틸리티 함수들을 제공하는 정적 클래스입니다.
    /// </summary>
    public static class UIToolkitUtility
    {
#if UNITY_EDITOR
        /// <summary>
        /// 지정된 <see cref="VisualElement"/>가 런타임 패널에 속하는지 여부를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 에디터에서는 <see cref="IRuntimePanel"/>에 속하고 <see cref="Kernel.isPlaying"/>이 true일 때만 런타임 패널로 간주합니다.
        /// 빌드된 애플리케이션에서는 항상 true를 반환합니다.
        /// </remarks>
        /// <param name="visualElement">확인할 <see cref="VisualElement"/>입니다.</param>
        /// <returns>지정된 <see cref="VisualElement"/>가 런타임 패널에 속하면 true, 그렇지 않으면 false를 반환합니다.</returns>
        public static bool IsRuntimePanel(this VisualElement visualElement) => visualElement.panel is IRuntimePanel && Kernel.isPlaying;

        /// <summary>
        /// 지정된 <see cref="VisualElement"/>가 에디터 패널에 속하는지 여부를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 에디터에서는 <see cref="IRuntimePanel"/>에 속하지 않거나 <see cref="Kernel.isPlaying"/>이 false일 때만 에디터 패널로 간주합니다.
        /// 빌드된 애플리케이션에서는 항상 false를 반환합니다.
        /// </remarks>
        /// <param name="visualElement">확인할 <see cref="VisualElement"/>입니다.</param>
        /// <returns>지정된 <see cref="VisualElement"/>가 에디터 패널에 속하면 true, 그렇지 않으면 false를 반환합니다.</returns>
        public static bool IsEditorPanel(this VisualElement visualElement) => visualElement.panel is not IRuntimePanel || !Kernel.isPlaying;
#else
#pragma warning disable IDE0060 // 사용하지 않는 매개 변수를 제거하세요.
        /// <summary>
        /// 지정된 <see cref="VisualElement"/>가 런타임 패널에 속하는지 여부를 반환합니다.
        /// 빌드된 애플리케이션에서는 항상 true를 반환합니다.
        /// </summary>
        /// <param name="visualElement">확인할 <see cref="VisualElement"/>입니다.</param>
        /// <returns>항상 true를 반환합니다.</returns>
        public static bool IsRuntimePanel(this VisualElement visualElement) => true;

        /// <summary>
        /// 지정된 <see cref="VisualElement"/>가 에디터 패널에 속하는지 여부를 반환합니다.
        /// 빌드된 애플리케이션에서는 항상 false를 반환합니다.
        /// </summary>
        /// <param name="visualElement">확인할 <see cref="VisualElement"/>입니다.</param>
        /// <returns>항상 false를 반환합니다.</returns>
        public static bool IsEditorPanel(this VisualElement visualElement) => false;
#pragma warning restore IDE0060 // 사용하지 않는 매개 변수를 제거하세요.
#endif
    }
}
