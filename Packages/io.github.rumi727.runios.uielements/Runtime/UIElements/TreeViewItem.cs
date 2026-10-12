#nullable enable
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    public class TreeViewItem<T> : IList<TreeViewItem<T>>
    {
        static readonly ConditionalWeakTable<IEnumerable<TreeViewItemData<T>>, TreeViewItem<T>> owners = new();

        public TreeViewItem(int id, T entry) : this(new TreeViewItemData<T>(id, entry, [])) => owners.Add(rawChildren, this);

        TreeViewItem(TreeViewItemData<T> rawItem)
        {
            this.rawItem = rawItem;
            rawChildren = (IList<TreeViewItemData<T>>)rawItem.children;
        }

        readonly TreeViewItemData<T> rawItem;
        readonly IList<TreeViewItemData<T>> rawChildren;

        public TreeViewItem<T> this[int index]
        {
            get => FromItemData(rawChildren[index]);
            set => rawChildren[index] = value.rawItem;
        }

        public int count => rawChildren.Count;
        int ICollection<TreeViewItem<T>>.Count => rawChildren.Count;

        bool ICollection<TreeViewItem<T>>.IsReadOnly => false;

        public void Add(TreeViewItem<T> item) => rawChildren.Add(item.rawItem);
        public void Insert(int index, TreeViewItem<T> item) => rawChildren.Insert(index, item.rawItem);

        public bool Remove(TreeViewItem<T> item)
        {
            int index = IndexOf(item);
            if (index < 0)
                return false;

            rawChildren.RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index) => rawChildren.RemoveAt(index);
        public void Clear() => rawChildren.Clear();

        public int IndexOf(TreeViewItem<T> item)
        {
            for (int i = 0; i < rawChildren.Count; i++)
            {
                if (ReferenceEquals(rawChildren[i].children, item.rawChildren))
                    return i;
            }

            return -1;
        }

        public bool Contains(TreeViewItem<T> item) => IndexOf(item) >= 0;

        public void CopyTo(TreeViewItem<T>[] array, int arrayIndex)
        {
            for (int i = 0; i < rawChildren.Count; i++)
                array[arrayIndex + i] = FromItemData(rawChildren[i]);
        }

        public IEnumerator<TreeViewItem<T>> GetEnumerator() => rawChildren.Select(FromItemData).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TreeViewItemData<T> ToItemData() => rawItem;
        public static TreeViewItem<T> FromItemData(TreeViewItemData<T> item) => owners.GetValue(item.children, _ => new TreeViewItem<T>(item));

        public static implicit operator TreeViewItemData<T>(TreeViewItem<T> item) => item.ToItemData();
    }
}
