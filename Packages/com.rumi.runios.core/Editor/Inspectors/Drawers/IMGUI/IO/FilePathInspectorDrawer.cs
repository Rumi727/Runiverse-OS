#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using RuniOS.IO;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.IO
{
    [CustomInspectorDrawer(typeof(FilePath))]
    public class FilePathInspectorDrawer : GenericInspectorDrawer
    {
        public FilePathInspectorDrawer(IInspectorVariableElement element, IUndoRecorder? undoRecorder = null) : base(element, undoRecorder) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => FilePathField(position, label, (FilePath)value!);
    }
}