#nullable enable
using System;
using System.Reflection;

namespace RuniOS
{
    public static class PointerUtility
    {
        public static IntPtr ToIntPtr(this Pointer pointer) => Unsafe.PointerUtility.ToIntPtr(pointer);
    }
}
