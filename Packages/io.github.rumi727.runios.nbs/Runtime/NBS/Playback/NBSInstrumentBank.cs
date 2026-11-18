#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.Resource;
using RuniOS.Resource.Sounds;
using RuniOS.Sounds;

namespace RuniOS.NBS
{
    /// <summary>
    /// Owns the sound resources of the instruments that a song plays.<br/>
    /// 곡이 재생하는 악기의 사운드 리소스를 소유합니다.
    /// </summary>
    /// <remarks>
    /// The bank maps sounds by instrument index when it loads. After editing a clip, call <c>Reload</c> of the owner when a sound file changes,
    /// when custom instruments are reordered, or when a layer that was muted at load is unmuted.<br/>
    /// Built-in instruments resolve to <c>runios:block.note_block.*</c> in <see cref="WaveAudioAssetRegistry"/>.<br/>
    /// Custom instruments resolve to their <see cref="NBSInstrument.soundFile"/> in the song namespace, without a leading <c>sounds/</c> and without the extension. Missing sounds stay silent.
    /// <br/><br/>
    /// bank는 로드할 때 악기 인덱스별로 사운드를 연결합니다. 클립을 편집한 뒤 사운드 파일이 바뀌거나, 커스텀 악기 순서가 바뀌거나, 로드할 때 무음이던 레이어의 무음을 해제하면 소유자의 <c>Reload</c>를 호출해야 합니다.<br/>
    /// 기본 악기는 <see cref="WaveAudioAssetRegistry"/>의 <c>runios:block.note_block.*</c>로 확인합니다.<br/>
    /// 커스텀 악기는 곡 네임스페이스에서 앞의 <c>sounds/</c>와 확장자를 뺀 <see cref="NBSInstrument.soundFile"/>로 확인합니다. 없는 사운드는 무음입니다.
    /// </remarks>
    public sealed class NBSInstrumentBank : IDisposable
    {
        static readonly string[] vanillaPaths =
        [
            "block.note_block.harp",
            "block.note_block.bass",
            "block.note_block.bassdrum",
            "block.note_block.snare",
            "block.note_block.hat",
            "block.note_block.guitar",
            "block.note_block.flute",
            "block.note_block.bell",
            "block.note_block.chime",
            "block.note_block.xylophone",
            "block.note_block.iron_xylophone",
            "block.note_block.cow_bell",
            "block.note_block.didgeridoo",
            "block.note_block.bit",
            "block.note_block.banjo",
            "block.note_block.pling",
            "block.note_block.trumpet",
            "block.note_block.trumpet_exposed",
            "block.note_block.trumpet_weathered",
            "block.note_block.trumpet_oxidized"
        ];

        /// <summary>
        /// The key at which built-in instruments play at their original pitch, which is F#4.<br/>
        /// 기본 악기가 원래 피치로 재생되는 건반인 F#4입니다.
        /// </summary>
        public const int vanillaKey = 45;

        NBSInstrumentBank(ResourceKey?[] keys, IAssetScope<WaveAudioClip>?[] scopes)
        {
            this.keys = keys;
            this.scopes = scopes;
        }

        // The keys are kept so that a sound which was missing at load, such as before the resource packs were loaded, can be found by IsSameTarget.
        readonly ResourceKey?[] keys;
        readonly IAssetScope<WaveAudioClip>?[] scopes;

        /// <summary>
        /// Loads the instruments that the audible notes of a map use.<br/>
        /// 맵에서 소리 나는 노트가 사용하는 악기를 로드합니다.
        /// </summary>
        /// <param name="map">
        /// The map whose instruments to load.<br/>
        /// 악기를 로드할 맵입니다.
        /// </param>
        /// <param name="nameSpace">
        /// The resource namespace that custom instrument sounds are looked up in.<br/>
        /// 커스텀 악기 사운드를 찾을 리소스 네임스페이스입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용하는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the bank.<br/>
        /// 비동기 작업이 완료되면 bank를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="map"/> is <see langword="null"/>.<br/>
        /// <paramref name="map"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>. Loaded sounds are released.<br/>
        /// <paramref name="cancellationToken"/>을 통해 작업이 취소된 경우 발생합니다. 로드한 사운드는 해제됩니다.
        /// </exception>
        public static async UniTask<NBSInstrumentBank> LoadAsync(NBSMap map, string nameSpace, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(map);

            int count = NBSInstrument.vanillaCount + map.instruments.Length;
            ResourceKey?[] keys = new ResourceKey?[count];
            IAssetScope<WaveAudioClip>?[] scopes = new IAssetScope<WaveAudioClip>?[count];
            bool[] used = new bool[count];

            ReadOnlySpan<NBSMapNote> notes = map.notes.AsSpan();
            for (int i = 0; i < notes.Length; i++)
            {
                int instrument = notes[i].note.instrument;
                if ((uint)instrument < (uint)count && map.GetFunction(instrument) == NBSInstrumentFunction.none && map.IsLayerAudible(notes[i].layer))
                    used[instrument] = true;
            }

            NBSInstrumentBank bank = new NBSInstrumentBank(keys, scopes);
            try
            {
                for (int instrument = 0; instrument < count; instrument++)
                {
                    if (!used[instrument] || !TryGetResourceKey(map, instrument, nameSpace, out ResourceKey key))
                        continue;

                    keys[instrument] = key;
                    scopes[instrument] = await ResourceManager.LoadScopeAsync<WaveAudioClip>(key);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            catch
            {
                bank.Dispose();
                throw;
            }

            return bank;
        }

        /// <summary>
        /// Gets the sound of an instrument.<br/>
        /// 악기의 사운드를 가져옵니다.
        /// </summary>
        /// <param name="instrument">
        /// The instrument index of a note.<br/>
        /// 노트의 악기 인덱스입니다.
        /// </param>
        /// <returns>
        /// The loaded sound, or <see langword="null"/> when the instrument is silent.<br/>
        /// 로드된 사운드를 반환하며, 악기가 무음이면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public WaveAudioClip? GetClip(int instrument) => (uint)instrument < (uint)scopes.Length ? scopes[instrument]?.asset : null;

        /// <summary>
        /// Determines whether every instrument sound still resolves to the sound that this bank loaded.<br/>
        /// 모든 악기 사운드가 여전히 이 bank가 로드한 사운드로 확인되는지 확인합니다.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when every instrument resolves to the loaded sound, or is still missing when it was missing at load; otherwise, <see langword="false"/>.<br/>
        /// 모든 악기가 로드한 사운드로 확인되거나, 로드할 때 없던 사운드가 여전히 없으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool IsSameTarget()
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] is not { } key)
                    continue;

