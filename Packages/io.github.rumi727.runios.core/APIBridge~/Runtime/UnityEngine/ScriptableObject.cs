#nullable enable
using System;

namespace RuniOS.APIBridge.UnityEngine
{
    public class ScriptableObject : Object
    {
        public static new Type type { get; } = typeof(global::UnityEngine.ScriptableObject);

        protected ScriptableObject() { }
    }
}
