#nullable enable
using Newtonsoft.Json;
using RuniOS.Resource;
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization.Resource
{
    public sealed class ResourceKeyConverter : UxmlAttributeConverter<ResourceKey>
    {
        public override ResourceKey FromString(string? value)
        {
            try
            {
                return JsonConvert.DeserializeObject<ResourceKey>(value ?? string.Empty);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return new ResourceKey();
            }
        }
        
        public override string ToString(ResourceKey value) => JsonConvert.SerializeObject(value);
    }
}