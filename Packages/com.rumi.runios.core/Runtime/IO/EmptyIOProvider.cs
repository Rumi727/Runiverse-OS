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

        public IWritableIOProvider Recreate(RuniPath path) => this;

        public bool IsSameTarget(IIOProvider other) => ReferenceEquals(this, other);

        public UniTask<bool> DirectoryExists(RuniPath path, CancellationToken cancellationToken = default) => UniTask.FromResult(false);
        public UniTask<bool> FileExists(RuniPath path, CancellationToken cancellationToken = default) => UniTask.FromResult(false);

        public UniTask<IOEntry?> GetEntry(RuniPath path, CancellationToken cancellationToken = default) => UniTask.FromResult<IOEntry?>(null);
        public IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(RuniPath path, bool recursive, CancellationToken cancellationToken = default) => UniTaskAsyncEnumerable.Empty<IOEntry>();

        public UniTask<Stream> OpenRead(RuniPath path, CancellationToken cancellationToken = default) => UniTask.FromResult(Stream.Null);
        public UniTask<Stream> OpenWrite(RuniPath path, CancellationToken cancellationToken = default) => UniTask.FromResult(Stream.Null);

        public UniTask CreateDirectory(RuniPath path, CancellationToken cancellationToken = default) => UniTask.CompletedTask;
        public UniTask<Stream> CreateFile(RuniPath path, CancellationToken cancellationToken = default) => UniTask.FromResult(Stream.Null);

        public UniTask DeleteDirectory(RuniPath path, CancellationToken cancellationToken = default) => UniTask.CompletedTask;
        public UniTask DeleteFile(RuniPath path, CancellationToken cancellationToken = default) => UniTask.CompletedTask;

        void IDisposable.Dispose() { }
    }
}
