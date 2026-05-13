#nullable enable
namespace RuniOS.IO.Virtual
{
    public class VirtualDirectory : VirtualDirectoryBase
    {
        readonly SortedDictionary<string, VirtualNode> children = [];

        public override VirtualNode? GetChildNode(string name)
        {
            ThrowIfDeletedException();
            ThrowIfInvalidNodeName(name);

            return children.GetValueOrDefault(name);
        }

        public override IEnumerable<VirtualNode> EnumerateChildNodes()
        {
            ThrowIfDeletedException();
            return children.Values;
        }

        public override void AttachChild(string name, VirtualNode child)
        {
            ThrowIfDeletedException();
            ThrowIfInvalidNodeName(name);

            child.ThrowIfDeletedException();
            child.ThrowIfAttachedException();

            GetChildNode(name)?.ThrowIfAttachedException();

            InvalidateCache(); // 디렉토리 구조 변경 전에 캐시 무효화
            children[name] = child;

            BindChild(name, child);
        }

        public override void SetChild(string name, VirtualNode child)
        {
            ThrowIfDeletedException();
            ThrowIfInvalidNodeName(name);

            child.ThrowIfDeletedException();
            child.ThrowIfAttachedException();

            GetChildNode(name)?.Detach();

            InvalidateCache(); // 디렉토리 구조 변경 전에 캐시 무효화
            children[name] = child;

            BindChild(name, child);
        }

        protected internal override void OnDetachChild(VirtualNode child)
        {
            ThrowIfDeletedException();
            
            child.ThrowIfNotAttachedException();
            if (!children.ContainsKey(child.name))
                ThrowNodeNotFound(child.name);

            InvalidateCache(); // 디렉토리 구조 변경 전에 캐시 무효화
            children.Remove(child.name);
        }

        public override void OnDelete()
        {
            InvalidateCache(); // 디렉토리 구조 변경 전에 캐시 무효화

            foreach (var item in children.ToList())
                item.Value.Delete();
            
            children.Clear();
        }
    }
}