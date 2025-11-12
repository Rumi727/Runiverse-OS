#nullable enable
using RuniOS.UIElements;
using System;
using System.Collections;
using System.Linq;
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
        public override VisualElement Build(string label = "", InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            CheckInspectableList();
            
            return new ListView(inspectableList) 
            {
                headerTitle = label,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                makeItem = () => new Inspector(rootInspector),
                bindItem = (visualElement, i) =>
                {
                    if (visualElement is not Inspector inspector)
                        return;

                    IInspectorListElement? element = inspectableList.GetElement(i, flags);
                    if (inspector.element == element)
                        return;
                    
                    if (element == null)
                    {
                        inspector.Rebuild(Enumerable.Empty<IInspectorElement>(), InspectorFlags.None);
                        return;
                    }

                    inspector.Rebuild(element, flags, true);
                },
                showFoldoutHeader = true,
                showBorder = true,
                showAddRemoveFooter = true
            };
        }

        public override void Bind(VisualElement visualElement, InspectorFlags flags, out Action? readAction)
        {
            if (visualElement is not ListView listView)
            {
                readAction = null;
                return;
            }
            
            readAction = () => listView.RefreshItems();
        }
    }
}