#nullable enable
using System;
using RuniOS.Editor.APIBridge.UnityEditor.Search;
using System.Runtime.CompilerServices;

using BridgeTarget = UnityEditor.Search.SearchProvider;

namespace RuniOS.Editor.APIBridge.UnityEditor.UIElements
{
    public class TypeSearchProvider : SearchProvider
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_UIBuilderModule.GetType("UnityEditor.UIElements.TypeSearchProvider");

        public static TypeSearchProvider CreateInstance(Type baseType) => new TypeSearchProvider((BridgeTarget)Activator.CreateInstance(type, baseType));

        static readonly ConditionalWeakTable<BridgeTarget, TypeSearchProvider> cached = new ConditionalWeakTable<BridgeTarget, TypeSearchProvider>();
        public static TypeSearchProvider GetInstance(BridgeTarget instance)
        {
            if (!cached.TryGetValue(instance, out TypeSearchProvider? element))
            {
                element = new TypeSearchProvider(instance);
                cached.Add(instance, element);
            }

            element.instance = instance;
            return element;
        }

        TypeSearchProvider(BridgeTarget instance) => this.instance = instance;

        public BridgeTarget instance { get; set; }
    }
}
