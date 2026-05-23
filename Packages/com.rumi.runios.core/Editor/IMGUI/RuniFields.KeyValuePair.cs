#nullable enable
namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniFields
    {
        public static KeyValuePair<TKey, TValue> KeyValuePairField<TKey, TValue>(Rect position, KeyValuePair<TKey, TValue> value, Func<Rect, TKey, TKey> drawKeyAction, Func<Rect, TValue, TValue> drawValueAction) => DoKeyValuePairField(position, value, drawKeyAction, drawValueAction);
        public static KeyValuePair<TKey, TValue> KeyValuePairField<TKey, TValue>(Rect position, string label, KeyValuePair<TKey, TValue> value, Func<Rect, TKey, TKey> drawKeyAction, Func<Rect, TValue, TValue> drawValueAction) => KeyValuePairField(position, new GUIContent(label), value, drawKeyAction, drawValueAction);
        public static KeyValuePair<TKey, TValue> KeyValuePairField<TKey, TValue>(Rect position, GUIContent label, KeyValuePair<TKey, TValue> value, Func<Rect, TKey, TKey> drawKeyAction, Func<Rect, TValue, TValue> drawValueAction)
        {
            position = DrawMultiColumnsFieldPrefixLabel(position, label, 3); // 2로 하면 크기 절반 줄어듬

            BeginIndentLevel(0);
            value = DoKeyValuePairField(position, value, drawKeyAction, drawValueAction);
            EndIndentLevel();

            return value;
        }

        static KeyValuePair<TKey, TValue> DoKeyValuePairField<TKey, TValue>(Rect position, KeyValuePair<TKey, TValue> value, Func<Rect, TKey, TKey> drawKeyAction, Func<Rect, TValue, TValue> drawValueAction)
        {
            float fieldWidth = (position.width - 15) / 2f;

            TKey valueKey = value.Key;
            TValue valueValue = value.Value;

            {
                position.width = fieldWidth;

                string keyLabel = GetTextOrKey("gui.key");

                BeginLabelWidth(keyLabel);
                EditorGUI.PrefixLabel(position, new GUIContent(keyLabel));
                valueKey = drawKeyAction.Invoke(position, valueKey);
                EndLabelWidth();

                position.x += position.width + 15;
            }

            {
                position.width = fieldWidth.Ceil();

                string valueLabel = GetTextOrKey("gui.value");

                BeginLabelWidth(valueLabel);
                EditorGUI.PrefixLabel(position, new GUIContent(valueLabel));
                valueValue = drawValueAction.Invoke(position, valueValue);
                EndLabelWidth();
            }

            return KeyValuePair.Create(valueKey, valueValue);
        }
    }
}
