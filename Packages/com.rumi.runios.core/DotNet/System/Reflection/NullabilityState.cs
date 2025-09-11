#nullable enable
// Source : https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Reflection/NullabilityInfo.cs

#pragma warning disable
// ReSharper disable all
namespace System.Reflection
{
    /// <summary>
    /// An enum that represents nullability state
    /// </summary>
#if !RUNI_ENGINE_DOTNET_INTERNAL && !RUNI_ENGINE_DOTNET_INTERNAL_NULLABILITY_STATE 
    public 
#endif
    enum NullabilityState
    {
        /// <summary>
        /// Nullability context not enabled (oblivious)
        /// </summary>
        Unknown,
        /// <summary>
        /// Non nullable value or reference type
        /// </summary>
        NotNull,
        /// <summary>
        /// Nullable value or reference type
        /// </summary>
        Nullable
    }
}