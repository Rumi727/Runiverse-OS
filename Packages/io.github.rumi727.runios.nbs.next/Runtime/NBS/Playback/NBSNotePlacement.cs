#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Describes where a note sounds on the timeline and how its sound is played.<br/>
    /// 노트가 타임라인의 어디에서 소리 나는지와 소리를 어떻게 재생하는지 설명합니다.
    /// </summary>
    /// <param name="low">
    /// The lower song time of the sounding range, in seconds.<br/>
    /// 소리 나는 범위의 아래쪽 곡 시간(초)입니다.
    /// </param>
    /// <param name="high">
    /// The upper song time of the sounding range, in seconds.<br/>
    /// 소리 나는 범위의 위쪽 곡 시간(초)입니다.
    /// </param>
    /// <param name="attack">
    /// The song time where the start of the sound is heard. It equals <paramref name="low"/> or <paramref name="high"/>.<br/>
    /// 소리의 시작 부분이 들리는 곡 시간입니다. <paramref name="low"/> 또는 <paramref name="high"/>와 같습니다.
    /// </param>
    /// <param name="forward">
    /// Whether the timeline moves forward, which makes <paramref name="low"/> the entry point.<br/>
    /// 타임라인이 정방향으로 진행하는지 여부이며, 그러면 <paramref name="low"/>가 진입점입니다.
    /// </param>
    /// <param name="sourceScale">
    /// The sound seconds played per timeline second.<br/>
    /// 타임라인 1초당 재생되는 소리의 초입니다.
    /// </param>
    public readonly record struct NBSNotePlacement(double low, double high, double attack, bool forward, double sourceScale)
    {
        /// <summary>
        /// Gets the song time where playback reaches the note first.<br/>
        /// 재생이 노트에 처음 도달하는 곡 시간을 가져옵니다.
        /// </summary>
        public double entry => forward ? low : high;

        /// <summary>
        /// Gets the song time where the note stops sounding.<br/>
        /// 노트 소리가 끝나는 곡 시간을 가져옵니다.
        /// </summary>
        public double exit => forward ? high : low;

        /// <summary>
        /// Gets whether the sound starts from its beginning at the entry point and plays forwards.<br/>
        /// 소리가 진입점에서 처음부터 시작해 정방향으로 재생되는지 여부를 가져옵니다.
        /// </summary>
        public bool startsAtAttack => entry == attack;

        /// <summary>
        /// Gets the position in the sound, in seconds from its beginning, that is heard at a song time.<br/>
        /// 곡 시간에 들리는 소리 안의 위치를 소리 시작 기준 초 단위로 가져옵니다.
        /// </summary>
        /// <param name="time">
        /// The song time in seconds.<br/>
        /// 초 단위 곡 시간입니다.
        /// </param>
        /// <returns>
        /// The sound position in seconds.<br/>
        /// 초 단위 소리 위치를 반환합니다.
        /// </returns>
        public double GetSourceTime(double time) => Math.Abs(time - attack) * sourceScale;

        /// <summary>
        /// Places a note.<br/>
        /// 노트를 배치합니다.
        /// </summary>
        /// <param name="noteTime">
        /// The song time of the note in seconds.<br/>
        /// 노트의 곡 시간(초)입니다.
        /// </param>
        /// <param name="length">
        /// The timeline length of the sound in seconds after sound stoppers are applied.<br/>
        /// Sound Stopper를 적용한 소리의 타임라인 길이(초)입니다.
        /// </param>
        /// <param name="sourceScale">
        /// The sound seconds played per timeline second.<br/>
        /// 타임라인 1초당 재생되는 소리의 초입니다.
        /// </param>
        /// <param name="rate">
        /// The tempo, pitch, and reverse alignment.<br/>
        /// tempo, pitch, 리버스 배치입니다.
        /// </param>
        /// <returns>
        /// The placement.<br/>
        /// 배치를 반환합니다.
        /// </returns>
        /// <remarks>
        /// <see cref="NBSReversePitchAlignment.attackAtNote"/> keeps the attack on the note: the range is after the note when the tempo and pitch signs match and before it otherwise.<br/>
        /// <see cref="NBSReversePitchAlignment.startAtNote"/> always uses the range after the note and moves the attack to its end when the tempo and pitch signs differ.
        /// <br/><br/>
        /// <see cref="NBSReversePitchAlignment.attackAtNote"/>는 어택을 노트 위치에 둡니다. tempo와 pitch 부호가 같으면 범위가 노트 뒤에, 다르면 노트 앞에 놓입니다.<br/>
        /// <see cref="NBSReversePitchAlignment.startAtNote"/>는 항상 노트 뒤의 범위를 쓰고, tempo와 pitch 부호가 다르면 어택을 범위 끝으로 옮깁니다.
        /// </remarks>
        public static NBSNotePlacement Compute(double noteTime, double length, double sourceScale, in NBSPlaybackRate rate)
        {
            bool forward = rate.tempo > 0;
            bool signsMatch = forward == (rate.pitch > 0);
            if (rate.alignment == NBSReversePitchAlignment.attackAtNote)
            {
                return signsMatch
                    ? new NBSNotePlacement(noteTime, noteTime + length, noteTime, forward, sourceScale)
                    : new NBSNotePlacement(noteTime - length, noteTime, noteTime, forward, sourceScale);
            }

            // A negative tempo mirrors the positive-tempo rendering with the opposite pitch sign, so the attack follows the sign product.
            return new NBSNotePlacement(noteTime, noteTime + length, signsMatch ? noteTime : noteTime + length, forward, sourceScale);
        }
    }
}
