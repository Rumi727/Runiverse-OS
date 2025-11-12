#nullable enable
using RuniOS.APIBridge.UnityEngine;
using RuniOS.APIBridge.UnityEngine.UIElements;
using RuniOS.APIMarshal.UnityEngine.UIElements;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Primitives
{
    public partial class NativeIntegerField : TextValueFieldMarshal<IntPtr>
    {
        public new const string ussClassName = "runios-native-integer-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";
 
        public NativeIntegerInput nativeIntegerInput => (NativeIntegerInput)textInputBase;
        
        
        
        public NativeIntegerField() : this(string.Empty) { }

        public NativeIntegerField(int maxLength) : this(string.Empty, maxLength) { }
        
        public NativeIntegerField(string label, int maxLength = 1000) : base(label, maxLength, new NativeIntegerInput())
        {
            AddToClassList(ussClassName);
            
            labelElement.AddToClassList(labelUssClassName);
            nativeIntegerInput.AddToClassList(inputUssClassName);
            
            AddLabelDragger<IntPtr>();
        }
        
        
        
        protected override string ValueToString(IntPtr v) => v.ClampToLong().ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

        protected override IntPtr StringToValue(string str)
        {
            bool flag = UINumericFieldsUtilsBridge.TryConvertStringToInt(str, textInputBase.originalText, out int num, out ExpressionEvaluatorBridge.ExpressionBridge expression);
            
            Action<ExpressionEvaluatorBridge.ExpressionBridge>? expressionEvaluated = BaseFieldBridge<IntPtr>.__GetInstanceFrom(this).expressionEvaluated;
            expressionEvaluated?.Invoke(expression);
            
            return flag ? num.ClampToNInt() : rawValue;
        }
        
        
        
        public override bool CanTryParse(string textString) => long.TryParse(textString, out long _);
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, IntPtr startValue) => nativeIntegerInput.ApplyInputDeviceDelta(delta, speed, startValue);


        public class NativeIntegerInput : TextValueInputMarshal
        {
            public NativeIntegerField parentNativeIntegerField => (NativeIntegerField)parent;

            public NativeIntegerInput() => formatString = UINumericFieldsUtilsBridge.k_IntFieldFormatString;

            protected override string allowedCharacters => UINumericFieldsUtilsBridge.k_AllowedCharactersForInt;

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, IntPtr startValue)
            {
                double dragSensitivity = NumericFieldDraggerUtilityBridge.CalculateIntDragSensitivity(startValue.ClampToLong());
                float acceleration = NumericFieldDraggerUtilityBridge.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                
                long value = StringToValue(text).ClampToLong();
                long minMaxValue = ClampMinMaxLongValue((long)(NumericFieldDraggerUtilityBridge.NiceDelta(delta, acceleration) * dragSensitivity).Round(), value);
                
                if (parentNativeIntegerField.isDelayed)
                    text = ValueToString(minMaxValue.ClampToNInt());
                else
                    parentNativeIntegerField.value = minMaxValue.ClampToNInt();
            }
            
            long ClampMinMaxLongValue(long niceDelta, long value)
            {
                long num = niceDelta.Abs();
                return niceDelta > 0L ? (value > 0L && num > long.MaxValue - value ? long.MaxValue : value + niceDelta) : (value < 0L && value < long.MinValue + num ? long.MinValue : value - num);
            }

            protected override string ValueToString(IntPtr v) => v.ClampToLong().ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

            protected override IntPtr StringToValue(string str) => parentNativeIntegerField.StringToValue(str);
        }
    }
}
