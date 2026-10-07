#nullable enable

namespace RuniOS.Textures
{
    static partial class FreeImage
    {
#if UNITY_EDITOR_LINUX
        // Unity Editor already loads its own FreeImage on Linux. Bind to the
        // symbols in the current process instead of loading the package plugin;
        // two libraries exporting the same FreeImage_* symbols can interpose
        // calls and make one library free the other's FIBITMAP metadata.
        const string dllName = "__Internal";
#else
        const string dllName = "FreeImage";
#endif
    }
}
