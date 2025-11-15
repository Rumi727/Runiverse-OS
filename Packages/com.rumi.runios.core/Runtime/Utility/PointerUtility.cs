#nullable enable
using System.Reflection;

namespace RuniOS.Utility
{
    public static class PointerUtility
    {
        public static IntPtr ToIntPtr(this Pointer pointer) => Unsafe.PointerUtility.ToIntPtr(pointer);
    }
}