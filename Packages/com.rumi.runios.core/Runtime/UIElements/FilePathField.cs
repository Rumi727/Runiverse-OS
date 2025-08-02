#nullable enable
using RuniOS.IO;
using System.Linq;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    [UxmlElement]
    public partial class FilePathField : BaseField<FilePath>
    {
        public new const string ussClassName = "runios-file-path-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";

        public TextField visualInput { get; }

        public FilePathField() : this(string.Empty) { }
        public FilePathField(string label) : base(label, new TextField(label))
        {
            styleSheets.Add(UIToolkitUtility.rosControlStyle);
            
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            
            visualInput = this.Q<TextField>(className: BaseField<HexColor>.inputUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            
            visualInput.RegisterValueChangedCallback(ChangeEventCallback);
            visualInput.RegisterCallback<FocusOutEvent>(FocusOutEventCallback);
        }
        
        void ChangeEventCallback(ChangeEvent<string> evt)
        {
            value = evt.newValue;

            string inputValue = rawValue.value;
            if (evt.newValue.Length > 0 && evt.newValue[^1] == FilePath.directorySeparatorChar)
                inputValue += FilePath.directorySeparatorChar;
            
            int indexDifference = inputValue.Length - evt.newValue.Length;
            visualInput.SetValueWithoutNotify(inputValue);
            visualInput.cursorIndex += indexDifference;
            visualInput.selectIndex += indexDifference;
        }

        void FocusOutEventCallback(FocusOutEvent evt) => visualInput.SetValueWithoutNotify(rawValue);



        public override void SetValueWithoutNotify(FilePath newValue)
        {
            base.SetValueWithoutNotify(newValue);
            
            string inputValue = newValue;
            if (visualInput.text.Length > 0 && visualInput.text[^1] == FilePath.directorySeparatorChar)
                inputValue += FilePath.directorySeparatorChar;
            
            visualInput.SetValueWithoutNotify(inputValue);
        }

        protected override void UpdateMixedValueContent() => visualInput.showMixedValue = showMixedValue;
    }
}
