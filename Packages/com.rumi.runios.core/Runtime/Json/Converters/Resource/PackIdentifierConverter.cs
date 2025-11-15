#nullable enable
using Newtonsoft.Json;
using RuniOS.IO;
using RuniOS.Resource;

namespace RuniOS.Json.Converters.Resource
{
    /// <summary>
    /// <see cref="PackIdentifier"/> 구조체를 JSON 객체로 직렬화하고 역직렬화하는 <see cref="JsonConverter"/>입니다.
    /// <br/>
    /// 이 컨버터는 <see cref="PackIdentifier.identifier"/> 또는 <see cref="PackIdentifier.path"/> 중
    /// 유효한 필드 하나만 JSON 속성으로 직렬화하며, 역직렬화 시에도 둘 중 하나를 파싱하여
    /// <see cref="PackIdentifier"/> 인스턴스를 생성합니다.
    /// </summary>
    public class PackIdentifierConverter : JsonConverter<PackIdentifier>
    {
        /// <summary>
        /// <see cref="PackIdentifier"/> 객체를 JSON 객체로 직렬화합니다.
        /// <br/>
        /// <see cref="PackIdentifier.identifier"/>가 유효하면 "identifier" 속성을 사용하여 해당 값을 직렬화합니다.
        /// <br/>
        /// 그렇지 않고 <see cref="PackIdentifier.path"/>가 유효하면 "path" 속성을 사용하여 해당 값을 직렬화합니다.
        /// <br/>
        /// 두 필드(<see cref="PackIdentifier.identifier"/>와 <see cref="PackIdentifier.path"/>)가 모두 <see langword="null"/>인 경우에는
        /// "identifier" 속성을 사용하여 <see cref="Identifier.empty"/> 값을 직렬화합니다.
        /// </summary>
        /// <param name="writer">JSON 작성을 위한 <see cref="JsonWriter"/> 객체입니다.</param>
        /// <param name="value">직렬화할 <see cref="PackIdentifier"/> 값입니다.</param>
        /// <param name="serializer">직렬화 프로세스를 위한 <see cref="JsonSerializer"/> 객체입니다.</param>
        public override void WriteJson(JsonWriter writer, PackIdentifier value, JsonSerializer serializer)
        {
            writer.WriteStartObject(); // JSON 객체 시작
            
            // identifier 또는 path 중 하나만 직렬화
            if (value.identifier != null)
            {
                writer.WritePropertyName(nameof(PackIdentifier.identifier));
                writer.WriteValue(value.identifier);
            }
            else if (value.path != null)
            {
                writer.WritePropertyName(nameof(PackIdentifier.path));
                writer.WriteValue(value.path);
            }
            else
            {
                writer.WritePropertyName(nameof(PackIdentifier.identifier));
                writer.WriteValue(Identifier.empty);
            }
            
            writer.WriteEndObject(); // JSON 객체 종료
        }

        /// <summary>
        /// JSON 객체를 <see cref="PackIdentifier"/> 객체로 역직렬화합니다.
        /// <br/>
        /// JSON 객체 내의 "identifier" 또는 "path" 속성을 찾아 해당 값을 읽어
        /// <see cref="PackIdentifier.CreateByID(Identifier)"/> 또는 <see cref="PackIdentifier.CreateByPath(FilePath)"/>를 통해 인스턴스를 생성합니다.
        /// 유효한 속성이 없거나 JSON 형식이 올바르지 않으면 <see cref="PackIdentifier.empty"/>를 반환합니다.
        /// </summary>
        /// <param name="reader">JSON 읽기를 위한 <see cref="JsonReader"/> 객체입니다.</param>
        /// <param name="objectType">역직렬화할 객체의 <see cref="Type"/>입니다.</param>
        /// <param name="existingValue">기존에 존재하는 값입니다.</param>
        /// <param name="hasExistingValue">기존 값이 존재하는지 여부를 나타내는 <see langword="true"/> 또는 <see langword="false"/>입니다.</param>
        /// <param name="serializer">역직렬화 프로세스를 위한 <see cref="JsonSerializer"/> 객체입니다.</param>
        /// <returns>역직렬화된 <see cref="PackIdentifier"/> 객체입니다.</returns>
        /// <exception cref="JsonSerializationException">
        /// JSON 형식이 유효하지 않거나, "identifier" 또는 "path" 속성 값의 타입이 예상과 다를 경우 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="InvalidIdentifierException">
        /// "identifier" 속성 값을 <see cref="Identifier"/>로 역직렬화하는 과정에서
        /// <see cref="Identifier.Parse(string)"/> 메서드 내부적으로 발생할 수 있습니다.
        /// </exception>
        public override PackIdentifier ReadJson(JsonReader reader, Type objectType, PackIdentifier existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // JsonToken.Null 토큰을 받으면 PackIdentifier.empty를 반환합니다.
            if (reader.TokenType == JsonToken.Null)
                return PackIdentifier.empty;
            
            // JSON 객체의 시작인지 확인
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException($"Expected StartObject token when deserializing PackIdentifier, but got {reader.TokenType}.");
            
            // 객체 내부의 속성을 순회하며 "identifier" 또는 "path"를 찾습니다.
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string propertyName = reader.Value?.ToString() ?? string.Empty;
                    reader.Read(); // 속성 값을 읽기 위해 리더를 다음 토큰으로 이동
                    
                    switch (propertyName)
                    {
                        case nameof(PackIdentifier.identifier):
                            // "identifier" 속성의 값을 Identifier 타입으로 역직렬화하고 PackIdentifier를 생성합니다.
                            // serializer.Deserialize<Identifier>(reader) 호출 시 IdentifierConverter가 사용되며,
                            // Identifier.Parse() 호출 시 InvalidIdentifierException이 발생할 수 있습니다.
                            return PackIdentifier.CreateByID(serializer.Deserialize<Identifier>(reader));
                        case nameof(PackIdentifier.path):
                            // "path" 속성의 값을 FilePath 타입으로 역직렬화하고 PackIdentifier를 생성합니다.
                            // FilePathConverter가 사용될 것입니다.
                            return PackIdentifier.CreateByPath(serializer.Deserialize<FilePath>(reader));
                        default:
                            reader.Skip();
                            break;
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject)
                    break;
            }
            
            return PackIdentifier.empty;
        }
    }
}