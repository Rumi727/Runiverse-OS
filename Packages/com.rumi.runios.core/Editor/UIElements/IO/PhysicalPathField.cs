#nullable enable
using RuniOS.Editor.APIMarshal.UnityEngine.UIElements;
using RuniOS.IO;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.IO
{
    [UxmlElement]
    public partial class PhysicalPathField : TextInputBaseFieldMarshal<PhysicalPath>
    {
        public new const string ussClassName = "runios-file-path-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";

        public TextInput textInput => (TextInput)textInputBase;
        public TextElement textElement => textInput.textElement;

        public PhysicalPathField() : this(string.Empty) { }
        public PhysicalPathField(string label) : base(label, -1, '*', new TextInput())
        {
            this.RegisterDefaultStyleSheet(UIToolkitUtility.rosControlStyle);
            
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            
            textInput.AddToClassList(inputUssClassName);
            textInput.RegisterCallback<FocusOutEvent>(FocusOutEventCallback);
        }

        void FocusOutEventCallback(FocusOutEvent evt) => textElement.SetValueWithoutNotify(rawValue.value);



        public override void SetValueWithoutNotify(PhysicalPath newValue)
        {
            base.SetValueWithoutNotify(newValue);
            
            string inputValue = newValue.value;
            if (textElement.text.Length > 0 && textElement.text[^1] == RuniPath.directorySeparatorChar)
                inputValue += RuniPath.directorySeparatorChar;
            
            textElement.SetValueWithoutNotify(inputValue);
        }

        protected override string ValueToString(PhysicalPath value) => value.value;
        protected override PhysicalPath StringToValue(string str) => (PhysicalPath)str;

        public class TextInput : TextInputBaseMarshal { }
    }
}