#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.NBS;
using RuniOS.Resource;

namespace RuniOS.Editor.NBS
{
    /// <summary>
    /// Loads and owns the instruments of the <see cref="NoteBlockClip"/>s that a preview plays, and hands out a <see cref="Player"/> for each clip.<br/>
    /// 미리보기가 재생하는 <see cref="NoteBlockClip"/>의 악기를 로드하고 소유하며, 클립마다 <see cref="Player"/>를 제공합니다.
    /// </summary>
    /// <remarks>
    /// Disposing the preview stops its players and releases the instruments.<br/>
    /// 미리보기를 해제하면 플레이어를 멈추고 악기를 해제합니다.
    /// </remarks>
    public sealed partial class NBSPreview : IDisposable, IReloadable
    {
        /// <summary>
        /// Initializes a preview.<br/>
        /// 미리보기를 초기화합니다.
        /// </summary>
        /// <param name="instrumentNamespace">
        /// The resource namespace of custom instrument sounds.<br/>
        /// 커스텀 악기 사운드의 리소스 네임스페이스입니다.
        /// </param>
        public NBSPreview(string instrumentNamespace)
        {
            this.instrumentNamespace = instrumentNamespace;
            ResourceManager.AttachReloadable(this);
        }

        readonly string instrumentNamespace;
        readonly Dictionary<NoteBlockClip, NBSInstrumentBank> loadedBanks = [];
        readonly HashSet<NoteBlockClip> loadingBanks = [];

        /// <summary>
        /// Occurs when loading the instruments of a clip completes, whether or not it succeeded.<br/>
        /// 클립의 악기 로드가 성공 여부와 관계없이 끝나면 발생합니다.
        /// </summary>
        public event Action<NoteBlockClip>? onBankLoadCompleted;

        /// <summary>
        /// Gets whether the preview has been disposed.<br/>
        /// 미리보기가 해제되었는지 여부를 가져옵니다.
        /// </summary>
        public bool isDisposed { get; private set; }

        /// <summary>
        /// Gets the instruments of a clip, and starts loading them when they are not loaded.<br/>
        /// 클립의 악기를 가져오며, 로드되지 않았으면 로드를 시작합니다.
        /// </summary>
        /// <param name="clip">
        /// The clip whose instruments to get.<br/>
        /// 악기를 가져올 클립입니다.
        /// </param>
        /// <returns>
        /// The loaded instruments, or <see langword="null"/> while they are loading or after loading failed.<br/>
        /// 로드된 악기를 반환하며, 로드 중이거나 로드에 실패했으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the preview has been disposed.<br/>
        /// 미리보기가 해제된 경우 발생합니다.
        /// </exception>
        public NBSInstrumentBank? GetBank(NoteBlockClip clip)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            if (loadedBanks.TryGetValue(clip, out NBSInstrumentBank? bank))
                return bank;

            if (loadingBanks.Add(clip))
                LoadBank(clip).Forget();

            return null;
        }

        /// <summary>
        /// Stops every player and releases the instruments.<br/>
        /// 모든 플레이어를 멈추고 악기를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            isDisposed = true;
            ResourceManager.DetachReloadable(this);

            // The schedulers of the players use the banks, so they are released first.
            foreach (Player player in _players.Values)
                player.Dispose();

            _players.Clear();

            foreach (NBSInstrumentBank bank in loadedBanks.Values)
                bank.Dispose();

            loadedBanks.Clear();
            loadingBanks.Clear();
        }

        /// <summary>
        /// Releases the instruments whose sounds changed, such as after the resource packs were loaded, and loads them again for the players that were playing.<br/>
        /// 리소스 팩이 로드된 뒤처럼 사운드가 바뀐 악기를 해제하고, 재생 중이던 플레이어를 위해 다시 로드합니다.
        /// </summary>
        /// <returns>
        /// A completed operation. The instruments load in the background.<br/>
        /// 완료된 작업입니다. 악기는 백그라운드에서 로드됩니다.
        /// </returns>
        public UniTask Reload()
        {
            if (isDisposed)
                return UniTask.CompletedTask;

            // Removing the current entry while enumerating a dictionary is allowed.
            foreach ((NoteBlockClip clip, NBSInstrumentBank bank) in loadedBanks)
            {
                if (bank.IsSameTarget())
                    continue;

                // The transport of the player uses the bank, so it is released first.
                if (_players.TryGetValue(clip, out Player? player))
                    player.ReleaseTransport();

                bank.Dispose();
                loadedBanks.Remove(clip);
            }

            // Loading starts after the enumeration, because a load that completes without suspending adds to the dictionary.
            foreach (Player player in _players.Values)
                player.ResumeLoading();

            return UniTask.CompletedTask;
        }

        async UniTaskVoid LoadBank(NoteBlockClip clip)
        {
            NBSInstrumentBank? bank = null;
            try
            {
                bank = await NBSInstrumentBank.LoadAsync(clip.map, instrumentNamespace);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (isDisposed)
            {
                bank?.Dispose();
                return;
            }

            // A failed load is forgotten, so the next request tries again.
            loadingBanks.Remove(clip);
            if (bank != null)
                loadedBanks.Add(clip, bank);

            onBankLoadCompleted?.Invoke(clip);
        }
    }
}
