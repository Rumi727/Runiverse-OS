#nullable enable
using RuniOS.Editor.APIMarshal.UnityEngine.UIElements;
using System.Globalization;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Primitives
{
    [UxmlElement]
    public partial class CharField : TextInputBaseFieldMarshal<char>
    {
        public new const string ussClassName = "runios-char-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";
        public const string extensionSeparatorUssClassName = ussClassName + "__extension-separator";

        public TextInput textInput => (TextInput)textInputBase;
        public TextElement textElement => textInput.textElement;
        
        public bool isFocused { get; private set; }

        public CharField() : this(string.Empty) { }
        public CharField(string label) : base(label, -1, '*', new TextInput())
        {
            this.RegisterDefaultStyleSheet(UIToolkitUtility.rosControlStyle);
            
            AddToClassList(ussClassName);
            
            labelElement.AddToClassList(labelUssClassName);
            textInput.AddToClassList(inputUssClassName);
            
            textInput.RegisterCallback<FocusInEvent>(_ =>
            {
                isFocused = true;
                textElement.SetValueWithoutNotify(ValueToString(value));
            });
            
            textInput.RegisterCallback<FocusOutEvent>(_ =>
            {
                isFocused = false;
                textElement.SetValueWithoutNotify(ValueToString(value));
            });
        }

        public override void SetValueWithoutNotify(char newValue)
        {
            base.SetValueWithoutNotify(newValue);
            
            if (!isFocused)
                textElement.SetValueWithoutNotify(ValueToString(value));
        }

        protected override string ValueToString(char value)
        {
            string stringValue;
            switch (value)
            {
                case '\n':
                    stringValue = "\\n";
                    break;
                case '\r':
                    stringValue = "\\r";
                    break;
                case '\t':
                    stringValue = "\\t";
                    break;
                case '\v':
                    stringValue = "\\v";
                    break;
                case '\0':
                    stringValue = "\\0";
                    break;
                case '\a':
                    stringValue = "\\a";
                    break;
                case '\b':
                    stringValue = "\\b";
                    break;
                case '\f':
                    stringValue = "\\f";
                    break;
                default:
                {
                    if (char.IsControl(value))
                        stringValue = $"\\u{(int)value:X4}";
                    else
                        stringValue = value.ToString();
                    break;
                }
            }
            
            return stringValue;
        }
        
        protected override char StringToValue(string str)
        {
            if (str.StartsWith("\\u", StringComparison.OrdinalIgnoreCase))
            {
                if (str.Length == 6 && uint.TryParse(str.Substring(2), NumberStyles.HexNumber, null, out uint result))
                    return (char)result;
            }
            else switch (str)
            {
                case "\\n":
                    return '\n';
                case "\\r":
                    return '\r';
                case "\\t":
                    return '\t';
                case "\\v":
                    return '\v';
                case "\\0":
                    return '\0';
                case "\\a":
                    return '\a';
                case "\\b":
                    return '\b';
                case "\\f":
                    return '\f';
                default:
                {
                    if (char.TryParse(str, out char result))
                        return result;
                    break;
                }
            }

            return rawValue;
        }

        public class TextInput : TextInputBaseMarshal { }
    }
}