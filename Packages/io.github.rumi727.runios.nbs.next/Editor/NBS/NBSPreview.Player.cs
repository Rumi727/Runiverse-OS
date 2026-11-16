#nullable enable
using RuniOS.NBS;
using RuniOS.Sounds;

namespace RuniOS.Editor.NBS
{
    public sealed partial class NBSPreview
    {
        static readonly HashSet<Player> globalPlayers = [];

        /// <summary>
        /// Gets the players of this preview by clip.<br/>
        /// 이 미리보기의 플레이어를 클립별로 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<NoteBlockClip, Player> players => _players;
        readonly Dictionary<NoteBlockClip, Player> _players = [];

        /// <summary>
        /// Gets or sets the volume shared by every clip preview.<br/>
        /// 모든 클립 미리보기가 공유하는 볼륨을 가져오거나 설정합니다.
        /// </summary>
        public static float globalVolume
        {
            get;
            set
            {
                field = value;
                foreach (Player player in globalPlayers)
                    player.UpdateVolume();
            }
        } = 0.5f;

        /// <summary>
        /// Gets or sets whether a clip preview starts playing when it is shown.<br/>
        /// 클립 미리보기가 표시될 때 재생을 시작할지 여부를 가져오거나 설정합니다.
        /// </summary>
        public static bool autoPlay { get; set; }

        /// <summary>
        /// Gets the player of a clip, creating it when it does not exist.<br/>
        /// 클립의 플레이어를 가져오며, 없으면 만듭니다.
        /// </summary>
        /// <param name="clip">
        /// The clip to play.<br/>
        /// 재생할 클립입니다.
        /// </param>
        /// <returns>
        /// The player of <paramref name="clip"/>.<br/>
        /// <paramref name="clip"/>의 플레이어를 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the preview has been disposed.<br/>
        /// 미리보기가 해제된 경우 발생합니다.
        /// </exception>
        public Player GetOrCreatePlayer(NoteBlockClip clip)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            if (!players.TryGetValue(clip, out Player? player))
                player = new Player(this, clip);

            return player;
        }

        /// <summary>
        /// Plays one clip of an <see cref="NBSPreview"/>.<br/>
        /// <see cref="NBSPreview"/>의 클립 하나를 재생합니다.
        /// </summary>
        /// <remarks>
        /// Playing before the instruments are loaded loads them first, and playback starts once they are loaded.<br/>
        /// 악기가 로드되기 전에 재생하면 먼저 로드하고, 로드되면 재생을 시작합니다.
        /// </remarks>
        public sealed class Player : IPlayControl, IStoppable, ISeekable
        {
            internal Player(NBSPreview preview, NoteBlockClip clip)
            {
                this.preview = preview;
                this.clip = clip;

                preview._players.Add(clip, this);
                preview.onBankLoadCompleted += OnBankLoadCompleted;

                globalPlayers.Add(this);
            }

            readonly NBSPreview preview;
            readonly NoteBlockClip clip;

            NBSPreviewTransport? transport;
            double? pendingStartTime;

            /// <summary>
            /// Gets whether the player is waiting for the instruments to start playing.<br/>
            /// 플레이어가 재생을 시작하려고 악기를 기다리는 중인지 여부를 가져옵니다.
            /// </summary>
            public bool isLoading => pendingStartTime != null;

            /// <summary>
            /// Gets whether the instruments are loaded and the player can be controlled.<br/>
            /// 악기가 로드되어 플레이어를 조작할 수 있는지 여부를 가져옵니다.
            /// </summary>
            public bool isLoaded => transport != null;

            /// <inheritdoc/>
            public bool isPlaying => transport?.isPlaying == true;

            /// <inheritdoc/>
            public double time
            {
                get => transport?.time ?? 0;
                set
                {
                    if (transport != null)
                        transport.time = value;
                    else
                        Play(value);
                }
            }

            /// <inheritdoc/>
            public double length => clip.length;

            /// <summary>
            /// Gets or sets <see cref="globalVolume"/>.<br/>
            /// <see cref="globalVolume"/>을 가져오거나 설정합니다.
            /// </summary>
            public float volume
            {
                get => globalVolume;
                set => globalVolume = value;
            }

            /// <summary>
            /// Gets or sets whether the clip loops. Until the instruments are loaded it follows the NBS header, and setting it has no effect.<br/>
            /// 클립을 반복할지 여부를 가져오거나 설정합니다. 악기가 로드되기 전에는 NBS 헤더를 따르며, 설정해도 효과가 없습니다.
            /// </summary>
            public bool loop
            {
                get => transport?.loop ?? clip.header.loop;
                set
                {
                    if (transport != null)
                        transport.loop = value;
                }
            }

            /// <inheritdoc/>
            public void Play(double startTime = 0)
            {
                if (transport == null)
                {
                    // Set before requesting, because a load that completes without suspending raises the event inside the request.
                    pendingStartTime = startTime;
                    NBSInstrumentBank? bank = preview.GetBank(clip);
                    if (transport != null || bank == null)
                        return;

                    CreateTransport(bank);
                }

                pendingStartTime = null;
                transport!.Play(startTime);
            }

            /// <inheritdoc/>
            public void Stop()
            {
                pendingStartTime = null;
                transport?.Stop();
            }

            internal void Dispose()
            {
                globalPlayers.Remove(this);

                transport?.Dispose();
                transport = null;
                pendingStartTime = null;
            }

            internal void UpdateVolume() => transport?.volume = globalVolume;

            void CreateTransport(NBSInstrumentBank bank) => transport = new NBSPreviewTransport(clip, bank) { volume = globalVolume };

            void OnBankLoadCompleted(NoteBlockClip loaded)
            {
                if (loaded != clip || pendingStartTime is not { } startTime)
                    return;

                pendingStartTime = null;
                if (!preview.loadedBanks.TryGetValue(clip, out NBSInstrumentBank? bank))
                    return;

                CreateTransport(bank);
                transport!.Play(startTime);
            }
        }
    }
}
