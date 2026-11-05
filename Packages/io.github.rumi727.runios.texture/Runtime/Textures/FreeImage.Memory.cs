#nullable enable
using System.Runtime.InteropServices;

namespace RuniOS.Textures
{
    static partial class FreeImage
    {
        [StructLayout(LayoutKind.Sequential)]
        public partial struct Memory : IDisposable
        {
            public static Memory Open(IntPtr data, uint sizeInBytes) => FreeImage_OpenMemory(data, sizeInBytes);

            IntPtr handle;

            public readonly bool isInvalid => handle == IntPtr.Zero;

            public Format GetFileType(int size) => FreeImage_GetFileTypeFromMemory(this, size);

            public void Dispose()
            {
                if (handle == IntPtr.Zero)
                    return;

                FreeImage_CloseMemory(handle);
                handle = IntPtr.Zero;
            }

            [LibraryImport(dllName)] private static partial Format FreeImage_GetFileTypeFromMemory(Memory memory, int size);

            [LibraryImport(dllName)] private static partial Memory FreeImage_OpenMemory(IntPtr data, uint sizeInBytes);
            [LibraryImport(dllName)] private static partial void FreeImage_CloseMemory(IntPtr memory);
        }
    }
}