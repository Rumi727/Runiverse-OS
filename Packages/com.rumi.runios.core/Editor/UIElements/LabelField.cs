using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements;

[UxmlElement]
public partial class LabelField : BaseField<string>
{
    public new const string ussClassName = "runios-label-field";
    public new const string labelUssClassName = ussClassName + "__label";
    public new const string inputUssClassName = ussClassName + "__input";
        
    public Label visualInput { get; }
        
    public LabelField() : this(string.Empty, string.Empty) { }
    public LabelField(string label) : this(label, string.Empty) { }
    public LabelField(string label, string text) : base(label, new Label())
    {
        this.RegisterDefaultStyleSheet(UIToolkitUtility.rosControlStyle);
            
        AddToClassList(ussClassName);
        labelElement.AddToClassList(labelUssClassName);
            
        visualInput = this.Q<Label>(className: BaseField<SerializableType>.inputUssClassName);
        visualInput.AddToClassList(inputUssClassName);
        visualInput.SetValueWithoutNotify(text);
    }

    public override void SetValueWithoutNotify(string newValue)
    {
        base.SetValueWithoutNotify(newValue);
        visualInput.SetValueWithoutNotify(newValue);
    }
}