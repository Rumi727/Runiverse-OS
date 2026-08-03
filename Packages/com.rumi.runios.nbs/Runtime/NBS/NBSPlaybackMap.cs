#nullable enable
using RuniOS.Resource;
// ReSharper disable NotAccessedPositionalProperty.Global

namespace RuniOS.NBS
{
    /// <summary>Identifies a prepared playback entry type.<br/>준비된 재생 항목 타입을 식별합니다.</summary>
    public enum NBSPlaybackEntryKind
    {
        note,
        soundStop
    }

    /// <summary>Identifies the NBS timeline traversal direction.<br/>NBS 타임라인 진행 방향을 식별합니다.</summary>
    public enum NBSPlaybackDirection
    {
        forward,
        reverse
    }

    /// <summary>Identifies a command emitted by a playback schedule.<br/>재생 스케줄에서 생성된 명령을 식별합니다.</summary>
    public enum NBSPlaybackCommandKind
    {
        note,
        soundStop
    }

    /// <summary>
    /// Stores an instrument identifier without owning its clip or resource scope.<br/>
    /// 클립이나 리소스 스코프를 소유하지 않고 악기 식별 정보를 저장합니다.
    /// </summary>
    public readonly record struct NBSInstrumentReference
    {
        static readonly Identifier waveRegistryId = new Identifier("runios", "waves");

        NBSInstrumentReference(bool isFunctional, bool usesSongNamespace, Identifier fixedAssetId, string relativePath)
        {
            this.isFunctional = isFunctional;
            this.usesSongNamespace = usesSongNamespace;
            this.fixedAssetId = fixedAssetId;
            _relativePath = relativePath;
        }

        /// <summary>Gets whether the reference represents a functional instrument with no audio clip.<br/>오디오 클립이 없는 기능성 악기를 나타내는지 여부를 가져옵니다.</summary>
        public bool isFunctional { get; }

        /// <summary>Gets whether the NBS asset namespace is used while resolving the reference.<br/>참조를 확인할 때 NBS 에셋 네임스페이스를 사용하는지 여부를 가져옵니다.</summary>
        public bool usesSongNamespace { get; }

        /// <summary>Gets the fixed asset identifier used by vanilla instruments.<br/>기본 악기가 사용하는 고정 에셋 식별자를 가져옵니다.</summary>
        public Identifier fixedAssetId { get; }

        /// <summary>Gets the normalized custom-instrument path without its file extension.<br/>파일 확장자를 제외한 정규화된 커스텀 악기 경로를 가져옵니다.</summary>
        public string relativePath => _relativePath ?? string.Empty;
        readonly string? _relativePath;

        /// <summary>Gets whether this reference can resolve an audio resource.<br/>이 참조가 오디오 리소스를 확인할 수 있는지 여부를 가져옵니다.</summary>
        public bool isValid => !isFunctional && (usesSongNamespace ? relativePath.Length > 0 : fixedAssetId.path.length > 0);

        internal static NBSInstrumentReference Functional() => new NBSInstrumentReference(true, false, default, string.Empty);
        internal static NBSInstrumentReference Vanilla(Identifier assetId) => new NBSInstrumentReference(false, false, assetId, string.Empty);
        internal static NBSInstrumentReference Custom(string relativePath) => new NBSInstrumentReference(false, true, default, relativePath);

        /// <summary>
        /// Resolves this reference for the NBS asset identified by <paramref name="nbsAssetId"/>.<br/>
        /// <paramref name="nbsAssetId"/>로 식별되는 NBS 에셋을 기준으로 이 참조를 확인합니다.
        /// </summary>
        /// <param name="nbsAssetId">The owning NBS asset identifier.<br/>소유 NBS 에셋 식별자입니다.</param>
        /// <returns>The resolved wave resource key.<br/>확인된 웨이브 리소스 키입니다.</returns>
        /// <exception cref="InvalidOperationException">Thrown when this reference does not identify an audio resource.<br/>이 참조가 오디오 리소스를 식별하지 않는 경우 발생합니다.</exception>
        public ResourceKey Resolve(Identifier nbsAssetId)
        {
            if (!isValid)
                throw new InvalidOperationException("The NBS instrument reference does not identify an audio resource.");

            Identifier assetId = usesSongNamespace
                ? new Identifier(nbsAssetId.nameSpace, relativePath)
                : fixedAssetId;
            return new ResourceKey(waveRegistryId, assetId);
        }

        /// <summary>
        /// Normalizes a custom-instrument file path for resource lookup.<br/>
        /// 리소스 조회에 사용할 커스텀 악기 파일 경로를 정규화합니다.
        /// </summary>
        /// <param name="soundFile">The path stored in the NBS file.<br/>NBS 파일에 저장된 경로입니다.</param>
        /// <param name="path">The normalized extensionless path when successful.<br/>성공한 경우 정규화된 확장자 없는 경로입니다.</param>
        /// <returns><see langword="true"/> when the path is valid; otherwise, <see langword="false"/>.<br/>경로가 유효하면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.</returns>
        public static bool TryNormalizeCustomPath(string soundFile, out string path)
        {
            path = soundFile.Trim().Replace('\\', '/');
            if (path.StartsWith('/') || path.Contains(':'))
                return false;

            if (path.StartsWith("sounds/", StringComparison.OrdinalIgnoreCase))
                path = path.Substring("sounds/".Length);

            string[] parts = path.Split('/');
            if (parts.Length == 0 || parts.Any(static x => string.IsNullOrWhiteSpace(x) || x == "." || x == ".."))
                return false;

            int lastSlash = path.LastIndexOf('/');
            int lastDot = path.LastIndexOf('.');
            if (lastDot > lastSlash)
                path = path.Substring(0, lastDot);

            return path.Length > 0;
        }
    }

