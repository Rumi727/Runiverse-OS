#nullable enable
using System.Collections;

namespace RuniOS.Collections
{
    public interface ISerializableDictionary : IDictionary, ISerializationCallbackReceiver
    {
        IList pairs { get; }
    }
}