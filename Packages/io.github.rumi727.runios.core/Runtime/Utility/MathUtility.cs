#nullable enable
using ExtendedNumerics;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

namespace RuniOS.Utility
{
    public static class MathUtility
    {
        const int maxRoundingDigits = 15;
        static readonly double[] roundPower10Double =
        [
            1E+0, 1E+1, 1E+2, 1E+3, 1E+4, 1E+5, 1E+6, 1E+7, 1E+8, 1E+9,
            1E+10, 1E+11, 1E+12, 1E+13, 1E+14, 1E+15
        ];

        static readonly BigDecimal[] roundPower10BigDecimal =
        [
            1E+0, 1E+1, 1E+2, 1E+3, 1E+4, 1E+5, 1E+6, 1E+7, 1E+8, 1E+9,
            1E+10, 1E+11, 1E+12, 1E+13, 1E+14, 1E+15
        ];

        public const float pi = (float)Math.PI;
        public const float e = (float)Math.E;
        public const float deg2Rad = pi / 180;
        public const float rad2Deg = 180 / pi;

        public const double piDouble = Math.PI;
        public const double eDouble = Math.E;
        public const double deg2RadDouble = piDouble / 180;
        public const double rad2DegDouble = 180 / piDouble;

        public const float epsilonFloatWithAccuracy = 0.0001f;
        
        public static long nintMinSize => -(1L << ((8 * UIntPtr.Size) - 1));
        public static long nintMaxSize => (1L << ((8 * UIntPtr.Size) - 1)) - 1;

        public const ulong nuintMinSize = 0;
        public static ulong nuintMaxSize
        {
            get
            {
                if (UIntPtr.Size >= 8)
                    return ulong.MaxValue;
                else
                    return (1uL << ((8 * UIntPtr.Size) - 1));
            }
        }

        /*public static void FunctionList()
        {
            Abs();
            Acos();
            Acosh();
            Approximately(); //Mathf
            Asin();
            Asinh();
            Atan();
            Atan2();
            Atanh();
            Cbrt();
            Ceiling(); //Ceil
            Clamp();
            Clamp01(); //Mathf
            ClosestPowerOfTwo(); //Mathf
            CorrelatedColorTemperatureToRGB(); //Mathf
            Cos();
            Cosh();
            DeltaAngle(); //Mathf
            Exp();
            Floor();
            Gamma(); //Mathf
            GammaToLinearSpace(); //Mathf
            IEEERemainder();
            InverseLerp(); //Mathf
            IsPowerOfTwo(); //Mathf
            Lerp(); //Mathf
            LerpAngle(); //Mathf
            LinearToGammaSpace(); //Mathf
            Log();
            Log10();
            Max();
            Min();
            MoveTowards(); //Mathf
            MoveTowardsAngle(); //Mathf
            NextPowerOfTwo(); //Mathf
            PerlinNoise(); //Mathf
            PingPong(); //Mathf
            Pow();
            Repeat(); //Mathf
            Round();
            Sign();
            Sin();
            Sinh();
            SmoothDamp(); //Mathf
            SmoothDampAngle(); //Mathf
            SmoothStep(); //Mathf
            Sqrt();
            Tan();
            Tanh();
            Truncate();
        }*/

        #region Abs
        public static T Abs<T>(this T value) where T : INumberBase<T> => T.Abs(value);

        public static BigDecimal Abs(this BigDecimal value) => value < 0 ? -value : value;
        #endregion Abs

        #region Acos
        public static T Acos<T>(this T value) where T : ITrigonometricFunctions<T> => T.Acos(value);
        public static decimal Acos(this decimal value) => DecimalMath.Acos(value);
        #endregion

        #region Acosh
        public static T Acosh<T>(this T value) where T : IHyperbolicFunctions<T> => T.Acosh(value);
        #endregion

        #region Arithmetic Sequence Sum
        public static T ArithmeticSequenceSum<T>(this T start, T end) where T : INumber<T> => ((start.Distance(end) + T.One) * (start + end)) / (T.One + T.One);
        public static BigDecimal ArithmeticSequenceSum(this BigDecimal start, BigDecimal end) => (start.Distance(end) + 1) * (start + end) * 0.5;
        #endregion

        public static float Angle(this Vector2 current, Vector2 target)
        {
            Vector2 dir = target - current;
            return Atan2(dir.y, dir.x) * rad2Deg;
        }

        #region Approximately
        public static bool Approximately(this float a, float b) => (b - a).Abs() < Max(1E-06f * Max(a.Abs(), b.Abs()), float.Epsilon * 8f);
        public static bool Approximately(this double a, double b) => (b - a).Abs() < Max(1E-14d * Max(a.Abs(), b.Abs()), double.Epsilon * 8f);
        #endregion

        #region Asin
        public static T Asin<T>(this T value) where T : ITrigonometricFunctions<T> => T.Asin(value);
        public static decimal Asin(this decimal value) => DecimalMath.Asin(value);
        #endregion

        #region Asinh
        public static T Asinh<T>(this T value) where T : IHyperbolicFunctions<T> => T.Asinh(value);
        #endregion

        #region Atan
        public static T Atan<T>(this T value) where T : ITrigonometricFunctions<T> => T.Atan(value);
        public static decimal Atan(this decimal value) => DecimalMath.ATan(value);
        #endregion

        #region Atan2
        public static T Atan2<T>(this T y, T x) where T : IFloatingPointIeee754<T> => T.Atan2(y, x);
        public static decimal Atan2(this decimal y, decimal x) => DecimalMath.Atan2(y, x);
        #endregion

        #region Atanh
        public static T Atanh<T>(this T value) where T : IHyperbolicFunctions<T> => T.Atanh(value);
        #endregion

        #region Cbrt
        public static T Cbrt<T>(this T value) where T : IRootFunctions<T> => T.Cbrt(value);
        #endregion

        #region Ceil
        public static T Ceil<T>(this T value) where T : IFloatingPoint<T> => T.Ceiling(value);
        public static BigDecimal Ceil(this BigDecimal value) => BigDecimal.Ceiling(value);

        public static T Ceil<T>(this T value, int digits) where T : IFloatingPoint<T>
        {
            if (digits < 0 || digits > maxRoundingDigits)
                throw new ArgumentOutOfRangeException(nameof(digits));

            T num = T.CreateChecked(roundPower10Double[digits]);
            return (value * num).Ceil() / num;
        }
        public static BigDecimal Ceil(this BigDecimal value, int digits)
        {
            if (digits < 0 || digits > maxRoundingDigits)
                throw new ArgumentOutOfRangeException(nameof(digits));

            BigDecimal num = roundPower10BigDecimal[digits];
            return (value * num).Ceil() / num;
        }
        #endregion
        
        #region Ceil To SByte
        public static sbyte CeilToSByte<T>(this T value) where T : IFloatingPoint<T> => sbyte.CreateSaturating(T.Ceiling(value));
        #endregion
        
        #region Ceil To Byte
        public static byte CeilToByte<T>(this T value) where T : IFloatingPoint<T> => byte.CreateSaturating(T.Ceiling(value));
        #endregion
        
        #region Ceil To Short
        public static short CeilToShort<T>(this T value) where T : IFloatingPoint<T> => short.CreateSaturating(T.Ceiling(value));
        #endregion
        
        #region Ceil To Ushort
        public static ushort CeilToUShort<T>(this T value) where T : IFloatingPoint<T> => ushort.CreateSaturating(T.Ceiling(value));
        #endregion
        
