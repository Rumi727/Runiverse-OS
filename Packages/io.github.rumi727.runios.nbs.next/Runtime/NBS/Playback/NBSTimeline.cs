#nullable enable
using RuniOS.Sounds;

namespace RuniOS.NBS
{
    /// <summary>
    /// Places the notes of a map on the timeline for any tempo, pitch, and loop, using the loaded instrument sounds.<br/>
    /// 로드된 악기 사운드를 사용하여 임의의 tempo, pitch, 반복에서 맵의 노트를 타임라인에 배치합니다.
    /// </summary>
    /// <remarks>
    /// The timeline is immutable and keeps no playback position. Per-note values that do not depend on the tempo or pitch are computed once,
    /// so a change of tempo or pitch never rebuilds anything.<br/>
    /// A note of sound length <c>L</c> and pitch ratio <c>Q</c> covers <c>D = (|tempo| / |pitch|) · L / Q</c> timeline seconds, shortened by sound stoppers.
    /// <br/><br/>
    /// 타임라인은 불변이며 재생 위치를 갖지 않습니다. tempo나 pitch와 무관한 노트별 값은 한 번만 계산하므로 tempo나 pitch가 바뀌어도 다시 만들 것이 없습니다.<br/>
    /// 소리 길이 <c>L</c>, 피치 배율 <c>Q</c>인 노트는 <c>D = (|tempo| / |pitch|) · L / Q</c> 타임라인 초를 차지하며, Sound Stopper가 이를 줄입니다.
    /// </remarks>
    public sealed partial class NBSTimeline
    {
        /// <summary>
        /// Initializes a timeline for a map and an instrument bank.<br/>
        /// 맵과 악기 bank로 타임라인을 초기화합니다.
        /// </summary>
        /// <param name="map">
        /// The map to place.<br/>
        /// 배치할 맵입니다.
        /// </param>
        /// <param name="bank">
        /// The instrument sounds. The bank must stay alive while the timeline is used.<br/>
        /// 악기 사운드입니다. 타임라인을 사용하는 동안 bank가 살아 있어야 합니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="map"/> or <paramref name="bank"/> is <see langword="null"/>.<br/>
        /// <paramref name="map"/> 또는 <paramref name="bank"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public NBSTimeline(NBSMap map, NBSInstrumentBank bank)
        {
            ArgumentNullException.ThrowIfNull(map);
            ArgumentNullException.ThrowIfNull(bank);

            this.map = map;

            ReadOnlySpan<NBSMapNote> notes = map.notes.AsSpan();
            clips = new WaveAudioClip?[notes.Length];
            ratios = new double[notes.Length];
            bases = new double[notes.Length];
            mixes = new NBSNoteMix[notes.Length];

            for (int i = 0; i < notes.Length; i++)
            {
                NBSNote note = notes[i].note;
                NBSNoteMix mix = map.GetMix(note);
                mixes[i] = mix;
                bases[i] = double.NaN;

                WaveAudioClip? clip = bank.GetClip(note.instrument);
                if (clip == null || clip.samples == 0 || mix.volume <= 0 || map.GetFunction(note.instrument) != NBSInstrumentFunction.none || !map.IsLayerAudible(note.layer))
                    continue;

                // play_sound of Note Block Studio: 0.5 · 2^((key + instrument key + fine pitch / 100 − 78) / 12)
                double ratio = 0.5 * Math.Pow(2, (note.key + bank.GetKey(note.instrument) + (mix.pitch / 100d) - 78) / 12d);
                if (!double.IsFinite(ratio) || ratio <= 0)
                    continue;

                clips[i] = clip;
                ratios[i] = ratio;
                bases[i] = clip.length / ratio;
                maximumBase = Math.Max(maximumBase, bases[i]);
            }
        }

        readonly WaveAudioClip?[] clips;
        readonly double[] ratios;
        readonly double[] bases;
        readonly NBSNoteMix[] mixes;

        /// <summary>
        /// Gets the placed map.<br/>
        /// 배치하는 맵을 가져옵니다.
        /// </summary>
        public NBSMap map { get; }

        /// <summary>
        /// Gets the longest sound length divided by its pitch ratio, in seconds.<br/>
        /// 가장 긴 소리 길이를 피치 배율로 나눈 값을 초 단위로 가져옵니다.
        /// </summary>
        public double maximumBase { get; }

        /// <summary>
        /// Gets the sound of a note.<br/>
        /// 노트의 소리를 가져옵니다.
        /// </summary>
        /// <param name="mapIndex">
        /// The index of the note in <see cref="NBSMap.notes"/>.<br/>
        /// <see cref="NBSMap.notes"/>에서 노트의 인덱스입니다.
        /// </param>
        /// <returns>
        /// The sound, or <see langword="null"/> when the note is silent.<br/>
        /// 소리를 반환하며, 노트가 무음이면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public WaveAudioClip? GetClip(int mapIndex) => clips[mapIndex];

        /// <summary>
        /// Gets the pitch ratio of a note at a pitch of <c>1</c>.<br/>
        /// pitch가 <c>1</c>일 때 노트의 피치 배율을 가져옵니다.
        /// </summary>
        /// <param name="mapIndex">
        /// The index of the note in <see cref="NBSMap.notes"/>.<br/>
        /// <see cref="NBSMap.notes"/>에서 노트의 인덱스입니다.
        /// </param>
        /// <returns>
        /// The ratio, or <c>0</c> when the note is silent.<br/>
        /// 배율을 반환하며, 노트가 무음이면 <c>0</c>을 반환합니다.
        /// </returns>
        public double GetRatio(int mapIndex) => ratios[mapIndex];

