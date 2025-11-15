#nullable enable
using RuniOS.Editor.APIBridge.UnityEngine;
using RuniOS.Editor.APIBridge.UnityEngine.UIElements;
using RuniOS.Editor.APIMarshal.UnityEngine.UIElements;
using System.Globalization;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Primitives;

[UxmlElement]
public partial class ShortField : TextValueFieldMarshal<short>
{
    public new const string ussClassName = "runios-short-field";
    public new const string labelUssClassName = ussClassName + "__label";
    public new const string inputUssClassName = ussClassName + "__input";
 
    public ShortInput shortInput => (ShortInput)textInputBase;
        
        
        
    public ShortField() : this(string.Empty) { }

    public ShortField(int maxLength) : this(string.Empty, maxLength) { }
        
    public ShortField(string label, int maxLength = 1000) : base(label, maxLength, new ShortInput())
    {
        AddToClassList(ussClassName);
            
        labelElement.AddToClassList(labelUssClassName);
        shortInput.AddToClassList(inputUssClassName);
            
        AddLabelDragger<short>();
    }
        
        
        
    protected override string ValueToString(short v) => v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

    protected override short StringToValue(string str)
    {
        bool flag = UINumericFieldsUtilsBridge.TryConvertStringToInt(str, textInputBase.originalText, out int num, out ExpressionEvaluatorBridge.ExpressionBridge expression);
            
        Action<ExpressionEvaluatorBridge.ExpressionBridge>? expressionEvaluated = BaseFieldBridge<short>.__GetInstanceFrom(this).expressionEvaluated;
        expressionEvaluated?.Invoke(expression);
            
        return flag ? num.ClampToShort() : rawValue;
    }
        
        
        
    public override bool CanTryParse(string textString) => short.TryParse(textString, out short _);
    public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, short startValue) => shortInput.ApplyInputDeviceDelta(delta, speed, startValue);


    public class ShortInput : TextValueInputMarshal
    {
        public ShortField parentShortField => (ShortField)parent;

        public ShortInput() => formatString = UINumericFieldsUtilsBridge.k_IntFieldFormatString;

        protected override string allowedCharacters => UINumericFieldsUtilsBridge.k_AllowedCharactersForInt;

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, short startValue)
        {
            double dragSensitivity = NumericFieldDraggerUtilityBridge.CalculateIntDragSensitivity(startValue);
            float acceleration = NumericFieldDraggerUtilityBridge.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
            long value = StringToValue(text) + (long)(NumericFieldDraggerUtilityBridge.NiceDelta(delta, acceleration) * dragSensitivity).Round();
                
            if (parentShortField.isDelayed)
                text = ValueToString(value.ClampToShort());
            else
                parentShortField.value = value.ClampToShort();
        }

        protected override string ValueToString(short v) => v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

        protected override short StringToValue(string str) => parentShortField.StringToValue(str);
    }
}