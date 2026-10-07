namespace RuniOS.Collections.Generic
{
    /// <summary>
    /// <see cref="SerializableKeyValuePair{TKey, TValue}"/> 구조체와 관련된 유틸리티 메서드 및 상수를 제공하는 정적 클래스입니다.
    /// </summary>
    public static class SerializableKeyValuePair
    {
        /// <summary>
        /// <see cref="SerializableKeyValuePair{TKey, TValue}"/> 구조체의 키 필드 이름입니다.
        /// <br/>
        /// 리플렉션 접근 시 사용될 수 있습니다.
        /// </summary>
        public const string nameOfKey = "Key";
        
        /// <summary>
        /// <see cref="SerializableKeyValuePair{TKey, TValue}"/> 구조체의 값 필드 이름입니다.
        /// <br/>
        /// 리플렉션 접근 시 사용될 수 있습니다.
        /// </summary>
        public const string nameOfValue = "Value";
        
        /// <summary>
        /// <see cref="SerializableKeyValuePair{TKey, TValue}"/> 구조체의 내부 키 필드 이름입니다.
        /// <br/>
        /// 유니티 직렬화 및 리플렉션 접근 시 사용될 수 있습니다.
        /// </summary>
        public const string nameOfInternalKey = "key";
        
        /// <summary>
        /// <see cref="SerializableKeyValuePair{TKey, TValue}"/> 구조체의 내부 값 필드 이름입니다.
        /// <br/>
        /// 유니티 직렬화 및 리플렉션 접근 시 사용될 수 있습니다.
        /// </summary>
        public const string nameOfInternalValue = "value";
        
        /// <summary>
        /// 지정된 키와 값을 사용하여 <see cref="SerializableKeyValuePair{TKey, TValue}"/> 구조체의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <typeparam name="TKey">키의 타입입니다.</typeparam>
        /// <typeparam name="TValue">값의 타입입니다.</typeparam>
        /// <param name="key">키 값입니다.</param>
        /// <param name="value">값입니다.</param>
        /// <returns>생성된 <see cref="SerializableKeyValuePair{TKey, TValue}"/> 인스턴스입니다.</returns>
        public static SerializableKeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value) => new SerializableKeyValuePair<TKey, TValue>(key, value);
        
        /// <summary>
        /// 지정된 키-값 쌍 타입의 기본 (<see cref="ISerializableKeyValuePair{TKey, TValue}"/>의 내부) 타입을 가져옵니다.
        /// </summary>
        /// <param name="type">기본 타입을 가져올 키-값 쌍 타입입니다 (예: <c>SerializableKeyValuePair&lt;string, int&gt;</c>).</param>
        /// <returns>
        /// <paramref name="type"/>이 <see cref="ISerializableKeyValuePair{TKey, TValue}"/>의 인스턴스이면 해당 T 타입이고,
        /// 그렇지 않으면 <see langword="null"/>입니다.
        /// </returns>
        public static (Type? key, Type? value) GetUnderlyingType(Type type)
        {
            // 제네릭 타입이고 제네릭 정의가 아닌 경우에만 처리합니다.
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                // ISerializableNullable<>의 제네릭 정의에 할당 가능한지 확인합니다.
                if (type.IsAssignableToGenericDefinition(typeof(ISerializableKeyValuePair<,>), out Type? resolvedType))
                {
                    Type key = resolvedType.GetGenericArguments()[0];
                    Type value = resolvedType.GetGenericArguments()[1];
                    
                    return (key, value);
                }
            }

            return default;
        }
    }
}