        #region Ceil To Int
        public static int CeilToInt<T>(this T value) where T : IFloatingPoint<T> => int.CreateSaturating(T.Ceiling(value));
        public static BigInteger CeilToInt(this BigDecimal value) => (BigInteger)BigDecimal.Ceiling(value);
        #endregion
        
        #region Ceil To UInt
        public static uint CeilToUInt<T>(this T value) where T : IFloatingPoint<T> => uint.CreateSaturating(T.Ceiling(value));
        #endregion
        
        #region Ceil To Long
        public static long CeilToLong<T>(this T value) where T : IFloatingPoint<T> => long.CreateSaturating(T.Ceiling(value));
        #endregion
        
        #region Ceil To ULong
        public static ulong CeilToULong<T>(this T value) where T : IFloatingPoint<T> => ulong.CreateSaturating(T.Ceiling(value));
        #endregion

        #region Clamp
        public static T Clamp<T, TMin>(this T value, TMin min) where T : INumber<T> where TMin : INumberBase<TMin>
        {
            T minValue = T.CreateSaturating(min);
            return value < minValue ? minValue : value;
        }

        public static T Clamp<T, TMin, TMax>(this T value, TMin min, TMax max) where T : INumber<T> where TMin : INumberBase<TMin> where TMax : INumberBase<TMax>
        {
            T minValue = T.CreateSaturating(min);
            T maxValue = T.CreateSaturating(max);

            if (value < minValue)
                return minValue;
            else if (value > maxValue)
                return maxValue;
            else
                return value;
        }

        public static BigDecimal Clamp(this BigDecimal value, BigDecimal min) => value < min ? min : value;

        public static BigDecimal Clamp(this BigDecimal value, BigDecimal min, BigDecimal max)
        {
            if (value < min)
                return min;
            else if (value > max)
                return max;
            else
                return value;
        }

        public static Vector2 Clamp(this Vector2 value, Vector2 min) => new Vector2(value.x.Clamp(min.x), value.y.Clamp(min.y));
        public static Vector2 Clamp(this Vector2 value, Vector2 min, Vector2 max) => new Vector2(value.x.Clamp(min.x, max.x), value.y.Clamp(min.y, max.y));
        public static Vector3 Clamp(this Vector3 value, Vector3 min) => new Vector3(value.x.Clamp(min.x), value.y.Clamp(min.y), value.z.Clamp(min.z));
        public static Vector3 Clamp(this Vector3 value, Vector3 min, Vector3 max) => new Vector3(value.x.Clamp(min.x, max.x), value.y.Clamp(min.y, max.y), value.z.Clamp(min.z, max.z));
        public static Vector4 Clamp(this Vector4 value, Vector4 min) => new Vector4(value.x.Clamp(min.x), value.y.Clamp(min.y), value.z.Clamp(min.z), value.w.Clamp(min.w));
        public static Vector4 Clamp(this Vector4 value, Vector4 min, Vector4 max) => new Vector4(value.x.Clamp(min.x, max.x), value.y.Clamp(min.y, max.y), value.z.Clamp(min.z, max.z), value.w.Clamp(min.w, max.w));
        public static Vector2Int Clamp(this Vector2Int value, Vector2Int min) => new Vector2Int(value.x.Clamp(min.x), value.y.Clamp(min.y));
        public static Vector2Int Clamp(this Vector2Int value, Vector2Int min, Vector2Int max) => new Vector2Int(value.x.Clamp(min.x, max.x), value.y.Clamp(min.y, max.y));
        public static Vector3Int Clamp(this Vector3Int value, Vector3Int min) => new Vector3Int(value.x.Clamp(min.x), value.y.Clamp(min.y), value.z.Clamp(min.z));
        public static Vector3Int Clamp(this Vector3Int value, Vector3Int min, Vector3Int max) => new Vector3Int(value.x.Clamp(min.x, max.x), value.y.Clamp(min.y, max.y), value.z.Clamp(min.z, max.z));
        public static Rect Clamp(this Rect value, Rect min) => new Rect(value.x.Clamp(min.x), value.y.Clamp(min.y), value.width.Clamp(min.width), value.height.Clamp(min.height));
        public static Rect Clamp(this Rect value, Rect min, Rect max) => new Rect(value.x.Clamp(min.x, max.x), value.y.Clamp(min.y, max.y), value.width.Clamp(min.width, max.width), value.height.Clamp(min.height, max.height));
        public static RectInt Clamp(this RectInt value, RectInt min) => new RectInt(value.x.Clamp(min.x), value.y.Clamp(min.y), value.width.Clamp(min.width), value.height.Clamp(min.height));
        public static RectInt Clamp(this RectInt value, RectInt min, RectInt max) => new RectInt(value.x.Clamp(min.x, max.x), value.y.Clamp(min.y, max.y), value.width.Clamp(min.width, max.width), value.height.Clamp(min.height, max.height));
        public static Color Clamp(this Color value, Color min) => new Color(value.r.Clamp(min.r), value.g.Clamp(min.g), value.b.Clamp(min.b), value.a.Clamp(min.a));
        public static Color Clamp(this Color value, Color min, Color max) => new Color(value.r.Clamp(min.r, max.r), value.g.Clamp(min.g, max.g), value.b.Clamp(min.b, max.b), value.a.Clamp(min.a, max.a));
        public static Color32 Clamp(this Color32 value, Color32 min) => new Color32(value.r.Clamp(min.r), value.g.Clamp(min.g), value.b.Clamp(min.b), value.a.Clamp(min.a));
        public static Color32 Clamp(this Color32 value, Color32 min, Color32 max) => new Color32(value.r.Clamp(min.r, max.r), value.g.Clamp(min.g, max.g), value.b.Clamp(min.b, max.b), value.a.Clamp(min.a, max.a));
        #endregion

        #region ClampMagnitude
        public static Vector2 ClampMagnitude(this Vector2 value, float maxLength) => Vector2.ClampMagnitude(value, maxLength);
        public static Vector3 ClampMagnitude(this Vector3 value, float maxLength) => Vector3.ClampMagnitude(value, maxLength);
        #endregion

        #region Clamp01
        public static T Clamp01<T>(this T value) where T : INumber<T> => value < T.Zero ? T.Zero : value > T.One ? T.One : value;

        public static BigDecimal Clamp01(this BigDecimal value)
        {
            if (value < 0)
                return BigDecimal.Zero;
            else if (value > 1)
                return BigDecimal.One;
            else
                return value;
        }

        public static Vector2 Clamp01(this Vector2 value) => new Vector2(value.x.Clamp01(), value.y.Clamp01());
        public static Vector3 Clamp01(this Vector3 value) => new Vector3(value.x.Clamp01(), value.y.Clamp01(), value.z.Clamp01());
        public static Vector4 Clamp01(this Vector4 value) => new Vector4(value.x.Clamp01(), value.y.Clamp01(), value.z.Clamp01(), value.w.Clamp01());
        public static Vector2Int Clamp01(this Vector2Int value) => new Vector2Int(value.x.Clamp01(), value.y.Clamp01());
        public static Vector3Int Clamp01(this Vector3Int value) => new Vector3Int(value.x.Clamp01(), value.y.Clamp01(), value.z.Clamp01());
        public static Rect Clamp01(this Rect value) => new Rect(value.x.Clamp01(), value.y.Clamp01(), value.width.Clamp01(), value.height.Clamp01());
        public static RectInt Clamp01(this RectInt value) => new RectInt(value.x.Clamp01(), value.y.Clamp01(), value.width.Clamp01(), value.height.Clamp01());
        public static Color Clamp01(this Color value) => new Color(value.r.Clamp01(), value.g.Clamp01(), value.b.Clamp01(), value.a.Clamp01());
        public static Color32 Clamp01(this Color32 value) => new Color32(value.r.Clamp01(), value.g.Clamp01(), value.b.Clamp01(), value.a.Clamp01());
        #endregion

