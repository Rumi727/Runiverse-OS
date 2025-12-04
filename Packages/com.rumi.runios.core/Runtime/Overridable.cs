namespace RuniOS
{
    public sealed class Overridable<T>
    {
        public Overridable(T value) => rawValue = value;
        
        public T rawValue { get; set; }
        public T value => modifiers.Aggregate(rawValue, (current, item) => item.func.Invoke(current));
        
        readonly LinkedList<Modifier> modifiers = new LinkedList<Modifier>();

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

        readonly struct Modifier
        {
            public readonly Func<T, T> func;
            public readonly int order;
            
            public Modifier(Func<T, T> func, int order)
            {
                this.func = func;
                this.order = order;
            }
        }

        sealed class Token : IDisposable
        {
            LinkedList<Modifier>? list;
            LinkedListNode<Modifier>? node;

            public Token(LinkedList<Modifier> list, LinkedListNode<Modifier> node)
            {
                this.list = list;
                this.node = node;
            }
            
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