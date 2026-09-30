#nullable enable
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Primitives
{
    [UxmlElement]
    public partial class NativeIntegerField : TextValueField<IntPtr>
    {
        public new const string ussClassName = "runios-native-integer-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";

        public NativeIntegerField() : this(string.Empty) { }

        public NativeIntegerField(int maxLength) : this(string.Empty, maxLength) { }

        public NativeIntegerField(string label, int maxLength = 1000) : base(label, maxLength, new NativeIntegerInput())
        {
            AddToClassList(ussClassName);

            labelElement.AddToClassList(labelUssClassName);
            nativeIntegerInput.AddToClassList(inputUssClassName);

            AddLabelDragger<IntPtr>();
        }

        NativeIntegerInput nativeIntegerInput => (NativeIntegerInput)textInputBase;

        protected override string ValueToString(IntPtr value) => value.ToInt64().ToString(formatString, CultureInfo.InvariantCulture);

        protected override IntPtr StringToValue(string value)
        {
            if (long.TryParse(value, out long result))
                return (IntPtr)result;
            else
                return rawValue;
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, IntPtr startValue) => nativeIntegerInput.ApplyInputDeviceDelta(delta, speed, startValue);

        protected class NativeIntegerInput : TextValueInput
        {
            public new NativeIntegerField parent => (NativeIntegerField)base.parent;

            protected override string allowedCharacters => "0123456789-";

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, IntPtr startValue)
            {
                nint dragSensitivity = NumericFieldDragUtility.CalculateSensitivity(startValue);
                long value = StringToValue(text).ToInt64() + (NumericFieldDragUtility.GetSignedDelta(delta, speed) * dragSensitivity).RoundToLong();

                if (parent.isDelayed)
                    text = ValueToString(value.ClampToNInt());
                else
                    parent.value = value.ClampToNInt();
            }

            protected override string ValueToString(IntPtr value) => parent.ValueToString(value);

            protected override IntPtr StringToValue(string value) => parent.StringToValue(value);
        }
    }
}