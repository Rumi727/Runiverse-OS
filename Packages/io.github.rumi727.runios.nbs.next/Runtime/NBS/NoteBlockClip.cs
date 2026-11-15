#nullable enable
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.Sounds;

namespace RuniOS.NBS
{
    /// <summary>
    /// Stores an editable Note Block Studio song and its applied <see cref="NBSMap"/>.<br/>
    /// 편집할 수 있는 Note Block Studio 곡과 적용된 <see cref="NBSMap"/>을 저장합니다.
    /// </summary>
    /// <remarks>
    /// <see cref="header"/>, <see cref="notes"/>, <see cref="layers"/>, and <see cref="instruments"/> are the serialized song data and can be edited freely.<br/>
    /// Edits take effect in <see cref="map"/> only after <see cref="Apply"/>, like <see cref="Texture2D.Apply()"/>. The map is also rebuilt after deserialization.
    /// <br/><br/>
    /// <see cref="header"/>, <see cref="notes"/>, <see cref="layers"/>, <see cref="instruments"/>는 직렬화되는 곡 데이터이며 자유롭게 편집할 수 있습니다.<br/>
    /// 편집 내용은 <see cref="Texture2D.Apply()"/>처럼 <see cref="Apply"/>를 호출한 뒤에만 <see cref="map"/>에 반영됩니다. 맵은 역직렬화 후에도 다시 만들어집니다.
    /// </remarks>
    public sealed class NoteBlockClip : ScriptableObject, IAudioClip, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Gets or sets the song header.<br/>
        /// 곡 헤더를 가져오거나 설정합니다.
        /// </summary>
        [field: SerializeField] public NBSHeader header { get; set; } = new NBSHeader();

        /// <summary>
        /// Gets the notes. Notes and layers are independent; a note refers to its layer by <see cref="NBSNote.layer"/>.<br/>
        /// 노트를 가져옵니다. 노트와 레이어는 서로 독립이며, 노트는 <see cref="NBSNote.layer"/>로 레이어를 가리킵니다.
        /// </summary>
        [field: SerializeField] public List<NBSNote> notes { get; private set; } = [];

        /// <summary>
        /// Gets the layer rows.<br/>
        /// 레이어 행을 가져옵니다.
        /// </summary>
        [field: SerializeField] public List<NBSLayer> layers { get; private set; } = [];

        /// <summary>
        /// Gets the custom instruments.<br/>
        /// 커스텀 악기를 가져옵니다.
        /// </summary>
        [field: SerializeField] public List<NBSInstrument> instruments { get; private set; } = [];

        /// <summary>
        /// Gets the map created by the last <see cref="Apply"/> or deserialization.<br/>
        /// 마지막 <see cref="Apply"/> 또는 역직렬화로 만든 맵을 가져옵니다.
        /// </summary>
        /// <remarks>
        /// The getter is thread-safe. The returned map is immutable, so it stays consistent while another thread calls <see cref="Apply"/>.<br/>
        /// getter는 thread-safe합니다. 반환된 맵은 불변이므로 다른 스레드가 <see cref="Apply"/>를 호출해도 일관성을 유지합니다.
        /// </remarks>
        public NBSMap map => Volatile.Read(ref _map);
        NBSMap _map = new NBSMap(new NBSHeader(), default, default, default);

        /// <summary>
        /// Gets the song length in seconds from <see cref="map"/>.<br/>
        /// <see cref="map"/>의 곡 길이를 초 단위로 가져옵니다.
        /// </summary>
        public double length => map.length;

        /// <summary>
        /// Creates a new map from the current song data and makes it <see cref="map"/>.<br/>
        /// 현재 곡 데이터로 새 맵을 만들고 <see cref="map"/>으로 설정합니다.
        /// </summary>
        public void Apply() => Volatile.Write(ref _map, CreateMap(header, notes, layers, instruments));

        /// <summary>
        /// Creates an empty clip.<br/>
        /// 빈 클립을 만듭니다.
        /// </summary>
        /// <returns>
        /// The created clip.<br/>
        /// 만든 클립을 반환합니다.
        /// </returns>
        /// <remarks>
        /// Call this from the main thread.<br/>
        /// 메인 스레드에서 호출해야 합니다.
        /// </remarks>
        public static NoteBlockClip Create() => CreateInstance<NoteBlockClip>();

        /// <summary>
        /// Creates a clip from song data.<br/>
        /// 곡 데이터로 클립을 만듭니다.
        /// </summary>
        /// <param name="header">
        /// The song header.<br/>
        /// 곡 헤더입니다.
        /// </param>
        /// <param name="notes">
        /// The notes in any order.<br/>
        /// 순서와 관계없는 노트입니다.
        /// </param>
        /// <param name="layers">
        /// The layer rows.<br/>
        /// 레이어 행입니다.
        /// </param>
        /// <param name="instruments">
        /// The custom instruments.<br/>
        /// 커스텀 악기입니다.
        /// </param>
        /// <returns>
        /// The created clip with an applied map.<br/>
        /// 맵이 적용된 클립을 반환합니다.
        /// </returns>
        /// <remarks>
        /// Call this from the main thread. The data is copied.<br/>
        /// 메인 스레드에서 호출해야 합니다. 데이터는 복사됩니다.
        /// </remarks>
        public static NoteBlockClip Create(NBSHeader header, ReadOnlySpan<NBSNote> notes, ReadOnlySpan<NBSLayer> layers, ReadOnlySpan<NBSInstrument> instruments)
        {
            List<NBSNote> noteList = new List<NBSNote>(notes.Length);
            noteList.AddRange(notes);

            List<NBSLayer> layerList = new List<NBSLayer>(layers.Length);
            layerList.AddRange(layers);

            List<NBSInstrument> instrumentList = new List<NBSInstrument>(instruments.Length);
            instrumentList.AddRange(instruments);

            return Create(header, noteList, layerList, instrumentList, CreateMap(header, noteList, layerList, instrumentList));
        }

