#nullable enable
using RuniOS.Spans;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.IO.Virtual
{
    public abstract class VirtualDirectoryBase : VirtualNode
    {
        /// <summary>
        /// 지정된 경로에 해당하는 <see cref="VirtualDirectory"/> 인스턴스를 캐싱하여 가져옵니다.<br/>
        /// 이 캐시는 가상 파일 시스템의 구조가 변경될 때 무효화되어야 합니다.
        /// </summary>
        protected Dictionary<FilePath, VirtualNode?> rootDirectoryCache
        {
            get
            {
                if (root == this)
                    return _rootDirectoryCache;
                else
                    return root!.rootDirectoryCache;
            }
        }
        readonly Dictionary<FilePath, VirtualNode?> _rootDirectoryCache = [];

        public abstract void AttachChild(string name, VirtualNode child);

        protected void BindChild(string name, VirtualNode child) => child.OnAttached(name, this);

        public abstract void SetChild(string name, VirtualNode child);

        public void DetachChild(string name)
        {
            ThrowIfInvalidNodeName(name);
            GetChildNode(name)?.Detach();
        }

        /// <summary>
        /// 지정된 경로에 해당하는 <see cref="VirtualNode"/> 인스턴스를 가져옵니다.
        /// 이 메서드는 내부 캐시를 사용하여 성능을 최적화합니다.
        /// </summary>
        /// <param name="path">가져올 디렉토리의 경로입니다. 예: "assets/runios/textures", "assets/runios/sounds"</param>
        /// <returns>
        /// 지정된 경로의 <see cref="VirtualNode"/> 인스턴스이거나,<br/>
        /// 해당 경로의 노드를 찾을 수 없는 경우 <see langword="null"/>을 반환합니다.
        /// </returns>
        public virtual VirtualNode? GetNode(FilePath path)
        {
            ThrowIfDeletedException();

            // 캐시에서 먼저 시도
            if (rootDirectoryCache.TryGetValue(fullPath + path, out VirtualNode? cachedNode))
            {
                // 캐시된 값이 null이라면 해당 경로에 노드가 없음을 의미
                return cachedNode;
            }

            if (path.IsEmpty())
            {
                rootDirectoryCache[fullPath + path] = this; // 이 인스턴스의 디렉토리 캐싱
                return this;
            }

            VirtualNode? childNode = this;
            VirtualDirectoryBase childDirectory = this;

            foreach (var directoryName in path.value.AsSpan().Split(FilePath.directorySeparatorChar))
            {
                if (childNode != childDirectory)
                {
                    // 경로 중간에 디렉토리가 아닌 노드가 있거나 노드를 찾지 못했을 경우 null 반환
                    rootDirectoryCache[fullPath + path] = null;
                    return null;
                }

                childNode = childDirectory.GetChildNode(new string(directoryName));
                if (childNode is VirtualDirectoryBase valueDirectory)
                    childDirectory = valueDirectory;
            }

            rootDirectoryCache[fullPath + path] = childNode;
            return childNode;
        }

        public abstract VirtualNode? GetChildNode(string name);

        public abstract IEnumerable<VirtualNode> EnumerateNode();

        protected internal abstract void OnDetachChild(VirtualNode child);

        /// <summary>
        /// 루트 디렉토리 인스턴스에 대한 캐시를 무효화합니다.
        /// </summary>
        public void InvalidateCache()
        {
            rootDirectoryCache.Clear();
            _rootDirectoryCache.Clear();
        }

        [DoesNotReturn]
        public static void ThrowNodeNotFound(string name) => throw new InvalidOperationException(/* TODO 예외 메시지 적기*/);
    }
}