#nullable enable
using RuniOS.Linq;
using System.Text;

namespace RuniOS.NBS
{
    /// <summary>
    /// Reads Note Block Studio versions 0 through 6 and rejects newer layouts.<br/>
    /// Note Block Studio 버전 0부터 6까지 읽고 그보다 새로운 레이아웃은 거부합니다.
    /// </summary>
    public static class NBSReader
    {
        const int maximumStringBytes = 16 * 1024 * 1024;
        static readonly UTF8Encoding strictUtf8 = new UTF8Encoding(false, true);

        /// <summary>
        /// Reads and parses one NBS file from <paramref name="stream"/> without taking ownership of the stream.<br/>
        /// <paramref name="stream"/>의 소유권을 가져오지 않고 NBS 파일 하나를 읽고 파싱합니다.
        /// </summary>
        /// <param name="stream">
        /// The readable stream positioned at the beginning of an NBS file.<br/>
        /// NBS 파일 시작 위치에 놓인 읽기 가능한 스트림입니다.
        /// </param>
        /// <returns>
        /// The immutable parsed file and its precomputed tempo map.<br/>
        /// 파싱된 불변 파일과 미리 계산된 템포 맵을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stream"/> is <see langword="null"/>.<br/>
        /// <paramref name="stream"/>이 <see langword="null"/>이면 발생합니다.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Thrown when the file is truncated, corrupt, or uses version 7 or newer.<br/>
        /// 파일이 잘렸거나 손상되었거나 버전 7 이상이면 발생합니다.
        /// </exception>
        public static NBSFile Read(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                using BinaryReader reader = new BinaryReader(stream, strictUtf8, true);
                ushort firstValue = ReadNonNegativeShort(reader, "format marker or classic song length");

                byte version;
                byte vanillaInstrumentCount;
                ushort declaredSongLength;
                ushort layerCount;
                if (firstValue != 0)
                {
                    version = 0;
                    vanillaInstrumentCount = 10;
                    declaredSongLength = firstValue;
                    layerCount = ReadNonNegativeShort(reader, "layer count");
                }
                else
                {
                    version = reader.ReadByte();
                    if (version > 6)
                        throw new InvalidDataException($"NBS version {version} is newer than the supported version 6.");

                    vanillaInstrumentCount = reader.ReadByte();
                    declaredSongLength = version >= 3
                        ? ReadNonNegativeShort(reader, "song length")
                        : (ushort)0;
                    layerCount = ReadNonNegativeShort(reader, "layer count");
                }

                string songName = ReadString(reader);
                string author = ReadString(reader);
                string originalAuthor = ReadString(reader);
                string description = ReadString(reader);

                ushort rawTempo = ReadNonNegativeShort(reader, "song tempo");
                double ticksPerSecond = rawTempo / 100d;
                if (ticksPerSecond <= 0)
                    throw new InvalidDataException($"The NBS header tempo must be greater than zero: raw value {rawTempo}, {ticksPerSecond} ticks per second.");

                bool autoSave = reader.ReadByte() != 0;
                byte autoSaveDuration = reader.ReadByte();
                byte timeSignature = reader.ReadByte();
                int minutesSpent = reader.ReadInt32();
                int leftClicks = reader.ReadInt32();
                int rightClicks = reader.ReadInt32();
                int blocksAdded = reader.ReadInt32();
                int blocksRemoved = reader.ReadInt32();
                string importedFileName = ReadString(reader);

                bool loopEnabled = false;
                byte maxLoopCount = 0;
                ushort loopStartTick = 0;
                if (version >= 4)
                {
                    loopEnabled = reader.ReadByte() != 0;
                    maxLoopCount = reader.ReadByte();
                    short rawLoopStartTick = reader.ReadInt16();
                    if (rawLoopStartTick < 0)
                        throw new InvalidDataException($"NBS loop start tick cannot be negative: {rawLoopStartTick}.");

                    loopStartTick = (ushort)rawLoopStartTick;
                }

                List<NBSNote> notes = ReadNotes(reader, version);
                List<NBSLayer> layers = ReadLayers(reader, version, layerCount);
                List<NBSCustomInstrument> customInstruments = ReadCustomInstruments(reader);
                ValidateCoordinatesAndInstruments(notes, layerCount, vanillaInstrumentCount, customInstruments);

                List<NBSSpecialEvent> specialEvents = ParseSpecialEvents(vanillaInstrumentCount, notes, customInstruments);

                int lastNoteTick = 0;
                if (notes.Count > 0)
                {
                    int maximumNoteTick = notes.Max(x => x.tick);
                    if (maximumNoteTick == int.MaxValue)
                        throw new InvalidDataException($"NBS song length exceeds Int32: last note tick {maximumNoteTick}, required length {(long)maximumNoteTick + 1}.");

                    lastNoteTick = maximumNoteTick + 1;
                }

                int tickLength = MathUtility.Max(declaredSongLength, lastNoteTick);

                NBSTick[] ticks = notes
                    .GroupBy(x => x.tick)
                    .OrderBy(x => x.Key)
                    .Select(x => new NBSTick(x.Key, x.OrderBy(y => y.layer).ToArray().AsReadOnly()))
                    .ToArray();

                NBSHeader header = new NBSHeader
                (
                    version,
                    vanillaInstrumentCount,
                    declaredSongLength,
                    layerCount,
                    songName,
                    author,
                    originalAuthor,
                    description,
                    ticksPerSecond,
                    autoSave,
                    autoSaveDuration,
                    timeSignature,
                    minutesSpent,
                    leftClicks,
                    rightClicks,
                    blocksAdded,
                    blocksRemoved,
                    importedFileName,
                    loopEnabled,
                    maxLoopCount,
                    loopStartTick
                );

                return new NBSFile
                (
                    header,
                    ticks.AsReadOnly(),
                    layers.AsReadOnly(),
                    customInstruments.AsReadOnly(),
                    specialEvents.OrderBy(x => x.tick).ThenBy(x => x.layer).ToArray().AsReadOnly(),
                    tickLength
                );
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException($"The NBS file ended before the current structure was complete{GetStreamPositionDescription(stream)}.", exception);
            }
            catch (OverflowException exception)
            {
                throw new InvalidDataException($"The NBS file contains an out-of-range length or coordinate{GetStreamPositionDescription(stream)}.", exception);
            }
        }

