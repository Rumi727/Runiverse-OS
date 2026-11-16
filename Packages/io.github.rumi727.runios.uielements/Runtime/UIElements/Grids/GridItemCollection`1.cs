#nullable enable
using System.Collections;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// The items of a layer, in the order they were added. Adding or removing an item updates the layer on the next frame.<br/>
    /// 레이어의 아이템 목록이며 추가한 순서를 유지합니다. 아이템을 추가하거나 제거하면 다음 프레임에 레이어가 갱신됩니다.
    /// </summary>
    /// <remarks>
    /// An item can be in only one collection at a time. Layers keep their own copy sorted by <see cref="GridItem.start"/>; see <see cref="GridLayer.sortedItems"/>.<br/>
    /// Removing many items one by one costs a search each time, so prefer <see cref="RemoveAll"/> for bulk removal.
    /// <br/><br/>
    /// 아이템은 한 번에 한 컬렉션에만 있을 수 있습니다. 레이어는 <see cref="GridItem.start"/> 순으로 정렬된 별도 목록을 가집니다. <see cref="GridLayer.sortedItems"/>를 참고하세요.<br/>
    /// 많은 아이템을 하나씩 제거하면 매번 검색 비용이 들므로, 대량 제거에는 <see cref="RemoveAll"/>을 쓰세요.
    /// </remarks>
    /// <typeparam name="TItem">
    /// The item type of the layer.<br/>
    /// 레이어의 아이템 타입입니다.
    /// </typeparam>
    /// <param name="owner">
    /// The layer that owns the items.<br/>
    /// 아이템을 가질 레이어입니다.
    /// </param>
    public sealed class GridItemCollection<TItem>(GridLayer owner) : IList<TItem>, IReadOnlyList<TItem> where TItem : GridItem
    {
        readonly List<TItem> list = [];

        /// <summary>
        /// Gets the layer that owns the items.<br/>
        /// 아이템을 가지는 레이어를 가져옵니다.
        /// </summary>
        public GridLayer owner { get; } = owner;

        /// <summary>
        /// Gets the number of items.<br/>
        /// 아이템 수를 가져옵니다.
        /// </summary>
        public int Count => list.Count;

        bool ICollection<TItem>.IsReadOnly => false;

        /// <summary>
        /// Gets or sets the item at <paramref name="index"/>. Setting removes the previous item from the layer.<br/>
        /// <paramref name="index"/> 위치의 아이템을 가져오거나 설정합니다. 설정하면 이전 아이템은 레이어에서 제거됩니다.
        /// </summary>
        /// <param name="index">
        /// The index of the item.<br/>
        /// 아이템의 인덱스입니다.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the collection.<br/>
        /// <paramref name="index"/>가 컬렉션 범위 밖인 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the new item is already in a layer.<br/>
        /// 새 아이템이 이미 레이어에 있는 경우 발생합니다.
        /// </exception>
        public TItem this[int index]
        {
            get => list[index];
            set
            {
                TItem previous = list[index];
                if (previous == value)
                    return;

                Attach(value);
                list[index] = value;
                Detach(previous);
            }
        }

        /// <summary>
        /// Adds <paramref name="item"/> to the end.<br/>
        /// <paramref name="item"/>을 끝에 추가합니다.
        /// </summary>
        /// <param name="item">
        /// The item to add.<br/>
        /// 추가할 아이템입니다.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="item"/> is already in a layer.<br/>
        /// <paramref name="item"/>이 이미 레이어에 있는 경우 발생합니다.
        /// </exception>
        public void Add(TItem item)
        {
            Attach(item);
            list.Add(item);
        }

        /// <summary>
        /// Adds <paramref name="items"/> to the end.<br/>
        /// <paramref name="items"/>를 끝에 추가합니다.
        /// </summary>
        /// <param name="items">
        /// The items to add. Already sorted items are arranged fastest.<br/>
        /// 추가할 아이템입니다. 이미 정렬되어 있으면 가장 빠르게 배치됩니다.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when one of <paramref name="items"/> is already in a layer. The items before it stay added.<br/>
        /// <paramref name="items"/> 중 하나가 이미 레이어에 있는 경우 발생합니다. 그 앞의 아이템은 추가된 채로 남습니다.
        /// </exception>
        public void AddRange(params ReadOnlySpan<TItem> items)
        {
            list.EnsureCapacity(list.Count + items.Length);
            for (int i = 0; i < items.Length; i++)
                Add(items[i]);
        }

        /// <inheritdoc cref="AddRange(ReadOnlySpan{TItem})"/>
        // An array converts to both other overloads, so it needs its own to avoid an ambiguous call.
        public void AddRange(TItem[] items) => AddRange(items.AsSpan());

        /// <inheritdoc cref="AddRange(ReadOnlySpan{TItem})"/>
        public void AddRange(IEnumerable<TItem> items)
        {
            foreach (TItem item in items)
                Add(item);
        }

        /// <summary>
        /// Inserts <paramref name="item"/> at <paramref name="index"/>.<br/>
        /// <paramref name="item"/>을 <paramref name="index"/> 위치에 삽입합니다.
        /// </summary>
        /// <param name="index">
        /// The index to insert at.<br/>
        /// 삽입할 인덱스입니다.
        /// </param>
        /// <param name="item">
        /// The item to insert.<br/>
        /// 삽입할 아이템입니다.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the collection.<br/>
        /// <paramref name="index"/>가 컬렉션 범위 밖인 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="item"/> is already in a layer.<br/>
        /// <paramref name="item"/>이 이미 레이어에 있는 경우 발생합니다.
        /// </exception>
        public void Insert(int index, TItem item)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)list.Count, nameof(index));

            Attach(item);
            list.Insert(index, item);
        }

        /// <summary>
        /// Removes <paramref name="item"/> from the layer.<br/>
        /// <paramref name="item"/>을 레이어에서 제거합니다.
        /// </summary>
        /// <param name="item">
        /// The item to remove.<br/>
        /// 제거할 아이템입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the item was removed; otherwise, <see langword="false"/>.<br/>
        /// 아이템을 제거했으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool Remove(TItem item)
        {
            if (item.layer != owner)
                return false;

            int index = list.IndexOf(item);
            if (index < 0)
                return false;

            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Removes the item at <paramref name="index"/>.<br/>
        /// <paramref name="index"/> 위치의 아이템을 제거합니다.
        /// </summary>
        /// <param name="index">
        /// The index of the item to remove.<br/>
        /// 제거할 아이템의 인덱스입니다.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the collection.<br/>
        /// <paramref name="index"/>가 컬렉션 범위 밖인 경우 발생합니다.
        /// </exception>
        public void RemoveAt(int index)
        {
            TItem item = list[index];
            list.RemoveAt(index);
            Detach(item);
        }

        /// <summary>
        /// Removes every item that matches <paramref name="match"/> in one pass.<br/>
        /// <paramref name="match"/>에 맞는 모든 아이템을 한 번에 제거합니다.
        /// </summary>
        /// <param name="match">
        /// Returns <see langword="true"/> for the items to remove.<br/>
        /// 제거할 아이템에 대해 <see langword="true"/>를 반환하는 함수입니다.
        /// </param>
        /// <returns>
        /// The number of removed items.<br/>
        /// 제거한 아이템 수를 반환합니다.
        /// </returns>
        public int RemoveAll(Predicate<TItem> match)
        {
            return list.RemoveAll(item =>
            {
                if (!match(item))
                    return false;

                Detach(item);
                return true;
            });
        }

        /// <summary>
        /// Removes all items.<br/>
        /// 모든 아이템을 제거합니다.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < list.Count; i++)
                Detach(list[i]);

            list.Clear();
        }

        /// <summary>
        /// Determines whether <paramref name="item"/> is in this collection.<br/>
        /// <paramref name="item"/>이 이 컬렉션에 있는지 확인합니다.
        /// </summary>
        /// <param name="item">
        /// The item to find.<br/>
        /// 찾을 아이템입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the item is in this collection; otherwise, <see langword="false"/>.<br/>
        /// 아이템이 이 컬렉션에 있으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool Contains(TItem item) => item.layer == owner;

        /// <summary>
        /// Gets the index of <paramref name="item"/>.<br/>
        /// <paramref name="item"/>의 인덱스를 가져옵니다.
        /// </summary>
        /// <param name="item">
        /// The item to find.<br/>
        /// 찾을 아이템입니다.
        /// </param>
        /// <returns>
        /// The index if found; otherwise, <c>-1</c>.<br/>
        /// 찾으면 인덱스를, 그렇지 않으면 <c>-1</c>을 반환합니다.
        /// </returns>
        public int IndexOf(TItem item) => item.layer == owner ? list.IndexOf(item) : -1;

        /// <inheritdoc/>
        public void CopyTo(TItem[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);

        /// <summary>
        /// Returns an enumerator over the items in collection order.<br/>
        /// 컬렉션 순서로 아이템을 열거하는 열거자를 반환합니다.
        /// </summary>
        /// <returns>
        /// The enumerator.<br/>
        /// 열거자를 반환합니다.
        /// </returns>
        public List<TItem>.Enumerator GetEnumerator() => list.GetEnumerator();

        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

        void Attach(TItem item)
        {
            if (item.layer != null)
                throw new InvalidOperationException("The item is already in a layer.");

            item.layer = owner;
            owner.OnItemAdded(item);
        }

        void Detach(TItem item)
        {
            owner.OnItemRemoved(item);
            item.layer = null;
        }
    }
}
