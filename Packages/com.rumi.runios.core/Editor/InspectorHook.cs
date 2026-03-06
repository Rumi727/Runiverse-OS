using RuniOS.Editor.APIBridge.UnityEditor;

namespace RuniOS.Editor
{
    public static class InspectorHook
    {
        public static event Action<InspectorWindowBridge>? onGUI { add => _onGUI += value; remove => _onGUI -= value; }
        internal static Action<InspectorWindowBridge>? _onGUI; // Patches/Editor/UnityEditor.PropertyEditor.cs를 참고해주세요
    }
}