#nullable enable
using RuniOS.Spans;
using System.Diagnostics.CodeAnalysis;
using System.IO;

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

        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public abstract void AttachChild(string name, VirtualNode child);

        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        protected void BindChild(string name, VirtualNode child) => child.OnAttached(name, this);

        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public abstract void SetChild(string name, VirtualNode child);

        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public void DetachChild(string name)
        {
            ThrowIfDeletedException();
            ThrowIfInvalidNodeName(name);

            GetChildNode(name)?.Detach();
        }

        /// <summary>
        /// 지정된 경로에 해당하는 노드를 가져옵니다.
        /// 이 메서드는 내부 캐시를 사용하여 성능을 최적화합니다.
        /// </summary>
        /// <param name="path">가져올 디렉토리의 경로입니다. 예: "assets/runios/textures", "assets/runios/sounds"</param>
        /// <returns>
        /// 지정된 경로의 <see cref="VirtualNode"/> 인스턴스이거나,<br/>
        /// 해당 경로의 노드를 찾을 수 없는 경우 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
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

        /// <summary>
        /// 지정된 이름에 해당하는 직계 노드를 가져옵니다.
        /// 이 메서드는 내부 캐시를 사용하여 성능을 최적화합니다.
        /// </summary>
        /// <returns>
        /// 지정된 이름의 <see cref="VirtualNode"/> 인스턴스이거나,<br/>
        /// 해당 이름의 직계 노드를 찾을 수 없는 경우 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// 지정한 이름이 잘못된 노드 이름일 때 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public abstract VirtualNode? GetChildNode(string name);

        /// <summary>
        /// 모든 하위 디렉토리의 노드를 포함하여 모든 노드를 열거합니다.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public virtual IEnumerable<VirtualNode> EnumerateNodes()
        {
            ThrowIfDeletedException();

            VirtualDirectoryBase node = this;
            foreach (var childNode in node.EnumerateChildNodes())
            {
                if (childNode is VirtualDirectoryBase childDirectory)
                {
                    foreach (var childNode2 in childDirectory.EnumerateNodes())
                        yield return childNode2;
                }

                yield return childNode;
            }
        }

        /// <summary>
        /// 모든 직계 노드를 열거합니다.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public abstract IEnumerable<VirtualNode> EnumerateChildNodes();

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
        public static void ThrowNodeNotFound(FilePath path) => throw new InvalidOperationException($"The node at path '{path}' was not found.");

        [DoesNotReturn]
        public static void ThrowDirectoryNotFound(FilePath path) => throw new DirectoryNotFoundException($"The directory at path '{path}' was not found.");

        [DoesNotReturn]
        public static void ThrowFileNotFound(FilePath path) => throw new FileNotFoundException($"The file at path '{path}' was not found.");
    }
}