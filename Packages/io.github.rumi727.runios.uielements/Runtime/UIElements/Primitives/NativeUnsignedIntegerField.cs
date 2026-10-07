#nullable enable
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Primitives
{
    [UxmlElement]
    public partial class NativeUnsignedIntegerField : TextValueField<UIntPtr>
    {
        public new const string ussClassName = "runios-native-integer-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";

        public NativeUnsignedIntegerField() : this(string.Empty) { }

        public NativeUnsignedIntegerField(int maxLength) : this(string.Empty, maxLength) { }

        public NativeUnsignedIntegerField(string label, int maxLength = 1000) : base(label, maxLength, new NativeUnsignedIntegerInput())
        {
            AddToClassList(ussClassName);

            labelElement.AddToClassList(labelUssClassName);
            nativeIntegerInput.AddToClassList(inputUssClassName);

            AddLabelDragger<UIntPtr>();
        }

        NativeUnsignedIntegerInput nativeIntegerInput => (NativeUnsignedIntegerInput)textInputBase;

        protected override string ValueToString(UIntPtr value) => value.ClampToULong().ToString(formatString, CultureInfo.InvariantCulture);

        protected override UIntPtr StringToValue(string value)
        {
            if (ulong.TryParse(value, out ulong result))
                return (UIntPtr)result;
            else
                return rawValue;
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, UIntPtr startValue) => nativeIntegerInput.ApplyInputDeviceDelta(delta, speed, startValue);

        protected class NativeUnsignedIntegerInput : TextValueInput
        {
            public new NativeUnsignedIntegerField parent => (NativeUnsignedIntegerField)base.parent;

            protected override string allowedCharacters => "0123456789";

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, UIntPtr startValue)
            {
                nuint dragSensitivity = NumericFieldDragUtility.CalculateSensitivity(startValue);

                float signedDelta = NumericFieldDragUtility.GetSignedDelta(delta, speed) * dragSensitivity;
                ulong deltaMagnitude = Math.Abs(signedDelta).RoundToULong();

                ulong value = StringToValue(text).ToUInt64();

                if (signedDelta < 0)
                    value = deltaMagnitude > value ? 0 : value - deltaMagnitude;
                else
                    value = deltaMagnitude > ulong.MaxValue - value ? ulong.MaxValue : value + deltaMagnitude;

                if (parent.isDelayed)
                    text = ValueToString(value.ClampToNUInt());
                else
                    parent.value = value.ClampToNUInt();
            }

            protected override string ValueToString(UIntPtr value) => parent.ValueToString(value);

            protected override UIntPtr StringToValue(string value) => parent.StringToValue(value);
        }
    }
}