        static List<NBSNote> ReadNotes
        (
            BinaryReader reader,
            byte version
        )
        {
            List<NBSNote> notes = [];
            int tick = -1;

            while (true)
            {
                ushort tickJump = ReadNonNegativeShort(reader, "tick jump");
                if (tickJump == 0)
                    break;

                tick = AddCoordinateJump(tick, tickJump, "tick");
                int layer = -1;
                while (true)
                {
                    ushort layerJump = ReadNonNegativeShort(reader, "layer jump");
                    if (layerJump == 0)
                        break;

                    layer = AddCoordinateJump(layer, layerJump, "layer");
                    byte instrument = reader.ReadByte();
                    byte key = reader.ReadByte();
                    byte velocity = version >= 4 ? reader.ReadByte() : (byte)100;
                    byte panning = version >= 4 ? reader.ReadByte() : (byte)100;
                    short pitch = version >= 4 ? reader.ReadInt16() : (short)0;
                    notes.Add(new NBSNote(tick, layer, instrument, key, velocity, panning, pitch));
                }
            }

            return notes;
        }

        static List<NBSLayer> ReadLayers
        (
            BinaryReader reader,
            byte version,
            ushort layerCount
        )
        {
            List<NBSLayer> layers = new List<NBSLayer>(layerCount);
            for (int i = 0; i < layerCount; i++)
            {
                string name;
                try
                {
                    name = ReadString(reader);
                }
                catch (EndOfStreamException) when (i == 0)
                {
                    for (; i < layerCount; i++)
                        layers.Add(new NBSLayer(string.Empty, false, 100, 100));

                    return layers;
                }

                bool locked = version >= 4 && reader.ReadByte() != 0;
                byte volume = reader.ReadByte();
                byte panning = version >= 2 ? reader.ReadByte() : (byte)100;

                layers.Add(new NBSLayer(name, locked, volume, panning));
            }

            return layers;
        }

