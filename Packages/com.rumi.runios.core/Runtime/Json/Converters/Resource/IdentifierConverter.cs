#nullable enable
using Newtonsoft.Json;
using RuniOS.Resource;

namespace RuniOS.Json.Converters.Resource;

/// <summary>
/// <see cref="Identifier"/> 구조체를 JSON 문자열로 직렬화하고 역직렬화하는 <see cref="JsonConverter"/>입니다.
/// <br/>
/// 이 컨버터는 <see cref="Identifier"/> 인스턴스를 해당 문자열 표현으로 변환하거나,
/// JSON 문자열을 <see cref="Identifier"/> 인스턴스로 파싱하는 역할을 합니다.
/// </summary>
public class IdentifierConverter : JsonConverter<Identifier>
{
    /// <summary>
    /// <see cref="Identifier"/> 객체를 JSON 문자열로 직렬화합니다.
    /// <br/>
    /// <see cref="Identifier"/>의 <see cref="Version.ToString"/> 메서드를 사용하여 식별자를 문자열로 기록합니다.
    /// </summary>
    /// <param name="writer">JSON 작성을 위한 <see cref="JsonWriter"/> 객체입니다.</param>
    /// <param name="value">직렬화할 <see cref="Identifier"/> 값입니다.</param>
    /// <param name="serializer">직렬화 프로세스를 위한 <see cref="JsonSerializer"/> 객체입니다.</param>
    public override void WriteJson(JsonWriter writer, Identifier value, JsonSerializer serializer) => writer.WriteValue(value.ToString());

    /// <summary>
    /// JSON 문자열을 <see cref="Identifier"/> 객체로 역직렬화합니다.
    /// <br/>
    /// JSON 토큰이 <see langword="null"/>인 경우, <see cref="Identifier.empty"/>를 반환합니다.
    /// 그렇지 않으면 JSON 문자열을 읽어 <see cref="Identifier.Parse(string)"/> 메서드를 통해 새로운 <see cref="Identifier"/> 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="reader">JSON 읽기를 위한 <see cref="JsonReader"/> 객체입니다.</param>
    /// <param name="objectType">역직렬화할 객체의 <see cref="Type"/>입니다.</param>
    /// <param name="existingValue">기존에 존재하는 값입니다.</param>
    /// <param name="hasExistingValue">기존 값이 존재하는지 여부를 나타내는 <see langword="true"/> 또는 <see langword="false"/>입니다.</param>
    /// <param name="serializer">역직렬화 프로세스를 위한 <see cref="JsonSerializer"/> 객체입니다.</param>
    /// <returns>역직렬화된 <see cref="Identifier"/> 객체입니다.</returns>
    /// <exception cref="JsonSerializationException">JSON 토큰이 문자열이 아닌데도 <see cref="Identifier"/>로 변환을 시도할 때 발생합니다.</exception>
    /// <exception cref="InvalidIdentifierException">
    /// JSON 문자열이 <see cref="Identifier"/>의 유효한 형식이 아닐 경우
    /// <see cref="Identifier.Parse(string)"/> 메서드 내부에서 발생할 수 있습니다.
    /// </exception>
    public override Identifier ReadJson(JsonReader reader, Type objectType, Identifier existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return Identifier.empty;
            
        // reader.ReadAsString()은 현재 토큰이 문자열이 아닐 경우 JsonSerializationException을 발생시킬 수 있습니다.
        string value = reader.ReadAsString() ?? string.Empty;
            
        // Identifier.Parse(string) 메서드가 InvalidIdentifierException을 발생시킬 수 있습니다.
        return Identifier.Parse(value);
    }
}