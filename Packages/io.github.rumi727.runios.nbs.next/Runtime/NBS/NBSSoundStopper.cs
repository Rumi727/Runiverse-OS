#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Stores a <see cref="NBSInstrumentFunction.soundStopper"/> note and the layers it stops.<br/>
    /// <see cref="NBSInstrumentFunction.soundStopper"/> 노트와 그 노트가 멈추는 레이어를 저장합니다.
    /// </summary>
    /// <param name="mapIndex">
    /// The index of the stopper note in <see cref="NBSMap.notes"/>.<br/>
    /// <see cref="NBSMap.notes"/>에서 Stopper 노트의 인덱스입니다.
    /// </param>
    /// <param name="tick">
    /// The tick of the stopper.<br/>
    /// Stopper의 tick입니다.
    /// </param>
    /// <param name="layer">
    /// The zero-based layer that holds the stopper.<br/>
    /// Stopper가 놓인 0부터 시작하는 레이어입니다.
    /// </param>
    /// <param name="time">
    /// The song time of the stopper in seconds.<br/>
    /// Stopper의 곡 시간(초)입니다.
    /// </param>
    /// <param name="startLayer">
    /// The first one-based target layer, or <c>0</c> to stop every layer.<br/>
    /// 1부터 시작하는 첫 대상 레이어이며, <c>0</c>이면 모든 레이어를 멈춥니다.
    /// </param>
    /// <param name="endLayer">
    /// The last one-based target layer. When it is less than a positive <paramref name="startLayer"/>, the stopper targets no layer.<br/>
    /// 1부터 시작하는 마지막 대상 레이어입니다. 양수인 <paramref name="startLayer"/>보다 작으면 대상 레이어가 없습니다.
    /// </param>
    public readonly record struct NBSSoundStopper(int mapIndex, int tick, int layer, double time, int startLayer, int endLayer)
    {
        /// <summary>
        /// Creates the stopper encoded in <paramref name="note"/> as Note Block Studio does.<br/>
        /// Note Block Studio와 같은 방식으로 <paramref name="note"/>에 인코딩된 Stopper를 만듭니다.
        /// </summary>
        /// <param name="mapIndex">
        /// The index of the note in <see cref="NBSMap.notes"/>.<br/>
        /// <see cref="NBSMap.notes"/>에서 노트의 인덱스입니다.
        /// </param>
        /// <param name="note">
        /// The stopper note. Its pitch holds the start layer and its panning and velocity hold the end layer.<br/>
        /// Stopper 노트입니다. pitch에 시작 레이어가, panning과 velocity에 끝 레이어가 들어 있습니다.
        /// </param>
        /// <param name="time">
        /// The song time of the note in seconds.<br/>
        /// 노트의 곡 시간(초)입니다.
        /// </param>
        /// <returns>
        /// The decoded stopper.<br/>
        /// 디코딩된 Stopper를 반환합니다.
        /// </returns>
        public static NBSSoundStopper FromNote(int mapIndex, NBSNote note, double time)
        {
            int startLayer = Math.Max(0, (int)note.pitch);
            int endLayer = ((note.panning + 156) % 256) + (((note.velocity + 156) % 256) * 256);
            return new NBSSoundStopper(mapIndex, note.tick, note.layer, time, startLayer, endLayer);
        }

        /// <summary>
        /// Determines whether the stopper stops sounds on a layer.<br/>
        /// Stopper가 레이어의 소리를 멈추는지 확인합니다.
        /// </summary>
        /// <param name="layer">
        /// The zero-based layer to test.<br/>
        /// 확인할 0부터 시작하는 레이어입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the layer is a target; otherwise, <see langword="false"/>.<br/>
        /// 레이어가 대상이면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool Targets(int layer) => startLayer == 0 || (layer + 1 >= startLayer && layer + 1 <= endLayer);
    }
}