    /// <summary>Stores one immutable playback-map entry.<br/>불변 재생 맵 항목 하나를 저장합니다.</summary>
    public readonly record struct NBSPlaybackEntry
    (
        int id,
        double originalTime,
        int layer,
        NBSPlaybackEntryKind kind,
        NBSInstrumentReference instrument,
        double staticPitchRatio,
        float staticVolume,
        float staticPan,
        NBSNote note,
        NBSSpecialEvent specialEvent
    );

    /// <summary>Provides clip length metadata without transferring clip ownership.<br/>클립 소유권을 이전하지 않고 클립 길이 메타데이터를 제공합니다.</summary>
    public interface INBSClipMetadataProvider
    {
        /// <summary>
        /// Tries to get the original clip length for <paramref name="instrument"/>.<br/>
        /// <paramref name="instrument"/>의 원본 클립 길이를 가져오려고 시도합니다.
        /// </summary>
        /// <param name="instrument">The instrument to inspect.<br/>확인할 악기입니다.</param>
        /// <param name="length">The clip length in seconds when available.<br/>사용 가능한 경우 초 단위 클립 길이입니다.</param>
        /// <returns><see langword="true"/> when valid metadata is available; otherwise, <see langword="false"/>.<br/>유효한 메타데이터를 사용할 수 있으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.</returns>
        bool TryGetLength(NBSInstrumentReference instrument, out double length);
    }

    /// <summary>
    /// Stores static, clip-independent playback data derived from an NBS file.<br/>
    /// NBS 파일에서 파생된 클립 독립적 정적 재생 데이터를 저장합니다.
    /// </summary>
    public sealed class NBSPlaybackMap
    {
        static readonly string[] vanillaInstrumentPaths =
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

        internal NBSPlaybackMap
        (
            NBSHeader header,
            IReadOnlyList<NBSLayer> layers,
            IReadOnlyList<NBSCustomInstrument> customInstruments,
            NBSNoteMap noteMap
        )
        {
            Dictionary<(int tick, int layer), NBSSpecialEvent> eventsByCoordinate = [];
            for (int i = 0; i < noteMap.specialEvents.Count; i++)
            {
                NBSSpecialEvent specialEvent = noteMap.specialEvents[i].specialEvent;
                eventsByCoordinate[(specialEvent.tick, specialEvent.layer)] = specialEvent;
            }

            List<NBSPlaybackEntry> result = [];
            for (int i = 0; i < noteMap.notes.Count; i++)
            {
                NBSMappedNote mapped = noteMap.notes[i];
                NBSNote note = mapped.note;
                if (eventsByCoordinate.TryGetValue((note.tick, note.layer), out NBSSpecialEvent specialEvent))
                {
                    if (specialEvent.kind == NBSSpecialEventKind.soundStop)
                    {
                        result.Add(new NBSPlaybackEntry
                        (
                            mapped.id,
                            mapped.time,
                            note.layer,
                            NBSPlaybackEntryKind.soundStop,
                            NBSInstrumentReference.Functional(),
                            0,
                            0,
                            0,
                            note,
                            specialEvent
                        ));
                    }

                    continue;
                }

                NBSInstrumentReference instrument;
                int instrumentKeyOffset;
                if (note.instrument < header.vanillaInstrumentCount)
                {
                    instrumentKeyOffset = 0;
                    instrument = note.instrument < vanillaInstrumentPaths.Length
                        ? NBSInstrumentReference.Vanilla(new Identifier("runios", vanillaInstrumentPaths[note.instrument]))
                        : default;
                }
                else
                {
                    int customIndex = note.instrument - header.vanillaInstrumentCount;
                    if (customIndex < 0 || customIndex >= customInstruments.Count)
                        continue;

                    NBSCustomInstrument custom = customInstruments[customIndex];
                    if (custom.IsFunctionalInstrument())
                        continue;

                    instrumentKeyOffset = custom.key - 45;
                    instrument = NBSInstrumentReference.TryNormalizeCustomPath(custom.soundFile, out string path)
                        ? NBSInstrumentReference.Custom(path)
                        : default;
                }

                NBSLayer layer = layers[note.layer];
                double semitones = ((note.key + instrumentKeyOffset) - 45) + (note.pitch / 100d);
                double staticPitchRatio = Math.Pow(2, semitones / 12d);
                float staticVolume = (note.velocity / 100f) * (layer.volume / 100f);
                float combinedNbsPan = layer.panning == 100 ? note.panning : (layer.panning + note.panning) * 0.5f;
                float staticPan = (combinedNbsPan - 100) / 100f;

                result.Add(new NBSPlaybackEntry
                (
                    mapped.id,
                    mapped.time,
                    note.layer,
                    NBSPlaybackEntryKind.note,
                    instrument,
                    staticPitchRatio,
                    staticVolume,
                    staticPan,
                    note,
                    default
                ));
            }

            entries = result.AsReadOnly();
        }

