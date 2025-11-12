using System;

namespace RuniOS.Editor.UIElements
{
    [Flags]
    public enum CompositeConfig
    {
        /// <summary>
        /// 아무런 설정도 적용하지 않습니다.
        /// </summary>
        none = 0,
        /// <summary>
        /// 내부 필드에 USS 클래스를 자동으로 추가합니다.
        /// </summary>
        includeCompositeUSS = 1 << 0,
        /// <summary>
        /// 내부 필드를 하나의 큰 필드로 간주하게 합니다. (프리팹 UI에 영향을 줍니다.)
        /// </summary>
        compositedField = 1 << 1
    }
}