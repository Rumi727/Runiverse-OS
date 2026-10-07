#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    public static class NumericFieldDragUtility
    {
        const decimal dragSensitivity = 0.03m;
        static bool useYSign = false;

        public static sbyte CalculateSensitivity(sbyte value) => (value.ClampToDouble().Abs().Sqrt() * (float)dragSensitivity).ClampToSByte().Clamp(1);
        public static byte CalculateSensitivity(byte value) => (value.ClampToDouble().Abs().Sqrt() * (float)dragSensitivity).ClampToByte().Clamp(1);
        public static short CalculateSensitivity(short value) => (value.ClampToDouble().Abs().Sqrt() * (float)dragSensitivity).ClampToShort().Clamp(1);
        public static ushort CalculateSensitivity(ushort value) => (value.ClampToDouble().Abs().Sqrt() * (float)dragSensitivity).ClampToUShort().Clamp(1);
        public static int CalculateSensitivity(int value) => (value.ClampToDouble().Abs().Sqrt() * (float)dragSensitivity).ClampToInt().Clamp(1);
        public static uint CalculateSensitivity(uint value) => (value.ClampToDouble().Abs().Sqrt() * (float)dragSensitivity).ClampToUInt().Clamp(1);
        public static long CalculateSensitivity(long value) => (value.ClampToDouble().Abs().Sqrt() * (float)dragSensitivity).ClampToLong().Clamp(1);
        public static ulong CalculateSensitivity(ulong value) => (value.ClampToDouble().Abs().Sqrt() * (float)dragSensitivity).ClampToULong().Clamp(1);
        public static nint CalculateSensitivity(nint value) => (value.ClampToDouble().Abs().Sqrt() * (float)dragSensitivity).ClampToNInt().Clamp(1);
        public static nuint CalculateSensitivity(nuint value) => (value.ClampToDouble().Abs().Sqrt() * (float)dragSensitivity).ClampToNUInt().Clamp(1);

        public static float CalculateSensitivity(float value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value))
                return 0;

            return value.Abs().Sqrt().Clamp(1) * (float)dragSensitivity;
        }

        public static double CalculateSensitivity(double value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value))
                return 0;

            return value.Abs().Sqrt().Clamp(1) * (double)dragSensitivity;
        }

        public static decimal CalculateSensitivity(decimal value) => value.Abs().Sqrt().Clamp(1) * dragSensitivity;

        public static sbyte CalculateRangeSensitivity(sbyte minValue, sbyte maxValue) => ((minValue.Distance(maxValue) / 100f) * (float)dragSensitivity).ClampToSByte().Clamp(1);
        public static byte CalculateRangeSensitivity(byte minValue, byte maxValue) => ((minValue.Distance(maxValue) / 100f) * (float)dragSensitivity).ClampToByte().Clamp(1);
        public static short CalculateRangeSensitivity(short minValue, short maxValue) => ((minValue.Distance(maxValue) / 100f) * (float)dragSensitivity).ClampToShort().Clamp(1);
        public static ushort CalculateRangeSensitivity(ushort minValue, ushort maxValue) => ((minValue.Distance(maxValue) / 100f) * (float)dragSensitivity).ClampToUShort().Clamp(1);
        public static int CalculateRangeSensitivity(int minValue, int maxValue) => ((minValue.Distance(maxValue) / 100f) * (float)dragSensitivity).ClampToInt().Clamp(1);
        public static uint CalculateRangeSensitivity(uint minValue, uint maxValue) => ((minValue.Distance(maxValue) / 100f) * (float)dragSensitivity).ClampToUInt().Clamp(1);
        public static long CalculateRangeSensitivity(long minValue, long maxValue) => ((minValue.Distance(maxValue) / 100f) * (float)dragSensitivity).ClampToLong().Clamp(1);
        public static ulong CalculateRangeSensitivity(ulong minValue, ulong maxValue) => ((minValue.Distance(maxValue) / 100f) * (float)dragSensitivity).ClampToULong().Clamp(1);

        public static float CalculateRangeSensitivity(float minValue, float maxValue) => (minValue.Distance(maxValue) / 100f) * (float)dragSensitivity;
        public static double CalculateRangeSensitivity(double minValue, double maxValue) => (minValue.Distance(maxValue) / 100d) * (double)dragSensitivity;
        public static decimal CalculateRangeSensitivity(decimal minValue, decimal maxValue) => (minValue.Distance(maxValue) / 100m) * dragSensitivity;

        public static float GetSpeedMultiplier(DeltaSpeed speed) => GetSpeedMultiplier(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);
        public static float GetSpeedMultiplier(bool shiftPressed, bool altPressed) => (shiftPressed ? 4f : 1) * (altPressed ? 0.25f : 1);

        public static float GetSignedDelta(Vector2 deviceDelta, DeltaSpeed speed) => GetSignedDelta(deviceDelta, GetSpeedMultiplier(speed));
        public static float GetSignedDelta(Vector2 deviceDelta, float multiplier)
        {
            deviceDelta.y = -deviceDelta.y;
            if (deviceDelta.x.Abs().Distance(deviceDelta.y.Abs()) / Max(deviceDelta.x.Abs(), deviceDelta.y.Abs()) > 0.1f)
                useYSign = deviceDelta.x.Abs() <= deviceDelta.y.Abs();

            if (useYSign)
                return deviceDelta.magnitude * deviceDelta.y.Sign() * multiplier;
            else
                return deviceDelta.magnitude * deviceDelta.x.Sign() * multiplier;
        }
    }
}