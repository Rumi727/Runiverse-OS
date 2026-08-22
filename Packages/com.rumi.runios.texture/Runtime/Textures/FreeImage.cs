#nullable enable
using System.Runtime.InteropServices;

namespace RuniOS.Textures
{
    static partial class FreeImage
    {
        const string dllName = "FreeImage";

        public static extern bool isLittleEndian
        {
            [DllImport(dllName, EntryPoint = "FreeImage_IsLittleEndian")]
            get;
        }
    }
}
