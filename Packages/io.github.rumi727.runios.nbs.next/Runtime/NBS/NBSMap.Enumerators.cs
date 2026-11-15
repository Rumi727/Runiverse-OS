#nullable enable
namespace RuniOS.NBS
{
    public sealed partial class NBSMap
    {
        /// <summary>
        /// Enumerates the notes inside a tick and layer rectangle without allocating.<br/>
        /// tick과 레이어 사각형 안의 노트를 할당 없이 열거합니다.
        /// </summary>
        public ref struct GridEnumerator
        {
            internal GridEnumerator(NBSMap map, int startTick, int endTick, int startLayer, int endLayer)
            {
                notes = map.notes.AsSpan();
                columns = map.columns.AsSpan();
                column = map.FindColumnAtOrAfter(startTick) - 1;
                this.endTick = endTick;
                this.startLayer = startLayer;
                this.endLayer = endLayer;
                index = 0;
                columnEnd = 0;
            }

            readonly ReadOnlySpan<NBSMapNote> notes;
            readonly ReadOnlySpan<NBSMapColumn> columns;
            readonly int endTick;
            readonly int startLayer;
            readonly int endLayer;
            int column;
            int index;
            int columnEnd;

            /// <summary>
            /// Gets the current note.<br/>
            /// 현재 노트를 가져옵니다.
            /// </summary>
            public readonly ref readonly NBSMapNote Current => ref notes[index];

            /// <summary>
            /// Moves to the next matching note.<br/>
            /// 다음 일치하는 노트로 이동합니다.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if a note is available; otherwise, <see langword="false"/>.<br/>
            /// 노트가 있으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
            /// </returns>
            public bool MoveNext()
            {
                index++;
                while (true)
                {
                    if (index < columnEnd && notes[index].layer < endLayer)
                        return true;

                    if (++column >= columns.Length || columns[column].tick >= endTick)
                        return false;

                    NBSMapColumn current = columns[column];
                    columnEnd = current.noteStart + current.noteCount;
                    index = current.noteStart;
                    while (index < columnEnd && notes[index].layer < startLayer)
                        index++;
                }
            }

            /// <summary>
            /// Gets this enumerator for <see langword="foreach"/>.<br/>
            /// <see langword="foreach"/>에 사용할 이 열거자를 가져옵니다.
            /// </summary>
            /// <returns>
            /// This enumerator.<br/>
            /// 이 열거자를 반환합니다.
            /// </returns>
            public readonly GridEnumerator GetEnumerator() => this;
        }

        /// <summary>
        /// Enumerates the notes of one layer in ascending tick order without allocating.<br/>
        /// 레이어 하나의 노트를 tick 오름차순으로 할당 없이 열거합니다.
        /// </summary>
        public ref struct LayerEnumerator
        {
            internal LayerEnumerator(ReadOnlySpan<NBSMapNote> notes, ReadOnlySpan<int> indexes)
            {
                this.notes = notes;
                this.indexes = indexes;
                position = -1;
            }

            readonly ReadOnlySpan<NBSMapNote> notes;
            readonly ReadOnlySpan<int> indexes;
            int position;

            /// <summary>
            /// Gets the number of notes on the layer.<br/>
            /// 레이어의 노트 개수를 가져옵니다.
            /// </summary>
            public readonly int count => indexes.Length;

            /// <summary>
            /// Gets the current note.<br/>
            /// 현재 노트를 가져옵니다.
            /// </summary>
            public readonly ref readonly NBSMapNote Current => ref notes[indexes[position]];

            /// <summary>
            /// Gets the index in <see cref="NBSMap.notes"/> of the current note.<br/>
            /// 현재 노트의 <see cref="NBSMap.notes"/> 인덱스를 가져옵니다.
            /// </summary>
            public readonly int currentMapIndex => indexes[position];

            /// <summary>
            /// Moves to the next note.<br/>
            /// 다음 노트로 이동합니다.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> if a note is available; otherwise, <see langword="false"/>.<br/>
            /// 노트가 있으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
            /// </returns>
            public bool MoveNext() => ++position < indexes.Length;

            /// <summary>
            /// Gets this enumerator for <see langword="foreach"/>.<br/>
            /// <see langword="foreach"/>에 사용할 이 열거자를 가져옵니다.
            /// </summary>
            /// <returns>
            /// This enumerator.<br/>
            /// 이 열거자를 반환합니다.
            /// </returns>
            public readonly LayerEnumerator GetEnumerator() => this;
        }
    }
}
