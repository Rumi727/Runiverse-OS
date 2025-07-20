#nullable enable
using RuniEngine.IO;
using RuniEngine.Spans;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace RuniEngine
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
            rootDirectoryCache = new();
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

            rootDirectoryCache = null;
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
        /// 이 가상 파일 시스템 노드(디렉토리 또는 파일)가 독립적인 최상위 항목인지 여부를 나타내는 값을 가져옵니다.<br/>
        /// 즉, 이 항목이 다른 가상 파일 시스템 엔트리의 하위가 아닌, 스스로 루트 역할을 하는지 여부를 나타냅니다.
        /// </summary>
        public bool isIndependent
        {
            get
            {
                // isDeleted 상태에서도 isIndependent를 확인해야 할 수 있으므로 ThrowDeletedException()을 호출하지 않음
                // 하지만 isDeleted 상태라면 독립적이지 않다고 간주하는 것이 일반적
                if (isDeleted)
                    return false;

                return root == this && parent == null;
            }
        }

        /// <summary>
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 상위 디렉토리에서 제거되어 유효하지 않은 상태인지 나타내는 값입니다.
        /// 이 속성을 <see langword="true"/>로 설정하면, 해당 디렉토리의 모든 하위 항목(디렉토리 및 파일)의 상태도 재귀적으로 <see langword="true"/>로 설정됩니다.
        /// </summary>
        public bool isDeleted { get; private set; } = false;

        /// <summary>
        /// 이 디렉토리의 직접적인 하위 항목(디렉토리 및 파일)을 저장하는 컬렉션입니다.<br/>
        /// 키는 항목의 이름(파일명 또는 디렉토리명)이며, 값은 해당 <see cref="IVirtualNode"/> 인스턴스입니다.
        /// </summary>
        readonly Dictionary<string, IVirtualNode> children = new();

        /// <summary>
        /// 지정된 경로에 해당하는 <see cref="VirtualDirectory"/> 인스턴스를 캐싱하여 가져옵니다.<br/>
        /// 이 캐시는 가상 파일 시스템의 구조가 변경될 때 무효화되어야 합니다.<br/>
        /// 루트 디렉토리가 아닐 경우, 항상 <see langword="null"/> 입니다.
        /// </summary>
        readonly Dictionary<FilePath, VirtualDirectory?>? rootDirectoryCache = null;

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
                string directoryName = new string(directoryNameSpan.AsSpan());
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

                    if (isCreated)
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

            InvalidateCache(); // 디렉토리 구조 변경 전에 캐시 무효화

            parentDirectory.children.Remove(path.GetFileName());
            existingNode.SetDeleted(); // 디렉토리의 isDeleted 상태를 true로 설정 (하위 항목 포함)

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

            if (rootDirectoryCache != null)
            {
                // 캐시에서 먼저 시도
                if (rootDirectoryCache.TryGetValue(fullPath + path, out VirtualDirectory? cachedDirectory))
                {
                    // 캐시된 값이 null이라면 해당 경로에 디렉토리가 없음을 의미
                    if (cachedDirectory == null)
                        return null;

                    return cachedDirectory;
                }

                if (path.IsEmpty())
                {
                    rootDirectoryCache[fullPath + path] = this; // 이 인스턴스의 디렉토리 캐싱
                    return this;
                }
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
                if (rootDirectoryCache != null)
                    rootDirectoryCache[fullPath + path] = null;

                return null;
            }

            if (rootDirectoryCache != null)
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

            if (virtualFile.isIndependent)
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

            InvalidateCache(); // 디렉토리 구조 변경 전에 캐시 무효화

            parentDirectory.children.Remove(path.GetFileName());
            existingNode.SetDeleted(); // 파일의 isDeleted 상태를 true로 설정

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
        /// 지정된 경로에 있는 모든 직접적인 하위 디렉토리의 이름을 가져옵니다.
        /// </summary>
        /// <param name="path">하위 디렉토리를 검색할 디렉토리의 경로입니다.</param>
        /// <returns>해당 디렉토리의 모든 직접적인 하위 디렉토리 이름 컬렉션입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">
        /// 지정된 경로의 디렉토리를 찾을 수 없거나, 경로 중간에 파일이 있어 디렉토리를 탐색할 수 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 더 이상 가상 파일 시스템의 일부가 아니거나 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public IEnumerable<string> GetDirectories(FilePath path)
        {
            ThrowIfDeletedException();

            VirtualDirectory? directory = GetDirectory(path);
            if (directory == null)
                ThrowDirectoryNotFoundException(path);

            return directory.children.Where(x => x.Value is VirtualDirectory).Select(x => x.Key);
        }

        /// <summary>
        /// 지정된 경로를 시작으로 모든 하위 디렉토리의 전체 경로를 깊이 우선 탐색(DFS) 방식으로 가져옵니다.<br/>
        /// 시작 경로 자체는 결과에 포함되지 않습니다.
        /// </summary>
        /// <param name="path">탐색을 시작할 디렉토리의 경로입니다.</param>
        /// <returns>시작 경로의 모든 하위 디렉토리의 전체 경로 컬렉션입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">
        /// 지정된 경로의 디렉토리를 찾을 수 없거나, 경로 중간에 파일이 있어 디렉토리를 탐색할 수 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 더 이상 가상 파일 시스템의 일부가 아니거나 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public IEnumerable<FilePath> GetAllDirectories(FilePath path) => InternalGetAllDirectories(path, false).Select(x => x.Key);

        /// <summary>
        /// 재귀적으로 모든 하위 디렉토리를 탐색하여 경로와 <see cref="VirtualDirectory"/> 쌍을 반환합니다.<br/>
        /// 깊이 우선 탐색(DFS) 방식을 사용합니다.
        /// </summary>
        /// <param name="path">탐색을 시작할 디렉토리의 경로입니다.</param>
        /// <param name="includeSelf">탐색 시작 디렉토리 자체를 결과에 포함할지 여부입니다.</param>
        /// <returns>탐색된 모든 디렉토리의 <see cref="FilePath"/>와 <see cref="VirtualDirectory"/> 쌍 컬렉션입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">
        /// 지정된 경로의 디렉토리를 찾을 수 없거나, 경로 중간에 파일이 있어 디렉토리를 탐색할 수 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 더 이상 가상 파일 시스템의 일부가 아니거나 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        IEnumerable<KeyValuePair<FilePath, VirtualDirectory>> InternalGetAllDirectories(FilePath path, bool includeSelf)
        {
            ThrowIfDeletedException();

            VirtualDirectory? initialDirectory = GetDirectory(path);
            if (initialDirectory == null)
                ThrowDirectoryNotFoundException(path);

            if (includeSelf)
                yield return new(path, initialDirectory);

            // DFS(깊이 우선 탐색)를 위해 Stack 사용
            Stack<(FilePath currentPath, VirtualDirectory dir)> stack = new Stack<(FilePath currentPath, VirtualDirectory dir)>();
            stack.Push((path, initialDirectory)); // 시작 디렉토리를 스택에 추가

            while (stack.Count > 0)
            {
                (FilePath currentPath, VirtualDirectory currentDir) = stack.Pop(); // 스택에서 디렉토리와 현재 경로를 꺼냄

                // Dictionary의 ValueCollection을 직접 순회
                foreach (var item in currentDir.children.Reverse()) // children이 Dictionary인 경우 Values 사용
                {
                    if (item.Value is VirtualDirectory childDirectory)
                    {
                        FilePath newPath = currentPath + item.Key;
                        yield return new(newPath, childDirectory); // 자식 디렉토리와 조합된 경로 반환

                        stack.Push((newPath, childDirectory)); // 자식 디렉토리와 새로운 경로를 스택에 추가하여 나중에 탐색
                    }
                }
            }
        }

        /// <summary>
        /// 지정된 경로에 있는 모든 직접적인 하위 파일의 이름을 가져옵니다.
        /// </summary>
        /// <param name="path">파일을 검색할 디렉토리의 경로입니다.</param>
        /// <returns>해당 디렉토리의 모든 직접적인 하위 파일 이름 컬렉션입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">
        /// 지정된 경로의 디렉토리를 찾을 수 없거나, 경로 중간에 파일이 있어 디렉토리를 탐색할 수 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 더 이상 가상 파일 시스템의 일부가 아니거나 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public IEnumerable<string> GetFiles(FilePath path)
        {
            ThrowIfDeletedException();

            VirtualDirectory? directory = GetDirectory(path);
            if (directory == null)
                ThrowDirectoryNotFoundException(path);

            return directory.children.Where(x => x.Value is VirtualFile).Select(x => x.Key);
        }

        /// <summary>
        /// 지정된 경로를 시작으로 모든 하위 디렉토리의 파일을 포함하여 모든 파일의 전체 경로를 가져옵니다.
        /// </summary>
        /// <param name="path">탐색을 시작할 디렉토리의 경로입니다.</param>
        /// <returns>시작 경로의 모든 하위 디렉토리에 있는 모든 파일의 전체 경로 컬렉션입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">
        /// 지정된 경로의 디렉토리를 찾을 수 없거나, 경로 중간에 파일이 있어 디렉토리를 탐색할 수 없는 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 더 이상 가상 파일 시스템의 일부가 아니거나 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public IEnumerable<FilePath> GetAllFiles(FilePath path)
        {
            ThrowIfDeletedException();

            var directories = InternalGetAllDirectories(path, true);
            return directories.SelectMany(directoryItem =>
                directoryItem.Value.children
                    .Where(x => x.Value is VirtualFile)
                    .Select(fileItem => directoryItem.Key + fileItem.Key));
        }



        /// <summary>
        /// 루트 디렉토리 인스턴스에 대한 캐시를 무효화합니다.<br/>
        /// 디렉토리 구조가 변경될 때마다 이 메서드를 호출하여 캐시 일관성을 유지해야 합니다.
        /// </summary>
        void InvalidateCache() => root.rootDirectoryCache?.Clear();



        /// <summary>
        /// 이 디렉토리의 인스턴스를 상위 디렉토리에서 제거되어 유효하지 않은 상태로 설정합니다
        /// 이 디렉토리의 모든 하위 항목(디렉토리 및 파일)의 상태도 재귀적으로 <see langword="true"/>로 설정됩니다.
        /// </summary>
        void SetDeleted()
        {
            isDeleted = true; // 현재 디렉토리의 상태 업데이트
            foreach (var item in children)
                item.Value.SetDeleted();
        }
        void IVirtualNode.SetDeleted() => SetDeleted();



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
        void ThrowDirectoryNotFoundException(FilePath path) => throw new DirectoryNotFoundException($"The directory at path '{path}' was not found or is invalid.");


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
        void ThrowPathIsFileException(FilePath path, string segmentName)
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
        void ThrowPathIsDirectoryException(FilePath path, string segmentName)
        {
            throw new UnauthorizedAccessException(
                $"Path operation failed for '{path}'. " +
                $"The segment '{segmentName}' is a directory or another non-file item, " +
                $"but a file was expected. Direct file operations on a directory are not permitted."
            );
        }
    }
}