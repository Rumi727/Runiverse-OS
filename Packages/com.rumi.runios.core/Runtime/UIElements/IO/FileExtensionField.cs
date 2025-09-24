#nullable enable
using RuniOS.APIMarshal.UnityEngine.UIElements;
using RuniOS.IO;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class FileExtensionField : TextInputBaseFieldMarshal<FileExtension>
    {
        public new const string ussClassName = "runios-file-extension-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";
        public const string extensionSeparatorUssClassName = ussClassName + "__extension-separator";

        public TextInput textInput => (TextInput)textInputBase;
        public TextElement textElement => textInput.textElement;
        
        public TextElement extensionSeparatorTextElement { get; }

        public FileExtensionField() : this(string.Empty) { }
        public FileExtensionField(string label) : base(label, -1, '*', new TextInput())
        {
            this.RegisterDefaultStyleSheet(UIToolkitUtility.rosControlStyle);
            
            AddToClassList(ussClassName);
            
            labelElement.AddToClassList(labelUssClassName);
            textInput.AddToClassList(inputUssClassName);
            
            extensionSeparatorTextElement = new TextElement { name = extensionSeparatorUssClassName };
            extensionSeparatorTextElement.AddToClassList(extensionSeparatorUssClassName);
            
            textInput.hierarchy.Insert(0, extensionSeparatorTextElement);
            
            textInput.RegisterCallback<FocusInEvent>(_ => UpdateExtensionSeparator(true));
            textInput.RegisterCallback<FocusOutEvent>(_ => UpdateExtensionSeparator(false));
        }
        
        void UpdateExtensionSeparator(bool isFocused)
        {
            if (value == FileExtension.empty && !isFocused)
                extensionSeparatorTextElement.SetValueWithoutNotify(string.Empty);
            else
                extensionSeparatorTextElement.SetValueWithoutNotify(FileExtension.extensionSeparatorChar.ToString());
        }

        public override void SetValueWithoutNotify(FileExtension newValue)
        {
            base.SetValueWithoutNotify(newValue);

            string inputValue = newValue.value.TrimStart(FileExtension.extensionSeparatorChar);
            int indexDifference = (inputValue.Length - textElement.text.Length);
            textElement.SetValueWithoutNotify(inputValue);
            cursorIndex += indexDifference;
            selectIndex += indexDifference;

            UpdateExtensionSeparator(focusController?.focusedElement == this);
        }

        protected override string ValueToString(FileExtension value) => value.value.TrimStart(FileExtension.extensionSeparatorChar);
        protected override FileExtension StringToValue(string str)
        {
            FileExtension value = FileExtension.extensionSeparatorChar + str;
            if (value.value.Length <= 1)
                value = FileExtension.empty;

            if (str.Contains(FileExtension.extensionSeparatorChar))
            {
                string inputValue = value.value.TrimStart(FileExtension.extensionSeparatorChar);
                textElement.SetValueWithoutNotify(inputValue);
                cursorIndex = 0;
                selectIndex = 0;
            }
            
            return value;
        }

        public class TextInput : TextInputBaseMarshal { }
    }
}
