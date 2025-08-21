#nullable enable
using RuniOS.Resource;
using UnityEditor.UIElements;
using UnityEngine;

namespace RuniOS.Editor.UIElements.Serialization
{
    public sealed class PackIdentifierConverter : UxmlAttributeConverter<PackIdentifier>
    {
        public override PackIdentifier FromString(string value) => JsonUtility.FromJson<PackIdentifier>(value);
        public override string ToString(PackIdentifier value) => JsonUtility.ToJson(value);
    }
}
