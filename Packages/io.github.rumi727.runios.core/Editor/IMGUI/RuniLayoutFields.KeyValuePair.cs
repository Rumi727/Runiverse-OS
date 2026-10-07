#nullable enable

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static KeyValuePair<TKey, TValue> KeyValuePairField<TKey, TValue>(KeyValuePair<TKey, TValue> value, Func<Rect, TKey, TKey> drawKeyAction, Func<Rect, TValue, TValue> drawValueAction) => KeyValuePairField(GUIContent.none, value, drawKeyAction, drawValueAction);
        public static KeyValuePair<TKey, TValue> KeyValuePairField<TKey, TValue>(string label, KeyValuePair<TKey, TValue> value, Func<Rect, TKey, TKey> drawKeyAction, Func<Rect, TValue, TValue> drawValueAction) => KeyValuePairField(new GUIContent(label), value, drawKeyAction, drawValueAction);
        public static KeyValuePair<TKey, TValue> KeyValuePairField<TKey, TValue>(GUIContent label, KeyValuePair<TKey, TValue> value, Func<Rect, TKey, TKey> drawKeyAction, Func<Rect, TValue, TValue> drawValueAction) => RuniFields.KeyValuePairField(GetMultiColumnsControlRect(label), label, value, drawKeyAction, drawValueAction);
    }
}
