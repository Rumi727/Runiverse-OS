#nullable enable
using System;

namespace RuniEngine.APIBridge.UnityEngine
{
    public class Object
    {
        public static Type type { get; } = typeof(global::UnityEngine.Object);

        protected Object() { }
    }
}
