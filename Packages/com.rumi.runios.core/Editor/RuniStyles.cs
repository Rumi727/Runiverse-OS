#nullable enable
namespace RuniOS.Editor
{
    public static class RuniStyles
    {
        public static GUIStyle contentBox => _contentBox ??= new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(4, 4, 4, 4) };
        static GUIStyle? _contentBox;
    }
}