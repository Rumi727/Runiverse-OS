#nullable enable
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization
{
    public sealed class HexColorConverter : UxmlAttributeConverter<HexColor>
    {
        public override HexColor FromString(string value) => new HexColor(value);
        public override string ToString(HexColor value) => value.ToString();
    }
}