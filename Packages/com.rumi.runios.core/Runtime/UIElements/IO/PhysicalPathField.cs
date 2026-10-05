#nullable enable
using RuniOS.Editor.APIMarshal.UnityEngine.UIElements;
using RuniOS.IO;
using System.IO;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class PhysicalPathField : TextInputBaseFieldMarshal<PhysicalPath>
    {
        public new const string ussClassName = "runios-file-path-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";

        protected TextInput textInput => (TextInput)textInputBase;
        protected TextElement textElement => textInput.textElement;

        public PhysicalPathField() : this(string.Empty) { }
        public PhysicalPathField(string label) : base(label, -1, '*', new TextInput())
        {
            styleSheets.Add(UIElementsUtility.rosControlStyle);
            
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            
            textInput.AddToClassList(inputUssClassName);
            textInput.RegisterCallback<FocusOutEvent>(FocusOutEventCallback);

            // ReSharper disable once VirtualMemberCallInConstructor
            textElement.SetValueWithoutNotify(ValueToString(rawValue));
        }

        void FocusOutEventCallback(FocusOutEvent evt)
        {
            if (!isDelayed)
                textElement.SetValueWithoutNotify(rawValue.value);
        }



        public override void SetValueWithoutNotify(PhysicalPath newValue)
        {
            base.SetValueWithoutNotify(newValue);
            if (isDelayed)
            {
                textElement.SetValueWithoutNotify(newValue.value);
                return;
            }
            
            string inputValue = newValue.value;
            if (textElement.text.Length > 0 && textElement.text[^1] == Path.DirectorySeparatorChar)
                inputValue += Path.DirectorySeparatorChar;
            
            textElement.SetValueWithoutNotify(inputValue);
        }

        protected override string ValueToString(PhysicalPath value) => value.value;
        protected override PhysicalPath StringToValue(string str) => (PhysicalPath)str;

        protected class TextInput : TextInputBaseMarshal { }
    }
}