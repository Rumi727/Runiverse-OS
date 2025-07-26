#nullable enable
using System.Collections;

namespace RuniEngine.Collections
{
    public interface ISerializableDictionary : IDictionary
    {
        IList pairs { get; }
    }
}
