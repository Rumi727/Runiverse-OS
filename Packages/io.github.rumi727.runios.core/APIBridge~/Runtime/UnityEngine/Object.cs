#nullable enable
using System;

namespace RuniOS.APIBridge.UnityEngine
{
    public class Object
    {
        public static Type type { get; } = typeof(global::UnityEngine.Object);

        protected Object() { }
    }
}