        /// <summary>Gets playback entries in stable original order.<br/>안정적인 원본 순서의 재생 항목을 가져옵니다.</summary>
        public IReadOnlyList<NBSPlaybackEntry> entries { get; }

        /// <summary>
        /// Creates a Player-specific schedule using current transport rates and clip metadata.<br/>
        /// 현재 트랜스포트 속도와 클립 메타데이터를 사용하여 Player별 스케줄을 생성합니다.
        /// </summary>
        /// <param name="tempo">The signed NBS timeline rate.<br/>부호 있는 NBS 타임라인 속도입니다.</param>
        /// <param name="pitch">The signed clip PCM rate multiplier.<br/>부호 있는 클립 PCM 속도 배율입니다.</param>
        /// <param name="clipMetadata">The clip metadata provider.<br/>클립 메타데이터 프로바이더입니다.</param>
        /// <returns>A newly prepared immutable playback schedule.<br/>새로 준비된 불변 재생 스케줄입니다.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="clipMetadata"/> is <see langword="null"/>.<br/><paramref name="clipMetadata"/>가 <see langword="null"/>인 경우 발생합니다.</exception>
        public NBSPlaybackSchedule CreateSchedule(float tempo, float pitch, INBSClipMetadataProvider clipMetadata)
        {
            if (clipMetadata == null)
                throw new ArgumentNullException(nameof(clipMetadata));

            return new NBSPlaybackSchedule(entries, tempo, pitch, clipMetadata);
        }
    }

    /// <summary>Stores one note with its Player-specific interval and source direction.<br/>Player별 구간 및 소스 방향과 함께 음표 하나를 저장합니다.</summary>
    public readonly record struct NBSPreparedNote
    (
        int mapEntryId,
        double originalStartTime,
        double originalEndTime,
        double anchorTime,
        double timelineDuration,
        double sourceLength,
        bool reverseSource,
        int layer,
        NBSInstrumentReference instrument,
        double staticPitchRatio,
        float staticVolume,
        float staticPan,
        int momentIndex,
        int entryIndex
    );

    /// <summary>Stores one prepared note or Sound Stopper entry.<br/>준비된 음표 또는 Sound Stopper 항목 하나를 저장합니다.</summary>
    public readonly record struct NBSPreparedEntry
    (
        NBSPlaybackEntryKind kind,
        int mapEntryId,
        NBSPreparedNote note,
        int stopStartLayer,
        int stopEndLayer
    );

    /// <summary>Groups prepared entries that share one timeline anchor.<br/>하나의 타임라인 anchor를 공유하는 준비된 항목을 묶습니다.</summary>
    public readonly record struct NBSPlaybackMoment(double anchorTime, IReadOnlyList<NBSPreparedEntry> entries);

    /// <summary>Uniquely identifies one schedule occurrence.<br/>스케줄 occurrence 하나를 고유하게 식별합니다.</summary>
    public readonly record struct NBSOccurrenceId(long scheduleGeneration, long loopIteration, int momentIndex, int entryIndex);

    /// <summary>Stores one loop-relative transport position.<br/>루프 기준 트랜스포트 위치 하나를 저장합니다.</summary>
    public readonly record struct NBSPlaybackPosition(double fileTime, long loopIteration);

    /// <summary>Stores the active loop range and repeat limit.<br/>활성 루프 범위와 반복 제한을 저장합니다.</summary>
    public readonly record struct NBSLoopInfo(bool enabled, double startTime, double endTime, long maximumLoops = 0)
    {
        /// <summary>Gets the loop duration.<br/>루프 길이를 가져옵니다.</summary>
        public double range => endTime - startTime;

        internal bool IsUsable => enabled && double.IsFinite(startTime) && double.IsFinite(endTime) && range > 0;
        internal bool CanUseIteration(long iteration) => iteration >= 0 && (!IsUsable || maximumLoops <= 0 || iteration <= maximumLoops);
    }

    /// <summary>Stores immutable inputs for one playback-schedule query.<br/>재생 스케줄 조회 하나의 불변 입력을 저장합니다.</summary>
    public readonly record struct NBSPlaybackQueryContext
    (
        NBSPlaybackPosition currentPosition,
        double schedulingLookahead,
        float tempo,
        float pitch,
        NBSLoopInfo loopInfo
    );

    /// <summary>Points to the first schedule moment that has not been submitted.<br/>아직 제출되지 않은 첫 스케줄 moment를 가리킵니다.</summary>
    public struct NBSPlaybackCursor
    {
        public int momentIndex;
        public long loopIteration;
        public long scheduleGeneration;
        public NBSPlaybackDirection direction;
        public bool initialized;
    }