        static List<NBSCustomInstrument> ReadCustomInstruments
        (
            BinaryReader reader
        )
        {
            List<NBSCustomInstrument> instruments = [];
            int count;
            try
            {
                count = reader.ReadByte();
            }
            catch (EndOfStreamException)
            {
                return instruments;
            }

            instruments.Capacity = count;
            for (int i = 0; i < count; i++)
            {
                string name = ReadString(reader);
                string soundFile = ReadString(reader);
                byte key = reader.ReadByte();
                bool pressKey = reader.ReadByte() != 0;
                if (key > 87)
                    throw new InvalidDataException($"NBS custom instrument {i} contains invalid key {key}.");

                instruments.Add(new NBSCustomInstrument(name, soundFile, key, pressKey));
            }

            return instruments;
        }

        static void ValidateCoordinatesAndInstruments
        (
            IEnumerable<NBSNote> notes,
            ushort layerCount,
            byte vanillaInstrumentCount,
            IReadOnlyList<NBSCustomInstrument> customInstruments
        )
        {
            int instrumentCount = vanillaInstrumentCount + customInstruments.Count;
            foreach (NBSNote note in notes)
            {
                if (note.layer < 0 || note.layer >= layerCount)
                    throw new InvalidDataException($"NBS note at tick {note.tick} refers to layer {note.layer}, but the file contains {layerCount} layers.");
                if (note.instrument >= instrumentCount)
                    throw new InvalidDataException($"NBS note at tick {note.tick}, layer {note.layer} refers to missing instrument {note.instrument}, but the file contains {instrumentCount} instruments.");
            }
        }

        static List<NBSSpecialEvent> ParseSpecialEvents
        (
            byte vanillaInstrumentCount,
            IEnumerable<NBSNote> notes,
            IReadOnlyList<NBSCustomInstrument> customInstruments
        )
        {
            List<NBSSpecialEvent> result = [];
            foreach (NBSNote note in notes)
            {
                int customIndex = note.instrument - vanillaInstrumentCount;
                if (customIndex < 0 || customIndex >= customInstruments.Count)
                    continue;

                string name = customInstruments[customIndex].name.Trim();
                if (name.Equals("Tempo Changer", StringComparison.OrdinalIgnoreCase))
                {
                    double bpm = note.pitch.Abs();
                    if (bpm == 0)
                        Debug.RuntimeLogWarning($"Ignored a zero BPM Tempo Changer at tick {note.tick}, layer {note.layer}.", nameof(NBSReader));

                    result.Add(new NBSSpecialEvent(NBSSpecialEventKind.tempoChange, note.tick, note.layer, bpm));
                }
                else if (name.Equals("Sound Stopper", StringComparison.OrdinalIgnoreCase))
                {
                    int startLayer = MathUtility.Max((short)0, note.pitch);
                    int packedEndLayer = (note.velocity << 8) | note.panning;
                    int endLayer = packedEndLayer == 0 ? int.MaxValue : packedEndLayer;
                    result.Add(new NBSSpecialEvent(NBSSpecialEventKind.soundStop, note.tick, note.layer, 0, startLayer, endLayer));
                }
                else if (name.Equals("Toggle Rainbow", StringComparison.OrdinalIgnoreCase))
                    result.Add(new NBSSpecialEvent(NBSSpecialEventKind.toggleRainbow, note.tick, note.layer));
                else if (name.Equals("Show Save Popup", StringComparison.OrdinalIgnoreCase))
                    result.Add(new NBSSpecialEvent(NBSSpecialEventKind.showSavePopup, note.tick, note.layer));
                else if (name.Equals("Toggle Background Accent", StringComparison.OrdinalIgnoreCase))
                    result.Add(new NBSSpecialEvent(NBSSpecialEventKind.toggleBackgroundAccent, note.tick, note.layer));
                else if (TryParseMainColor(name, out HexColor color))
                    result.Add(new NBSSpecialEvent(NBSSpecialEventKind.changeMainColor, note.tick, note.layer, color: color));
            }

            return result;
        }

