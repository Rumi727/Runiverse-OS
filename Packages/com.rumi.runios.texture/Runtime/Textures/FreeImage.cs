#nullable enable
using System.Runtime.InteropServices;

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

        public static extern bool isLittleEndian
        {
            [DllImport(dllName, EntryPoint = "FreeImage_IsLittleEndian")]
            get;
        }
    }
}
