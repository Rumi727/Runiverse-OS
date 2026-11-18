#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Stores the signed tempo and pitch that place notes on the timeline.<br/>
    /// 노트를 타임라인에 배치하는 부호 있는 tempo와 pitch를 저장합니다.
    /// </summary>
    /// <param name="tempo">
    /// The timeline speed. A negative value plays the timeline backwards.<br/>
    /// 타임라인 속도입니다. 음수이면 타임라인을 거꾸로 재생합니다.
    /// </param>
    /// <param name="pitch">
    /// The sound speed. A negative value plays sounds backwards.<br/>
    /// 소리 속도입니다. 음수이면 소리를 거꾸로 재생합니다.
    /// </param>
    /// <param name="alignment">
    /// The placement of reversed sounds.<br/>
    /// 리버스된 소리의 배치입니다.
    /// </param>
    public readonly record struct NBSPlaybackRate(float tempo, float pitch, NBSReversePitchAlignment alignment)
    {
        /// <summary>
        /// Gets whether both rates are finite and non-zero, so notes can be placed.<br/>
        /// 두 속도가 모두 유한하고 0이 아니어서 노트를 배치할 수 있는지 여부를 가져옵니다.
        /// </summary>
        public bool canPlace => tempo != 0 && pitch != 0 && float.IsFinite(tempo) && float.IsFinite(pitch);

        /// <summary>
        /// Gets the timeline seconds that one second of sound covers at the original note pitch, <c>|tempo| / |pitch|</c>.<br/>
        /// 원래 노트 피치에서 소리 1초가 차지하는 타임라인 초인 <c>|tempo| / |pitch|</c>를 가져옵니다.
        /// </summary>
        public double timelineScale => Math.Abs((double)tempo) / Math.Abs((double)pitch);

        /// <summary>
        /// Gets whether the entry point of a note moves when its length changes.<br/>
        /// 노트 길이가 바뀔 때 노트의 진입점이 움직이는지 여부를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// When it is <see langword="false"/>, the entry point is always the note time, so scheduled starts can be moved in time when the rate magnitude changes.<br/>
        /// <see langword="false"/>이면 진입점이 항상 노트 시간이므로, 속도 크기가 바뀔 때 예약된 시작 시점을 옮기기만 하면 됩니다.
        /// </remarks>
        public bool entryDependsOnLength => alignment == NBSReversePitchAlignment.attackAtNote ? pitch < 0 : tempo < 0;
    }
}
