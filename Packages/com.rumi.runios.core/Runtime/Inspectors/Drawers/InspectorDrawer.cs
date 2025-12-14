#nullable enable
using RuniOS.Undos;
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
        
        public IUndoRecorder? undoRecorder { get; }

        protected InspectorDrawer(IInspectorElement element, IUndoRecorder? undoRecorder = null)
        {
            inspectable = element.inspectable;

            this.element = element;
            variableElement = element as IInspectorVariableElement;
            actionElement = element as IInspectorActionElement;

            inspectableList = variableElement?.inspectableListElement;
            inspectableDictionary = variableElement?.inspectableDictionaryElement;

            this.undoRecorder = undoRecorder;
        }

        protected InspectorDrawer(IInspectableList inspectableList, IUndoRecorder? undoRecorder = null)
        {
            inspectable = inspectableList;
            this.inspectableList = inspectableList;
            
            this.undoRecorder = undoRecorder;
        }

        protected InspectorDrawer(IInspectableDictionary inspectableDictionary, IUndoRecorder? undoRecorder = null)
        {
            inspectable = inspectableDictionary;
            this.inspectableDictionary = inspectableDictionary;
            
            this.undoRecorder = undoRecorder;
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