        public static int ClosestPowerOfTwo(this int value) => Mathf.ClosestPowerOfTwo(value);

        public static Color CorrelatedColorTemperatureToRGB(this float value) => Mathf.CorrelatedColorTemperatureToRGB(value);

        #region Cos
        public static T Cos<T>(this T value) where T : ITrigonometricFunctions<T> => T.Cos(value);
        public static decimal Cos(this decimal value) => DecimalMath.Cos(value);
        #endregion

        #region Cosh
        public static T Cosh<T>(this T value) where T : IHyperbolicFunctions<T> => T.Cosh(value);
        public static decimal Cosh(this decimal value) => DecimalMath.Cosh(value);
        #endregion

        #region Delta Angle
        public static T DeltaAngle<T>(this T value, T target) where T : INumber<T>
        {
            T fullTurn = T.CreateChecked(360);
            T result = (target - value).Repeat(fullTurn);
            if (result > T.CreateChecked(180))
                result -= fullTurn;

            return result;
        }

        public static BigDecimal DeltaAngle(this BigDecimal value, BigDecimal target)
        {
            BigDecimal result = (target - value).Repeat(360);
            if (result > 180)
                result -= 360;

            return result;
        }

        #endregion

        #region Distance
        public static T Distance<T>(this T a, T b) where T : INumber<T> => a > b ? a - b : b - a;

        public static BigDecimal Distance(this BigDecimal a, BigDecimal b) => (a - b).Abs();
        #endregion

        #region Exp
        public static T Exp<T>(this T value) where T : IExponentialFunctions<T> => T.Exp(value);
        public static decimal Exp(this decimal value) => DecimalMath.Exp(value);
        public static BigDecimal Exp(this BigInteger value) => BigDecimal.Exp(value);
        #endregion

        #region Floor
        public static T Floor<T>(this T value) where T : IFloatingPoint<T> => T.Floor(value);
        public static BigDecimal Floor(this BigDecimal value) => BigDecimal.Floor(value);

        public static T Floor<T>(this T value, int digits) where T : IFloatingPoint<T>
        {
            if (digits < 0 || digits > maxRoundingDigits)
                throw new ArgumentOutOfRangeException(nameof(digits));

            T num = T.CreateChecked(roundPower10Double[digits]);
            return (value * num).Floor() / num;
        }
        public static BigDecimal Floor(this BigDecimal value, int digits)
        {
            if (digits < 0 || digits > maxRoundingDigits)
                throw new ArgumentOutOfRangeException(nameof(digits));

            BigDecimal num = roundPower10BigDecimal[digits];
            return (value * num).Floor() / num;
        }
        #endregion
        
        #region Floor To SByte
        public static sbyte FloorToSByte<T>(this T value) where T : IFloatingPoint<T> => sbyte.CreateSaturating(T.Floor(value));
        #endregion
        
        #region Floor To Byte
        public static byte FloorToByte<T>(this T value) where T : IFloatingPoint<T> => byte.CreateSaturating(T.Floor(value));
        #endregion
        
        #region Floor To Short
        public static short FloorToShort<T>(this T value) where T : IFloatingPoint<T> => short.CreateSaturating(T.Floor(value));
        #endregion
        
        #region Floor To Ushort
        public static ushort FloorToUShort<T>(this T value) where T : IFloatingPoint<T> => ushort.CreateSaturating(T.Floor(value));
        #endregion
        
        #region Floor To Int
        public static int FloorToInt<T>(this T value) where T : IFloatingPoint<T> => int.CreateSaturating(T.Floor(value));
        public static BigInteger FloorToInt(this BigDecimal value) => (BigInteger)BigDecimal.Floor(value);
        #endregion
        
        #region Floor To UInt
        public static uint FloorToUInt<T>(this T value) where T : IFloatingPoint<T> => uint.CreateSaturating(T.Floor(value));
        #endregion
        
        #region Floor To Long
        public static long FloorToLong<T>(this T value) where T : IFloatingPoint<T> => long.CreateSaturating(T.Floor(value));
        #endregion
        
        #region Floor To ULong
        public static ulong FloorToULong<T>(this T value) where T : IFloatingPoint<T> => ulong.CreateSaturating(T.Floor(value));
        #endregion

        #region Gamma
        public static T Gamma<T>(this T value, T absMax, T gamma) where T : INumber<T>, IPowerFunctions<T>
        {
            bool flag = value < T.Zero;
            T temp = value.Abs();

            if (temp > absMax)
                return flag ? -temp : temp;

            T result = (temp / absMax.Pow(gamma)) * absMax;
            return flag ? -result : result;
        }

        #endregion

        #region GCD
        /// <summary>
        /// 두 정수의 최대 공약수(GCD)를 계산합니다.
        /// </summary>
        /// <param name="a">첫 번째 정수입니다.</param>
        /// <param name="b">두 번째 정수입니다.</param>
        /// <returns>두 정수의 최대 공약수입니다.</returns>
        /// <exception cref="ArgumentException">두 입력이 모두 0인 경우 발생합니다. GCD는 0,0에 대해 정의되지 않습니다.</exception>
        public static ulong GCD(this ulong a, ulong b)
        {
            if (a == 0 && b == 0)
                throw new ArgumentException("GCD is not defined for both arguments being zero.");
    
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return a | b;
        }
        
        /// <summary>
        /// 주어진 정수 배열의 모든 요소에 대한 최대 공약수(GCD)를 계산합니다.
        /// </summary>
        /// <param name="numbers">GCD를 계산할 정수 배열입니다. <br/> 배열에는 2개 이상의 정수가 포함되어야 합니다.</param>
        /// <returns>배열의 모든 요소에 대한 최대 공약수입니다.</returns>
        /// <exception cref="ArgumentException">배열이 <see langword="null"/>이거나, 2개 미만의 요소를 포함하거나, 모든 요소가 0인 경우 발생합니다.</exception>
        public static ulong GCD(params ulong[] numbers)
        {
            if (numbers == null || numbers.Length < 2)
                throw new ArgumentException("Input array must not be null and contain at least two numbers.");
    
            if (numbers.All(static n => n == 0))
                throw new ArgumentException("GCD is not defined for all arguments being zero.");

            ulong result = numbers[0];
            for (int i = 1; i < numbers.Length; i++)
                result = GCD(result, numbers[i]);

            return result;
        }
        #endregion

        #region Get Number Of Decimals For Minimum Difference
        public static int GetNumberOfDecimalsForMinimumDifference(this float minDifference) => (-minDifference.Abs().Log10().FloorToInt()).Clamp(0);
        public static int GetNumberOfDecimalsForMinimumDifference(this double minDifference) => (-minDifference.Abs().Log10().FloorToInt()).Clamp(0);
        public static int GetNumberOfDecimalsForMinimumDifference(this decimal minDifference) => (-minDifference.Abs().Log10().FloorToInt()).Clamp(0);
        #endregion

