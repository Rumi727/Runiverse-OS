#nullable enable
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    /// <summary>
    /// 단축키 하나를 나타냅니다. <see cref="keyCode"/>가 <see cref="KeyCode.None"/>이면 단축키가 없는 것으로 봅니다.
    /// </summary>
    /// <param name="keyCode">눌러야 하는 키입니다.</param>
    /// <param name="actionKey"><see cref="KeyboardEventBase{T}.actionKey"/>와 같은 의미로, Windows·Linux에서는 Ctrl, macOS에서는 Command입니다.</param>
    /// <param name="modifiers"><paramref name="actionKey"/> 외에 함께 눌러야 하는 보조 키입니다.</param>
    [Serializable]
    public record struct KeyShortcut([field: SerializeField] KeyCode keyCode, [field: SerializeField] bool actionKey = false, [field: SerializeField] EventModifiers modifiers = EventModifiers.None)
    {
        const EventModifiers keyModifiers = EventModifiers.Shift | EventModifiers.Control | EventModifiers.Alt | EventModifiers.Command;

        static bool isMacOS => Application.platform is RuntimePlatform.OSXEditor or RuntimePlatform.OSXPlayer;
        static EventModifiers actionModifier => isMacOS ? EventModifiers.Command : EventModifiers.Control;

        public readonly bool Matches(KeyDownEvent evt)
        {
            if (keyCode == KeyCode.None || evt.keyCode != keyCode || evt.actionKey != actionKey)
                return false;

            // 동작 키는 actionKey로 이미 비교했으므로, 나머지 보조 키만 비교합니다.
            EventModifiers ignored = actionKey ? actionModifier : EventModifiers.None;
            return (evt.modifiers & keyModifiers & ~ignored) == (modifiers & keyModifiers & ~ignored);
        }

        /// <summary>
        /// 실행 중인 플랫폼의 표기로 단축키를 표시합니다. 단축키가 없으면 빈 문자열입니다.
        /// </summary>
        public override readonly string ToString()
        {
            if (keyCode == KeyCode.None)
                return string.Empty;

            EventModifiers all = modifiers | (actionKey ? actionModifier : EventModifiers.None);
            StringBuilder builder = new();

            if (isMacOS)
            {
                if ((all & EventModifiers.Control) != 0)
                    builder.Append('⌃');
                if ((all & EventModifiers.Alt) != 0)
                    builder.Append('⌥');
                if ((all & EventModifiers.Shift) != 0)
                    builder.Append('⇧');
                if ((all & EventModifiers.Command) != 0)
                    builder.Append('⌘');

                builder.Append(GetKeyText(keyCode));
                return builder.ToString();
            }

            if ((all & EventModifiers.Control) != 0)
                builder.Append("Ctrl+");
            if ((all & EventModifiers.Alt) != 0)
                builder.Append("Alt+");
            if ((all & EventModifiers.Shift) != 0)
                builder.Append("Shift+");
            if ((all & EventModifiers.Command) != 0)
                builder.Append("Win+");

            builder.Append(GetKeyText(keyCode));
            return builder.ToString();
        }

        static string GetKeyText(KeyCode keyCode)
        {
            if (keyCode is >= KeyCode.Alpha0 and <= KeyCode.Alpha9)
                return ((char)('0' + (keyCode - KeyCode.Alpha0))).ToString();

            return keyCode.ToString();
        }
    }
}