        /// <summary>
        /// Reads an NBS file from a stream and creates a clip.<br/>
        /// 스트림에서 NBS 파일을 읽어 클립을 만듭니다.
        /// </summary>
        /// <param name="stream">
        /// The readable stream positioned at the start of an NBS file. It stays open.<br/>
        /// NBS 파일 시작 위치에 있는 읽기 가능한 스트림입니다. 스트림은 열린 상태로 유지됩니다.
        /// </param>
        /// <returns>
        /// The created clip with an applied map.<br/>
        /// 맵이 적용된 클립을 반환합니다.
        /// </returns>
        /// <remarks>
        /// Call this from the main thread.<br/>
        /// 메인 스레드에서 호출해야 합니다.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stream"/> is <see langword="null"/>.<br/>
        /// <paramref name="stream"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Thrown when the file is invalid or newer than <see cref="NBSReader.latestVersion"/>.<br/>
        /// 파일이 유효하지 않거나 <see cref="NBSReader.latestVersion"/>보다 새로운 경우 발생합니다.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Thrown when the file ends inside a section.<br/>
        /// 파일이 섹션 도중에 끝난 경우 발생합니다.
        /// </exception>
        public static NoteBlockClip Create(Stream stream)
        {
            Read(stream, out NBSHeader header, out List<NBSNote> notes, out List<NBSLayer> layers, out List<NBSInstrument> instruments);
            return Create(header, notes, layers, instruments, CreateMap(header, notes, layers, instruments));
        }

        /// <summary>
        /// Asynchronously reads an NBS file and creates a clip.<br/>
        /// NBS 파일을 비동기로 읽어 클립을 만듭니다.
        /// </summary>
        /// <param name="node">
        /// The NBS file to read.<br/>
        /// 읽을 NBS 파일입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용하는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the created clip with an applied map.<br/>
        /// 비동기 작업이 완료되면 맵이 적용된 클립을 반환합니다.
        /// </returns>
        /// <remarks>
        /// Reading and map creation run on the thread pool. The clip is created on the main thread.<br/>
        /// 읽기와 맵 생성은 스레드 풀에서 실행됩니다. 클립은 메인 스레드에서 만들어집니다.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="node"/> is not valid.<br/>
        /// <paramref name="node"/>가 유효하지 않은 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Thrown when the file is invalid or newer than <see cref="NBSReader.latestVersion"/>.<br/>
        /// 파일이 유효하지 않거나 <see cref="NBSReader.latestVersion"/>보다 새로운 경우 발생합니다.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Thrown when the file ends inside a section.<br/>
        /// 파일이 섹션 도중에 끝난 경우 발생합니다.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.<br/>
        /// <paramref name="cancellationToken"/>을 통해 작업이 취소된 경우 발생합니다.
        /// </exception>
        public static async UniTask<NoteBlockClip> LoadAsync(IONode node, CancellationToken cancellationToken = default)
        {
            if (!node.isValid)
                throw new ArgumentException("The NBS file node must be valid.", nameof(node));

            NBSHeader header;
            List<NBSNote> notes;
            List<NBSLayer> layers;
            List<NBSInstrument> instruments;

            await using (Stream stream = await node.file.OpenRead(cancellationToken))
            {
                await UniTask.SwitchToThreadPool();
                Read(stream, out header, out notes, out layers, out instruments);
            }

            cancellationToken.ThrowIfCancellationRequested();
            NBSMap map = CreateMap(header, notes, layers, instruments);

            await UniTask.SwitchToMainThread(cancellationToken);

            NoteBlockClip clip = Create(header, notes, layers, instruments, map);
            clip.name = Path.GetFileNameWithoutExtension(node.name);
            return clip;
        }

        static void Read(Stream stream, out NBSHeader header, out List<NBSNote> notes, out List<NBSLayer> layers, out List<NBSInstrument> instruments)
        {
            notes = [];
            layers = [];
            instruments = [];

            using NBSReader reader = new NBSReader(stream, true);
            header = reader.ReadHeader();
            reader.ReadNotes(notes);
            if (reader.ReadLayers(layers))
                reader.ReadInstruments(instruments);
        }

        static NBSMap CreateMap(NBSHeader header, List<NBSNote> notes, List<NBSLayer> layers, List<NBSInstrument> instruments) =>
            new NBSMap(header, CollectionsMarshal.AsSpan(notes), CollectionsMarshal.AsSpan(layers), CollectionsMarshal.AsSpan(instruments));

        static NoteBlockClip Create(NBSHeader header, List<NBSNote> notes, List<NBSLayer> layers, List<NBSInstrument> instruments, NBSMap map)
        {
            NoteBlockClip clip = CreateInstance<NoteBlockClip>();
            clip.header = header;
            clip.notes = notes;
            clip.layers = layers;
            clip.instruments = instruments;
            clip._map = map;
            return clip;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize() => Apply();
    }
}
