#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Spans;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace RuniOS.IO
{
    /// <summary>
    /// 가상 파일 시스템 내의 디렉토리를 나타내는 클래스입니다.<br/>
    /// 이 클래스는 계층적 디렉토리 구조를 관리하며, 하위 디렉토리와 파일을 포함할 수 있습니다.
    /// </summary>
    public sealed class VirtualDirectory : IVirtualNode
    {
        /// <summary>
        /// 새로운 <see cref="VirtualDirectory"/> 인스턴스를 초기화하고, 자신을 루트 디렉토리로 설정합니다.<br/>
        /// 이 생성자는 가상 파일 시스템의 최상위 루트 디렉토리를 생성할 때 사용됩니다.
        /// </summary>
        public VirtualDirectory()
        {
            _root = this;
            rootDirectoryCache = new Dictionary<FilePath, VirtualDirectory?>();
        }

        /// <summary>
        /// 지정된 루트 디렉토리와 부모 디렉토리를 가진 새로운 <see cref="VirtualDirectory"/> 인스턴스를 초기화합니다.<br/>
        /// 이 생성자는 하위 디렉토리를 생성할 때 내부적으로 사용됩니다.
        /// </summary>
        /// <param name="parent">이 디렉토리의 부모 디렉토리입니다. 루트 디렉토리인 경우 <see langword="null"/>일 수 있습니다.</param>
        /// <param name="name">이 디렉토리의 이름입니다.</param>
        VirtualDirectory(VirtualDirectory? parent, string name)
        {
            _root = parent?.root ?? this;
            _parent = parent;

            _name = name;
            _fullPath = parent?.fullPath + name;

            rootDirectoryCache = _root.rootDirectoryCache;
        }

        /// <summary>
        /// 이 디렉토리가 속한 가상 파일 시스템의 최상위 루트 디렉토리를 가져옵니다.<br/>
        /// 이 속성은 읽기 전용입니다.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 더 이상 가상 파일 시스템의 일부가 아니거나 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public VirtualDirectory root
        {
            get
            {
                ThrowIfDeletedException();
                return _root;
            }
        }
        readonly VirtualDirectory _root;

        /// <summary>
        /// 이 디렉토리의 부모 디렉토리를 가져옵니다.<br/>
        /// 이 속성은 읽기 전용이며, 루트 디렉토리인 경우 <see langword="null"/>입니다.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 더 이상 가상 파일 시스템의 일부가 아니거나 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public VirtualDirectory? parent
        {
            get
            {
                ThrowIfDeletedException();
                return _parent;
            }
        }
        readonly VirtualDirectory? _parent = null;

        /// <summary>
        /// 이 디렉토리의 이름입니다.<br/>
        /// 이 속성은 읽기 전용입니다.
        /// </summary>
        public string name
        {
            get
            {
                ThrowIfDeletedException();
                return _name;
            }
        }
        readonly string _name = string.Empty;

        /// <summary>
        /// 이 디렉토리의 전체 경로입니다.<br/>
        /// 이 속성은 읽기 전용입니다.
        /// </summary>
        public FilePath fullPath
        {
            get
            {
                ThrowIfDeletedException();
                return _fullPath;
            }
        }
        readonly FilePath _fullPath = FilePath.empty;
        FilePath? IVirtualNode.fullPath => fullPath;

        /// <summary>
        /// 이 가상 디렉토리가 독립적인 최상위 항목인지 여부를 나타내는 값을 가져옵니다.<br/>
        /// 즉, 이 항목이 다른 가상 파일 시스템 엔트리의 하위가 아닌, 스스로 루트 역할을 하는지 여부를 나타냅니다.
        /// </summary>
        public bool isIndependent
        {
            get
            {
                // isDeleted 상태에서도 isIndependent를 확인해야 할 수 있으므로 ThrowIfDeletedException()을 호출하지 않음
                // 하지만 isDeleted 상태라면 독립적이지 않다고 간주하는 것이 일반적
                if (isDeleted)
                    return false;

                return root == this && parent == null;
            }
        }

        /// <summary>
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 상위 디렉토리에서 제거되어 유효하지 않은 상태인지 나타내는 값입니다.
        /// </summary>
        public bool isDeleted { get; private set; } = false;

        /// <summary>
        /// 이 디렉토리의 직접적인 하위 항목(디렉토리 및 파일)을 저장하는 컬렉션입니다.<br/>
        /// 키는 항목의 이름(파일명 또는 디렉토리명)이며, 값은 해당 <see cref="IVirtualNode"/> 인스턴스입니다.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal readonly Dictionary<string, IVirtualNode> children = new();

        /// <summary>
        /// 지정된 경로에 해당하는 <see cref="VirtualDirectory"/> 인스턴스를 캐싱하여 가져옵니다.<br/>
        /// 이 캐시는 가상 파일 시스템의 구조가 변경될 때 무효화되어야 합니다.<br/>
        /// 루트 디렉토리가 아닐 경우, 항상 <see langword="null"/> 입니다.
        /// </summary>
        readonly Dictionary<FilePath, VirtualDirectory?> rootDirectoryCache;

        /// <summary>
        /// 지정된 경로에 새로운 디렉토리를 생성합니다.<br/>
        /// 중간 경로가 없으면 자동으로 생성됩니다.
        /// </summary>
        /// <param name="path">생성할 디렉토리의 경로입니다. 예: "assets/runios/textures", "assets/runios/sounds"</param>
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
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 더 이상 가상 파일 시스템의 일부가 아니거나 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public bool CreateDirectory(FilePath path)
        {
            ThrowIfDeletedException();

            if (path.IsEmpty())
                return false; // 빈 경로는 false 반환 (유효하지 않은 요청)

            bool isCreated = false;
            VirtualDirectory childDirectory = this;
            foreach (var directoryNameSpan in path.value.AsSpan().Split(FilePath.directorySeparatorChar))
            {
                string directoryName = new string(directoryNameSpan);
                if (childDirectory.children.ContainsKey(directoryName))
                {
                    var entry = children[directoryName];
                    if (entry is VirtualDirectory value)
                        childDirectory = value;
                    else
                    {
                        // 경로 중간에 파일이나 디렉토리가 아닌 다른 노드가 있는 경우
                        // 이는 비정상적인 상황이므로 예외를 던집니다.
                        ThrowPathIsFileException(path, directoryName);
                    }
                }
                else
                {
                    VirtualDirectory directory = new VirtualDirectory(childDirectory, directoryName);

                    InvalidateCache(); // 디렉토리 구조 변경 전에 캐시 무효화

                    childDirectory.children[directoryName] = directory;
                    childDirectory = directory;

                    isCreated = true;
                }
            }

            return isCreated;
        }

        /// <summary>
        /// 지정된 경로의 가상 디렉토리를 삭제합니다.
        /// </summary>
        /// <param name="path">삭제할 가상 디렉토리의 경로입니다. 예: "assets/runios/textures"</param>
        /// <returns>
        /// 디렉토리가 성공적으로 삭제되었으면 <see langword="true"/>를 반환하고, <br/>
        /// 해당 경로에 디렉토리가 존재하지 않거나 경로가 유효하지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <exception cref="DirectoryNotFoundException">
        /// 디렉토리를 삭제할 상위 디렉토리를 찾을 수 없거나, 경로 중간에 파일이 있어 디렉토리를 탐색할 수 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// 지정된 경로에 디렉토리가 아닌 파일과 같은 다른 유형의 항목이 존재하는 경우 발생합니다.<br/>
        /// 이 예외는 디렉토리를 기대했지만 실제로는 파일이 존재하는 상황에 사용됩니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 더 이상 가상 파일 시스템의 일부가 아니거나 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public bool DeleteDirectory(FilePath path)
        {
            ThrowIfDeletedException();

            VirtualDirectory? parentDirectory = GetDirectory(path.GetParentPath());
            if (parentDirectory == null)
                ThrowDirectoryNotFoundException(path);

            string directoryName = path.GetFileName();
            if (!parentDirectory.children.TryGetValue(directoryName, out IVirtualNode existingNode))
                return false; // 디렉토리가 존재하지 않으므로 false 반환

            if (existingNode is not VirtualDirectory)
            {
                // ThrowPathIsFileException은 DirectoryNotFoundException을 던집니다.
                // 이는 디렉토리를 기대했지만 대상 경로에 파일이 있을 때 발생하는 예외입니다.
                ThrowPathIsFileException(path, directoryName);
            }

            existingNode.Delete();
            return true;
        }

        /// <summary>
        /// 지정된 경로에 해당하는 <see cref="VirtualDirectory"/> 인스턴스를 가져옵니다.
        /// 이 메서드는 내부 캐시를 사용하여 성능을 최적화합니다.
        /// </summary>
        /// <param name="path">가져올 디렉토리의 경로입니다. 예: "assets/runios/textures", "assets/runios/sounds"</param>
        /// <returns>
        /// 지정된 경로의 <see cref="VirtualDirectory"/> 인스턴스이거나,<br/>
        /// 해당 경로의 디렉토리를 찾을 수 없는 경우 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 더 이상 가상 파일 시스템의 일부가 아니거나 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public VirtualDirectory? GetDirectory(FilePath path)
        {
            ThrowIfDeletedException();

            // 캐시에서 먼저 시도
            if (rootDirectoryCache.TryGetValue(fullPath + path, out VirtualDirectory? cachedDirectory))
            {
                // 캐시된 값이 null이라면 해당 경로에 디렉토리가 없음을 의미
                return cachedDirectory;
            }

            if (path.IsEmpty())
            {
                rootDirectoryCache[fullPath + path] = this; // 이 인스턴스의 디렉토리 캐싱
                return this;
            }

            VirtualDirectory childDirectory = this;
            foreach (var directoryName in path.value.AsSpan().Split(FilePath.directorySeparatorChar))
            {
                if (childDirectory.children.TryGetValue(new string(directoryName), out IVirtualNode existingNode) && existingNode is VirtualDirectory valueDirectory)
                {
                    childDirectory = valueDirectory;
                    continue;
                }

                // 찾지 못한 경우 캐시에 null을 저장하고 null 반환
                rootDirectoryCache[fullPath + path] = null;
                return null;
            }

            rootDirectoryCache[fullPath + path] = childDirectory; // 찾은 디렉토리 캐싱
            return childDirectory;
        }

        /// <summary>
        /// 지정된 경로에 가상 파일을 씁니다.<br/>
        /// 파일이 위치할 디렉토리는 자동으로 생성되지 않습니다. 미리 <see cref="CreateDirectory(FilePath)"/>를 통해 생성해야 합니다.
        /// </summary>
        /// <param name="path">파일을 쓸 경로입니다. 예: "assets/runios/sounds.json"</param>
        /// <param name="virtualFile">쓸 <see cref="VirtualFile"/> 인스턴스입니다.</param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="virtualFile"/> 인스턴스가 이미 다른 디렉토리와 연결되어 독립적이지 않은 경우 발생합니다.<br/>
        /// 파일을 새 위치에 쓰려면 명시적으로 이동하거나 복사해야 합니다.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// 파일을 쓸 상위 디렉토리를 찾을 수 없거나, 경로 중간에 파일이 있어 디렉토리를 탐색할 수 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// 지정된 경로에 파일이 아닌 디렉토리와 같은 다른 유형의 항목이 존재하는 경우 발생합니다.<br/>
        /// 이 예외는 파일을 기대했지만 실제로는 디렉토리가 존재하는 상황에 사용되며, 해당 디렉토리에 파일 작업이 허용되지 않음을 나타냅니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 더 이상 가상 파일 시스템의 일부가 아니거나 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public void FileWrite(FilePath path, VirtualFile virtualFile)
        {
            ThrowIfDeletedException();

            if (!virtualFile.isIndependent)
                throw new InvalidOperationException("The virtual file is already associated with another directory and cannot be written to a new location without being explicitly moved or copied.");

            VirtualDirectory? directory = GetDirectory(path.GetParentPath());
            if (directory == null)
                ThrowDirectoryNotFoundException(path);

            string fileName = path.GetFileName();

            // 대상 경로에 이미 다른 종류의 노드(예: 디렉토리)가 있다면 예외 발생
            if (directory.children.TryGetValue(fileName, out IVirtualNode? existingNode) && existingNode is not VirtualFile)
            {
                // ThrowPathIsDirectoryException은 UnauthorizedAccessException을 던집니다.
                // 이는 파일을 기대했지만 대상 경로에 디렉토리가 있을 때 발생하는 예외입니다.
                ThrowPathIsDirectoryException(path, fileName);
            }

            InvalidateCache(); // 디렉토리 구조 변경 전에 캐시 무효화

            // 파일 쓰기 또는 기존 파일 덮어쓰기
            directory.children[fileName] = virtualFile;

            // 파일의 루트와 부모 디렉토리 설정
            virtualFile.root = directory.root;
            virtualFile.parent = directory;

            virtualFile.name = fileName;
            virtualFile.fullPath = fullPath + path;

            virtualFile.metaData = new IOMetaData
            {
                name = fileName,
                lastWriteTime = DateTime.UtcNow,
                attributes = FileAttributes.Normal
            };
        }

        /// <summary>
        /// 지정된 경로의 가상 파일에 데이터를 쓰기 위한 스트림을 엽니다.
        /// 쓰기 시작 시점에 새 <see cref="VirtualFile"/> 인스턴스를 만들고 해당 경로의 기존 파일을 대체합니다.
        /// </summary>
        /// <param name="path">쓰기 스트림을 열 파일 경로입니다.</param>
        /// <returns>파일 내용을 쓸 수 있는 <see cref="Stream"/> 스트림입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">
        /// 파일을 쓸 상위 디렉토리를 찾을 수 없거나, 경로 중간에 파일이 있어 디렉토리를 탐색할 수 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// 지정된 경로에 디렉토리가 존재하는 경우 발생합니다.
        /// </exception>
        public UniTask<Stream> OpenWrite(FilePath path)
        {
            ThrowIfDeletedException();

            VirtualDirectory? directory = GetDirectory(path.GetParentPath());
            if (directory == null)
                ThrowDirectoryNotFoundException(path);

            string fileName = path.GetFileName();
            if (directory.children.TryGetValue(fileName, out IVirtualNode? existingNode) && existingNode is not VirtualFile)
            {
                ThrowPathIsDirectoryException(path, fileName);
            }

            VirtualFile virtualFile = new VirtualFile([]);
            FileWrite(path, virtualFile);
            return virtualFile.OpenWrite();
        }

        /// <summary>
        /// 지정된 경로의 가상 파일을 삭제합니다.
        /// </summary>
        /// <param name="path">삭제할 가상 파일의 경로입니다. 예: "assets/runios/sounds.json"</param>
        /// <returns>
        /// 파일이 성공적으로 삭제되었으면 <see langword="true"/>를 반환하고, <br/>
        /// 해당 경로에 파일이 존재하지 않거나 경로가 유효하지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <exception cref="DirectoryNotFoundException">
        /// 파일을 삭제할 상위 디렉토리를 찾을 수 없거나, 경로 중간에 파일이 있어 디렉토리를 탐색할 수 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// 지정된 경로에 파일이 아닌 디렉토리와 같은 다른 유형의 항목이 존재하는 경우 발생합니다.<br/>
        /// 이 예외는 파일을 기대했지만 실제로는 디렉토리가 존재하는 상황에 사용되며, 해당 디렉토리에 파일 작업이 허용되지 않음을 나타냅니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 더 이상 가상 파일 시스템의 일부가 아니거나 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public bool DeleteFile(FilePath path)
        {
            ThrowIfDeletedException();

            VirtualDirectory? parentDirectory = GetDirectory(path.GetParentPath());
            if (parentDirectory == null)
                ThrowDirectoryNotFoundException(path);

            string fileName = path.GetFileName();
            if (!parentDirectory.children.TryGetValue(fileName, out IVirtualNode existingNode))
                return false; // 파일이 존재하지 않으므로 false 반환

            if (existingNode is not VirtualFile)
            {
                // ThrowPathIsDirectoryException은 UnauthorizedAccessException을 던집니다.
                // 이는 파일을 기대했지만 대상 경로에 디렉토리가 있을 때 발생하는 예외입니다.
                ThrowPathIsDirectoryException(path, fileName);
            }

            existingNode.Delete();
            return true;
        }

        /// <summary>
        /// 지정된 경로에 해당하는 <see cref="VirtualFile"/> 인스턴스를 가져옵니다.
        /// </summary>
        /// <param name="path">가져올 파일의 경로입니다. 예: "assets/runios/textures/player.png", "assets/runios/sounds.json"</param>
        /// <returns>
        /// 지정된 경로의 <see cref="VirtualFile"/> 인스턴스이거나,<br/>
        /// 해당 경로의 파일을 찾을 수 없거나 파일이 위치한 상위 디렉토리를 찾을 수 없는 경우 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 더 이상 가상 파일 시스템의 일부가 아니거나 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public VirtualFile? GetFile(FilePath path)
        {
            ThrowIfDeletedException();

            FilePath parentPath = path.GetParentPath();
            string fileName = path.GetFileName();

            return GetDirectory(parentPath)?.children.GetValueOrDefault(fileName) as VirtualFile ?? null;
        }

        /// <summary>
        /// 지정된 경로의 파일 또는 디렉토리 엔트리 스냅샷을 가져옵니다.
        /// </summary>
        public IOEntry? GetEntry(FilePath path)
        {
            ThrowIfDeletedException();

            if (path.IsEmpty())
                return CreateDirectoryEntry(path);

            VirtualDirectory? directory = GetDirectory(path);
            if (directory != null)
                return CreateDirectoryEntry(path);

            VirtualFile? file = GetFile(path);
            if (file != null)
                return CreateFileEntry(path, file);

            return null;
        }

        /// <summary>
        /// 지정된 디렉토리 경로 내의 파일 및 하위 디렉토리 엔트리 스냅샷을 열거합니다.
        /// </summary>
        public IEnumerable<IOEntry> EnumerateEntries(FilePath path, bool recursive)
        {
            ThrowIfDeletedException();

            VirtualDirectory? initialDirectory = GetDirectory(path);
            if (initialDirectory == null)
                ThrowDirectoryNotFoundException(path);

            if (!recursive)
            {
                foreach (KeyValuePair<string, IVirtualNode> child in initialDirectory.children)
                    yield return CreateEntry(path + child.Key, child.Value);

                yield break;
            }

            Stack<(FilePath currentPath, VirtualDirectory directory)> stack = new Stack<(FilePath currentPath, VirtualDirectory directory)>();
            stack.Push((path, initialDirectory));

            while (stack.Count > 0)
            {
                (FilePath currentPath, VirtualDirectory currentDirectory) = stack.Pop();

                foreach (KeyValuePair<string, IVirtualNode> child in currentDirectory.children)
                {
                    FilePath childPath = currentPath + child.Key;
                    yield return CreateEntry(childPath, child.Value);

                    if (child.Value is VirtualDirectory childDirectory)
                        stack.Push((childPath, childDirectory));
                }
            }
        }

        static IOEntry CreateEntry(FilePath path, IVirtualNode node) => node switch
        {
            VirtualDirectory => CreateDirectoryEntry(path),
            VirtualFile file => CreateFileEntry(path, file),
            _ => throw new InvalidDataException($"Unknown virtual node type '{node.GetType().Name}' at path '{path}'.")
        };

        static IOEntry CreateDirectoryEntry(FilePath path) => new IOEntry
        {
            path = path,
            metaData = new IOMetaData
            {
                name = path.GetFileName(),
                attributes = FileAttributes.Directory
            },
            isDirectory = true
        };

        static IOEntry CreateFileEntry(FilePath path, VirtualFile file)
        {
            IOMetaData fileMetaData = file.metaData ?? new IOMetaData(path.GetFileName());

            return new IOEntry
            {
                path = path,
                metaData = new IOMetaData
                {
                    name = fileMetaData.name,
                    size = fileMetaData.size,
                    creationTime = fileMetaData.creationTime,
                    lastAccessTime = fileMetaData.lastAccessTime,
                    lastWriteTime = fileMetaData.lastWriteTime,
                    attributes = fileMetaData.attributes
                },
                isDirectory = false
            };
        }



        /// <summary>
        /// 루트 디렉토리 인스턴스에 대한 캐시를 무효화합니다.
        /// </summary>
        public void InvalidateCache() => rootDirectoryCache.Clear();



        /// <summary>
        /// 이 디렉토리의 인스턴스를 상위 디렉토리에서 제거합니다
        /// 이 디렉토리의 모든 하위 항목(디렉토리 및 파일)도 재귀적으로 제거합니다.
        /// </summary>
        public void Delete()
        {
            ThrowIfDeletedException();

            InvalidateCache(); // 디렉토리 구조 변경 전에 캐시 무효화
            parent?.children.Remove(name);

            foreach (var item in children.ToList())
                item.Value.Delete();

            isDeleted = true; // 현재 디렉토리의 상태 업데이트
        }



        /// <summary>
        /// 이 <see cref="VirtualDirectory"/> 인스턴스의 <see cref="isDeleted"/> 상태가 <see langword="true"/>일 때 예외를 던집니다.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 더 이상 가상 파일 시스템의 일부가 아니거나 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        void ThrowIfDeletedException()
        {
            if (isDeleted)
                throw new ObjectDisposedException(nameof(VirtualDirectory), $"This '{nameof(VirtualDirectory)}' instance is no longer part of the virtual file system and is invalid for operations.");
        }

        /// <summary>
        /// 항상 예외를 던집니다.<br/>
        /// 이는 지정된 경로의 디렉토리를 찾을 수 없거나, 경로 중간에 파일이 있어 티렉토리를 탐색할 수 없는 상황에 사용됩니다.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException">
        /// 지정된 경로의 디렉토리를 찾을 수 없거나, 경로 중간에 파일이 있어 디렉토리를 탐색할 수 없는 경우 발생합니다.
        /// </exception>
        [DoesNotReturn]
        static void ThrowDirectoryNotFoundException(FilePath path) => throw new DirectoryNotFoundException($"The directory at path '{path}' was not found or is invalid.");


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
        static void ThrowPathIsFileException(FilePath path, string segmentName)
        {
            throw new DirectoryNotFoundException(
                $"Path operation failed for '{path}'. " +
                $"The segment '{segmentName}' is a file or another non-directory item, " +
                $"but a directory was expected."
            );
        }

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
        static void ThrowPathIsDirectoryException(FilePath path, string segmentName)
        {
            throw new UnauthorizedAccessException(
                $"Path operation failed for '{path}'. " +
                $"The segment '{segmentName}' is a directory or another non-file item, " +
                $"but a file was expected. Direct file operations on a directory are not permitted."
            );
        }
    }
}
