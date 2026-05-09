using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System.IO;
using System.Threading;

namespace RuniOS.IONew
{
    public sealed class StreamingIOProvider : IIOProvider
    {
        public static StreamingIOProvider instance { get; } = new StreamingIOProvider();

        StreamingIOProvider()
        {
#if UNITY_ANDROID
            provider = new AndroidStreamingIOProvider();
#else
            provider = new PhysicalIOProvider(Application.streamingAssetsPath);
#endif
        }

        public IIOProvider provider { get; }

        /// <inheritdoc/>
        public bool isIndependent => provider.isIndependent;

        /// <inheritdoc/>
        public UniTask<IOEntry?> GetEntry(FilePath path, CancellationToken cancellationToken = default) => provider.GetEntry(path, cancellationToken);

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(FilePath path, bool recursive, CancellationToken cancellationToken = default) => provider.EnumerateEntries(path, recursive, cancellationToken);

        /// <inheritdoc/>
        public UniTask<Stream> OpenRead(FilePath path, CancellationToken cancellationToken = default) => provider.OpenRead(path, cancellationToken);

        /// <inheritdoc/>
        public void Dispose() => provider.Dispose();
    }
}