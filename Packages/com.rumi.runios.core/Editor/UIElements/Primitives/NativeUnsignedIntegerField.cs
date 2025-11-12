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
    public partial class NativeUnsignedIntegerField : TextValueFieldMarshal<UIntPtr>
    {
        public new const string ussClassName = "runios-native-integer-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";
 
        public NativeUnsignedIntegerInput nativeIntegerInput => (NativeUnsignedIntegerInput)textInputBase;
        
        
        
        public NativeUnsignedIntegerField() : this(string.Empty) { }

        public NativeUnsignedIntegerField(int maxLength) : this(string.Empty, maxLength) { }
        
        public NativeUnsignedIntegerField(string label, int maxLength = 1000) : base(label, maxLength, new NativeUnsignedIntegerInput())
        {
            AddToClassList(ussClassName);
            
            labelElement.AddToClassList(labelUssClassName);
            nativeIntegerInput.AddToClassList(inputUssClassName);
            
            AddLabelDragger<UIntPtr>();
        }
        
        
        
        protected override string ValueToString(UIntPtr v) => v.ClampToULong().ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

        protected override UIntPtr StringToValue(string str)
        {
            bool flag = UINumericFieldsUtilsBridge.TryConvertStringToInt(str, textInputBase.originalText, out int num, out ExpressionEvaluatorBridge.ExpressionBridge expression);
            
            Action<ExpressionEvaluatorBridge.ExpressionBridge>? expressionEvaluated = BaseFieldBridge<UIntPtr>.__GetInstanceFrom(this).expressionEvaluated;
            expressionEvaluated?.Invoke(expression);
            
            return flag ? num.ClampToNUInt() : rawValue;
        }
        
        
        
        public override bool CanTryParse(string textString) => long.TryParse(textString, out long _);
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, UIntPtr startValue) => nativeIntegerInput.ApplyInputDeviceDelta(delta, speed, startValue);


        public class NativeUnsignedIntegerInput : TextValueInputMarshal
        {
            public NativeUnsignedIntegerField parentNativeIntegerField => (NativeUnsignedIntegerField)parent;

            public NativeUnsignedIntegerInput() => formatString = UINumericFieldsUtilsBridge.k_IntFieldFormatString;

            protected override string allowedCharacters => UINumericFieldsUtilsBridge.k_AllowedCharactersForInt;

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, UIntPtr startValue)
            {
                double dragSensitivity = NumericFieldDraggerUtilityBridge.CalculateIntDragSensitivity(startValue.ClampToULong());
                float acceleration = NumericFieldDraggerUtilityBridge.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
                
                ulong value = StringToValue(text).ClampToULong();
                ulong minMaxValue = ClampToMinMaxULongValue((long)(NumericFieldDraggerUtilityBridge.NiceDelta(delta, acceleration) * dragSensitivity).Round(), value);
                
                if (parentNativeIntegerField.isDelayed)
                    text = ValueToString(minMaxValue.ClampToNUInt());
                else
                    parentNativeIntegerField.value = minMaxValue.ClampToNUInt();
            }

            static ulong ClampToMinMaxULongValue(long niceDelta, ulong value)
            {
                ulong num = (ulong)niceDelta.Abs();
                return niceDelta > 0L ? (num > ulong.MaxValue - value ? ulong.MaxValue : value + num) : (num > value ? 0UL : value - num);
            }

            protected override string ValueToString(UIntPtr v) => v.ClampToULong().ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

            protected override UIntPtr StringToValue(string str) => parentNativeIntegerField.StringToValue(str);
        }
    }
}
