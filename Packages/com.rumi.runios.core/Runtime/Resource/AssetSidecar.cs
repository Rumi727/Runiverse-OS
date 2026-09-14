#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RuniOS.IO;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Resource
{
    public sealed class AssetSidecar(IONode node, FileMetaData metaData = default) : IEnumerable<Identifier>
    {
        public static readonly AssetSidecar empty = new AssetSidecar(IONode.empty);

        public IONode node { get; } = node;
        public FileMetaData metaData { get; private set; } = metaData;

        public int count => value.Count;

        public Dictionary<Identifier, JObject>.KeyCollection keys => value.Keys;

        Dictionary<Identifier, JObject> value = [];

        public T? GetValue<T>(Identifier key) where T : struct
        {
            if (TryGetValue(key, out T value))
                return value;

            return null;
        }

        public JObject? GetValue(Identifier key)
        {
            if (TryGetValue(key, out JObject? value))
                return value;

            return null;
        }

        public bool ContainsKey(Identifier key) => value.ContainsKey(key);

        public bool TryGetValue<T>(Identifier key, [NotNullWhen(true)] out T? value)
        {
            value = default;

            if (!this.value.TryGetValue(key, out JObject jObject))
                return false;

            try
            {
                value = jObject.ToObject<T>();
                return value != null;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        public async UniTask Reload()
        {
            if (await node.file.GetEntry() is not { } entry)
            {
                value = [];
                metaData = default;

                return;
            }

            metaData = entry.metaData;

            try
            {
                string text = await node.file.ReadAllText();
                value = JsonConvert.DeserializeObject<Dictionary<Identifier, JObject>>(text) ?? [];
            }
            catch (Exception e)
            {
                value = [];
                Debug.LogError($"Failed to load import settings at path {entry.path}! The exception is: {e}");
            }
        }

        public bool IsSameTarget(AssetSidecar other) => node.IsSameTarget(other.node) && metaData.IsSameRevision(other.metaData);

        public Dictionary<Identifier, JObject>.KeyCollection.Enumerator GetEnumerator() => keys.GetEnumerator();
        IEnumerator<Identifier> IEnumerable<Identifier>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}