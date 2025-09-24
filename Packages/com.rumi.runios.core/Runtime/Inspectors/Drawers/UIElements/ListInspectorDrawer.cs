#nullable enable
using RuniOS.UIElements;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace RuniOS.Inspectors.Drawers.UIElements
{
    [CustomInspectorDrawer(typeof(IList))]
    public class ListInspectorDrawer : UIElementInspectorDrawer
    {
        public ListInspectorDrawer(IInspectorElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }
        public ListInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList, rootInspector) { }

        readonly ConditionalWeakTable<VisualElement, Action?> readActions = new();
        public override VisualElement Build()
        {
            if (inspectableList?.inspectionElementDisplayName == null)
                return new ListView();
            
            return new ListView(inspectableList) 
            { 
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                makeItem = () => new Inspector(rootInspector),
                bindItem = (visualElement, i) =>
                {
                    if (visualElement is not Inspector inspector)
                        return;
                    
                    if (inspectableList.GetElements()[i] is not IInspectorListElement listElement)
                        return;

                    if (inspector.targetElement != listElement)
                    {
                        inspector.targetElement = listElement;
                        inspector.Rebuild();
                    }
                },
                showFoldoutHeader = true,
                showBorder = true,
                showAddRemoveFooter = true
            };
        }

        public override void Bind(VisualElement visualElement, out Action? readAction)
        {
            if (visualElement is not ListView listView)
            {
                readAction = null;
                return;
            }
            
            readAction = () => listView.RefreshItems();
            foreach (var item in readActions)
                item.Value?.Invoke();
        }
    }
}