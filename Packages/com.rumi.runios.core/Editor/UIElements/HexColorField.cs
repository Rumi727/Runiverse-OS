using Unity.Properties;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements;

[UxmlElement]
public partial class HexColorField : BaseField<HexColor>
{
    public new const string ussClassName = "runios-hex-color-field";
    public new const string labelUssClassName = ussClassName + "__label";
    public new const string inputUssClassName = ussClassName + "__input";
        
    public static readonly BindingId showEyeDropperProperty = (BindingId)nameof(showEyeDropper);
    public static readonly BindingId showAlphaProperty = (BindingId)nameof(showAlpha);

    public override HexColor value
    {
        get => base.value;
        set
        {
            base.value = value;
            visualInput.SetValueWithoutNotify(value);
        }
    }

    [UxmlAttribute]
    [CreateProperty]
    public bool showEyeDropper
    {
        get => visualInput.showEyeDropper;
        set
        {
            visualInput.showEyeDropper = value;
            NotifyPropertyChanged(in showEyeDropperProperty);
        }
    }
        
    [UxmlAttribute]
    [CreateProperty]
    public bool showAlpha
    {
        get => visualInput.showAlpha;
        set
        {
            visualInput.showAlpha = value;
            NotifyPropertyChanged(in showAlphaProperty);
        }
    }

    public ColorField visualInput { get; }

    public HexColorField() : this(string.Empty) { }
    public HexColorField(string label) : base(label, new ColorField(label))
    {
        styleSheets.Add(UIToolkitUtility.rosControlStyle);
            
        AddToClassList(ussClassName);
        labelElement.AddToClassList(labelUssClassName);
            
        visualInput = this.Q<ColorField>(className: BaseField<HexColor>.inputUssClassName);
        visualInput.AddToClassList(inputUssClassName);

        visualInput.RegisterValueChangedCallback(ChangeEventCallback);
    }
        
    void ChangeEventCallback(ChangeEvent<Color> x) => value = new HexColor(x.newValue);



    public override void SetValueWithoutNotify(HexColor newValue)
    {
        base.SetValueWithoutNotify(newValue);
        visualInput.SetValueWithoutNotify(newValue);
    }

    protected override void UpdateMixedValueContent() => visualInput.showMixedValue = showMixedValue;
}