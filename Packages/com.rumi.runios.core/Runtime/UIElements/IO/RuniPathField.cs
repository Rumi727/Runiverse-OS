#nullable enable
using RuniOS.Editor.APIMarshal.UnityEngine.UIElements;
using RuniOS.IO;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class RuniPathField : TextInputBaseFieldMarshal<RuniPath>
    {
        public new const string ussClassName = "runios-file-path-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";

        protected TextInput textInput => (TextInput)textInputBase;
        protected TextElement textElement => textInput.textElement;

        public RuniPathField() : this(string.Empty) { }
        public RuniPathField(string label) : base(label, -1, '*', new TextInput())
        {
            styleSheets.Add(UIElementsUtility.rosControlStyle);
            
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            
            textInput.AddToClassList(inputUssClassName);
            textInput.RegisterCallback<FocusOutEvent>(FocusOutEventCallback);

            // ReSharper disable once VirtualMemberCallInConstructor
            textElement.SetValueWithoutNotify(ValueToString(rawValue));
        }

        void FocusOutEventCallback(FocusOutEvent evt) => textElement.SetValueWithoutNotify(rawValue.value);



        public override void SetValueWithoutNotify(RuniPath newValue)
        {
            base.SetValueWithoutNotify(newValue);
            
            string inputValue = newValue.value;
            if (textElement.text.Length > 0 && textElement.text[^1] == RuniPath.directorySeparatorChar)
                inputValue += RuniPath.directorySeparatorChar;
            
            textElement.SetValueWithoutNotify(inputValue);
        }

        protected override string ValueToString(RuniPath value) => value.value;
        protected override RuniPath StringToValue(string str) => (RuniPath)str;

        protected class TextInput : TextInputBaseMarshal { }
    }
}