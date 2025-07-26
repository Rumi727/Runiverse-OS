#nullable enable
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // 명명 스타일
namespace RuniEngine
{
    public interface ISerializableKeyValuePair
    {
        object? Key { get; set; }
        object? Value { get; set; }
    }
}
#pragma warning restore IDE1006 // 명명 스타일
