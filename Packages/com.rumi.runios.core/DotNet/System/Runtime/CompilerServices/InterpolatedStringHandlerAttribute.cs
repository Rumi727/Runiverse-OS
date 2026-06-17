#nullable enable
// Source : https://source.dot.net/#System.Private.CoreLib/src/runtime/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/InterpolatedStringHandlerAttribute.cs
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>Indicates the attributed type is to be used as an interpolated string handler.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
#if !RUNI_ENGINE_DOTNET_INTERNAL && !RUNI_ENGINE_DOTNET_INTERNAL_IS_EXTERNAL_INIT
    public
#endif
        sealed class InterpolatedStringHandlerAttribute : Attribute
    {
        /// <summary>Initializes the <see cref="InterpolatedStringHandlerAttribute"/>.</summary>
        public InterpolatedStringHandlerAttribute() { }
    }
}