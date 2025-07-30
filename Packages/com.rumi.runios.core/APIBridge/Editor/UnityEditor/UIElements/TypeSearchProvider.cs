#nullable enable
using System;
using RuniOS.Editor.APIBridge.UnityEditor.Search;
using UniSearchProvider = UnityEditor.Search.SearchProvider;

namespace RuniOS.Editor.APIBridge.UnityEditor.UIElements
{
    public class TypeSearchProvider : SearchProvider
    {
        public static new Type type { get; } = EditorAssemblyManager.UnityEditor_UIBuilderModule.GetType("UnityEditor.UIElements.TypeSearchProvider");

        public static TypeSearchProvider CreateInstance(Type baseType) => new TypeSearchProvider((UniSearchProvider)Activator.CreateInstance(type, baseType));

        public static TypeSearchProvider GetInstance(UniSearchProvider instance) => new TypeSearchProvider(instance);

        TypeSearchProvider(UniSearchProvider instance) => this.instance = instance;

        public UniSearchProvider instance { get; }
    }
}
