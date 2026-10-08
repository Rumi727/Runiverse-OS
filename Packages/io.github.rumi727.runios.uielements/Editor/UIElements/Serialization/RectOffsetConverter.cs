#nullable enable
using RuniOS.Spans;
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization
{
    public sealed class RectOffsetConverter : UxmlAttributeConverter<RectOffset>
    {
        public override RectOffset FromString(string value)
        {
            RectOffset result = new RectOffset();

            int index = 0;
            foreach (var splittedValue in value.AsSpan().Split(','))
            {
                if (float.TryParse(splittedValue, out float parseResult))
                {
                    switch (index)
                    {
                        case 0:
                            result.left = parseResult;
                            break;
                        case 1:
                            result.right = parseResult;
                            break;
                        case 2:
                            result.top = parseResult;
                            break;
                        case 3:
                            result.bottom = parseResult;
                            break;
                    }
                }

                index++;
            }
            
            return result;
        }

        public override string ToString(RectOffset value) => string.Join(',', value.left, value.right, value.top, value.bottom);
    }
}