    /// <summary>Stores one note or Sound Stopper command emitted by a schedule query.<br/>스케줄 조회에서 생성된 음표 또는 Sound Stopper 명령 하나를 저장합니다.</summary>
    public readonly record struct NBSPlaybackCommand
    (
        NBSOccurrenceId occurrence,
        NBSPlaybackCommandKind kind,
        double wallDelay,
        double sourceOffset,
        NBSPreparedNote note,
        int stopStartLayer,
        int stopEndLayer
    );

    /// <summary>
    /// Stores immutable Player-specific playback moments and an interval index for snapshot restoration.<br/>
    /// 불변 Player별 재생 moment와 snapshot 복원용 구간 인덱스를 저장합니다.
    /// </summary>
    public sealed class NBSPlaybackSchedule
    {
        readonly record struct PendingEntry(double anchorTime, int mapOrder, NBSPlaybackEntry source, NBSPreparedNote note);

        internal NBSPlaybackSchedule(IReadOnlyList<NBSPlaybackEntry> mapEntries, float tempo, float pitch, INBSClipMetadataProvider clipMetadata)
        {
            this.tempo = tempo;
            this.pitch = pitch;
            direction = tempo < 0 ? NBSPlaybackDirection.reverse : NBSPlaybackDirection.forward;

            List<PendingEntry> pending = [];
            double absoluteTempo = Math.Abs((double)tempo);
            double absolutePitch = Math.Abs((double)pitch);
            for (int i = 0; i < mapEntries.Count; i++)
            {
                NBSPlaybackEntry entry = mapEntries[i];
                if (entry.kind == NBSPlaybackEntryKind.soundStop)
                {
                    pending.Add(new PendingEntry(entry.originalTime, entry.id, entry, default));
                    continue;
                }

                if (!entry.instrument.isValid || entry.staticVolume <= 0 ||
                    !double.IsFinite(entry.staticPitchRatio) || entry.staticPitchRatio <= 0 ||
                    !double.IsFinite(absoluteTempo) || absoluteTempo <= 0 ||
                    !double.IsFinite(absolutePitch) || absolutePitch <= 0 ||
                    !clipMetadata.TryGetLength(entry.instrument, out double sourceLength) ||
                    !double.IsFinite(sourceLength) || sourceLength <= 0)
                    continue;

                double timelineDuration = (sourceLength * absoluteTempo) / (entry.staticPitchRatio * absolutePitch);
                double endTime = entry.originalTime + timelineDuration;
                if (!double.IsFinite(timelineDuration) || timelineDuration <= 0 || !double.IsFinite(endTime))
                    continue;

                double anchor = direction == NBSPlaybackDirection.reverse ? endTime : entry.originalTime;
                NBSPreparedNote note = new NBSPreparedNote
                (
                    entry.id,
                    entry.originalTime,
                    endTime,
                    anchor,
                    timelineDuration,
                    sourceLength,
                    pitch < 0,
                    entry.layer,
                    entry.instrument,
                    entry.staticPitchRatio,
                    entry.staticVolume,
                    entry.staticPan,
                    -1,
                    -1
                );
                pending.Add(new PendingEntry(anchor, entry.id, entry, note));
            }

            pending.Sort(static (left, right) =>
            {
                int anchorComparison = left.anchorTime.CompareTo(right.anchorTime);
                return anchorComparison != 0 ? anchorComparison : left.mapOrder.CompareTo(right.mapOrder);
            });

            List<NBSPlaybackMoment> momentList = [];
            List<NBSPreparedNote> noteList = [];
            int pendingIndex = 0;
            while (pendingIndex < pending.Count)
            {
                int momentIndex = momentList.Count;
                double anchor = pending[pendingIndex].anchorTime;
                int endIndex = pendingIndex + 1;
                while (endIndex < pending.Count && pending[endIndex].anchorTime.Equals(anchor))
                    endIndex++;

                NBSPreparedEntry[] preparedEntries = new NBSPreparedEntry[endIndex - pendingIndex];
                for (int i = pendingIndex; i < endIndex; i++)
                {
                    PendingEntry item = pending[i];
                    int entryIndex = i - pendingIndex;
                    if (item.source.kind == NBSPlaybackEntryKind.note)
                    {
                        NBSPreparedNote note = item.note with { momentIndex = momentIndex, entryIndex = entryIndex };
                        preparedEntries[entryIndex] = new NBSPreparedEntry(NBSPlaybackEntryKind.note, item.source.id, note, 0, 0);
                        noteList.Add(note);
                    }
                    else
                    {
                        preparedEntries[entryIndex] = new NBSPreparedEntry
                        (
                            NBSPlaybackEntryKind.soundStop,
                            item.source.id,
                            default,
                            item.source.specialEvent.startLayer,
                            item.source.specialEvent.endLayer
                        );
                    }
                }

                momentList.Add(new NBSPlaybackMoment(anchor, Array.AsReadOnly(preparedEntries)));
                pendingIndex = endIndex;
            }

            moments = momentList.AsReadOnly();
            preparedNotes = noteList.OrderBy(static x => x.originalStartTime).ThenBy(static x => x.mapEntryId).ToArray();
            maximumTimelineDuration = preparedNotes.Length == 0 ? 0 : preparedNotes.Max(static x => x.timelineDuration);

            intervalTreeBase = 1;
            while (intervalTreeBase < preparedNotes.Length)
                intervalTreeBase <<= 1;

            intervalMaximumEnds = new double[intervalTreeBase * 2];
            Array.Fill(intervalMaximumEnds, double.NegativeInfinity);
            for (int i = 0; i < preparedNotes.Length; i++)
                intervalMaximumEnds[intervalTreeBase + i] = preparedNotes[i].originalEndTime;
            for (int i = intervalTreeBase - 1; i > 0; i--)
                intervalMaximumEnds[i] = Math.Max(intervalMaximumEnds[i * 2], intervalMaximumEnds[(i * 2) + 1]);
        }

