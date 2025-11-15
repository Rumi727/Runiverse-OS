#nullable enable
using Newtonsoft.Json;

namespace RuniOS.Json.Converters
{
    /// <summary>
    /// <see cref="SerializableType"/> 구조체를 JSON 문자열로 직렬화하고 역직렬화하는 <see cref="JsonConverter"/>입니다.
    /// <br/>
    /// 이 컨버터는 <see cref="Type"/> 객체를 유니티 환경에 적합한 문자열로 변환하거나,
    /// 문자열로부터 <see cref="Type"/> 객체를 다시 파싱합니다.
    /// </summary>
    public class SerializableTypeConverter : JsonConverter<SerializableType>
    {
        /// <summary>
        /// <see cref="SerializableType"/> 객체를 JSON 문자열로 직렬화합니다.
        /// <br/>
        /// 내부 <see cref="Type"/> 값이 <see langword="null"/>인 경우 JSON <see langword="null"/>을 기록하고,
        /// 그렇지 않으면 <see cref="TypeUtility.SerializeToString(Type)"/> 메서드를 사용하여 타입을 문자열로 변환하여 기록합니다.
        /// </summary>
        /// <param name="writer">JSON 작성을 위한 <see cref="JsonWriter"/> 객체입니다.</param>
        /// <param name="value">직렬화할 <see cref="SerializableType"/> 값입니다.</param>
        /// <param name="serializer">직렬화 프로세스를 위한 <see cref="JsonSerializer"/> 객체입니다.</param>
        public override void WriteJson(JsonWriter writer, SerializableType value, JsonSerializer serializer)
        {
            if (value.value == null)
                writer.WriteNull();
            else
                writer.WriteValue(value.value.SerializeToString());
        }

        /// <summary>
        /// JSON 문자열을 <see cref="SerializableType"/> 객체로 역직렬화합니다.
        /// <br/>
        /// JSON 토큰이 <see langword="null"/>이거나 읽어온 문자열이 비어 있으면 <see langword="null"/> <see cref="Type"/>을 포함하는
        /// 새로운 <see cref="SerializableType"/> 인스턴스를 반환합니다.
        /// <br/>
        /// 그렇지 않으면 <see cref="TypeUtility.DeserializeFromString(string)"/> 메서드를 사용하여 문자열로부터 <see cref="Type"/>을 파싱합니다.
        /// 파싱에 실패하면 <see langword="null"/> <see cref="Type"/>을 반환할 수 있습니다.
        /// </summary>
        /// <param name="reader">JSON 읽기를 위한 <see cref="JsonReader"/> 객체입니다.</param>
        /// <param name="objectType">역직렬화할 객체의 <see cref="Type"/>입니다.</param>
        /// <param name="existingValue">기존에 존재하는 값입니다.</param>
        /// <param name="hasExistingValue">기존 값이 존재하는지 여부를 나타내는 <see langword="true"/> 또는 <see langword="false"/>입니다.</param>
        /// <param name="serializer">역직렬화 프로세스를 위한 <see cref="JsonSerializer"/> 객체입니다.</param>
        /// <returns>역직렬화된 <see cref="SerializableType"/> 객체입니다.</returns>
        /// <exception cref="JsonSerializationException">JSON 토큰이 문자열이 아닌데도 <see cref="SerializableType"/>으로 변환을 시도할 때 발생합니다.</exception>
        public override SerializableType ReadJson(JsonReader reader, Type objectType, SerializableType existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return new SerializableType(null);

            // reader.ReadAsString()은 현재 토큰이 문자열이 아닐 경우 JsonSerializationException을 발생시킬 수 있습니다.
            string? typeName = reader.ReadAsString();
            if (string.IsNullOrEmpty(typeName))
                return new SerializableType(null);

            // TypeUtility.DeserializeFromString()은 문자열로부터 Type을 역직렬화하며, 실패 시 null을 반환할 수 있습니다.
            return new SerializableType(TypeUtility.DeserializeFromString(typeName));
        }
    }
}