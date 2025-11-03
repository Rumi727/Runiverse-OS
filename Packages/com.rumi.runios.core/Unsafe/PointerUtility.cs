#nullable enable
using System;
using System.Reflection;

namespace RuniOS.Unsafe
{
    public static class PointerUtility
    {
        public static unsafe IntPtr ToIntPtr(Pointer pointer) => (IntPtr)Pointer.Unbox(pointer);
    }
}
