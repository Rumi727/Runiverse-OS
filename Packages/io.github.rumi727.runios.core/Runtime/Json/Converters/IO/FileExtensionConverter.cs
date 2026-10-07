#nullable enable
using Newtonsoft.Json;
using RuniOS.IO;

namespace RuniOS.Json.Converters.IO
{
    /// <summary>
    /// <see cref="FileExtension"/> 구조체를 JSON 문자열로 직렬화하고 역직렬화하는 <see cref="JsonConverter"/>입니다.
    /// <br/>
    /// 이 컨버터는 <see cref="FileExtension"/> 인스턴스를 해당 문자열 표현으로 변환하거나,
    /// JSON 문자열을 <see cref="FileExtension"/> 인스턴스로 파싱하는 역할을 합니다.
    /// </summary>
    public class FileExtensionConverter : JsonConverter<FileExtension>
    {
        /// <summary>
        /// <see cref="FileExtension"/> 객체를 JSON 문자열로 직렬화합니다.
        /// <br/>
        /// <see cref="FileExtension"/>의 <see cref="FileExtension.ToString"/> 메서드를 사용하여 경로를 문자열로 기록합니다.
        /// </summary>
        /// <param name="writer">JSON 작성을 위한 <see cref="JsonWriter"/> 객체입니다.</param>
        /// <param name="value">직렬화할 <see cref="FileExtension"/> 값입니다.</param>
        /// <param name="serializer">직렬화 프로세스를 위한 <see cref="JsonSerializer"/> 객체입니다.</param>
        public override void WriteJson(JsonWriter writer, FileExtension value, JsonSerializer serializer) => writer.WriteValue(value.ToString());

        /// <summary>
        /// JSON 문자열을 <see cref="FileExtension"/> 객체로 역직렬화합니다.
        /// <br/>
        /// JSON 문자열을 읽어 새로운 <see cref="FileExtension"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="reader">JSON 읽기를 위한 <see cref="JsonReader"/> 객체입니다.</param>
        /// <param name="objectType">역직렬화할 객체의 <see cref="Type"/>입니다.</param>
        /// <param name="existingValue">기존에 존재하는 값입니다.</param>
        /// <param name="hasExistingValue">기존 값이 존재하는지 여부를 나타내는 <see langword="true"/> 또는 <see langword="false"/>입니다.</param>
        /// <param name="serializer">역직렬화 프로세스를 위한 <see cref="JsonSerializer"/> 객체입니다.</param>
        /// <returns>역직렬화된 <see cref="FileExtension"/> 객체입니다.</returns>
        /// <exception cref="JsonReaderException">JSON 토큰이 문자열이 아닌데도 <see cref="FileExtension"/>로 변환을 시도할 때 발생합니다.</exception>
        public override FileExtension ReadJson(JsonReader reader, Type objectType, FileExtension existingValue, bool hasExistingValue, JsonSerializer serializer) => reader.TokenType switch
        {
            JsonToken.String => new FileExtension((string?)reader.Value ?? string.Empty),
            _ => throw new JsonReaderException($"Unexpected token type '{reader.TokenType}' when parsing FileExtension.")
        };
    }
}