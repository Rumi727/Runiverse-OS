#nullable enable
using System.Collections;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// The selected items of a grid. It is shared by all layers of the grid, and its order is not specified.<br/>
    /// 그리드에서 선택된 아이템들입니다. 그리드의 모든 레이어가 공유하며 순서는 정해져 있지 않습니다.
    /// </summary>
    /// <remarks>
    /// Methods that change several items raise <see cref="changed"/> once.<br/>
    /// 여러 아이템을 바꾸는 메서드는 <see cref="changed"/>를 한 번만 발생시킵니다.
    /// </remarks>
    public sealed class GridSelection : IReadOnlyCollection<GridItem>
    {
        readonly HashSet<GridItem> items = [];

        /// <summary>
        /// Occurs when the selected items change.<br/>
        /// 선택된 아이템이 바뀌면 발생합니다.
        /// </summary>
        public event Action? changed;

        /// <summary>
        /// Gets the number of selected items.<br/>
        /// 선택된 아이템 수를 가져옵니다.
        /// </summary>
        public int Count => items.Count;

        /// <summary>
        /// Gets the axis range from the earliest to the latest start of the selected items, or <see langword="null"/> when nothing is selected.<br/>
        /// Useful for keeping relative positions when copying.
        /// <br/><br/>
        /// 선택된 아이템의 가장 이른 시작부터 가장 늦은 시작까지의 축 범위를 가져옵니다. 선택이 없으면 <see langword="null"/>입니다.<br/>
        /// 복사할 때 상대 위치를 유지하는 데 유용합니다.
        /// </summary>
        public GridRange? range
        {
            get
            {
                if (items.Count == 0)
                    return null;

                double start = double.PositiveInfinity;
                double end = double.NegativeInfinity;
                foreach (GridItem item in items)
                {
                    start = Math.Min(start, item.start);
                    end = Math.Max(end, item.start);
                }

                return new GridRange(start, end);
            }
        }

        /// <summary>
        /// Determines whether <paramref name="item"/> is selected.<br/>
        /// <paramref name="item"/>이 선택되어 있는지 확인합니다.
        /// </summary>
        /// <param name="item">
        /// The item to check.<br/>
        /// 확인할 아이템입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the item is selected; otherwise, <see langword="false"/>.<br/>
        /// 아이템이 선택되어 있으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool Contains(GridItem item) => items.Contains(item);

        /// <summary>
        /// Selects <paramref name="item"/>.<br/>
        /// <paramref name="item"/>을 선택합니다.
        /// </summary>
        /// <param name="item">
        /// The item to select.<br/>
        /// 선택할 아이템입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the item was not selected before; otherwise, <see langword="false"/>.<br/>
        /// 아이템이 전에 선택되어 있지 않았으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool Add(GridItem item)
        {
            if (!items.Add(item))
                return false;

            changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Selects all of <paramref name="items"/>.<br/>
        /// <paramref name="items"/>를 모두 선택합니다.
        /// </summary>
        /// <param name="items">
        /// The items to select.<br/>
        /// 선택할 아이템입니다.
        /// </param>
        public void AddRange(IEnumerable<GridItem> items)
        {
            int count = this.items.Count;
            this.items.UnionWith(items);
            if (this.items.Count != count)
                changed?.Invoke();
        }

        /// <summary>
        /// Deselects <paramref name="item"/>.<br/>
        /// <paramref name="item"/>의 선택을 해제합니다.
        /// </summary>
        /// <param name="item">
        /// The item to deselect.<br/>
        /// 선택을 해제할 아이템입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the item was selected; otherwise, <see langword="false"/>.<br/>
        /// 아이템이 선택되어 있었으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool Remove(GridItem item)
        {
            if (!items.Remove(item))
                return false;

            changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Deselects all of <paramref name="items"/>.<br/>
        /// <paramref name="items"/>의 선택을 모두 해제합니다.
        /// </summary>
        /// <param name="items">
        /// The items to deselect.<br/>
        /// 선택을 해제할 아이템입니다.
        /// </param>
        public void RemoveRange(IEnumerable<GridItem> items)
        {
            if (this.items.Count == 0)
                return;

            int count = this.items.Count;
            this.items.ExceptWith(items);
            if (this.items.Count != count)
                changed?.Invoke();
        }

        /// <summary>
        /// Selects <paramref name="item"/> if it is not selected, and deselects it otherwise.<br/>
        /// <paramref name="item"/>이 선택되어 있지 않으면 선택하고, 선택되어 있으면 해제합니다.
        /// </summary>
        /// <param name="item">
        /// The item to toggle.<br/>
        /// 전환할 아이템입니다.
        /// </param>
        public void Toggle(GridItem item)
        {
            if (!items.Remove(item))
                items.Add(item);

            changed?.Invoke();
        }

        /// <summary>
        /// Replaces the selection with <paramref name="items"/>.<br/>
        /// 선택을 <paramref name="items"/>로 바꿉니다.
        /// </summary>
        /// <param name="items">
        /// The items to select.<br/>
        /// 선택할 아이템입니다.
        /// </param>
        public void Set(IEnumerable<GridItem> items)
        {
            HashSet<GridItem> next = [.. items];
            if (this.items.SetEquals(next))
                return;

            this.items.Clear();
            this.items.UnionWith(next);
            changed?.Invoke();
        }

        /// <summary>
        /// Replaces the selection with <paramref name="item"/> alone.<br/>
        /// 선택을 <paramref name="item"/> 하나로 바꿉니다.
        /// </summary>
        /// <param name="item">
        /// The item to select.<br/>
        /// 선택할 아이템입니다.
        /// </param>
        public void Set(GridItem item)
        {
            if (items.Count == 1 && items.Contains(item))
                return;

            items.Clear();
            items.Add(item);
            changed?.Invoke();
        }

        /// <summary>
        /// Deselects all items.<br/>
        /// 모든 아이템의 선택을 해제합니다.
        /// </summary>
        public void Clear()
        {
            if (items.Count == 0)
                return;

            items.Clear();
            changed?.Invoke();
        }

        /// <summary>
        /// Returns an enumerator over the selected items.<br/>
        /// 선택된 아이템을 열거하는 열거자를 반환합니다.
        /// </summary>
        /// <returns>
        /// The enumerator.<br/>
        /// 열거자를 반환합니다.
        /// </returns>
        public HashSet<GridItem>.Enumerator GetEnumerator() => items.GetEnumerator();

        IEnumerator<GridItem> IEnumerable<GridItem>.GetEnumerator() => items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
    }
}
