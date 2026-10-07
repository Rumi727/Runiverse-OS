#nullable enable
namespace RuniOS.Editor
{
    public static class RuniStyles
    {
        public static GUIStyle largeLabel => _largeLabel ??= new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
        static GUIStyle? _largeLabel;

        public static GUIStyle richLabel => _richLabel ??= new GUIStyle(GUI.skin.label) { richText = true };
        static GUIStyle? _richLabel;

        public static GUIStyle labelButton => _labelButton ??= new GUIStyle(GUI.skin.label)
        {
            hover = new GUIStyleState { textColor = new Color(0, 0.2352941176f, 0.5333333333f) },
            active = new GUIStyleState { textColor = new Color(0, 0.2352941176f * 2, 0.5333333333f * 2) }
        };
        static GUIStyle? _labelButton;

        public static GUIStyle contentBox => _contentBox ??= new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(4, 4, 4, 4) };
        static GUIStyle? _contentBox;
    }
}