#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Vector2Int))]
    public class Vector2IntInspectorDrawer : GenericInspectorDrawer
    {
        public Vector2IntInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray, Rect? clipping) => EditorGUI.Vector2IntField(position, label, (Vector2Int)value!);

        protected override float CalculationHeight(GUIContent label, InspectorFlags flags, bool isInArray, Rect? clipping) => GetMultiColumnsFieldHeight(label);
    }
}