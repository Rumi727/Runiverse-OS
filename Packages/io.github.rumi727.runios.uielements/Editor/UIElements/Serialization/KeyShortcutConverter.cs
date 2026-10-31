#nullable enable
using RuniOS.UIElements;
using System.Text;
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization
{
    /// <summary>
    /// <see cref="KeyShortcut"/>를 <c>Action+Shift+T</c>처럼 보조 키와 키 이름을 <c>+</c>로 이은 문자열로 변환합니다.
    /// 빈 문자열은 단축키가 없는 것으로 봅니다.
    /// </summary>
    public sealed class KeyShortcutConverter : UxmlAttributeConverter<KeyShortcut>
    {
        public override KeyShortcut FromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return default;

            bool actionKey = false;
            EventModifiers modifiers = EventModifiers.None;
            KeyCode keyCode = KeyCode.None;

            foreach (string token in value.Split('+'))
            {
                string part = token.Trim();
                if (part.Equals("Action", StringComparison.OrdinalIgnoreCase))
                    actionKey = true;
                else if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
                    modifiers |= EventModifiers.Control;
                else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    modifiers |= EventModifiers.Alt;
                else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    modifiers |= EventModifiers.Shift;
                else if (part.Equals("Cmd", StringComparison.OrdinalIgnoreCase))
                    modifiers |= EventModifiers.Command;
                else if (part.Length == 1 && char.IsDigit(part[0]))
                    keyCode = KeyCode.Alpha0 + (part[0] - '0');
                else if (Enum.TryParse(part, true, out KeyCode parsed))
                    keyCode = parsed;
                else
                    Debug.LogError($"'{part}' is not a valid key in shortcut '{value}'.");
            }

            return new KeyShortcut(keyCode, actionKey, modifiers);
        }

        public override string ToString(KeyShortcut value)
        {
            if (value.keyCode == KeyCode.None)
                return string.Empty;

            StringBuilder builder = new();

            if (value.actionKey)
                builder.Append("Action+");
            if ((value.modifiers & EventModifiers.Control) != 0)
                builder.Append("Ctrl+");
            if ((value.modifiers & EventModifiers.Alt) != 0)
                builder.Append("Alt+");
            if ((value.modifiers & EventModifiers.Shift) != 0)
                builder.Append("Shift+");
            if ((value.modifiers & EventModifiers.Command) != 0)
                builder.Append("Cmd+");

            builder.Append(value.keyCode);
            return builder.ToString();
        }
    }
}
