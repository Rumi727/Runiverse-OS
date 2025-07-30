using Unity.Properties;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements
{
    [UxmlElement]
    public partial class HexColorField : BaseField<HexColor>
    {
        public new const string ussClassName = "runios-hex-color-field";
        
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
            visualInput = this.Q<ColorField>(className: inputUssClassName);

            visualInput.RegisterCallback<ChangeEvent<Color>>(ChangeEventCallback);
        }
        
        void ChangeEventCallback(ChangeEvent<Color> x) => value = new HexColor(x.newValue);



        public override void SetValueWithoutNotify(HexColor newValue)
        {
            base.SetValueWithoutNotify(newValue);
            visualInput.SetValueWithoutNotify(newValue);
        }

        protected override void UpdateMixedValueContent() => visualInput.showMixedValue = showMixedValue;
    }
}