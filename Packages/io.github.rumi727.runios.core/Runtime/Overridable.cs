namespace RuniOS
{
    public sealed class Overridable<T>(T value)
    {
        public T rawValue { get; set; } = value;
        public T value => modifiers.Aggregate(rawValue, (current, item) => item.func.Invoke(current));
        
        readonly LinkedList<Modifier> modifiers = [];

        public IDisposable Override(Func<T, T> func, int order = 0)
        {
            Modifier modifier = new Modifier(func, order);
            LinkedListNode<Modifier> node;

            var current = modifiers.First;
            while (current != null && current.Value.order <= order)
                current = current.Next;

            if (current == null)
                node = modifiers.AddLast(modifier);
            else
                node = modifiers.AddBefore(current, modifier);

            return new Token(modifiers, node);
        }

        readonly struct Modifier(Func<T, T> func, int order)
        {
            public readonly Func<T, T> func = func;
            public readonly int order = order;

        }

        sealed class Token(LinkedList<Modifier> list, LinkedListNode<Modifier> node) : IDisposable
        {
            LinkedList<Modifier>? list = list;
            LinkedListNode<Modifier>? node = node;

            public void Dispose()
            {
                if (list == null || node == null)
                    return;

                if (node.List == list)
                    list.Remove(node);
                
                list = null;
                node = null;
            }
        }
    }
}