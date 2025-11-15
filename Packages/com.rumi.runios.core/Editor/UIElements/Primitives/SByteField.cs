#nullable enable
using RuniOS.Editor.APIBridge.UnityEngine;
using RuniOS.Editor.APIBridge.UnityEngine.UIElements;
using RuniOS.Editor.APIMarshal.UnityEngine.UIElements;
using System.Globalization;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Primitives;

[UxmlElement]
public partial class SByteField : TextValueFieldMarshal<sbyte>
{
    public new const string ussClassName = "runios-sbyte-field";
    public new const string labelUssClassName = ussClassName + "__label";
    public new const string inputUssClassName = ussClassName + "__input";
 
    public SByteInput sbyteInput => (SByteInput)textInputBase;
        
        
        
    public SByteField() : this(string.Empty) { }

    public SByteField(int maxLength) : this(string.Empty, maxLength) { }
        
    public SByteField(string label, int maxLength = 1000) : base(label, maxLength, new SByteInput())
    {
        AddToClassList(ussClassName);
            
        labelElement.AddToClassList(labelUssClassName);
        sbyteInput.AddToClassList(inputUssClassName);
            
        AddLabelDragger<sbyte>();
    }
        
        
        
    protected override string ValueToString(sbyte v) => v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

    protected override sbyte StringToValue(string str)
    {
        bool flag = UINumericFieldsUtilsBridge.TryConvertStringToInt(str, textInputBase.originalText, out int num, out ExpressionEvaluatorBridge.ExpressionBridge expression);
            
        Action<ExpressionEvaluatorBridge.ExpressionBridge>? expressionEvaluated = BaseFieldBridge<sbyte>.__GetInstanceFrom(this).expressionEvaluated;
        expressionEvaluated?.Invoke(expression);
            
        return flag ? num.ClampToSByte() : rawValue;
    }
        
        
        
    public override bool CanTryParse(string textString) => sbyte.TryParse(textString, out sbyte _);
    public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, sbyte startValue) => sbyteInput.ApplyInputDeviceDelta(delta, speed, startValue);


    public class SByteInput : TextValueInputMarshal
    {
        public SByteField parentIntegerField => (SByteField)parent;

        public SByteInput() => formatString = UINumericFieldsUtilsBridge.k_IntFieldFormatString;

        protected override string allowedCharacters => UINumericFieldsUtilsBridge.k_AllowedCharactersForInt;

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, sbyte startValue)
        {
            double dragSensitivity = NumericFieldDraggerUtilityBridge.CalculateIntDragSensitivity(startValue);
            float acceleration = NumericFieldDraggerUtilityBridge.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
            long value = StringToValue(text) + (long)(NumericFieldDraggerUtilityBridge.NiceDelta(delta, acceleration) * dragSensitivity).Round();
                
            if (parentIntegerField.isDelayed)
                text = ValueToString(value.ClampToSByte());
            else
                parentIntegerField.value = value.ClampToSByte();
        }

        protected override string ValueToString(sbyte v) => v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

        protected override sbyte StringToValue(string str) => parentIntegerField.StringToValue(str);
    }
}