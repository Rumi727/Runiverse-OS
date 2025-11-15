#nullable enable
using RuniOS.Collections.Generic;
using System.Collections;

namespace RuniOS
{
    [Serializable]
    public sealed class ConditionalUniObjectTable<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : Object 
    {
        [SerializeField] SerializableDictionary<TKey, TValue> uniObjects = new();

        public void Add(TKey key, TValue value)
        {
            ExceptionUtility.ThrowIfArgumentNull(key, nameof(key));
            Clean();
            
            uniObjects.Add(key, value);
        }
        
        public void AddOrUpdate(TKey key, TValue value)
        {
            ExceptionUtility.ThrowIfArgumentNull(key, nameof(key));
            Clean();
            
            uniObjects[key] = value;
        }

        public bool TryAdd(TKey key, TValue value)
        {
            ExceptionUtility.ThrowIfArgumentNull(key, nameof(key));
            Clean();
            
            return uniObjects.TryAdd(key, value);
        }
        
        public bool TryGetValue(TKey key, out TValue value)
        {
            ExceptionUtility.ThrowIfArgumentNull(key, nameof(key));
            Clean();
            
            return uniObjects.TryGetValue(key, out value);
        }

        public void Remove(TKey key)
        {
            ExceptionUtility.ThrowIfArgumentNull(key, nameof(key));
            Clean();
            
            uniObjects.Remove(key);
        }

        public TValue GetOrCreateValue(TKey key)
        {
            ExceptionUtility.ThrowIfArgumentNull(key, nameof(key));
            Clean();
            
            if (uniObjects.TryGetValue(key, out TValue value))
                return value;

            return uniObjects[key] = Activator.CreateInstance<TValue>();
        }

        List<TKey> cleanCache = new();
        void Clean()
        {
            foreach (var item in uniObjects.Select(x => x.Key).Where(x => !x))
                cleanCache.Add(item);

            for (int i = 0; i < cleanCache.Count; i++)
                uniObjects.Remove(cleanCache[i]);

            cleanCache.Clear();
        }
        
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => uniObjects.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}