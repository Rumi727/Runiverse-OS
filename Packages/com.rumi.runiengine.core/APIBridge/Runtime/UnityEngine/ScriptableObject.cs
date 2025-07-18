#nullable enable
using System;

namespace RuniEngine.APIBridge.UnityEngine
{
    public class ScriptableObject : Object
    {
        public static new Type type { get; } = typeof(global::UnityEngine.ScriptableObject);

        protected ScriptableObject() { }
    }
}
