#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.IO;
using System.Threading;

namespace RuniOS.IO
{
    /// <summary>
    /// Exposes several read-only I/O providers as one provider, using provider order as priority.<br/>
    /// 여러 읽기 전용 I/O 프로바이더를 하나의 프로바이더처럼 노출하며, 프로바이더 순서를 우선순위로 사용합니다.
    /// </summary>
    public sealed class GroupIOProvider : IIOProvider
    {
        /// <summary>
        /// Initializes a new <see cref="GroupIOProvider"/> instance from the specified providers.<br/>
        /// 지정된 프로바이더들로 새 <see cref="GroupIOProvider"/> 인스턴스를 초기화합니다.
        /// </summary>
        public GroupIOProvider(params IIOProvider[] providers) : this(providers, false) { }

        /// <summary>
        /// Initializes a new <see cref="GroupIOProvider"/> instance from the specified providers.<br/>
        /// 지정된 프로바이더들로 새 <see cref="GroupIOProvider"/> 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="providers">
        /// The providers to expose as one provider. Earlier providers have higher priority.<br/>
        /// 하나의 프로바이더처럼 노출할 프로바이더입니다. 앞에 있는 프로바이더가 더 높은 우선순위를 가집니다.
        /// </param>
        /// <param name="leaveOpen">
        /// <see langword="true"/> to keep child providers open when this provider is disposed; otherwise, <see langword="false"/>.<br/>
        /// 이 프로바이더가 해제될 때 하위 프로바이더를 열어 둘 경우 <see langword="true"/>, 함께 해제할 경우 <see langword="false"/>입니다.
        /// </param>
        public GroupIOProvider(IEnumerable<IIOProvider> providers, bool leaveOpen = false)
        {
            this.providers = providers.ToArray();
            this.leaveOpen = leaveOpen;
        }

        /// <summary>
        /// Gets the grouped providers in priority order.<br/>
        /// 우선순위 순서대로 그룹화된 프로바이더를 가져옵니다.
        /// </summary>
        public IReadOnlyList<IIOProvider> providers { get; }

        readonly bool leaveOpen;

        /// <inheritdoc/>
        public bool isIndependent => providers.All(x => x.isIndependent);

        /// <inheritdoc/>
        public bool IsSameTarget(IIOProvider other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other is not GroupIOProvider otherGroup || providers.Count != otherGroup.providers.Count)
                return false;

            // ReSharper disable once LoopCanBeConvertedToQuery
            for (int i = 0; i < providers.Count; i++)
            {
                if (!providers[i].IsSameTarget(otherGroup.providers[i]))
                    return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public async UniTask<IOEntry?> GetEntry(RuniPath path, CancellationToken cancellationToken = default)
        {
            foreach (IIOProvider provider in providers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                IOEntry? entry = await provider.GetEntry(path, cancellationToken);
                if (entry.HasValue)
                    return entry;
            }

            return null;
        }

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(RuniPath path, bool recursive, CancellationToken cancellationToken = default) => UniTaskAsyncEnumerable.Create<IOEntry>(async (writer, iterationToken) =>
        {
            using var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, iterationToken);
            CancellationToken ct = linkedCTS.Token;

            HashSet<RuniPath> yieldedPaths = [];
            foreach (IIOProvider provider in providers)
            {
                ct.ThrowIfCancellationRequested();

                await foreach (IOEntry entry in provider.EnumerateEntries(path, recursive, ct))
                {
                    ct.ThrowIfCancellationRequested();

                    if (!yieldedPaths.Add(entry.path))
                        continue;

                    await writer.YieldAsync(entry);
                }
            }
        });

        /// <inheritdoc/>
        public async UniTask<Stream> OpenRead(RuniPath path, CancellationToken cancellationToken = default)
        {
            IIOProvider? provider = await FindReadProvider(path, cancellationToken);
            if (provider == null)
                throw new FileNotFoundException($"Could not find file '{path}'.", path.value);

            return await provider.OpenRead(path, cancellationToken);
        }

        /// <inheritdoc/>
        public async UniTask<byte[]> ReadAllBytes(RuniPath path, CancellationToken cancellationToken = default)
        {
            IIOProvider? provider = await FindReadProvider(path, cancellationToken);
            if (provider == null)
                throw new FileNotFoundException($"Could not find file '{path}'.", path.value);

            return await provider.ReadAllBytes(path, cancellationToken);
        }

        /// <inheritdoc/>
        public async UniTask<string> ReadAllText(RuniPath path, CancellationToken cancellationToken = default)
        {
            IIOProvider? provider = await FindReadProvider(path, cancellationToken);
            if (provider == null)
                throw new FileNotFoundException($"Could not find file '{path}'.", path.value);

            return await provider.ReadAllText(path, cancellationToken);
        }

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<string> ReadLines(RuniPath path, CancellationToken cancellationToken = default) => UniTaskAsyncEnumerable.Create<string>(async (writer, iterationToken) =>
        {
            using var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, iterationToken);
            CancellationToken ct = linkedCTS.Token;

            IIOProvider? provider = await FindReadProvider(path, ct);
            if (provider == null)
                throw new FileNotFoundException($"Could not find file '{path}'.", path.value);

            await foreach (string line in provider.ReadLines(path, ct))
            {
                ct.ThrowIfCancellationRequested();
                await writer.YieldAsync(line);
            }
        });

        async UniTask<IIOProvider?> FindReadProvider(RuniPath path, CancellationToken cancellationToken)
        {
            foreach (IIOProvider provider in providers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                IOEntry? entry = await provider.GetEntry(path, cancellationToken);
                if (!entry.HasValue)
                    continue;

                if (entry.Value.isDirectory)
                    throw new IOException($"The path '{path}' is a directory.");

                return provider;
            }

            return null;
        }

        public void Dispose()
        {
            if (leaveOpen)
                return;

            foreach (IIOProvider provider in providers)
                provider.Dispose();
        }
    }
}
