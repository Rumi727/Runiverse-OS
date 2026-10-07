#nullable enable
using System.Runtime.InteropServices;

namespace RuniOS.Textures
{
    static partial class FreeImage
    {
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Bitmap : IDisposable
        {
            public static Bitmap LoadFromMemory(Format format, Memory memory, LoadFlags flags) => FreeImage_LoadFromMemory(format, memory, flags);

            readonly IntPtr handle;

            public bool isInvalid => handle == IntPtr.Zero;

            public ColorType colorType => FreeImage_GetColorType(this);
            public ImageType imageType => FreeImage_GetImageType(this);

            public int transparencyCount => FreeImage_GetTransparencyCount(this);

            public uint usedColors => FreeImage_GetColorsUsed(this);
            public IntPtr palette => FreeImage_GetPalette(this); // 안전하게 Span 반환하고 싶은데 길이랑 타입을 어떻게 해야하지?

            public bool isTransparent => FreeImage_IsTransparent(this);

            public uint redMask => FreeImage_GetRedMask(this);
            public uint greenMask => FreeImage_GetGreenMask(this);
            public uint blueMask => FreeImage_GetBlueMask(this);

            public uint width => FreeImage_GetWidth(this);
            public uint height => FreeImage_GetHeight(this);

            public int bitsPerPixel => FreeImage_GetBPP(this);
            public IntPtr bits => FreeImage_GetBits(this); // 안전하게 Span 반환하고 싶은데 길이랑 타입을 어떻게 해야하지?

            public int pitch => FreeImage_GetPitch(this);

            public Bitmap ConvertTo24Bits() => FreeImage_ConvertTo24Bits(this);
            public Bitmap ConvertTo32Bits() => FreeImage_ConvertTo32Bits(this);

            public void Dispose() => FreeImage_Unload(handle);

            [DllImport(dllName)] static extern ImageType FreeImage_GetImageType(Bitmap bitmap);
            [DllImport(dllName)] static extern ColorType FreeImage_GetColorType(Bitmap bitmap);

            [DllImport(dllName)] static extern int FreeImage_GetTransparencyCount(Bitmap bitmap);

            [DllImport(dllName)] static extern uint FreeImage_GetColorsUsed(Bitmap bitmap);
            [DllImport(dllName)] static extern IntPtr FreeImage_GetPalette(Bitmap bitmap);

            [DllImport(dllName)] static extern bool FreeImage_IsTransparent(Bitmap bitmap);

            [DllImport(dllName)] static extern uint FreeImage_GetRedMask(Bitmap bitmap);
            [DllImport(dllName)] static extern uint FreeImage_GetGreenMask(Bitmap bitmap);
            [DllImport(dllName)] static extern uint FreeImage_GetBlueMask(Bitmap bitmap);

            [DllImport(dllName)] static extern uint FreeImage_GetWidth(Bitmap bitmap);
            [DllImport(dllName)] static extern uint FreeImage_GetHeight(Bitmap bitmap);

            [DllImport(dllName)] static extern int FreeImage_GetBPP(Bitmap bitmap);
            [DllImport(dllName)] static extern IntPtr FreeImage_GetBits(Bitmap bitmap);

            [DllImport(dllName)] static extern int FreeImage_GetPitch(Bitmap bitmap);

            [DllImport(dllName)] static extern Bitmap FreeImage_ConvertTo24Bits(Bitmap bitmap);
            [DllImport(dllName)] static extern Bitmap FreeImage_ConvertTo32Bits(Bitmap bitmap);

            [DllImport(dllName)] static extern Bitmap FreeImage_LoadFromMemory(Format format, Memory memory, LoadFlags flags);
            [DllImport(dllName)] static extern void FreeImage_Unload(IntPtr bitmap);
        }
    }
}