        public static float GammaToLinearSpace(this float value) => Mathf.GammaToLinearSpace(value);

        #region IEEE Remainder
        public static T IEEERemainder<T>(this T x, T y) where T : IFloatingPointIeee754<T> => T.Ieee754Remainder(x, y);
        #endregion

        #region Inverse Lerp
        public static T InverseLerp<T>(this T a, T b, T t) where T : INumber<T>
        {
            if (a != b)
                return ((t - a) / (b - a)).Clamp01();

            return T.Zero;
        }

        public static float InverseLerp(this float a, float b, float t)
        {
            if (!Approximately(a, b))
                return ((t - a) / (b - a)).Clamp01();

            return 0;
        }

        public static double InverseLerp(this double a, double b, double t)
        {
            if (!Approximately(a, b))
                return ((t - a) / (b - a)).Clamp01();

            return 0;
        }

        public static BigDecimal InverseLerp(this BigDecimal a, BigDecimal b, BigDecimal t)
        {
            if (a != b)
                return ((t - a) / (b - a)).Clamp01();

            return 0;
        }
        #endregion

        public static bool IsPowerOfTwo(this int value) => int.IsPow2(value);

        #region Lerp
        public static T Lerp<T>(this T current, T target, T t) where T : IBinaryInteger<T> => t > T.Zero ? target : current;

        public static T Lerp<T, TFactor>(this T current, T target, TFactor t) where T : IBinaryInteger<T> where TFactor : IFloatingPoint<TFactor>
        {
            t = t.Clamp01();
            TFactor start = TFactor.CreateTruncating(current);
            return T.CreateTruncating(start + ((TFactor.CreateTruncating(target) - start) * t));
        }

        public static T Lerp<T>(this T current, T target, BigDecimal t) where T : IBinaryInteger<T>
        {
            t = t.Clamp01();
            BigDecimal start = BigInteger.CreateTruncating(current);
            return T.CreateTruncating((BigInteger)(start + ((BigInteger.CreateTruncating(target) - start) * t)));
        }

        public static float Lerp(this float current, float target, float t)
        {
            t = t.Clamp01();
            return current + ((target - current) * t);
        }

        public static double Lerp(this double current, double target, double t)
        {
            t = t.Clamp01();
            return current + ((target - current) * t);
        }

        public static decimal Lerp(this decimal current, decimal target, decimal t)
        {
            t = t.Clamp01();
            return current + ((target - current) * t);
        }

        public static BigDecimal Lerp(this BigDecimal current, BigDecimal target, BigDecimal t)
        {
            t = t.Clamp01();
            return current + ((target - current) * t);
        }

        public static Vector2 Lerp(this Vector2 current, Vector2 target, float t)
        {
            t = t.Clamp01();
            return new Vector2(current.x + ((target.x - current.x) * t), current.y + ((target.y - current.y) * t));
        }

        public static Vector3 Lerp(this Vector3 current, Vector3 target, float t)
        {
            t = t.Clamp01();
            return new Vector3(current.x + ((target.x - current.x) * t), current.y + ((target.y - current.y) * t), current.z + ((target.z - current.z) * t));
        }

        public static Vector4 Lerp(this Vector4 current, Vector4 target, float t)
        {
            t = t.Clamp01();
            return new Vector4(current.x + ((target.x - current.x) * t), current.y + ((target.y - current.y) * t), current.z + ((target.z - current.z) * t), current.w + ((target.w - current.w) * t));
        }

        public static Quaternion Lerp(this Quaternion current, Quaternion target, float t)
        {
            t = t.Clamp01();
            return Quaternion.Lerp(current, target, t);
        }

        public static Rect Lerp(this Rect current, Rect target, float t)
        {
            t = t.Clamp01();
            return new Rect(current.x + ((target.x - current.x) * t), current.y + ((target.y - current.y) * t), current.width + ((target.width - current.width) * t), current.height + ((target.height - current.height) * t));
        }
        public static RectVertices Lerp(this RectVertices current, RectVertices target, float t)
        {
            t = t.Clamp01();
            return new RectVertices(current.bottomLeft + ((target.bottomLeft - current.bottomLeft) * t), current.topLeft + ((target.topRight - current.topLeft) * t), current.topRight + ((target.topRight - current.topRight) * t), current.bottomRight + ((target.bottomRight - current.bottomRight) * t));
        }

        public static Color Lerp(this Color current, Color target, float t)
        {
            t = t.Clamp01();
            return new Color(current.r + ((target.r - current.r) * t), current.g + ((target.g - current.g) * t), current.b + ((target.b - current.b) * t), current.a + ((target.a - current.a) * t));
        }

        public static Color LerpNoAlpha(this Color current, Color target, float t)
        {
            t = t.Clamp01();
            return new Color(current.r + ((target.r - current.r) * t), current.g + ((target.g - current.g) * t), current.b + ((target.b - current.b) * t), current.a);
        }
        
        public static CornerRadius Lerp(this CornerRadius current, CornerRadius target, float t)
        {
            t = t.Clamp01();
            return new CornerRadius(current.bottomLeft + ((target.bottomLeft - current.bottomLeft) * t), current.topLeft + ((target.topRight - current.topLeft) * t), current.topRight + ((target.topRight - current.topRight) * t), current.bottomRight + ((target.bottomRight - current.bottomRight) * t));
        }
        #endregion

        #region Lerp Angle
        public static float LerpAngle(this float current, float target, float t)
        {
            float num = (target - current).Repeat(360);
            if (num > 180)
                num -= 360;

            return current + (num * t.Clamp01());
        }

        public static double LerpAngle(this double current, double target, double t)
        {
            double num = (target - current).Repeat(360);
            if (num > 180)
                num -= 360;

            return current + (num * t.Clamp01());
        }

        public static decimal LerpAngle(this decimal current, decimal target, decimal t)
        {
            decimal num = (target - current).Repeat(360);
            if (num > 180)
                num -= 360;

            return current + (num * t.Clamp01());
        }

        public static BigDecimal LerpAngle(this BigDecimal current, BigDecimal target, BigDecimal t)
        {
            BigDecimal num = (target - current).Repeat(360);
            if (num > 180)
                num -= 360;

            return current + (num * t.Clamp01());
        }
        #endregion

        #region Lerp Unclamped
        public static T LerpUnclamped<T>(this T current, T target, T t) where T : IBinaryInteger<T> => current + ((target - current) * t);

        public static T LerpUnclamped<T, TFactor>(this T current, T target, TFactor t) where T : IBinaryInteger<T> where TFactor : IFloatingPoint<TFactor>
        {
            TFactor start = TFactor.CreateTruncating(current);
            return T.CreateTruncating(start + ((TFactor.CreateTruncating(target) - start) * t));
        }

        public static T LerpUnclamped<T>(this T current, T target, BigDecimal t) where T : IBinaryInteger<T>
        {
            BigDecimal start = BigInteger.CreateTruncating(current);
            return T.CreateTruncating((BigInteger)(start + ((BigInteger.CreateTruncating(target) - start) * t)));
        }

        public static float LerpUnclamped(this float current, float target, float t) => current + ((target - current) * t);
        public static double LerpUnclamped(this double current, double target, double t) => current + ((target - current) * t);
        public static decimal LerpUnclamped(this decimal current, decimal target, decimal t) => current + ((target - current) * t);

