#nullable enable
using System.Collections;
using UnityEngine;

namespace RuniOS.Collections
{
    public interface ISerializableDictionary : IDictionary, ISerializationCallbackReceiver
    {
        IList pairs { get; }
    }
}