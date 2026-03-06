#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.IO;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.IO
{
    [CustomInspectorDrawer(typeof(FileExtension))]
    public class FileExtensionInspectorDrawer : GenericInspectorDrawer
    {
        public FileExtensionInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => FileExtensionField(position, label, (FileExtension)value!);
    }
}