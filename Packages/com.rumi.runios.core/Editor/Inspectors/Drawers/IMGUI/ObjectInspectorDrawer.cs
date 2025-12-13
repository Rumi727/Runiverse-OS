#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using UnityEditor.AnimatedValues;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(object), true, allowInDebug = true)]
    public class ObjectInspectorDrawer : IMGUIInspectorDrawer
    {
        public ObjectInspectorDrawer(IInspectorVariableElement element) : base(element) => inspector = new Inspector();

        public override bool isField => false;

        public Inspector inspector { get; }
        public bool isExpanded { get; set; }

        static float foldoutYSize => EditorGUIUtility.singleLineHeight;

        readonly AnimFloat animFloat = new AnimFloat(0);
        readonly AnimFloat nullableAnimFloat = new AnimFloat(1);
        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false, Rect? clipping = null)
        {
            CheckVariableElement();
            
            if (NullToggleField(variableElement, position, out Rect foldoutPosition, label, flags, nullText, undoRecorder))
                return;

            foldoutPosition.height = foldoutYSize;
            isExpanded = EditorGUI.Foldout(foldoutPosition, isExpanded, label, true);
            
            if (inspector.inspectable != variableElement.inspectableObjectElement || inspector.inspectorFlags != flags)
                inspector.Rebuild(variableElement.inspectableObjectElement, flags);

            position.y += foldoutYSize + 2;
            position.height = inspector.GetHeight(label, flags, isInArray);
            
            BeginIndentLevel();

            if (!isInArray)
            {
                if (isExpanded || animFloat.isAnimating)
                    inspector.Draw(position, label, isInArray, clipping);
            }
            else if (isExpanded)
                inspector.Draw(position, label, isInArray, clipping);

            EndIndentLevel();
        }

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            CheckVariableElement();

            bool valueIsNull =
                !variableElement.variableType.IsValueType &&
                variableElement.IsReadable(flags) &&
                variableElement.value.IsNull();
            bool isExpanded = this.isExpanded && !valueIsNull;
            
            animFloat.target = isExpanded ? 1 : 0;
            
            float size = foldoutYSize;
            if (!isInArray && animFloat.isAnimating)
            {
                size += ((inspector.GetHeight(label, flags, isInArray) + 2) * animFloat.value);
                RepaintCurrentWindow();
            }
            else
                size += isExpanded ? inspector.GetHeight(label, flags, isInArray) + 2 : 0;

            return size;
        }
    }
}