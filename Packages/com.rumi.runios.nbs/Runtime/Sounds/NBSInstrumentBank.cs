#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.NBS;
using RuniOS.Resource;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Owns the unique instrument scopes required by one NBS Player generation.<br/>
    /// NBS Player 세대 하나에 필요한 고유 악기 스코프를 소유합니다.
    /// </summary>
    public sealed class NBSInstrumentBank : IDisposable, INBSClipMetadataProvider
    {
        readonly Dictionary<NBSInstrumentReference, IAssetScope<WaveAudioClip>> scopes = [];
        readonly object lifetimeLock = new object();
        int activeUsers;
        bool disposeRequested;
        bool isDisposed;

        NBSInstrumentBank() { }

        /// <summary>
        /// Loads every unique audio instrument used by <paramref name="playbackMap"/>.<br/>
        /// <paramref name="playbackMap"/>에서 사용하는 모든 고유 오디오 악기를 로드합니다.
        /// </summary>
        /// <param name="playbackMap">The clip-independent playback map.<br/>클립 독립적 재생 맵입니다.</param>
        /// <param name="nbsAssetId">The owning NBS asset identifier used for custom instruments.<br/>커스텀 악기에 사용할 소유 NBS 에셋 식별자입니다.</param>
        /// <returns>When loading completes, returns the instrument bank.<br/>로드가 완료되면 악기 bank를 반환합니다.</returns>
        public static async UniTask<NBSInstrumentBank> Create(NBSPlaybackMap playbackMap, Identifier nbsAssetId)
        {
            if (playbackMap == null)
                throw new ArgumentNullException(nameof(playbackMap));

            NBSInstrumentBank bank = new NBSInstrumentBank();
            HashSet<NBSInstrumentReference> instruments = [];
            for (int i = 0; i < playbackMap.entries.Count; i++)
            {
                NBSPlaybackEntry entry = playbackMap.entries[i];
                if (entry.kind == NBSPlaybackEntryKind.note && entry.instrument.isValid)
                    instruments.Add(entry.instrument);
            }

            try
            {
                foreach (NBSInstrumentReference instrument in instruments)
                {
                    ResourceKey key = instrument.Resolve(nbsAssetId);
                    IAssetScope<WaveAudioClip>? scope = await ResourceManager.LoadScopeAsync<WaveAudioClip>(key);
                    if (scope == null)
                    {
                        Debug.RuntimeLogWarning($"NBS instrument resource '{key.assetId}' is unavailable. Notes using it will be silent.", nameof(NoteBlockSource));
                        continue;
                    }

                    bank.scopes.Add(instrument, scope);
                }
            }
            catch
            {
                DisposeQueue.Enqueue(bank);
                throw;
            }

            return bank;
        }

        /// <inheritdoc/>
        public bool TryGetLength(NBSInstrumentReference instrument, out double length)
        {
            lock (lifetimeLock)
            {
                if (!isDisposed && scopes.TryGetValue(instrument, out IAssetScope<WaveAudioClip>? scope))
                {
                    length = scope.asset.length;
                    return double.IsFinite(length) && length > 0;
                }
            }

            length = 0;
            return false;
        }

        internal bool TryGetClip(NBSInstrumentReference instrument, out WaveAudioClip clip)
        {
            lock (lifetimeLock)
            {
                if (!isDisposed && scopes.TryGetValue(instrument, out IAssetScope<WaveAudioClip>? scope) && !scope.asset.isDisposed)
                {
                    clip = scope.asset;
                    return true;
                }
            }

            clip = null!;
            return false;
        }

        internal bool TryRetain()
        {
            lock (lifetimeLock)
            {
                if (disposeRequested)
                    return false;

                activeUsers++;
                return true;
            }
        }

        internal void Release()
        {
            IAssetScope<WaveAudioClip>[]? scopesToDispose;
            lock (lifetimeLock)
            {
                if (activeUsers <= 0)
                    throw new InvalidOperationException("NBS instrument bank lease count is already zero.");

                activeUsers--;
                scopesToDispose = TakeScopesForDisposalUnsafe();
            }

            DisposeScopes(scopesToDispose);
        }

        /// <summary>
        /// Requests disposal of every owned instrument scope after active playback users finish.<br/>
        /// 활성 재생 사용자가 끝난 뒤 소유한 모든 악기 스코프의 해제를 요청합니다.
        /// </summary>
        public void Dispose()
        {
            IAssetScope<WaveAudioClip>[]? scopesToDispose;
            lock (lifetimeLock)
            {
                if (disposeRequested)
                    return;

                disposeRequested = true;
                scopesToDispose = TakeScopesForDisposalUnsafe();
            }

            DisposeScopes(scopesToDispose);
        }

        IAssetScope<WaveAudioClip>[]? TakeScopesForDisposalUnsafe()
        {
            if (!disposeRequested || activeUsers != 0 || isDisposed)
                return null;

            isDisposed = true;
            IAssetScope<WaveAudioClip>[] result = scopes.Values.ToArray();
            scopes.Clear();
            return result;
        }

        static void DisposeScopes(IAssetScope<WaveAudioClip>[]? scopesToDispose)
        {
            if (scopesToDispose == null)
                return;

            for (int i = 0; i < scopesToDispose.Length; i++)
                scopesToDispose[i].Dispose();
        }
    }
}