        readonly NBSPreparedNote[] preparedNotes;
        readonly int intervalTreeBase;
        readonly double[] intervalMaximumEnds;

        /// <summary>Gets prepared moments in ascending anchor order.<br/>anchor 오름차순으로 준비된 moment를 가져옵니다.</summary>
        public IReadOnlyList<NBSPlaybackMoment> moments { get; }

        /// <summary>Gets the signed tempo used to create this schedule.<br/>이 스케줄 생성에 사용된 부호 있는 tempo를 가져옵니다.</summary>
        public float tempo { get; }

        /// <summary>Gets the signed pitch used to create this schedule.<br/>이 스케줄 생성에 사용된 부호 있는 pitch를 가져옵니다.</summary>
        public float pitch { get; }

        /// <summary>Gets the timeline traversal direction.<br/>타임라인 진행 방향을 가져옵니다.</summary>
        public NBSPlaybackDirection direction { get; }

        /// <summary>Gets the largest prepared note duration in NBS timeline seconds.<br/>가장 긴 준비된 음표 길이를 NBS 타임라인 초 단위로 가져옵니다.</summary>
        public double maximumTimelineDuration { get; }

        /// <summary>
        /// Creates a cursor at <paramref name="position"/>.<br/>
        /// <paramref name="position"/>에 커서를 생성합니다.
        /// </summary>
        /// <param name="position">The current loop-relative transport position.<br/>현재 루프 기준 트랜스포트 위치입니다.</param>
        /// <param name="loopInfo">The active loop configuration.<br/>활성 루프 설정입니다.</param>
        /// <param name="scheduleGeneration">The owning Player schedule generation.<br/>소유 Player의 스케줄 세대입니다.</param>
        /// <param name="includeCurrent">Whether a moment at the exact position is included.<br/>정확히 현재 위치에 있는 moment를 포함할지 여부입니다.</param>
        /// <returns>The initialized playback cursor.<br/>초기화된 재생 커서입니다.</returns>
        public NBSPlaybackCursor CreateCursor
        (
            NBSPlaybackPosition position,
            NBSLoopInfo loopInfo,
            long scheduleGeneration,
            bool includeCurrent
        )
        {
            int momentIndex = direction == NBSPlaybackDirection.forward
                ? FindForwardMoment(position.fileTime, includeCurrent)
                : FindReverseMoment(position.fileTime, includeCurrent);
            NBSPlaybackCursor cursor = new NBSPlaybackCursor
            {
                momentIndex = momentIndex,
                loopIteration = loopInfo.IsUsable ? Math.Max(0, position.loopIteration) : 0,
                scheduleGeneration = scheduleGeneration,
                direction = direction,
                initialized = true
            };
            NormalizeCursor(ref cursor, loopInfo);
            return cursor;
        }

        /// <summary>
        /// Queries commands from <paramref name="cursor"/> through the configured lookahead range.<br/>
        /// <paramref name="cursor"/>부터 설정된 lookahead 범위까지 명령을 조회합니다.
        /// </summary>
        /// <param name="cursor">The first unsubmitted occurrence.<br/>첫 미제출 occurrence입니다.</param>
        /// <param name="context">The current transport and loop state.<br/>현재 트랜스포트 및 루프 상태입니다.</param>
        /// <param name="includePreviousNotes">Whether active tails before the cursor are restored.<br/>커서 이전의 활성 tail을 복원할지 여부입니다.</param>
        /// <param name="output">The destination command list.<br/>명령을 저장할 대상 목록입니다.</param>
        /// <param name="nextCursor">The cursor after every returned or consumed moment.<br/>반환되거나 소비된 모든 moment 다음의 커서입니다.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="output"/> is <see langword="null"/>.<br/><paramref name="output"/>이 <see langword="null"/>인 경우 발생합니다.</exception>
        public void Query
        (
            NBSPlaybackCursor cursor,
            in NBSPlaybackQueryContext context,
            bool includePreviousNotes,
            List<NBSPlaybackCommand> output,
            out NBSPlaybackCursor nextCursor
        )
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            nextCursor = cursor;
            if (moments.Count == 0 || !double.IsFinite(context.currentPosition.fileTime) ||
                !double.IsFinite(context.schedulingLookahead) || context.schedulingLookahead < 0 ||
                !float.IsFinite(context.tempo) || context.tempo == 0 ||
                Math.Sign(context.tempo) != (direction == NBSPlaybackDirection.forward ? 1 : -1))
                return;

