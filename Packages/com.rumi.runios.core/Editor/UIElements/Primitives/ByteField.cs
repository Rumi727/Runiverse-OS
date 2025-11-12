#nullable enable
using RuniOS.Editor.APIBridge.UnityEngine;
using RuniOS.Editor.APIBridge.UnityEngine.UIElements;
using RuniOS.Editor.APIMarshal.UnityEngine.UIElements;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Primitives
{
    [UxmlElement]
    public partial class ByteField : TextValueFieldMarshal<byte>
    {
        public new const string ussClassName = "runios-byte-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";
 
        public ByteInput byteInput => (ByteInput)textInputBase;
        
        
        
        public ByteField() : this(string.Empty) { }

        public ByteField(int maxLength) : this(string.Empty, maxLength) { }
        
        public ByteField(string label, int maxLength = 1000) : base(label, maxLength, new ByteInput())
        {
            AddToClassList(ussClassName);
            
            labelElement.AddToClassList(labelUssClassName);
            byteInput.AddToClassList(inputUssClassName);
            
            AddLabelDragger<byte>();
        }
        
        
        
        protected override string ValueToString(byte v) => v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

        protected override byte StringToValue(string str)
        {
            bool flag = UINumericFieldsUtilsBridge.TryConvertStringToInt(str, textInputBase.originalText, out int num, out ExpressionEvaluatorBridge.ExpressionBridge expression);
            
            Action<ExpressionEvaluatorBridge.ExpressionBridge>? expressionEvaluated = BaseFieldBridge<byte>.__GetInstanceFrom(this).expressionEvaluated;
            expressionEvaluated?.Invoke(expression);
            
            return flag ? num.ClampToByte() : rawValue;
        }
        
        
        
        public override bool CanTryParse(string textString) => byte.TryParse(textString, out byte _);
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, byte startValue) => byteInput.ApplyInputDeviceDelta(delta, speed, startValue);


        public class ByteInput : TextValueInputMarshal
        {
            public ByteField parentIntegerField => (ByteField)parent;

            public ByteInput() => formatString = UINumericFieldsUtilsBridge.k_IntFieldFormatString;

            protected override string allowedCharacters => UINumericFieldsUtilsBridge.k_AllowedCharactersForInt;

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, byte startValue)
            {
                double dragSensitivity = NumericFieldDraggerUtilityBridge.CalculateIntDragSensitivity(startValue);
                float acceleration = NumericFieldDraggerUtilityBridge.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                long value = StringToValue(text) + (long)(NumericFieldDraggerUtilityBridge.NiceDelta(delta, acceleration) * dragSensitivity).Round();
                
                if (parentIntegerField.isDelayed)
                    text = ValueToString(value.ClampToByte());
                else
                    parentIntegerField.value = value.ClampToByte();
            }

            protected override string ValueToString(byte v) => v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

            protected override byte StringToValue(string str) => parentIntegerField.StringToValue(str);
        }
    }
}
