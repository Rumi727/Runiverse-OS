#pragma warning disable CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace RuniOS.APIBridge.UnityEngine.UIElements
{
    public partial class BaseVisualElementPanelBridge
    {
        readonly Dictionary<HierarchyEventBridge, Stack<HierarchyEvent>> __registeredHierarchyChangedEvents = [];

        public event HierarchyEventBridge hierarchyChanged
        {
            add
            {
                HierarchyEvent method = Method;
                if (!__registeredHierarchyChangedEvents.TryGetValue(value, out var methods))
                {
                    methods = [];
                    __registeredHierarchyChangedEvents.Add(value, methods);
                }

                methods.Push(method);
                ((BaseVisualElementPanel)__instance).hierarchyChanged += method;

                void Method(VisualElement ve, HierarchyChangeType changeType, IReadOnlyList<VisualElement>? additionalContext = null) =>
                    value.Invoke(ve, (HierarchyChangeTypeBridge)changeType, additionalContext);
            }
            remove
            {
                if (!__registeredHierarchyChangedEvents.TryGetValue(value, out var methods) || methods.Count == 0)
                    return;

                HierarchyEvent method = methods.Pop();
                if (methods.Count == 0)
                    __registeredHierarchyChangedEvents.Remove(value);

                ((BaseVisualElementPanel)__instance).hierarchyChanged -= method;
            }
        }
    }
}
#pragma warning restore CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.