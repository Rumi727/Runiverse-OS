#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using RuniOS.IO;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.IO
{
    [CustomInspectorDrawer(typeof(FileExtension))]
    public class FileExtensionInspectorDrawer : GenericInspectorDrawer
    {
        public FileExtensionInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        protected override object DrawField(Rect position, GUIContent label, object? value, bool isInArray) => FileExtensionField(position, label, (FileExtension)value!);
    }
}