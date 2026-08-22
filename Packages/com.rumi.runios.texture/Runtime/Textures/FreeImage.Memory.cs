#nullable enable
using System.Runtime.InteropServices;

namespace RuniOS.Textures
{
    static partial class FreeImage
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Memory : IDisposable
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

            [DllImport(dllName)] static extern Format FreeImage_GetFileTypeFromMemory(Memory memory, int size);

            [DllImport(dllName)] static extern Memory FreeImage_OpenMemory(IntPtr data, uint sizeInBytes);
            [DllImport(dllName)] static extern void FreeImage_CloseMemory(IntPtr memory);
        }
    }
}