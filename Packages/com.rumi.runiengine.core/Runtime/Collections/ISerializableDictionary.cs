#nullable enable
using System.Collections;
using UnityEngine;

namespace RuniEngine.Collections
{
    public interface ISerializableDictionary : IDictionary, ISerializationCallbackReceiver
    {
        IList serializableKeys { get; }
        IList serializableValues { get; }
    }
}
