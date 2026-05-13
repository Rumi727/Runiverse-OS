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
        protected Dictionary<FilePath, VirtualNode> rootDirectoryCache
        {
            get
            {
                if (root == this)
                    return _rootDirectoryCache;
                else
                    return root!.rootDirectoryCache;
            }
        }
        readonly Dictionary<FilePath, VirtualNode> _rootDirectoryCache = [];

        public void Attach(FilePath path, VirtualNode child)
        {
            VirtualDirectoryBase? directory = GetNode(path.GetParentPath())?.AsDirectory();
            if (directory == null)
                ThrowDirectoryNotFound(path);

            directory.AttachChild(path.GetFileName(), child);
        }

        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectoryBase"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public abstract void AttachChild(string name, VirtualNode child);

        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectoryBase"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        protected void BindChild(string name, VirtualNode child) => child.OnAttached(name, this);

        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectoryBase"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public abstract void SetChild(string name, VirtualNode child);

        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectoryBase"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public void DetachChild(string name)
        {
            ThrowIfDeletedException();
            ThrowIfInvalidNodeName(name);

            GetChildNode(name)?.Detach();
        }

        /// <summary>
        /// 지정된 경로에 새로운 디렉토리를 생성합니다.<br/>
        /// 중간 경로가 없으면 자동으로 생성됩니다.
        /// </summary>
        /// <param name="path">생성할 디렉토리의 경로입니다. 예: "assets/runios/textures", "assets/runios/sounds"</param>
        /// <param name="constructor">디렉토리를 생성하기 위해 호출되는 함수입니다. <see langword="null"/>이라면 기본값을 사용합니다.</param>
        /// <returns>
        /// 디렉토리가 성공적으로 생성되었거나 이미 존재하여 접근할 수 있는 경우 <see langword="true"/>를 반환하고,<br/>
        /// 경로가 비어있는 경우 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <exception cref="DirectoryNotFoundException">
        /// 경로의 주어진 세그먼트가 디렉토리가 아닌 다른 유형의 항목일 때 발생합니다.<br/>
        /// 예를 들어, 디렉토리를 생성하거나 찾으려는데 경로 중간 또는 마지막에 파일이 존재하는 경우,
        /// 시스템은 기대하는 디렉토리를 찾을 수 없으므로 이 예외를 발생시킵니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectoryBase"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public bool CreateDirectory(FilePath path, Func<VirtualDirectoryBase>? constructor = null)
        {
            ThrowIfDeletedException();

            if (path.IsEmpty())
                return false; // 빈 경로는 false 반환 (유효하지 않은 요청)

            constructor ??= () => new VirtualDirectory();

            bool isCreated = false;
            VirtualDirectoryBase childDirectory = this;
            foreach (var directoryNameSpan in path.value.AsSpan().Split(FilePath.directorySeparatorChar))
            {
                string directoryName = new string(directoryNameSpan);
                VirtualNode? childNode = childDirectory.GetChildNode(directoryName);
                if (childNode != null)
                {
                    if (childNode is VirtualDirectoryBase dirNode)
                        childDirectory = dirNode;
                    else
                    {
                        // 경로 중간에 파일이나 디렉토리가 아닌 다른 노드가 있는 경우
                        // 이는 비정상적인 상황이므로 예외를 던집니다.
                        ThrowPathIsFileException(path, directoryName);
                    }
                }
                else
                {
                    VirtualDirectoryBase directory = constructor.Invoke();

                    childDirectory.AttachChild(directoryName, directory);
                    childDirectory = directory;

                    isCreated = true;
                }
            }

            return isCreated;
        }

        /// <summary>
        /// 지정된 경로에 해당하는 노드를 가져옵니다.
        /// </summary>
        /// <param name="path">가져올 노드의 경로입니다. 예: "assets/runios/textures", "assets/runios/sounds"</param>
        /// <returns>
        /// 지정된 경로의 <see cref="VirtualNode"/> 인스턴스이거나,<br/>
        /// 해당 경로의 노드를 찾을 수 없는 경우 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectoryBase"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public virtual VirtualNode? GetNode(FilePath path)
        {
            ThrowIfDeletedException();

            // 캐시에서 먼저 시도
            if (rootDirectoryCache.TryGetValue(fullPath + path, out VirtualNode cachedNode))
                return cachedNode;

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
                    return null;
                }

                childNode = childDirectory.GetChildNode(new string(directoryName));
                if (childNode is VirtualDirectoryBase valueDirectory)
                    childDirectory = valueDirectory;
            }

            if (childNode != null)
                rootDirectoryCache[fullPath + path] = childNode;

            return childNode;
        }

        /// <summary>
        /// 지정된 이름에 해당하는 직계 노드를 가져옵니다.
        /// </summary>
        /// <returns>
        /// 지정된 이름의 <see cref="VirtualNode"/> 인스턴스이거나,<br/>
        /// 해당 이름의 직계 노드를 찾을 수 없는 경우 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// 지정한 이름이 잘못된 노드 이름일 때 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectoryBase"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public abstract VirtualNode? GetChildNode(string name);

        /// <summary>
        /// 지정된 경로에 해당하는 디렉토리를 가져옵니다.
        /// </summary>
        /// <param name="path">가져올 디렉토리의 경로입니다. 예: "assets/runios/textures", "assets/runios/sounds"</param>
        /// <returns>
        /// 지정된 경로의 <see cref="VirtualDirectoryBase"/> 인스턴스이거나,<br/>
        /// 해당 경로의 디렉토리를 찾을 수 없는 경우 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectoryBase"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public virtual VirtualDirectoryBase? GetDirectory(FilePath path)
        {
            VirtualDirectoryBase? directory = GetNode(path.GetParentPath())?.AsDirectory();
            if (directory == null)
                return null;

            string fileName = path.GetFileName();
            VirtualNode? childNode = directory.GetChildNode(fileName);

            switch (childNode)
            {
                case VirtualDirectoryBase childDirectory:
                    return childDirectory;
                case null:
                    return null;
                default:
                    ThrowPathIsFileException(path, fileName);
                    throw null;
            }
        }

        /// <summary>
        /// 지정된 경로에 해당하는 파일을 가져옵니다.<br/>
        /// 지정된 경로에 파일이 없다면, 새로 만듭니다.
        /// </summary>
        /// <param name="path">가져올 파일의 경로입니다. 예: "assets/runios/textures/monster.png", "assets/runios/sounds/attack.ogg"</param>
        /// <returns>
        /// 지정된 경로의 <see cref="VirtualFileBase"/> 인스턴스이거나,<br/>
        /// 해당 경로의 파일을 찾을 수 없는 경우 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectoryBase"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public virtual VirtualFileBase? GetFile(FilePath path)
        {
            VirtualDirectoryBase? directory = GetNode(path.GetParentPath())?.AsDirectory();
            if (directory == null)
                ThrowDirectoryNotFound(path);

            string fileName = path.GetFileName();
            VirtualNode? node = directory.GetChildNode(fileName);

            switch (node)
            {
                case VirtualFileBase file:
                    return file;
                case null:
                    return null;
                default:
                    ThrowPathIsDirectoryException(path, fileName);
                    throw null;
            }
        }

        /// <summary>
        /// 지정된 경로에 해당하는 파일을 가져옵니다.<br/>
        /// 지정된 경로에 파일이 없다면, 새로 만듭니다.
        /// </summary>
        /// <param name="path">가져올 파일의 경로입니다. 예: "assets/runios/textures", "assets/runios/sounds"</param>
        /// <returns>
        /// 지정된 경로의 <see cref="VirtualFileBase"/> 인스턴스입니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectoryBase"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public virtual VirtualFileBase GetOrCreateFile(FilePath path)
        {
            VirtualDirectoryBase? directory = GetNode(path.GetParentPath())?.AsDirectory();
            if (directory == null)
                ThrowDirectoryNotFound(path);

            string fileName = path.GetFileName();
            VirtualNode? node = directory.GetChildNode(fileName);

            switch (node)
            {
                case VirtualFileBase file:
                    return file;
                case null:
                {
                    VirtualFile file = new VirtualFile();
                    directory.AttachChild(fileName, file);

                    return file;
                }
                default:
                    ThrowPathIsDirectoryException(path, fileName);
                    throw null;
            }
        }

        /// <summary>
        /// 모든 하위 디렉토리의 노드를 포함하여 모든 노드를 열거합니다.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectoryBase"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
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
        /// 이 <see cref="VirtualDirectoryBase"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
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

        /// <summary>
        /// 항상 예외를 던집니다.<br/>
        /// 이는 경로 중간에 파일이 있어 티렉토리를 탐색할 수 없는 상황에 사용됩니다.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException">
        /// 경로 중간에 파일이 있어 디렉토리를 탐색할 수 없는 경우 발생합니다.
        /// </exception>
        [DoesNotReturn]
        public static void ThrowInvalidDirectoryException(FilePath path) => throw new DirectoryNotFoundException($"The directory at path '{path}' was invalid.");

        /// <summary>
        /// 항상 예외를 던집니다.<br/>
        /// 이는 파일을 기대했지만 실제로는 디렉토리가 존재하는 상황에 사용됩니다.
        /// </summary>
        /// <param name="path">문제가 발생한 전체 경로입니다.</param>
        /// <param name="segmentName">파일이 아닌 항목의 이름(문제의 원인이 된 경로 세그먼트)입니다.</param>
        /// <exception cref="UnauthorizedAccessException">
        /// 경로의 주어진 세그먼트가 파일이 아닌 다른 유형의 항목일 때 발생합니다.<br/>
        /// 예를 들어, 파일을 삭제하거나 찾으려는데 경로의 해당 위치에 디렉토리가 존재하는 경우,
        /// 해당 디렉토리에 대한 파일 작업이 허용되지 않음을 나타냅니다.
        /// </exception>
        [DoesNotReturn]
        public static void ThrowPathIsDirectoryException(FilePath path, string segmentName)
        {
            throw new UnauthorizedAccessException(
                $"Path operation failed for '{path}'. " +
                $"The segment '{segmentName}' is a directory or another non-file item, " +
                $"but a file was expected. Direct file operations on a directory are not permitted."
            );
        }


        /// <summary>
        /// 항상 예외를 던집니다.<br/>
        /// 이는 디렉토리를 기대했지만 실제로는 디렉토리가 아닌 다른 유형의 항목인 상황에 사용됩니다.
        /// </summary>
        /// <param name="path">문제가 발생한 전체 경로입니다.</param>
        /// <param name="segmentName">디렉토리가 아닌 항목의 이름(문제의 원인이 된 경로 세그먼트)입니다.</param>
        /// <exception cref="DirectoryNotFoundException">
        /// 경로의 주어진 세그먼트가 디렉토리가 아닌 다른 유형의 항목일 때 발생합니다.<br/>
        /// 예를 들어, 디렉토리를 생성하거나 찾으려는데 경로 중간 또는 마지막에 파일이 존재하는 경우,
        /// 시스템은 기대하는 디렉토리를 찾을 수 없으므로 이 예외를 발생시킵니다.
        /// </exception>
        [DoesNotReturn]
        public static void ThrowPathIsFileException(FilePath path, string segmentName)
        {
            throw new DirectoryNotFoundException(
                $"Path operation failed for '{path}'. " +
                $"The segment '{segmentName}' is a file or another non-directory item, " +
                $"but a directory was expected."
            );
        }
    }
}