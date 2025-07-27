namespace RuniEngine
{
    /// <summary>
    /// <see cref="SerializableKeyValuePair{TKey, TValue}"/> 구조체와 관련된 유틸리티 메서드 및 상수를 제공하는 정적 클래스입니다.
    /// </summary>
    public static class SerializableKeyValuePair
    {
        /// <summary>
        /// <see cref="SerializableKeyValuePair{TKey, TValue}"/> 구조체의 키 필드 이름입니다.
        /// <br/>
        /// 유니티 직렬화 및 리플렉션 접근 시 사용될 수 있습니다.
        /// </summary>
        public const string nameofKey = "key";
        /// <summary>
        /// <see cref="SerializableKeyValuePair{TKey, TValue}"/> 구조체의 값 필드 이름입니다.
        /// <br/>
        /// 유니티 직렬화 및 리플렉션 접근 시 사용될 수 있습니다.
        /// </summary>
        public const string nameofValue = "value";
        
        /// <summary>
        /// 지정된 키와 값을 사용하여 <see cref="SerializableKeyValuePair{TKey, TValue}"/> 구조체의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <typeparam name="TKey">키의 타입입니다.</typeparam>
        /// <typeparam name="TValue">값의 타입입니다.</typeparam>
        /// <param name="key">키 값입니다.</param>
        /// <param name="value">값입니다.</param>
        /// <returns>생성된 <see cref="SerializableKeyValuePair{TKey, TValue}"/> 인스턴스입니다.</returns>
        public static SerializableKeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value) => new SerializableKeyValuePair<TKey, TValue>(key, value);
    }
}