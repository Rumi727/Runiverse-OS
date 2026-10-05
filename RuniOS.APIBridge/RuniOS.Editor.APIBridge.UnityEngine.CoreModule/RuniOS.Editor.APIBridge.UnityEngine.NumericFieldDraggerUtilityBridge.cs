namespace RuniOS.Editor.APIBridge.UnityEngine
{
    public partial class NumericFieldDraggerUtilityBridge
    {
        /// <summary>
        /// 지정된 <paramref name="value"/>를 기반으로 드래그 민감도를 계산합니다.
        /// </summary>
        /// <remarks>
        /// 이 메서드는 유니티 내부 코드의 <c>CalculateFloatDragSensitivity</c> 메서드를 <see langword="decimal"/> 타입으로 포팅한 것입니다.<br/>
        /// 이 코드는 Unity 내부 코드로의 <b>브릿지가 아니며</b>, 편의를 위해 직접 작성되었습니다.
        /// <br/><br/>
        /// This method is a port of the <c>CalculateFloatDragSensitivity</c> method from Unity's internal code to the <see langword="decimal"/> type.<br/>
        /// This code is <b>not a bridge</b> to Unity's internal code; it was written directly for convenience.
        /// </remarks>
        /// <param name="value">계산에 사용될 <see langword="decimal"/> 값입니다.</param>
        /// <returns>계산된 드래그 민감도 값을 반환합니다.</returns>
        public static decimal CalculateDecimalDragSensitivity(decimal value)
        {
            const decimal Zero = 0.0M;
            const decimal One = 1.0M;
            const decimal E = 2.7182818284590452353602874713526624977572470936999595749M;
            const decimal Einv = 0.3678794411714423215955237701614608674458111310317678M;
            const int MaxIteration = 100;
            
            return Max(1.0m, Pow(Abs(value), 0.5m)) * 0.03m;

            static decimal Max(decimal a, decimal b)
            {
                if (a > b)
                    return a;
                
                return b;
            }

            static decimal Abs(decimal value) => value < 0 ? -value : value;
            
            static decimal Pow(decimal value, decimal pow)
            {
                if (value == Zero)
                    return Zero;
                
                return Exp(pow * Log(value));
            }

            static decimal Exp(decimal x)
            {
                var count = 0;

                if (x > One)
                {
                    count = decimal.ToInt32(decimal.Truncate(x));
                    x -= decimal.Truncate(x);
                }

                if (x < Zero)
                {
                    count = decimal.ToInt32(decimal.Truncate(x) - 1);
                    x = One + (x - decimal.Truncate(x));
                }

                var iteration = 1;
                var result = One;
                var factorial = One;
                decimal cachedResult;
                do
                {
                    cachedResult = result;
                    factorial *= x / iteration++;
                    result += factorial;
                }
                while (cachedResult != result);

                if (count == 0)
                    return result;
                return result * PowerN(E, count);
            }

            static decimal PowerN(decimal value, int power)
            {
                while (true)
                {
                    if (power == Zero) return One;
                    if (power < Zero)
                    {
                        value = One / value;
                        power = -power;
                        continue;
                    }

                    var q = power;
                    var prod = One;
                    var current = value;
                    while (q > 0)
                    {
                        if (q % 2 == 1)
                        {
                            // detects the 1s in the binary expression of power
                            prod = current * prod; // picks up the relevant power
                            q--;
                        }

                        current *= current; // value^i -> value^(2*i)
                        q >>= 1;
                    }

                    return prod;
                }
            }
            
            static decimal Log(decimal x)
            {
                var count = 0;
                while (x >= One)
                {
                    x *= Einv;
                    count++;
                }
                while (x <= Einv)
                {
                    x *= E;
                    count--;
                }
                x--;
                if (x == Zero) return count;
                var result = Zero;
                var iteration = 0;
                var y = One;
                var cacheResult = result - One;
                while (cacheResult != result && iteration < MaxIteration)
                {
                    iteration++;
                    cacheResult = result;
                    y *= -x;
                    result += y / iteration;
                }
                return count - result;
            }
        }
    }
}