        static bool TryParseMainColor(string name, out HexColor color)
        {
            const string prefix = "Change Color to ";
            color = default;
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || name.Length != prefix.Length + 7)
                return false;

            if (HexColor.TryParse(name.Substring(prefix.Length), out Color32 result))
            {
                color = result;
                return true;
            }

            return false;
        }

        static string ReadString(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length < 0 || length > maximumStringBytes)
                throw new InvalidDataException($"Invalid NBS string byte length: {length}.");

            byte[] bytes = reader.ReadBytes(length);
            if (bytes.Length != length)
                throw new EndOfStreamException();

            try
            {
                return strictUtf8.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return DecodeWindows1252(bytes);
            }
        }

        static ushort ReadNonNegativeShort
        (
            BinaryReader reader,
            string fieldName
        )
        {
            short value = reader.ReadInt16();
            if (value < 0)
                throw new InvalidDataException($"NBS {fieldName} cannot be negative: {value}.");

            return (ushort)value;
        }

        static int AddCoordinateJump(int current, ushort jump, string coordinateName)
        {
            long result = (long)current + jump;
            if (result > int.MaxValue)
                throw new InvalidDataException($"NBS {coordinateName} exceeds Int32: current value {current}, jump {jump}, result {result}.");

            return (int)result;
        }

        static string GetStreamPositionDescription(Stream stream)
        {
            try
            {
                if (!stream.CanSeek)
                    return string.Empty;

                return $" at byte offset {stream.Position} of {stream.Length}";
            }
            catch (IOException)
            {
                return string.Empty;
            }
            catch (NotSupportedException)
            {
                return string.Empty;
            }
            catch (ObjectDisposedException)
            {
                return string.Empty;
            }
        }

        static string DecodeWindows1252(byte[] bytes)
        {
            char[] characters = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
                characters[i] = DecodeWindows1252Byte(bytes[i]);

            return new string(characters);
        }

        static char DecodeWindows1252Byte(byte value) => value switch
        {
            0x80 => '\u20ac', // EURO SIGN
            0x82 => '\u201a', // SINGLE LOW-9 QUOTATION MARK
            0x83 => '\u0192', // LATIN SMALL LETTER F WITH HOOK
            0x84 => '\u201e', // DOUBLE LOW-9 QUOTATION MARK
            0x85 => '\u2026', // HORIZONTAL ELLIPSIS
            0x86 => '\u2020', // DAGGER
            0x87 => '\u2021', // DOUBLE DAGGER
            0x88 => '\u02c6', // MODIFIER LETTER CIRCUMFLEX ACCENT
            0x89 => '\u2030', // PER MILLE SIGN
            0x8a => '\u0160', // LATIN CAPITAL LETTER S WITH CARON
            0x8b => '\u2039', // SINGLE LEFT-POINTING ANGLE QUOTATION MARK
            0x8c => '\u0152', // LATIN CAPITAL LIGATURE OE
            0x8e => '\u017d', // LATIN CAPITAL LETTER Z WITH CARON
            0x91 => '\u2018', // LEFT SINGLE QUOTATION MARK
            0x92 => '\u2019', // RIGHT SINGLE QUOTATION MARK
            0x93 => '\u201c', // LEFT DOUBLE QUOTATION MARK
            0x94 => '\u201d', // RIGHT DOUBLE QUOTATION MARK
            0x95 => '\u2022', // BULLET
            0x96 => '\u2013', // EN DASH
            0x97 => '\u2014', // EM DASH
            0x98 => '\u02dc', // SMALL TILDE
            0x99 => '\u2122', // TRADE MARK SIGN
            0x9a => '\u0161', // LATIN SMALL LETTER S WITH CARON
            0x9b => '\u203a', // SINGLE RIGHT-POINTING ANGLE QUOTATION MARK
            0x9c => '\u0153', // LATIN SMALL LIGATURE OE
            0x9e => '\u017e', // LATIN SMALL LETTER Z WITH CARON
            0x9f => '\u0178', // LATIN CAPITAL LETTER Y WITH DIAERESIS
            _ => (char)value
        };
    }
}