        public static BigDecimal LerpUnclamped(this BigDecimal current, BigDecimal target, BigDecimal t) => current + ((target - current) * t);

        public static Vector2 LerpUnclamped(this Vector2 current, Vector2 target, float t) => new Vector2(current.x + ((target.x - current.x) * t), current.y + ((target.y - current.y) * t));
        public static Vector3 LerpUnclamped(this Vector3 current, Vector3 target, float t) => new Vector3(current.x + ((target.x - current.x) * t), current.y + ((target.y - current.y) * t), current.z + ((target.z - current.z) * t));
        public static Vector4 LerpUnclamped(this Vector4 current, Vector4 target, float t) => new Vector4(current.x + ((target.x - current.x) * t), current.y + ((target.y - current.y) * t), current.z + ((target.z - current.z) * t), current.w + ((target.w - current.w) * t));

        public static Quaternion LerpUnclamped(this Quaternion current, Quaternion target, float t) => Quaternion.Lerp(current, target, t);

        public static Rect LerpUnclamped(this Rect current, Rect target, float t) => new Rect(current.x + ((target.x - current.x) * t), current.y + ((target.y - current.y) * t), current.width + ((target.width - current.width) * t), current.height + ((target.height - current.height) * t));
        public static RectVertices LerpUnclamped(this RectVertices current, RectVertices target, float t) => new RectVertices(current.bottomLeft + ((target.bottomLeft - current.bottomLeft) * t), current.topLeft + ((target.topRight - current.topLeft) * t), current.topRight + ((target.topRight - current.topRight) * t), current.bottomRight + ((target.bottomRight - current.bottomRight) * t));

        public static Color LerpUnclamped(this Color current, Color target, float t) => new Color(current.r + ((target.r - current.r) * t), current.g + ((target.g - current.g) * t), current.b + ((target.b - current.b) * t), current.a + ((target.a - current.a) * t));
        public static Color LerpNoAlphaUnclamped(this Color current, Color target, float t) => new Color(current.r + ((target.r - current.r) * t), current.g + ((target.g - current.g) * t), current.b + ((target.b - current.b) * t), current.a);
        
        public static CornerRadius LerpUnclamped(this CornerRadius current, CornerRadius target, float t) => new CornerRadius(current.bottomLeft + ((target.bottomLeft - current.bottomLeft) * t), current.topLeft + ((target.topRight - current.topLeft) * t), current.topRight + ((target.topRight - current.topRight) * t), current.bottomRight + ((target.bottomRight - current.bottomRight) * t));
        #endregion

        public static float LinearToGammaSpace(this float value) => Mathf.LinearToGammaSpace(value);

        #region Log
        public static T Log<T>(this T value) where T : ILogarithmicFunctions<T> => T.Log(value);
        public static decimal Log(this decimal value) => DecimalMath.Log(value);
        #endregion

        #region Log 10
        public static T Log10<T>(this T value) where T : ILogarithmicFunctions<T> => T.Log10(value);
        public static decimal Log10(this decimal value) => DecimalMath.Log10(value);
        #endregion

        #region Max
        public static T Max<T>(T a, T b) where T : INumber<T> => T.Max(a, b);

        public static BigDecimal Max(BigDecimal a, BigDecimal b)
        {
            if (a > b)
                return a;
            else
                return b;
        }

        #endregion

