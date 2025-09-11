#nullable enable
using RuniOS.APIBridge.UnityEngine;
using RuniOS.APIBridge.UnityEngine.UIElements;
using RuniOS.APIMarshal.UnityEngine.UIElements;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Primitives
{
    [UxmlElement]
    public partial class UShortField : TextValueFieldMarshal<ushort>
    {
        public new const string ussClassName = "runios-ushort-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";
 
        public UShortInput ushortInput => (UShortInput)textInputBase;
        
        
        
        public UShortField() : this(null) { }

        public UShortField(int maxLength) : this(null, maxLength) { }
        
        public UShortField(string? label, int maxLength = 1000) : base(label, maxLength, new UShortInput())
        {
            AddToClassList(ussClassName);
            
            labelElement.AddToClassList(labelUssClassName);
            ushortInput.AddToClassList(inputUssClassName);
            
            AddLabelDragger<ushort>();
        }
        
        
        
        protected override string ValueToString(ushort v) => v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

        protected override ushort StringToValue(string str)
        {
            bool flag = UINumericFieldsUtilsBridge.TryConvertStringToInt(str, textInputBase.originalText, out int num, out ExpressionEvaluatorBridge.ExpressionBridge expression);
            
            Action<ExpressionEvaluatorBridge.ExpressionBridge>? expressionEvaluated = BaseFieldBridge<ushort>.__GetInstanceFrom(this).expressionEvaluated;
            expressionEvaluated?.Invoke(expression);
            
            return flag ? num.ClampToUShort() : rawValue;
        }
        
        
        
        public override bool CanTryParse(string textString) => ushort.TryParse(textString, out ushort _);
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, ushort startValue) => ushortInput.ApplyInputDeviceDelta(delta, speed, startValue);


        public class UShortInput : TextValueInputMarshal
        {
            public UShortField parentIntegerField => (UShortField)parent;

            public UShortInput() => formatString = UINumericFieldsUtilsBridge.k_IntFieldFormatString;

            protected override string allowedCharacters => UINumericFieldsUtilsBridge.k_AllowedCharactersForInt;

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, ushort startValue)
            {
                double dragSensitivity = NumericFieldDraggerUtilityBridge.CalculateIntDragSensitivity(startValue);
                float acceleration = NumericFieldDraggerUtilityBridge.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                long value = StringToValue(text) + (NumericFieldDraggerUtilityBridge.NiceDelta(delta, acceleration) * dragSensitivity).RoundToLong();
                
                if (parentIntegerField.isDelayed)
                    text = ValueToString(value.ClampToUShort());
                else
                    parentIntegerField.value = value.ClampToUShort();
            }

            protected override string ValueToString(ushort v) => v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

            protected override ushort StringToValue(string str) => parentIntegerField.StringToValue(str);
        }
    }
}
