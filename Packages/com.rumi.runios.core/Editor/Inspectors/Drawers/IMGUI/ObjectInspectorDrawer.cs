#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;
using UnityEditor.AnimatedValues;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(object), true, allowInDebug = true)]
    public class ObjectInspectorDrawer : IMGUIInspectorDrawer
    {
        public ObjectInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) =>
            inspector = new Inspector(attributes.FilterInheritable(), undoRecorder);

        public override bool isField => false;

        public Inspector inspector { get; }
        public bool isExpanded { get; set; }

        static float foldoutYSize => EditorGUIUtility.singleLineHeight;

        void Rebuild(InspectorFlags flags)
        {
            CheckVariableElement();
            if (inspector.inspectable != variableElement.inspectableObjectElement || inspector.inspectorFlags != flags)
                inspector.Rebuild(variableElement.inspectableObjectElement, flags);
        }

        readonly AnimFloat animFloat = new AnimFloat(0);
        readonly AnimFloat nullableAnimFloat = new AnimFloat(1);
        protected override void OnGUI(Rect position, GUIContent? label, InspectorFlags flags, DrawerContext context = default)
        {
            CheckVariableElement();

            label ??= new GUIContent(element.displayName);
            
            if (NullToggleField(variableElement, position, out Rect foldoutPosition, label, flags, nullText, undoRecorder))
                return;

            foldoutPosition.height = foldoutYSize;
            isExpanded = EditorGUI.Foldout(foldoutPosition, isExpanded, label, true);
            
            Rebuild(flags);

            position.y += foldoutYSize + 2;
            position.height = inspector.GetHeight(label, flags, context);
            
            BeginIndentLevel();

            if (!context.isInArray)
            {
                if (isExpanded || animFloat.isAnimating)
                    inspector.Draw(position, label, context);
            }
            else if (isExpanded)
                inspector.Draw(position, label, context);

            EndIndentLevel();
        }

        public override float GetHeight(GUIContent? label, InspectorFlags flags, DrawerContext context = default)
        {
            CheckVariableElement();

            bool valueIsNull =
                !variableElement.variableType.IsValueType &&
                variableElement.IsReadable(flags) &&
                variableElement.value.IsNull();
            bool isExpanded = this.isExpanded && !valueIsNull;
            
            animFloat.target = isExpanded ? 1 : 0;
            
            Rebuild(flags);
            
            float size = foldoutYSize;
            if (!context.isInArray && animFloat.isAnimating)
            {
                size += ((inspector.GetHeight(label, flags, context) + 2) * animFloat.value);
                RepaintCurrentWindow();
            }
            else
                size += isExpanded ? inspector.GetHeight(label, flags, context) + 2 : 0;

            return size;
        }
    }
}