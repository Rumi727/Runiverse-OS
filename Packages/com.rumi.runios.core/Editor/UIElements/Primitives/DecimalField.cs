#nullable enable
using RuniOS.Editor.APIBridge.UnityEngine;
using RuniOS.Editor.APIBridge.UnityEngine.UIElements;
using RuniOS.Editor.APIMarshal.UnityEngine.UIElements;
using System.Globalization;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Primitives
{
    public partial class DecimalField : TextValueFieldMarshal<decimal>
    {
        public new const string ussClassName = "runios-decimal-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";
 
        public DecimalInput decimalInput => (DecimalInput)textInputBase;
        
        
        
        public DecimalField() : this(string.Empty) { }

        public DecimalField(int maxLength) : this(string.Empty, maxLength) { }
        
        public DecimalField(string label, int maxLength = 1000) : base(label, maxLength, new DecimalInput())
        {
            AddToClassList(ussClassName);
            
            labelElement.AddToClassList(labelUssClassName);
            decimalInput.AddToClassList(inputUssClassName);
            
            AddLabelDragger<decimal>();
        }
        
        
        
        protected override string ValueToString(decimal v) => v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

        protected override decimal StringToValue(string str)
        {
            bool flag = UINumericFieldsUtilsBridge.TryConvertStringToDouble(str, textInputBase.originalText, out double num, out ExpressionEvaluatorBridge.ExpressionBridge expression);
            
            Action<ExpressionEvaluatorBridge.ExpressionBridge>? expressionEvaluated = BaseFieldBridge<decimal>.__GetInstanceFrom(this).expressionEvaluated;
            expressionEvaluated?.Invoke(expression);
            
            return flag ? num.ClampToDecimal() : rawValue;
        }
        
        
        
        public override bool CanTryParse(string textString) => decimal.TryParse(textString, out decimal _);
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, decimal startValue) => decimalInput.ApplyInputDeviceDelta(delta, speed, startValue);


        public class DecimalInput : TextValueInputMarshal
        {
            public DecimalField parentDecimalField => (DecimalField)parent;

            public DecimalInput() => formatString = "G";

            protected override string allowedCharacters => UINumericFieldsUtilsBridge.k_AllowedCharactersForFloat;

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, decimal startValue)
            {
                decimal dragSensitivity = NumericFieldDraggerUtilityBridge.CalculateDecimalDragSensitivity(startValue);
                float acceleration = NumericFieldDraggerUtilityBridge.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                decimal value = (StringToValue(text) + (NumericFieldDraggerUtilityBridge.NiceDelta(delta, acceleration).ClampToDecimal() * dragSensitivity)).RoundBasedOnMinimumDifference(dragSensitivity);
                
                if (parentDecimalField.isDelayed)
                    text = ValueToString(value);
                else
                    parentDecimalField.value = value;
            }

            protected override string ValueToString(decimal v) => v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

            protected override decimal StringToValue(string str) => parentDecimalField.StringToValue(str);
        }
    }
}