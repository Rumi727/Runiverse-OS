#nullable enable
using Newtonsoft.Json;
using RuniEngine.Json.Converters;
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace RuniEngine
{
    /// <summary>
    /// 16진수 문자열로 색상을 표현하고 관리하는 직렬화 가능한 구조체입니다.
    /// <br/>
    /// 유니티의 <see cref="Color"/> 및 <see cref="Color32"/> 타입과 암시적 변환을 지원하며,
    /// Json.NET 직렬화 시 <see cref="HexColorConverter"/>를 사용합니다.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(HexColorConverter))]
    public struct HexColor : IEquatable<HexColor>, ISerializationCallbackReceiver
    {
        /// <summary>
        /// 완전히 투명한 16진수 색상 문자열 "#00000000"을 나타내는 상수입니다.
        /// </summary>
        public const string clearHex = "#00000000";
        /// <summary>
        /// 검은색 16진수 색상 문자열 "#000000"을 나타내는 상수입니다.
        /// </summary>
        public const string blackHex = "#000000";
        /// <summary>
        /// 흰색 16진수 색상 문자열 "#ffffff"을 나타내는 상수입니다.
        /// </summary>
        public const string whiteHex = "#ffffff";

        /// <summary>
        /// 완전히 투명한 <see cref="HexColor"/> 인스턴스를 나타냅니다. (<see cref="clearHex"/>와 동일)
        /// </summary>
        public static HexColor clear = new();
        /// <summary>
        /// 검은색 <see cref="HexColor"/> 인스턴스를 나타냅니다. (<see cref="blackHex"/>와 동일)
        /// </summary>
        public static HexColor black = new(blackHex);
        /// <summary>
        /// 흰색 <see cref="HexColor"/> 인스턴스를 나타냅니다. (<see cref="whiteHex"/>와 동일)
        /// </summary>
        public static HexColor white = new(whiteHex);

        /// <summary>
        /// <see cref="Color"/> 값으로 <see cref="HexColor"/> 구조체의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="color">초기화할 <see cref="Color"/> 값입니다.</param>
        public HexColor(Color color) : this((Color32)color) { }
        /// <summary>
        /// <see cref="Color32"/> 값으로 <see cref="HexColor"/> 구조체의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="color">초기화할 <see cref="Color32"/> 값입니다.</param>
        public HexColor(Color32 color) : this(color.r, color.g, color.b, color.a) { }

        /// <summary>
        /// RGB 및 선택적으로 알파(기본값은 1f) float 값으로 <see cref="HexColor"/> 구조체의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="r">빨간색 구성 요소의 float 값 (0.0f - 1.0f)입니다.</param>
        /// <param name="g">초록색 구성 요소의 float 값 (0.0f - 1.0f)입니다.</param>
        /// <param name="b">파란색 구성 요소의 float 값 (0.0f - 1.0f)입니다.</param>
        /// <param name="a">알파 구성 요소의 float 값 (0.0f - 1.0f)입니다. 기본값은 1f입니다.</param>
        public HexColor(float r, float g, float b, float a = 1f) : this(new Color(r, g, b, a)) { }

        /// <summary>
        /// RGB 및 선택적으로 알파(기본값은 <see cref="byte.MaxValue"/>) byte 값으로 <see cref="HexColor"/> 구조체의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="r">빨간색 구성 요소의 byte 값 (0 - 255)입니다.</param>
        /// <param name="g">초록색 구성 요소의 byte 값 (0 - 255)입니다.</param>
        /// <param name="b">파란색 구성 요소의 byte 값 (0 - 255)입니다.</param>
        /// <param name="a">알파 구성 요소의 byte 값 (0 - 255)입니다. 기본값은 <see cref="byte.MaxValue"/>입니다.</param>
        public HexColor(byte r, byte g, byte b, byte a = byte.MaxValue)
        {
            _r = r;
            _g = g;
            _b = b;
            _a = a;

            _value = ToHex(r, g, b, a);
        }

        /// <summary>
        /// 16진수 문자열로 <see cref="HexColor"/> 구조체의 새 인스턴스를 초기화합니다.
        /// <br/>
        /// 문자열 파싱에 실패하면 (<see cref="TryParse(string, out Color32)"/> 참조),
        /// <see cref="clearHex"/> 값으로 초기화됩니다.
        /// </summary>
        /// <param name="hex">"#RRGGBBAA", "#RRGGBB", "#RGBA", "#RGB" 형식의 16진수 색상 문자열입니다.</param>
        public HexColor(string hex)
        {
            if (TryParse(hex, out Color32 color))
            {
                _value = hex;

                _r = color.r;
                _g = color.g;
                _b = color.b;
                _a = color.a;
            }
            else
            {
                _value = clearHex;

                _r = 0;
                _g = 0;
                _b = 0;
                _a = 0;
            }
        }

        /// <summary>
        /// 이 <see cref="HexColor"/> 인스턴스의 16진수 문자열 표현을 가져오거나 설정합니다.
        /// <br/>
        /// 설정 시 유효하지 않은 16진수 문자열이 제공되면
        /// <see cref="clearHex"/> 값으로 설정되고 모든 색상 구성 요소는 0으로 초기화됩니다.
        /// </summary>
        [AllowNull]
        public string value
        {
            readonly get => _value ?? clearHex;
            set
            {
                if (TryParse(value, out Color32 color))
                {
                    _value = value;

                    _r = color.r;
                    _g = color.g;
                    _b = color.b;
                    _a = color.a;
                }
                else
                {
                    _value = clearHex;

                    _r = 0;
                    _g = 0;
                    _b = 0;
                    _a = 0;
                }
            }
        }
        [SerializeField, FieldName("gui.value"), NotNullField] string? _value;

        /// <summary>
        /// 이 <see cref="HexColor"/> 인스턴스의 빨간색 구성 요소(byte 값)를 가져오거나 설정합니다.
        /// <br/>
        /// 설정 시 16진수 문자열 값(<see cref="value"/>)이 자동으로 업데이트됩니다.
        /// </summary>
        [JsonIgnore]
        public byte r
        {
            readonly get => _r;
            set
            {
                _r = value;
                _value = ToString();
            }
        }
        [JsonIgnore] byte _r;

        /// <summary>
        /// 이 <see cref="HexColor"/> 인스턴스의 초록색 구성 요소(byte 값)를 가져오거나 설정합니다.
        /// <br/>
        /// 설정 시 16진수 문자열 값(<see cref="value"/>)이 자동으로 업데이트됩니다.
        /// </summary>
        [JsonIgnore]
        public byte g
        {
            readonly get => _g;
            set
            {
                _g = value;
                _value = ToString();
            }
        }
        [JsonIgnore] byte _g;

        /// <summary>
        /// 이 <see cref="HexColor"/> 인스턴스의 파란색 구성 요소(byte 값)를 가져오거나 설정합니다.
        /// <br/>
        /// 설정 시 16진수 문자열 값(<see cref="value"/>)이 자동으로 업데이트됩니다.
        /// </summary>
        [JsonIgnore]
        public byte b
        {
            readonly get => _b;
            set
            {
                _b = value;
                _value = ToString();
            }
        }
        [JsonIgnore] byte _b;

        /// <summary>
        /// 이 <see cref="HexColor"/> 인스턴스의 알파 구성 요소(byte 값)를 가져오거나 설정합니다.
        /// <br/>
        /// 설정 시 16진수 문자열 값(<see cref="value"/>)이 자동으로 업데이트됩니다.
        /// </summary>
        [JsonIgnore]
        public byte a
        {
            readonly get => _a;
            set
            {
                _a = value;
                _value = ToString();
            }
        }
        [JsonIgnore] byte _a;

        /// <summary>
        /// 이 <see cref="HexColor"/> 인스턴스의 <see cref="Color"/> 표현을 가져오거나 설정합니다.
        /// <br/>
        /// 설정 시 내부 byte 구성 요소와 16진수 문자열 값(<see cref="value"/>)이 자동으로 업데이트됩니다.
        /// </summary>
        [JsonIgnore]
        public Color color
        {
            readonly get => new Color32(_r, _g, _b, _a);
            set
            {
                Color32 color32 = value;

                _r = color32.r;
                _g = color32.g;
                _b = color32.b;
                _a = color32.a;

                _value = ToHex(value);
            }
        }

        /// <summary>
        /// 이 <see cref="HexColor"/> 인스턴스의 <see cref="Color32"/> 표현을 가져오거나 설정합니다.
        /// <br/>
        /// 설정 시 내부 byte 구성 요소와 16진수 문자열 값(<see cref="value"/>)이 자동으로 업데이트됩니다.
        /// </summary>
        [JsonIgnore]
        public Color32 color32
        {
            readonly get => new Color32(_r, _g, _b, _a);
            set
            {
                _r = value.r;
                _g = value.g;
                _b = value.b;
                _a = value.a;

                _value = ToHex(value);
            }
        }

        /// <summary>
        /// 이 <see cref="HexColor"/> 인스턴스와 지정된 <see cref="object"/>의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="obj">현재 인스턴스와 비교할 <see cref="object"/>입니다.</param>
        /// <returns>지정된 <see cref="object"/>가 <see cref="HexColor"/>이고 현재 인스턴스와 같은 값을 가지면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public override readonly bool Equals(object? obj) => obj is HexColor other && Equals(other);
        
        /// <summary>
        /// 이 <see cref="HexColor"/> 인스턴스와 다른 지정된 <see cref="HexColor"/> 인스턴스의 값이 같은지 여부를 결정합니다.
        /// </summary>
        /// <param name="other">현재 인스턴스와 비교할 <see cref="HexColor"/>입니다.</param>
        /// <returns>지정된 <see cref="HexColor"/>가 현재 인스턴스와 같은 값을 가지면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public readonly bool Equals(HexColor other) => _r == other._r && _g == other._g && _b == other._b && _a == other._a;

        /// <summary>
        /// 이 <see cref="HexColor"/> 인스턴스의 해시 코드를 반환합니다.
        /// </summary>
        /// <returns>32비트 부호 있는 정수 해시 코드입니다.</returns>
        public override readonly int GetHashCode() => HashCode.Combine(_r, _g, _b, _a);

        /// <summary>
        /// 이 <see cref="HexColor"/> 인스턴스의 16진수 문자열 표현을 반환합니다.
        /// </summary>
        /// <returns>"#RRGGBBAA" 또는 "#RRGGBB" 형식의 문자열입니다.</returns>
        public override readonly string ToString() => ToHex(_r, _g, _b, _a);

        /// <summary>
        /// <see cref="HexColor"/>를 <see cref="Color"/>로 암시적으로 변환합니다.
        /// </summary>
        /// <param name="value">변환할 <see cref="HexColor"/> 값입니다.</param>
        /// <returns>변환된 <see cref="Color"/> 값입니다.</returns>
        public static implicit operator Color(HexColor value) => new Color32(value.r, value.g, value.b, value.a);
        
        /// <summary>
        /// <see cref="HexColor"/>를 <see cref="Color32"/>로 암시적으로 변환합니다.
        /// </summary>
        /// <param name="value">변환할 <see cref="HexColor"/> 값입니다.</param>
        /// <returns>변환된 <see cref="Color32"/> 값입니다.</returns>
        public static implicit operator Color32(HexColor value) => new Color32(value.r, value.g, value.b, value.a);

        /// <summary>
        /// <see cref="Color"/>를 <see cref="HexColor"/>로 암시적으로 변환합니다.
        /// </summary>
        /// <param name="value">변환할 <see cref="Color"/> 값입니다.</param>
        /// <returns>변환된 <see cref="HexColor"/> 값입니다.</returns>
        public static implicit operator HexColor(Color value) => new HexColor(value);
        
        /// <summary>
        /// <see cref="Color32"/>를 <see cref="HexColor"/>로 암시적으로 변환합니다.
        /// </summary>
        /// <param name="value">변환할 <see cref="Color32"/> 값입니다.</param>
        /// <returns>변환된 <see cref="HexColor"/> 값입니다.</returns>
        public static implicit operator HexColor(Color32 value) => new HexColor(value);

        
        
        /// <summary>
        /// 이 <see cref="HexColor"/> 인스턴스가 직렬화되기 전에 호출됩니다.
        /// 내부 값을 최신 상태로 동기화합니다.
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize() => value = value;
        
        /// <summary>
        /// 이 <see cref="HexColor"/> 인스턴스가 역직렬화된 후에 호출됩니다.
        /// 내부 값을 최신 상태로 동기화합니다.
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize() => value = value;

        
        
        /// <summary>
        /// 16진수 문자열을 <see cref="Color"/>로 구문 분석합니다.
        /// </summary>
        /// <param name="hex">구문 분석할 16진수 문자열입니다.</param>
        /// <param name="result">구문 분석 성공 시 결과 <see cref="Color"/>가 할당됩니다.</param>
        /// <returns>구문 분석에 성공하면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool TryParse(string hex, out Color result)
        {
            bool success = TryParse(hex, out Color32 color32);

            result = color32;
            return success;
        }

        /// <summary>
        /// 16진수 문자열을 <see cref="Color32"/>로 구문 분석합니다.
        /// <br/>
        /// 지원되는 형식은 "#RRGGBBAA", "#RRGGBB", "#RGBA", "#RGB"입니다.
        /// </summary>
        /// <param name="hex">구문 분석할 16진수 문자열입니다.</param>
        /// <param name="result">구문 분석 성공 시 결과 <see cref="Color32"/>가 할당됩니다.</param>
        /// <returns>구문 분석에 성공하면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public static bool TryParse(string? hex, out Color32 result)
        {
            if (string.IsNullOrEmpty(hex))
            {
                result = default;
                return false;
            }

            int sharp = 0;
            if (hex[0] == '#')
                sharp++;

            try
            {
                // '#'를 고려한 후 파싱할 충분한 문자열이 있는지 확인
                if (hex.Length >= 3 + sharp && hex.Length <= 8 + sharp) // 최대 길이: #RRGGBBAA
                {
                    if (hex.Length == 8 + sharp) // #RRGGBBAA
                    {
                        result = new Color32(Convert.ToByte(hex.Substring(0 + sharp, 2), 16), Convert.ToByte(hex.Substring(2 + sharp, 2), 16), Convert.ToByte(hex.Substring(4 + sharp, 2), 16), Convert.ToByte(hex.Substring(6 + sharp, 2), 16));
                        return true;
                    }
                    else if (hex.Length == 6 + sharp) // #RRGGBB
                    {
                        result = new Color32(Convert.ToByte(hex.Substring(0 + sharp, 2), 16), Convert.ToByte(hex.Substring(2 + sharp, 2), 16), Convert.ToByte(hex.Substring(4 + sharp, 2), 16), 255);
                        return true;
                    }
                    else if (hex.Length == 4 + sharp) // #RGBA
                    {
                        string i = hex[0 + sharp].ToString();
                        string i1 = hex[1 + sharp].ToString();
                        string i2 = hex[2 + sharp].ToString();
                        string i3 = hex[3 + sharp].ToString();

                        result = new Color32(Convert.ToByte(i + i, 16), Convert.ToByte(i1 + i1, 16), Convert.ToByte(i2 + i2, 16), Convert.ToByte(i3 + i3, 16));
                        return true;
                    }
                    else if (hex.Length == 3 + sharp) // #RGB
                    {
                        string i = hex[0 + sharp].ToString();
                        string i1 = hex[1 + sharp].ToString();
                        string i2 = hex[2 + sharp].ToString();

                        result = new Color32(Convert.ToByte(i + i, 16), Convert.ToByte(i1 + i1, 16), Convert.ToByte(i2 + i2, 16), 255);
                        return true;
                    }
                }
            }
            catch
            {
                // 파싱 실패 시 false를 반환합니다.
            }

            result = default;
            return false;
        }

        /// <summary>
        /// RGB float 값으로 16진수 색상 문자열을 생성합니다.
        /// </summary>
        /// <param name="r">빨간색 구성 요소의 float 값 (0.0f - 1.0f)입니다.</param>
        /// <param name="g">초록색 구성 요소의 float 값 (0.0f - 1.0f)입니다.</param>
        /// <param name="b">파란색 구성 요소의 float 값 (0.0f - 1.0f)입니다.</param>
        /// <returns>"#RRGGBB" 형식의 16진수 문자열입니다.</returns>
        public static string ToHex(float r, float g, float b) => ToHex(new Color(r, g, b));
        
        /// <summary>
        /// RGBA float 값으로 16진수 색상 문자열을 생성합니다.
        /// </summary>
        /// <param name="r">빨간색 구성 요소의 float 값 (0.0f - 1.0f)입니다.</param>
        /// <param name="g">초록색 구성 요소의 float 값 (0.0f - 1.0f)입니다.</param>
        /// <param name="b">파란색 구성 요소의 float 값 (0.0f - 1.0f)입니다.</param>
        /// <param name="a">알파 구성 요소의 float 값 (0.0f - 1.0f)입니다.</param>
        /// <returns>"#RRGGBBAA" 형식의 16진수 문자열입니다.</returns>
        public static string ToHex(float r, float g, float b, float a) => ToHex(new Color(r, g, b, a));

        /// <summary>
        /// <see cref="Color"/> 값으로 16진수 색상 문자열을 생성합니다.
        /// </summary>
        /// <param name="color">변환할 <see cref="Color"/> 값입니다.</param>
        /// <returns>"#RRGGBBAA" 또는 "#RRGGBB" 형식의 16진수 문자열입니다.</returns>
        public static string ToHex(Color color) => ToHex((Color32)color);
        
        /// <summary>
        /// <see cref="Color32"/> 값으로 16진수 색상 문자열을 생성합니다.
        /// </summary>
        /// <param name="color">변환할 <see cref="Color32"/> 값입니다.</param>
        /// <returns>"#RRGGBBAA" 또는 "#RRGGBB" 형식의 16진수 문자열입니다.</returns>
        public static string ToHex(Color32 color) => ToHex(color.r, color.g, color.b, color.a);

        /// <summary>
        /// RGB byte 값으로 16진수 색상 문자열을 생성합니다.
        /// </summary>
        /// <param name="r">빨간색 구성 요소의 byte 값 (0 - 255)입니다.</param>
        /// <param name="g">초록색 구성 요소의 byte 값 (0 - 255)입니다.</param>
        /// <param name="b">파란색 구성 요소의 byte 값 (0 - 255)입니다.</param>
        /// <returns>"#RRGGBB" 형식의 16진수 문자열입니다.</returns>
        public static string ToHex(byte r, byte g, byte b) => ToHex(r, g, b, byte.MaxValue);
        
        /// <summary>
        /// RGBA byte 값으로 16진수 색상 문자열을 생성합니다.
        /// </summary>
        /// <param name="r">빨간색 구성 요소의 byte 값 (0 - 255)입니다.</param>
        /// <param name="g">초록색 구성 요소의 byte 값 (0 - 255)입니다.</param>
        /// <param name="b">파란색 구성 요소의 byte 값 (0 - 255)입니다.</param>
        /// <param name="a">알파 구성 요소의 byte 값 (0 - 255)입니다.</param>
        /// <returns>알파 값이 <see cref="byte.MaxValue"/>이면 "#RRGGBB" 형식, 그렇지 않으면 "#RRGGBBAA" 형식의 16진수 문자열입니다.</returns>
        public static string ToHex(byte r, byte g, byte b, byte a)
        {
            if (a == byte.MaxValue)
                return $"#{r:X2}{g:X2}{b:X2}";
            else
                return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }
    }
}