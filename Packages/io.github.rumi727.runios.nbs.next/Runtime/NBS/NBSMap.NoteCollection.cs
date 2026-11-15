#nullable enable
using System.Collections;
using System.Collections.Immutable;

namespace RuniOS.NBS
{
    public sealed partial class NBSMap
    {
        /// <summary>
        /// Provides the sorted notes of an <see cref="NBSMap"/> by index or by tick and layer.<br/>
        /// <see cref="NBSMap"/>의 정렬된 노트를 인덱스 또는 tick과 레이어로 제공합니다.
        /// </summary>
        public readonly struct NoteCollection : IReadOnlyList<NBSMapNote>
        {
            internal NoteCollection(NBSMap map, ImmutableArray<NBSMapNote> array)
            {
                this.map = map;
                this.array = array;
            }

            readonly NBSMap map;

            /// <summary>
            /// Gets the notes as an immutable array.<br/>
            /// 노트를 불변 배열로 가져옵니다.
            /// </summary>
            public ImmutableArray<NBSMapNote> array { get; }

            /// <summary>
            /// Gets the number of notes.<br/>
            /// 노트 개수를 가져옵니다.
            /// </summary>
            public int Count => array.Length;

            /// <summary>
            /// Gets the note at an index.<br/>
            /// 인덱스의 노트를 가져옵니다.
            /// </summary>
            /// <param name="index">
            /// The index in tick and layer order.<br/>
            /// tick, 레이어 순서의 인덱스입니다.
            /// </param>
            /// <exception cref="IndexOutOfRangeException">
            /// Thrown when <paramref name="index"/> is out of range.<br/>
            /// <paramref name="index"/>가 범위를 벗어난 경우 발생합니다.
            /// </exception>
            public ref readonly NBSMapNote this[int index] => ref array.ItemRef(index);

            /// <summary>
            /// Gets the note at a tick and layer.<br/>
            /// tick과 레이어의 노트를 가져옵니다.
            /// </summary>
            /// <param name="tick">
            /// The tick of the note.<br/>
            /// 노트의 tick입니다.
            /// </param>
            /// <param name="layer">
            /// The zero-based layer of the note.<br/>
            /// 노트의 0부터 시작하는 레이어입니다.
            /// </param>
            /// <exception cref="KeyNotFoundException">
            /// Thrown when no note is at <paramref name="tick"/> and <paramref name="layer"/>.<br/>
            /// <paramref name="tick"/>과 <paramref name="layer"/>에 노트가 없는 경우 발생합니다.
            /// </exception>
            /// <remarks>
            /// When several notes share the coordinate, one of them is returned.<br/>
            /// 여러 노트가 같은 좌표에 있으면 그중 하나를 반환합니다.
            /// </remarks>
            public ref readonly NBSMapNote this[int tick, int layer]
            {
                get
                {
                    int index = IndexOf(tick, layer);
                    if (index < 0)
                        throw new KeyNotFoundException($"No NBS note is at tick {tick}, layer {layer}.");

                    return ref array.ItemRef(index);
                }
            }

            /// <summary>
            /// Finds the index of the note at a tick and layer.<br/>
            /// tick과 레이어에 있는 노트의 인덱스를 찾습니다.
            /// </summary>
            /// <param name="tick">
            /// The tick of the note.<br/>
            /// 노트의 tick입니다.
            /// </param>
            /// <param name="layer">
            /// The zero-based layer of the note.<br/>
            /// 노트의 0부터 시작하는 레이어입니다.
            /// </param>
            /// <returns>
            /// The note index, or <c>-1</c> when no note is at the coordinate.<br/>
            /// 노트 인덱스를 반환하며, 좌표에 노트가 없으면 <c>-1</c>을 반환합니다.
            /// </returns>
            public int IndexOf(int tick, int layer)
            {
                if (!map.TryGetColumn(tick, out NBSMapColumn column))
                    return -1;

                ReadOnlySpan<NBSMapNote> span = array.AsSpan();
                int low = column.noteStart;
                int high = column.noteStart + column.noteCount;
                while (low < high)
                {
                    int middle = (low + high) >> 1;
                    if (span[middle].layer < layer)
                        low = middle + 1;
                    else
                        high = middle;
                }

                return low < column.noteStart + column.noteCount && span[low].layer == layer ? low : -1;
            }

            /// <summary>
            /// Tries to get the note at a tick and layer.<br/>
            /// tick과 레이어의 노트를 가져오려고 시도합니다.
            /// </summary>
            /// <param name="tick">
            /// The tick of the note.<br/>
            /// 노트의 tick입니다.
            /// </param>
            /// <param name="layer">
            /// The zero-based layer of the note.<br/>
            /// 노트의 0부터 시작하는 레이어입니다.
            /// </param>
            /// <param name="note">
            /// The note when found; otherwise, <see langword="default"/>.<br/>
            /// 찾으면 노트이고, 그렇지 않으면 <see langword="default"/>입니다.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if a note is at the coordinate; otherwise, <see langword="false"/>.<br/>
            /// 좌표에 노트가 있으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
            /// </returns>
            public bool TryGet(int tick, int layer, out NBSMapNote note)
            {
                int index = IndexOf(tick, layer);
                note = index >= 0 ? array[index] : default;
                return index >= 0;
            }

            /// <summary>
            /// Gets the notes as a span.<br/>
            /// 노트를 span으로 가져옵니다.
            /// </summary>
            /// <returns>
            /// A read-only span over every note.<br/>
            /// 모든 노트에 대한 읽기 전용 span을 반환합니다.
            /// </returns>
            public ReadOnlySpan<NBSMapNote> AsSpan() => array.AsSpan();

            /// <summary>
            /// Gets an allocation-free enumerator over the notes.<br/>
            /// 노트를 할당 없이 열거하는 열거자를 가져옵니다.
            /// </summary>
            /// <returns>
            /// The enumerator.<br/>
            /// 열거자를 반환합니다.
            /// </returns>
            public ReadOnlySpan<NBSMapNote>.Enumerator GetEnumerator() => array.AsSpan().GetEnumerator();

            NBSMapNote IReadOnlyList<NBSMapNote>.this[int index] => array[index];

            IEnumerator<NBSMapNote> IEnumerable<NBSMapNote>.GetEnumerator() => ((IEnumerable<NBSMapNote>)array).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)array).GetEnumerator();
        }
    }
}
