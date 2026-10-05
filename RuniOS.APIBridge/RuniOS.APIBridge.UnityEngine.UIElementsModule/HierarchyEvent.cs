#pragma warning disable CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace RuniOS.APIBridge.UnityEngine.UIElements
{
    public delegate void HierarchyEventBridge(VisualElement ve, HierarchyChangeTypeBridge changeType, IReadOnlyList<VisualElement>? additionalContext = null);
}
#pragma warning restore CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.