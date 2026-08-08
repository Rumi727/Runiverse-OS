#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RuniOS.IO;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Resource
{
    public sealed class AssetImportData(IONode node, FileMetaData? metaData = null) : IReadOnlyDictionary<Identifier, JObject>
    {
        public static readonly AssetImportData empty = new AssetImportData();

        public AssetImportData() : this(IONode.empty) { }

        public IONode node { get; } = node;
        public FileMetaData? metaData { get; private set; } = metaData;

        public int count => value.Count;
        int IReadOnlyCollection<KeyValuePair<Identifier, JObject>>.Count => count;

        public JObject? this[Identifier key] => value.GetValueOrDefault(key);
        JObject IReadOnlyDictionary<Identifier, JObject>.this[Identifier key] => value[key];

        public Dictionary<Identifier, JObject>.KeyCollection keys => value.Keys;
        IEnumerable<Identifier> IReadOnlyDictionary<Identifier, JObject>.Keys => keys;

        public Dictionary<Identifier, JObject>.ValueCollection values => value.Values;
        IEnumerable<JObject> IReadOnlyDictionary<Identifier, JObject>.Values => values;

        Dictionary<Identifier, JObject> value = [];

        public T? GetValue<T>(Identifier key)
        {
            if (value.TryGetValue(key, out JObject jObject))
                return jObject.ToObject<T>();

            return default;
        }

        public bool ContainsKey(Identifier key) => value.ContainsKey(key);

        public bool TryGetValue(Identifier key, out JObject value) => this.value.TryGetValue(key, out value);
        public bool TryGetValue<T>(Identifier key, [NotNullWhen(true)] out T? value)
        {
            if (this.value.TryGetValue(key, out JObject jObject))
            {
                value = jObject.ToObject<T>();
                return value != null;
            }

            value = default;
            return false;
        }

        public async UniTask Reload()
        {
            if (await node.file.GetEntry() is not { } entry)
            {
                value = [];
                metaData = null;

                return;
            }

            try
            {
                string text = await node.file.ReadAllText();
                value = JsonConvert.DeserializeObject<Dictionary<Identifier, JObject>>(text) ?? [];
                metaData = entry.metaData;
            }
            catch (Exception e)
            {
                value = [];
                metaData = null;

                Debug.LogError($"Failed to load import settings at path {entry.path}! The exception is: {e}");
            }
        }

        public bool IsSameTarget(AssetImportData other) => node.IsSameTarget(other.node) && metaData == other.metaData;

        public IEnumerator<KeyValuePair<Identifier, JObject>> GetEnumerator() => value.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}