#nullable enable
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    /// <summary>
    /// 단축키 하나를 나타냅니다. <see cref="keyCode"/>가 <see cref="KeyCode.None"/>이면 단축키가 없는 것으로 봅니다.
    /// </summary>
    [Serializable]
    public record struct KeyShortcut(KeyCode keyCode, bool actionKey = false, EventModifiers modifiers = EventModifiers.None)
    {
        const EventModifiers keyModifiers = EventModifiers.Shift | EventModifiers.Control | EventModifiers.Alt | EventModifiers.Command;

        static bool isMacOS => Application.platform is RuntimePlatform.OSXEditor or RuntimePlatform.OSXPlayer;
        static EventModifiers actionModifier => isMacOS ? EventModifiers.Command : EventModifiers.Control;

        /// <summary>눌러야 하는 키입니다.</summary>
        public KeyCode keyCode
        {
            readonly get => _keyCode;
            set => _keyCode = value;
        }
        [SerializeField] KeyCode _keyCode = keyCode;

        /// <summary><see cref="KeyboardEventBase{T}.actionKey"/>와 같은 의미로, Windows·Linux에서는 Ctrl, macOS에서는 Command입니다.</summary>
        public bool actionKey
        {
            readonly get => _actionKey;
            set => _actionKey = value;
        }
        [SerializeField] bool _actionKey = actionKey;

        /// <summary><see cref="actionKey"/> 외에 함께 눌러야 하는 보조 키입니다.</summary>
        public EventModifiers modifiers
        {
            readonly get => _modifiers;
            set => _modifiers = value;
        }
        [SerializeField] EventModifiers _modifiers = modifiers;

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
            StringBuilder builder = StringBuilderCache.Acquire();

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
                return StringBuilderCache.GetStringAndRelease(builder);
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
            return StringBuilderCache.GetStringAndRelease(builder);
        }

        static string GetKeyText(KeyCode keyCode)
        {
            if (keyCode is >= KeyCode.Alpha0 and <= KeyCode.Alpha9)
                return ((char)('0' + (keyCode - KeyCode.Alpha0))).ToString();

            return keyCode.ToString();
        }
    }
}
