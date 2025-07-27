#nullable enable
using Newtonsoft.Json;
using System;

namespace RuniEngine.Json.Converters
{
    /// <summary>
    /// <see cref="HexColor"/> 구조체를 16진수 색상 문자열로 직렬화하고 역직렬화하는 <see cref="JsonConverter"/>입니다.
    /// <br/>
    /// 이 컨버터는 <see cref="HexColor"/> 인스턴스를 "#RRGGBBAA" 또는 "#RRGGBB" 형식의 문자열로 변환하고,
    /// JSON 문자열을 <see cref="HexColor"/> 인스턴스로 파싱합니다.
    /// </summary>
    public class HexColorConverter : JsonConverter<HexColor>
    {
        /// <summary>
        /// <see cref="HexColor"/> 객체를 JSON 문자열로 직렬화합니다.
        /// <br/>
        /// <see cref="HexColor"/>의 <see cref="object.ToString"/> 메서드를 사용하여 16진수 색상 문자열을 기록합니다.
        /// </summary>
        /// <param name="writer">JSON 작성을 위한 <see cref="JsonWriter"/> 객체입니다.</param>
        /// <param name="value">직렬화할 <see cref="HexColor"/> 값입니다.</param>
        /// <param name="serializer">직렬화 프로세스를 위한 <see cref="JsonSerializer"/> 객체입니다.</param>
        public override void WriteJson(JsonWriter writer, HexColor value, JsonSerializer serializer) => writer.WriteValue(value.ToString());

        /// <summary>
        /// JSON 문자열을 <see cref="HexColor"/> 객체로 역직렬화합니다.
        /// <br/>
        /// JSON 토큰이 <see langword="null"/>이거나 읽어온 문자열이 <see langword="null"/>인 경우 <see cref="HexColor.clear"/>를 반환합니다.
        /// <br/>
        /// 유효하지 않은 16진수 문자열이 제공될 경우, <see cref="HexColor"/> 생성자 내부에서 파싱에 실패하여
        /// 결과적으로 <see cref="HexColor.clear"/>에 해당하는 값으로 인스턴스가 생성됩니다.
        /// </summary>
        /// <param name="reader">JSON 읽기를 위한 <see cref="JsonReader"/> 객체입니다.</param>
        /// <param name="objectType">역직렬화할 객체의 <see cref="Type"/>입니다.</param>
        /// <param name="existingValue">기존에 존재하는 값입니다.</param>
        /// <param name="hasExistingValue">기존 값이 존재하는지 여부를 나타내는 <see langword="true"/> 또는 <see langword="false"/>입니다.</param>
        /// <param name="serializer">역직렬화 프로세스를 위한 <see cref="JsonSerializer"/> 객체입니다.</param>
        /// <returns>역직렬화된 <see cref="HexColor"/> 객체입니다.</returns>
        /// <exception cref="JsonSerializationException">JSON 토큰이 문자열이 아닌데도 <see cref="HexColor"/>로 변환을 시도할 때 발생합니다.</exception>
        public override HexColor ReadJson(JsonReader reader, Type objectType, HexColor existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return HexColor.clear;

            // reader.ReadAsString()은 현재 토큰이 문자열이 아닐 경우 JsonSerializationException을 발생시킬 수 있습니다.
            string? value = reader.ReadAsString();
            
            // 읽어온 문자열이 null인 경우 clear 반환
            if (value == null)
                return HexColor.clear;
            
            // HexColor 생성자는 파싱에 실패할 경우 내부적으로 clearHex 값으로 설정됩니다.
            return new HexColor(value);
        }
    }
}
