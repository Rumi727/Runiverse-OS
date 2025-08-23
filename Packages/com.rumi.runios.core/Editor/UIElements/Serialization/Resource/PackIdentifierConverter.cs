#nullable enable
using Newtonsoft.Json;
using RuniOS.Resource;
using System;
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization.Resource
{
    public sealed class PackIdentifierConverter : UxmlAttributeConverter<PackIdentifier>
    {
        public override PackIdentifier FromString(string? value)
        {
            try
            {
                return JsonConvert.DeserializeObject<PackIdentifier>(value ?? string.Empty);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return PackIdentifier.empty;
            }
        }
        
        public override string ToString(PackIdentifier value) => JsonConvert.SerializeObject(value);
    }
}
