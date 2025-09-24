#nullable enable
using RuniOS.UIElements;
using System;
using UnityEngine.UIElements;

namespace RuniOS.Inspectors.Drawers.UIElements
{
    [CustomInspectorDrawer(typeof(object))]
    public class ObjectInspectorDrawer : UIElementInspectorDrawer
    {
        public ObjectInspectorDrawer(IInspectorElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }
        public ObjectInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList, rootInspector) { }
        
        public Inspector? inspector { get; private set; }

        public override VisualElement Build()
        {
            inspector = new Inspector(rootInspector); 
            
            Foldout foldout = new Foldout { value = false };
            foldout.Add(inspector);
            
            return foldout;
        }

        public override void Bind(VisualElement visualElement, out Action? readAction)
        {
            if (visualElement is not Foldout foldout || variableElement == null)
            {
                readAction = null;
                return;
            }
            
            readAction = () =>
            {
                if (!foldout.value || inspector == null)
                    return;
                
                if (inspector.targetInspectable != variableElement.inspectableObjectElement)
                {
                    inspector.targetInspectable = variableElement.inspectableObjectElement;
                    inspector.Rebuild();
                }
            };
        }
    }
}