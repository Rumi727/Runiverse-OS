#nullable enable
using System.Globalization;
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization.Primitives
{
    public sealed class IntPtrConverter : UxmlAttributeConverter<IntPtr>
    {
        public override IntPtr FromString(string value) => (IntPtr)ulong.Parse(value);
        public override string ToString(IntPtr value) => value.ToInt64().ToString(CultureInfo.InvariantCulture);
    }
}