        #region Max Array
        public static T Max<T>(T value, params T[] values) where T : INumber<T>
        {
            ArgumentNullException.ThrowIfNull(values);

            T result = value;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] > result)
                    result = values[i];
            }

            return result;
        }

        public static BigDecimal Max(BigDecimal value, params BigDecimal[] values)
        {
            if (values == null)
                throw new ArgumentNullException();

            int length = values.Length;
            if (length == 0)
                return 0;

            BigDecimal num2 = value;
            for (int i = 0; i < length; i++)
            {
                if (values[i] > num2)
                    num2 = values[i];
            }

            return num2;
        }

        #endregion

        #region Min
        public static T Min<T>(T a, T b) where T : INumber<T> => T.Min(a, b);

        public static BigDecimal Min(BigDecimal a, BigDecimal b)
        {
            if (a < b)
                return a;
            else
                return b;
        }

        #endregion

        #region Min Array
        public static T Min<T>(T value, params T[] values) where T : INumber<T>
        {
            ArgumentNullException.ThrowIfNull(values);

            T result = value;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] < result)
                    result = values[i];
            }

            return result;
        }

        public static BigDecimal Min(BigDecimal value, params BigDecimal[] values)
        {
            if (values == null)
                throw new ArgumentNullException();

            int length = values.Length;
            if (length == 0)
                return 0;

            BigDecimal num2 = value;
            for (int i = 0; i < length; i++)
            {
                if (values[i] < num2)
                    num2 = values[i];
            }

            return num2;
        }

        #endregion

        #region Move Towards
        public static T MoveTowards<T>(this T current, T target, T maxDelta) where T : INumber<T>
        {
            if (current.Distance(target) <= maxDelta)
                return target;

            return target > current ? current + maxDelta : current - maxDelta;
        }

        public static BigDecimal MoveTowards(this BigDecimal current, BigDecimal target, BigDecimal maxDelta)
        {
            if ((target - current).Abs() <= maxDelta)
                return target;

            return current + ((target - current).Sign() * maxDelta);
        }

        public static Vector2 MoveTowards(this Vector2 current, Vector2 target, float maxDistanceDelta)
        {
            float num = target.x - current.x;
            float num2 = target.y - current.y;
            float num3 = (num * num) + (num2 * num2);
            if (num3 == 0f || (maxDistanceDelta >= 0f && num3 <= maxDistanceDelta * maxDistanceDelta))
                return target;

            float num4 = (float)Math.Sqrt(num3);
            return new Vector2(current.x + ((num / num4) * maxDistanceDelta), current.y + ((num2 / num4) * maxDistanceDelta));
        }
        public static Vector3 MoveTowards(this Vector3 current, Vector3 target, float maxDistanceDelta)
        {
            float num = target.x - current.x;
            float num2 = target.y - current.y;
            float num3 = target.z - current.z;
            float num4 = (num * num) + (num2 * num2) + (num3 * num3);
            if (num4 == 0f || (maxDistanceDelta >= 0f && num4 <= maxDistanceDelta * maxDistanceDelta))
                return target;

            float num5 = (float)Math.Sqrt(num4);
            return new Vector3(current.x + ((num / num5) * maxDistanceDelta), current.y + ((num2 / num5) * maxDistanceDelta), current.z + ((num3 / num5) * maxDistanceDelta));
        }
        public static Vector4 MoveTowards(this Vector4 current, Vector4 target, float maxDistanceDelta)
        {
            float num = target.x - current.x;
            float num2 = target.y - current.y;
            float num3 = target.z - current.z;
            float num4 = target.w - current.w;
            float num5 = (num * num) + (num2 * num2) + (num3 * num3) + (num4 * num4);
            if (num5 == 0f || (maxDistanceDelta >= 0f && num5 <= maxDistanceDelta * maxDistanceDelta))
                return target;

            float num6 = (float)Math.Sqrt(num5);
            return new Vector4(current.x + ((num / num6) * maxDistanceDelta), current.y + ((num2 / num6) * maxDistanceDelta), current.z + ((num3 / num6) * maxDistanceDelta), current.w + ((num4 / num6) * maxDistanceDelta));
        }
        public static Rect MoveTowards(this Rect current, Rect target, float maxDistanceDelta)
        {
            float num = target.x - current.x;
            float num2 = target.y - current.y;
            float num3 = target.width - current.width;
            float num4 = target.height - current.height;
            float num5 = (num * num) + (num2 * num2) + (num3 * num3) + (num4 * num4);
            if (num5 == 0f || (maxDistanceDelta >= 0f && num5 <= maxDistanceDelta * maxDistanceDelta))
                return target;

            float num6 = (float)Math.Sqrt(num5);
            return new Rect(current.x + ((num / num6) * maxDistanceDelta), current.y + ((num2 / num6) * maxDistanceDelta), current.width + ((num3 / num6) * maxDistanceDelta), current.height + ((num4 / num6) * maxDistanceDelta));
        }
        public static Color MoveTowards(this Color current, Color target, float maxDistanceDelta)
        {
            float num = target.r - current.r;
            float num2 = target.g - current.g;
            float num3 = target.b - current.b;
            float num4 = target.a - current.a;
            float num5 = (num * num) + (num2 * num2) + (num3 * num3) + (num4 * num4);
            if (num5 == 0f || (maxDistanceDelta >= 0f && num5 <= maxDistanceDelta * maxDistanceDelta))
                return target;

            float num6 = (float)Math.Sqrt(num5);
            return new Color(current.r + ((num / num6) * maxDistanceDelta), current.g + ((num2 / num6) * maxDistanceDelta), current.b + ((num3 / num6) * maxDistanceDelta), current.a + ((num4 / num6) * maxDistanceDelta));
        }
        #endregion

        #region Move Towards Angle
        public static T MoveTowardsAngle<T>(this T current, T target, T maxDelta) where T : INumber<T>
        {
            T temp = current.DeltaAngle(target);
            if (-maxDelta < temp && temp < maxDelta)
                return target;

            target = current + temp;
            return MoveTowards(current, target, maxDelta);
        }

        public static BigDecimal MoveTowardsAngle(this BigDecimal current, BigDecimal target, BigDecimal maxDelta)
        {
            BigDecimal temp = current.DeltaAngle(target);
            if (-maxDelta < temp && temp < maxDelta)
                return target;

            target = current + temp;
            return MoveTowards(current, target, maxDelta);
        }

        #endregion

        #region Next Power Of
        public static T NextPowerOf<T>(this T value, int power) where T : INumber<T>
        {
            bool negative = false;
            if (value == T.Zero)
                return T.Zero;
            else if (value < T.Zero)
            {
                value = -value;
                negative = true;
            }

            T powerValue = T.CreateChecked(power.Clamp(1));

            T tempValue = T.One;
            T result;
            if (value < T.One)
            {
                while (tempValue > value)
                    tempValue /= powerValue;

                result = tempValue * powerValue;
            }
            else
            {
                while (tempValue < value)
                    tempValue *= powerValue;

                result = tempValue;
            }

            return negative ? -result : result;
        }

        public static BigDecimal NextPowerOf(this BigDecimal value, int power)
        {
            bool negative = false;
            if (value == 0)
                return 0;
            else if (value < 0)
            {
                value = -value;
                negative = true;
            }

            power = power.Clamp(1);

            BigDecimal tempValue = 1;
            if (value < 1)
            {
                while (tempValue > value)
                    tempValue /= power;

                var result = tempValue * power;
                if (negative)
                    return -result;
                else
                    return result;
            }
            else
            {
                while (tempValue < value)
                    tempValue *= power;

                var result = tempValue;
                if (negative)
                    return -result;
                else
                    return result;
            }
        }

        #endregion

        public static int NextPowerOfTwo(this int value) => (int)BitOperations.RoundUpToPowerOf2((uint)value);

        public static float PerlinNoise(this float x, float y) => Mathf.PerlinNoise(x, y);

        #region Pre Power Of
        public static T PrePowerOf<T>(this T value, int power) where T : INumber<T>
        {
            bool negative = false;
            if (value == T.Zero)
                return T.Zero;
            else if (value < T.Zero)
            {
                value = -value;
                negative = true;
            }

            T powerValue = T.CreateChecked(power.Clamp(1));

            T tempValue = T.One;
            T result;
            if (value < T.One)
            {
                while (tempValue > value)
                    tempValue /= powerValue;

                result = tempValue;
            }
            else
            {
                while (tempValue < value)
                    tempValue *= powerValue;

                result = tempValue / powerValue;
            }

            return negative ? -result : result;
        }

        public static BigDecimal PrePowerOf(this BigDecimal value, int power)
        {
            bool negative = false;
            if (value == 0)
                return 0;
            else if (value < 0)
            {
                value = -value;
                negative = true;
            }

            power = power.Clamp(1);

            BigDecimal tempValue = 1;
            if (value < 1)
            {
                while (tempValue > value)
                    tempValue /= power;

                var result = tempValue;
                if (negative)
                    return -result;
                else
                    return result;
            }
            else
            {
                while (tempValue < value)
                    tempValue *= power;

                var result = tempValue / power;
                if (negative)
                    return -result;
                else
                    return result;
            }
        }

        #endregion

        #region Ping Pong
        public static T PingPong<T>(this T value, T length) where T : INumber<T>
        {
            T doubleLength = length + length;
            T remainder = value.Repeat(doubleLength);
            return remainder <= length ? remainder : doubleLength - remainder;
        }

        public static BigDecimal PingPong(this BigDecimal value, BigDecimal length) => length - (value.Repeat(length * 2) - length).Abs();
        #endregion

        #region Pow
        public static T Pow<T>(this T x, T y) where T : IPowerFunctions<T> => T.Pow(x, y);
        public static long Pow(this int baseValue, int exponent) => ((long)baseValue).Pow(exponent);
        public static ulong Pow(this uint baseValue, uint exponent) => ((ulong)baseValue).Pow(exponent);
        public static long Pow(this long baseValue, long exponent)
        {
            if (exponent < 0)
            {
                // 음수 지수는 일반적으로 분수나 실수를 반환하므로,
                // 정수 거듭제곱에서는 예외를 발생시키거나 다른 처리가 필요합니다.
                // 여기서는 예외를 발생시킵니다.
                throw new ArgumentException("The exponent cannot be negative.", nameof(exponent));
            }

            long result = 1;
            while (exponent > 0)
            {
                // 지수가 홀수이면 결과에 현재 밑 값을 곱합니다.
                if (exponent % 2 == 1)
                    result *= baseValue;

                // 밑 값을 제곱합니다.
                baseValue *= baseValue;

                // 지수를 2로 나눕니다.
                exponent /= 2;
            }

            return result;
        }
        
        public static ulong Pow(this ulong baseValue, ulong exponent)
        {
            ulong result = 1;
            while (exponent > 0)
            {
                // 지수가 홀수이면 결과에 현재 밑 값을 곱합니다.
                if (exponent % 2 == 1)
                    result *= baseValue;

                // 밑 값을 제곱합니다.
                baseValue *= baseValue;

                // 지수를 2로 나눕니다.
                exponent /= 2;
            }

            return result;
        }
        
        public static decimal Pow(this decimal x, int y) => DecimalMath.PowerN(x, y);
        public static decimal Pow(this decimal x, decimal y) => DecimalMath.Power(x, y);
        public static BigInteger Pow(this BigInteger x, int y) => BigInteger.Pow(x, y);
        public static BigDecimal Pow(this BigDecimal x, BigInteger y) => BigDecimal.Pow(x, y);
        #endregion

        #region Repeat
        public static T Repeat<T>(this T t, T length) where T : INumber<T>
        {
            T remainder = t % length;
            return remainder < T.Zero ? remainder + length : remainder;
        }
        public static T Repeat<T>(this T t, T start, T end) where T : INumber<T> => (t - start).Repeat(start.Distance(end)) + start;

        public static BigDecimal Repeat(this BigDecimal t, BigDecimal length) => (t - ((t / length).Floor() * length)).Clamp(0, length);
        public static BigDecimal Repeat(this BigDecimal t, BigDecimal start, BigDecimal end) => (t - start).Repeat(start.Distance(end)) + start;
        #endregion

        #region Repeat While
        public static T RepeatWhile<T>(this T t, T start, T length) where T : INumber<T>
        {
            if (t >= start)
            {
                while (t > length)
                    t -= length;
            }
            else
            {
                while (t < start)
                    t += length;
            }

            return t;
        }

        public static T RepeatWhile<T>(this T t, T length) where T : INumber<T>
        {
            if (t >= T.Zero)
            {
                while (t > length)
                    t -= length;
            }
            else
            {
                while (t < T.Zero)
                    t += length;
            }

            return t;
        }

        public static BigDecimal RepeatWhile(this BigDecimal t, BigDecimal start, BigDecimal length)
        {
            if (t >= start)
            {
                while (t > length)
                    t -= length;
            }
            else
            {
                while (t < start)
                    t += length;
            }

            return t;
        }

        public static BigDecimal RepeatWhile(this BigDecimal t, BigDecimal length)
        {
            if (t >= 0)
            {
                while (t > length)
                    t -= length;
            }
            else
            {
                while (t < 0)
                    t += length;
            }

            return t;
        }

        #endregion

        #region Repeat While Other
        public static T RepeatWhileOther<T>(this T t, T start, T length) where T : INumber<T>
        {
            if (t >= start)
            {
                while (t >= length)
                    t -= length;
            }
            else
            {
                while (t < start)
                    t += length;
            }

            return t;
        }

        public static T RepeatWhileOther<T>(this T t, T length) where T : INumber<T>
        {
            if (t >= T.Zero)
            {
                while (t >= length)
                    t -= length;
            }
            else
            {
                while (t < T.Zero)
                    t += length;
            }

            return t;
        }

        public static BigDecimal RepeatWhileOther(this BigDecimal t, BigDecimal start, BigDecimal length)
        {
            if (t >= start)
            {
                while (t >= length)
                    t -= length;
            }
            else
            {
                while (t < start)
                    t += length;
            }

            return t;
        }

        public static BigDecimal RepeatWhileOther(this BigDecimal t, BigDecimal length)
        {
            if (t >= 0)
            {
                while (t >= length)
                    t -= length;
            }
            else
            {
                while (t < 0)
                    t += length;
            }

            return t;
        }

        #endregion

        #region Round
        public static T Round<T>(this T value) where T : IFloatingPoint<T> => T.Round(value);
        public static BigDecimal Round(this BigDecimal value) => BigDecimal.Round(value);

        public static T Round<T>(this T value, MidpointRounding midpointRounding) where T : IFloatingPoint<T> => T.Round(value, midpointRounding);
        public static BigDecimal Round(this BigDecimal value, MidpointRounding midpointRounding) => midpointRounding switch
        {
            MidpointRounding.ToEven => BigDecimal.Round(value, RoundingStrategy.ToEven),
            MidpointRounding.AwayFromZero => BigDecimal.Round(value, RoundingStrategy.AwayFromZero),
            /*MidpointRounding.ToZero => BigDecimal.Truncate(value),
            MidpointRounding.ToNegativeInfinity => BigDecimal.Floor(value),
            MidpointRounding.ToPositiveInfinity => BigDecimal.Ceiling(value),*/
            _ => throw new ArgumentOutOfRangeException(nameof(midpointRounding), midpointRounding, null)
        };

        public static T Round<T>(this T value, int digits) where T : IFloatingPoint<T> => T.Round(value, digits);
        public static BigDecimal Round(this BigDecimal value, int digits)
        {
            if (digits < 0 || digits > maxRoundingDigits)
                throw new ArgumentOutOfRangeException(nameof(digits));

            BigDecimal num = roundPower10BigDecimal[digits];
            return BigDecimal.Round(value * num) / num;
        }

        public static T Round<T>(this T value, int digits, MidpointRounding midpointRounding) where T : IFloatingPoint<T> => T.Round(value, digits, midpointRounding);
        #endregion
        
        #region Round To SByte
        public static sbyte RoundToSByte<T>(this T value) where T : IFloatingPoint<T> => sbyte.CreateSaturating(T.Round(value));
        #endregion
        
        #region Round To Byte
        public static byte RoundToByte<T>(this T value) where T : IFloatingPoint<T> => byte.CreateSaturating(T.Round(value));
        #endregion
        
        #region Round To Short
        public static short RoundToShort<T>(this T value) where T : IFloatingPoint<T> => short.CreateSaturating(T.Round(value));
        #endregion
        
        #region Round To Ushort
        public static ushort RoundToUShort<T>(this T value) where T : IFloatingPoint<T> => ushort.CreateSaturating(T.Round(value));
        #endregion
        
        #region Round To Int
        public static int RoundToInt<T>(this T value) where T : IFloatingPoint<T> => int.CreateSaturating(T.Round(value));
        public static BigInteger RoundToInt(this BigDecimal value) => BigDecimal.Round(value);
        #endregion
        
        #region Round To UInt
        public static uint RoundToUInt<T>(this T value) where T : IFloatingPoint<T> => uint.CreateSaturating(T.Round(value));
        #endregion
        
        #region Round To Long
        public static long RoundToLong<T>(this T value) where T : IFloatingPoint<T> => long.CreateSaturating(T.Round(value));
        #endregion
        
        #region Round To ULong
        public static ulong RoundToULong<T>(this T value) where T : IFloatingPoint<T> => ulong.CreateSaturating(T.Round(value));
        #endregion

        #region  Round Based On Minimum Difference
        public static float RoundBasedOnMinimumDifference(this float valueToRound, float minDifference) => minDifference == 0.0f ? valueToRound.DiscardLeastSignificantDecimal() : valueToRound.Round(minDifference.GetNumberOfDecimalsForMinimumDifference(), MidpointRounding.AwayFromZero);
        public static double RoundBasedOnMinimumDifference(this double valueToRound, double minDifference) => minDifference == 0.0 ? valueToRound.DiscardLeastSignificantDecimal() : valueToRound.Round(minDifference.GetNumberOfDecimalsForMinimumDifference(), MidpointRounding.AwayFromZero);
        public static decimal RoundBasedOnMinimumDifference(this decimal valueToRound, decimal minDifference) => minDifference == 0.0m ? valueToRound.DiscardLeastSignificantDecimal() : valueToRound.Round(minDifference.GetNumberOfDecimalsForMinimumDifference(), MidpointRounding.AwayFromZero);
        #endregion 

        #region Sign
        public static int Sign<T>(this T value) where T : INumber<T> => value < T.Zero ? -1 : 1;

        public static int Sign(this BigDecimal value)
        {
            if (value < BigDecimal.Zero)
                return -1;
            else
                return 1;
        }

        #endregion
        
        #region Sign With Zero
        public static int SignWithZero<T>(this T value) where T : INumber<T> => T.Sign(value);

        public static int SignWithZero(this BigDecimal value)
        {
            if (value < BigDecimal.Zero)
                return -1;
            else if (value > 0)
                return 1;
                
            return 0;
        }

        #endregion

        #region Sin
        public static T Sin<T>(this T value) where T : ITrigonometricFunctions<T> => T.Sin(value);
        public static decimal Sin(this decimal value) => DecimalMath.Sin(value);
        #endregion

        #region Sinh
        public static T Sinh<T>(this T value) where T : IHyperbolicFunctions<T> => T.Sinh(value);
        public static decimal Sinh(this decimal value) => DecimalMath.Sinh(value);
        #endregion

        #region Smooth Damp
        public static float SmoothDamp(this float current, float target, ref float currentVelocity, float smoothTime) => Mathf.SmoothDamp(current, target, ref currentVelocity, smoothTime, float.PositiveInfinity, Time.deltaTime);
        public static float SmoothDamp(this float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed) => Mathf.SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, Time.deltaTime);
        public static float SmoothDamp(this float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime) => Mathf.SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        #endregion

        #region Smooth Damp Angle
        public static float SmoothDampAngle(this float current, float target, ref float currentVelocity, float smoothTime) => Mathf.SmoothDampAngle(current, target, ref currentVelocity, smoothTime, float.PositiveInfinity, Time.deltaTime);
        public static float SmoothDampAngle(this float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed) => Mathf.SmoothDampAngle(current, target, ref currentVelocity, smoothTime, maxSpeed, Time.deltaTime);
        public static float SmoothDampAngle(this float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime) => Mathf.SmoothDampAngle(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        #endregion

        public static float SmoothStep(this float from, float to, float t) => Mathf.SmoothStep(from, to, t);

        #region Sqrt
        public static T Sqrt<T>(this T value) where T : IRootFunctions<T> => T.Sqrt(value);
        public static decimal Sqrt(this decimal value) => DecimalMath.Sqrt(value);
        #endregion

        #region Tan
        public static T Tan<T>(this T value) where T : ITrigonometricFunctions<T> => T.Tan(value);
        public static decimal Tan(this decimal value) => DecimalMath.Tan(value);
        #endregion

        #region Tanh
        public static T Tanh<T>(this T value) where T : IHyperbolicFunctions<T> => T.Tanh(value);
        public static decimal Tanh(this decimal value) => DecimalMath.Tanh(value);
        #endregion
        
        #region Clamp To SByte
        public static sbyte ClampToSByte<T>(this T value) where T : INumberBase<T> => sbyte.CreateSaturating(value);
        #endregion
        
        #region Clamp To Byte
        public static byte ClampToByte<T>(this T value) where T : INumberBase<T> => byte.CreateSaturating(value);
        #endregion
        
        #region Clamp To Short
        public static short ClampToShort<T>(this T value) where T : INumberBase<T> => short.CreateSaturating(value);
        #endregion
        
        #region Clamp To UShort
        public static ushort ClampToUShort<T>(this T value) where T : INumberBase<T> => ushort.CreateSaturating(value);
        #endregion

        #region Clamp To Int
        public static int ClampToInt<T>(this T value) where T : INumberBase<T> => int.CreateSaturating(value);
        #endregion
        
        #region Clamp To UInt
        public static uint ClampToUInt<T>(this T value) where T : INumberBase<T> => uint.CreateSaturating(value);
        #endregion
        
        #region Clamp To Long
        public static long ClampToLong<T>(this T value) where T : INumberBase<T> => long.CreateSaturating(value);
        #endregion
        
        #region Clamp To ULong
        public static ulong ClampToULong<T>(this T value) where T : INumberBase<T> => ulong.CreateSaturating(value);
        #endregion
        
        #region Clamp To Float
        public static float ClampToFloat<T>(this T value) where T : INumberBase<T> => float.CreateSaturating(value);
        public static float ClampToFloat(this double value) => (float)value.Clamp(float.MinValue, float.MaxValue);
        #endregion
        
        #region Clamp To Double
        public static double ClampToDouble<T>(this T value) where T : INumberBase<T> => double.CreateSaturating(value);
        #endregion
        
        #region Clamp To Decimal
        public static decimal ClampToDecimal<T>(this T value) where T : INumberBase<T> => decimal.CreateSaturating(value);
        #endregion
        
        #region Clamp To NInt
        public static nint ClampToNInt<T>(this T value) where T : INumberBase<T> => nint.CreateSaturating(value);
        #endregion
        
        #region Clamp To NUInt
        public static nuint ClampToNUInt<T>(this T value) where T : INumberBase<T> => nuint.CreateSaturating(value);
        #endregion

        #region Truncate
        public static T Truncate<T>(this T value) where T : IFloatingPoint<T> => T.Truncate(value);
        public static BigDecimal Truncate(this BigDecimal value) => BigDecimal.Truncate(value);
        #endregion

        #region Plus
        public static Color32 Plus(this Color32 a, Color32 b) => new Color32((byte)(a.r + b.r), (byte)(a.g + b.g), (byte)(a.b + b.b), (byte)(a.a + b.a));
        #endregion

        #region Minus
        public static Color32 Minus(this Color32 a, Color32 b) => new Color32((byte)(a.r - b.r), (byte)(a.g - b.g), (byte)(a.b - b.b), (byte)(a.a - b.a));
        #endregion

        #region Multiply
        public static Vector3 Multiply(this Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        public static Vector4 Multiply(this Vector4 a, Vector4 b) => new Vector4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);

        public static Color32 Multiply(this Color32 a, Color32 b) => new Color32((byte)(a.r * b.r), (byte)(a.g * b.g), (byte)(a.b * b.b), (byte)(a.a * b.a));
        #endregion

        #region Discard Least Significant Decimal
        public static float DiscardLeastSignificantDecimal(this float value) => ((double)value).DiscardLeastSignificantDecimal().ClampToFloat();

        public static double DiscardLeastSignificantDecimal(this double value)
        {
            int digits = (int)(5 - value.Abs().Log10()).Clamp(0);
            try
            {
                return value.Round(digits);
            }
            catch (ArgumentOutOfRangeException)
            {
                return 0;
            }
        }
        
        public static decimal DiscardLeastSignificantDecimal(this decimal value)
        {
            int digits = (int)(5 - value.Abs().Log10()).Clamp(0);
            try
            {
                return value.Round(digits);
            }
            catch (ArgumentOutOfRangeException)
            {
                return 0;
            }
        }
        #endregion

        #region Divide
        public static Vector2 Divide(this Vector2 a, Vector2 b) => new Vector3(a.x / b.x, a.y / b.y);
        public static Vector3 Divide(this Vector3 a, Vector3 b) => new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        public static Vector4 Divide(this Vector4 a, Vector4 b) => new Vector4(a.x / b.x, a.y / b.y, a.z / b.z, a.w / b.w);

        public static Color Divide(this Color a, Color b) => new Color(a.r / b.r, a.g / b.g, a.b / b.b, a.a / b.a);
        public static Color32 Divide(this Color32 a, Color32 b) => new Color32((byte)(a.r / b.r), (byte)(a.g / b.g), (byte)(a.b / b.b), (byte)(a.a / b.a));
        #endregion
    }
}