using UnityEngine.UIElements;

namespace RuniOS
{
    public static class IMGUIUtility
    {
        // Patches/Runtime/UnityEngine.UIElements.UIElementsUtility.cs를 참고해주세요
        /// <summary>
        /// 현재 호출 스택에서 UI Toolkit의 <see cref="IMGUIContainer"/>를 통해 IMGUI 렌더링을 시작한 <see cref="IMGUIContainer"/>를 가져옵니다.<br/>
        /// 현재 스택에 해당 <see cref="IMGUIContainer"/>를 통한 IMGUI 호출이 없거나, IMGUI를 렌더링하게 한 <see cref="IMGUIContainer"/>가 없을 경우 <see langword="null"/>을 반환합니다.
        /// </summary>
        /// <remarks>
        /// 이 프로퍼티는 UI Toolkit의 <see cref="IMGUIContainer"/>를 통해 IMGUI 렌더링이 발생하는 특정 상황을 추적하고, 해당 컨테이너 인스턴스를 참조하기 위해 사용됩니다.<br/>
        /// 이는 IMGUI 기반의 레거시 코드가 UI Toolkit 환경과 상호작용할 때, 해당 IMGUI를 감싸고 있는 컨테이너에 접근하여 추가적인 로직을 적용하거나 상태를 관리하는 데 활용될 수 있습니다.
        /// <br/><br/>
        /// 이 프로퍼티는 Unity UI Toolkit의 `UIElementsUtility` 클래스에 대한 패치(Patches.UnityEngine.UIElements.UIElementsUtility.cs)를 통해 추가되었습니다.
        /// </remarks>
#if !UNITY_EDITOR && ENABLE_IL2CPP
        [System.Obsolete("IL2CPP environment is not supported.", true)]
#endif
        public static IMGUIContainer? currentIMGUIContainer { get; internal set; }

#if !UNITY_EDITOR && ENABLE_IL2CPP
        [System.Obsolete("IL2CPP environment is not supported.", true)]
#endif
        public static void UpdateContainerHeight(float height)
        {
            if (currentIMGUIContainer != null)
            {
                StyleLength lastHeight = currentIMGUIContainer.style.height;
                currentIMGUIContainer.style.height = new Length(height);
                currentIMGUIContainer.style.height = lastHeight;
            }
        }
    }
}