            NBSLoopInfo loopInfo = SanitizeLoop(context.loopInfo);
            double currentUnwrapped = GetUnwrappedPosition(context.currentPosition, loopInfo, direction);
            double absoluteTempo = Math.Abs((double)context.tempo);

            if (includePreviousNotes)
                AddPreviousNotes(context.currentPosition, loopInfo, currentUnwrapped, cursor.scheduleGeneration, output);

            if (!cursor.initialized || cursor.scheduleGeneration < 0 || cursor.direction != direction)
                return;

            NBSPlaybackCursor working = cursor;
            NormalizeCursor(ref working, loopInfo);
            int safety = 0;
            while (working.initialized && safety++ < 1_000_000)
            {
                if (!TryGetMomentOccurrence(working, loopInfo, out NBSPlaybackMoment moment, out double occurrenceAnchor))
                    break;

                double signedDistance = direction == NBSPlaybackDirection.forward
                    ? occurrenceAnchor - currentUnwrapped
                    : currentUnwrapped - occurrenceAnchor;
                double wallDelay = signedDistance / absoluteTempo;
                if (wallDelay > context.schedulingLookahead)
                    break;

                for (int entryIndex = 0; entryIndex < moment.entries.Count; entryIndex++)
                {
                    NBSPreparedEntry entry = moment.entries[entryIndex];
                    if (!IsEntryAllowed(entry, moment.anchorTime, working.loopIteration, loopInfo))
                        continue;

                    NBSOccurrenceId occurrence = new NBSOccurrenceId
                    (
                        cursor.scheduleGeneration,
                        working.loopIteration,
                        working.momentIndex,
                        entryIndex
                    );

                    if (entry.kind == NBSPlaybackEntryKind.soundStop)
                    {
                        if (ContainsOccurrence(output, occurrence))
                            continue;

                        output.Add(new NBSPlaybackCommand
                        (
                            occurrence,
                            NBSPlaybackCommandKind.soundStop,
                            Math.Max(0, wallDelay),
                            0,
                            default,
                            entry.stopStartLayer,
                            entry.stopEndLayer
                        ));
                        continue;
                    }

                    double sourceOffset;
                    if (ContainsOccurrence(output, occurrence))
                        continue;

                    if (wallDelay >= 0)
                        sourceOffset = entry.note.reverseSource ? entry.note.sourceLength : 0;
                    else if (WasStopped(entry.note, working.loopIteration, currentUnwrapped, loopInfo) ||
                        !TryGetSourceOffset(entry.note, working.loopIteration, currentUnwrapped, loopInfo, out sourceOffset))
                        continue;

                    output.Add(new NBSPlaybackCommand
                    (
                        occurrence,
                        NBSPlaybackCommandKind.note,
                        Math.Max(0, wallDelay),
                        sourceOffset,
                        entry.note,
                        0,
                        0
                    ));
                }

                AdvanceCursor(ref working, loopInfo);
            }

            nextCursor = working;
        }

        void AddPreviousNotes
        (
            NBSPlaybackPosition currentPosition,
            NBSLoopInfo loopInfo,
            double currentUnwrapped,
            long scheduleGeneration,
            List<NBSPlaybackCommand> output
        )
        {
            long currentIteration = Math.Max(0, currentPosition.loopIteration);
            long firstIteration;
            if (loopInfo.IsUsable)
            {
                double priorIterationCount = Math.Ceiling(maximumTimelineDuration / loopInfo.range) + 1;
                long requiredIterations = priorIterationCount >= long.MaxValue ? long.MaxValue : (long)priorIterationCount;
                firstIteration = Math.Max(0, currentIteration - requiredIterations);
            }
            else
            {
                firstIteration = 0;
                currentIteration = 0;
            }

            List<NBSPreparedNote> candidates = [];
            for (long iteration = firstIteration; iteration <= currentIteration; iteration++)
            {
                double localPosition = currentUnwrapped - GetIterationShift(iteration, loopInfo, direction);
                candidates.Clear();
                CollectNotesAtTime(1, 0, intervalTreeBase, localPosition, candidates);
                for (int i = 0; i < candidates.Count; i++)
                {
                    NBSPreparedNote note = candidates[i];
                    if (!IsNoteAllowed(note, iteration, loopInfo) ||
                        WasStopped(note, iteration, currentUnwrapped, loopInfo))
                        continue;

                    NBSOccurrenceId occurrence = new NBSOccurrenceId
                    (
                        scheduleGeneration,
                        iteration,
                        note.momentIndex,
                        note.entryIndex
                    );
                    if (ContainsOccurrence(output, occurrence) ||
                        !TryGetSourceOffset(note, iteration, currentUnwrapped, loopInfo, out double sourceOffset))
                        continue;

                    output.Add(new NBSPlaybackCommand
                    (
                        occurrence,
                        NBSPlaybackCommandKind.note,
                        0,
                        sourceOffset,
                        note,
                        0,
                        0
                    ));
                }

                if (iteration == long.MaxValue)
                    break;
            }
        }

