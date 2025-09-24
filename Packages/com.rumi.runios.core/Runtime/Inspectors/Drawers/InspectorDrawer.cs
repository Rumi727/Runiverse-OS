#nullable enable
namespace RuniOS.Inspectors.Drawers
{
    public abstract class InspectorDrawer
    {
        public IInspectable inspectable { get; }
        public IInspectableList? inspectableList { get; }
        
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
        }
        
        protected InspectorDrawer(IInspectableList inspectableList)
        {
            inspectable = inspectableList;
            this.inspectableList = inspectableList;
        }
    }
}