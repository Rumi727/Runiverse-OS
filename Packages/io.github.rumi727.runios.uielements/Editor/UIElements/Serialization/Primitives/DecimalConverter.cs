#nullable enable
using System.Globalization;
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization.Primitives
{
    public sealed class DecimalConverter : UxmlAttributeConverter<decimal>
    {
        public override decimal FromString(string value) => decimal.Parse(value);
        public override string ToString(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    }
}