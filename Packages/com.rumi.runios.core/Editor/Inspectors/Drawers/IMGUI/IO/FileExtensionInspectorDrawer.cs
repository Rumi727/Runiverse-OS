#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using RuniOS.IO;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.IO
{
    [CustomInspectorDrawer(typeof(FileExtension))]
    public class FileExtensionInspectorDrawer : GenericInspectorDrawer
    {
        public FileExtensionInspectorDrawer(IInspectorVariableElement element) : base(element) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => FileExtensionField(position, label, (FileExtension)value!);
    }
}