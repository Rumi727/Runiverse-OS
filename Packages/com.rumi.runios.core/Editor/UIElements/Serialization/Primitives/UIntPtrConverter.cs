#nullable enable
using System.Globalization;
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization.Primitives
{
    public sealed class UIntPtrConverter : UxmlAttributeConverter<UIntPtr>
    {
        public override UIntPtr FromString(string value) => (UIntPtr)ulong.Parse(value);
        public override string ToString(UIntPtr value) => value.ToUInt64().ToString(CultureInfo.InvariantCulture);
    }
}