                IAssetHandle? handle = ResourceManager.GetHandle(key);
                IAssetScope<WaveAudioClip>? scope = scopes[i];
                if (handle == null ? scope != null : scope == null || !scope.handle.IsSameTarget(handle))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the resource key that an instrument sound is loaded from.<br/>
        /// 악기 사운드를 로드할 리소스 키를 가져옵니다.
        /// </summary>
        /// <param name="map">
        /// The map that defines the custom instruments.<br/>
        /// 커스텀 악기를 정의하는 맵입니다.
        /// </param>
        /// <param name="instrument">
        /// The instrument index of a note.<br/>
        /// 노트의 악기 인덱스입니다.
        /// </param>
        /// <param name="nameSpace">
        /// The namespace of custom instrument sounds.<br/>
        /// 커스텀 악기 사운드의 네임스페이스입니다.
        /// </param>
        /// <param name="key">
        /// The resource key when successful; otherwise, <see langword="default"/>.<br/>
        /// 성공하면 리소스 키이고, 그렇지 않으면 <see langword="default"/>입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the instrument has a valid sound path; otherwise, <see langword="false"/>.<br/>
        /// 악기에 유효한 사운드 경로가 있으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool TryGetResourceKey(NBSMap map, int instrument, string nameSpace, out ResourceKey key)
        {
            key = default;
            if ((uint)instrument < NBSInstrument.vanillaCount)
            {
                key = new ResourceKey(WaveAudioAssetRegistry.id, new Identifier(Identifier.defaultNamespace, vanillaPaths[instrument]));
                return true;
            }

            if (!map.TryGetCustomInstrumentIndex(instrument, out int customIndex) ||
                !TryGetCustomPath(map.instruments[customIndex].soundFile, out string path) ||
                !Identifier.IsNamespaceValid(nameSpace) ||
                !Identifier.IsPathValid(RuniPath.From(path)))
                return false;

            key = new ResourceKey(WaveAudioAssetRegistry.id, new Identifier(nameSpace, path));
            return true;
        }

        /// <summary>
        /// Converts a custom instrument sound file to a resource path.<br/>
        /// 커스텀 악기 사운드 파일을 리소스 경로로 변환합니다.
        /// </summary>
        /// <param name="soundFile">
        /// The sound file stored in the NBS file.<br/>
        /// NBS 파일에 저장된 사운드 파일입니다.
        /// </param>
        /// <param name="path">
        /// The path without a leading <c>sounds/</c> and without the extension when successful; otherwise, an empty string.<br/>
        /// 성공하면 앞의 <c>sounds/</c>와 확장자를 뺀 경로이고, 그렇지 않으면 빈 문자열입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the path is relative and has no empty, <c>.</c>, or <c>..</c> segment; otherwise, <see langword="false"/>.<br/>
        /// 경로가 상대 경로이고 비어 있거나 <c>.</c>, <c>..</c>인 구간이 없으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool TryGetCustomPath(string soundFile, out string path)
        {
            path = string.Empty;
            string normalized = soundFile.Trim().Replace('\\', '/');
            if (normalized.Length == 0 || normalized[0] == '/' || normalized.Contains(':'))
                return false;

            if (normalized.StartsWith("sounds/", StringComparison.OrdinalIgnoreCase))
                normalized = normalized["sounds/".Length..];

            int lastSlash = normalized.LastIndexOf('/');
            int lastDot = normalized.LastIndexOf('.');
            if (lastDot > lastSlash)
                normalized = normalized[..lastDot];

            foreach (Range segment in normalized.AsSpan().Split('/'))
            {
                ReadOnlySpan<char> part = normalized.AsSpan()[segment];
                if (part.IsWhiteSpace() || part is "." or "..")
                    return false;
            }

            path = normalized;
            return true;
        }

        /// <summary>
        /// Releases every instrument sound.<br/>
        /// 모든 악기 사운드를 해제합니다.
        /// </summary>
        /// <remarks>
        /// Stop the voices that use the sounds first.<br/>
        /// 먼저 사운드를 사용하는 Voice를 정지해야 합니다.
        /// </remarks>
        public void Dispose()
        {
            for (int i = 0; i < scopes.Length; i++)
            {
                scopes[i]?.Dispose();
                scopes[i] = null;
            }
        }
    }
}
