#nullable enable
using RuniOS.Resource;
using UnityEditor.UIElements;

namespace RuniOS.Editor.UIElements.Serialization.Resource;

public sealed class IdentifierConverter : UxmlAttributeConverter<Identifier>
{
    public override Identifier FromString(string value)
    {
        try
        {
            return value;
        }
        catch (InvalidIdentifierException e)
        {
            Debug.LogException(e);
        }

        return Identifier.empty;
    }
        
    public override string ToString(Identifier value) => value;
}