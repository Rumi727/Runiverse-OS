#nullable enable
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace RuniOS.NBS
{
    /// <summary>
    /// Reads an NBS file from a stream section by section.<br/>
    /// 스트림에서 NBS 파일을 섹션 단위로 읽습니다.
    /// </summary>
    /// <remarks>
    /// Call <see cref="ReadHeader"/>, <see cref="ReadNotes"/>, <see cref="ReadLayers"/>, and <see cref="ReadInstruments"/> in that order. Reading can stop after any section.<br/>
    /// The stream is read sequentially and never loaded into memory at once. Corrections follow the Note Block Studio loader: keys are clamped to <c>0</c>–<c>87</c>, layer volumes to <c>0</c>–<c>100</c>, the time signature to <c>2</c>–<c>8</c>, and work statistics to non-negative values.
    /// Custom instrument indexes are moved to the current layout of <see cref="NBSInstrument.vanillaCount"/> built-in instruments.
    /// <br/><br/>
    /// <see cref="ReadHeader"/>, <see cref="ReadNotes"/>, <see cref="ReadLayers"/>, <see cref="ReadInstruments"/>를 이 순서대로 호출합니다. 어느 섹션 뒤에서든 읽기를 멈출 수 있습니다.<br/>
    /// 스트림은 순차적으로 읽으며 한 번에 메모리에 올리지 않습니다. 보정은 Note Block Studio 로더를 따릅니다. 건반은 <c>0</c>–<c>87</c>, 레이어 음량은 <c>0</c>–<c>100</c>, 박자표는 <c>2</c>–<c>8</c>로 제한하고 작업 통계는 음수가 되지 않게 합니다.
    /// 커스텀 악기 인덱스는 <see cref="NBSInstrument.vanillaCount"/>개 기본 악기의 현재 배치로 옮깁니다.
    /// </remarks>
    public sealed class NBSReader : IDisposable
    {
        /// <summary>
        /// Identifies the next section that the reader expects.<br/>
        /// 리더가 다음으로 기대하는 섹션을 식별합니다.
        /// </summary>
        public enum Section
        {
            /// <summary>
            /// The header has not been read.<br/>
            /// 헤더를 아직 읽지 않았습니다.
            /// </summary>
            header,

            /// <summary>
            /// The note block section is next.<br/>
            /// 다음은 노트 블록 섹션입니다.
            /// </summary>
            notes,

            /// <summary>
            /// The optional layer section is next.<br/>
            /// 다음은 선택적인 레이어 섹션입니다.
            /// </summary>
            layers,

            /// <summary>
            /// The optional custom instrument section is next.<br/>
            /// 다음은 선택적인 커스텀 악기 섹션입니다.
            /// </summary>
            instruments,

            /// <summary>
            /// Every section has been read or the file ended.<br/>
            /// 모든 섹션을 읽었거나 파일이 끝났습니다.
            /// </summary>
            end
        }

        /// <summary>
        /// The newest format version the reader accepts.<br/>
        /// 리더가 받아들이는 가장 새로운 포맷 버전입니다.
        /// </summary>
        public const byte latestVersion = 6;

        /// <summary>
        /// Initializes a reader for <paramref name="stream"/>.<br/>
        /// <paramref name="stream"/>을 읽는 리더를 초기화합니다.
        /// </summary>
        /// <param name="stream">
        /// The readable stream positioned at the start of an NBS file.<br/>
        /// NBS 파일 시작 위치에 있는 읽기 가능한 스트림입니다.
        /// </param>
        /// <param name="leaveOpen">
        /// Whether <paramref name="stream"/> stays open after the reader is disposed.<br/>
        /// 리더를 해제한 뒤에도 <paramref name="stream"/>을 열어 둘지 여부입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stream"/> is <see langword="null"/>.<br/>
        /// <paramref name="stream"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="stream"/> cannot be read.<br/>
        /// <paramref name="stream"/>을 읽을 수 없는 경우 발생합니다.
        /// </exception>
        public NBSReader(Stream stream, bool leaveOpen = false)
        {
            ArgumentNullException.ThrowIfNull(stream);
            if (!stream.CanRead)
                throw new ArgumentException("The stream must be readable.", nameof(stream));

            this.stream = stream;
            this.leaveOpen = leaveOpen;
        }

        readonly Stream stream;
        readonly bool leaveOpen;

        static readonly UTF8Encoding strictUtf8 = new UTF8Encoding(false, true);

        /// <summary>
        /// Gets the next section that the reader expects.<br/>
        /// 리더가 다음으로 기대하는 섹션을 가져옵니다.
        /// </summary>
        public Section section { get; private set; } = Section.header;

        /// <summary>
        /// Gets the header after <see cref="ReadHeader"/> succeeds.<br/>
        /// <see cref="ReadHeader"/>가 성공한 뒤의 헤더를 가져옵니다.
        /// </summary>
        public NBSHeader header { get; private set; }

        /// <summary>
        /// Reads the header.<br/>
        /// 헤더를 읽습니다.
        /// </summary>
        /// <returns>
        /// The header read from the stream.<br/>
        /// 스트림에서 읽은 헤더를 반환합니다.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the header has already been read.<br/>
        /// 헤더를 이미 읽은 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Thrown when the version is newer than <see cref="latestVersion"/> or a field is invalid.<br/>
        /// 버전이 <see cref="latestVersion"/>보다 새롭거나 필드가 유효하지 않은 경우 발생합니다.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Thrown when the stream ends inside the header.<br/>
        /// 헤더 도중에 스트림이 끝난 경우 발생합니다.
        /// </exception>
        public NBSHeader ReadHeader()
        {
            ExpectSection(Section.header);

            NBSHeader result = new NBSHeader();
            short first = ReadInt16();
            if (first == 0)
            {
                result.version = ReadByte();
                if (result.version > latestVersion)
                    throw new InvalidDataException($"NBS version {result.version} is newer than the supported version {latestVersion}.");

                result.vanillaInstrumentCount = ReadByte();
                result.songLength = result.version >= 3 ? ReadInt16() : (short)0;
            }
            else
            {
                result.version = 0;
                result.vanillaInstrumentCount = 0;
                result.songLength = first;
            }

            result.layerCount = ReadInt16();
            if (result.layerCount < 0)
                throw new InvalidDataException($"The NBS layer count cannot be negative: {result.layerCount}.");

            result.name = ReadString();
            result.author = ReadString();
            result.originalAuthor = ReadString();
            result.description = ReadString();
            result.rawTempo = ReadInt16();
            result.autoSave = ReadByte() != 0;
            result.autoSaveDuration = ReadByte();
            result.timeSignature = Math.Clamp(ReadByte(), (byte)2, (byte)8);
            result.minutesSpent = Math.Max(0, ReadInt32());
            result.leftClicks = Math.Max(0, ReadInt32());
            result.rightClicks = Math.Max(0, ReadInt32());
            result.blocksAdded = Math.Max(0, ReadInt32());
            result.blocksRemoved = Math.Max(0, ReadInt32());
            result.importedFileName = ReadString();

            if (result.version >= 4)
            {
                result.loop = ReadByte() != 0;
                result.maxLoopCount = ReadByte();
                result.loopStartTick = ReadInt16();
            }

            header = result;
            section = Section.notes;
            return result;
        }

        /// <summary>
        /// Reads every note block and appends it to <paramref name="notes"/> in file order, which is ascending by tick and then by layer.<br/>
        /// 모든 노트 블록을 파일 순서(tick, 레이어 오름차순)대로 <paramref name="notes"/>에 추가합니다.
        /// </summary>
        /// <param name="notes">
        /// The list that receives the notes.<br/>
        /// 노트를 받을 리스트입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="notes"/> is <see langword="null"/>.<br/>
        /// <paramref name="notes"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the reader is not at <see cref="Section.notes"/>.<br/>
        /// 리더가 <see cref="Section.notes"/>에 있지 않은 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Thrown when a jump is negative or a coordinate exceeds <see cref="int"/>.<br/>
        /// 점프 값이 음수이거나 좌표가 <see cref="int"/>를 넘는 경우 발생합니다.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Thrown when the stream ends inside the note block section.<br/>
        /// 노트 블록 섹션 도중에 스트림이 끝난 경우 발생합니다.
        /// </exception>
        public void ReadNotes(List<NBSNote> notes)
        {
            ArgumentNullException.ThrowIfNull(notes);
            ExpectSection(Section.notes);

            bool hasNoteDetails = header.version >= 4;
            int customIndexOffset = header.version == 0 ? 0 : NBSInstrument.vanillaCount - header.vanillaInstrumentCount;

            // A note needs at least a layer jump, an instrument, and a key. The bound only reserves memory, it never limits reading.
            if (stream.CanSeek)
            {
                long minimumNoteBytes = hasNoteDetails ? 8 : 4;
                notes.EnsureCapacity(notes.Count + (int)Math.Min(int.MaxValue - notes.Count, (stream.Length - stream.Position) / minimumNoteBytes));
            }

            int tick = -1;
            while (true)
            {
                short tickJump = ReadInt16();
                if (tickJump == 0)
                    break;

                tick = AddJump(tick, tickJump, "tick");

                int layer = -1;
                while (true)
                {
                    short layerJump = ReadInt16();
                    if (layerJump == 0)
                        break;

                    layer = AddJump(layer, layerJump, "layer");

                    int instrument = ReadByte();
                    if (instrument >= header.vanillaInstrumentCount)
                        instrument += customIndexOffset;

                    byte key = Math.Min(ReadByte(), (byte)87);
                    byte velocity = 100;
                    byte panning = 100;
                    short pitch = 0;
                    if (hasNoteDetails)
                    {
                        velocity = ReadByte();
                        panning = ReadByte();
                        pitch = ReadInt16();
                    }

                    notes.Add(new NBSNote(tick, layer, instrument, key, velocity, panning, pitch));
                }
            }

            section = Section.layers;
        }

        /// <summary>
        /// Reads the layer section and appends <see cref="NBSHeader.layerCount"/> layers to <paramref name="layers"/>.<br/>
        /// 레이어 섹션을 읽고 <see cref="NBSHeader.layerCount"/>개의 레이어를 <paramref name="layers"/>에 추가합니다.
        /// </summary>
        /// <param name="layers">
        /// The list that receives the layers.<br/>
        /// 레이어를 받을 리스트입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the section exists; <see langword="false"/> if the file ended after the notes.<br/>
        /// 섹션이 있으면 <see langword="true"/>를 반환하고, 파일이 노트 뒤에서 끝났으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="layers"/> is <see langword="null"/>.<br/>
        /// <paramref name="layers"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the reader is not at <see cref="Section.layers"/>.<br/>
        /// 리더가 <see cref="Section.layers"/>에 있지 않은 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Thrown when a string length is invalid.<br/>
        /// 문자열 길이가 유효하지 않은 경우 발생합니다.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Thrown when the stream ends inside the layer section.<br/>
        /// 레이어 섹션 도중에 스트림이 끝난 경우 발생합니다.
        /// </exception>
        public bool ReadLayers(List<NBSLayer> layers)
        {
            ArgumentNullException.ThrowIfNull(layers);
            ExpectSection(Section.layers);

            if (!TryReadByte(out byte firstByte))
            {
                section = Section.end;
                return false;
            }

            layers.EnsureCapacity(layers.Count + header.layerCount);
            for (int i = 0; i < header.layerCount; i++)
            {
                string name = i == 0 ? ReadString(firstByte) : ReadString();
                NBSLayerLock lockState = header.version >= 4 ? (NBSLayerLock)ReadByte() : NBSLayerLock.none;
                byte volume = Math.Min(ReadByte(), (byte)100);
                byte panning = header.version >= 2 ? ReadByte() : (byte)100;
                layers.Add(new NBSLayer(name, lockState, volume, panning));
            }

            // With no layers, the first byte already belongs to the instrument count.
            if (header.layerCount == 0)
            {
                pendingByte = firstByte;
                hasPendingByte = true;
            }

            section = Section.instruments;
            return true;
        }

        /// <summary>
        /// Reads the custom instrument section and appends every instrument to <paramref name="instruments"/>.<br/>
        /// 커스텀 악기 섹션을 읽고 모든 악기를 <paramref name="instruments"/>에 추가합니다.
        /// </summary>
        /// <param name="instruments">
        /// The list that receives the instruments.<br/>
        /// 악기를 받을 리스트입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the section exists; <see langword="false"/> if the file ended after the layers.<br/>
        /// 섹션이 있으면 <see langword="true"/>를 반환하고, 파일이 레이어 뒤에서 끝났으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="instruments"/> is <see langword="null"/>.<br/>
        /// <paramref name="instruments"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the reader is not at <see cref="Section.instruments"/>.<br/>
        /// 리더가 <see cref="Section.instruments"/>에 있지 않은 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Thrown when a string length is invalid.<br/>
        /// 문자열 길이가 유효하지 않은 경우 발생합니다.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Thrown when the stream ends inside the custom instrument section.<br/>
        /// 커스텀 악기 섹션 도중에 스트림이 끝난 경우 발생합니다.
        /// </exception>
        public bool ReadInstruments(List<NBSInstrument> instruments)
        {
            ArgumentNullException.ThrowIfNull(instruments);
            ExpectSection(Section.instruments);

            section = Section.end;
            if (!TryReadByte(out byte count))
                return false;

            instruments.EnsureCapacity(instruments.Count + count);
            for (int i = 0; i < count; i++)
            {
                string name = ReadString();
                string soundFile = ReadString();
                byte key = ReadByte();
                bool pressKey = ReadByte() != 0;
                instruments.Add(new NBSInstrument(name, soundFile, key, pressKey));
            }

            return true;
        }

        /// <summary>
        /// Releases the reader and, unless the reader was created with <c>leaveOpen</c>, the stream.<br/>
        /// 리더를 해제하고, <c>leaveOpen</c>으로 생성하지 않았다면 스트림도 해제합니다.
        /// </summary>
        public void Dispose()
        {
            if (!leaveOpen)
                stream.Dispose();
        }

        byte pendingByte;
        bool hasPendingByte;

        void ExpectSection(Section expected)
        {
            if (section != expected)
                throw new InvalidOperationException($"The NBS reader expected the {expected} section, but it is at the {section} section.");
        }

        static int AddJump(int coordinate, short jump, string coordinateName)
        {
            if (jump < 0)
                throw new InvalidDataException($"The NBS {coordinateName} jump cannot be negative: {jump}.");

            long result = (long)coordinate + jump;
            if (result > int.MaxValue)
                throw new InvalidDataException($"The NBS {coordinateName} exceeds Int32: {result}.");

            return (int)result;
        }

        bool TryReadByte(out byte value)
        {
            if (hasPendingByte)
            {
                hasPendingByte = false;
                value = pendingByte;
                return true;
            }

            int result = stream.ReadByte();
            value = (byte)result;
            return result >= 0;
        }

        byte ReadByte() => TryReadByte(out byte value) ? value : throw new EndOfStreamException();

        short ReadInt16()
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            ReadExactly(buffer);
            return BinaryPrimitives.ReadInt16LittleEndian(buffer);
        }

        int ReadInt32()
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            ReadExactly(buffer);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        void ReadExactly(Span<byte> buffer)
        {
            if (buffer.IsEmpty)
                return;

            if (hasPendingByte)
            {
                hasPendingByte = false;
                buffer[0] = pendingByte;
                buffer = buffer[1..];
            }

            stream.ReadExactly(buffer);
        }

        string ReadString()
        {
            Span<byte> lengthBytes = stackalloc byte[sizeof(int)];
            ReadExactly(lengthBytes);
            return ReadStringContent(BinaryPrimitives.ReadInt32LittleEndian(lengthBytes));
        }

        string ReadString(byte firstLengthByte)
        {
            Span<byte> lengthBytes = stackalloc byte[sizeof(int)];
            lengthBytes[0] = firstLengthByte;
            ReadExactly(lengthBytes[1..]);
            return ReadStringContent(BinaryPrimitives.ReadInt32LittleEndian(lengthBytes));
        }

        string ReadStringContent(int length)
        {
            if (length < 0 || (stream.CanSeek && length > stream.Length - stream.Position))
                throw new InvalidDataException($"Invalid NBS string byte length: {length}.");

            if (length == 0)
                return string.Empty;

            byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                Span<byte> bytes = buffer.AsSpan(0, length);
                ReadExactly(bytes);

                try
                {
                    return strictUtf8.GetString(bytes);
                }
                catch (DecoderFallbackException)
                {
                    // Note Block Studio maps every byte directly to a character.
                    return Encoding.Latin1.GetString(bytes);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
