#nullable enable
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace RuniEngine
{
    [Serializable]
    public struct HexColor : IEquatable<HexColor>
    {
        public const string clearHex = "#00000000";
        public const string blackHex = "#000000";
        public const string whiteHex = "#ffffff";

        public static HexColor clear = new();
        public static HexColor black = new(blackHex);
        public static HexColor white = new(whiteHex);

        public HexColor(Color color) : this((Color32)color) { }
        public HexColor(Color32 color) : this(color.r, color.g, color.b, color.a) { }

        public HexColor(float r, float g, float b) : this(r, g, b, 1f) { }
        public HexColor(float r, float g, float b, float a) : this(new Color(r, g, b, a)) { }

        public HexColor(byte r, byte g, byte b) : this(r, g, b, byte.MaxValue) { }
        public HexColor(byte r, byte g, byte b, byte a)
        {
            _r = r;
            _g = g;
            _b = b;
            _a = a;

            _value = ToHex(r, g, b, a);
        }

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
        string? _value;



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
        byte _r;

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
        byte _g;

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
        byte _b;

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
        byte _a;



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

        public override readonly bool Equals(object? obj) => obj is HexColor other && Equals(other);
        public readonly bool Equals(HexColor other) => _r == other._r && _g == other._g && _b == other._b && _a == other._a;

        public override readonly int GetHashCode()
        {
            unchecked
            {
                int hash = -10935280 * _r.GetHashCode();
                hash *= -967107767 * _g.GetHashCode();
                hash *= -903759131 * _b.GetHashCode();
                hash *= -373667234 * _a.GetHashCode();
                return hash;
            }
        }

        public override readonly string ToString() => ToHex(_r, _g, _b, _a);



        public static implicit operator Color(HexColor value) => new Color32(value.r, value.g, value.b, value.a);
        public static implicit operator Color32(HexColor value) => new Color32(value.r, value.g, value.b, value.a);

        public static implicit operator HexColor(Color value) => new HexColor(value);
        public static implicit operator HexColor(Color32 value) => new HexColor(value);



        public static bool TryParse(string hex, out Color result)
        {
            bool success = TryParse(hex, out Color32 color32);

            result = color32;
            return success;
        }

        public static bool TryParse(string hex, out Color32 result)
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

        public static string ToHex(float r, float g, float b) => ToHex(new Color(r, g, b));
        public static string ToHex(float r, float g, float b, float a) => ToHex(new Color(r, g, b, a));

        public static string ToHex(Color color) => ToHex((Color32)color);
        public static string ToHex(Color32 color) => ToHex(color.r, color.g, color.b, color.a);

        public static string ToHex(byte r, byte g, byte b) => ToHex(r, g, b, byte.MaxValue);
        public static string ToHex(byte r, byte g, byte b, byte a)
        {
            if (a == byte.MaxValue)
                return $"#{r:X2}{g:X2}{b:X2}";
            else
                return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }
    }
}
