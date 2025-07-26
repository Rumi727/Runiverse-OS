namespace RuniEngine
{
    public static class SerializableKeyValuePair
    {
        public const string nameofKey = "key";
        public const string nameofValue = "value";
        
        public static SerializableKeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value) => new SerializableKeyValuePair<TKey, TValue>(key, value);
    }
}