        /// <summary>
        /// Gets the volume, stereo position, and fine pitch of a note.<br/>
        /// 노트의 음량, 스테레오 위치, 미세 피치를 가져옵니다.
        /// </summary>
        /// <param name="mapIndex">
        /// The index of the note in <see cref="NBSMap.notes"/>.<br/>
        /// <see cref="NBSMap.notes"/>에서 노트의 인덱스입니다.
        /// </param>
        /// <returns>
        /// The mix of the note.<br/>
        /// 노트의 믹스를 반환합니다.
        /// </returns>
        public NBSNoteMix GetMix(int mapIndex) => mixes[mapIndex];

        /// <summary>
        /// Determines whether a note produces sound at a rate.<br/>
        /// 노트가 지정한 속도에서 소리를 내는지 확인합니다.
        /// </summary>
        /// <param name="mapIndex">
        /// The index of the note in <see cref="NBSMap.notes"/>.<br/>
        /// <see cref="NBSMap.notes"/>에서 노트의 인덱스입니다.
        /// </param>
        /// <param name="rate">
        /// The playback rate.<br/>
        /// 재생 속도입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the note sounds; otherwise, <see langword="false"/>. Streamed sounds cannot play backwards and are silent with a negative pitch.<br/>
        /// 노트가 소리를 내면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다. 스트림 사운드는 거꾸로 재생할 수 없으므로 pitch가 음수이면 무음입니다.
        /// </returns>
        public bool IsAudible(int mapIndex, in NBSPlaybackRate rate)
        {
            WaveAudioClip? clip = clips[mapIndex];
            return clip != null && rate.canPlace && !(rate.pitch < 0 && clip.isStream);
        }

        /// <summary>
        /// Gets the timeline length of a note after sound stoppers are applied.<br/>
        /// Sound Stopper를 적용한 노트의 타임라인 길이를 가져옵니다.
        /// </summary>
        /// <param name="mapIndex">
        /// The index of the note in <see cref="NBSMap.notes"/>.<br/>
        /// <see cref="NBSMap.notes"/>에서 노트의 인덱스입니다.
        /// </param>
        /// <param name="rate">
        /// The playback rate.<br/>
        /// 재생 속도입니다.
        /// </param>
        /// <param name="loop">
        /// The loop range, which lets stoppers after the loop start stop notes that ring across the loop end.<br/>
        /// 반복 범위이며, 반복 끝을 넘어 울리는 노트를 반복 시작 이후의 Stopper가 멈추게 합니다.
        /// </param>
        /// <param name="stopped">
        /// Whether a sound stopper shortens the sound.<br/>
        /// Sound Stopper가 소리를 줄였는지 여부입니다.
        /// </param>
        /// <returns>
        /// The length in seconds, or <see cref="double.NaN"/> when the note is silent.<br/>
        /// 초 단위 길이를 반환하며, 노트가 무음이면 <see cref="double.NaN"/>을 반환합니다.
        /// </returns>
        /// <remarks>
        /// Stoppers are applied to the forward rendering of the song, as Note Block Studio plays it. Other directions play the same shortened sound.<br/>
        /// Stopper는 Note Block Studio가 재생하는 곡의 정방향 렌더링에 적용합니다. 다른 방향도 같은 줄어든 소리를 재생합니다.
        /// </remarks>
        public double GetLength(int mapIndex, in NBSPlaybackRate rate, NBSLoopRange? loop, out bool stopped)
        {
            stopped = false;
            double length = rate.timelineScale * bases[mapIndex];
            if (!double.IsFinite(length))
                return double.NaN;

            ref readonly NBSMapNote note = ref map.notes[mapIndex];
            double stopTime = note.nextStopTime;
            if (loop is { } range && note.time < range.end && stopTime >= range.end)
            {
                double wrappedStop = map.FindStopTimeAtOrAfter(note.layer, range.start);
                stopTime = wrappedStop < range.end ? range.end + (wrappedStop - range.start) : double.PositiveInfinity;
            }

            double stopLength = stopTime - note.time;
            if (stopLength < length)
            {
                stopped = true;
                return stopLength;
            }

            return length;
        }

        /// <summary>
        /// Gets the placement of a note.<br/>
        /// 노트의 배치를 가져옵니다.
        /// </summary>
        /// <param name="mapIndex">
        /// The index of the note in <see cref="NBSMap.notes"/>.<br/>
        /// <see cref="NBSMap.notes"/>에서 노트의 인덱스입니다.
        /// </param>
        /// <param name="length">
        /// The timeline length from <see cref="GetLength"/>.<br/>
        /// <see cref="GetLength"/>에서 얻은 타임라인 길이입니다.
        /// </param>
        /// <param name="rate">
        /// The playback rate.<br/>
        /// 재생 속도입니다.
        /// </param>
        /// <returns>
        /// The placement.<br/>
        /// 배치를 반환합니다.
        /// </returns>
        public NBSNotePlacement GetPlacement(int mapIndex, double length, in NBSPlaybackRate rate) =>
            NBSNotePlacement.Compute(map.notes[mapIndex].time, length, ratios[mapIndex] / rate.timelineScale, rate);
    }
}
