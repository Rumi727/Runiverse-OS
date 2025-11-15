#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Inspectors.Drawers
{
    public abstract class InspectorDrawer
    {
        public IInspectable inspectable { get; }
        public IInspectableList? inspectableList { get; }
        public IInspectableDictionary? inspectableDictionary { get; }
        
        public IInspectorElement? element { get; }
        public IInspectorVariableElement? variableElement { get; }
        public IInspectorActionElement? actionElement { get; }

        protected InspectorDrawer(IInspectorElement element)
        {
            inspectable = element.inspectable;
            
            this.element = element;
            variableElement = element as IInspectorVariableElement;
            actionElement = element as IInspectorActionElement;

            inspectableList = variableElement?.inspectableListElement;
            inspectableDictionary = variableElement?.inspectableDictionaryElement;
        }
        
        protected InspectorDrawer(IInspectableList inspectableList)
        {
            inspectable = inspectableList;
            this.inspectableList = inspectableList;
        }
        
        protected InspectorDrawer(IInspectableDictionary inspectableDictionary)
        {
            inspectable = inspectableDictionary;
            this.inspectableDictionary = inspectableDictionary;
        }
        
        [MemberNotNull(nameof(inspectableList))]
        protected void CheckInspectableList()
        {
            if (inspectableList == null)
                throw new InvalidOperationException($"{nameof(inspectableList)} is null");
        }
        
        [MemberNotNull(nameof(inspectableDictionary))]
        protected void CheckInspectableDictionary()
        {
            if (inspectableDictionary == null)
                throw new InvalidOperationException($"{nameof(inspectableDictionary)} is null");
        }
        
        [MemberNotNull(nameof(element))]
        protected void CheckElement()
        {
            if (element == null)
                throw new InvalidOperationException($"{nameof(element)} is null");
        }

        [MemberNotNull(nameof(element), nameof(variableElement))]
        protected void CheckVariableElement()
        {
            if (element == null || variableElement == null)
                throw new InvalidOperationException($"{nameof(variableElement)} is null");
        }
        
        [MemberNotNull(nameof(element), nameof(actionElement))]
        protected void CheckActionElement()
        {
            if (element == null || actionElement == null)
                throw new InvalidOperationException($"{nameof(actionElement)} is null");
        }
    }
}