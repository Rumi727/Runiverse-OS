#nullable enable
using RuniOS.Editor.APIMarshal.UnityEngine.UIElements;
using RuniOS.IO;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.IO
{
    [UxmlElement]
    public partial class FilePathField : TextInputBaseFieldMarshal<FilePath>
    {
        public new const string ussClassName = "runios-file-path-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";

        public TextInput textInput => (TextInput)textInputBase;
        public TextElement textElement => textInput.textElement;

        public FilePathField() : this(string.Empty) { }
        public FilePathField(string label) : base(label, -1, '*', new TextInput())
        {
            this.RegisterDefaultStyleSheet(UIToolkitUtility.rosControlStyle);
            
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            
            textInput.AddToClassList(inputUssClassName);
            textInput.RegisterCallback<FocusOutEvent>(FocusOutEventCallback);
        }

        void FocusOutEventCallback(FocusOutEvent evt) => textElement.SetValueWithoutNotify(rawValue.value);



        public override void SetValueWithoutNotify(FilePath newValue)
        {
            base.SetValueWithoutNotify(newValue);
            
            string inputValue = newValue;
            if (textElement.text.Length > 0 && textElement.text[^1] == FilePath.directorySeparatorChar)
                inputValue += FilePath.directorySeparatorChar;
            
            textElement.SetValueWithoutNotify(inputValue);
        }

        protected override string ValueToString(FilePath value) => value;
        protected override FilePath StringToValue(string str) => str;

        public class TextInput : TextInputBaseMarshal { }
    }
}