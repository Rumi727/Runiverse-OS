#nullable enable
namespace RuniOS.IO;

public sealed class StreamingIOHandler : FileIOHandler
{
    public static StreamingIOHandler instance { get; } = new StreamingIOHandler();

    StreamingIOHandler() : base(Application.streamingAssetsPath) { }
}