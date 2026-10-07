using RuniOS.APIBridge.UnityEngine.UIElements;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    public class ChildrenChangedManipulator : Manipulator
    {
        public ChildrenChangedManipulator() { }
        public ChildrenChangedManipulator(Action callback) => onChildrenChanged = callback;

        public event Action? onChildrenChanged;

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<AttachToPanelEvent>(OnAttach);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetach);

            if (target.panel != null)
                Subscribe(target.panel);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<AttachToPanelEvent>(OnAttach);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);

            if (target.panel != null)
                Unsubscribe(target.panel);
        }

        protected virtual void OnAttach(AttachToPanelEvent evt) => Subscribe(evt.destinationPanel);
        protected virtual void OnDetach(DetachFromPanelEvent evt) => Unsubscribe(evt.originPanel);

        void Subscribe(IPanel panel) => BaseVisualElementPanelBridge.__GetInstanceFrom(panel).hierarchyChanged += OnHierarchyChanged;
        void Unsubscribe(IPanel panel) => BaseVisualElementPanelBridge.__GetInstanceFrom(panel).hierarchyChanged -= OnHierarchyChanged;

        void OnHierarchyChanged(VisualElement ve, HierarchyChangeTypeBridge changeType, IReadOnlyList<VisualElement>? additionalContext = null)
        {
            switch (changeType)
            {
                case HierarchyChangeTypeBridge.AddedToParent:
                case HierarchyChangeTypeBridge.RemovedFromParent:
                {
                    if (ve.hierarchy.parent == target)
                        OnChildrenChanged();

                    break;
                }
                case HierarchyChangeTypeBridge.ChildrenReordered:
                {
                    if (ve == target)
                        OnChildrenChanged();

                    break;
                }
            }
        }

        protected void OnChildrenChanged() => onChildrenChanged?.Invoke();
    }
}