#nullable enable
using RuniOS.Resource;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Editor
{
    public static partial class EditorResourcePack
    {
        public static ResourcePack? pack { get; private set; }

        [OnAssemblyLoaded]
        static void OnAssemblyLoaded() => pack = ResourcePack.Create("runios:editor", EditorStreamingAssetsService.editorProvider, RequiredPackSort.BeforeVanilla);

        [OnAssemblyUnloading]
        static void OnAssemblyUnloading() => pack?.Dispose();
    }
}