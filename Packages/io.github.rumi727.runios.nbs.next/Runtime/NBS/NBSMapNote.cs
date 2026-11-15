#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Stores a note of an <see cref="NBSMap"/> together with its song time.<br/>
    /// <see cref="NBSMap"/>의 노트를 곡 시간과 함께 저장합니다.
    /// </summary>
    /// <param name="note">
    /// The note as it was when the map was created.<br/>
    /// 맵을 만들 때의 노트입니다.
    /// </param>
    /// <param name="sourceIndex">
    /// The index of the note in the note list the map was created from.<br/>
    /// 맵을 만든 노트 리스트에서의 노트 인덱스입니다.
    /// </param>
    /// <param name="time">
    /// The song time of the note in seconds.<br/>
    /// 노트의 곡 시간(초)입니다.
    /// </param>
    /// <param name="nextStopTime">
    /// The song time of the first <see cref="NBSInstrumentFunction.soundStopper"/> that stops the note, or <see cref="double.PositiveInfinity"/> when none does. Loops are not considered.<br/>
    /// 노트를 멈추는 첫 <see cref="NBSInstrumentFunction.soundStopper"/>의 곡 시간이며, 없으면 <see cref="double.PositiveInfinity"/>입니다. 반복은 고려하지 않습니다.
    /// </param>
    public readonly record struct NBSMapNote(NBSNote note, int sourceIndex, double time, double nextStopTime)
    {
        /// <summary>
        /// Gets the tick of <see cref="note"/>.<br/>
        /// <see cref="note"/>의 tick을 가져옵니다.
        /// </summary>
        public int tick => note.tick;

        /// <summary>
        /// Gets the layer of <see cref="note"/>.<br/>
        /// <see cref="note"/>의 레이어를 가져옵니다.
        /// </summary>
        public int layer => note.layer;
    }
}
