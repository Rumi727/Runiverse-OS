#nullable enable
// Source: https://referencesource.microsoft.com/#System.ServiceModel/System/ServiceModel/SynchronizedCollection.cs
// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace System.Collections.Generic
{
    [Runtime.InteropServices.ComVisible(false)]
    public class SynchronizedCollection<T> : IList<T>, IList, IReadOnlyList<T>
    {
        public readonly List<T> internalList;
        public readonly object internalSync = new();

        public SynchronizedCollection() => internalList = new List<T>();

        public SynchronizedCollection(IEnumerable<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            internalList = new List<T>(list);
        }

        public SynchronizedCollection(params T[] list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            internalList = new List<T>(list);
        }

        public int Count
        {
            get
            {
                lock (internalSync)
                {
                    return internalList.Count;
                }
            }
        }

#pragma warning disable IDE1006 // 명명 스타일
        protected List<T> Items => internalList;
#pragma warning restore IDE1006 // 명명 스타일

        public T this[int index]
        {
            get
            {
                lock (internalSync)
                {
                    return internalList[index];
                }
            }
            set
            {
                lock (internalSync)
                {
                    SetItem(index, value);
                }
            }
        }

        public void Add(T item)
        {
            lock (internalSync)
            {
                int index = internalList.Count;
                InsertItem(index, item);
            }
        }

        public void Clear()
        {
            lock (internalSync)
            {
                ClearItems();
            }
        }

        public void CopyTo(T[] array, int index)
        {
            lock (internalSync)
            {
                internalList.CopyTo(array, index);
            }
        }

        public bool Contains(T item)
        {
            lock (internalSync)
            {
                return internalList.Contains(item);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (internalSync)
            {
                return internalList.GetEnumerator();
            }
        }

        public int IndexOf(T item)
        {
            lock (internalSync)
            {
                return InternalIndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (internalSync)
            {
                if (index < 0 || index > internalList.Count)
                    throw new ArgumentOutOfRangeException();

                InsertItem(index, item);
            }
        }

        int InternalIndexOf(T item)
        {
            int count = internalList.Count;

            for (int i = 0; i < count; i++)
            {
                if (Equals(internalList[i], item))
                    return i;
            }
            return -1;
        }

        public bool Remove(T item)
        {
            lock (internalSync)
            {
                int index = InternalIndexOf(item);
                if (index < 0)
                    return false;

                RemoveItem(index);
                return true;
            }
        }

        public void RemoveAt(int index)
        {
            lock (internalSync)
            {
                if (index < 0 || index >= internalList.Count)
                    throw new ArgumentOutOfRangeException();

                RemoveItem(index);
            }
        }

        protected virtual void ClearItems() => internalList.Clear();

        protected virtual void InsertItem(int index, T item) => internalList.Insert(index, item);

        protected virtual void RemoveItem(int index) => internalList.RemoveAt(index);

        protected virtual void SetItem(int index, T item) => internalList[index] = item;

        bool ICollection<T>.IsReadOnly => false;

        IEnumerator IEnumerable.GetEnumerator() => ((IList)internalList).GetEnumerator();

        bool ICollection.IsSynchronized => true;

        object ICollection.SyncRoot => internalSync;

        void ICollection.CopyTo(Array array, int index)
        {
            lock (internalSync)
            {
                ((IList)internalList).CopyTo(array, index);
            }
        }

        object? IList.this[int index]
        {
            get => this[index];
            set
            {
                VerifyValueType(value);
                this[index] = (T)value!;
            }
        }

        bool IList.IsReadOnly => false;

        bool IList.IsFixedSize => false;

        int IList.Add(object? value)
        {
            VerifyValueType(value);

            lock (internalSync)
            {
                Add((T)value!);
                return Count - 1;
            }
        }

        bool IList.Contains(object? value)
        {
            VerifyValueType(value);
            return Contains((T)value!);
        }

        int IList.IndexOf(object? value)
        {
            VerifyValueType(value);
            return IndexOf((T)value!);
        }

        void IList.Insert(int index, object? value)
        {
            VerifyValueType(value);
            Insert(index, (T)value!);
        }

        void IList.Remove(object? value)
        {
            VerifyValueType(value);
            Remove((T)value!);
        }

        static void VerifyValueType(object? value)
        {
            if (value == null)
            {
                if (typeof(T).IsValueType)
                    throw new ArgumentException();
            }
            else if (value is not T)
                throw new ArgumentException();
        }
    }
}