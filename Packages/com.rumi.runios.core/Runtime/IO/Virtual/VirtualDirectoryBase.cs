#nullable enable
using RuniOS.Spans;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace RuniOS.IO.Virtual
{
    /// <summary>
    /// Provides path-based directory operations for a virtual directory node.<br/>
    /// 가상 디렉터리 노드의 경로 기반 디렉터리 작업을 제공합니다.
    /// </summary>
    public abstract class VirtualDirectoryBase : VirtualNode
    {
        /// <summary>
        /// Gets the path lookup cache owned by the root directory.<br/>
        /// 루트 디렉터리가 소유한 경로 조회 캐시를 가져옵니다.
        /// </summary>
        protected Dictionary<RuniPath, VirtualNode> rootDirectoryCache
        {
            get
            {
                if (root == this)
                    return _rootDirectoryCache;
                else
                    return root!.rootDirectoryCache;
            }
        }
        readonly Dictionary<RuniPath, VirtualNode> _rootDirectoryCache = [];

        /// <summary>
        /// Attaches a child node at the specified path.<br/>
        /// 지정된 경로에 자식 노드를 연결합니다.
        /// </summary>
        /// <param name="path">
        /// The path where <paramref name="child"/> should be attached.<br/>
        /// <paramref name="child"/>를 연결할 경로입니다.
        /// </param>
        /// <param name="child">
        /// The node to attach.<br/>
        /// 연결할 노드입니다.
        /// </param>
        /// <exception cref="DirectoryNotFoundException">
        /// Thrown when the parent directory for <paramref name="path"/> does not exist.<br/>
        /// <paramref name="path"/>의 부모 디렉터리가 없는 경우 발생합니다.
        /// </exception>
        public void Attach(RuniPath path, VirtualNode child)
        {
            VirtualDirectoryBase? directory = GetNode(path.GetParentPath())?.AsDirectory();
            if (directory == null)
                ThrowDirectoryNotFound(path);

            directory.AttachChild(path.GetFileName(), child);
        }

        /// <summary>
        /// Attaches a child node with the specified name.<br/>
        /// 지정된 이름으로 자식 노드를 연결합니다.
        /// </summary>
        /// <param name="name">
        /// The child name to assign.<br/>
        /// 할당할 자식 이름입니다.
        /// </param>
        /// <param name="child">
        /// The child node to attach.<br/>
        /// 연결할 자식 노드입니다.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this directory or <paramref name="child"/> has been deleted.<br/>
        /// 이 디렉터리 또는 <paramref name="child"/>가 삭제된 경우 발생합니다.
        /// </exception>
        public abstract void AttachChild(string name, VirtualNode child);

        /// <summary>
        /// Binds a child node to this directory after it has been added to the directory storage.<br/>
        /// 자식 노드가 디렉터리 저장소에 추가된 뒤 이 디렉터리에 바인딩합니다.
        /// </summary>
        /// <param name="name">
        /// The child name assigned by this directory.<br/>
        /// 이 디렉터리가 할당한 자식 이름입니다.
        /// </param>
        /// <param name="child">
        /// The child node to bind.<br/>
        /// 바인딩할 자식 노드입니다.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when <paramref name="child"/> has been deleted.<br/>
        /// <paramref name="child"/>가 삭제된 경우 발생합니다.
        /// </exception>
        protected void BindChild(string name, VirtualNode child) => child.OnAttached(name, this);

        /// <summary>
        /// Sets the child node for the specified name, replacing an existing child when present.<br/>
        /// 지정된 이름의 자식 노드를 설정하며, 기존 자식이 있으면 교체합니다.
        /// </summary>
        /// <param name="name">
        /// The child name to set.<br/>
        /// 설정할 자식 이름입니다.
        /// </param>
        /// <param name="child">
        /// The child node to attach.<br/>
        /// 연결할 자식 노드입니다.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this directory or <paramref name="child"/> has been deleted.<br/>
        /// 이 디렉터리 또는 <paramref name="child"/>가 삭제된 경우 발생합니다.
        /// </exception>
        public abstract void SetChild(string name, VirtualNode child);

        /// <summary>
        /// Detaches the child node with the specified name if it exists.<br/>
        /// 지정된 이름의 자식 노드가 있으면 분리합니다.
        /// </summary>
        /// <param name="name">
        /// The child name to detach.<br/>
        /// 분리할 자식 이름입니다.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this directory has been deleted.<br/>
        /// 이 디렉터리가 삭제된 경우 발생합니다.
        /// </exception>
        public void DetachChild(string name)
        {
            ThrowIfDeletedException();
            ThrowIfInvalidNodeName(name);

            GetChildNode(name)?.Detach();
        }

        /// <summary>
        /// Creates a directory at the specified path, creating missing intermediate directories as needed.<br/>
        /// 지정된 경로에 디렉터리를 만들고, 필요한 경우 누락된 중간 디렉터리도 만듭니다.
        /// </summary>
        /// <param name="path">
        /// The directory path to create.<br/>
        /// 만들 디렉터리 경로입니다.
        /// </param>
        /// <param name="constructor">
        /// The factory used to create new directory nodes, or <see langword="null"/> to use <see cref="VirtualDirectory"/>.<br/>
        /// 새 디렉터리 노드를 만들 팩터리이며, <see langword="null"/>이면 <see cref="VirtualDirectory"/>를 사용합니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if any directory was created; otherwise, <see langword="false"/>.<br/>
        /// 디렉터리가 하나라도 생성되었으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <exception cref="DirectoryNotFoundException">
        /// Thrown when a path segment exists but is not a directory.<br/>
        /// 경로 세그먼트가 존재하지만 디렉터리가 아닌 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this directory has been deleted.<br/>
        /// 이 디렉터리가 삭제된 경우 발생합니다.
        /// </exception>
        public bool CreateDirectory(RuniPath path, Func<VirtualDirectoryBase>? constructor = null)
        {
            ThrowIfDeletedException();

            if (path.IsEmpty())
                return false; // 빈 경로는 false 반환 (유효하지 않은 요청)

            constructor ??= () => new VirtualDirectory();

            bool isCreated = false;
            VirtualDirectoryBase childDirectory = this;
            foreach (var directoryNameSpan in path.value.AsSpan().Split(RuniPath.directorySeparatorChar))
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
        /// Gets the node at the specified path.<br/>
        /// 지정된 경로의 노드를 가져옵니다.
        /// </summary>
        /// <param name="path">
        /// The path of the node to get.<br/>
        /// 가져올 노드의 경로입니다.
        /// </param>
        /// <returns>
        /// The matching <see cref="VirtualNode"/> if found; otherwise, <see langword="null"/>.<br/>
        /// 값을 찾은 경우 해당 <see cref="VirtualNode"/>를 반환하고, 그렇지 않으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this directory has been deleted.<br/>
        /// 이 디렉터리가 삭제된 경우 발생합니다.
        /// </exception>
        public virtual VirtualNode? GetNode(RuniPath path)
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

            foreach (var directoryName in path.value.AsSpan().Split(RuniPath.directorySeparatorChar))
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
        /// Gets the direct child node with the specified name.<br/>
        /// 지정된 이름의 직계 자식 노드를 가져옵니다.
        /// </summary>
        /// <param name="name">
        /// The child node name to resolve.<br/>
        /// 조회할 자식 노드 이름입니다.
        /// </param>
        /// <returns>
        /// The matching child node if found; otherwise, <see langword="null"/>.<br/>
        /// 값을 찾은 경우 해당 자식 노드를 반환하고, 그렇지 않으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="name"/> is not a valid node name.<br/>
        /// <paramref name="name"/>이 유효한 노드 이름이 아닌 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this directory has been deleted.<br/>
        /// 이 디렉터리가 삭제된 경우 발생합니다.
        /// </exception>
        public abstract VirtualNode? GetChildNode(string name);

        /// <summary>
        /// Gets the directory at the specified path.<br/>
        /// 지정된 경로의 디렉터리를 가져옵니다.
        /// </summary>
        /// <param name="path">
        /// The path of the directory to get.<br/>
        /// 가져올 디렉터리의 경로입니다.
        /// </param>
        /// <returns>
        /// The matching <see cref="VirtualDirectoryBase"/> if found; otherwise, <see langword="null"/>.<br/>
        /// 값을 찾은 경우 해당 <see cref="VirtualDirectoryBase"/>를 반환하고, 그렇지 않으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this directory has been deleted.<br/>
        /// 이 디렉터리가 삭제된 경우 발생합니다.
        /// </exception>
        public virtual VirtualDirectoryBase? GetDirectory(RuniPath path)
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
        /// Gets the file at the specified path.<br/>
        /// 지정된 경로의 파일을 가져옵니다.
        /// </summary>
        /// <param name="path">
        /// The path of the file to get.<br/>
        /// 가져올 파일의 경로입니다.
        /// </param>
        /// <returns>
        /// The matching <see cref="VirtualFileBase"/> if found; otherwise, <see langword="null"/>.<br/>
        /// 값을 찾은 경우 해당 <see cref="VirtualFileBase"/>를 반환하고, 그렇지 않으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this directory has been deleted.<br/>
        /// 이 디렉터리가 삭제된 경우 발생합니다.
        /// </exception>
        public virtual VirtualFileBase? GetFile(RuniPath path)
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
        /// Gets the file at the specified path, or creates it when it does not exist.<br/>
        /// 지정된 경로의 파일을 가져오며, 파일이 없으면 새로 만듭니다.
        /// </summary>
        /// <param name="path">
        /// The path of the file to get or create.<br/>
        /// 가져오거나 만들 파일의 경로입니다.
        /// </param>
        /// <returns>
        /// The existing or newly created <see cref="VirtualFileBase"/>.<br/>
        /// 기존 또는 새로 만든 <see cref="VirtualFileBase"/>를 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this directory has been deleted.<br/>
        /// 이 디렉터리가 삭제된 경우 발생합니다.
        /// </exception>
        public virtual VirtualFileBase GetOrCreateFile(RuniPath path)
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
        /// Enumerates all descendant nodes recursively.<br/>
        /// 모든 하위 노드를 재귀적으로 열거합니다.
        /// </summary>
        /// <returns>
        /// A sequence containing every descendant node under this directory.<br/>
        /// 이 디렉터리 아래의 모든 하위 노드를 포함하는 시퀀스입니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this directory has been deleted.<br/>
        /// 이 디렉터리가 삭제된 경우 발생합니다.
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
        /// Enumerates the direct child nodes of this directory.<br/>
        /// 이 디렉터리의 직계 자식 노드를 열거합니다.
        /// </summary>
        /// <returns>
        /// A sequence containing direct child nodes.<br/>
        /// 직계 자식 노드를 포함하는 시퀀스입니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this directory has been deleted.<br/>
        /// 이 디렉터리가 삭제된 경우 발생합니다.
        /// </exception>
        public abstract IEnumerable<VirtualNode> EnumerateChildNodes();

        /// <summary>
        /// Removes the specified attached child from this directory's child storage.<br/>
        /// 지정된 연결된 자식을 이 디렉터리의 자식 저장소에서 제거합니다.
        /// </summary>
        /// <param name="child">
        /// The attached child node to remove.<br/>
        /// 제거할 연결된 자식 노드입니다.
        /// </param>
        protected internal abstract void OnDetachChild(VirtualNode child);

        /// <summary>
        /// Clears cached path lookups for this directory and its root directory.<br/>
        /// 이 디렉터리와 루트 디렉터리의 경로 조회 캐시를 지웁니다.
        /// </summary>
        public void InvalidateCache()
        {
            rootDirectoryCache.Clear();
            _rootDirectoryCache.Clear();
        }

        /// <summary>
        /// Throws an exception indicating that a node was not found at the specified path.<br/>
        /// 지정된 경로에서 노드를 찾을 수 없음을 나타내는 예외를 발생시킵니다.
        /// </summary>
        /// <param name="path">
        /// The missing node path.<br/>
        /// 찾을 수 없는 노드 경로입니다.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Always thrown.<br/>
        /// 항상 발생합니다.
        /// </exception>
        [DoesNotReturn]
        public static void ThrowNodeNotFound(RuniPath path) => throw new InvalidOperationException($"The node at path '{path}' was not found.");

        /// <summary>
        /// Throws an exception indicating that a directory was not found at the specified path.<br/>
        /// 지정된 경로에서 디렉터리를 찾을 수 없음을 나타내는 예외를 발생시킵니다.
        /// </summary>
        /// <param name="path">
        /// The missing directory path.<br/>
        /// 찾을 수 없는 디렉터리 경로입니다.
        /// </param>
        /// <exception cref="DirectoryNotFoundException">
        /// Always thrown.<br/>
        /// 항상 발생합니다.
        /// </exception>
        [DoesNotReturn]
        public static void ThrowDirectoryNotFound(RuniPath path) => throw new DirectoryNotFoundException($"The directory at path '{path}' was not found.");

        /// <summary>
        /// Throws an exception indicating that a file was not found at the specified path.<br/>
        /// 지정된 경로에서 파일을 찾을 수 없음을 나타내는 예외를 발생시킵니다.
        /// </summary>
        /// <param name="path">
        /// The missing file path.<br/>
        /// 찾을 수 없는 파일 경로입니다.
        /// </param>
        /// <exception cref="FileNotFoundException">
        /// Always thrown.<br/>
        /// 항상 발생합니다.
        /// </exception>
        [DoesNotReturn]
        public static void ThrowFileNotFound(RuniPath path) => throw new FileNotFoundException($"The file at path '{path}' was not found.");

        /// <summary>
        /// Throws an exception indicating that the directory path is invalid.<br/>
        /// 디렉터리 경로가 유효하지 않음을 나타내는 예외를 발생시킵니다.
        /// </summary>
        /// <param name="path">
        /// The invalid directory path.<br/>
        /// 유효하지 않은 디렉터리 경로입니다.
        /// </param>
        /// <exception cref="DirectoryNotFoundException">
        /// Always thrown.<br/>
        /// 항상 발생합니다.
        /// </exception>
        [DoesNotReturn]
        public static void ThrowInvalidDirectoryException(RuniPath path) => throw new DirectoryNotFoundException($"The directory at path '{path}' was invalid.");

        /// <summary>
        /// Throws an exception indicating that a directory exists where a file was expected.<br/>
        /// 파일이 필요한 위치에 디렉터리가 있음을 나타내는 예외를 발생시킵니다.
        /// </summary>
        /// <param name="path">
        /// The full path that caused the failure.<br/>
        /// 실패를 일으킨 전체 경로입니다.
        /// </param>
        /// <param name="segmentName">
        /// The path segment that was not a file.<br/>
        /// 파일이 아니었던 경로 세그먼트입니다.
        /// </param>
        /// <exception cref="UnauthorizedAccessException">
        /// Always thrown.<br/>
        /// 항상 발생합니다.
        /// </exception>
        [DoesNotReturn]
        public static void ThrowPathIsDirectoryException(RuniPath path, string segmentName)
        {
            throw new UnauthorizedAccessException(
                $"Path operation failed for '{path}'. " +
                $"The segment '{segmentName}' is a directory or another non-file item, " +
                $"but a file was expected. Direct file operations on a directory are not permitted."
            );
        }


        /// <summary>
        /// Throws an exception indicating that a file exists where a directory was expected.<br/>
        /// 디렉터리가 필요한 위치에 파일이 있음을 나타내는 예외를 발생시킵니다.
        /// </summary>
        /// <param name="path">
        /// The full path that caused the failure.<br/>
        /// 실패를 일으킨 전체 경로입니다.
        /// </param>
        /// <param name="segmentName">
        /// The path segment that was not a directory.<br/>
        /// 디렉터리가 아니었던 경로 세그먼트입니다.
        /// </param>
        /// <exception cref="DirectoryNotFoundException">
        /// Always thrown.<br/>
        /// 항상 발생합니다.
        /// </exception>
        [DoesNotReturn]
        public static void ThrowPathIsFileException(RuniPath path, string segmentName)
        {
            throw new DirectoryNotFoundException(
                $"Path operation failed for '{path}'. " +
                $"The segment '{segmentName}' is a file or another non-directory item, " +
                $"but a directory was expected."
            );
        }
    }
}
