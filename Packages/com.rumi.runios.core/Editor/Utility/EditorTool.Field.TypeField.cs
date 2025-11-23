#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using UnityEditor.Search;
using UnityEngine.Search;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static Type? TypeFieldLayout(Type? value, Type? baseType = null) => TypeField(EditorGUILayout.GetControlRect(), value, baseType);
        public static Type? TypeFieldLayout(string label, Type? value, Type? baseType = null) => TypeField(EditorGUILayout.GetControlRect(), label, value, baseType);
        public static Type? TypeFieldLayout(GUIContent label, Type? value, Type? baseType = null) => TypeField(EditorGUILayout.GetControlRect(), label, value, baseType);

        public static Type? TypeField(Rect position, Type? value, Type? baseType = null) => DoTypeField(position, value, baseType);
        public static Type? TypeField(Rect position, string label, Type? value, Type? baseType = null) => TypeField(position, new GUIContent(label), value, baseType);
        public static Type? TypeField(Rect position, GUIContent label, Type? value, Type? baseType = null)
        {
            position = EditorGUI.PrefixLabel(position, label);
            BeginIndentLevel(0);
            value = DoTypeField(position, value, baseType);
            EndIndentLevel();
            return value;
        }

        static int? typeFieldLastControlID;
        static Type? typeFieldSelectedType;
        static Type? DoTypeField(Rect position, Type? value, Type? baseType)
        {
            string buttonText = GetTextOrKey("gui.type_field.select_type");
            float buttonWidth = GetXSize(buttonText, GUI.skin.button);

            position.width -= buttonWidth + 3;

            EditorGUI.LabelField(position, value?.SerializeToString() ?? GetTextOrKey("gui.none"));

            position.x += position.width + 3;
            position.width = buttonWidth;

            if (GUI.Button(position, buttonText))
            {
                int lastControlID = EditorGUIUtilityBridge.s_LastControlID;
                ShowTypePicker(x =>
                {
                    typeFieldLastControlID = lastControlID;
                    typeFieldSelectedType = x;
                }, baseType);
            }

            if (typeFieldLastControlID != null && typeFieldLastControlID == EditorGUIUtilityBridge.s_LastControlID)
            {
                value = typeFieldSelectedType;

                typeFieldSelectedType = null;
                typeFieldLastControlID = null;

                GUI.changed = true;
            }

            return value;
        }

        public static void ShowTypePicker(Action<Type?> selectHandler, Type? baseType = null)
        {
            var provider = new TypeSearchProvider(baseType ?? typeof(object));
            var context = SearchService.CreateContext(provider, "type:");
            var viewState = new SearchViewState(context)
            {
                title = GetTextOrKey("gui.type"),
                queryBuilderEnabled = true,
                hideTabs = true,
                selectHandler = (SearchItem item, bool cancelled) =>
                {
                    if (cancelled)
                        return;

                    if (item.data is Type type)
                        selectHandler.Invoke(type);
                    else
                        selectHandler.Invoke(null);
                },
                flags = (SearchViewFlags.TableView | SearchViewFlags.DisableInspectorPreview | SearchViewFlags.DisableBuilderModeToggle)
            };
            SearchService.ShowPicker(viewState);
        }
    }
}