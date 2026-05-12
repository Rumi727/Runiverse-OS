#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace RuniOS.IO
{
    /// <summary>
    /// 아무 데이터도 가지지 않는 빈 I/O 프로바이더입니다.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    sealed class EmptyIOProvider : IWritableIOProvider
    {
        public static EmptyIOProvider instance { get; } = new EmptyIOProvider();

        EmptyIOProvider() { }

        public IOWriteNode rootNode => new IOWriteNode(this);

        public bool isIndependent => true;

        public IWritableIOProvider Recreate(FilePath path) => this;

        public bool IsSameTarget(IIOProvider other) => ReferenceEquals(this, other);

        public UniTask<IOEntry?> GetEntry(FilePath path, CancellationToken cancellationToken = default) => UniTask.FromResult<IOEntry?>(null);
        public IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(FilePath path, bool recursive, CancellationToken cancellationToken = default) => UniTaskAsyncEnumerable.Empty<IOEntry>();

        public UniTask<Stream> OpenRead(FilePath path, CancellationToken cancellationToken = default) => UniTask.FromResult(Stream.Null);
        public UniTask<Stream> OpenWrite(FilePath path, CancellationToken cancellationToken = default) => UniTask.FromResult(Stream.Null);

        public UniTask<Stream> CreateFile(FilePath path, CancellationToken cancellationToken = default) => UniTask.FromResult(Stream.Null);

        public UniTask DeleteDirectory(FilePath path, CancellationToken cancellationToken = default) => UniTask.CompletedTask;
        public UniTask DeleteFile(FilePath path, CancellationToken cancellationToken = default) => UniTask.CompletedTask;

        void IDisposable.Dispose() { }
    }
}
