#nullable enable
namespace RuniOS.IO.Virtual
{
    /// <summary>
    /// Stores child virtual nodes in memory and resolves them by name.<br/>
    /// 자식 가상 노드를 메모리에 저장하고 이름으로 조회합니다.
    /// </summary>
    public class VirtualDirectory : VirtualDirectoryBase
    {
        readonly SortedDictionary<string, VirtualNode> children = [];

        /// <inheritdoc/>
        public override VirtualNode? GetChildNode(string name)
        {
            ThrowIfDeletedException();
            ThrowIfInvalidNodeName(name);

            return children.GetValueOrDefault(name);
        }

        /// <inheritdoc/>
        public override IEnumerable<VirtualNode> EnumerateChildNodes()
        {
            ThrowIfDeletedException();
            return children.Values;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        protected internal override void OnDetachChild(VirtualNode child)
        {
            ThrowIfDeletedException();
            
            child.ThrowIfNotAttachedException();
            if (!children.ContainsKey(child.name))
                ThrowNodeNotFound((RuniPath)child.name);

            InvalidateCache(); // 디렉토리 구조 변경 전에 캐시 무효화
            children.Remove(child.name);
        }

        /// <inheritdoc/>
        public override void OnDelete()
        {
            InvalidateCache(); // 디렉토리 구조 변경 전에 캐시 무효화

            foreach (var item in children.ToList())
                item.Value.Delete();
            
            children.Clear();
        }
    }
}
