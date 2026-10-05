#nullable enable
#pragma warning disable IDE1006 // 명명 스타일
// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis
{
#pragma warning disable CS9113 // 매개 변수를 읽지 않았습니다.
    [AttributeUsage(AttributeTargets.Parameter)]
    sealed class MaybeNullWhenAttribute(bool ReturnValue) : Attribute;
#pragma warning restore CS9113 // 매개 변수를 읽지 않았습니다.
}
#pragma warning restore IDE1006 // 명명 스타일
