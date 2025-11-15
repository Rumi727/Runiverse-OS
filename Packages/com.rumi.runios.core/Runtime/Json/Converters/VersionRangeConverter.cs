#nullable enable
using Newtonsoft.Json;

namespace RuniOS.Json.Converters;

/// <summary>
/// <see cref="VersionRange"/> 구조체를 JSON 문자열로 직렬화하고 역직렬화하는 <see cref="JsonConverter"/>입니다.
/// <br/>
/// 이 컨버터는 <see cref="VersionRange"/> 인스턴스를 해당 문자열 표현으로 변환하거나,
/// JSON 문자열을 <see cref="VersionRange"/> 인스턴스로 파싱하는 역할을 합니다.
/// </summary>
public class VersionRangeConverter : JsonConverter<VersionRange>
{
    /// <summary>
    /// <see cref="VersionRange"/> 객체를 JSON 문자열로 직렬화합니다.
    /// <br/>
    /// <see cref="VersionRange"/>의 <see cref="Version.ToString"/> 메서드를 사용하여 버전을 문자열로 기록합니다.
    /// </summary>
    /// <param name="writer">JSON 작성을 위한 <see cref="JsonWriter"/> 객체입니다.</param>
    /// <param name="value">직렬화할 <see cref="VersionRange"/> 값입니다.</param>
    /// <param name="serializer">직렬화 프로세스를 위한 <see cref="JsonSerializer"/> 객체입니다.</param>
    public override void WriteJson(JsonWriter writer, VersionRange value, JsonSerializer serializer) => writer.WriteValue(value.ToString());

    /// <summary>
    /// JSON 문자열을 <see cref="VersionRange"/> 객체로 역직렬화합니다.
    /// <br/>
    /// JSON 토큰이 <see langword="null"/>인 경우, 새 <see cref="VersionRange"/>를 반환합니다.
    /// 그렇지 않으면 JSON 문자열을 읽어 새로운 <see cref="VersionRange"/> 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="reader">JSON 읽기를 위한 <see cref="JsonReader"/> 객체입니다.</param>
    /// <param name="objectType">역직렬화할 객체의 <see cref="Type"/>입니다.</param>
    /// <param name="existingValue">기존에 존재하는 값입니다.</param>
    /// <param name="hasExistingValue">기존 값이 존재하는지 여부를 나타내는 <see langword="true"/> 또는 <see langword="false"/>입니다.</param>
    /// <param name="serializer">역직렬화 프로세스를 위한 <see cref="JsonSerializer"/> 객체입니다.</param>
    /// <returns>역직렬화된 <see cref="Version"/> 객체입니다.</returns>
    /// <exception cref="JsonSerializationException">JSON 토큰이 문자열이 아닌데도 <see cref="Version"/>로 변환을 시도할 때 발생합니다.</exception>
    public override VersionRange ReadJson(JsonReader reader, Type objectType, VersionRange existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return new VersionRange();
            
        // reader.ReadAsString()은 현재 토큰이 문자열이 아닐 경우 JsonSerializationException을 발생시킬 수 있습니다.
        return new VersionRange(reader.ReadAsString() ?? string.Empty);
    }
}