        void CollectNotesAtTime(int node, int rangeStart, int rangeEnd, double time, List<NBSPreparedNote> output)
        {
            if (rangeStart >= preparedNotes.Length || intervalMaximumEnds[node] < time || preparedNotes[rangeStart].originalStartTime > time)
                return;

            if (rangeEnd - rangeStart == 1)
            {
                NBSPreparedNote note = preparedNotes[rangeStart];
                if (note.originalStartTime <= time && time < note.originalEndTime)
                    output.Add(note);
                return;
            }

            int middle = (rangeStart + rangeEnd) / 2;
            CollectNotesAtTime(node * 2, rangeStart, middle, time, output);
            CollectNotesAtTime((node * 2) + 1, middle, rangeEnd, time, output);
        }

        bool WasStopped(NBSPreparedNote note, long noteIteration, double currentUnwrapped, NBSLoopInfo loopInfo)
        {
            double noteAnchor = note.anchorTime + GetIterationShift(noteIteration, loopInfo, direction);
            long currentIteration = GetIterationFromUnwrapped(currentUnwrapped, loopInfo, direction, noteIteration);
            for (long iteration = noteIteration; iteration <= currentIteration; iteration++)
            {
                double shift = GetIterationShift(iteration, loopInfo, direction);
                for (int momentIndex = 0; momentIndex < moments.Count; momentIndex++)
                {
                    NBSPlaybackMoment moment = moments[momentIndex];
                    double stopperAnchor = moment.anchorTime + shift;
                    double fromNote = direction == NBSPlaybackDirection.forward ? stopperAnchor - noteAnchor : noteAnchor - stopperAnchor;
                    double toCurrent = direction == NBSPlaybackDirection.forward ? currentUnwrapped - stopperAnchor : stopperAnchor - currentUnwrapped;
                    if (fromNote < 0 || toCurrent < 0)
                        continue;

                    for (int entryIndex = 0; entryIndex < moment.entries.Count; entryIndex++)
                    {
                        NBSPreparedEntry entry = moment.entries[entryIndex];
                        if (entry.kind != NBSPlaybackEntryKind.soundStop ||
                            !IsEntryAllowed(entry, moment.anchorTime, iteration, loopInfo) ||
                            note.layer < entry.stopStartLayer || note.layer > entry.stopEndLayer)
                            continue;

                        if (fromNote > 0 || momentIndex != note.momentIndex || entryIndex > note.entryIndex)
                            return true;
                    }
                }

                if (iteration == long.MaxValue)
                    break;
            }

            return false;
        }

        long GetIterationFromUnwrapped(double currentUnwrapped, NBSLoopInfo loopInfo, NBSPlaybackDirection playbackDirection, long minimum)
        {
            if (!loopInfo.IsUsable)
                return 0;

            double raw = playbackDirection == NBSPlaybackDirection.forward
                ? (currentUnwrapped - loopInfo.startTime) / loopInfo.range
                : (loopInfo.endTime - currentUnwrapped) / loopInfo.range;
            if (!double.IsFinite(raw) || raw <= minimum)
                return minimum;
            if (raw >= long.MaxValue)
                return long.MaxValue;
            return Math.Max(minimum, (long)Math.Floor(raw));
        }

        bool TryGetSourceOffset
        (
            NBSPreparedNote note,
            long loopIteration,
            double currentUnwrapped,
            NBSLoopInfo loopInfo,
            out double sourceOffset
        )
        {
            double shift = GetIterationShift(loopIteration, loopInfo, direction);
            double progress = direction == NBSPlaybackDirection.forward
                ? currentUnwrapped - (note.originalStartTime + shift)
                : (note.originalEndTime + shift) - currentUnwrapped;
            if (!double.IsFinite(progress) || progress < 0 || progress >= note.timelineDuration)
            {
                sourceOffset = 0;
                return false;
            }

            double sourceTravel = (progress * note.sourceLength) / note.timelineDuration;
            sourceOffset = note.reverseSource ? note.sourceLength - sourceTravel : sourceTravel;
            sourceOffset = Math.Clamp(sourceOffset, 0, note.sourceLength);
            return true;
        }

        bool TryGetMomentOccurrence
        (
            NBSPlaybackCursor cursor,
            NBSLoopInfo loopInfo,
            out NBSPlaybackMoment moment,
            out double occurrenceAnchor
        )
        {
            if (cursor.momentIndex < 0 || cursor.momentIndex >= moments.Count)
            {
                moment = default;
                occurrenceAnchor = 0;
                return false;
            }

            moment = moments[cursor.momentIndex];
            occurrenceAnchor = moment.anchorTime + GetIterationShift(cursor.loopIteration, loopInfo, direction);
            return IsMomentAllowed(cursor.momentIndex, cursor.loopIteration, loopInfo);
        }

