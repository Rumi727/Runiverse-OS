#nullable enable
using System.Collections;

namespace RuniOS.Texts
{
    /// <summary>
    /// Represents a sequence of text elements that should be resolved or rendered in order.<br/>
    /// 순서대로 해석되거나 렌더링되어야 하는 텍스트 요소 시퀀스를 나타냅니다.
    /// </summary>
    public sealed class GroupText : Text, IReadOnlyList<Text>, IList<Text>
    {
        /// <summary>
        /// Initializes an empty text group.<br/>
        /// 빈 텍스트 그룹을 초기화합니다.
        /// </summary>
        public GroupText() { }

        /// <summary>
        /// Initializes a text group with the specified items.<br/>
        /// 지정된 항목으로 텍스트 그룹을 초기화합니다.
        /// </summary>
        /// <param name="items">
        /// The items to add to the group.<br/>
        /// 그룹에 추가할 항목입니다.
        /// </param>
        public GroupText(IEnumerable<Text> items) => this.items.AddRange(items);

        readonly List<Text> items = [];

        /// <summary>
        /// Gets or sets the text element at the specified index.<br/>
        /// 지정된 인덱스의 텍스트 요소를 가져오거나 설정합니다.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the text element.<br/>
        /// 텍스트 요소의 0부터 시작하는 인덱스입니다.
        /// </param>
        /// <returns>
        /// The text element at <paramref name="index"/>.<br/>
        /// <paramref name="index"/> 위치의 텍스트 요소를 반환합니다.
        /// </returns>
        public Text this[int index]
        {
            get => items[index];
            set => items[index] = value;
        }

        /// <summary>
        /// Gets the number of text elements in this group.<br/>
        /// 이 그룹에 포함된 텍스트 요소 수를 가져옵니다.
        /// </summary>
        public int count => items.Count;
        int IReadOnlyCollection<Text>.Count => count;
        int ICollection<Text>.Count => count;

        bool ICollection<Text>.IsReadOnly => ((ICollection<Text>)items).IsReadOnly;

        /// <summary>
        /// Appends a text element to the end of this group.<br/>
        /// 이 그룹의 끝에 텍스트 요소를 추가합니다.
        /// </summary>
        /// <param name="text">
        /// The text element to append.<br/>
        /// 추가할 텍스트 요소입니다.
        /// </param>
        /// <returns>
        /// This text group.<br/>
        /// 이 텍스트 그룹을 반환합니다.
        /// </returns>
        public GroupText Add(Text text)
        {
            items.Add(text);
            return this;
        }
        void ICollection<Text>.Add(Text item) => Add(item);

        /// <summary>
        /// Inserts a text element at the specified index.<br/>
        /// 지정된 인덱스에 텍스트 요소를 삽입합니다.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which to insert <paramref name="item"/>.<br/>
        /// <paramref name="item"/>을 삽입할 0부터 시작하는 인덱스입니다.
        /// </param>
        /// <param name="item">
        /// The text element to insert.<br/>
        /// 삽입할 텍스트 요소입니다.
        /// </param>
        public void Insert(int index, Text item) => items.Insert(index, item);

        /// <summary>
        /// Removes the first occurrence of a text element from this group.<br/>
        /// 이 그룹에서 텍스트 요소의 첫 번째 일치 항목을 제거합니다.
        /// </summary>
        /// <param name="item">
        /// The text element to remove.<br/>
        /// 제거할 텍스트 요소입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="item"/> was removed; otherwise, <see langword="false"/>.<br/>
        /// <paramref name="item"/>이 제거되었으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool Remove(Text item) => items.Remove(item);
        /// <summary>
        /// Removes the text element at the specified index.<br/>
        /// 지정된 인덱스의 텍스트 요소를 제거합니다.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the text element to remove.<br/>
        /// 제거할 텍스트 요소의 0부터 시작하는 인덱스입니다.
        /// </param>
        public void RemoveAt(int index) => items.RemoveAt(index);

        /// <summary>
        /// Removes all text elements from this group.<br/>
        /// 이 그룹에서 모든 텍스트 요소를 제거합니다.
        /// </summary>
        public void Clear() => items.Clear();

        /// <summary>
        /// Determines whether this group contains the specified text element.<br/>
        /// 이 그룹이 지정된 텍스트 요소를 포함하는지 확인합니다.
        /// </summary>
        /// <param name="item">
        /// The text element to locate.<br/>
        /// 찾을 텍스트 요소입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="item"/> is found; otherwise, <see langword="false"/>.<br/>
        /// <paramref name="item"/>을 찾으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool Contains(Text item) => items.Contains(item);
        /// <summary>
        /// Gets the index of the specified text element.<br/>
        /// 지정된 텍스트 요소의 인덱스를 가져옵니다.
        /// </summary>
        /// <param name="item">
        /// The text element to locate.<br/>
        /// 찾을 텍스트 요소입니다.
        /// </param>
        /// <returns>
        /// The zero-based index of <paramref name="item"/>, or -1 if it is not found.<br/>
        /// <paramref name="item"/>의 0부터 시작하는 인덱스이며, 찾지 못하면 -1을 반환합니다.
        /// </returns>
        public int IndexOf(Text item) => items.IndexOf(item);

        /// <summary>
        /// Copies the text elements to an array starting at the specified array index.<br/>
        /// 지정된 배열 인덱스부터 텍스트 요소를 배열에 복사합니다.
        /// </summary>
        /// <param name="array">
        /// The destination array.<br/>
        /// 대상 배열입니다.
        /// </param>
        /// <param name="arrayIndex">
        /// The zero-based index in <paramref name="array"/> at which copying begins.<br/>
        /// 복사를 시작할 <paramref name="array"/>의 0부터 시작하는 인덱스입니다.
        /// </param>
        public void CopyTo(Text[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        public IEnumerator<Text> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)items).GetEnumerator();

        /// <summary>
        /// Converts an interpolated string handler to a text group.<br/>
        /// 보간 문자열 핸들러를 텍스트 그룹으로 변환합니다.
        /// </summary>
        /// <param name="handler">
        /// The interpolated string handler to convert.<br/>
        /// 변환할 보간 문자열 핸들러입니다.
        /// </param>
        public static implicit operator GroupText(GroupTextStringHandler handler) => handler.ToGroupText();
    }
}
