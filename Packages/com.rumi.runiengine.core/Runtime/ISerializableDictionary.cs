#nullable enable
using System.Collections;
using UnityEngine;

namespace RuniEngine
{
    public interface ISerializableDictionary : IDictionary, ISerializationCallbackReceiver
    {
        IList serializableKeys { get; }
        IList serializableValues { get; }
    }
}