        void AdvanceCursor(ref NBSPlaybackCursor cursor, NBSLoopInfo loopInfo)
        {
            cursor.momentIndex += direction == NBSPlaybackDirection.forward ? 1 : -1;
            NormalizeCursor(ref cursor, loopInfo);
        }

        void NormalizeCursor(ref NBSPlaybackCursor cursor, NBSLoopInfo loopInfo)
        {
            int step = direction == NBSPlaybackDirection.forward ? 1 : -1;
            while (cursor.initialized)
            {
                while (cursor.momentIndex >= 0 && cursor.momentIndex < moments.Count)
                {
                    if (IsMomentAllowed(cursor.momentIndex, cursor.loopIteration, loopInfo))
                        return;
                    cursor.momentIndex += step;
                }

                if (!loopInfo.IsUsable || !loopInfo.CanUseIteration(cursor.loopIteration + 1))
                {
                    cursor.initialized = false;
                    return;
                }

                int loopMomentIndex = FindLoopMoment(loopInfo);
                if (loopMomentIndex < 0)
                {
                    cursor.initialized = false;
                    return;
                }

                cursor.loopIteration++;
                cursor.momentIndex = loopMomentIndex;
                return;
            }
        }

        int FindForwardMoment(double time, bool includeCurrent)
        {
            int low = 0;
            int high = moments.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                bool before = includeCurrent ? moments[middle].anchorTime < time : moments[middle].anchorTime <= time;
                if (before)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

        int FindReverseMoment(double time, bool includeCurrent)
        {
            int low = 0;
            int high = moments.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                bool accepted = includeCurrent ? moments[middle].anchorTime <= time : moments[middle].anchorTime < time;
                if (accepted)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low - 1;
        }

        int FindLoopMoment(NBSLoopInfo loopInfo)
        {
            int index = direction == NBSPlaybackDirection.forward
                ? FindForwardMoment(loopInfo.startTime, true)
                : moments.Count - 1;
            int step = direction == NBSPlaybackDirection.forward ? 1 : -1;
            while (index >= 0 && index < moments.Count)
            {
                if (IsMomentAllowed(index, 1, loopInfo))
                    return index;
                index += step;
            }

            return -1;
        }

        bool IsMomentAllowed(int momentIndex, long iteration, NBSLoopInfo loopInfo)
        {
            NBSPlaybackMoment moment = moments[momentIndex];
            for (int i = 0; i < moment.entries.Count; i++)
            {
                if (IsEntryAllowed(moment.entries[i], moment.anchorTime, iteration, loopInfo))
                    return true;
            }

            return false;
        }

        bool IsEntryAllowed(NBSPreparedEntry entry, double anchor, long iteration, NBSLoopInfo loopInfo)
        {
            if (!loopInfo.IsUsable)
                return iteration == 0;
            if (!loopInfo.CanUseIteration(iteration))
                return false;

            if (iteration == 0)
                return direction == NBSPlaybackDirection.forward ? anchor < loopInfo.endTime : anchor > loopInfo.startTime;

            if (entry.kind == NBSPlaybackEntryKind.note)
                return entry.note.originalStartTime >= loopInfo.startTime && entry.note.originalStartTime < loopInfo.endTime;

            return direction == NBSPlaybackDirection.forward
                ? anchor >= loopInfo.startTime && anchor < loopInfo.endTime
                : anchor > loopInfo.startTime && anchor <= loopInfo.endTime;
        }

        bool IsNoteAllowed(NBSPreparedNote note, long iteration, NBSLoopInfo loopInfo)
        {
            if (!loopInfo.IsUsable)
                return iteration == 0;
            if (!loopInfo.CanUseIteration(iteration))
                return false;
            if (iteration == 0)
                return direction == NBSPlaybackDirection.forward
                    ? note.anchorTime < loopInfo.endTime
                    : note.anchorTime > loopInfo.startTime;

            return note.originalStartTime >= loopInfo.startTime && note.originalStartTime < loopInfo.endTime;
        }

        static NBSLoopInfo SanitizeLoop(NBSLoopInfo loopInfo) => loopInfo.IsUsable ? loopInfo : default;

        static double GetUnwrappedPosition(NBSPlaybackPosition position, NBSLoopInfo loopInfo, NBSPlaybackDirection playbackDirection) =>
            position.fileTime + GetIterationShift(Math.Max(0, position.loopIteration), loopInfo, playbackDirection);

        static double GetIterationShift(long iteration, NBSLoopInfo loopInfo, NBSPlaybackDirection playbackDirection)
        {
            if (!loopInfo.IsUsable || iteration <= 0)
                return 0;

            double shift = iteration * loopInfo.range;
            return playbackDirection == NBSPlaybackDirection.forward ? shift : -shift;
        }

        static bool ContainsOccurrence(List<NBSPlaybackCommand> commands, NBSOccurrenceId occurrence)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                if (commands[i].occurrence == occurrence)
                    return true;
            }

            return false;
        }
    }
}
