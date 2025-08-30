using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    [UxmlElement]
    public partial class LabelField : BaseField<string>
    {
        public Label visualInput { get; }
        
        public LabelField() : this(null, string.Empty) { }
        public LabelField(string? label, string text) : base(label, new Label())
        {
            this.RegisterDefaultStyleSheet(UIToolkitUtility.rosControlStyle);
            
            labelElement.AddToClassList(labelUssClassName);
            AddToClassList(ussClassName);
            
            visualInput = this.Q<Label>(className: BaseField<SerializableType>.inputUssClassName);
            visualInput.SetValueWithoutNotify(text);
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
            visualInput.SetValueWithoutNotify(newValue);
        }
    }
}