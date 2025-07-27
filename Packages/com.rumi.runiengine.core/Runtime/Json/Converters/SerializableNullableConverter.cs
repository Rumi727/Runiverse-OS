#nullable enable
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace RuniEngine.Json.Converters
{
    /// <summary>
    /// <see cref="ISerializableNullable{T}"/> 인터페이스를 구현하는 구조체를
    /// 표준 C# <see cref="Nullable{T}"/> (<see langword="struct T?"/>)와 유사하게 직렬화하고 역직렬화하는 <see cref="JsonConverter"/>입니다.<br/>
    /// 이 컨버터는 <see cref="ISerializableNullable{T}.HasValue"/>에 따라 내부 값 또는 JSON <see langword="null"/>로 처리합니다.
    /// </summary>
    public class SerializableNullableConverter : JsonConverter
    {
        /// <summary>
        /// 지정된 <see cref="Type"/>이 이 컨버터에 의해 변환될 수 있는지 여부를 결정합니다.<br/>
        /// <see cref="ISerializableNullable{T}"/>의 제네릭 정의에 할당 가능한 타입만 변환을 허용합니다.
        /// </summary>
        /// <param name="objectType">변환을 확인할 <see cref="Type"/>입니다.</param>
        /// <returns>지정된 <see cref="Type"/>이 <see cref="ISerializableNullable{T}"/>를 구현하는 경우 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        public override bool CanConvert(Type objectType)
        {
            // SerializableNullable.GetUnderlyingType 메서드는 T?도 함께 확인하므로 직접 사용하지 않습니다.
            // 호환성과 확장성을 위해 관련 없는 타입은 무시하는 것이 좋습니다.
            // objectType이 제네릭 타입이고 ISerializableNullable<>의 제네릭 정의에 할당 가능한지 확인합니다.
            return objectType.IsGenericType && objectType.IsAssignableToGenericDefinition(typeof(ISerializableNullable<>));
        }
        
        /// <summary>
        /// <see cref="ISerializableNullable{T}"/> 객체를 JSON으로 직렬화합니다.
        /// <br/>
        /// <see cref="ISerializableNullable{T}.HasValue"/>가 <see langword="true"/>이면 내부 값을 직렬화하고,
        /// 그렇지 않으면 JSON <see langword="null"/>을 직렬화합니다.
        /// </summary>
        /// <param name="writer">JSON 작성을 위한 <see cref="JsonWriter"/> 객체입니다.</param>
        /// <param name="value">직렬화할 <see cref="ISerializableNullable{T}"/> 값입니다.</param>
        /// <param name="serializer">직렬화 프로세스를 위한 <see cref="JsonSerializer"/> 객체입니다.</param>
        /// <exception cref="JsonSerializationException">직렬화할 객체가 예상된 <see cref="ISerializableNullable{T}"/> 형식이 아니거나, 필요한 속성("value", "hasValue")을 찾을 수 없는 경우 발생합니다.</exception>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            // 값이 null이거나 ISerializableNullable<>을 구현하지 않은 경우 JSON null을 기록합니다.
            if (value == null || !value.GetType().IsAssignableToGenericDefinition(typeof(ISerializableNullable<>), out Type? nullableType))
            {
                writer.WriteNull();
                return;
            }

            // HasValue 속성을 리플렉션을 통해 가져와 값을 확인합니다.
            bool hasValue = (bool)(nullableType.GetProperty(SerializableNullable.nameofHasValue, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new JsonSerializationException($"'{nullableType.FullName}' type does not contain a readable '{SerializableNullable.nameofHasValue}' property for serialization."))
                .GetValue(value);
            
            if (hasValue)
            {
                object innerValue = nullableType.GetProperty(SerializableNullable.nameofValue, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(value)
                    ?? throw new JsonSerializationException($"'{nullableType.FullName}' type does not contain a readable '{SerializableNullable.nameofValue}' property for serialization.");
                
                serializer.Serialize(writer, innerValue);
            }
            else
                writer.WriteNull(); // 값이 없으면 JSON null 기록
        }

        /// <summary>
        /// JSON을 <see cref="ISerializableNullable{T}"/> 객체로 역직렬화합니다.
        /// <br/>
        /// JSON 토큰이 <see langword="null"/>이면 해당 타입의 기본값(<see cref="ISerializableNullable{T}.HasValue"/>가 <see langword="false"/>인 인스턴스)을 반환합니다.
        /// <br/>
        /// 그렇지 않으면 JSON 값을 읽어 <see cref="ISerializableNullable{T}"/>의 내부 타입으로 역직렬화한 후,
        /// 해당 값으로 <see cref="ISerializableNullable{T}"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="reader">JSON 읽기를 위한 <see cref="JsonReader"/> 객체입니다.</param>
        /// <param name="objectType">역직렬화할 객체의 <see cref="Type"/>입니다. 이는 <see cref="ISerializableNullable{T}"/>를 구현하는 타입이어야 합니다.</param>
        /// <param name="existingValue">기존에 존재하는 값입니다.</param>
        /// <param name="serializer">역직렬화 프로세스를 위한 <see cref="JsonSerializer"/> 객체입니다.</param>
        /// <returns>역직렬화된 <see cref="ISerializableNullable{T}"/> 객체입니다.</returns>
        /// <exception cref="JsonSerializationException">
        /// <paramref name="objectType"/>이 <see cref="ISerializableNullable{T}"/>의 유효한 구현체가 아니거나,
        /// 내부 타입이 유효하지 않은 경우 발생합니다.
        /// </exception>
        /// <exception cref="MissingMethodException">
        /// <see cref="ISerializableNullable{T}"/> 구현체에 내부 값을 인자로 받는 적절한 생성자를 찾을 수 없는 경우 발생합니다.
        /// </exception>
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            // JSON 토큰이 null이면 해당 타입의 기본 인스턴스를 생성하여 반환합니다 (HasValue = false).
            if (reader.TokenType == JsonToken.Null)
                return Activator.CreateInstance(objectType);

            // ISerializableNullable<T>에서 T의 실제 타입을 가져옵니다.
            Type? innerType = SerializableNullable.GetUnderlyingType(objectType);
            if (innerType == null)
                throw new JsonSerializationException($"Could not determine underlying type for ISerializableNullable from '{objectType.FullName}'.");
            
            // 리더의 현재 위치에서 내부 값을 역직렬화합니다.
            object? innerValue = serializer.Deserialize(reader, innerType);

            // 역직렬화된 내부 값을 사용하여 ISerializableNullable<T> 인스턴스를 생성합니다.
            // 이 생성자는 (T value) 또는 (T? value) 형태의 생성자일 것입니다.
            return Activator.CreateInstance(objectType, innerValue);
        }
    }
}